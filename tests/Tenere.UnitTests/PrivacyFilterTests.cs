using Tenere.Core.Abstractions;
using Tenere.Core.Models;
using Tenere.Core.Services;
using Tenere.Shared.Settings;
using Xunit;

namespace Tenere.UnitTests;

public class PrivacyFilterTests
{
    private static SettingsService CreateSettings(Action<AppSettings>? configure = null)
    {
        var settings = new SettingsService(Path.Combine(Path.GetTempPath(), $"tenere-test-{Guid.NewGuid():N}.json"));
        configure?.Invoke(settings.Current);
        return settings;
    }

    private static ClipboardSnapshot Snapshot(string text, string? app = null) =>
        new(text, null, null, null, app);

    [Fact]
    public void PasswordManagerApp_IsBlocked()
    {
        var filter = new PrivacyFilter(CreateSettings());

        Assert.Equal(PrivacyDecision.Block, filter.Evaluate(Snapshot("some value", "KeePassXC")));
    }

    [Fact]
    public void ApiKeyPattern_IsBlocked()
    {
        var filter = new PrivacyFilter(CreateSettings());

        Assert.Equal(PrivacyDecision.Block, filter.Evaluate(Snapshot("AKIA1234567890ABCDEF")));
    }

    [Fact]
    public void PrivateKeyHeader_IsBlocked()
    {
        var filter = new PrivacyFilter(CreateSettings());

        Assert.Equal(PrivacyDecision.Block,
            filter.Evaluate(Snapshot("-----BEGIN PRIVATE KEY-----")));
    }

    [Fact]
    public void PlainText_IsAllowed()
    {
        var filter = new PrivacyFilter(CreateSettings());

        Assert.Equal(PrivacyDecision.Allow, filter.Evaluate(Snapshot("meeting notes")));
    }

    [Fact]
    public void DisabledShield_AllowsEverything()
    {
        var filter = new PrivacyFilter(CreateSettings(s => s.Privacy.ShieldEnabled = false));

        Assert.Equal(PrivacyDecision.Allow, filter.Evaluate(Snapshot("AKIA1234567890ABCDEF", "KeePassXC")));
    }
}