# Ally Preparation Placement Design

## Goal

Allow allies to be placed only in the right half of the battle map, place newly summoned allies into available grid slots instead of a vertical line, and restore every ally to its saved preparation position after each wave.

## Scope

- Expand the scene's battle-area reference to the map's full horizontal extent so the right map edge is usable.
- Restrict ally preparation placement to the right half of that full battle area.
- Keep collider padding at the outer map edges so an ally's body cannot leave the map.
- Automatically place newly summoned allies into free grid slots in the right half, scanning horizontally before moving to the next row.
- Save preparation positions per owned ally and restore those positions after a wave.
- Preserve the target position when two allies merge.
- Do not change combat movement bounds, wave rules, roster limits, public APIs unrelated to placement, or external packages.

## Current Cause

`Panel_BattleArea` currently ends at 70% of the canvas width, so `BattleAreaBounds` clamps allies before the visible right edge. `UnitSpawner` applies `-spawnIndex * 0.75` only to Y, which creates a vertical line. `UnitManager.RestoreAlliesForPreparation` recomputes that same formation instead of retaining the positions chosen during preparation.

## Design

### Battle-area bounds

`BattleAreaBounds` remains the single source of world-space map bounds. The scene `Panel_BattleArea` will cover the full intended battle-map width. The bounds component will expose ally-placement operations that use the horizontal midpoint as the left boundary and the existing world maximum as the right boundary.

Placement checks and clamping will apply collider padding independently at the midpoint and outer edges. This means an ally's center may approach the right edge only as far as its collider permits; the visible body remains inside the map while the previous artificial 70% limit is removed.

Combat movement continues to use the existing full-area `Clamp` behavior. Only preparation dragging and automatic ally placement use the right-half restriction.

### Saved preparation positions

`UnitManager` will own a mapping from each owned `AllyUnit` to its saved preparation position. This state belongs with the owned roster because it must survive combat death/deactivation and remain valid until that ally is released or merged.

- On successful spawn, select a valid grid slot and record it.
- On successful drag release, update the ally's saved position.
- On rejected drag or a merge that does not complete, keep the previous saved position.
- On ally release, remove its saved position.
- On merge, remove both input entries and save the target ally's position for the result.
- On wave resolution, restore every owned ally to its saved position, reactivate it, and reset its combat state and mana.

The position mapping is runtime-only prototype state and does not change save data.

### Automatic spawn grid

The automatic layout scans deterministic candidate positions inside the right-half placement area. Columns advance horizontally first; after the final column, scanning continues on the next row. Candidate spacing includes the unit's placement padding so spawned units remain inside bounds and do not overlap occupied saved slots.

The search considers the saved positions of all currently owned allies. If a preferred candidate is occupied, it advances to the next candidate. With the current six-unit maximum during pinball summoning, the grid only needs to guarantee enough positions for the existing roster limit; no generalized unbounded layout system will be added.

If no candidate is available because the configured area is too small, spawning fails visibly with a warning instead of placing a unit outside the map. The taken pooled object is returned without adding it to the owned roster.

### Drag flow

During preparation dragging, `AllyUnit` clamps the pointer to the right-half placement bounds. On release, `UnitManager.IsValidAllyPlacement` validates the same bounds. A successful non-merge placement notifies `UnitManager` to save the final position. This keeps preview clamping, final validation, and persisted position on one coordinate definition.

### Wave restoration

Wave resolution no longer resets ally spawn order or asks `UnitSpawner` for a vertical formation. `UnitManager` restores each ally from its saved preparation position. A defensive fallback may select a free grid slot only when a position entry is unexpectedly missing; it must not overwrite valid saved layouts.

## Component Changes

- `BattleAreaBounds`: expose full bounds and right-half ally placement contains/clamp helpers.
- `UnitSpawner`: stop applying vertical ally formation offsets; activate an ally at a position supplied by `UnitManager`.
- `UnitManager`: select grid slots, own saved positions, update positions after drag/merge, and restore them after waves.
- `AllyUnit`: use right-half drag clamping and report successful placements.
- `02. Game.unity`: extend the referenced battle-area rectangle to the intended full map width.
- EditMode tests: cover half-area calculations, right-edge clamping, horizontal-first grid selection, occupied-slot skipping, and saved-position restoration helpers where they can be tested without scene play mode.

## Verification

### Automated

- A point left of the horizontal midpoint is invalid for ally preparation placement.
- A point at the padded right boundary is valid; a point beyond it is clamped.
- Grid indices advance X before changing Y.
- Occupied grid candidates are skipped deterministically.
- Existing deployment-limit and battle tests continue to pass.

### Manual in Unity

1. Enter the preparation phase and drag an ally toward the left half; verify it cannot cross the midpoint.
2. Drag an ally to the far-right map edge; verify its body reaches the edge without leaving the map.
3. Summon allies repeatedly; verify they fill right-half grid slots horizontally before starting another row and never leave the map.
4. Arrange allies into a distinct non-grid pattern, start and finish a wave, and verify the exact preparation layout returns.
5. Merge two allies, finish a wave, and verify the result returns to the merge target's position.
6. Verify enemies and combat movement still use the full battle area.

## Constraints

- Unity 6, C#, PC WebGL.
- Scene placement and Inspector references remain the default initialization strategy.
- Existing pooling with `SetActive` remains in use.
- `[SerializeField]` names do not use underscores.
- No top-level folders, file moves, renames, package changes, broad refactors, or unrelated formatting.
