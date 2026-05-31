# Skylanders Trap Team GamePad — Windows Driver

System tray app that connects the Skylanders Trap Team tablet GamePad over BLE and exposes it as a virtual Xbox 360 controller via ViGEmBus.

---

## Prerequisites

1. **ViGEmBus driver** — install once, system-wide:
   https://github.com/nefarius/ViGEmBus/releases  
   Download the latest `.exe` installer, run it, reboot.

2. **.NET 6 SDK (or newer)**:
   https://dotnet.microsoft.com/download

3. **Bluetooth 4.0+ adapter** with BLE (built-in on most laptops made after ~2013).

---

## Build & run

```powershell
cd windows
dotnet build -c Release
dotnet run            # or run the built .exe directly
```

The app starts minimised to the system tray. The tray icon shows connection state:

| Icon | Meaning |
|---|---|
| Gray circle | Disconnected |
| Gold circle | Scanning… |
| Green circle | Connected and streaming |

Right-click the icon for **Reconnect** (when disconnected) or **Quit**.

---

## Configuration

Edit `Config.cs` before building:

| Setting | Default | Description |
|---|---|---|
| `DeadZone` | `8` | Stick dead-zone radius on −128..127 scale |
| `InvertY` | `false` | Flip Y axes on both sticks |
| `ScanTimeout` | `10s` | Per-phase scan timeout before falling back to name matching |
| `ReconnectBackoffBase/Max` | `1s / 5s` | Exponential backoff on disconnect |

---

## Verifying inputs

Open **joy.cpl** (`Win+R → joy.cpl`) or launch Steam in Big Picture mode. The pad should appear as an **Xbox 360 Controller**. Press buttons to verify all inputs register.

---

## Auto-start on login

1. Build a release binary: `dotnet publish -c Release -r win-x64 --self-contained`
2. Press `Win+R`, type `shell:startup`, press Enter.
3. Drop a shortcut to `SkylandersGamePad.exe` into that folder.

---

## Troubleshooting

**ViGEmBus not found / ViGEmClient throws on startup**  
Install the ViGEmBus driver (see Prerequisites) and reboot.

**Pad never found**  
- Confirm BT adapter is enabled in Device Manager.
- Make sure the pad is powered on and not already connected to another device.
- Try moving the pad closer to the adapter.
- The driver first filters by service UUID; if that misses, it scans by device name. Run a debug build and attach a debugger to see which phase matches.

**Pad found but no inputs register in games**  
Some games enumerate gamepads at launch. Quit and relaunch the game after the tray icon turns green.

**Stick drift**  
Increase `Config.DeadZone` (e.g. `12`) and rebuild.

**Up/down reversed**  
Set `Config.InvertY = true` and rebuild.
