# Skylanders Trap Team GamePad — Linux BLE Driver

Exposes the Skylanders Trap Team tablet GamePad as a virtual Xbox-style gamepad via `uinput`. Works on Raspberry Pi 5 (ARM64) and Linux Mint (x86_64) with no code changes.

---

## Prerequisites

```bash
sudo apt update
sudo apt install -y python3-pip bluetooth bluez

pip3 install bleak evdev

# Allow your user to create uinput devices (log out/in afterwards)
sudo usermod -aG input $USER

# Load uinput now and on every boot
sudo modprobe uinput
echo uinput | sudo tee /etc/modules-load.d/uinput.conf
```

---

## Running

```bash
python3 main.py
```

With verbose packet logging:

```bash
python3 main.py --debug
```

The script scans for the pad, connects, and keeps running. It auto-reconnects on disconnect.

---

## Verifying inputs with evtest

```bash
sudo evtest
```

Select the **Skylanders Trap Team GamePad** device from the list. Press buttons and move sticks — you should see `EV_KEY` and `EV_ABS` events.

---

## Configuration

All tunables are in `config.py`:

| Variable | Default | Description |
|---|---|---|
| `DEAD_ZONE` | `8` | Stick dead-zone radius on −128..127 scale |
| `INVERT_Y` | `False` | Flip Y axes on both sticks if up/down feel reversed |
| `SCAN_TIMEOUT` | `10.0` | Seconds per scan attempt |
| `RECONNECT_BACKOFF_BASE` | `1.0` | Initial reconnect delay (doubles on each failure, capped at `RECONNECT_BACKOFF_MAX`) |
| `DEVICE_DISPLAY_NAME` | `"Skylanders Trap Team GamePad"` | Name shown in evtest / Steam |
| `VENDOR_ID` / `PRODUCT_ID` | `0x1234` / `0x5678` | Change to `0x045E` / `0x028E` to appear as Xbox 360 |

---

## Running as a systemd user service (Raspberry Pi 5 / headless)

Save as `~/.config/systemd/user/skylanders-gamepad.service`:

```ini
[Unit]
Description=Skylanders Trap Team GamePad BLE driver
After=bluetooth.target

[Service]
ExecStart=/usr/bin/python3 /home/%u/Documents/Workspace/skylanders-controller-driver/main.py
Restart=on-failure
RestartSec=5

[Install]
WantedBy=default.target
```

Enable and start:

```bash
systemctl --user daemon-reload
systemctl --user enable --now skylanders-gamepad.service
systemctl --user status skylanders-gamepad.service
```

---

## Troubleshooting

**"Permission denied" opening uinput**
Run `sudo usermod -aG input $USER`, log out, log back in. Verify with `groups | grep input`.

**Device never found**
- Confirm Bluetooth is on: `bluetoothctl show`
- Confirm BLE is supported: `hciconfig -a` — look for `LE` in features
- Try `--debug` to see scan results
- If UUID filtering misses the pad, the driver falls back to name matching. Check the debug log for the actual advertised name and add it to `DEVICE_NAME_HINTS` in `config.py`.

**Stick drift**
Increase `DEAD_ZONE` in `config.py` (e.g. `12`).

**Up/down reversed**
Set `INVERT_Y = True` in `config.py`.

**evtest shows the device but Steam doesn't see it**
Change `VENDOR_ID`/`PRODUCT_ID` to `0x045E`/`0x028E` in `config.py` to present as Xbox 360.
