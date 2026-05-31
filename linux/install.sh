#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_NAME="skylanders-gamepad.service"
SERVICE_DST="$HOME/.config/systemd/user/$SERVICE_NAME"
UDEV_RULE='/etc/udev/rules.d/99-uinput.rules'
MODULES_FILE='/etc/modules-load.d/uinput.conf'

echo "=== Skylanders Trap Team GamePad — Linux installer ==="
echo

# ── System packages ──────────────────────────────────────────────────────────
echo "[1/6] Installing system packages..."
sudo apt-get update -qq
sudo apt-get install -y python3-pip bluetooth bluez

# ── Python packages ───────────────────────────────────────────────────────────
echo "[2/6] Installing Python packages..."
pip3 install --break-system-packages --quiet bleak evdev

# ── input group ───────────────────────────────────────────────────────────────
echo "[3/6] Checking input group membership..."
if groups | grep -qw input; then
    echo "      Already in 'input' group — OK"
    NEED_RELOGIN=false
else
    sudo usermod -aG input "$USER"
    echo "      Added $USER to 'input' group."
    NEED_RELOGIN=true
fi

# ── udev rule for /dev/uinput ─────────────────────────────────────────────────
echo "[4/6] Installing udev rule for /dev/uinput..."
echo 'KERNEL=="uinput", GROUP="input", MODE="0660"' | sudo tee "$UDEV_RULE" > /dev/null
sudo udevadm control --reload-rules
sudo udevadm trigger --name-match=uinput
# Fix permissions for the current session without waiting for udev
if [ -e /dev/uinput ]; then
    sudo chgrp input /dev/uinput
    sudo chmod 660  /dev/uinput
fi

# ── uinput kernel module ──────────────────────────────────────────────────────
echo "[5/6] Loading uinput module..."
sudo modprobe uinput
if ! grep -qx uinput "$MODULES_FILE" 2>/dev/null; then
    echo uinput | sudo tee "$MODULES_FILE" > /dev/null
fi

# ── systemd user service ──────────────────────────────────────────────────────
echo "[6/6] Installing systemd user service..."
mkdir -p "$(dirname "$SERVICE_DST")"

# Write the service file with the exact path to this install
sed "s|ExecStart=.*|ExecStart=/usr/bin/python3 $SCRIPT_DIR/main.py|" \
    "$SCRIPT_DIR/$SERVICE_NAME" > "$SERVICE_DST"

systemctl --user daemon-reload
systemctl --user enable --now "$SERVICE_NAME"

# ── Done ──────────────────────────────────────────────────────────────────────
echo
echo "=== Installation complete ==="
echo
systemctl --user status "$SERVICE_NAME" --no-pager | head -5

if [ "$NEED_RELOGIN" = true ]; then
    echo
    echo "⚠  You were just added to the 'input' group."
    echo "   Log out and log back in, then run:"
    echo "     systemctl --user restart $SERVICE_NAME"
fi

echo
echo "Follow logs with:  journalctl --user -u $SERVICE_NAME -f"
echo "Check status with: systemctl --user status $SERVICE_NAME"
