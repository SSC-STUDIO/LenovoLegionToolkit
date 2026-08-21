#!/usr/bin/env python3
"""Click through a running Universal Device Toolkit window for a promo capture.

Requires: DISPLAY with the Electron window visible, xdotool.
Coords assume a maximized ~1920-wide window on a 1920x1200 XFCE desktop
(window at 0,29). Sidebar click x is 140.
"""
from __future__ import annotations

import os
import subprocess
import time

os.environ.setdefault("DISPLAY", ":1")

NAV_X = 140
DASHBOARD = (NAV_X, 110)
KEYBOARD = (NAV_X, 170)
AUTOMATION = (NAV_X, 230)
MACRO = (NAV_X, 290)
OPTIMIZE = (NAV_X, 350)
PLUGINS = (NAV_X, 980)
SETTINGS = (NAV_X, 1060)
ABOUT = (NAV_X, 1110)

# System optimization segmented tabs (y of the pill row)
TAB_Y = 240
TAB_CLEANUP = (560, TAB_Y)
TAB_NETWORK = (820, TAB_Y)

# Appearance theme tiles (Settings → 全局界面颜色模式)
TILE_LIGHT = (780, 560)
TILE_DARK = (1120, 560)
TILE_SYSTEM = (1460, 560)


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


def mark(wid: str, label: str, t0: float) -> None:
    print(f"{time.time() - t0:6.2f}s  {label:16s}  {window_name(wid)}", flush=True)


def main() -> None:
    wid = window_id()
    sh("xdotool", "windowactivate", wid)
    time.sleep(0.25)
    t0 = time.time()
    mark(wid, "start", t0)

    click_at(*DASHBOARD)
    time.sleep(0.7)
    move_to(620, 310, 0.4)
    time.sleep(0.8)
    move_to(960, 310, 0.35)
    time.sleep(0.8)
    move_to(1320, 310, 0.35)
    time.sleep(0.8)
    move_to(900, 700, 0.35)
    time.sleep(0.9)

    click_at(*OPTIMIZE)
    time.sleep(1.0)
    mark(wid, "optimize", t0)
    move_to(900, 430, 0.3)
    time.sleep(0.8)
    click_at(*TAB_CLEANUP)
    time.sleep(1.6)
    click_at(*TAB_NETWORK)
    time.sleep(2.2)
    move_to(860, 480, 0.3)
    time.sleep(0.6)

    click_at(*AUTOMATION)
    time.sleep(1.3)
    mark(wid, "automation", t0)
    move_to(700, 300, 0.35)
    time.sleep(0.7)
    move_to(1180, 380, 0.35)
    time.sleep(0.8)

    click_at(*MACRO)
    time.sleep(1.0)
    mark(wid, "macro", t0)
    click_at(520, 450, 0.35)
    time.sleep(0.9)
    move_to(1180, 400, 0.35)
    time.sleep(1.0)

    click_at(*PLUGINS)
    time.sleep(1.3)
    mark(wid, "plugins", t0)
    move_to(760, 320, 0.35)
    time.sleep(0.6)
    move_to(760, 450, 0.3)
    time.sleep(0.6)
    move_to(760, 580, 0.3)
    time.sleep(0.8)

    click_at(*SETTINGS)
    time.sleep(1.6)
    mark(wid, "settings", t0)
    # Hold the three 全局界面颜色模式 tiles on camera (user's 6.0.0 proof).
    move_to(*TILE_LIGHT, 0.5)
    time.sleep(1.4)
    move_to(*TILE_DARK, 0.55)
    time.sleep(1.4)
    move_to(*TILE_SYSTEM, 0.55)
    time.sleep(2.4)
    move_to(900, 700, 0.4)
    time.sleep(1.0)

    click_at(*ABOUT)
    time.sleep(1.6)
    mark(wid, "about", t0)

    click_at(*DASHBOARD)
    time.sleep(1.1)
    mark(wid, "done", t0)
    print(f"elapsed {time.time() - t0:.2f}s", flush=True)


if __name__ == "__main__":
    main()
