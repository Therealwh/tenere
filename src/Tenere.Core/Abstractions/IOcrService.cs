namespace Tenere.Core.Abstractions;

public interface IOcrService
{
    Task<string?> RecognizeAsync(byte[] imagePng, string languageTag, CancellationToken ct);
    Task<IReadOnlyList<string>> GetAvailableLanguagesAsync(CancellationToken ct);
}