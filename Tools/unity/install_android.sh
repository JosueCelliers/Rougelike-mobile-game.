#!/usr/bin/env bash
# Installs Unity Android Build Support (playback engine, OpenJDK, SDK, NDK) for the headless
# Linux editor, using the files and destinations listed by Unity's release API for
# 6000.3.25f1. Unity Hub does the same on a desktop. Needs ~9 GB free while installing,
# ~5.5 GB afterwards (the Development and Mono player variations are skipped: release IL2CPP
# builds do not use them).
# Usage: Tools/unity/install_android.sh [download-dir]
set -euo pipefail
EDITOR_ROOT="${UNITY_ROOT:-/opt/unity/6000.3.25f1}"
A="$EDITOR_ROOT/Editor/Data/PlaybackEngines/AndroidPlayer"
DL="${1:-$HOME/android-install}"
mkdir -p "$DL" "$A"
cd "$DL"
get() { [ -s "$2" ] || curl -sSL --retry 3 --fail -o "$2" "$1"; }
G=https://dl.google.com/android/repository

# 1. Android playback engine: an xar .pkg wrapping a gzip'd cpio payload.
if [ ! -f "$A/AndroidPlayerBuildProgram.exe" ]; then
  get https://download.unity3d.com/download_unity/e1dba0a9aba4/MacEditorTargetInstaller/UnitySetup-Android-Support-for-Editor-6000.3.25f1.pkg android.pkg
  7z x -y android.pkg -opkg >/dev/null
  rm -f android.pkg
  (cd "$A" && cpio -idm -f './Variations/il2cpp/Development/*' './Variations/mono/Development/*' './Variations/mono/Release/*' < "$DL/pkg/Payload~")
  rm -rf pkg
fi

# 2. OpenJDK 17 -> AndroidPlayer/OpenJDK
get https://download.unity3d.com/download_unity/open-jdk/open-jdk-linux-x64/jdk17.0.18-8_86b9da7ec57717bdf4d79c35f1c2dde77b5a587fb706909085a8902cf37ead51.zip jdk.zip
mkdir -p "$A/OpenJDK" && unzip -qo jdk.zip -d "$A/OpenJDK"

# 3. NDK r27c -> AndroidPlayer/NDK
get $G/android-ndk-r27c-linux.zip ndk.zip
rm -rf "$A/NDK" && mkdir -p "$A/NDK.tmp" && unzip -qo ndk.zip -d "$A/NDK.tmp" && mv "$A/NDK.tmp/android-ndk-r27c" "$A/NDK" && rm -rf "$A/NDK.tmp"

# 4. SDK pieces -> AndroidPlayer/SDK
S="$A/SDK"
mkdir -p "$S/platforms" "$S/build-tools" "$S/cmdline-tools" "$S/cmake"
get $G/platform-tools_r36.0.0-linux.zip platform-tools.zip && unzip -qo platform-tools.zip -d "$S"
get $G/build-tools_r36_linux.zip build-tools.zip && rm -rf "$S/build-tools/36.0.0" && unzip -qo build-tools.zip -d "$S/build-tools" && mv "$S/build-tools/android-16" "$S/build-tools/36.0.0"
get $G/platform-36_r02.zip platform-36.zip && unzip -qo platform-36.zip -d "$S/platforms"
get $G/platform-35_r01.zip platform-35.zip && unzip -qo platform-35.zip -d "$S/platforms"
get $G/commandlinetools-linux-12266719_latest.zip cmdline-tools.zip && rm -rf "$S/cmdline-tools/16.0" && unzip -qo cmdline-tools.zip -d "$S/cmdline-tools" && mv "$S/cmdline-tools/cmdline-tools" "$S/cmdline-tools/16.0"
get $G/cmake-3.22.1-linux.zip cmake.zip && rm -rf "$S/cmake/3.22.1" && mkdir -p "$S/cmake/3.22.1" && unzip -qo cmake.zip -d "$S/cmake/3.22.1"
chmod -R u+x "$A/OpenJDK/bin" "$S/platform-tools" "$S/build-tools/36.0.0" "$S/cmdline-tools/16.0/bin" "$S/cmake/3.22.1/bin" 2>/dev/null || true
echo "[install_android] done: $(du -sh "$A" | cut -f1) in $A"
