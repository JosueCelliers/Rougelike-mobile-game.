#!/usr/bin/env bash
# Runs the Linux player build in scripted capture mode at phone resolution.
# Usage: Tools/unity/capture.sh <scenario> [width] [height] [outdir]
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SCEN="${1:-hole1}"; W="${2:-1080}"; H="${3:-2340}"
OUT="${4:-$ROOT/Captures/raw/$SCEN-${W}x${H}}"
mkdir -p "$OUT"
export DISPLAY="${DISPLAY:-:99}"
if ! xdpyinfo -display "$DISPLAY" >/dev/null 2>&1; then
  (Xvfb "$DISPLAY" -screen 0 1920x2560x24 -nolisten tcp >/dev/null 2>&1 &)
  for i in $(seq 1 50); do xdpyinfo -display "$DISPLAY" >/dev/null 2>&1 && break; sleep 0.1; done
fi
export ALSA_CONFIG_PATH_UNUSED=1
timeout 900 "$ROOT/Builds/Linux/BADLIE.x86_64" -force-glcore -screen-fullscreen 0 -screen-width "$W" -screen-height "$H" \
  -capture "$SCEN" -captureOut "$OUT" -logFile "$OUT/player.log"
echo "[capture.sh] exit=$? out=$OUT"
ls "$OUT"
