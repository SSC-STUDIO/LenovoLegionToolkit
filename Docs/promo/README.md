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
| First Linux promo | `origin/electron` @ `2ed697e6d` (later deleted) | Theme preview chrome still used macOS-style traffic lights. |
| Second promo (looked “abnormal”) | **`v6.0.0`** tag → `f09e76640` | Correct Windows caption-button tiles, but the **display** was wrong: maximized on 1920x1200, Auto UI scale 136%, 跟随系统 followed WhiteSur-Light, and Linux mica CSS punched chrome transparent over an opaque `#202020` window (dark shell + white cards). The encode then cropped 1920x1200 → 1920x1080 and burned an 88 px lower-third that looked like a misplaced UI bar. |
| Current promo | **`v6.0.0`** + Linux opaque-backdrop fix (`cursor/linux-opaque-backdrop-6fe9`) | 1600x900 window, devicePixelRatio ~1, UI scale **100%**, dark 跟随系统 (OS `prefers-color-scheme: dark`), no mica transparency, native 16:9 encode (crop the window, scale to 1920x1080). No fade-to-black, no lower-thirds. |

Do not invent a fake 6.0.0. Do not generate AI mockups of the Windows UI.

## What was visually wrong (second capture)

Evidence in `/opt/cursor/artifacts/udt-promo.mp4` (before this recapture) and `/tmp/promo-frames/`:

- **f01 / f11**: first and last frames crushed to black (fade-in / fade-out).
- **f02–f04 dashboard**: light-gray sensor board on a `#202020` sidebar; empty 1920x1200 chrome; Auto scale 1.36.
- **f09 / poster**: Settings tiles were the real v6.0.0 mocks (Windows caption buttons, 跟随系统 blue border), but the page sat on the same mixed light-content / dark-shell theme, with the encode overlay covering 界面缩放.

Root cause in product CSS (v6.0.0 on Linux): `applyWindowBackdrop('Windows')` set `data-backdrop=mica` while Electron ignores DWM `backgroundMaterial` on Linux. `WindowBackdrop.css` then made `.udt-nav` / `.udt-titlebar` transparent over `backgroundColor: '#202020'`. 跟随系统 + a light XFCE theme = washed-out mixed UI.

The fix lives on `cursor/linux-opaque-backdrop-6fe9` (from tag `v6.0.0`): force `data-backdrop=none` on Linux and skip mica/acrylic transparency for `[data-platform=linux]`.

## What was captured (current)

Source capture: 1920x1080 desktop, Electron window **1600x900** at (160, 90), title `Universal Device Toolkit - ...`.

Worktree: `/tmp/udt-v6` at `v6.0.0` plus the Linux backdrop fix. About page shows **版本 6.0.0**. Settings → Appearance holds the three mock-window tiles:

- 亮色 / 暗色 / **跟随系统** (blue border)
- Windows caption-button mocks, diagonal split on 跟随系统
- 「调整全局界面颜色时修改系统颜色」 unchecked
- UI scale **100%**

The Windows Host sidecar does not run on this Linux VM; empty sensors, Host-unavailable network copy, and the title-bar model string **Linux Desktop** are real UI states.

Click-through (see `record-clickthrough.py`):

| Screen | Shown |
|---|---|
| Dashboard (控制台) | CPU / battery / GPU gauges waiting for sensor data |
| Windows optimization (系统优化) | Beautify checklists, then 垃圾清理 / 网络与加速 |
| Automation (自动化) | Empty pipeline list |
| Macro (自定义宏) | Numpad editor |
| Plugins (插件扩展) | Cursor and Pointer, Nilesoft Shell Manager, ViVeTool |
| Settings (设置) | Appearance theme tiles (held on camera) |
| About (关于) | **版本 6.0.0** and project links |

## Rebuild from a recording

Dependencies: `ffmpeg`, Noto Sans CJK (`fonts-noto-cjk`).

```bash
sudo apt-get install -y ffmpeg fonts-noto-cjk
cp /path/to/capture.mp4 /opt/cursor/artifacts/udt-real-ui-source.mp4
UDT_PROMO_CROP=1600:900:160:90 ./Docs/promo/build-promo.sh
```

The script crops the 1600x900 window out of a 1920x1080 desktop (no 16:10→16:9 chop), scales to 1920x1080, and writes H.264 `yuv420p` `+faststart` plus a poster frame (default: Settings appearance). It does **not** burn lower-thirds unless `UDT_PROMO_LABELS` is set.

If you recapture, set `UDT_PROMO_START` / `UDT_PROMO_DURATION` / `UDT_PROMO_CROP` / `UDT_PROMO_POSTER_SS`.

## Recapture the Electron UI

1. Worktree tag **`v6.0.0`** (or that tag plus the Linux opaque-backdrop fix). Do not merge Electron back onto the promo branch unless you only need it to launch the app.
2. Desktop: 1920x1080, dark `prefers-color-scheme`, UI scale 100%, window 1600x900.
3. In that client: `npm install`, then:

   ```bash
   DISPLAY=:1 ELECTRON_DISABLE_SANDBOX=1 UDT_HOST_PATH=/tmp/udt-stub-host/UniversalDeviceToolkit.Host \
     npx electron-vite dev -- --no-sandbox --disable-dev-shm-usage --ozone-platform=x11
   ```

4. Host/hardware may fail on Linux. That is expected. Record the real shell anyway.
5. Run `record-clickthrough.py` so the mouse is visible. **Leave Settings → Appearance on screen long enough that the three theme tiles are readable.**
6. Point `build-promo.sh` at the new MP4.

Do not add Lenovo or Legion branding. Do not replace the MP4 with generated UI mockups.
