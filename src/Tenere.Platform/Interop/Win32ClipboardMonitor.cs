using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;

namespace Tenere.Platform.Interop;

/// <summary>
/// Clipboard monitor using a hidden message-only window on a dedicated STA thread.
/// Applies a 150 ms debounce so a single copy raises a single event.
/// </summary>
public sealed class Win32ClipboardMonitor : IClipboardMonitor
{
    private const int DebounceMs = 150;
    private const int StartTimeoutMs = 3000;
    private const int StopTimeoutMs = 2000;
    private const int MaxSuppressedWrites = 16;

    private static readonly NativeMethods.WndProc WndProcDelegate = OnWndProc;
    private static readonly ConcurrentDictionary<IntPtr, Win32ClipboardMonitor> Instances = new();
    private static readonly object CreationGate = new();
    private static Win32ClipboardMonitor? _creating;

    private readonly object _gate = new();
    private readonly ManualResetEventSlim _started = new(false);

    private Thread? _thread;
    private Timer? _debounce;
    private IntPtr _hwnd;
    private int _suppressCount;
    private bool _running;
    private bool _disposed;

    public event EventHandler<ClipboardSnapshot>? ClipboardChanged;

    public bool IsRunning => _running;

    /// <summary>
    /// Called by the writer before it changes the clipboard. Each call skips one
    /// WM_CLIPBOARDUPDATE notification, so our own writes do not become clips.
    /// </summary>
    internal void SuppressNext(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            var current = Volatile.Read(ref _suppressCount);
            if (current >= MaxSuppressedWrites) break;
            Interlocked.Increment(ref _suppressCount);
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_disposed || _running) return;
            _running = true;
            _started.Reset();

            _thread = new Thread(ThreadProc)
            {
                IsBackground = true,
                Name = "Tenere.Clipboard.Monitor"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();

            if (!_started.Wait(StartTimeoutMs))
            {
                _running = false;
                var hwnd = _hwnd;
                if (hwnd != IntPtr.Zero)
                    NativeMethods.PostMessage(hwnd, NativeMethods.WM_APP_STOP, IntPtr.Zero, IntPtr.Zero);
                _thread.Join(StopTimeoutMs);
                _thread = null;
            }
        }
    }

    public void Stop()
    {
        Thread? thread;
        lock (_gate)
        {
            if (!_running) return;
            _running = false;
            thread = _thread;

            var hwnd = _hwnd;
            if (hwnd != IntPtr.Zero)
                NativeMethods.PostMessage(hwnd, NativeMethods.WM_APP_STOP, IntPtr.Zero, IntPtr.Zero);
        }

        CancelRead();
        if (thread is not null && thread.IsAlive && thread != Thread.CurrentThread)
            thread.Join(StopTimeoutMs);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _debounce?.Dispose();
        _debounce = null;
        _started.Dispose();
    }

    private void ThreadProc()
    {
        var hwnd = IntPtr.Zero;
        try
        {
            var className = "TenereClipboardMonitor_" + Guid.NewGuid().ToString("N");
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

            _hwnd = hwnd;
            Instances[hwnd] = this;
            _started.Set();

            while (NativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                NativeMethods.TranslateMessage(ref message);
                NativeMethods.DispatchMessage(ref message);
            }
        }
        catch { }
        finally
        {
            if (hwnd != IntPtr.Zero)
            {
                Instances.TryRemove(hwnd, out _);
                NativeMethods.RemoveClipboardFormatListener(hwnd);
                NativeMethods.DestroyWindow(hwnd);
            }
            _hwnd = IntPtr.Zero;
            _started.Set();
        }
    }

    private void CancelRead()
    {
        try { _debounce?.Change(Timeout.Infinite, Timeout.Infinite); }
        catch { }
    }

    private void ScheduleRead()
    {
        try
        {
            _debounce ??= new Timer(_ =>
            {
                var target = _hwnd;
                if (target != IntPtr.Zero)
                    NativeMethods.PostMessage(target, NativeMethods.WM_APP_READ_CLIPBOARD, IntPtr.Zero, IntPtr.Zero);
            });

            _debounce.Change(DebounceMs, Timeout.Infinite);
        }
        catch { }
    }

    private void ReadAndRaise()
    {
        try
        {
            var snapshot = new ClipboardSnapshot(
                ClipboardData.ReadText(),
                ClipboardData.ReadHtml(),
                ClipboardData.ReadDibPng(),
                ClipboardData.ReadFiles(),
                SourceAppResolver.Resolve());

            ClipboardChanged?.Invoke(this, snapshot);
        }
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
                NativeMethods.AddClipboardFormatListener(hWnd);
                return IntPtr.Zero;

            case NativeMethods.WM_CLIPBOARDUPDATE:
                if (Volatile.Read(ref self._suppressCount) > 0)
                {
                    Interlocked.Decrement(ref self._suppressCount);
                    self.CancelRead();
                    return IntPtr.Zero;
                }
                self.ScheduleRead();
                return IntPtr.Zero;

            case NativeMethods.WM_APP_READ_CLIPBOARD:
                self.ReadAndRaise();
                return IntPtr.Zero;

            case NativeMethods.WM_APP_STOP:
                NativeMethods.RemoveClipboardFormatListener(hWnd);
                NativeMethods.DestroyWindow(hWnd);
                return IntPtr.Zero;

            case NativeMethods.WM_DESTROY:
                NativeMethods.RemoveClipboardFormatListener(hWnd);
                NativeMethods.PostQuitMessage(0);
                return IntPtr.Zero;

            default:
                return NativeMethods.DefWindowProc(hWnd, message, wParam, lParam);
        }
    }
}