using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace SkylandersGamePad;

public enum BleState { Disconnected, Scanning, Connected }

internal sealed class BleController : IDisposable
{
    public event Action<BleState, string?>? StateChanged;
    public event Action<byte[]>? PacketReceived;

    private CancellationTokenSource _cts = new();
    private Task _loop = Task.CompletedTask;

    public void Start()
    {
        _loop = RunLoopAsync(_cts.Token);
    }

    public void Restart()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        _loop = RunLoopAsync(_cts.Token);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var backoff = Config.ReconnectBackoffBase;

        while (!ct.IsCancellationRequested)
        {
            NotifyState(BleState.Scanning, null);

            ulong? address = null;
            try { address = await ScanAsync(ct); }
            catch (OperationCanceledException) { break; }

            if (address == null)
            {
                await Delay(backoff, ct);
                backoff = Cap(backoff * 2);
                continue;
            }

            backoff = Config.ReconnectBackoffBase;

            bool clean = false;
            try { clean = await ConnectAsync(address.Value, ct); }
            catch (OperationCanceledException) { break; }

            if (!clean)
            {
                await Delay(backoff, ct);
                backoff = Cap(backoff * 2);
            }
        }

        NotifyState(BleState.Disconnected, null);
    }

    // Two-phase scan: UUID filter first, fall back to name matching.
    private async Task<ulong?> ScanAsync(CancellationToken ct)
    {
        var result = await ScanPhaseAsync(byUuid: true, ct);
        return result ?? await ScanPhaseAsync(byUuid: false, ct);
    }

    private async Task<ulong?> ScanPhaseAsync(bool byUuid, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<ulong>();
        var watcher = new BluetoothLEAdvertisementWatcher();

        if (byUuid)
            watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(Config.AdvertisedServiceUuid);

        watcher.Received += (_, args) =>
        {
            if (!byUuid)
            {
                var name = args.Advertisement.LocalName ?? "";
                if (!Config.DeviceNameHints.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase)))
                    return;
            }
            tcs.TrySetResult(args.BluetoothAddress);
        };

        watcher.Start();
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(Config.ScanTimeout);
            await tcs.Task.WaitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            ct.ThrowIfCancellationRequested(); // re-throw only outer cancel
        }
        finally
        {
            watcher.Stop();
        }

        return tcs.Task.IsCompletedSuccessfully ? tcs.Task.Result : null;
    }

    private async Task<bool> ConnectAsync(ulong address, CancellationToken ct)
    {
        BluetoothLEDevice? device = null;
        try
        {
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
            if (device == null) return false;

            var notifyChar = await FindNotifyCharacteristicAsync(device);
            if (notifyChar == null) return false;

            var status = await notifyChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status != GattCommunicationStatus.Success) return false;

            var disconnected = new TaskCompletionSource();
            device.ConnectionStatusChanged += (d, _) =>
            {
                if (d.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                    disconnected.TrySetResult();
            };
            if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                disconnected.TrySetResult();

            notifyChar.ValueChanged += (_, args) =>
            {
                var reader = DataReader.FromBuffer(args.CharacteristicValue);
                var data = new byte[args.CharacteristicValue.Length];
                reader.ReadBytes(data);
                PacketReceived?.Invoke(data);
            };

            NotifyState(BleState.Connected, device.Name);
            await disconnected.Task.WaitAsync(ct);
            return false; // normal disconnect → trigger reconnect
        }
        finally
        {
            NotifyState(BleState.Disconnected, null);
            device?.Dispose();
        }
    }

    private static async Task<GattCharacteristic?> FindNotifyCharacteristicAsync(BluetoothLEDevice device)
    {
        // Try the two known service UUIDs
        foreach (var uuid in new[] { Config.GattServiceUuid, Config.AdvertisedServiceUuid })
        {
            var svcResult = await device.GetGattServicesForUuidAsync(uuid, BluetoothCacheMode.Uncached);
            if (svcResult.Status != GattCommunicationStatus.Success) continue;
            foreach (var svc in svcResult.Services)
            {
                var charResult = await svc.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                var c = charResult.Characteristics.FirstOrDefault(x =>
                    x.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify));
                if (c != null) return c;
            }
        }

        // Fallback: any service whose UUID contains "1531"
        var all = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (all.Status == GattCommunicationStatus.Success)
        {
            foreach (var svc in all.Services)
            {
                if (!svc.Uuid.ToString().Contains("1531", StringComparison.OrdinalIgnoreCase)) continue;
                var charResult = await svc.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                var c = charResult.Characteristics.FirstOrDefault(x =>
                    x.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify));
                if (c != null) return c;
            }
        }

        return null;
    }

    private void NotifyState(BleState state, string? name) =>
        StateChanged?.Invoke(state, name);

    private static TimeSpan Cap(TimeSpan t) =>
        t > Config.ReconnectBackoffMax ? Config.ReconnectBackoffMax : t;

    private static async Task Delay(TimeSpan t, CancellationToken ct)
    {
        try { await Task.Delay(t, ct); } catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
