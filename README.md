# Skylanders Trap Team GamePad Driver

> Rescued from a drawer. Turned into a proper gamepad.

The Skylanders Trap Team tablet GamePad is a perfectly decent Bluetooth controller that became a paperweight the moment you lost the original dongle — Activision paired it to a proprietary USB receiver, so without it the pad was completely useless.

Turns out the pad is actually a standard Bluetooth Low Energy device. No proprietary protocol, no special hardware. It just needed a driver.

This project connects to the pad over BLE and exposes it to the OS as a virtual Xbox-style gamepad. Plug-and-play in any game, no dongle required.

---

## Platform support

| Platform | Folder | Notes |
|---|---|---|
| **Linux** — Raspberry Pi 5, Linux Mint, any BlueZ system | [`linux/`](linux/) | Python 3, runs as a systemd service |
| **Windows** 10 / 11 | [`windows/`](windows/) | C# .NET 6+, system tray app |

---

## Linux — quick start

Requires: Bluetooth 4.0+ adapter, Python 3.11+, `sudo` access.

```bash
git clone https://github.com/your-username/skylanders-controller-driver
cd skylanders-controller-driver
bash linux/install.sh
```

The script handles everything: packages, permissions, udev rules, uinput module, and a systemd service that starts automatically on login. Full details and troubleshooting in [`linux/README.md`](linux/README.md).

---

## Windows — quick start

Requires: Bluetooth 4.0+ adapter, [ViGEmBus driver](https://github.com/nefarius/ViGEmBus/releases), .NET 6+ SDK.

```powershell
git clone https://github.com/your-username/skylanders-controller-driver
cd skylanders-controller-driver\windows
dotnet run
```

A tray icon appears and turns green when the pad connects. Full details in [`windows/README.md`](windows/README.md).

---

## How it works

The pad advertises a Bluetooth LE service. This driver scans for that service UUID, connects, and subscribes to the notify characteristic. The pad then streams 20-byte input packets continuously. The driver parses those packets and forwards every button press, stick movement, and trigger pull to a virtual Xbox-style controller that the OS and any game can read normally.

Auto-reconnects on disconnect. Turn the pad off and back on — it comes back without restarting anything.

### Packet layout

| Byte | Mask | Input |
|---|---|---|
| `[8]` | `0x01` / `0x02` / `0x04` / `0x08` | D-pad Up / Down / Left / Right |
| `[8]` | `0x10` / `0x20` / `0x40` / `0x80` | A / B / X / Y |
| `[9]` | `0x10` / `0x20` | L1 / R1 |
| `[10]` | `0xFF` = held | L2 |
| `[11]` | `0xFF` = held | R2 |
| `[12]` | signed int8 | Right Stick X |
| `[13]` | signed int8 | Right Stick Y |
| `[14]` | signed int8 | Left Stick X |
| `[15]` | signed int8 | Left Stick Y |

Protocol originally reverse-engineered for macOS by [dasilvacontin](https://github.com/dasilvacontin/SkylandersGamePadEnabler).

---

## Repo structure

```
linux/          Python driver — bleak (BLE) + evdev (uinput virtual gamepad)
  install.sh    One-command installer
  main.py       Entry point, reconnect loop
  ble.py        BLE scanning and characteristic discovery
  gamepad.py    uinput virtual gamepad
  config.py     All tunables (dead-zone, Y-inversion, timeouts…)

windows/        C# driver — WinRT BLE + ViGEm virtual Xbox 360 controller
  Program.cs    Entry point
  TrayApp.cs    System tray UI
  BleController.cs  BLE scanning and connection
  GamepadMapper.cs  Packet parsing + ViGEm report
  Config.cs     All tunables
```
