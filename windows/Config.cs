namespace SkylandersGamePad;

internal static class Config
{
    // UUID advertised in BLE packets (used for watcher filter)
    public static readonly Guid AdvertisedServiceUuid = Guid.Parse("00001531-0000-1000-8000-00805f9b34fb");
    // Actual GATT service UUID on the device (proprietary 128-bit, confirmed on Linux)
    public static readonly Guid GattServiceUuid = Guid.Parse("533e1531-3abe-f33f-cd00-594e8b0a8ea3");

    // Case-insensitive substrings matched against LocalName when UUID filter misses
    public static readonly string[] DeviceNameHints = ["Skylanders", "GamePad", "TrapTeam"];

    public const int DeadZone = 8;      // ±units on raw -128..127 stick scale
    public static bool InvertY = false;  // flip Y axes on both sticks; set true if up/down feel reversed

    public static readonly TimeSpan ScanTimeout          = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan ReconnectBackoffBase = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan ReconnectBackoffMax  = TimeSpan.FromSeconds(5);
}
