# World Health Bar Design

## Goal

Display an always-visible world-space health bar above every allied and enemy battle unit. Use the supplied team-specific visual assets and show a delayed damage gauge after units take damage.

## Scope

- Apply health bars to both `AllyUnit.prefab` and `EnemyUnit.prefab`.
- Keep health bars visible during preparation and battle while the owning unit is active.
- Use the existing `UnitBase.CurrentHp`, `UnitBase.MaxHp`, and `UnitBase.HpRatio` values as the only health data source.
- Preserve the existing damage, healing, death, pooling, and battle-state behavior.
- Do not change public APIs, battle data formats, or project folder structure.
- Keep the existing `_stateLabel` debug display unchanged.

## Visual Assets

The health bar is composed in this back-to-front order:

1. Shared background: `world_hp_background.png`
2. Team-specific delayed damage gauge:
   - Ally: `world_hp_damage_delay_ally.png`
   - Enemy: `world_hp_damage_delay_enemy.png`
3. Team-specific current health gauge:
   - Ally: `world_hp_fill_ally.png`
   - Enemy: `world_hp_fill_enemy.png`
4. Team-specific frame:
   - Ally: `world_hp_frame_ally.png`
   - Enemy: `world_hp_frame_enemy.png`

The allied and enemy prefabs receive their own Inspector-assigned sprites. Runtime team checks and runtime sprite loading are not required.

## Architecture

Add one focused `WorldHealthBarController` component to each unit prefab. The component owns only health-bar presentation and references:

- The owning `UnitBase`
- Background `SpriteRenderer`
- Delayed damage `SpriteRenderer`
- Current health `SpriteRenderer`
- Frame `SpriteRenderer`

Each prefab contains a pre-placed child hierarchy for these renderers. This follows the project preference for scene or prefab placement with Inspector references instead of runtime object creation.

The health bar uses `SpriteRenderer` rather than a world-space Canvas. Existing battle units already render and move in world space through SpriteRenderers, so this avoids per-unit Canvases, camera assignment, and screen-to-world tracking.

## Gauge Behavior

The controller reads `UnitBase.HpRatio` while active.

- Current health decreases immediately when `HpRatio` decreases.
- The delayed damage gauge holds its previous value for a short Inspector-configurable delay, then moves toward current health at an Inspector-configurable speed.
- When health increases, current health and delayed damage both update immediately. Healing does not create a reverse damage trail.
- When the component is enabled, both gauges immediately synchronize to the current `HpRatio`. This prevents stale values when a pooled unit is reactivated.
- A dead or pooled unit needs no separate visibility rule because the existing unit GameObject deactivation hides its child health bar.

The fill sprites use their imported left-bottom pivot, so changing horizontal scale keeps the left edge anchored. Gauge ratios are clamped to the range from zero to one.

## Prefab Layout and Rendering

Both unit prefabs receive a child health-bar root positioned above the character sprite. The root and its children are sized in local space to compensate for the existing prefab root scale and to produce the same apparent bar size for allies and enemies.

Renderer order is fixed as:

`background < delayed damage < current health < frame`

All four layers render above the unit sprite and ground shadow. The health-bar root follows unit movement automatically because it is a prefab child.

## Error Handling

The controller requires explicit Inspector references. Missing references should produce a clear validation error in development rather than silently creating objects or searching the scene at runtime. No fallback resource loading is introduced.

## Verification

### Automated checks

- Gauge ratio conversion clamps values below zero and above one.
- Damage updates current health immediately while leaving the delayed gauge temporarily higher.
- After the delay, the delayed gauge approaches the current gauge.
- Healing synchronizes both gauges immediately.
- Re-enabling the controller synchronizes both gauges and clears an old delayed state.

### Unity play-mode checks

- Every active ally and enemy displays a health bar during preparation and battle.
- Allied units use only allied fill, delay, and frame sprites.
- Enemy units use only enemy fill, delay, and frame sprites.
- Damage and healing display the intended gauge behavior.
- Pooled and reactivated units return with a full, synchronized bar.
- Bars remain correctly positioned and layered during movement, attacks, camera movement, death, and reuse.
- Existing unit combat, dragging, placement, death, and pooling behavior remains unchanged.

## Files Expected to Change During Implementation

- New: `Assets/02. Scripts/Battle/WorldHealthBarController.cs`
- New: controller EditMode test file and Unity metadata files as required
- Modify: `Assets/04. Prefabs/AllyUnit.prefab`
- Modify: `Assets/04. Prefabs/EnemyUnit.prefab`

No unrelated assets or the currently modified Arcane pinball image files are part of this work.
