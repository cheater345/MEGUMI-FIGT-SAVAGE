# Testing

## Test framework

`com.unity.test-framework` 1.4.5 is included in the manifest. Two suites:

- **Edit mode** tests — pure C#, no scene needed.
- **Play mode** tests — spin up scenes, verify component behaviour.

## Current coverage

Unit tests (planned/added in `Assets/Tests/`):

| Area | What to assert |
|---|---|
| `PlayerProgress` | Level-up threshold math, skill point grants |
| `CurrencyManager` | Spend/grant, insufficient-funds rejection |
| `Inventory` | Equip/unequip swapping, upgrade cost growth |
| `SkillTree` | Parent gating, re-learn rejection, bonus summation |
| `ServiceLocator` | Register/resolve caching, unknown-type throw |
| `EventBus` | Subscribe/publish/unsubscribe |

Integration tests (play mode):

- `PlayerController` — jump/coyote/dash state transitions.
- `HealthComponent` — parry negates, block reduces, i-frames reject damage.

## Running

In the Unity editor:

- **Window > General > Test Runner** → run Edit Mode / Play Mode suites.

Headless (CI-ready):

```
unity-editor -batchmode -runTests -projectPath . \
  -testPlatform EditMode -testResults results.xml -logFile -
```

## Guidance

- Keep unit tests free of scene requirements so they run in Edit Mode.
- Tag play-mode tests with `[UnityTest]` and reset `Time.timeScale` in teardown
  (finisher slow-motion changes global time scale).
- Never assert on frame-dependent timing; assert on state transitions.