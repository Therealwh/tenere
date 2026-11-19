using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Tenere.Platform.Interop;

/// <summary>
/// Global hotkey backed by a hidden message-only window on a dedicated STA thread.
/// Accepts strings such as "Ctrl+Shift+V".
/// </summary>
public sealed class GlobalHotkey : IDisposable
{
    private static readonly NativeMethods.WndProc WndProcDelegate = OnWndProc;
    private static readonly ConcurrentDictionary<IntPtr, GlobalHotkey> Instances = new();
    private static readonly object CreationGate = new();
    private static readonly Dictionary<string, uint> KeyMap = BuildKeyMap();
    private static GlobalHotkey? _creating;
    private static int _nextId;

    private readonly int _id = Interlocked.Increment(ref _nextId);
    private readonly uint _modifiers;
    private readonly uint _virtualKey;
    private readonly ManualResetEventSlim _started = new(false);

    private Thread? _thread;
    private IntPtr _hwnd;
    private volatile bool _registered;
    private bool _disposed;

    public event EventHandler? HotkeyPressed;

    public bool IsRegistered => _registered;

    public GlobalHotkey(string hotkey)
    {
        (_modifiers, _virtualKey) = Parse(hotkey);

        _thread = new Thread(ThreadProc)
        {
            IsBackground = true,
            Name = "Tenere.GlobalHotkey"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _started.Wait(3000);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var hwnd = _hwnd;
        if (hwnd != IntPtr.Zero)
            NativeMethods.PostMessage(hwnd, NativeMethods.WM_APP_STOP, IntPtr.Zero, IntPtr.Zero);
        else
            _registered = false;

        if (_thread is not null && _thread.IsAlive && _thread != Thread.CurrentThread)
            _thread.Join(2000);

        _started.Dispose();
    }

    private void ThreadProc()
    {
        var hwnd = IntPtr.Zero;
        var registered = false;
        try
        {
            var className = "TenereGlobalHotkey_" + Guid.NewGuid().ToString("N");
            var instance = NativeMethods.GetModuleHandle(null);

            var windowClass = new NativeMethods.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
                lpfnWndProc = WndProcDelegate,
                hInstance = instance,
                lpszClassName = className
            };

            lock (CreationGate)
            {
                if (NativeMethods.RegisterClassEx(ref windowClass) == 0) return;
                _creating = this;
                try
                {
                    hwnd = NativeMethods.CreateWindowEx(
                        0, className, string.Empty, 0,
                        0, 0, 0, 0,
                        NativeMethods.HWND_MESSAGE, IntPtr.Zero, instance, IntPtr.Zero);
                }
                finally { _creating = null; }
            }

            if (hwnd == IntPtr.Zero) return;

            Instances[hwnd] = this;
            _hwnd = hwnd;
            registered = _registered;

            while (NativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                NativeMethods.TranslateMessage(ref message);
                NativeMethods.DispatchMessage(ref message);
            }
        }
        catch { }
        finally
        {
            if (registered && hwnd != IntPtr.Zero)
                NativeMethods.UnregisterHotKey(hwnd, _id);

            _registered = false;
            if (hwnd != IntPtr.Zero)
            {
                Instances.TryRemove(hwnd, out _);
                NativeMethods.DestroyWindow(hwnd);
            }
            _hwnd = IntPtr.Zero;
            _started.Set();
        }
    }

    private void RaisePressed()
    {
        try { HotkeyPressed?.Invoke(this, EventArgs.Empty); }
        catch { }
    }

    private static IntPtr OnWndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (!Instances.TryGetValue(hWnd, out var self))
        {
            if (message == NativeMethods.WM_CREATE && _creating is not null)
            {
                self = _creating;
                Instances[hWnd] = self;
            }
            else
            {
                return NativeMethods.DefWindowProc(hWnd, message, wParam, lParam);
            }
        }

        switch (message)
        {
            case NativeMethods.WM_CREATE:
                self._registered = NativeMethods.RegisterHotKey(
                    hWnd, self._id, self._modifiers | NativeMethods.MOD_NOREPEAT, self._virtualKey);
                self._started.Set();
                return IntPtr.Zero;

            case NativeMethods.WM_HOTKEY:
                self.RaisePressed();
                return IntPtr.Zero;

            case NativeMethods.WM_APP_STOP:
                NativeMethods.DestroyWindow(hWnd);
                return IntPtr.Zero;

            case NativeMethods.WM_DESTROY:
                NativeMethods.UnregisterHotKey(hWnd, self._id);
                NativeMethods.PostQuitMessage(0);
                return IntPtr.Zero;

            default:
                return NativeMethods.DefWindowProc(hWnd, message, wParam, lParam);
        }
    }

    private static (uint Modifiers, uint VirtualKey) Parse(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
            throw new ArgumentException("Hotkey string is empty.", nameof(hotkey));

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            throw new ArgumentException("Hotkey must include a modifier, e.g. Ctrl+Shift+V.", nameof(hotkey));

        uint modifiers = 0;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            modifiers += parts[i].ToLowerInvariant() switch
            {
                "ctrl" or "control" or "ctl" => NativeMethods.MOD_CONTROL,
                "alt" => NativeMethods.MOD_ALT,
                "shift" => NativeMethods.MOD_SHIFT,
                "win" or "meta" or "super" => NativeMethods.MOD_WIN,
                _ => throw new ArgumentException($"Unknown modifier '{parts[i]}'.", nameof(hotkey))
            };
        }

        var key = parts[^1];
        uint virtualKey;
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            virtualKey = char.IsLetter(key[0])
                ? (uint)(char.ToUpperInvariant(key[0]) - 'A' + 0x41)
                : (uint)(key[0] - '0' + 0x30);
        }
        else if (KeyMap.TryGetValue(key.ToLowerInvariant(), out var mapped))
        {
            virtualKey = mapped;
        }
        else
        {
            throw new ArgumentException($"Unknown key '{key}'.", nameof(hotkey));
        }

        return (modifiers, virtualKey);
    }

    private static Dictionary<string, uint> BuildKeyMap()
    {
        var map = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            ["space"] = 0x20,
            ["enter"] = 0x0D,
            ["return"] = 0x0D,
            ["tab"] = 0x09,
            ["esc"] = 0x1B,
            ["escape"] = 0x1B,
            ["backspace"] = 0x08,
            ["insert"] = 0x2D,
            ["ins"] = 0x2D,
            ["delete"] = 0x2E,
            ["del"] = 0x2E,
            ["home"] = 0x24,
            ["end"] = 0x23,
            ["pgup"] = 0x21,
            ["pageup"] = 0x21,
            ["pgdn"] = 0x22,
            ["pagedown"] = 0x22,
            ["left"] = 0x25,
            ["up"] = 0x26,
            ["right"] = 0x27,
            ["down"] = 0x28,
            ["capslock"] = 0x14,
            ["numlock"] = 0x90,
            ["scrolllock"] = 0x91,
            ["add"] = 0x6B,
            ["subtract"] = 0x6D,
            ["multiply"] = 0x6A,
            ["divide"] = 0x6F,
            ["plus"] = 0xBB,
            ["minus"] = 0xBD
        };

        for (var i = 1; i <= 24; i++)
            map[$"f{i}"] = (uint)(0x70 + i - 1);

        return map;
    }
}