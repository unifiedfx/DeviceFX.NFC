#!/usr/bin/env python3
"""Tap and dump the Android accessibility tree via adb uiautomator."""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
import time
from pathlib import Path


def sdk_root() -> Path:
    return Path(
        os.environ.get("ANDROID_SDK_ROOT")
        or os.environ.get("ANDROID_HOME")
        or Path.home() / "Library/Android/sdk"
    )


def adb_bin() -> str:
    candidate = sdk_root() / "platform-tools" / "adb"
    if candidate.is_file():
        return str(candidate)
    return "adb"


def adb_cmd(serial: str | None) -> list[str]:
    cmd = [adb_bin()]
    if serial:
        cmd += ["-s", serial]
    return cmd


def dump_xml(serial: str | None) -> str:
    p = subprocess.run(
        adb_cmd(serial) + ["exec-out", "uiautomator", "dump", "/dev/tty"],
        capture_output=True,
    )
    return (p.stdout or b"").decode("utf-8", "replace") + (p.stderr or b"").decode(
        "utf-8", "replace"
    )


def texts(xml: str) -> list[str]:
    return re.findall(r'text="([^"]+)"', xml)


def nodes(xml: str) -> list[tuple[str, int, int, int, int, int, int]]:
    """(text, cx, cy, x1, y1, x2, y2)"""
    found = []
    for m in re.finditer(r"<node\s+([^>]+)/?>", xml):
        attrs = m.group(1)
        tm = re.search(r'text="([^"]*)"', attrs)
        bm = re.search(r'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"', attrs)
        if not bm:
            continue
        x1, y1, x2, y2 = map(int, bm.groups())
        text = tm.group(1) if tm else ""
        found.append((text, (x1 + x2) // 2, (y1 + y2) // 2, x1, y1, x2, y2))
    return found


def find(xml: str, needle: str) -> list[tuple[str, int, int, tuple[int, int, int, int]]]:
    matches = []
    needle_l = needle.lower()
    for text, cx, cy, x1, y1, x2, y2 in nodes(xml):
        if needle_l in text.lower():
            matches.append((text, cx, cy, (x1, y1, x2, y2)))
    return matches


def tap_text(serial: str | None, needle: str, which: int, retries: int, delay: float) -> bool:
    for i in range(retries):
        xml = dump_xml(serial)
        ms = find(xml, needle)
        if ms:
            name, x, y, b = ms[which]
            print(f"HIT '{name}' @{x},{y} {b}")
            subprocess.check_call(adb_cmd(serial) + ["shell", "input", "tap", str(x), str(y)])
            return True
        print(f"MISS '{needle}' try {i + 1}; texts={texts(xml)[:20]}")
        time.sleep(delay)
    return False


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "-s",
        "--serial",
        default=os.environ.get("ANDROID_SERIAL"),
        help="adb serial (or ANDROID_SERIAL). Required if multiple devices.",
    )
    sub = parser.add_subparsers(dest="cmd", required=True)

    sub.add_parser("texts", help="Print visible text nodes")
    dump_p = sub.add_parser("dump", help="Print bounds for sizable nodes")
    dump_p.add_argument("--min-width", type=int, default=200)
    dump_p.add_argument("--min-height", type=int, default=40)

    tap_p = sub.add_parser("tap", help="Tap a node whose text contains NEEDLE")
    tap_p.add_argument("needle")
    tap_p.add_argument("index", nargs="?", type=int, default=-1)
    tap_p.add_argument("--retries", type=int, default=6)
    tap_p.add_argument("--delay", type=float, default=0.6)

    xy_p = sub.add_parser("xy", help="Tap raw coordinates")
    xy_p.add_argument("x", type=int)
    xy_p.add_argument("y", type=int)

    args = parser.parse_args()

    if args.cmd == "texts":
        print(" | ".join(texts(dump_xml(args.serial))))
        return 0
    if args.cmd == "dump":
        xml = dump_xml(args.serial)
        for text, _cx, _cy, x1, y1, x2, y2 in nodes(xml):
            w, h = x2 - x1, y2 - y1
            if w >= args.min_width and h >= args.min_height:
                print(f"{w:4}x{h:4} [{x1},{y1}][{x2},{y2}] {text[:80]}")
        return 0
    if args.cmd == "tap":
        ok = tap_text(args.serial, args.needle, args.index, args.retries, args.delay)
        return 0 if ok else 2
    if args.cmd == "xy":
        print(f"TAP {args.x},{args.y}")
        subprocess.check_call(
            adb_cmd(args.serial) + ["shell", "input", "tap", str(args.x), str(args.y)]
        )
        return 0
    return 1


if __name__ == "__main__":
    sys.exit(main())
