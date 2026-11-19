using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace Tenere.Platform.Interop;

/// <summary>
/// Clipboard read/write helpers. Call only from an STA thread.
/// Every method swallows errors and returns null/false on failure.
/// </summary>
internal static class ClipboardData
{
    private const ulong MaxDibBytes = 512UL * 1024 * 1024;
    private static readonly uint HtmlFormat = RegisterHtmlFormat();

    public static string? ReadText(int openTimeoutMs = 50)
    {
        try
        {
            if (!IsAvailable(NativeMethods.CF_UNICODETEXT)) return null;
            if (!TryOpen(openTimeoutMs)) return null;
            try
            {
                var handle = NativeMethods.GetClipboardData(NativeMethods.CF_UNICODETEXT);
                if (handle == IntPtr.Zero) return null;
                var ptr = NativeMethods.GlobalLock(handle);
                if (ptr == IntPtr.Zero) return null;
                try { return Marshal.PtrToStringUni(ptr); }
                finally { NativeMethods.GlobalUnlock(handle); }
            }
            finally { NativeMethods.CloseClipboard(); }
        }
        catch { return null; }
    }

    public static string? ReadHtml(int openTimeoutMs = 50)
    {
        try
        {
            if (HtmlFormat == 0 || !IsAvailable(HtmlFormat)) return null;
            if (!TryOpen(openTimeoutMs)) return null;
            try
            {
                var handle = NativeMethods.GetClipboardData(HtmlFormat);
                if (handle == IntPtr.Zero) return null;
                var ptr = NativeMethods.GlobalLock(handle);
                if (ptr == IntPtr.Zero) return null;
                string? raw;
                try { raw = Marshal.PtrToStringUni(ptr); }
                finally { NativeMethods.GlobalUnlock(handle); }
                return ExtractFragment(raw);
            }
            finally { NativeMethods.CloseClipboard(); }
        }
        catch { return null; }
    }

    public static IReadOnlyList<string>? ReadFiles(int openTimeoutMs = 50)
    {
        try
        {
            if (!IsAvailable(NativeMethods.CF_HDROP)) return null;
            if (!TryOpen(openTimeoutMs)) return null;
            try
            {
                var handle = NativeMethods.GetClipboardData(NativeMethods.CF_HDROP);
                if (handle == IntPtr.Zero) return null;
                var drop = NativeMethods.GlobalLock(handle);
                if (drop == IntPtr.Zero) return null;
                try
                {
                    var count = NativeMethods.DragQueryFile(drop, uint.MaxValue, null, 0);
                    if (count == 0 || count > 4096) return null;

                    var result = new List<string>((int)count);
                    var buffer = new StringBuilder(1024);
                    for (uint i = 0; i < count; i++)
                    {
                        buffer.Clear();
                        var needed = NativeMethods.DragQueryFile(drop, i, buffer, (uint)buffer.Capacity);
                        if (needed >= (uint)buffer.Capacity)
                        {
                            buffer.EnsureCapacity((int)needed + 1);
                            needed = NativeMethods.DragQueryFile(drop, i, buffer, (uint)buffer.Capacity);
                        }
                        if (needed > 0) result.Add(buffer.ToString());
                    }
                    return result.Count > 0 ? result : null;
                }
                finally { NativeMethods.GlobalUnlock(handle); }
            }
            finally { NativeMethods.CloseClipboard(); }
        }
        catch { return null; }
    }

    public static byte[]? ReadDibPng(int openTimeoutMs = 50)
    {
        try
        {
            if (!IsAvailable(NativeMethods.CF_DIB)) return null;
            if (!TryOpen(openTimeoutMs)) return null;
            try
            {
                var handle = NativeMethods.GetClipboardData(NativeMethods.CF_DIB);
                if (handle == IntPtr.Zero) return null;
                var size = NativeMethods.GlobalSize(handle).ToUInt64();
                if (size < 40 || size > MaxDibBytes || size > int.MaxValue) return null;
                var ptr = NativeMethods.GlobalLock(handle);
                if (ptr == IntPtr.Zero) return null;
                try
                {
                    var dib = new byte[(int)size];
                    Marshal.Copy(ptr, dib, 0, dib.Length);
                    return ConvertDibToPng(dib);
                }
                finally { NativeMethods.GlobalUnlock(handle); }
            }
            finally { NativeMethods.CloseClipboard(); }
        }
        catch { return null; }
    }

    public static bool WriteText(string text, int openTimeoutMs = 25)
    {
        var allocated = IntPtr.Zero;
        try
        {
            if (!TryOpen(openTimeoutMs)) return false;
            try
            {
                if (!NativeMethods.EmptyClipboard()) return false;

                var bytes = Encoding.Unicode.GetBytes(text + '\0');
                allocated = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)bytes.Length);
                if (allocated == IntPtr.Zero) return false;

                var ptr = NativeMethods.GlobalLock(allocated);
                if (ptr == IntPtr.Zero) return false;
                try { Marshal.Copy(bytes, 0, ptr, bytes.Length); }
                finally { NativeMethods.GlobalUnlock(allocated); }

                var result = NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, allocated);
                if (result == IntPtr.Zero) return false;

                allocated = IntPtr.Zero; // ownership moved to the system
                return true;
            }
            finally { NativeMethods.CloseClipboard(); }
        }
        catch { return false; }
        finally
        {
            if (allocated != IntPtr.Zero) NativeMethods.GlobalFree(allocated);
        }
    }

    public static bool Clear(int openTimeoutMs = 25)
    {
        try
        {
            if (!TryOpen(openTimeoutMs)) return false;
            try { return NativeMethods.EmptyClipboard(); }
            finally { NativeMethods.CloseClipboard(); }
        }
        catch { return false; }
    }

    private static bool IsAvailable(uint format)
    {
        try { return NativeMethods.IsClipboardFormatAvailable(format); }
        catch { return false; }
    }

    private static bool TryOpen(int timeoutMs)
    {
        var watch = Stopwatch.StartNew();
        do
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero)) return true;
            Thread.Sleep(5);
        }
        while (watch.ElapsedMilliseconds < timeoutMs);
        return false;
    }

    private static uint RegisterHtmlFormat()
    {
        try { return NativeMethods.RegisterClipboardFormat("HTML Format"); }
        catch { return 0; }
    }

    private static string? ExtractFragment(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        try
        {
            var headerEnd = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd < 0) headerEnd = raw.IndexOf("\n\n", StringComparison.Ordinal);
            if (headerEnd < 0) return raw.Trim();

            var header = raw[..headerEnd];
            var start = GetOffset(header, "StartFragment");
            var end = GetOffset(header, "EndFragment");
            if (start is null || end is null || end <= start || end > raw.Length) return raw.Trim();

            return raw[start.Value..end.Value];
        }
        catch { return raw.Trim(); }
    }

    private static int? GetOffset(string header, string name)
    {
        foreach (var line in header.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var index = line.IndexOf(':', StringComparison.Ordinal);
            if (index < 0) continue;
            if (!line[..index].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
            return int.TryParse(line[(index + 1)..].Trim(), out var value) ? value : null;
        }
        return null;
    }

    private static byte[]? ConvertDibToPng(byte[] dib)
    {
        if (dib.Length < 40) return null;

        var headerSize = BitConverter.ToInt32(dib, 0);
        if (headerSize < 40 || headerSize > dib.Length) return null;

        var bitCount = BitConverter.ToUInt16(dib, 14);
        var colorsUsed = headerSize >= 40 ? BitConverter.ToInt32(dib, 32) : 0;
        var paletteEntries = bitCount <= 8
            ? (colorsUsed > 0 ? colorsUsed : 1 << bitCount)
            : 0;
        var pixelOffset = 14 + headerSize + (paletteEntries * 4);
        if (pixelOffset > dib.Length) return null;

        var bmp = new byte[14 + dib.Length];
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        BitConverter.GetBytes(bmp.Length).CopyTo(bmp, 2);
        BitConverter.GetBytes((uint)pixelOffset).CopyTo(bmp, 10);
        Buffer.BlockCopy(dib, 0, bmp, 14, dib.Length);

        using var source = new MemoryStream(bmp, writable: false);
        using var image = Image.FromStream(source);
        using var output = new MemoryStream();
        image.Save(output, ImageFormat.Png);
        return output.ToArray();
    }
}