using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Tenere.UiTests;

public class MainWindowTests
{
    [Fact]
    [Trait("Category", "UI")]
    public void MainWindow_Opens_With_Search_And_History()
    {
        var executable = Path.Combine(AppContext.BaseDirectory, "Tenere.exe");
        Assert.True(File.Exists(executable), $"Missing app binary: {executable}");

        using var app = Application.Launch(executable);
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

        app.Close();
    }
}