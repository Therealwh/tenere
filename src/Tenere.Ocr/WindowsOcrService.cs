using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Tenere.Core.Abstractions;

namespace Tenere.Ocr;

/// <summary>Local OCR using the Windows Media OCR API. No network calls.</summary>
public sealed class WindowsOcrService : IOcrService
{
    public async Task<string?> RecognizeAsync(byte[] imagePng, string languageTag, CancellationToken ct)
    {
        if (imagePng.Length == 0) return null;

        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imagePng.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask(ct);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied).AsTask(ct);

        var language = new Language(languageTag);
        var engine = OcrEngine.TryCreateFromLanguage(language);
        engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null) return null;

        var result = await engine.RecognizeAsync(bitmap).AsTask(ct);
        return result?.Text;
    }

    public Task<IReadOnlyList<string>> GetAvailableLanguagesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(OcrEngine.AvailableRecognizerLanguages
            .Select(language => language.LanguageTag)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray());
}