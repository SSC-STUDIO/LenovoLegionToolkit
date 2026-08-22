# Universal Device Toolkit promo (real UI recording)

The deliverable is a screen recording of the **running Universal Device Toolkit window**, with visible mouse clicks through the real sidebar and pages. It is not Ken Burns motion on generated stills.

Outputs (not committed; rebuild locally):

- `/opt/cursor/artifacts/udt-promo.mp4` (48 s, 1920x1080 H.264 yuv420p, `+faststart`)
- `/opt/cursor/artifacts/udt-promo-poster.png` (one real frame: **控制台** light mica)

`udt-promo-poster.png` in this folder is a copy of that real frame for docs previews. The MP4 stays out of git (`*.mp4` in `.gitignore`).

`stills/` may remain as design references. Do not rebuild the promo from those PNGs.

## Which git revision was recorded

| Capture | Git ref | Notes |
|---|---|---|
| First Linux promo | `origin/electron` @ `2ed697e6d` (later deleted) | Theme preview chrome still used macOS-style traffic lights. |
| Second promo (looked “abnormal”) | **`v6.0.0`** tag → `f09e76640` | Correct Windows caption-button tiles, but Linux mica CSS punched chrome transparent over `#202020`. Mixed dark shell / white cards. |
| Third promo (still wrong) | **`v6.0.0`** + first Linux opaque-backdrop commit (`e69dde8ad`) | Forced `data-backdrop=none` and recorded **dark 跟随系统**. Opaque, but a dark Linux-looking app — not the Windows light mica console. |
| Current promo | **`v6.0.0`** + Linux mica-approx (`cursor/linux-opaque-backdrop-6fe9` @ `49e0451b8`) | **亮色**, 1600×900, UI scale **100%**. Linux keeps chrome opaque (no DWM) but paints the Win11-style sage sidebar (`#d5ded5` / RGB 213,222,213). Poster is the 控制台 frame. |

Do not invent a fake 6.0.0. Do not generate AI mockups of the Windows UI.

## What was visually wrong (previous captures)

- Transparent mica on Linux revealed Electron’s `#202020` window (white cards on a dark shell).
- Forcing `data-backdrop=none` and recording dark 跟随系统 produced a correct *opacity* but the wrong *theme*: charcoal chrome instead of the light-green Windows console.

The product fix now: keep Linux from punching `.udt-nav` / `.udt-titlebar` transparent, and when backdrop is mica/acrylic + light theme, fill chrome with an opaque approximation of Windows mica (existing light-mica 55%/78% whites composited over `#f6f6f6` + a Win11 bloom sample). Dark theme stays opaque `#202020`.

## What was captured (current)

Source capture: 1920×1080 desktop, Electron window **1600×900** at (160, 90), title `Universal Device Toolkit - 控制台`.

Worktree: `/tmp/udt-v6` at `v6.0.0` plus `cursor/linux-opaque-backdrop-6fe9`. About page shows **版本 6.0.0**. Appearance is **亮色** (not 跟随系统), UI scale **100%**.

Honest leftover differences vs a real Windows Legion machine:

- Title-bar model is **Linux Desktop** (Host stub `system.info`). Do not paint `Legion Y9000P IRX9`.
- Sensor gauges show 等待传感器数据 (no Windows hardware).
- No Vantage / Hotkeys toasts (Host cannot detect them on Linux).

Click-through (see `record-clickthrough.py`):

| Screen | Shown |
|---|---|
| Dashboard (控制台) | Light sage sidebar, orange **日志**, skeleton CPU / battery / GPU gauges |
| Windows optimization (系统优化) | Beautify checklists, then 垃圾清理 / 网络与加速 |
| Automation (自动化) | Empty pipeline list |
| Macro (自定义宏) | Numpad editor |
| Plugins (插件扩展) | Cursor and Pointer, Nilesoft Shell Manager, ViVeTool |
| Settings (设置) | Appearance tiles; **亮色** selected |
| About (关于) | **版本 6.0.0** and project links |

## Rebuild from a recording

Dependencies: `ffmpeg`, Noto Sans CJK (`fonts-noto-cjk`).

```bash
sudo apt-get install -y ffmpeg fonts-noto-cjk
cp /path/to/capture.mp4 /opt/cursor/artifacts/udt-real-ui-source.mp4
UDT_PROMO_CROP=1600:900:160:90 UDT_PROMO_POSTER_SS=2 ./Docs/promo/build-promo.sh
```

The script crops the 1600×900 window out of a 1920×1080 desktop, scales to 1920×1080, and writes H.264 `yuv420p` `+faststart` plus a poster frame (default: early 控制台). It does **not** burn lower-thirds unless `UDT_PROMO_LABELS` is set.

If you recapture, set `UDT_PROMO_START` / `UDT_PROMO_DURATION` / `UDT_PROMO_CROP` / `UDT_PROMO_POSTER_SS`.

## Recapture the Electron UI

1. Worktree tag **`v6.0.0`** plus `cursor/linux-opaque-backdrop-6fe9`. Do not merge Electron back onto the promo branch unless you only need it to launch the app.
2. Desktop: 1920×1080, UI scale 100%, window 1600×900. In-app theme **亮色**.
3. In that client: `npm install`, then:

   ```bash
   DISPLAY=:1 ELECTRON_DISABLE_SANDBOX=1 UDT_HOST_PATH=/tmp/udt-stub-host/UniversalDeviceToolkit.Host \
     npx electron-vite dev -- --no-sandbox --disable-dev-shm-usage --ozone-platform=x11
   ```

4. Host/hardware may fail on Linux. That is expected. Record the real shell anyway.
5. Run `record-clickthrough.py` so the mouse is visible. Start on 控制台 so the first impression is the light-green console.
6. Point `build-promo.sh` at the new MP4 (`UDT_PROMO_POSTER_SS=2` for the console frame).

Do not add Lenovo or Legion branding. Do not replace the MP4 with generated UI mockups.
