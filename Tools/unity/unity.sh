#!/usr/bin/env bash
# Runs the Unity editor in batch mode against this project.
# Usage: Tools/unity/unity.sh <logname> [extra Unity args...]
# Logs go to Logs/<logname>.log. Exit code is Unity's.
# A virtual X display (Xvfb :99) is started when needed so rendering works headless.
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
UNITY="${UNITY_EDITOR:-/opt/unity/6000.3.25f1/Editor/Unity}"
NAME="$1"; shift
mkdir -p "$ROOT/Logs"
LOG="$ROOT/Logs/$NAME.log"
if [ -z "${DISPLAY:-}" ]; then
  export DISPLAY=:99
  if ! xdpyinfo -display :99 >/dev/null 2>&1; then
    (Xvfb :99 -screen 0 1920x2560x24 -nolisten tcp >/dev/null 2>&1 &)
    for i in $(seq 1 50); do xdpyinfo -display :99 >/dev/null 2>&1 && break; sleep 0.1; done
  fi
fi
"$UNITY" -batchmode -projectPath "$ROOT" -logFile "$LOG" "$@"
CODE=$?
echo "[unity.sh] exit=$CODE log=$LOG"
exit $CODE
