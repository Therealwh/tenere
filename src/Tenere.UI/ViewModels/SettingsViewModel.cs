using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tenere.Core.Abstractions;
using Tenere.Shared.Localization;
using Tenere.Shared.Settings;

namespace Tenere.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IClipRepository _repository;

    [ObservableProperty] private string language = "en";
    [ObservableProperty] private string theme = "system";
    [ObservableProperty] private string hotkey = "Ctrl+Shift+V";
    [ObservableProperty] private string retentionDays = "30";
    [ObservableProperty] private bool privacyShieldEnabled = true;
    [ObservableProperty] private string excludedApps = string.Empty;
    [ObservableProperty] private bool ocrEnabled = true;
    [ObservableProperty] private bool plainTextDefault;
    [ObservableProperty] private string newProcessName = string.Empty;
    [ObservableProperty] private string newChainSteps = string.Empty;
    [ObservableProperty] private bool newRuleEnabled = true;

    public ObservableCollection<AppRuleInfo> Rules { get; } = new();

    public event EventHandler? RequestClose;

    public SettingsViewModel(ISettingsService settings, IClipRepository repository)
    {
        _settings = settings;
        _repository = repository;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var current = _settings.Current;
        Language = current.General.Language;
        Theme = current.Appearance.Theme;
        Hotkey = current.General.HotkeyToggle;
        RetentionDays = current.History.RetentionDays.ToString(CultureInfo.InvariantCulture);
        PrivacyShieldEnabled = current.Privacy.ShieldEnabled;
        ExcludedApps = string.Join(", ", current.Privacy.ExcludedApps);
        OcrEnabled = current.Ocr.Enabled;
        PlainTextDefault = current.Paste.PlainTextByDefault;

        Rules.Clear();
        foreach (var rule in await _repository.GetAppRulesAsync(CancellationToken.None))
            Rules.Add(rule);
    }

    [RelayCommand]
    private void Save()
    {
        var current = _settings.Current;
        current.General.Language = string.IsNullOrWhiteSpace(Language) ? "en" : Language.Trim();
        current.Appearance.Theme = string.IsNullOrWhiteSpace(Theme) ? "system" : Theme.Trim();
        current.General.HotkeyToggle = string.IsNullOrWhiteSpace(Hotkey) ? "Ctrl+Shift+V" : Hotkey.Trim();
        current.History.RetentionDays = int.TryParse(RetentionDays, out var days) && days > 0 ? days : 30;
        current.Privacy.ShieldEnabled = PrivacyShieldEnabled;
        current.Privacy.ExcludedApps = ExcludedApps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        current.Ocr.Enabled = OcrEnabled;
        current.Paste.PlainTextByDefault = PlainTextDefault;
        _settings.Save();

        try
        {
            Strings.Culture = new CultureInfo(current.General.Language);
        }
        catch (CultureNotFoundException)
        {
            Strings.Culture = CultureInfo.CurrentUICulture;
        }
        ThemeManager.Apply(System.Windows.Application.Current.Resources, current, ThemeManager.SystemUsesLightTheme());

        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private async Task AddRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProcessName)) return;
        var steps = NewChainSteps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var chainJson = System.Text.Json.JsonSerializer.Serialize(steps);
        await _repository.SaveAppRuleAsync(NewProcessName.Trim(), chainJson, NewRuleEnabled, CancellationToken.None);
        NewProcessName = string.Empty;
        NewChainSteps = string.Empty;
        Rules.Clear();
        foreach (var rule in await _repository.GetAppRulesAsync(CancellationToken.None))
            Rules.Add(rule);
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(AppRuleInfo rule)
    {
        await _repository.SaveAppRuleAsync(rule.ProcessName, rule.ChainJson, false, CancellationToken.None);
        Rules.Remove(rule);
    }
}