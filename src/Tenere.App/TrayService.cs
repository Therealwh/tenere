using System.Drawing;
using System.Windows.Forms;
using Tenere.Shared.Localization;

namespace Tenere.App;

/// <summary>
/// System tray icon with a compact, app-styled context menu.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Action _show;
    private readonly Action _exit;
    private bool _disposed;

    public TrayService(Icon icon, Action show, Action exit)
    {
        _show = show;
        _exit = exit;
        _icon = new NotifyIcon
        {
            Icon = icon,
            Text = Strings.AppName,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => _show();
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            _icon.ContextMenuStrip?.Dispose();
            _icon.ContextMenuStrip = BuildMenu();
        };
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(0x1C, 0x1F, 0x24),
            ForeColor = Color.FromArgb(0xF2, 0xF4, 0xF8),
            Font = new Font("Segoe UI Variable", 9.5f, FontStyle.Regular),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Padding(6, 6, 6, 6),
            RenderMode = ToolStripRenderMode.Professional
        };

        menu.Items.Add(CreateItem(Strings.TrayOpen, (_, _) => _show()));
        menu.Items.Add(new ToolStripSeparator
        {
            BackColor = Color.FromArgb(0x33, 0x37, 0x40),
            ForeColor = Color.FromArgb(0x33, 0x37, 0x40),
            Margin = new Padding(6, 4, 6, 4)
        });
        menu.Items.Add(CreateItem(Strings.TrayExit, (_, _) => _exit()));
        return menu;
    }

    private static ToolStripMenuItem CreateItem(string text, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            ForeColor = Color.FromArgb(0xF2, 0xF4, 0xF8),
            BackColor = Color.FromArgb(0x1C, 0x1F, 0x24),
            Padding = new Padding(10, 7, 10, 7),
            AutoSize = true
        };
        item.Click += onClick;
        item.MouseEnter += (_, _) => item.BackColor = Color.FromArgb(0x2E, 0x7B, 0xD6);
        item.MouseLeave += (_, _) => item.BackColor = Color.FromArgb(0x1C, 0x1F, 0x24);
        return item;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
    }
}