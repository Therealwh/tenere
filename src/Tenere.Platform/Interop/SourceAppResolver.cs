using System.Diagnostics;

namespace Tenere.Platform.Interop;

public static class SourceAppResolver
{
    public static string? Resolve()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return null;

            using var process = Process.GetProcessById((int)pid);
            var name = process.ProcessName;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch
        {
            return null;
        }
    }
}