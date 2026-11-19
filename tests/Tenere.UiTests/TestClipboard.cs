using System.Runtime.InteropServices;
using System.Text;

namespace Tenere.UiTests;

/// <summary>Clipboard access for tests without pulling WinForms into the test project.</summary>
internal static class TestClipboard
{
    public static void SetText(string text)
    {
        if (!Open()) return;
        try
        {
            EmptyClipboard();
            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            var handle = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (handle == IntPtr.Zero) return;
            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero) return;
            try { Marshal.Copy(bytes, 0, pointer, bytes.Length); }
            finally { GlobalUnlock(handle); }
            SetClipboardData(CF_UNICODETEXT, handle);
        }
        finally { CloseClipboard(); }
    }

    public static string GetText()
    {
        if (!Open()) return string.Empty;
        try
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT)) return string.Empty;
            var handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero) return string.Empty;
            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero) return string.Empty;
            try { return Marshal.PtrToStringUni(pointer) ?? string.Empty; }
            finally { GlobalUnlock(handle); }
        }
        finally { CloseClipboard(); }
    }

    private static bool Open()
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(1000);
        while (DateTime.UtcNow < deadline)
        {
            if (OpenClipboard(IntPtr.Zero)) return true;
            Thread.Sleep(10);
        }
        return false;
    }

    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll")] private static extern bool IsClipboardFormatAvailable(uint format);
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(uint format, IntPtr data);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr handle);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr handle);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr handle);

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;
}