# Universal Device Toolkit promo (real UI recording)

The deliverable is a screen recording of the **running Universal Device Toolkit window**, with visible mouse clicks through the real sidebar and pages. It is not Ken Burns motion on generated stills.

Outputs (not committed; rebuild locally):

- `/opt/cursor/artifacts/udt-promo.mp4` (about 43 s, 1920x1080 H.264 yuv420p, `+faststart`)
- `/opt/cursor/artifacts/udt-promo-poster.png` (one real frame)

`udt-promo-poster.png` in this folder is a copy of that real frame for docs previews. The MP4 stays out of git (`*.mp4` in `.gitignore`).

`stills/` may remain as design references. Do not rebuild the promo from those PNGs.

## What was captured

Source capture: 1920x1200 XFCE desktop, Electron window `universal-device-toolkit`, title `Universal Device Toolkit - ...`.

The Electron client is gone from `master`. Recording used a worktree of `origin/electron` (commit `2ed697e6d`) at `/tmp/udt-electron`. The Windows Host sidecar does not run on Linux; empty sensors, "this device does not support this feature", and the Linux network-acceleration banner are real UI states, not mocked success.

Click-through (see `record-clickthrough.py`):

| Screen | Shown |
|---|---|
| Dashboard (控制台) | CPU / battery / GPU gauges, power and graphics cards |
| Windows optimization (系统优化) | Category checklists |
| Network acceleration (网络与加速) | Honest Host-unavailable banner on Linux |
| Automation (自动化) | Empty pipeline list |
| Macro (自定义宏) | Numpad editor |
| Plugins (插件扩展) | Cursor and Pointer, Nilesoft Shell Manager, ViVeTool |
| Settings (设置) | Appearance (language, theme) |
| About (关于) | Version and project links |

Keyboard page and a custom tray popup were not part of this pass. The XFCE/Plank dock shows the running Electron icon.

## Rebuild from a recording

Dependencies: `ffmpeg`, Noto Sans CJK (`fonts-noto-cjk`).

```bash
sudo apt-get install -y ffmpeg fonts-noto-cjk
cp /path/to/capture.mp4 /opt/cursor/artifacts/udt-real-ui-source.mp4
./Docs/promo/build-promo.sh
```

Optional output paths:

```bash
./Docs/promo/build-promo.sh /tmp/udt-promo.mp4 /tmp/udt-promo-poster.png
```

The script:

1. Loads the raw capture (`UDT_PROMO_RAW`, `Docs/promo/recordings/udt-real-ui-demo.mp4`, or `/opt/cursor/artifacts/udt-real-ui-source.mp4`)
2. Trims the click-through (default start 10 s, duration 43 s)
3. Crops 1920x1200 to 1920x1080 (top of the frame, keeps the title bar)
4. Burns light Chinese lower-thirds (function names only)
5. Writes H.264 `yuv420p` `+faststart` and a poster frame

If you recapture, set `UDT_PROMO_START` / `UDT_PROMO_DURATION` and optionally `UDT_PROMO_LABELS` (CSV lines `start,end,label` in output time).

## Recapture the Electron UI

1. Worktree a revision that still has `UniversalDeviceToolkit.Electron` (for example `origin/electron`). Do not merge that tree into the promo branch unless you only need it to launch the app.
2. In that client: `npm install`, then start the renderer. Example:

   ```bash
   DISPLAY=:1 ELECTRON_DISABLE_SANDBOX=1 npx electron-vite dev -- --no-sandbox --disable-dev-shm-usage --ozone-platform=x11
   ```

3. Host/hardware may fail on Linux. That is expected. Record the real shell anyway.
4. Record the desktop (full 1920x1080 or native), then run `record-clickthrough.py` (or click the same nav items by hand) so the mouse is visible.
5. Point `build-promo.sh` at the new MP4.

Do not add Lenovo or Legion branding. Do not replace the MP4 with generated UI mockups.
