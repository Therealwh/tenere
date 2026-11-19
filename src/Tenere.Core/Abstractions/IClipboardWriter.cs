using Tenere.Core.Models;

namespace Tenere.Core.Abstractions;

public interface IClipboardWriter
{
    Task PasteAsync(ClipItem item, CancellationToken ct);
    Task CopyAsync(ClipItem item, CancellationToken ct);
    Task RestorePreviousAsync(CancellationToken ct);
}