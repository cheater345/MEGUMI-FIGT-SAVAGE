# CI / Release Pipeline

## Workflow

`.github/workflows/build.yml` (game-ci/unity-builder v4):

- **Triggers:** push to `main`, pull requests into `main`, manual `workflow_dispatch`, and published GitHub releases.
- **Job `build`** (single job → one Unity license):
  1. Checkout (LFS).
  2. Build Android Debug APK → `android-debug-apk` artifact.
  3. Build Android Release APK (signed from secrets) → `android-release-apk` artifact.
  4. Build Windows Standalone → `windows-build` artifact.
- **Job `release`** (only when a release is published): downloads artifacts and attaches them to the GitHub Release via `softprops/action-gh-release`.

## License activation (Unity Personal — free, no PC needed)

> Unity no longer supports manual (`.alf`/`.ulf` file) activation of **Personal**
> licenses. The current flow activates **online** inside the CI container using
> the email/password of a free Unity account.

1. Create a free Unity account at https://id.unity.com (any device with a browser works).
2. In the repo: **Settings → Secrets and variables → Actions → New repository secret**:
   - `UNITY_EMAIL` — your Unity account email
   - `UNITY_PASSWORD` — your Unity account password
3. Run the **Build** workflow. game-ci activates the Personal license online,
   builds, then releases the license after the job.

Notes:

- The same activation works on any OS — the license is not tied to a Unity
  editor version or platform.
- Do **not** create `UNITY_SERIAL` or `UNITY_LICENSE` secrets unless you own a
  paid Plus/Pro license.
- If you own Plus/Pro instead: add `UNITY_SERIAL`, `UNITY_EMAIL`,
  `UNITY_PASSWORD` and remove the personal note.

## Secrets required (Android signing, optional)

| Secret | Used for |
|---|---|
| `STEEL_TEMPEST_KEYSTORE_B64` | release signing keystore (base64) |
| `STEEL_TEMPEST_KEYSTORE_PASS` | keystore password |
| `STEEL_TEMPEST_KEY_ALIAS` | key alias |
| `STEEL_TEMPEST_KEY_PASS` | key password |

Without these the release APK is built unsigned (still installable by
sideloading).

## Making a release

1. Ensure activation secrets are set in the repo.
2. Push a tag (e.g. `v1.0.0`) or publish a GitHub Release — both trigger the pipeline.
3. The `release` job attaches the APKs to the release page automatically.

## Notes

- A single job keeps the Unity license used only once per run.
- `if-no-files-found: error` fails the job if a build silently produced nothing.
- The debug APK is always unsigned; release APK requires the keystore secret.