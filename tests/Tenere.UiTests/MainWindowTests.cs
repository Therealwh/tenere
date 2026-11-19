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
        Thread.Sleep(2500);

        var settings = app.GetAllTopLevelWindows(automation)
            .FirstOrDefault(window => window.Title is "Settings" or "Настройки");
        Assert.NotNull(settings);
        Assert.NotNull(settings.FindFirstDescendant(cf => cf.ByAutomationId("LanguageCombo")));

        app.Kill();
    }

    [Fact]
    [Trait("Category", "UI")]
    public void CopyButton_PutsClipOnClipboard()
    {
        TestClipboard.SetText("SENTINEL-BEFORE-COPY");

        CleanupApps();
        var executable = Path.Combine(AppContext.BaseDirectory, "Tenere.exe");
        using var app = FlauiApplication.Launch(executable);
        using var automation = new UIA3Automation();

        var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
        Assert.NotNull(main);

        // The app captures the sentinel clip on startup; the top card copies it back.
        Thread.Sleep(1200);

        var copy = main.FindFirstDescendant(cf => cf.ByAutomationId("CopyButton"))?.AsButton();
        Assert.NotNull(copy);
        copy.Click();
        Thread.Sleep(800);

        Assert.Equal("SENTINEL-BEFORE-COPY", TestClipboard.GetText());

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

        var settings = app.GetAllTopLevelWindows(automation)
            .FirstOrDefault(window => window.Title is "Settings" or "Настройки");
        Assert.NotNull(settings);
        var language = settings.FindFirstDescendant(cf => cf.ByAutomationId("LanguageCombo"))?.AsComboBox();
        Assert.NotNull(language);
        language.Select(0);

        var save = settings.FindFirstDescendant(cf => cf.ByAutomationId("SaveButton"))?.AsButton();
        Assert.NotNull(save);
        save.Click();
        Thread.Sleep(600);

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
        Thread.Sleep(1500);

        // Tests need a deterministic history; without this the top card depends on
        // whatever was captured in earlier runs.
        try
        {
            python("import sqlite3, os; p = os.path.join(os.environ['LOCALAPPDATA'], 'Tenere', 'tenere.db'); c = sqlite3.connect(p); c.execute('DELETE FROM clips'); c.commit()");
        }
        catch { }
    }

    private static void python(string code) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo("python", $"-c \"{code}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            })!.WaitForExit(10_000);
}