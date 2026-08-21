# Universal Device Toolkit 宣传片

Cinematic stills + ffmpeg Ken Burns trailer for **Universal Device Toolkit**.

Deliverable (not committed; rebuild locally):

- `/opt/cursor/artifacts/udt-promo.mp4` (~32 s, 1920x1080 H.264)
- `/opt/cursor/artifacts/udt-promo-poster.png`

A 1080p poster frame is also stored here as `udt-promo-poster.png` for docs previews. The MP4 is typically ~30 MB at ~8 Mbps, so it stays out of git.

## Storyboard (~32 s, 8 beats)

Chinese titles are burned with `drawtext` (Noto Sans CJK). Stills are English-first so generated UI type stays legible; the product name **Universal Device Toolkit** is on the title and end cards.

| # | File | Length | Camera | On-screen punch | Subtitle |
|---|------|--------|--------|-----------------|----------|
| 1 | `stills/promo-01-title.png` | 4.5 s | Slow zoom in | 开源硬件工具套件 | 不用账号 · 不碰遥测 |
| 2 | `stills/promo-02-dashboard.png` | 4.5 s | Pan + zoom | 硬件掌控 | CPU / 电池 / GPU 实时仪表 |
| 3 | `stills/promo-03-power.png` | 4.5 s | Pan | 实时传感器 | 电源 · 电池养护 · 低功率适配器感知 |
| 4 | `stills/promo-04-optimize.png` | 4.5 s | Zoom in | 网络加速 | 系统优化 · 加速模式 |
| 5 | `stills/promo-05-automation.png` | 4.5 s | Pan | 自动化 | 触发器 · 流水线 · 自定义宏 |
| 6 | `stills/promo-06-plugins.png` | 4.5 s | Zoom in | 插件 | CustomMouse · ShellIntegration · ViveTool |
| 7 | `stills/promo-07-tray.png` | 4.5 s | Zoom in | 托盘后台 | 通知中心 · 简体中文 · English |
| 8 | `stills/promo-08-end.png` | 4.5 s | Slow zoom in | 掌控你的硬件 | 开源 · 无遥测 · 无账号 |

Crossfade 0.6 s between beats. Timeline:

`8 * 4.5 - 7 * 0.6 = 31.8 s`

Audio is a quiet synthesized drone (`aevalsrc`), not a third-party track.

## Rebuild

Dependencies: `ffmpeg`, Noto Sans CJK (`fonts-noto-cjk`).

```bash
sudo apt-get install -y ffmpeg fonts-noto-cjk
./Docs/promo/build-promo.sh
```

Optional output paths:

```bash
./Docs/promo/build-promo.sh /tmp/udt-promo.mp4 /tmp/udt-promo-poster.png
```

The script:

1. Ken Burns (slow zoom/pan) each still to 1920x1080 @ 30 fps
2. Crossfade clips
3. Lower-third Chinese titles
4. Quiet pad + fade in/out
5. H.264 `yuv420p`, ~10 Mbps, `+faststart`

## Visual notes

Stills follow the real dark UI: charcoal cards, rounded controls, blue/green/amber gauges, Fluent-like icons. Do not add Lenovo/Legion branding, fake benchmarks, or AI claims.

Generated stills are 1536x1024 (model output). The build crops/scales them to 16:9 1920x1080.
