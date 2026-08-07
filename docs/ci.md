# CI / Release Pipeline

## Workflow

`.github/workflows/build.yml` (game-ci/unity-builder v4):

- **Triggers:** push to `main`, pull requests into `main`, manual `workflow_dispatch`, and published GitHub releases.
- **Job `build`** (single job → one Unity license):
  1. Checkout (LFS).
  2. Activate Unity license (`UNITY_LICENSE` secret).
  3. Build Android Debug APK → `android-debug-apk` artifact.
  4. Build Android Release APK (signed from secrets) → `android-release-apk` artifact.
  5. Build Windows Standalone → `windows-build` artifact.
  6. Deactivate license.
- **Job `release`** (only when a release is published): downloads artifacts and attaches them to the GitHub Release via `softprops/action-gh-release`.

## Secrets required

| Secret | Used for |
|---|---|
| `UNITY_LICENSE` | game-ci license activation |
| `STEEL_TEMPEST_KEYSTORE_B64` | release signing keystore (base64) |
| `STEEL_TEMPEST_KEYSTORE_PASS` | keystore password |
| `STEEL_TEMPEST_KEY_ALIAS` | key alias |
| `STEEL_TEMPEST_KEY_PASS` | key password |

## Making a release

1. Ensure `UNITY_LICENSE` + signing secrets are set in the repo.
2. Push a tag (e.g. `v1.0.0`) or publish a GitHub Release — both trigger the pipeline.
3. The `release` job attaches the APKs to the release page automatically.

## Notes

- A single job keeps the Unity license used only once per run.
- `if-no-files-found: error` fails the job if a build silently produced nothing.
- The debug APK is always unsigned; release APK requires the keystore secret.