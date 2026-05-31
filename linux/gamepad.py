import config
from evdev import UInput, AbsInfo, ecodes as e


def _scale_stick(raw: int) -> int:
    """Map signed int8 (-128..127) to evdev axis range (-32768..32767)."""
    return max(config.AXIS_MIN, min(config.AXIS_MAX, int(raw / 128.0 * 32768.0)))


def _dead_zone(raw: int) -> int:
    return 0 if abs(raw) <= config.DEAD_ZONE else raw


class VirtualGamepad:
    def __init__(self):
        caps = {
            e.EV_KEY: [
                e.BTN_SOUTH,  # A
                e.BTN_EAST,   # B
                e.BTN_WEST,   # X
                e.BTN_NORTH,  # Y
                e.BTN_TL,     # L1
                e.BTN_TR,     # R1
                e.BTN_TL2,    # L2 digital
                e.BTN_TR2,    # R2 digital
            ],
            e.EV_ABS: [
                (e.ABS_X,     AbsInfo(0, config.AXIS_MIN, config.AXIS_MAX, 0, 0, 0)),
                (e.ABS_Y,     AbsInfo(0, config.AXIS_MIN, config.AXIS_MAX, 0, 0, 0)),
                (e.ABS_RX,    AbsInfo(0, config.AXIS_MIN, config.AXIS_MAX, 0, 0, 0)),
                (e.ABS_RY,    AbsInfo(0, config.AXIS_MIN, config.AXIS_MAX, 0, 0, 0)),
                (e.ABS_Z,     AbsInfo(0, config.TRIGGER_MIN, config.TRIGGER_MAX, 0, 0, 0)),
                (e.ABS_RZ,    AbsInfo(0, config.TRIGGER_MIN, config.TRIGGER_MAX, 0, 0, 0)),
                (e.ABS_HAT0X, AbsInfo(0, -1, 1, 0, 0, 0)),
                (e.ABS_HAT0Y, AbsInfo(0, -1, 1, 0, 0, 0)),
            ],
        }
        self._ui = UInput(
            caps,
            name=config.DEVICE_DISPLAY_NAME,
            vendor=config.VENDOR_ID,
            product=config.PRODUCT_ID,
            version=config.VERSION,
        )

    def update(self, data: bytes) -> None:
        if len(data) < 16:
            return

        b8 = data[8]
        b9 = data[9]

        # Triggers: 0xFF = fully pressed, else released
        l2 = 1 if data[10] == 0xFF else 0
        r2 = 1 if data[11] == 0xFF else 0

        # Sticks: signed int8
        rs_x = data[12] if data[12] < 128 else data[12] - 256
        rs_y = data[13] if data[13] < 128 else data[13] - 256
        ls_x = data[14] if data[14] < 128 else data[14] - 256
        ls_y = data[15] if data[15] < 128 else data[15] - 256

        ui = self._ui
        wr = ui.write

        # Face buttons
        wr(e.EV_KEY, e.BTN_SOUTH, 1 if b8 & 0x10 else 0)
        wr(e.EV_KEY, e.BTN_EAST,  1 if b8 & 0x20 else 0)
        wr(e.EV_KEY, e.BTN_WEST,  1 if b8 & 0x40 else 0)
        wr(e.EV_KEY, e.BTN_NORTH, 1 if b8 & 0x80 else 0)

        # Shoulder buttons
        wr(e.EV_KEY, e.BTN_TL,  1 if b9 & 0x10 else 0)
        wr(e.EV_KEY, e.BTN_TR,  1 if b9 & 0x20 else 0)

        # Triggers (digital + analog)
        wr(e.EV_KEY, e.BTN_TL2, l2)
        wr(e.EV_KEY, e.BTN_TR2, r2)
        wr(e.EV_ABS, e.ABS_Z,  255 if l2 else 0)
        wr(e.EV_ABS, e.ABS_RZ, 255 if r2 else 0)

        # D-pad hat (up/left = -1, down/right = +1, center = 0)
        hat_x = -1 if b8 & 0x04 else (1 if b8 & 0x08 else 0)
        hat_y = -1 if b8 & 0x01 else (1 if b8 & 0x02 else 0)
        wr(e.EV_ABS, e.ABS_HAT0X, hat_x)
        wr(e.EV_ABS, e.ABS_HAT0Y, hat_y)

        # Left stick
        ls_x = _dead_zone(ls_x)
        ls_y = _dead_zone(ls_y)
        if config.INVERT_Y:
            ls_y = -ls_y
        wr(e.EV_ABS, e.ABS_X, _scale_stick(ls_x))
        wr(e.EV_ABS, e.ABS_Y, _scale_stick(ls_y))

        # Right stick
        rs_x = _dead_zone(rs_x)
        rs_y = _dead_zone(rs_y)
        if config.INVERT_Y:
            rs_y = -rs_y
        wr(e.EV_ABS, e.ABS_RX, _scale_stick(rs_x))
        wr(e.EV_ABS, e.ABS_RY, _scale_stick(rs_y))

        ui.syn()

    def close(self) -> None:
        self._ui.close()
