using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SkylandersGamePad;

internal sealed class TrayApp : ApplicationContext
{
    private readonly SynchronizationContext _ui;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _reconnectItem;
    private readonly BleController _ble;
    private readonly GamepadMapper _gamepad;

    private static readonly Icon IconDisconnected = MakeCircleIcon(Color.Gray);
    private static readonly Icon IconScanning     = MakeCircleIcon(Color.Gold);
    private static readonly Icon IconConnected    = MakeCircleIcon(Color.LimeGreen);

    public TrayApp()
    {
        _ui = SynchronizationContext.Current!;

        _gamepad = new GamepadMapper();

        _ble = new BleController();
        _ble.StateChanged  += OnStateChanged;
        _ble.PacketReceived += _gamepad.Update;

        _reconnectItem = new ToolStripMenuItem("Reconnect", null, (_, _) => _ble.Restart())
        {
            Enabled = false,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_reconnectItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, OnQuit);

        _trayIcon = new NotifyIcon
        {
            Icon    = IconDisconnected,
            Text    = "Skylanders GamePad — disconnected",
            ContextMenuStrip = menu,
            Visible = true,
        };

        _ble.Start();
    }

    private void OnStateChanged(BleState state, string? deviceName)
    {
        _ui.Post(_ =>
        {
            switch (state)
            {
                case BleState.Scanning:
                    _trayIcon.Icon = IconScanning;
                    _trayIcon.Text = "Skylanders GamePad — scanning…";
                    _reconnectItem.Enabled = false;
                    break;
                case BleState.Connected:
                    _trayIcon.Icon = IconConnected;
                    _trayIcon.Text = $"Skylanders GamePad — {deviceName ?? "connected"}";
                    _reconnectItem.Enabled = false;
                    break;
                default:
                    _trayIcon.Icon = IconDisconnected;
                    _trayIcon.Text = "Skylanders GamePad — disconnected";
                    _reconnectItem.Enabled = true;
                    break;
            }
        }, null);
    }

    private void OnQuit(object? sender, EventArgs e)
    {
        _ble.Dispose();
        _gamepad.Dispose();
        _trayIcon.Visible = false;
        Application.Exit();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static Icon MakeCircleIcon(Color color)
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);
        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }
}
