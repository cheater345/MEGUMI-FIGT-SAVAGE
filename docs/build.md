# Build & Project Configuration

## Unity version

- **6000.0.23f1** (Unity 6 LTS) — see `ProjectSettings/ProjectVersion.txt`.

## Required build support

- **Android** (OpenJDK + Android SDK via Unity Hub module install).
- **Windows Standalone** (IL2CPP optional; Mono is fine for local dev).

## Packages (see `Packages/manifest.json`)

| Package | Version | Purpose |
|---|---|---|
| com.unity.render-pipelines.universal | 17.0.3 | URP 2D pipeline |
| com.unity.inputsystem | 1.11.2 | Input System (future use; legacy Input API also enabled) |
| com.unity.addressables | 2.3.1 | Asset management for streaming content |
| com.unity.ugui | 2.0.0 | UI |
| com.unity.textmeshpro | 3.0.9 | Text for HUD |
| com.unity.2d.animation / 2d.spriteshape | 10.1.2 / 10.0.3 | 2D sprites |
| com.unity.test-framework | 1.4.5 | Edit/Play mode tests |
| com.unity.timeline | 1.8.7 | Cutscenes / cinematics |

The project currently uses the legacy Input Manager in code (`Input.GetKey`) but
also ships the Input System package for future rebinding. Set **Active Input
Handling** to **Both** in *Project Settings > Player > Active Input Handling* so
both APIs work while the migration happens.

## Build outputs

- `build/AndroidDebug.apk` — debug build (no keystore).
- `build/AndroidRelease.apk` — release build, signed when secrets are present.
- `build/Windows/SteelTempest.exe` — Windows standalone.

## Build methods

Editor menu:

- `Tools > Steel Tempest > Build Android Debug`
- `Tools > Steel Tempest > Build Android Release`

Headless (CI-compatible):

```
unity-editor -batchmode -quit -projectPath . \
  -executeMethod SteelTempest.EditorTools.BuildScript.CI_AndroidDebug -logFile -
```

Optional env var `STEEL_TEMPEST_BUILD_DIR` overrides the output root.

## Release signing (Android)

The release build reads these environment variables:

| Env var | Meaning |
|---|---|
| `STEEL_TEMPEST_KEYSTORE_B64` | Base64 of the release keystore |
| `STEEL_TEMPEST_KEYSTORE_PASS` | Keystore password |
| `STEEL_TEMPEST_KEY_ALIAS` | Key alias (default `steeltempest`) |
| `STEEL_TEMPEST_KEY_PASS` | Key password (defaults to store pass) |

If absent the release build runs unsigned — fine for sideloading, not for store
distribution. Generate a keystore locally with:

```
keytool -genkey -v -keystore steeltempest.keystore -alias steeltempest \
  -keyalg RSA -keysize 2048 -validity 10000
```

Then base64-encode it for the secret:

```
base64 -w0 steeltempest.keystore
```