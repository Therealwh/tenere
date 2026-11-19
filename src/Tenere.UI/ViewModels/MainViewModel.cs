using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;
using Tenere.Shared.Localization;
using Tenere.Shared.Settings;

namespace Tenere.UI.ViewModels;

public enum ClipFilter
{
    All,
    Images,
    Links,
    Code,
    Text
}

public sealed partial class FilterOption : ObservableObject
{
    public FilterOption(ClipFilter key, string name)
    {
        Key = key;
        Name = name;
    }

    public ClipFilter Key { get; }
    public string Name { get; }
}

public sealed partial class MainViewModel : ObservableObject
{
    private const int SearchDebounceMs = 150;
    private const int ResultLimit = 200;

    private readonly IClipRepository _repository;
    private readonly IClipboardWriter _writer;
    private readonly ISettingsService _settings;
    private readonly ILogger<MainViewModel> _logger;
    private readonly Func<SettingsWindow> _settingsWindowFactory;
    private readonly IAppRuleService _appRules;
    private readonly ITransformPipeline _pipeline;
    private readonly Func<string?> _targetAppResolver;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private FilterOption? selectedFilter;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private int itemCount;

    [ObservableProperty]
    private ClipItem? selectedItem;

    [ObservableProperty]
    private CollectionInfo? selectedCollection;

    [ObservableProperty]
    private string newCollectionName = string.Empty;

    public ObservableCollection<ClipItem> Items { get; } = new();

    public ObservableCollection<CollectionInfo> Collections { get; } = new();

    public ObservableCollection<FilterOption> Filters { get; }

    public bool HasItems => Items.Count > 0;

    public MainViewModel(
        IClipRepository repository,
        IClipboardWriter writer,
        ISettingsService settings,
        ILogger<MainViewModel> logger,
        Func<SettingsWindow> settingsWindowFactory,
        IAppRuleService appRules,
        ITransformPipeline pipeline,
        Func<string?> targetAppResolver)
    {
        _repository = repository;
        _writer = writer;
        _settings = settings;
        _logger = logger;
        _settingsWindowFactory = settingsWindowFactory;
        _appRules = appRules;
        _pipeline = pipeline;
        _targetAppResolver = targetAppResolver;

        Filters = new ObservableCollection<FilterOption>
        {
            new(ClipFilter.All, Strings.All),
            new(ClipFilter.Images, Strings.Images),
            new(ClipFilter.Links, Strings.Links),
            new(ClipFilter.Code, Strings.Code),
            new(ClipFilter.Text, Strings.Text)
        };

        SelectedFilter = Filters[0];

        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasItems));
            ItemCount = Items.Count;
        };

        _ = LoadCollectionsAsync();
    }

    partial void OnSelectedCollectionChanged(CollectionInfo? value) => _ = DebouncedSearchAsync();

    private async Task LoadCollectionsAsync()
    {
        try
        {
            var collections = await _repository.GetCollectionsAsync(CancellationToken.None);
            OnUi(() =>
            {
                Collections.Clear();
                foreach (var collection in collections)
                    Collections.Add(collection);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loading collections failed");
        }
    }

    partial void OnSearchTextChanged(string? value) => _ = DebouncedSearchAsync();

    partial void OnSelectedFilterChanged(FilterOption? value) => _ = DebouncedSearchAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        CancelPendingSearch();
        await RunSearchAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task PasteAsync(ClipItem item)
    {
        try
        {
            var target = _targetAppResolver();
            var rule = string.IsNullOrWhiteSpace(target) ? null : await _appRules.FindAsync(target, CancellationToken.None);
            var effective = item;
            if (rule is { Enabled: true } && rule.Chain.Count > 0 && !string.IsNullOrEmpty(item.PlainText))
            {
                var transformed = _pipeline.Apply(item.PlainText, rule.Chain);
                effective = new ClipItem
                {
                    Id = item.Id,
                    ContentType = item.ContentType,
                    Title = item.Title,
                    PlainText = transformed,
                    Html = item.Html,
                    ImagePath = item.ImagePath,
                    SourceApp = item.SourceApp,
                    CreatedAt = item.CreatedAt,
                    Pinned = item.Pinned,
                    CollectionId = item.CollectionId,
                    OcrStatus = item.OcrStatus,
                    OcrText = item.OcrText,
                    ContentHash = item.ContentHash
                };
            }
            await _writer.PasteAsync(effective, CancellationToken.None);
            if (_settings.Current.Paste.RestoreClipboardAfterPaste)
                await _writer.RestorePreviousAsync(CancellationToken.None);
            StatusMessage = "Pasted to clipboard.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste failed");
            StatusMessage = "Paste failed.";
        }
    }

    [RelayCommand]
    private async Task PinAsync(ClipItem item)
    {
        try
        {
            item.Pinned = !item.Pinned;
            await _repository.UpdateAsync(item, CancellationToken.None);
            var index = Items.IndexOf(item);
            if (index >= 0)
                Items[index] = item;
            StatusMessage = item.Pinned ? "Pinned." : "Unpinned.";
        }
        catch (Exception ex)
        {
            item.Pinned = !item.Pinned;
            _logger.LogError(ex, "Pin toggle failed");
            StatusMessage = "Could not update pin state.";
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(ClipItem item)
    {
        try
        {
            await _repository.DeleteAsync(item.Id, CancellationToken.None);
            Items.Remove(item);
            StatusMessage = "Deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed");
            StatusMessage = "Delete failed.";
        }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        try
        {
            await _repository.ClearAllAsync(CancellationToken.None);
            Items.Clear();
            StatusMessage = "History cleared.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear all failed");
            StatusMessage = "Could not clear history.";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var window = _settingsWindowFactory();
        window.Show();
    }

    [RelayCommand]
    private async Task NewCollectionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName)) return;
        await _repository.AddCollectionAsync(NewCollectionName.Trim(), CancellationToken.None);
        NewCollectionName = string.Empty;
        await LoadCollectionsAsync();
    }

    [RelayCommand]
    private async Task MoveToCollectionAsync(CollectionInfo collection)
    {
        if (SelectedItem is null) return;
        await _repository.AssignCollectionAsync(SelectedItem.Id, collection.Id, CancellationToken.None);
        SelectedItem.CollectionId = collection.Id;
        StatusMessage = "Moved to collection.";
        await LoadCollectionsAsync();
        await RunSearchAsync(CancellationToken.None);
    }

    private async Task DebouncedSearchAsync()
    {
        CancelPendingSearch();
        var cts = new CancellationTokenSource();
        _searchCts = cts;
        try
        {
            await Task.Delay(SearchDebounceMs, cts.Token);
            await RunSearchAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer query.
        }
    }

    private void CancelPendingSearch()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
    }

    private async Task RunSearchAsync(CancellationToken ct)
    {
        OnUi(() => IsLoading = true);
        try
        {
            var type = SelectedFilter?.Key switch
            {
                ClipFilter.Images => ClipContentType.Image,
                ClipFilter.Links => ClipContentType.Link,
                ClipFilter.Code => ClipContentType.Code,
                ClipFilter.Text => ClipContentType.Text,
                _ => (ClipContentType?)null
            };

            var text = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var query = new ClipQuery(text, type, null, null, SelectedCollection?.Id, null, null, ResultLimit, 0);
            var results = await _repository.SearchAsync(query, ct);

            OnUi(() =>
            {
                Items.Clear();
                foreach (var item in results)
                    Items.Add(item);
                StatusMessage = $"{Items.Count} item(s)";
            });
        }
        catch (OperationCanceledException)
        {
            // Search was superseded or the view model is shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clip search failed");
            OnUi(() => StatusMessage = "Search failed.");
        }
        finally
        {
            OnUi(() => IsLoading = false);
        }
    }

    private static void OnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }
}