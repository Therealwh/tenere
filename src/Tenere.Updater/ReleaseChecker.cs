using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Tenere.Updater;

public sealed record ReleaseInfo(string Version, string Tag, string Notes, string DownloadUrl, string ChecksumUrl);

public sealed class ReleaseChecker
{
    private const string LatestUrl = "https://api.github.com/repos/Therealwh/tenere/releases/latest";
    private readonly HttpClient _http;

    public ReleaseChecker(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "Tenere-Updater");
        _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    public async Task<ReleaseInfo?> CheckAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync(LatestUrl, ct);
            if (!response.IsSuccessStatusCode) return null;
            var payload = await response.Content.ReadFromJsonAsync<GitHubRelease>(ct);
            if (payload?.Assets is null) return null;

            var installer = payload.Assets.FirstOrDefault(asset =>
                asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            var checksum = payload.Assets.FirstOrDefault(asset =>
                asset.Name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase));
            if (installer is null || checksum is null) return null;

            return new ReleaseInfo(
                payload.TagName?.TrimStart('v') ?? string.Empty,
                payload.TagName ?? string.Empty,
                payload.Body ?? string.Empty,
                installer.BrowserDownloadUrl,
                checksum.BrowserDownloadUrl);
        }
        catch (HttpRequestException) { return null; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
    }

    public async Task<bool> VerifyChecksumAsync(string filePath, string expected, CancellationToken ct)
    {
        var actual = await SHA256.HashDataAsync(File.OpenRead(filePath), ct);
        return Convert.ToHexString(actual).Equals(expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}