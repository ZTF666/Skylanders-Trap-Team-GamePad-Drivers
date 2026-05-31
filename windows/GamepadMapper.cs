using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace SkylandersGamePad;

internal sealed class GamepadMapper : IDisposable
{
    private readonly ViGEmClient _client;
    private readonly IXbox360Controller _controller;

    public GamepadMapper()
    {
        _client = new ViGEmClient();
        _controller = _client.CreateXbox360Controller();
        _controller.Connect();
    }

    public void Update(byte[] data)
    {
        if (data.Length < 16) return;

        byte b8 = data[8];
        byte b9 = data[9];

        bool l2 = data[10] == 0xFF;
        bool r2 = data[11] == 0xFF;

        int rsX = (sbyte)data[12];
        int rsY = (sbyte)data[13];
        int lsX = (sbyte)data[14];
        int lsY = (sbyte)data[15];

        // Apply dead-zone
        lsX = DeadZone(lsX); lsY = DeadZone(lsY);
        rsX = DeadZone(rsX); rsY = DeadZone(rsY);

        if (Config.InvertY) { lsY = -lsY; rsY = -rsY; }

        var report = new Xbox360Report();

        // Face buttons
        Xbox360Button buttons = Xbox360Button.None;
        if ((b8 & 0x01) != 0) buttons |= Xbox360Button.Up;
        if ((b8 & 0x02) != 0) buttons |= Xbox360Button.Down;
        if ((b8 & 0x04) != 0) buttons |= Xbox360Button.Left;
        if ((b8 & 0x08) != 0) buttons |= Xbox360Button.Right;
        if ((b8 & 0x10) != 0) buttons |= Xbox360Button.A;
        if ((b8 & 0x20) != 0) buttons |= Xbox360Button.B;
        if ((b8 & 0x40) != 0) buttons |= Xbox360Button.X;
        if ((b8 & 0x80) != 0) buttons |= Xbox360Button.Y;
        if ((b9 & 0x10) != 0) buttons |= Xbox360Button.LeftShoulder;
        if ((b9 & 0x20) != 0) buttons |= Xbox360Button.RightShoulder;
        report.Buttons = buttons;

        // Triggers (analog only — no digital button in Xbox 360 protocol)
        report.LeftTrigger  = l2 ? (byte)255 : (byte)0;
        report.RightTrigger = r2 ? (byte)255 : (byte)0;

        // Sticks: scale -128..127 → -32768..32767
        report.LeftThumbX  = Scale(lsX);
        report.LeftThumbY  = Scale(lsY);
        report.RightThumbX = Scale(rsX);
        report.RightThumbY = Scale(rsY);

        _controller.SendReport(report);
    }

    private static int DeadZone(int v) => Math.Abs(v) <= Config.DeadZone ? 0 : v;

    private static short Scale(int raw) =>
        (short)Math.Clamp((int)(raw / 128.0 * 32768.0), short.MinValue, short.MaxValue);

    public void Dispose()
    {
        _controller.Disconnect();
        _client.Dispose();
    }
}
