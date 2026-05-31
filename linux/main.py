#!/usr/bin/env python3
"""Skylanders Trap Team GamePad — Linux BLE driver."""

import argparse
import asyncio
import logging
import signal
import sys

from bleak import BleakClient, BleakError

import config
from ble import find_device, get_notify_char
from gamepad import VirtualGamepad

logger = logging.getLogger(__name__)


def _parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Skylanders Trap Team GamePad driver")
    p.add_argument("--debug", action="store_true", help="Log raw BLE packets and verbose output")
    return p.parse_args()


async def run(gamepad: VirtualGamepad, debug: bool) -> None:
    backoff = config.RECONNECT_BACKOFF_BASE

    while True:
        try:
            device = await find_device()
        except BleakError as exc:
            print(f"Bluetooth not ready — retrying in {backoff:.0f}s… ({exc})")
            await asyncio.sleep(backoff)
            backoff = min(backoff * 2, config.RECONNECT_BACKOFF_MAX)
            continue

        if device is None:
            print(f"No device found — retrying in {backoff:.0f}s…")
            await asyncio.sleep(backoff)
            backoff = min(backoff * 2, config.RECONNECT_BACKOFF_MAX)
            continue

        backoff = config.RECONNECT_BACKOFF_BASE
        print(f"Found {device.name} [{device.address}]")

        disconnected = asyncio.Event()

        def on_disconnect(_client: BleakClient) -> None:
            disconnected.set()

        try:
            async with BleakClient(device, disconnected_callback=on_disconnect) as client:
                char_uuid = await get_notify_char(client)
                if char_uuid is None:
                    print("  0x1531 service not found on device — skipping")
                    await asyncio.sleep(config.RECONNECT_BACKOFF_BASE)
                    continue

                print("Connected")

                def on_notify(_handle: int, data: bytearray) -> None:
                    if debug:
                        logger.debug("packet: %s", data.hex())
                    gamepad.update(bytes(data))

                await client.start_notify(char_uuid, on_notify)
                print("Streaming")

                await disconnected.wait()

        except (BleakError, EOFError, OSError) as exc:
            print(f"Disconnected — retrying in {backoff:.0f}s… ({exc})")
        else:
            print(f"Disconnected — retrying in {backoff:.0f}s…")

        await asyncio.sleep(backoff)
        backoff = min(backoff * 2, config.RECONNECT_BACKOFF_MAX)


async def main() -> None:
    args = _parse_args()
    logging.basicConfig(
        level=logging.DEBUG if args.debug else logging.WARNING,
        format="%(levelname)s %(name)s: %(message)s",
    )

    gamepad = VirtualGamepad()
    loop = asyncio.get_running_loop()

    shutdown = asyncio.Event()

    def _signal_handler() -> None:
        print("\nShutting down…")
        shutdown.set()

    for sig in (signal.SIGINT, signal.SIGTERM):
        loop.add_signal_handler(sig, _signal_handler)

    run_task = loop.create_task(run(gamepad, args.debug))
    await shutdown.wait()
    run_task.cancel()
    try:
        await run_task
    except asyncio.CancelledError:
        pass
    finally:
        gamepad.close()
        print("Done.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        sys.exit(0)
