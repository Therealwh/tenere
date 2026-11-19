using Tenere.Core.Models;

namespace Tenere.Core.Abstractions;

public interface IClipboardMonitor : IDisposable
{
    event EventHandler<ClipboardSnapshot>? ClipboardChanged;
    void Start();
    void Stop();
}