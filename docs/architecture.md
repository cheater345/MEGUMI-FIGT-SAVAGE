# Architecture

## High-level design

Steel Tempest is a component-based Unity 6 project with a small dependency
container, an event bus for decoupled cross-system messaging, and data-driven
ScriptableObjects for all design-facing content (weapons, attacks, talents,
items, game modes).

```
Scene (Boot/Main prefab = GameBootstrap)
   │
   ├─ ServiceLocator  (DI: SaveManager, CurrencyManager, EventBus …)
   ├─ EventBus        (struct events: Damage, Defeated, Parry, Notification, …)
   └─ GameSystems     (player, enemy spawners, HUD, camera)
```

## Namespaces & responsibilities

| Namespace | Responsibility |
|---|---|
| `SteelTempest.Core.Di` | `ServiceLocator` — lightweight constructor-style DI |
| `SteelTempest.Core.Events` | `EventBus` + `DamageEvent`, `ActorDefeatedEvent`, `ParryEvent`, `NotificationEvent` |
| `SteelTempest.Core.Bootstrap` | `GameBootstrap` — scene-lifetime wiring/init |
| `SteelTempest.Player` | `Controls` (static input), `DesktopInput`, `PlayerController` |
| `SteelTempest.Combat` | `HealthComponent`, `DamageService`, `Hitbox`, `AttackData`, `ComboTree`, `PlayerCombat` |
| `SteelTempest.Weapons` | `WeaponData` (SO), `WeaponFactory` (authoring) |
| `SteelTempest.Enemies` | `EnemyController`, `BossController`, `EnemySpawner`, `PlayerMarker` |
| `SteelTempest.Items` | `ItemTemplate`, `ItemInstance`, `Inventory`, `EquipSlot`, `Rarity` |
| `SteelTempest.Economy` | `CurrencyManager`, `LootService`, `DailyRewards` |
| `SteelTempest.Progression` | `PlayerProgress` (level/XP), `Talent`, `SkillTree` |
| `SteelTempest.Save` | `SaveData`, `SaveManager` (JSON persistence) |
| `SteelTempest.Modes` | `ModeDefinition` (SO), `ModeSession` |
| `SteelTempest.UI.Hud` | `HudController` (health bar/currency/toasts), `CameraShake` |
| `SteelTempest.Pool` | `ObjectPool` — generic component pool for hitboxes/vfx |
| `SteelTempest.EditorTools` | `BuildScript` (CI), `PlaceholderArtGenerator` |

## Dependency rules

- **Downward only.** `UI` and gameplay systems depend on `Core`; `Core` depends
  on nothing. `Items`/`Weapons` never reach into gameplay systems.
- Systems talk via the **EventBus**, never direct references to unrelated monobehaviours.
- Persistent state lives in `SaveManager` (JSON) and is the single source of truth.

## Bootstrap sequence

1. `GameBootstrap.Awake()` → `DontDestroyOnLoad`.
2. Registers `EventBus`, `SaveManager`, `CurrencyManager` in the `ServiceLocator`.
3. `Initialize()`s the managers (loads save, wires currencies).
4. Scene gameplay prefabs resolve services via `ServiceLocator.Instance.Resolve<T>()`.
5. On `OnApplicationPause` the game writes the save file.

## Adding a new system

1. Put the class in the owning namespace under `Assets/Scripts/…`.
2. If it must outlive scenes, register it in `GameBootstrap.Awake()`.
3. Emit/consume `EventBus` messages rather than direct references where sensible.
4. Expose purely design-facing constants as ScriptableObject data.