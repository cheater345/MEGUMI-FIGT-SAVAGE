# Steel Tempest

A production-ready, **fully original** 2D martial-arts action RPG built with **Unity 6 LTS** (URP) for **Android** and **PC (Windows)**.

> All art, audio, code and design are original placeholder work in progress. Nothing in this project is copied from existing commercial games.

## Highlights

- **Combat** — light/heavy/charged attacks, combos, air attacks, perfect-block (parry) with counter windows, dodges (i-frames), dashes, finishers with slow-motion and camera shake.
- **10 weapon classes** — Sword, Greatsword, Katana, Dagger, Spear, Staff, Axe, Hammer, Fists, Dual Blades, each with unique tuned stats and combo trees.
- **Progression** — XP/levels, skill points, a data-driven talent tree, equipment (helmet/armor/gloves/boots/accessory) with rarities, upgrade levels and stat growth.
- **Economy** — coins, gems, loot/kill rewards, daily login rewards, lazy-persisted save file.
- **Enemies** — light/heavy/assassin/elite/mini-boss archetypes with movement, block and dodge reactions, plus a multi-phase boss controller.
- **Game modes** — Story, Survival, Boss Rush, Endless, Challenge, Training (mode data + wave spawner).
- **Technical** — clean-ish architecture, DI container (`ServiceLocator`), event bus, ScriptableObject data, object pooling, async-friendly saving, URP for Android-first performance.

## Repository layout

```
Assets/
  Editor/         Build scripts + placeholder art generator (editor-only)
  Scripts/
    Core/         DI container, event bus, game events, bootstrap
    Player/       Input state + desktop adapter, movement controller
    Combat/       Health, damage service, hitboxes, attack data, combos, player combat
    Weapons/      WeaponData SO + factory
    Enemies/      Enemy controller, boss controller, spawner, player marker
    Items/        Item templates, instances, inventory, equipment slots
    Economy/      Currency manager, loot service, daily rewards
    Progression/  Level/XP, skill tree
    Save/         SaveData + SaveManager (JSON)
    Modes/        Game mode definitions + session state
    Pooling/      Generic object pool
    UI/           HUD controller + camera shake
Packages/         UPM manifest (URP, Input System, Addressables, 2D, TMP, tests)
ProjectSettings/  Unity 6.0 (6000.0.81f1)
docs/             Architecture, gameplay, build, CI, testing, performance
.github/workflows/build.yml
```

## Requirements

- **Unity 6000.0.81f1** (Unity 6 LTS) with **Android Build Support** (OpenJDK/SDK via Unity Hub) and **Windows Build Support**.
- Git + [Git LFS](https://git-lfs.com/) (large binaries, if any, are tracked via LFS).

## Quick start

1. Clone the repo.
2. Open the folder as a **Unity project** (6000.0.81f1). Let packages import.
3. Run menu **Tools > Steel Tempest > Generate Placeholder Sprites** to create the placeholder silhouettes under `Assets/Art/Generated`.
4. Open the **Boot/Main scene** (add one scene to Build Settings; the scene list is also needed for CI builds) and press **Play**.

## Keyboard controls (desktop)

| Action          | Key        |
|-----------------|------------|
| Move            | A / D (or arrows) |
| Run             | Left Shift |
| Jump            | Space      |
| Dash            | X          |
| Dodge (i-frames)| C          |
| Crouch          | S (held)   |
| Block / Parry   | V (held)   |
| Light attack    | J          |
| Heavy (charge)  | K (press + release) |
| Skill           | L          |

On Android the same `Controls` static state is driven by on-screen buttons.

## Building

### Locally

- Editor menus: `Tools > Steel Tempest > Build Android Debug / Release`.
- Headless:

```
unity-editor -batchmode -quit -projectPath . \
  -executeMethod SteelTempest.EditorTools.BuildScript.CI_AndroidDebug \
  -logFile -
```

`STEEL_TEMPEST_BUILD_DIR` overrides the output folder (default `build/`).

### CI (GitHub Actions)

The `.github/workflows/build.yml` build on push/PR, and attach artifacts to GitHub Releases when a release tag is published.

**Unity license (free Personal, no PC needed):** add your free Unity account as secrets — `UNITY_EMAIL` and `UNITY_PASSWORD` (activate at https://id.unity.com). game-ci activates the license online inside the CI container. Unity no longer supports `.alf`/serial activation for Personal licenses.

Other secrets (optional, for signing the release APK):

| Secret                          | Purpose |
|---------------------------------|---------|
| `STEEL_TEMPEST_KEYSTORE_B64`    | base64-encoded Android release keystore |
| `STEEL_TEMPEST_KEYSTORE_PASS`   | keystore password |
| `STEEL_TEMPEST_KEY_ALIAS`       | key alias |
| `STEEL_TEMPEST_KEY_PASS`        | key password |

Without signing secrets the release APK is built unsigned (installs to sideload only).

## Documentation

- [Architecture](docs/architecture.md)
- [Gameplay & combat](docs/gameplay.md)
- [Build & project config](docs/build.md)
- [CI / release pipeline](docs/ci.md)
- [Testing](docs/testing.md)
- [Performance & targets](docs/performance.md)

## License

MIT — see [LICENSE](LICENSE).