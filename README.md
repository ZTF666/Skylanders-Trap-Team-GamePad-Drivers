# Skylanders Trap Team GamePad Driver

Connects the Skylanders Trap Team tablet GamePad over Bluetooth LE and exposes it to the OS as a standard Xbox-style virtual gamepad — no proprietary dongle required.

Pick your platform:

| Platform | Stack | Folder |
|---|---|---|
| **Linux** (Raspberry Pi 5, Linux Mint, any BlueZ system) | Python 3 + bleak + python-evdev (uinput) | [`linux/`](linux/) |
| **Windows** 10/11 | C# .NET 6+ + WinRT BLE + ViGEmBus | [`windows/`](windows/) |

---

## BLE Protocol

The pad advertises service UUID `00001531-0000-1000-8000-00805f9b34fb`. Its actual GATT service UUID is `533e1531-3abe-f33f-cd00-594e8b0a8ea3` with a single read+notify characteristic. Input packets are 20 bytes, streamed continuously.

| Byte | Mask | Input |
|---|---|---|
| `[8]` | `0x01` | D-pad Up |
| `[8]` | `0x02` | D-pad Down |
| `[8]` | `0x04` | D-pad Left |
| `[8]` | `0x08` | D-pad Right |
| `[8]` | `0x10` | A |
| `[8]` | `0x20` | B |
| `[8]` | `0x40` | X |
| `[8]` | `0x80` | Y |
| `[9]` | `0x10` | L1 |
| `[9]` | `0x20` | R1 |
| `[10]` | `0xFF` | L2 (0xFF = held) |
| `[11]` | `0xFF` | R2 (0xFF = held) |
| `[12]` | signed int8 | Right Stick X |
| `[13]` | signed int8 | Right Stick Y |
| `[14]` | signed int8 | Left Stick X |
| `[15]` | signed int8 | Left Stick Y |

---

## Quick start

See the README in your platform's folder for prerequisites, install steps, and troubleshooting.
