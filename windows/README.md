# Skylanders Trap Team GamePad — Windows Driver

System tray app that connects the Skylanders Trap Team tablet GamePad over BLE and exposes it as a virtual Xbox 360 controller via ViGEmBus.

---

## Installation (recommended — no .NET required)

1. **Install ViGEmBus** — one-time, system-wide:  
   https://github.com/nefarius/ViGEmBus/releases  
   Download the latest `.exe` installer, run it, **reboot your PC**.

2. **Download `SkylandersGamePad.exe`** from the [Releases page](https://github.com/ZTF666/Skylanders-Trap-Team-GamePad-Drivers/releases/latest) and double-click it to run.

> **Note:** `SkylandersGamePad.exe` is not a setup wizard — it is the app itself. Double-clicking it starts it immediately. Look for the tray icon (bottom-right corner of your taskbar) after running it.

No .NET install needed — the exe is self-contained.

---

## Build from source (optional)

Requires: .NET 6+ SDK, Bluetooth 4.0+ adapter.

```powershell
cd windows
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
.\publish\SkylandersGamePad.exe
```

---

## Usage

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
- **Check your Bluetooth adapter supports BLE.** This is the most common gotcha — cheap or older dongles often only support Classic Bluetooth (2.x/3.x), which is a completely different protocol. The pad is invisible to those. You need a **Bluetooth 4.0 or higher** adapter. To check: Device Manager → Bluetooth → right-click your adapter → Properties. Any adapter listed as BT 4.0+ will work. A basic BT 4.0 USB dongle costs £3–5 and is plug-and-play on Windows.
- Confirm BT adapter is enabled in Device Manager.
- Make sure the pad is powered on and not already connected to another device.
- Try moving the pad closer to the adapter.

**Pad found but no inputs register in games**  
Some games enumerate gamepads at launch. Quit and relaunch the game after the tray icon turns green.

**Stick drift**  
Increase `Config.DeadZone` (e.g. `12`) and rebuild.

**Up/down reversed**  
Set `Config.InvertY = true` and rebuild.
