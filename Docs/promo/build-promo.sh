#!/usr/bin/env bash
# Rebuild the Universal Device Toolkit promo trailer from stills.
# Usage: build-promo.sh [output.mp4] [poster.png]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
STILLS="${ROOT}/stills"
OUT_MP4="${1:-/opt/cursor/artifacts/udt-promo.mp4}"
OUT_POSTER="${2:-/opt/cursor/artifacts/udt-promo-poster.png}"

FONT_BOLD="${UDT_PROMO_FONT_BOLD:-/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc}"
FONT_REG="${UDT_PROMO_FONT_REG:-/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc}"

if [[ ! -f "${FONT_BOLD}" ]]; then
  echo "Missing CJK bold font: ${FONT_BOLD}" >&2
  echo "Install with: sudo apt-get install -y fonts-noto-cjk" >&2
  exit 1
fi
if ! command -v ffmpeg >/dev/null 2>&1; then
  echo "ffmpeg not found. Install with: sudo apt-get install -y ffmpeg" >&2
  exit 1
fi
if ! command -v ffprobe >/dev/null 2>&1; then
  echo "ffprobe not found (usually part of ffmpeg)." >&2
  exit 1
fi

CLIP_DUR="4.5"
FADE_DUR="0.6"
FPS="30"
FRAMES="135"
BITRATE="10M"
MAXRATE="12M"
BUFSIZE="20M"

# 8 * 4.5 - 7 * 0.6
TOTAL_DUR="31.8"

WORKDIR="$(mktemp -d /tmp/udt-promo.XXXXXX)"
cleanup() { rm -rf "${WORKDIR}"; }
trap cleanup EXIT

mkdir -p "$(dirname "${OUT_MP4}")" "$(dirname "${OUT_POSTER}")" "${WORKDIR}/clips" "${WORKDIR}/txt"

# punch, sub, ken-burns mode, title style
# modes: zin, zout, panr, panl, panu, pand
SCENES=(
  "promo-01-title.png|开源硬件工具套件|不用账号 · 不碰遥测|zin|hero"
  "promo-02-dashboard.png|硬件掌控|CPU / 电池 / GPU 实时仪表|panr|lower"
  "promo-03-power.png|实时传感器|电源 · 电池养护 · 低功率适配器感知|panl|lower"
  "promo-04-optimize.png|网络加速|系统优化 · 加速模式|zin|lower"
  "promo-05-automation.png|自动化|触发器 · 流水线 · 自定义宏|pand|lower"
  "promo-06-plugins.png|插件|CustomMouse · ShellIntegration · ViveTool|panr|lower"
  "promo-07-tray.png|托盘后台|通知中心 · 简体中文 · English|zin|lower"
  "promo-08-end.png|掌控你的硬件|开源 · 无遥测 · 无账号|zout|hero"
)

ken_burns_filter() {
  local mode="$1"
  # Upscale 3:2 stills, then crop a moving 1920x1080 window.
  local base="scale=2304:1536:flags=lanczos,format=yuv420p"
  case "${mode}" in
    zin)
      echo "${base},crop=1920:1080:x='(iw-ow)/2':y='(ih-oh)*(0.15+0.70*t/${CLIP_DUR})'"
      ;;
    zout)
      echo "${base},crop=1920:1080:x='(iw-ow)/2':y='(ih-oh)*(0.85-0.70*t/${CLIP_DUR})'"
      ;;
    panr)
      echo "${base},crop=1920:1080:x='(iw-ow)*t/${CLIP_DUR}':y='(ih-oh)/2'"
      ;;
    panl)
      echo "${base},crop=1920:1080:x='(iw-ow)*(1-t/${CLIP_DUR})':y='(ih-oh)/2'"
      ;;
    panu)
      echo "${base},crop=1920:1080:x='(iw-ow)/2':y='(ih-oh)*(1-t/${CLIP_DUR})'"
      ;;
    pand)
      echo "${base},crop=1920:1080:x='(iw-ow)/2':y='(ih-oh)*t/${CLIP_DUR}'"
      ;;
    *)
      echo "${base},crop=1920:1080:x='(iw-ow)/2':y='(ih-oh)/2'"
      ;;
  esac
}

title_filter() {
  local style="$1"
  local punch_file="$2"
  local sub_file="$3"
  local common="fontfile=${FONT_BOLD}:textfile=${punch_file}:fontcolor=white:shadowcolor=black@0.65:shadowx=1:shadowy=2"
  local sub="fontfile=${FONT_REG}:textfile=${sub_file}:fontcolor=0xD0D0D4:shadowcolor=black@0.55:shadowx=1:shadowy=1"
  if [[ "${style}" == "hero" ]]; then
    echo "drawbox=x=0:y=ih-210:w=iw:h=210:color=black@0.62:t=fill,drawtext=${common}:fontsize=44:x=(w-text_w)/2:y=h-158,drawtext=${sub}:fontsize=26:x=(w-text_w)/2:y=h-92"
  else
    echo "drawbox=x=0:y=ih-200:w=iw:h=200:color=black@0.55:t=fill,drawbox=x=72:y=ih-168:w=64:h=4:color=0x4F9DF7@1:t=fill,drawtext=${common}:fontsize=52:x=72:y=h-148,drawtext=${sub}:fontsize=26:x=72:y=h-78"
  fi
}

echo "==> Building Ken Burns clips"
idx=0
for scene in "${SCENES[@]}"; do
  IFS='|' read -r file punch sub mode style <<<"${scene}"
  src="${STILLS}/${file}"
  if [[ ! -f "${src}" ]]; then
    echo "Missing still: ${src}" >&2
    exit 1
  fi
  punch_file="${WORKDIR}/txt/punch-${idx}.txt"
  sub_file="${WORKDIR}/txt/sub-${idx}.txt"
  # UTF-8, no BOM, no trailing newline (drawtext would show an extra glyph box).
  python3 - "${punch}" "${punch_file}" "${sub}" "${sub_file}" << 'PY'
import sys
from pathlib import Path
punch, punch_path, sub, sub_path = sys.argv[1:5]
Path(punch_path).write_text(punch, encoding="utf-8")
Path(sub_path).write_text(sub, encoding="utf-8")
PY

  vf="$(ken_burns_filter "${mode}"),$(title_filter "${style}" "${punch_file}" "${sub_file}"),fps=${FPS}"
  if [[ "${idx}" -eq 0 ]]; then
    vf="${vf},fade=t=in:st=0:d=0.8"
  fi
  if [[ "${idx}" -eq $((${#SCENES[@]} - 1)) ]]; then
    vf="${vf},fade=t=out:st=3.3:d=1.2"
  fi

  clip="${WORKDIR}/clips/clip-$(printf '%02d' "${idx}").mp4"
  echo "    clip ${idx}: ${file} (${mode}, ${style})"
  ffmpeg -y -hide_banner -loglevel error \
    -loop 1 -framerate "${FPS}" -i "${src}" \
    -t "${CLIP_DUR}" -frames:v "${FRAMES}" \
    -vf "${vf}" \
    -an -c:v libx264 -preset fast -crf 16 -pix_fmt yuv420p \
    "${clip}"
  idx=$((idx + 1))
done

N="${#SCENES[@]}"
echo "==> Crossfade + audio"

inputs=()
filter=""
for i in $(seq 0 $((N - 1))); do
  inputs+=(-i "${WORKDIR}/clips/clip-$(printf '%02d' "${i}").mp4")
done

# xfade offsets: n * (CLIP_DUR - FADE_DUR)
filter+="[0:v]setpts=PTS-STARTPTS[v0];"
for i in $(seq 1 $((N - 1))); do
  prev=$((i - 1))
  filter+="[${i}:v]setpts=PTS-STARTPTS[v${i}];"
done

filter+="[v0][v1]xfade=transition=fade:duration=${FADE_DUR}:offset=3.9[x1];"
filter+="[x1][v2]xfade=transition=fade:duration=${FADE_DUR}:offset=7.8[x2];"
filter+="[x2][v3]xfade=transition=fade:duration=${FADE_DUR}:offset=11.7[x3];"
filter+="[x3][v4]xfade=transition=fade:duration=${FADE_DUR}:offset=15.6[x4];"
filter+="[x4][v5]xfade=transition=fade:duration=${FADE_DUR}:offset=19.5[x5];"
filter+="[x5][v6]xfade=transition=fade:duration=${FADE_DUR}:offset=23.4[x6];"
filter+="[x6][v7]xfade=transition=fade:duration=${FADE_DUR}:offset=27.3[vout];"

# Quiet synthesized pad (no third-party music). Pink noise bed + low sine stack.
audio="aevalsrc=exprs='0.010*sin(2*PI*49*t)+0.007*sin(2*PI*73.5*t)+0.005*sin(2*PI*98*t)+0.004*sin(2*PI*123.5*t)*(0.5+0.5*sin(2*PI*0.11*t))+0.002*sin(2*PI*196*t)*sin(2*PI*0.09*t)':s=48000:d=${TOTAL_DUR}[a0];anoisesrc=color=pink:amplitude=0.012:sample_rate=48000:duration=${TOTAL_DUR}[a1];[a1]lowpass=f=280,highpass=f=40[a1f];[a0][a1f]amix=inputs=2:weights=1 0.45:normalize=0,afade=t=in:st=0:d=1.4,afade=t=out:st=29.4:d=2.4,aformat=sample_fmts=fltp:channel_layouts=stereo,volume=0.7[aout]"

filter+="${audio}"

ffmpeg -y -hide_banner -loglevel error \
  "${inputs[@]}" \
  -filter_complex "${filter}" \
  -map "[vout]" -map "[aout]" \
  -c:v libx264 -preset medium -profile:v high -pix_fmt yuv420p \
  -b:v "${BITRATE}" -maxrate "${MAXRATE}" -bufsize "${BUFSIZE}" \
  -c:a aac -b:a 160k \
  -movflags +faststart \
  -t "${TOTAL_DUR}" \
  "${OUT_MP4}"

echo "==> Poster"
ffmpeg -y -hide_banner -loglevel error \
  -ss 1.8 -i "${OUT_MP4}" \
  -frames:v 1 -update 1 \
  "${OUT_POSTER}"

echo
echo "Wrote ${OUT_MP4}"
ffprobe -v error -show_entries format=duration,size,bit_rate -of default=noprint_wrappers=1 "${OUT_MP4}"
echo "Wrote ${OUT_POSTER}"
ls -lh "${OUT_MP4}" "${OUT_POSTER}"
