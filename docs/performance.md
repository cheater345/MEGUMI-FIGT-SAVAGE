# Performance & Targets

## Targets

| Metric | Goal |
|---|---|
| Frame rate | 60 FPS |
| Platform | mid-range Android (e.g. 2D scenes), plus Windows PC |
| RAM (Android) | < 1.5 GB working set |
| Loading | Level loads under ~2 s (Addressables streaming) |
| Build size | keep primary scene + pool light |

## Rules of thumb in this codebase

1. **Use the object pool** (`SteelTempest.Pool.ObjectPool`). Hitboxes (and any
   per-attack VFX) must go through the pool, not `Instantiate` per swing. This
   avoids GC churn in combat-dense fights.
2. **Avoid per-frame allocations.** The `EventBus` publishes value-type payloads
   (structs) and stores delegates; string building is limited to debug/HUD paths.
3. **`Time.fixedDeltaTime` scales with the finisher slow-motion.** The finisher
   corridor sets `Time.timeScale = 0.25` and restores `fixedDeltaTime` in
   `FinisherTimeController`; keep `Update` work cheap so the dilation doesn't
   accumulate.
4. **Rigidbody movement** uses `linearVelocity` + `MoveTowards` in `FixedUpdate` —
   no per-frame `SetActive` on the player.
5. **Data as SOs.** Design-facing stat tables are `ScriptableObject`, so tuning
   doesn't recompile or reallocate.

## URP 2D config

- `Packages/manifest.json` pins `com.unity.render-pipelines.universal` 17.0.3.
- 2D lights are cheap; prefer a few point lights for the combat feel rather than
  lots of overlapping shadow-casting lights (Android fill-rate).
- Silhouette art style ("black enemy silhouettes on lit backdrops") keeps
  overdraw low — one solid sprite per unit, no translucent layers.

## Loading & memory

- Addressables 2.3.1 is configured; large content (audio, alternate weapon
  skins) should be Addressable, streamed, and released when off-screen.
- Save/load uses a single JSON blob under `Application.persistentDataPath`;
  writes are deferred (on pause / scene exit), never per-frame.

## Profiling loop (suggested)

1. Enable `Profile Player` on device; watch spikes during finisher + waves.
2. Confirm no `GC.Alloc` growth in `PlayerCombat.Update` / `Hitbox.OnTriggerEnter2D`.
3. Profile build on a mid-range target (e.g. 4 GB RAM phone) at 60 fps;
   if frame time > 16.6 ms, first inspect sprite overdraw then collider counts.