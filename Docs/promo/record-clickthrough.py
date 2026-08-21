#!/usr/bin/env python3
"""Click through a running Universal Device Toolkit window for a promo capture.

Requires: DISPLAY with the Electron window visible, xdotool.
Coords assume a maximized ~1920-wide window (nav around 325px).
After scrolling the dashboard, call scroll_top() before optimization tabs
so the main pane is at the top (cleanup / network tab y=223).
"""
from __future__ import annotations

import os
import subprocess
import time

os.environ.setdefault("DISPLAY", ":1")

NAV_X = 160
DASHBOARD = (NAV_X, 102)
AUTOMATION = (NAV_X, 222)
MACRO = (NAV_X, 286)
OPTIMIZE = (NAV_X, 342)
PLUGINS = (NAV_X, 852)
SETTINGS = (NAV_X, 908)
ABOUT = (NAV_X, 972)
TAB_CLEANUP = (600, 223)
TAB_NETWORK = (868, 223)


def sh(*args: str | int) -> subprocess.CompletedProcess[str]:
    return subprocess.run([str(a) for a in args], check=True, text=True, capture_output=True)


def window_id() -> str:
    ids = sh("xdotool", "search", "--name", "Universal Device Toolkit").stdout.strip().split()
    if not ids:
        raise SystemExit("UDT window not found")
    return ids[-1]


def window_name(wid: str) -> str:
    return sh("xdotool", "getwindowname", wid).stdout.strip()


def mouse_pos() -> tuple[int, int]:
    out = sh("xdotool", "getmouselocation").stdout.strip()
    parts = dict(item.split(":", 1) for item in out.split() if ":" in item)
    return int(parts["x"]), int(parts["y"])


def move_to(x: int, y: int, duration: float = 0.45) -> None:
    x0, y0 = mouse_pos()
    steps = max(8, int(duration * 24))
    for i in range(1, steps + 1):
        t = i / steps
        ease = t * t * (3 - 2 * t)
        sh(
            "xdotool",
            "mousemove",
            int(round(x0 + (x - x0) * ease)),
            int(round(y0 + (y - y0) * ease)),
        )
        time.sleep(duration / steps)


def click_at(x: int, y: int, duration: float = 0.45) -> None:
    move_to(x, y, duration)
    time.sleep(0.08)
    sh("xdotool", "click", "1")
    time.sleep(0.18)


def wheel(n: int, delay: float = 0.11) -> None:
    button = "5" if n > 0 else "4"
    for _ in range(abs(n)):
        sh("xdotool", "click", button)
        time.sleep(delay)


def scroll_top() -> None:
    for _ in range(14):
        sh("xdotool", "click", "4")
        time.sleep(0.03)
    time.sleep(0.15)


def main() -> None:
    wid = window_id()
    sh("xdotool", "windowactivate", wid)
    time.sleep(0.2)
    print("start", window_name(wid), flush=True)

    click_at(*DASHBOARD)
    time.sleep(0.6)
    move_to(620, 290, 0.4)
    time.sleep(0.7)
    move_to(960, 290, 0.35)
    time.sleep(0.7)
    move_to(1320, 290, 0.35)
    time.sleep(0.7)
    move_to(900, 640, 0.35)
    wheel(5)
    time.sleep(0.9)
    wheel(-3)
    time.sleep(0.4)

    click_at(*OPTIMIZE)
    time.sleep(0.8)
    move_to(900, 400, 0.3)
    scroll_top()
    time.sleep(0.6)
    print("optimize", window_name(wid), flush=True)
    click_at(*TAB_CLEANUP)
    time.sleep(1.8)
    click_at(*TAB_NETWORK)
    time.sleep(2.4)
    move_to(780, 430, 0.3)
    time.sleep(0.5)

    click_at(*AUTOMATION)
    time.sleep(1.4)
    print("automation", window_name(wid), flush=True)
    move_to(700, 280, 0.35)
    time.sleep(0.7)
    move_to(1180, 360, 0.35)
    time.sleep(0.8)

    click_at(*MACRO)
    time.sleep(1.0)
    print("macro", window_name(wid), flush=True)
    click_at(520, 430, 0.35)
    time.sleep(0.9)
    move_to(1180, 380, 0.35)
    time.sleep(1.0)

    click_at(*PLUGINS)
    time.sleep(1.3)
    print("plugins", window_name(wid), flush=True)
    move_to(760, 300, 0.35)
    time.sleep(0.6)
    move_to(760, 430, 0.3)
    time.sleep(0.6)
    move_to(760, 560, 0.3)
    time.sleep(0.8)

    click_at(*SETTINGS)
    time.sleep(1.2)
    print("settings", window_name(wid), flush=True)
    move_to(900, 360, 0.35)
    time.sleep(0.8)

    click_at(*ABOUT)
    time.sleep(1.5)
    print("about", window_name(wid), flush=True)

    click_at(*DASHBOARD)
    time.sleep(1.0)
    print("done", window_name(wid), flush=True)


if __name__ == "__main__":
    main()
