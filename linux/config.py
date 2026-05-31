# Advertised UUID (used for BleakScanner filtering — BlueZ exposes the short 0x1531 form)
SERVICE_UUID = "00001531-0000-1000-8000-00805f9b34fb"
# Actual GATT service UUID on the device (proprietary 128-bit)
GATT_SERVICE_UUID = "533e1531-3abe-f33f-cd00-594e8b0a8ea3"

# Case-insensitive substrings matched against device name when UUID filter misses
DEVICE_NAME_HINTS = ["skylanders", "trapteam", "trap team", "gamepad"]

SCAN_TIMEOUT = 10.0           # seconds per scan attempt
RECONNECT_BACKOFF_BASE = 1.0  # seconds; doubles on each consecutive failure
RECONNECT_BACKOFF_MAX = 5.0   # cap in seconds

DEAD_ZONE = 8     # ±units on raw -128..127 stick scale; zero-out values inside
INVERT_Y = False  # flip Y axes on both sticks; set True if up/down feel reversed

AXIS_MIN = -32768
AXIS_MAX = 32767
TRIGGER_MIN = 0
TRIGGER_MAX = 255

# These appear in evtest / Steam; change to 0x045E / 0x028E to look like Xbox 360
VENDOR_ID = 0x1234
PRODUCT_ID = 0x5678
VERSION = 0x0100
DEVICE_DISPLAY_NAME = "Skylanders Trap Team GamePad"
