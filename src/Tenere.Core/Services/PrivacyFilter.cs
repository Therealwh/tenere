using System.Text.RegularExpressions;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;
using Tenere.Shared.Settings;

namespace Tenere.Core.Services;

public sealed partial class PrivacyFilter : IPrivacyFilter
{
    private readonly ISettingsService _settings;

    public PrivacyFilter(ISettingsService settings) => _settings = settings;

    public PrivacyDecision Evaluate(ClipboardSnapshot snapshot)
    {
        var privacy = _settings.Current.Privacy;
        if (!privacy.ShieldEnabled) return PrivacyDecision.Allow;

        var app = snapshot.SourceApp;
        if (!string.IsNullOrWhiteSpace(app) &&
            privacy.ExcludedApps.Any(excluded =>
                app.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
            return PrivacyDecision.Block;

        var text = snapshot.Text ?? string.Empty;
        foreach (var pattern in privacy.SecretPatterns)
        {
            try
            {
                if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50)))
                    return PrivacyDecision.Block;
            }
            catch (ArgumentException)
            {
                // Invalid user pattern: ignore it. Never throw from background capture.
            }
            catch (RegexMatchTimeoutException)
            {
                return PrivacyDecision.Suspect;
            }
        }

        return text.Length > 0 && LooksLikeSecret(text) ? PrivacyDecision.Suspect : PrivacyDecision.Allow;
    }

    private static bool LooksLikeSecret(string text)
    {
        var lowered = text.ToLowerInvariant();
        return (lowered.Contains("token") || lowered.Contains("api_key") || lowered.Contains("secret"))
               && text.Length < 512;
    }
}