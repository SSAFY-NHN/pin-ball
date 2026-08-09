# Battle Unit Golden Light Design

## Goal

When a wave enters the active combat state, show a restrained golden pool of light beneath every allied and enemy unit. Keep the light visible for the entire active wave, including while units move, and remove it outside combat without using per-unit real-time lights.

## Scope

- Apply the effect to both allied and enemy unit prefabs.
- Keep the effect disabled during `Pending`, `Victory`, and `Defeat`.
- Enable the effect throughout `EWaveState.Active`.
- Make the pool follow its owning unit by placing it as a serialized prefab child.
- Use the project's existing HDR camera, Bloom, and lightweight additive shader path.
- Preserve unit animation, state tint, combat logic, pooling, shadows, and sorting behavior.
- Do not add `Light2D`, runtime-created objects/components, external packages, or unrelated visual changes.

## Current Context

`BattleManager` already publishes `OnStateChanged` and exposes the current `EWaveState`. `AllyUnit.prefab` and `EnemyUnit.prefab` both contain a serialized `GroundShadow` child rendered below the character. Units are pooled and reused with `SetActive`, so the golden light must reset correctly whenever a pooled unit is enabled.

The game scene already enables HDR and post-processing through `ArcaneGameLook`. Its Bloom uses a threshold of `0.6`, intensity `1.1`, and low-cost filtering. The project also has a WebGL-suitable additive sprite shader with an HDR intensity property. The global Bloom configuration therefore does not need to change.

## Design

### Visual composition

Each unit prefab receives a `GoldenLightPool` child beside `GroundShadow`. Its SpriteRenderer displays a transparent, horizontally stretched golden pool with:

- a small, readable bright center;
- a soft transparent edge;
- enough HDR intensity for the existing Bloom to produce a controlled halo;
- no broad glow baked into the source image;
- a sorting order behind the unit and coordinated with the existing ground shadow.

The pool is sized to frame the unit's feet rather than the whole character. Its intensity and alpha remain modest so overlapping units do not produce a large white patch. Allies and enemies use the same visual treatment because the requested light describes the shared battle stage, not team identity.

### State controller

A small instance component owns only the golden-light visibility. It obtains `BattleManager` through the existing `App.Get<BattleManager>()` access policy and receives the prefab's light renderer or light object through an Inspector reference.

On initialization it subscribes to `BattleManager.OnStateChanged`, immediately applies the manager's current state, and unsubscribes symmetrically when destroyed. It enables the serialized light only when the state is `EWaveState.Active`.

Because pooled units are disabled and re-enabled, the component also reapplies the current state when enabled. This prevents an enemy spawned after the active-state event from missing the light and prevents a reused unit from carrying stale visibility into preparation.

The controller does not modify the character SpriteRenderer color. This preserves `UnitBase` hit, attack, idle, and death tint behavior.

### Bloom and material behavior

The golden pool uses the existing additive shader rather than a new real-time lighting system. A dedicated material sets the pool's baseline HDR intensity and lets the existing global Bloom create the final halo.

Global Bloom values remain unchanged. Raising global intensity would also brighten pinball VFX, projectiles, and other emissive art. Visual tuning is therefore isolated to the golden-pool material and prefab scale/alpha.

The first-pass tuning target is:

- clearly visible gold on the dark battle floor;
- a narrow Bloom halo around the pool;
- no loss of character silhouette or health-bar readability;
- acceptable overlap with five allies and the maximum configured enemy count.

Exact color, intensity, scale, and opacity are Inspector-tuned visual values, not new gameplay data.

### Pooling and lifecycle

No unit is instantiated solely for the effect. The light child is part of the existing ally and enemy prefabs, so it follows their pooling lifecycle automatically.

- Pooled/inactive unit: the entire hierarchy is inactive.
- Spawned during `Active`: `OnEnable` reads the active state and shows the light.
- Ally restored for preparation: the state has returned to `Pending`, so the light remains hidden when the ally is reactivated.
- Dead ally: the existing unit deactivation hides the complete hierarchy.
- Enemy returned to its pool: the existing unit deactivation hides the complete hierarchy.

## Component Changes

- New golden light pool Sprite and import metadata in the existing effects art area.
- New dedicated golden additive material in the existing materials structure, reusing the current additive shader.
- New focused battle-unit golden-light controller script in the existing visual scripts area.
- `AllyUnit.prefab`: serialized `GoldenLightPool` child and controller references.
- `EnemyUnit.prefab`: serialized `GoldenLightPool` child and controller references.
- EditMode tests: cover the state-to-visibility decision independently of scene play mode.

No scene or UI prefab change is required. `ArcaneGameLook` and its Bloom settings remain unchanged.

## Error Handling

- Missing serialized light reference logs a clear error and disables the controller instead of creating a runtime substitute.
- Missing `BattleManager` prevents state binding and logs the existing service lookup failure rather than silently showing the light.
- No fallback `Light2D`, `new GameObject()`, or `AddComponent()` path is introduced.

## Verification

### Automated and static

- `Active` resolves to visible; `Pending`, `Victory`, and `Defeat` resolve to hidden.
- Ally and enemy prefabs both contain the controller and serialized golden-light reference.
- The light SpriteRenderer sorts below the character.
- No new `static` member, `Destroy` call, `Light2D`, runtime `new GameObject()`, or runtime `AddComponent()` is introduced.
- Existing EditMode tests and Unity compilation continue to pass.

### Manual in Unity

1. Enter the game scene during preparation and verify no unit has a golden pool.
2. Start a wave and verify every existing ally and every spawned enemy shows the pool.
3. Watch moving units and verify the pool stays under their feet without covering their sprites or health bars.
4. Observe overlapping units and verify Bloom remains gold and does not clip into a large white patch.
5. Finish or lose the wave and verify lights disappear in the next preparation state or result state.
6. Start another wave to verify pooled enemies and persistent allies restore the correct light state.
7. Make a WebGL development build or profile the representative battle to confirm the Sprite-based effect introduces no real-time-light spike.

## Constraints

- Unity 6, URP 2D Renderer, PC WebGL.
- Scene/prefab placement and Inspector references remain the default initialization strategy.
- Existing `App.Get<T>()` calls are permitted.
- `[SerializeField]` names do not use underscores.
- Existing pooling and `SetActive` lifecycle remain unchanged.
- No top-level folder, file move, rename, package change, broad refactor, public API change, or global Bloom retuning.
