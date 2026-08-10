# Ally Drag Lineage Highlight Design

## Goal

During the preparation phase, dragging an ally highlights other deployed allies
that share the same base job. This makes merge and progression relationships
visible without adding new art.

## Lineage Rule

- "Same race" means the same base job lineage.
- Resolve the base job through `TitleData.TryGetRootAllyJob`.
- The dragged ally itself is not highlighted.
- Allies outside the dragged ally's lineage remain unchanged.

## Visual Behavior

- Matching allies receive a looping purple-gold brightness pulse while dragging.
- A matching ally that is immediately merge-compatible receives the stronger
  version of the pulse.
- The effect uses the existing unit `SpriteRenderer`; no new art asset is needed.
- The original color and scale are restored exactly when highlighting ends.

## Lifecycle

- Start highlights after a valid preparation-phase drag begins.
- Stop highlights on every drag exit: valid placement, rejected placement,
  pointer-over-UI cancellation, merge, or object disable.
- Inactive, pooled, reserved, dead, and source allies are excluded.

## Structure

- `AllyUnit` reports drag start and drag end to `UnitManager`.
- `UnitManager` resolves lineage and selects eligible active allies.
- A small unit visual component owns pulse animation and restoration so drag
  logic does not manipulate renderer state frame by frame.
- Merge eligibility reuses the existing merge rules rather than duplicating
  progression logic where possible.

## Verification

- Editor tests cover root-lineage matching and candidate filtering.
- A static build verifies compilation.
- Manual Unity check: drag a base or promoted ally and confirm only allies from
  the same root job pulse, with immediate cleanup on every release path.

## Out of Scope

- New species data fields, shaders, particles, sounds, and combat-phase hints.
