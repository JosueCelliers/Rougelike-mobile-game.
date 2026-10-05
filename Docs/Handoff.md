# Handoff: environment setup notes (from the first session)

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
