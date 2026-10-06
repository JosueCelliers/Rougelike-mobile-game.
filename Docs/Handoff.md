# Handoff: environment setup notes

Read this before installing anything. It records what was verified in the first cloud
session (2026-10-05), so the next session can skip the dead ends.

## Unity install (verified working)
- Version: **6000.3.25f1** (Unity 6.3 LTS, newest Unity 6 LTS on 2026-10-05).
  Found via `https://services.api.unity.com/unity/editor/release/v1/releases?stream=LTS&platform=LINUX&architecture=X86_64&limit=8&order=RELEASE_DATE_DESC`
- Install by streaming the archive. It takes about 3 minutes and uses 8.2 GB on disk, and
  `Editor/Data/PlaybackEngines/LinuxStandaloneSupport` is included:
  ```
  mkdir -p /opt/unity/6000.3.25f1 && cd /opt/unity/6000.3.25f1 && \
  curl -sSL --retry 3 --fail https://download.unity3d.com/download_unity/e1dba0a9aba4/LinuxEditorInstaller/Unity-6000.3.25f1.tar.xz | tar -xJf -
  ```
- System packages (apt works):
  `xvfb xauth libgl1-mesa-dri libglx-mesa0 libegl1 libgles2 libglu1-mesa libvulkan1 mesa-vulkan-drivers libgtk-3-0t64 libnss3 libasound2t64 libgbm1 libxss1 libxtst6 libxrandr2 libxcursor1 libxinerama1 libxi6 libxcomposite1 libxdamage1 libcups2t64 libsecret-1-0 libnotify4 libcanberra-gtk3-module p7zip-full cpio mesa-utils`
- Network: the environment allowlist must include `*.unity3d.com`, `*.unity.com`,
  `dl.google.com` (plus the default package-manager list).
- Modules: `LinuxEditorTargetInstaller/UnitySetup-Android-Support-for-Editor-<ver>.tar.xz`
  returns **404**. The release API lists the Android module as a `.pkg`, which has to be
  extracted with 7z and cpio. iOS, WebGL and Linux IL2CPP have Linux `.tar.xz` modules.
  **`Tools/unity/install_android.sh` does the whole Android install** (verified in session 2):
  - It extracts the playback engine from the `.pkg`, skipping the 3.7 GB of Development and
    Mono variations.
  - It installs OpenJDK 17, NDK r27c, SDK platforms 35 and 36, build-tools 36,
    platform-tools, cmdline-tools and CMake at the paths Unity's release API gives.
  - The install takes about 5.7 GB, and needs about 9 GB free while it runs.
- Android builds: `Tools/unity/unity.sh build_android -quit -buildTarget Android -executeMethod
  BadLie.EditorTools.BuildTools.BuildAndroid`.
  - The first IL2CPP build takes about 20 minutes on 4 cores.
  - Gradle downloads the Android Gradle Plugin 9.0.0 from `dl.google.com` through the
    proxy. Once, this failed transiently with "plugin was not found". Running the exported
    project's Gradle by hand (`Library/Bee/Android/Prj/IL2CPP/Gradle`) worked, and so did
    the next Unity build.

## Licensing (the blocker in session 1)
- A `Unity_lic.ulf` activated on another computer is **rejected** here.
  `Unity.Licensing.Client --showAllEntitlements` reports
  `Status: LicenseInvalidBindings — Machine bindings are not valid`. Batch mode then fails
  with `'com.unity.editor.headless' was not found`, and the windowed editor fails with
  `'com.unity.editor.ui' was not found`.
- Supported fix: activate a Personal license **for this machine**, using credentials stored
  as environment variables (never paste them in chat or print them):
  ```
  C=/opt/unity/6000.3.25f1/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client
  $C --activate-ulf --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"
  $C --showAllEntitlements   # expect a valid status incl. com.unity.editor.headless
  ```
  Return it at the end of the work: `$C --return-ulf --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"`.
- Environment variables are only read when a session starts or its VM is restored after
  idling, so a mid-session change needs a new session.

## Gotchas
- Don't run `pkill -f <pattern>` when the pattern also appears in your own command line,
  because it kills the shell. Kill by PID instead.
- There's no GPU. Render with Xvfb + Mesa llvmpipe, and capture real gameplay from a Linux
  player build at phone resolution (for example 1080×2340) driven by scripted input.
  Frame rates measured here say nothing about phone performance.

## Plan decisions so far
- Visual rules: `Docs/ArtDirection.md`, written from the three reference images.
- Golf feel: a custom deterministic, substepped ball simulation (surfaces with distinct
  friction, slopes and ledges, continuous wall collisions, a consistent cup-capture rule,
  hazards with a +1 penalty that returns the ball to its last lie). The **same simulation
  drives the trajectory preview**, so the preview always agrees with the shot.
- Holes are data (polygons with surface type and height model, walls, cup, tee,
  props). A builder turns them into physics and meshes. An editor tool generates the
  environment kit (vegetation, stonework, machinery prefabs, materials) reproducibly.
- Custom URP shaders: stylized lit with violet-brown shadow tint, terrain with mowing
  stripes, water, foliage wind, a ball with an occlusion silhouette, and height fog.
- Run structure: shared stroke budget, restore on completing a hole, 1-of-3 upgrade choice
  between holes. The current shot resolves before failure is checked. Interrupted shots are
  resolved deterministically on resume.

## Session 2 (2026-10-05/06): state at the end
- **Game.** The game is complete and playable: 5 holes, 6 upgrades and 2 combos, the run
  loop, UI, audio and save/resume. Status and launch steps are in `README.md`, tuning and
  playtest data in `Docs/Tuning.md`.
- **Captures.** `Tools/unity/capture.sh <scenario> W H [outdir]` drives a Linux player
  build:
  - Scenarios: `flow`, `lookall`, `holes`, `fx`, `look`, `hole1`.
  - Use the release build (`BuildLinuxRelease`, set `BADLIE_PLAYER`) for screenshots without
    the "Development Build" watermark.
  - Software rendering takes about 45 s per frame at 540×1170 and about 3 min at
    1080×2340.
  - Extra player arguments go in `CAPTURE_EXTRA`, for example
    `-captureHoles 2,5 -safearea 132,96`.
- **Long jobs.** The bot playtest (about 1 h) can run in a copy of the project, so the main
  project stays free for builds. `rsync` is not installed; use `cp -a`.
- **Web build.** The Unity WebGL module installs with one stream into the editor folder
  (5.5 GB):
  `curl -sSL https://download.unity3d.com/download_unity/e1dba0a9aba4/LinuxEditorTargetInstaller/UnitySetup-WebGL-Support-for-Editor-6000.3.25f1.tar.xz | tar -xJf - -C /opt/unity/6000.3.25f1`.
  - A first build takes about 8 minutes.
  - Artifacts serve only web file types, at most 16 MB each, so
    `Tools/web/package_artifact.sh` ships the gzipped wasm and data as base64 text.
  - `node Tools/web/test_local.mjs <dir> <png> play` boots the page in headless Chromium,
    taps NEW RUN and TEE OFF, and plays a shot. Set `PLAYWRIGHT_MODULE=$(npm root -g)/playwright`.
