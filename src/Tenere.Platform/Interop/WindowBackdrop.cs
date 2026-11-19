namespace Tenere.Platform.Interop;

public static class WindowBackdrop
{
    private const int Windows11Build = 22000;

    public static bool ApplyMica(IntPtr hwnd, bool useMica)
    {
        if (hwnd == IntPtr.Zero || hwnd == NativeMethods.InvalidHandleValue) return false;
        if (!IsWindows11OrGreater()) return false;

        var value = useMica
            ? NativeMethods.DWMSBT_MAINWINDOW
            : NativeMethods.DWMSBT_NONE;

        try
        {
            var hr = NativeMethods.DwmSetWindowAttribute(
                hwnd,
                NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE,
                ref value,
                sizeof(int));
            return hr >= 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsWindows11OrGreater()
    {
        if (!OperatingSystem.IsWindows()) return false;
        return Environment.OSVersion.Version.Build >= Windows11Build;
    }
}