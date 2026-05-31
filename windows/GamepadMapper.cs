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

        lsX = DeadZone(lsX); lsY = DeadZone(lsY);
        rsX = DeadZone(rsX); rsY = DeadZone(rsY);

        if (Config.InvertY) { lsY = -lsY; rsY = -rsY; }

        var c = _controller;

        // D-pad
        c.SetButtonState(Xbox360Button.Up,    (b8 & 0x01) != 0);
        c.SetButtonState(Xbox360Button.Down,  (b8 & 0x02) != 0);
        c.SetButtonState(Xbox360Button.Left,  (b8 & 0x04) != 0);
        c.SetButtonState(Xbox360Button.Right, (b8 & 0x08) != 0);

        // Face buttons
        c.SetButtonState(Xbox360Button.A, (b8 & 0x10) != 0);
        c.SetButtonState(Xbox360Button.B, (b8 & 0x20) != 0);
        c.SetButtonState(Xbox360Button.X, (b8 & 0x40) != 0);
        c.SetButtonState(Xbox360Button.Y, (b8 & 0x80) != 0);

        // Shoulder buttons
        c.SetButtonState(Xbox360Button.LeftShoulder,  (b9 & 0x10) != 0);
        c.SetButtonState(Xbox360Button.RightShoulder, (b9 & 0x20) != 0);

        // Triggers
        c.SetSliderValue(Xbox360Slider.LeftTrigger,  l2 ? (byte)255 : (byte)0);
        c.SetSliderValue(Xbox360Slider.RightTrigger, r2 ? (byte)255 : (byte)0);

        // Sticks
        c.SetAxisValue(Xbox360Axis.LeftThumbX,  Scale(lsX));
        c.SetAxisValue(Xbox360Axis.LeftThumbY,  Scale(lsY));
        c.SetAxisValue(Xbox360Axis.RightThumbX, Scale(rsX));
        c.SetAxisValue(Xbox360Axis.RightThumbY, Scale(rsY));

        c.SubmitReport();
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
