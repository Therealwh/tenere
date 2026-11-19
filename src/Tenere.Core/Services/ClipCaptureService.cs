using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;
using Tenere.Shared.Settings;

namespace Tenere.Core.Services;

public sealed class ClipCaptureService
{
    private readonly IClipRepository _repository;
    private readonly IPrivacyFilter _privacy;
    private readonly IOcrService _ocr;
    private readonly ISettingsService _settings;

    public ClipCaptureService(
        IClipRepository repository,
        IPrivacyFilter privacy,
        IOcrService ocr,
        ISettingsService settings)
    {
        _repository = repository;
        _privacy = privacy;
        _ocr = ocr;
        _settings = settings;
    }

    public async Task CaptureAsync(ClipboardSnapshot snapshot, CancellationToken ct)
    {
        var settings = _settings.Current;
        if (settings.Privacy.DisableHistory) return;

        var decision = _privacy.Evaluate(snapshot);
        if (decision == PrivacyDecision.Block) return;

        var text = snapshot.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) && snapshot.ImagePng is null) return;

        var imagePath = await SaveImageAsync(snapshot.ImagePng, ct);
        var now = DateTimeOffset.UtcNow;
        var item = new ClipItem
        {
            ContentType = DetectType(text, snapshot),
            Title = BuildTitle(text, snapshot),
            PlainText = text,
            Html = snapshot.Html,
            ImagePath = imagePath,
            SourceApp = snapshot.SourceApp,
            CreatedAt = now,
            Sensitivity = decision == PrivacyDecision.Suspect ? "suspect" : "normal",
            ExpiresAt = now.AddDays(settings.History.RetentionDays),
            OcrStatus = OcrStatus.NotApplicable,
            ContentHash = ComputeHash(text, snapshot.ImagePng)
        };

        var id = await _repository.AddAsync(item, new Dictionary<string, byte[]>(), ct);
        item.Id = id;

        if (snapshot.ImagePng is not null && settings.Ocr.Enabled && settings.Ocr.IndexOcrText)
            _ = Task.Run(() => RunOcrAsync(id, snapshot.ImagePng, settings.Ocr.Languages.FirstOrDefault() ?? "en", ct), ct);
    }

    private async Task RunOcrAsync(long id, byte[] png, string language, CancellationToken ct)
    {
        try
        {
            var text = await _ocr.RecognizeAsync(png, language, ct);
            var item = await _repository.GetAsync(id, ct);
            if (item is null) return;
            item.OcrText = text;
            item.OcrStatus = string.IsNullOrWhiteSpace(text) ? OcrStatus.Failed : OcrStatus.Done;
            await _repository.UpdateAsync(item, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            var item = await _repository.GetAsync(id, CancellationToken.None);
            if (item is null) return;
            item.OcrStatus = OcrStatus.Failed;
            await _repository.UpdateAsync(item, CancellationToken.None);
        }
    }

    private static async Task<string?> SaveImageAsync(byte[]? png, CancellationToken ct)
    {
        if (png is null) return null;
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tenere", "images");
        Directory.CreateDirectory(directory);
        var name = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Convert.ToHexString(SHA256.HashData(png))[..12]}.png";
        var path = Path.Combine(directory, name);
        await File.WriteAllBytesAsync(path, png, ct);
        return path;
    }

    private static ClipContentType DetectType(string text, ClipboardSnapshot snapshot)
    {
        if (snapshot.ImagePng is not null) return ClipContentType.Image;
        if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return ClipContentType.Link;
        if (text.Contains('{') && text.Contains('}') && text.Contains(':')) return ClipContentType.Code;
        if (text.Contains('\t') && text.Contains('\n')) return ClipContentType.Table;
        if (Regex.IsMatch(text, @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")) return ClipContentType.Color;
        return ClipContentType.Text;
    }

    private static string BuildTitle(string text, ClipboardSnapshot snapshot)
    {
        if (snapshot.ImagePng is not null) return "Image";
        var firstLine = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return firstLine.Length > 80 ? firstLine[..80] + "…" : firstLine;
    }

    private static string ComputeHash(string text, byte[]? png)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        if (png is not null) bytes = [.. bytes, .. png];
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}