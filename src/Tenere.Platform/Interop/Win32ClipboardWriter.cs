using System.Runtime.InteropServices;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;

namespace Tenere.Platform.Interop;

/// <summary>
/// Writes a clip to the clipboard and sends Ctrl+V via SendInput.
/// All clipboard access runs on a dedicated STA thread and stays under 50 ms.
/// No exception escapes to the caller's background thread.
/// </summary>
public sealed class Win32ClipboardWriter : IClipboardWriter
{
    private const int ClipboardTimeoutMs = 25;

    private readonly Win32ClipboardMonitor? _monitor;
    private readonly object _gate = new();
    private string? _previousText;

    public Win32ClipboardWriter(Win32ClipboardMonitor? monitor = null)
    {
        _monitor = monitor;
    }

    public Task PasteAsync(ClipItem item, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || item is null) return Task.CompletedTask;

        try
        {
            _monitor?.SuppressNext();

            var text = item.PlainText ?? string.Empty;

            StaClipboardRunner.Invoke(() =>
            {
                _previousText = ClipboardData.ReadText(ClipboardTimeoutMs);
                return ClipboardData.WriteText(text, ClipboardTimeoutMs);
            });

            SendPaste();
        }
        catch { }

        return Task.CompletedTask;
    }

    public Task RestorePreviousAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return Task.CompletedTask;

        try
        {
            _monitor?.SuppressNext();

            string? previous;
            lock (_gate)
            {
                previous = _previousText;
                _previousText = null;
            }

            StaClipboardRunner.Invoke(() => previous is null
                ? ClipboardData.Clear(ClipboardTimeoutMs)
                : ClipboardData.WriteText(previous, ClipboardTimeoutMs));
        }
        catch { }

        return Task.CompletedTask;
    }

    private static void SendPaste()
    {
        try
        {
            var inputs = new NativeMethods.INPUT[4];

            inputs[0].type = 1;
            inputs[0].U.ki = KeyDown(NativeMethods.VK_CONTROL);
            inputs[1].type = 1;
            inputs[1].U.ki = KeyDown(NativeMethods.VK_V);
            inputs[2].type = 1;
            inputs[2].U.ki = KeyUp(NativeMethods.VK_V);
            inputs[3].type = 1;
            inputs[3].U.ki = KeyUp(NativeMethods.VK_CONTROL);

            NativeMethods.SendInput(
                (uint)inputs.Length,
                ref inputs[0],
                Marshal.SizeOf<NativeMethods.INPUT>());
        }
        catch { }
    }

    private static NativeMethods.KEYBDINPUT KeyDown(uint virtualKey) => new()
    {
        wVk = (ushort)virtualKey,
        dwFlags = 0
    };

    private static NativeMethods.KEYBDINPUT KeyUp(uint virtualKey) => new()
    {
        wVk = (ushort)virtualKey,
        dwFlags = NativeMethods.KEYEVENTF_KEYUP
    };
}