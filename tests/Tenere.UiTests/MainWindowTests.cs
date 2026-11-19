using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlauiApplication = FlaUI.Core.Application;
using FlaUI.UIA3;

namespace Tenere.UiTests;

public class MainWindowTests
{
    [Fact]
    [Trait("Category", "UI")]
    public void MainWindow_Opens_With_Search_And_History()
    {
        CleanupApps();
        var executable = Path.Combine(AppContext.BaseDirectory, "Tenere.exe");
        Assert.True(File.Exists(executable), $"Missing app binary: {executable}");

        using var app = FlauiApplication.Launch(executable);
        using var automation = new UIA3Automation();

        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
        Assert.NotNull(window);
        Assert.Equal("Tenere", window.Title);

        var search = window.FindFirstDescendant(cf => cf.ByAutomationId("SearchBox"));
        Assert.NotNull(search);
        var searchBox = search.AsTextBox();
        Assert.True(searchBox.IsEnabled);
        searchBox.Text = "clipboard test";

        var list = window.FindFirstDescendant(cf => cf.ByAutomationId("ClipList"));
        Assert.NotNull(list);

        app.Kill();
    }

    [Fact]
    [Trait("Category", "UI")]
    public void SettingsButton_OpensSettingsWindow()
    {
        CleanupApps();
        var executable = Path.Combine(AppContext.BaseDirectory, "Tenere.exe");
        using var app = FlauiApplication.Launch(executable);
        using var automation = new UIA3Automation();

        var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
        Assert.NotNull(main);

        var settingsButton = (main.FindFirstDescendant(cf => cf.ByText("Settings"))
            ?? main.FindFirstDescendant(cf => cf.ByText("Настройки")))?.AsButton();
        Assert.NotNull(settingsButton);
        settingsButton.Click();
        Thread.Sleep(1200);

        var settings = app.GetAllTopLevelWindows(automation)
            .FirstOrDefault(window => window.Title is "Settings" or "Настройки");
        Assert.NotNull(settings);
        Assert.NotNull(settings.FindFirstDescendant(cf => cf.ByAutomationId("LanguageCombo")));

        app.Kill();
    }

    [Fact]
    [Trait("Category", "UI")]
    public void CopyButton_RunsCopyCommand()
    {
        CleanupApps();
        var executable = Path.Combine(AppContext.BaseDirectory, "Tenere.exe");
        using var app = FlauiApplication.Launch(executable);
        using var automation = new UIA3Automation();

        var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
        Assert.NotNull(main);

        var copy = main.FindFirstDescendant(cf => cf.ByAutomationId("CopyButton"))?.AsButton();
        Assert.NotNull(copy);
        copy.Click();
        Thread.Sleep(500);

        var status = main.FindFirstDescendant(cf => cf.ByText("Copied to clipboard."));
        Assert.NotNull(status);

        app.Kill();
    }

    [Fact]
    [Trait("Category", "UI")]
    public void LanguageSwitch_UpdatesMainWindowWithoutRestart()
    {
        CleanupApps();
        var executable = Path.Combine(AppContext.BaseDirectory, "Tenere.exe");
        using var app = FlauiApplication.Launch(executable);
        using var automation = new UIA3Automation();

        var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
        Assert.NotNull(main);

        var settingsButton = main.FindFirstDescendant(cf => cf.ByText("Settings"))
            ?? main.FindFirstDescendant(cf => cf.ByText("Настройки"));
        Assert.NotNull(settingsButton);
        settingsButton.AsButton().Click();
        Thread.Sleep(1000);

        var settings = app.GetAllTopLevelWindows(automation).First(window => window != main);
        var language = settings.FindFirstDescendant(cf => cf.ByAutomationId("LanguageCombo"))?.AsComboBox();
        Assert.NotNull(language);
        language.Select(0);

        var save = settings.FindFirstDescendant(cf => cf.ByAutomationId("SaveButton"))?.AsButton();
        Assert.NotNull(save);
        save.Click();
        Thread.Sleep(600);

        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tenere", "settings.json");
        Assert.Contains("\"Language\": \"en\"", File.ReadAllText(settingsPath));

        var history = main.FindFirstDescendant(cf => cf.ByText("History"));
        Assert.NotNull(history);

        app.Kill();
    }

    private static void CleanupApps()
    {
        foreach (var process in Process.GetProcessesByName("Tenere"))
        {
            try { process.Kill(); process.WaitForExit(3000); }
            catch (InvalidOperationException) { }
        }
        Thread.Sleep(500);
    }
}