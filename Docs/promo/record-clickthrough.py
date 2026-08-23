#!/usr/bin/env python3
"""Click through a running Universal Device Toolkit window for a promo capture.

Requires: DISPLAY with the Electron window visible, xdotool.

Coords are derived from the window origin. Defaults match a 1600x900 window
at (160, 90) on a 1920x1080 desktop with UI scale 100% (v6.0.0).
"""
from __future__ import annotations

import os
import subprocess
import time

os.environ.setdefault("DISPLAY", ":1")


def sh(*args: str | int) -> subprocess.CompletedProcess[str]:
    return subprocess.run([str(a) for a in args], check=True, text=True, capture_output=True)


def window_id() -> str:
    ids = sh("xdotool", "search", "--name", "Universal Device Toolkit").stdout.strip().split()
    if not ids:
        raise SystemExit("UDT window not found")
    return ids[-1]


def window_origin(wid: str) -> tuple[int, int]:
    geo = sh("xdotool", "getwindowgeometry", wid).stdout
    # Position: 160,90
    for line in geo.splitlines():
        if "Position:" in line:
            pair = line.split("Position:", 1)[1].split("(screen", 1)[0].strip()
            x_s, y_s = pair.split(",")
            return int(x_s), int(y_s)
    return 0, 0


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


def mark(wid: str, label: str, t0: float) -> None:
    print(f"{time.time() - t0:6.2f}s  {label:16s}  {window_name(wid)}", flush=True)


def main() -> None:
    wid = window_id()
    sh("xdotool", "windowactivate", wid)
    time.sleep(0.25)
    ox, oy = window_origin(wid)
    print(f"window origin {ox},{oy}  {window_name(wid)}", flush=True)

    def s(x: int, y: int) -> tuple[int, int]:
        return ox + x, oy + y

    # CSS-viewport centers at 1600x900 / 100% scale (see CDP getBoundingClientRect).
    dashboard = s(147, 58)
    automation = s(147, 146)
    macro = s(147, 190)
    optimize = s(147, 234)
    plugins = s(147, 755)
    settings = s(147, 799)
    about = s(147, 843)

    tab_cleanup = s(484, 158)
    tab_network = s(687, 158)

    tile_light = s(646, 486)
    tile_dark = s(882, 486)
    tile_system = s(1118, 486)

    t0 = time.time()
    mark(wid, "start", t0)

    click_at(*dashboard)
    time.sleep(1.0)
    move_to(*s(520, 220), 0.45)
    time.sleep(0.9)
    move_to(*s(800, 220), 0.4)
    time.sleep(0.9)
    move_to(*s(1080, 220), 0.4)
    time.sleep(0.9)
    move_to(*s(800, 420), 0.4)
    time.sleep(1.0)

    click_at(*optimize)
    time.sleep(1.0)
    mark(wid, "optimize", t0)
    move_to(*s(700, 320), 0.3)
    time.sleep(0.8)
    click_at(*tab_cleanup)
    time.sleep(1.6)
    click_at(*tab_network)
    time.sleep(2.2)
    move_to(*s(720, 360), 0.3)
    time.sleep(0.6)

    click_at(*automation)
    time.sleep(1.2)
    mark(wid, "automation", t0)
    move_to(*s(560, 210), 0.35)
    time.sleep(0.6)
    move_to(*s(1000, 280), 0.35)
    time.sleep(0.7)

    click_at(*macro)
    time.sleep(1.0)
    mark(wid, "macro", t0)
    move_to(*s(420, 360), 0.35)
    time.sleep(0.8)
    move_to(*s(980, 300), 0.35)
    time.sleep(0.9)

    click_at(*plugins)
    time.sleep(1.2)
    mark(wid, "plugins", t0)
    move_to(*s(620, 250), 0.35)
    time.sleep(0.5)
    move_to(*s(620, 380), 0.3)
    time.sleep(0.5)
    move_to(*s(620, 510), 0.3)
    time.sleep(0.7)

    click_at(*settings)
    time.sleep(1.5)
    mark(wid, "settings", t0)
    # Hold 全局界面颜色模式 tiles; click 亮色 so the selection matches the UI.
    move_to(*tile_light, 0.5)
    time.sleep(0.4)
    click_at(*tile_light, 0.2)
    time.sleep(1.6)
    move_to(*tile_dark, 0.55)
    time.sleep(1.2)
    move_to(*tile_system, 0.55)
    time.sleep(1.4)
    move_to(*tile_light, 0.45)
    time.sleep(1.2)
    move_to(*s(720, 620), 0.4)
    time.sleep(0.8)

    click_at(*about)
    time.sleep(1.6)
    mark(wid, "about", t0)

    click_at(*dashboard)
    time.sleep(1.1)
    mark(wid, "done", t0)
    print(f"elapsed {time.time() - t0:.2f}s", flush=True)


if __name__ == "__main__":
    main()
