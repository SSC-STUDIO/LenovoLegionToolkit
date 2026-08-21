# Universal Device Toolkit promo (real UI recording)

The deliverable is a screen recording of the **running Universal Device Toolkit window**, with visible mouse clicks through the real sidebar and pages. It is not Ken Burns motion on generated stills.

Outputs (not committed; rebuild locally):

- `/opt/cursor/artifacts/udt-promo.mp4` (48 s, 1920x1080 H.264 yuv420p, `+faststart`)
- `/opt/cursor/artifacts/udt-promo-poster.png` (one real frame: Settings → Appearance theme tiles)

`udt-promo-poster.png` in this folder is a copy of that real frame for docs previews. The MP4 stays out of git (`*.mp4` in `.gitignore`).

`stills/` may remain as design references. Do not rebuild the promo from those PNGs.

## Which git revision was recorded

| Capture | Git ref | Notes |
|---|---|---|
| First Linux promo (the one that looked “wrong”) | `origin/electron` @ `2ed697e6d0f812a463c255a4768aa027067b5a2f` (`chore(electron): tighten package footprint budgets`, 2026-08-15) | Worktree `/tmp/udt-electron`. That remote branch was later **deleted**. Package version was already `6.0.0`, but the renderer was **62 commits behind** the release tag. Settings was only hovered for ~1 s, so the theme tiles were easy to miss. Theme preview chrome still used macOS-style traffic lights. |
| Current promo | **`v6.0.0`** tag → `f09e766405518a618a0b429b33b56457c17e4798` (`test(plugin): use LocalApplicationData for CustomMouse settings test snapshot`, 2026-08-21) | Same commit as `origin/master` at tag time. Worktree `/tmp/udt-v6`. About page shows **版本 6.0.0**. Settings → Appearance holds the three mock-window tiles on camera. |

`2ed697e6d` **is an ancestor of** `v6.0.0`. The theme-tile Appearance UI (`AppearanceSection.tsx` / `udt-theme-option*`) exists on **both** refs. The release tag is the Windows-like polish (Windows caption buttons in the mocks, diagonal light/dark split for 跟随系统, font picker, settings skeleton). It was pushed; it is not a dirty local-only tree.

Do not invent a fake 6.0.0. Do not generate AI mockups of the Windows UI.

## What was captured (v6.0.0)

Source capture: 1920x1200 XFCE desktop, Electron window `universal-device-toolkit`, title `Universal Device Toolkit - ...`.

The Electron client is gone from the current promo-branch checkout of `master` history that dropped it; recording used a **detached worktree of tag `v6.0.0`**, which still contains `UniversalDeviceToolkit.Electron`. The Windows Host sidecar does not run on this Linux VM; empty sensors, "this device does not support this feature", the Host-unavailable network banner, and the title-bar model string **Linux Desktop** (from `system.info`) are real UI states, not mocked success.

Click-through (see `record-clickthrough.py`):

| Screen | Shown |
|---|---|
| Dashboard (控制台) | CPU / battery / GPU gauges waiting for sensor data |
| Windows optimization (系统优化) | Beautify checklists, then 垃圾清理 |
| Network acceleration (网络与加速) | Honest Host-unavailable banner on Linux |
| Automation (自动化) | Empty pipeline list |
| Macro (自定义宏) | Numpad editor |
| Plugins (插件扩展) | Cursor and Pointer, Nilesoft Shell Manager, ViVeTool |
| Settings (设置) | **Appearance: 全局界面颜色模式 — 亮色 / 暗色 / 跟随系统 tiles** (跟随系统 selected with blue border; 「调整全局界面颜色时修改系统颜色」 unchecked) |
| About (关于) | **版本 6.0.0** and project links |

Keyboard page was not part of this pass. The XFCE/Plank dock shows the running Electron icon.

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
2. Trims the click-through (default start 2 s, duration 48 s)
3. Crops 1920x1200 to 1920x1080 (top of the frame, keeps the title bar)
4. Burns light Chinese lower-thirds (function names only)
5. Writes H.264 `yuv420p` `+faststart` and a poster frame (default: Settings appearance)

If you recapture, set `UDT_PROMO_START` / `UDT_PROMO_DURATION` and optionally `UDT_PROMO_LABELS` (CSV lines `start,end,label` in output time).

## Recapture the Electron UI

1. Worktree a revision that still has `UniversalDeviceToolkit.Electron` (prefer **`v6.0.0`**, not the deleted `origin/electron` tip). Do not merge that tree into the promo branch unless you only need it to launch the app.
2. In that client: `npm install`, then start the renderer. Example:

   ```bash
   DISPLAY=:1 ELECTRON_DISABLE_SANDBOX=1 UDT_HOST_PATH=/tmp/udt-stub-host/UniversalDeviceToolkit.Host \
     npx electron-vite dev -- --no-sandbox --disable-dev-shm-usage --ozone-platform=x11
   ```

3. Host/hardware may fail on Linux. That is expected. Record the real shell anyway.
4. Record the desktop (full 1920x1080 or native), then run `record-clickthrough.py` (or click the same nav items by hand) so the mouse is visible. **Leave Settings → Appearance on screen long enough that the three theme tiles are readable.**
5. Point `build-promo.sh` at the new MP4.

Do not add Lenovo or Legion branding. Do not replace the MP4 with generated UI mockups.
