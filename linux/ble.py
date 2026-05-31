import logging
from typing import Optional

from bleak import BleakClient, BleakScanner
from bleak.backends.device import BLEDevice

import config

logger = logging.getLogger(__name__)


async def find_device() -> Optional[BLEDevice]:
    """Scan for the pad by service UUID; fall back to name hints if UUID filter misses."""
    print("Scanning…")

    uuid = config.SERVICE_UUID.lower()

    # Primary: filter advertisements on the full 128-bit service UUID
    device = await BleakScanner.find_device_by_filter(
        lambda d, ad: uuid in [s.lower() for s in (ad.service_uuids or [])],
        timeout=config.SCAN_TIMEOUT,
    )
    if device:
        logger.debug("UUID filter matched: %s [%s]", device.name, device.address)
        return device

    # Fallback: name substring match, then verify 0x1531 service on connect
    logger.debug("UUID filter found nothing; scanning by name hints")
    devices = await BleakScanner.discover(timeout=config.SCAN_TIMEOUT)
    hints = config.DEVICE_NAME_HINTS
    for d in devices:
        name = (d.name or "").lower()
        if any(h in name for h in hints):
            logger.debug("Name fallback matched device: '%s' [%s]", d.name, d.address)
            print(f"  (name fallback matched '{d.name}' — update DEVICE_NAME_HINTS if wrong)")
            return d

    return None


async def get_notify_char(client: BleakClient) -> Optional[str]:
    """Return the UUID of the notify characteristic under the 0x1531 service."""
    targets = {config.SERVICE_UUID.lower(), config.GATT_SERVICE_UUID.lower()}
    for svc in client.services:
        svc_uuid = svc.uuid.lower()
        if svc_uuid in targets or "1531" in svc_uuid:
            for char in svc.characteristics:
                if "notify" in char.properties:
                    return char.uuid
    return None
