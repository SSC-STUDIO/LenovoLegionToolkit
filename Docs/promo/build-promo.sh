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
#   UDT_PROMO_START     trim start seconds (default 1)
#   UDT_PROMO_DURATION  trim length seconds (default 48)
#   UDT_PROMO_CROP      ffmpeg crop, e.g. 1600:900:160:90
#   UDT_PROMO_LABELS    CSV lines start,end,label (omit for no lower-thirds)
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

# Defaults match the 1920x1080 capture of a 1600x900 v6.0.0 window.
START="${UDT_PROMO_START:-1}"
DURATION="${UDT_PROMO_DURATION:-48}"
POSTER_SS="${UDT_PROMO_POSTER_SS:-2}"
CRF="${UDT_PROMO_CRF:-18}"
PRESET="${UDT_PROMO_PRESET:-medium}"
# Crop the 1600x900 app window out of a 1920x1080 desktop. Override or set
# empty to use the full frame (already 16:9). Poster default is the early
# 控制台 frame (light mica), not Settings.
CROP="${UDT_PROMO_CROP-1600:900:160:90}"

WORKDIR="$(mktemp -d /tmp/udt-promo.XXXXXX)"
cleanup() { rm -rf "${WORKDIR}"; }
trap cleanup EXIT

mkdir -p "$(dirname "${OUT_MP4}")" "$(dirname "${OUT_POSTER}")" "${WORKDIR}/txt"

LABELS_FILE="${UDT_PROMO_LABELS:-}"

VF_FILE="${WORKDIR}/vf.txt"
python3 - "${LABELS_FILE}" "${FONT_BOLD}" "${FONT_REG}" "${VF_FILE}" "${DURATION}" "${CROP}" <<'PY'
from pathlib import Path
import sys

labels_path, font_bold, font_reg, vf_path, duration, crop = sys.argv[1:7]
lines = []
if labels_path:
    lines = [
        ln.strip()
        for ln in Path(labels_path).read_text(encoding="utf-8").splitlines()
        if ln.strip() and not ln.lstrip().startswith("#")
    ]
parts = []
if crop:
    parts.append(f"crop={crop}")
parts.extend(["scale=1920:1080:flags=lanczos", "format=yuv420p"])

if lines:
    workdir = Path(vf_path).parent / "txt"
    workdir.mkdir(parents=True, exist_ok=True)
    parts.append("drawbox=x=0:y=ih-56:w=iw:h=56:color=black@0.28:t=fill")
    parts.append("drawbox=x=36:y=ih-40:w=4:h=22:color=0x1677FF@1:t=fill")
    for i, line in enumerate(lines):
        start, end, label = line.split(",", 2)
        text_file = workdir / f"label-{i:02d}.txt"
        text_file.write_text(label, encoding="utf-8")
        tf = str(text_file).replace("\\", "\\\\").replace(":", "\\:").replace("'", "\\'")
        fb = font_bold.replace("\\", "\\\\").replace(":", "\\:").replace("'", "\\'")
        parts.append(
            "drawtext="
            f"fontfile={fb}:font='Noto Sans CJK SC':"
            f"textfile={tf}:fontsize=22:fontcolor=white:"
            "shadowcolor=black@0.55:shadowx=1:shadowy=1:"
            f"x=50:y=h-38:enable='between(t,{start},{end})'"
        )

# Keep the first/last frames of the real UI (do not fade to crushed black).
Path(vf_path).write_text(",\n".join(parts) + "\n", encoding="utf-8")
PY

echo "==> Real UI encode"
echo "    raw:      ${RAW}"
echo "    trim:     start=${START}s duration=${DURATION}s"
echo "    crop:     ${CROP:-<full frame>}"
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
