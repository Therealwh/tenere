using System.Drawing;
using System.Drawing.Imaging;
using Tenere.Ocr;
using Xunit;

namespace Tenere.IntegrationTests;

public class WindowsOcrServiceTests
{
    [Fact]
    [Trait("Category", "OCR")]
    public async Task GetAvailableLanguagesAsync_ReturnsCollection()
    {
        var service = new WindowsOcrService();
        var languages = await service.GetAvailableLanguagesAsync(CancellationToken.None);

        Assert.NotNull(languages);
    }

    [Fact]
    [Trait("Category", "OCR")]
    public async Task RecognizeAsync_DegradesGracefullyOrReadsText()
    {
        using var bitmap = new Bitmap(640, 140);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            using var font = new Font("Segoe UI", 42, FontStyle.Bold);
            graphics.DrawString("TENERE OCR 1234", font, Brushes.Black, new PointF(12, 24));
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);

        var service = new WindowsOcrService();
        var languages = await service.GetAvailableLanguagesAsync(CancellationToken.None);
        var language = languages.FirstOrDefault() ?? "en";
        var text = await service.RecognizeAsync(stream.ToArray(), language, CancellationToken.None);

        if (languages.Count == 0)
        {
            // No OCR language pack installed on this machine. The app must degrade quietly.
            Assert.Null(text);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Contains("TENERE", text!, StringComparison.OrdinalIgnoreCase);
        }
    }
}