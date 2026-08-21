#!/usr/bin/env bash
# Encode a real Universal Device Toolkit screen recording into the promo MP4.
#
# This is not a Ken Burns slideshow. Input must be pixels captured from a
# running app window (Electron renderer, browser, or similar).
#
# Usage:
#   ./Docs/promo/build-promo.sh [output.mp4] [poster.png]
#
# Input search order:
#   1. $UDT_PROMO_RAW
#   2. Docs/promo/recordings/udt-real-ui-demo.mp4  (gitignored)
#   3. /opt/cursor/artifacts/udt-real-ui-source.mp4
#   4. /opt/cursor/artifacts/udt-real-ui-demo-v3.mp4
#
# Optional env:
#   UDT_PROMO_START     trim start seconds (default 10)
#   UDT_PROMO_DURATION  trim length seconds (default 43)
#   UDT_PROMO_FONT_BOLD / UDT_PROMO_FONT_REG
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
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

RAW=""
for candidate in \
  "${UDT_PROMO_RAW:-}" \
  "${ROOT}/recordings/udt-real-ui-demo.mp4" \
  "/opt/cursor/artifacts/udt-real-ui-source.mp4" \
  "/opt/cursor/artifacts/udt-real-ui-demo-v3.mp4"
do
  if [[ -n "${candidate}" && -f "${candidate}" ]]; then
    RAW="${candidate}"
    break
  fi
done

if [[ -z "${RAW}" ]]; then
  cat >&2 <<'EOF'
No real screen recording found.

Capture the running Universal Device Toolkit window (mouse clicks, not stills),
then set UDT_PROMO_RAW to that MP4, or copy it to:

  Docs/promo/recordings/udt-real-ui-demo.mp4
  /opt/cursor/artifacts/udt-real-ui-source.mp4

See Docs/promo/README.md for the Electron worktree + click-through notes.
EOF
  exit 1
fi

# Defaults match the 1920x1200 XFCE capture of git tag v6.0.0 (f09e76640).
START="${UDT_PROMO_START:-2}"
DURATION="${UDT_PROMO_DURATION:-48}"
POSTER_SS="${UDT_PROMO_POSTER_SS:-36}"
CRF="${UDT_PROMO_CRF:-18}"
PRESET="${UDT_PROMO_PRESET:-medium}"

WORKDIR="$(mktemp -d /tmp/udt-promo.XXXXXX)"
cleanup() { rm -rf "${WORKDIR}"; }
trap cleanup EXIT

mkdir -p "$(dirname "${OUT_MP4}")" "$(dirname "${OUT_POSTER}")" "${WORKDIR}/txt"

# Output-relative lower thirds for the v6.0.0 click-through (source t=2..50).
# Settings appearance (three theme tiles) is held on camera for several seconds.
# Adjust UDT_PROMO_LABELS (start,end,label per line) if you recapture.
LABELS_FILE="${UDT_PROMO_LABELS:-}"
if [[ -z "${LABELS_FILE}" ]]; then
  LABELS_FILE="${WORKDIR}/labels.txt"
  python3 - "${LABELS_FILE}" <<'PY'
from pathlib import Path
import sys
Path(sys.argv[1]).write_text(
    "\n".join(
        [
            "0.3,9.6,控制台",
            "10.2,14.0,系统优化",
            "14.2,16.6,垃圾清理",
            "16.8,19.2,网络与加速",
            "20.0,23.6,自动化",
            "24.2,27.6,自定义宏",
            "28.2,33.6,插件扩展",
            "34.2,43.2,设置",
            "44.0,46.8,关于",
        ]
    )
    + "\n",
    encoding="utf-8",
)
PY
fi

VF_FILE="${WORKDIR}/vf.txt"
python3 - "${LABELS_FILE}" "${FONT_BOLD}" "${FONT_REG}" "${VF_FILE}" "${DURATION}" <<'PY'
from pathlib import Path
import sys

labels_path, font_bold, font_reg, vf_path, duration = sys.argv[1:6]
lines = [
    ln.strip()
    for ln in Path(labels_path).read_text(encoding="utf-8").splitlines()
    if ln.strip() and not ln.lstrip().startswith("#")
]
parts = [
    "crop=1920:1080:0:0",
    "format=yuv420p",
    "drawbox=x=0:y=ih-88:w=iw:h=88:color=black@0.42:t=fill",
    "drawbox=x=40:y=ih-62:w=5:h=32:color=0x1677FF@1:t=fill",
]

workdir = Path(vf_path).parent / "txt"
workdir.mkdir(parents=True, exist_ok=True)
for i, line in enumerate(lines):
    start, end, label = line.split(",", 2)
    text_file = workdir / f"label-{i:02d}.txt"
    text_file.write_text(label, encoding="utf-8")
    # Escape path for ffmpeg filter parser (colon, backslash, quotes).
    tf = str(text_file).replace("\\", "\\\\").replace(":", "\\:").replace("'", "\\'")
    fb = font_bold.replace("\\", "\\\\").replace(":", "\\:").replace("'", "\\'")
    parts.append(
        "drawtext="
        f"fontfile={fb}:font='Noto Sans CJK SC':"
        f"textfile={tf}:fontsize=32:fontcolor=white:"
        "shadowcolor=black@0.55:shadowx=1:shadowy=1:"
        f"x=58:y=h-58:enable='between(t,{start},{end})'"
    )

fade_out_start = max(0.0, float(duration) - 0.55)
parts.append("fade=t=in:st=0:d=0.35")
parts.append(f"fade=t=out:st={fade_out_start:.2f}:d=0.55")

Path(vf_path).write_text(",\n".join(parts) + "\n", encoding="utf-8")
PY

echo "==> Real UI encode"
echo "    raw:      ${RAW}"
echo "    trim:     start=${START}s duration=${DURATION}s"
echo "    output:   ${OUT_MP4}"

ffmpeg -y -hide_banner -loglevel error \
  -ss "${START}" -t "${DURATION}" -i "${RAW}" \
  -vf "$(tr '\n' ' ' < "${VF_FILE}")" \
  -an \
  -c:v libx264 -preset "${PRESET}" -profile:v high -pix_fmt yuv420p \
  -crf "${CRF}" \
  -movflags +faststart \
  "${OUT_MP4}"

echo "==> Poster (real frame, not a generated still)"
ffmpeg -y -hide_banner -loglevel error \
  -ss "${POSTER_SS}" -i "${OUT_MP4}" \
  -frames:v 1 -update 1 \
  "${OUT_POSTER}"

DOCS_POSTER="${ROOT}/udt-promo-poster.png"
if [[ "${OUT_POSTER}" != "${DOCS_POSTER}" ]]; then
  cp -f "${OUT_POSTER}" "${DOCS_POSTER}"
fi

echo
echo "Wrote ${OUT_MP4}"
ffprobe -v error -show_entries format=duration,size,bit_rate -of default=noprint_wrappers=1 "${OUT_MP4}"
echo "Wrote ${OUT_POSTER}"
ls -lh "${OUT_MP4}" "${OUT_POSTER}"
