#!/usr/bin/env bash
# Packages the web build (Builds/WebGL) for a static host such as a claude.ai Artifact:
# the loader and framework are copied as they are, the wasm and data files are gzipped and
# base64-encoded (they must be a served type and stay under the host's per-file limit; the
# page decodes and inflates them in the browser), and index.html gets the packed sizes.
# Usage: Tools/web/package_artifact.sh [outdir]   (default Builds/WebArtifact)
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SRC="$ROOT/Builds/WebGL/Build"
OUT="${1:-$ROOT/Builds/WebArtifact}"
LOADER="$(ls "$SRC"/*.loader.js | head -1)"
BASE="$(basename "$LOADER" .loader.js)"
rm -rf "$OUT" && mkdir -p "$OUT/Build"
cp "$SRC/$BASE.loader.js" "$OUT/Build/BADLIE.loader.js"
cp "$SRC/$BASE.framework.js" "$OUT/Build/BADLIE.framework.js"
# Artifacts serve only web file types (no arbitrary binaries), so the gzipped files ship
# as base64 text; the page decodes and inflates them.
gzip -9 -n -c "$SRC/$BASE.wasm" | base64 -w 0 > "$OUT/Build/BADLIE.wasm.gz.b64.txt"
gzip -9 -n -c "$SRC/$BASE.data" | base64 -w 0 > "$OUT/Build/BADLIE.data.gz.b64.txt"
W=$(stat -c %s "$OUT/Build/BADLIE.wasm.gz.b64.txt")
D=$(stat -c %s "$OUT/Build/BADLIE.data.gz.b64.txt")
sed -e "s/__WASM_GZ_SIZE__/$W/" -e "s/__DATA_GZ_SIZE__/$D/" "$ROOT/Tools/web/index.html" > "$OUT/index.html"
LIMIT=$((16 * 1024 * 1024))
for f in "$OUT"/Build/*; do
  s=$(stat -c %s "$f")
  [ "$s" -le "$LIMIT" ] || echo "WARNING: $(basename "$f") is $s bytes, over the 16 MB per-file limit for text"
done
ls -la "$OUT" "$OUT/Build"
