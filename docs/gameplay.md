# Gameplay & Combat

## Levels of player mobility

The `PlayerController` state machine:

- **Walk / Run** — Run is enabled with an input, giving faster ground speed.
- **Jump** — with coyote time (0.12 s) and input buffering (0.12 s) for reliable platforming.
- **Air control** — horizontal control persists in the air.
- **Dash** — forward burst; on cooldown; usable in air when configured.
- **Dodge** — invulnerable dodge with i-frames, slightly longer cooldown than dash.
- **Crouch** — grounded only, disengages when moving.

## Combat core

Combat is data-driven through `ComboTree` (ordered list of `AttackData`).

An attack has three timed phases:

1. **Startup** — wind-up; can be cancelled into dodge.
2. **Active** — the hitbox is live for `activeSeconds`; by default it can hit each target once per swing (`Hitbox._hit` set).
3. **Recovery** — after the swing; during this and the next combo window a successive attack continues the chain; otherwise the combo resets.

Light attacks chain through the **ground combo tree**. Charged heavy attacks use the `chargedCombos` tree. Air attacks use `airCombos`.

### Hits & crits

- Base damage from `WeaponData.baseDamage` × attack multiplier.
- Crits roll per swing (`AttackData.critChance`) apply `critMultiplier`.
- On hit, `DamageService` adds knockback (and optionally launches).

### Defense

- **Dodge** — i-frames ignore all damage.
- **Block (held)** — passive block always active while held; first `parryWindowSeconds` of a block is a **perfect block**.
- **Perfect block / parry** — negates the hit entirely, gives a short invulnerable window to counter, and raises `ParryEvent` (skills/HUD can react).

## Finishers & feel

- Attacks flagged `isFinisher` trigger the **slow-motion finisher** — time scale drops to ~0.25 s for 0.9 s (via `FinisherTimeController`), with `CameraShake` for impact.
- All effects are event-driven so the art/audio teams can hook in decoupled.

## Enemies

`EnemyController` handles light/heavy/assassin/elite archetypes:

- Chases the player inside leash range; falls back past the leash.
- Reacts defensively: chance to **dodge** or **block** after being struck.
- Attacks within range on a per-archetype cooldown.

`BossController` is a **phase machine**:

- **Phase 1:** telegraphed, slow heavy swings.
- **Phase 2** (≤66 % HP): faster, shorter intervals.
- **Phase 3** (≤33 % HP): enrage — faster movement, wider reach, occasional blocks.

`EnemySpawner` uses the active `ModeDefinition` to push waves, converting to boss
waves at `wavesBeforeBoss` intervals and scaling difficulty with `ModeSession.DifficultyScale`.

## Game modes

| Mode | Description |
|---|---|
| Story | Fixed objectives, checkpoint-based |
| Survival | Wave defense; no checkpoints, death penalty |
| Boss Rush | Boss-only gauntlet |
| Endless | Infinite scaling waves |
| Challenge | Modifier traffic (e.g. no wooden shield challenge) |
| Training | Free practice vs. dummies |

The active mode is a `ModeDefinition` ScriptableObject referenced by the spawner
and HUD.