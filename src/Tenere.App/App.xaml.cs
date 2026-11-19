using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;
using Tenere.Core.Services;
using Tenere.Data;
using Tenere.Ocr;
using Tenere.Platform.Interop;
using Tenere.Shared.Localization;
using Tenere.Shared.Logging;
using Tenere.Shared.Settings;
using Tenere.UI;
using Tenere.UI.ViewModels;

namespace Tenere.App;

/// <summary>
/// Composition root. Wires settings, logging, persistence, capture and the UI.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;
    private IClipboardMonitor? _monitor;
    private CancellationTokenSource? _shutdown;
    private ILogger<App>? _logger;
    private MainViewModel? _viewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var collection = new ServiceCollection();
            ConfigureServices(collection);
            _services = collection.BuildServiceProvider();
            _logger = _services.GetRequiredService<ILogger<App>>();

            var settings = _services.GetRequiredService<ISettingsService>();
            settings.Load();

            try
            {
                Strings.Culture = new CultureInfo(settings.Current.General.Language);
            }
            catch (CultureNotFoundException)
            {
                Strings.Culture = CultureInfo.CurrentUICulture;
            }

            ThemeManager.Apply(Resources, settings.Current, ThemeManager.SystemUsesLightTheme());

            var repository = _services.GetRequiredService<IClipRepository>();
            await repository.InitializeAsync(CancellationToken.None);

            var capture = _services.GetRequiredService<ClipCaptureService>();
            _monitor = _services.GetRequiredService<IClipboardMonitor>();
            _shutdown = new CancellationTokenSource();
            _monitor.ClipboardChanged += (_, snapshot) => _ = CaptureAsync(capture, snapshot);
            _monitor.Start();

            var window = _services.GetRequiredService<Tenere.UI.MainWindow>();
            window.Loaded += (_, _) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                WindowBackdrop.ApplyMica(handle, settings.Current.Appearance.UseMica);
            };
            _viewModel = (MainViewModel)window.DataContext;
            _ = _viewModel.LoadCommand.ExecuteAsync(null);
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Application startup failed");
            MessageBox.Show(
                $"Tenere could not start.{Environment.NewLine}{ex.Message}",
                "Tenere",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService>(_ => new SettingsService());

        services.AddLogging(builder =>
        {
            builder.AddProvider(new FileLoggerProvider());
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IClipRepository>(_ => SqliteClipRepository.CreateDefault());
        services.AddSingleton<ITransformPipeline, TransformPipeline>();
        services.AddSingleton<IPrivacyFilter, PrivacyFilter>();
        services.AddSingleton<IOcrService, WindowsOcrService>();
        services.AddSingleton<ClipCaptureService>();
        services.AddSingleton<Win32ClipboardMonitor>();
        services.AddSingleton<IClipboardMonitor>(sp => sp.GetRequiredService<Win32ClipboardMonitor>());
        services.AddSingleton<IClipboardWriter, Win32ClipboardWriter>();
        services.AddSingleton<IAppRuleService, DatabaseAppRuleService>();
        services.AddSingleton<Func<string?>>(_ => SourceAppResolver.Resolve);

        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<Func<SettingsWindow>>(sp => () => sp.GetRequiredService<SettingsWindow>());
        services.AddTransient<Tenere.UI.MainWindow>();
    }

    private async Task CaptureAsync(ClipCaptureService capture, ClipboardSnapshot snapshot)
    {
        try
        {
            await capture.CaptureAsync(snapshot, _shutdown!.Token);
            _ = _viewModel?.RefreshCommand.ExecuteAsync(null);
        }
        catch (OperationCanceledException)
        {
            // Shutdown in progress.
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Clipboard capture failed");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_monitor is not null)
        {
            _monitor.Stop();
            _monitor.Dispose();
        }
        _shutdown?.Cancel();
        _shutdown?.Dispose();

        try
        {
            _services?.GetService<ISettingsService>()?.Save();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Settings save failed");
        }

        (_services as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}