# Gameplay Feedback Milestone Design

## Goal

Complete the current gameplay-feedback milestone by removing the ally ownership cap, enforcing a five-ally battle-entry cap, making allied deaths permanent, adding an explicit two-second wave-resolution phase, strengthening HP and gold feedback, and making the pinball board and launcher handle visibly interactive.

## Confirmed Scope

- Allow the player to own and place any number of allied units that fit in the existing preparation area.
- Allow pinball launches regardless of the current allied-unit count.
- Allow a wave to start only when the player owns between one and five allied units, inclusive.
- Permanently remove an allied unit when it dies in combat.
- Add `EWaveState.Resolving` as an explicit wave lifecycle state.
- Show an immediate wave-result banner and hold `Resolving` for two seconds before advancing.
- Apply wave gold rewards or player HP damage when resolution begins so the banner and resource feedback play together.
- Strengthen the existing gold and HP change animations.
- Preserve and regression-test the existing launch-cost UI.
- Reuse the existing pinball mask image and additive/Bloom rendering path to make the board and launcher handle glow.
- Make the handle react to idle, hover, drag strength, and a successful launch without instructional text.

## Explicitly Deferred

- Tutorial implementation is deferred to a separate future task.
- The golden light pool beneath battle units is deferred.
- Existing unit ground shadows do not require changes or verification in this task.
- No `Light2D`, external package, save-data format, or new top-level folder is introduced.

## Existing Behavior and Gaps

- `UnitManager` already exposes a five-unit wave-start rule, but it also exposes an obsolete pinball limit that blocks launches starting at seven units.
- `PinballManager` and `WavePanel` consume that obsolete launch rule.
- Allied deaths currently remove units only from the active roster. The owned roster retains them, and `ResolveWaveResult` restores them at full health for preparation.
- `BattleManager.Update` completes or fails a wave on the first frame where either roster reaches zero.
- `ResultPanel` handles only final victory and defeat; there is no intermediate result banner.
- `StatusPanel` gives HP and gold the same small shake and scale punch.
- `WavePanel` already displays `발사 {cost}G`, reacts to cost changes, and changes the color when gold is insufficient.
- The scene already contains the arcane board and launcher parts, and the project has a reusable mask-glow/additive rendering path.

## Architecture

### Wave Lifecycle

The authoritative lifecycle becomes:

`Pending -> Active -> Resolving -> Pending | Victory | Defeat`

`BattleManager` owns the resolution result and the two-second deadline. UI observes the result but never controls game progression. This prevents a missing or disabled panel from leaving the run stuck or advancing it early.

`Resolving` is not a preparation state. While it is active:

- preparation actions remain unavailable;
- pinball launches remain unavailable;
- another wave cannot start;
- another wipe cannot begin a second resolution;
- no wave index or final run state changes until the two-second deadline expires.

### Resolution Result

Use a small explicit result type with `Cleared` and `Failed` values rather than a boolean. `BattleManager` publishes one resolution-start event containing the result and current wave number. `WaveResultPanel` consumes this event to choose its text and presentation.

The event is presentational. Reward, damage, cleanup, and state transitions remain inside `BattleManager` and `UnitManager`.

### Permanent Allied Death

`UnitManager.NotifyUnitDied` treats allied death as ownership removal:

1. Remove the ally from active and owned rosters.
2. Remove its saved preparation position.
3. publish the deployed-count change once.
4. Refresh ally item modifiers against the surviving roster.
5. Return the object to the existing unit pool.

`ResolveWaveResult` restores only allies still present in the owned roster. A dead ally therefore cannot be resurrected at the end of a wave.

### Ownership and Battle Entry

Ally spawning and placement retain the current preparation-area grid behavior, but no arbitrary roster-count limit is applied. If the physical preparation area has no valid free position, the existing placement failure remains the only spawn rejection.

The gameplay rules are:

- `0` allies: pinball launch allowed when all existing preparation, ball, and gold requirements pass; wave start rejected.
- `1..5` allies: pinball launch allowed; wave start allowed when all other conditions pass.
- `6+` allies: pinball launch allowed; wave start rejected.

The obsolete `CanLaunchPinballWithAllyCount` rule and its runtime consumers are removed. The five-unit constant and wave-start rule remain the single roster boundary.

## Resolution Data Flow

### Cleared Wave

1. During `Active`, `BattleManager` observes `RemainingEnemyCount <= 0`.
2. Enemy depletion wins the tie if both teams reach zero on the same frame, preserving the existing condition order.
3. `BattleManager` changes state to `Resolving` and stores `Cleared` plus a deadline of current time + 2 seconds.
4. It awards the current wave's regular or final-clear gold immediately and emits the existing gold-changed event.
5. It emits the resolution-start event.
6. `WaveResultPanel` shows `웨이브 클리어`; `StatusPanel` plays the stronger gold animation.
7. At the deadline, `UnitManager.ResolveWaveResult` cleans enemies and restores surviving allies to preparation.
8. The run advances to the next `Pending` wave, or changes to final `Victory` when the cleared wave was the last wave.

### Failed Wave

1. During `Active`, `BattleManager` observes `RemainingAllyCount <= 0` after checking enemy depletion.
2. It changes state to `Resolving` and stores `Failed` plus a two-second deadline.
3. It calculates breach damage once, applies player damage immediately, emits the HP-changed event, and awards retry gold immediately only when the player survives.
4. It emits the resolution-start event.
5. `WaveResultPanel` shows `방어 실패`; `StatusPanel` plays the stronger HP animation and, when applicable, the gold animation.
6. At the deadline, `UnitManager.ResolveWaveResult` cleans remaining enemies. No dead allies are restored.
7. The state changes to `Pending` when player HP remains above zero, otherwise to final `Defeat`.

### Timing Authority

The manager deadline is authoritative and uses normal game time. The result panel's animation duration visually fits inside the same two-second window but does not determine when the run advances. The resolution state and stored outcome prevent rewards or damage from being applied more than once.

## UI Design

### Wave Result Banner

Add a scene-placed `WaveResultPanel` under the existing game UI hierarchy. It contains a centered banner, one result text, and a `CanvasGroup` for a short scale/fade animation.

- `Cleared`: `웨이브 클리어`, using the existing gold-accent visual language.
- `Failed`: `방어 실패`, using the existing danger/red visual language.
- The banner appears immediately when resolution starts.
- It remains readable during the two-second resolution window and hides before the next state becomes interactive.
- On the final wave, the existing `ResultPanel` opens only after the banner window ends and the state changes to `Victory` or `Defeat`.

The panel does not repeat reward or damage numbers. Those values remain in the established status UI, whose animations are strengthened for this milestone.

### HP and Gold Feedback

Replace the shared generic emphasis with resource-specific feedback while keeping the existing text elements.

- Gold change: stop and normalize the previous tween, flash gold, apply a larger scale punch, then restore the serialized base color and unit scale.
- HP change: stop and normalize the previous tween, flash red, apply a larger scale punch plus a stronger horizontal shake, then restore the serialized base color and unit scale.
- Initialization never plays a change animation.
- A new change that arrives during an animation safely resets the previous tween before starting another, preventing accumulated scale or a stuck flash color.
- Ally-count rejection continues to use its existing emphasis behavior and over-limit color.

### Launch Cost

Preserve the existing `WavePanel` behavior:

- display `발사 {CurrentLaunchCost}G`;
- update when the launch cost changes;
- use the unavailable color when the player cannot afford the launch;
- keep launch-button availability dependent on preparation state, a loaded/available ball, pinball state, and gold, but not allied-unit count.

## Board and Launcher Glow

### Rendering

Reuse the existing arcane mask image, additive shader/material path, HDR camera, and Bloom. Glow renderers are scene-placed and connected through Inspector references. Do not create `Light2D` components or per-frame materials.

### Board

The board has a low-intensity, steady mask glow during preparation. It should improve separation from the background without competing with the launcher handle or UI.

### Launcher Handle

The handle glow communicates interactivity without text:

- no loaded ball: inactive or minimum intensity;
- loaded and idle: clearly visible breathing glow;
- pointer hover: immediate intensity increase and subtle scale increase;
- dragging: intensity increases monotonically with normalized pull distance;
- successful launch: strongest short pulse before returning to the unloaded state;
- pointer exit or canceled drag: return to the correct loaded idle intensity and original scale.

`PinballLauncherController` remains responsible for input and physical handle motion. A focused serialized glow reference receives state changes. Missing glow references degrade only the visual feedback; dragging and launching must still work.

## Component Changes

### `Enum.cs`

- Add `Resolving` to `EWaveState`.
- Add a clear/failed wave-resolution result enum near the existing battle enums.

### `BattleManager`

- Detect wipe only in `Active`.
- Begin resolution once, apply its immediate economy/HP effect, publish the result, and store its deadline.
- Finish resolution at two seconds, clean the field, advance the wave when applicable, and choose `Pending`, `Victory`, or `Defeat`.
- Keep `CanUsePreparationActions` false in `Resolving` through the existing `Pending` check.
- Cancel pending resolution work naturally when destroyed.

### `BattleRunState`

- Continue to own the public state, wave index, and player HP.
- Accept `Resolving` through its existing state mutation boundary.
- Preserve current wave data until resolution finishes so reward, damage, and banner information refer to the completed wave.

### `UnitManager`

- Permanently release dead allied units.
- Keep owned-count events exact and non-duplicated.
- Restore only surviving owned allies.
- Retain the `1..5` wave-start boundary.
- Remove the obsolete pinball-count rule.

### `PinballManager` and `WavePanel`

- Remove roster-count checks from launch execution and launch-button state.
- Preserve every non-roster launch guard.
- Preserve launch cost display and affordability color.

### `WaveResultPanel`

- Subscribe to the resolution-start event.
- Render the correct result text and visual treatment.
- Run presentation-only scale/fade animation within the two-second window.
- Unsubscribe and kill active tweens on destruction.

### `StatusPanel`

- Use separate gold and HP feedback methods.
- Store and restore each text's base color and scale.
- Preserve the wave progress HUD and ally-count behavior.

### `PinballLauncherController`

- Add hover entry/exit feedback.
- Drive glow intensity from loaded state and pull ratio.
- Pulse on successful launch.
- Reset visual transform and glow state on disable or canceled input.

### `Game.unity`

- Place and wire the result banner.
- Place and wire board and handle glow renderers using the existing mask and additive material path.
- Connect the result panel, status UI, and launcher references through Inspector fields.
- Do not disturb collider positions, pinball physics, existing UI anchors outside the touched elements, or unrelated artwork.

## Error and Edge Handling

- Simultaneous team depletion resolves as `Cleared`, matching the existing enemy-first check.
- `Resolving` prevents repeated reward, damage, and result events.
- A missing current wave cannot grant rewards; it resolves to final defeat after the banner window while logging the existing invalid-data error path.
- Player HP reaching zero during failed resolution transitions to final defeat only after two seconds.
- Retry gold is granted only when the player survives the failed wave.
- Permanent death updates both active and owned rosters before any result restoration can run.
- A missing `WaveResultPanel` never blocks state progression.
- Missing glow references never block handle input or pinball launch.
- Re-entrant DOTween feedback normalizes color and scale before replay.
- The pre-existing untracked Excel lock file remains untouched.

## Testing and Verification

### EditMode Tests

- Wave start boundaries: `0 -> false`, `1 -> true`, `5 -> true`, `6+ -> false`.
- Pinball launch roster rule no longer exists in manager and UI decision paths; count does not affect an otherwise valid launch.
- An allied death removes the unit from active and owned rosters, removes its saved placement, emits one count event, and returns it to the pool.
- Wave cleanup restores surviving allies only.
- Active wipe enters `Resolving` immediately.
- Resolution does not finish before two seconds and finishes at or after the deadline.
- Clear reward, retry reward, and HP damage are each applied exactly once.
- Intermediate clear advances to the next `Pending` wave after the delay.
- Final clear changes to `Victory` after the delay.
- Survived failure returns to `Pending`; lethal failure changes to `Defeat` after the delay.
- Simultaneous wipe resolves as clear.
- Result banner chooses the correct copy and does not own the progression timer.
- Launch-cost UI remains wired and renders the current cost.
- Glow-state math preserves `unloaded < loaded idle < hover < full pull/launch` intensity ordering.

### Scene and Play Mode Checks

1. Spawn more than five allies and confirm additional pinball launches remain available when gold and ball requirements pass.
2. Confirm wave start is rejected at six or more allies and succeeds again after the roster returns to five.
3. Kill an ally, finish or fail the wave, and confirm the dead ally never returns.
4. Clear a wave and confirm the result banner and gold animation begin immediately, while the next wave waits two seconds.
5. Fail a wave and confirm the result banner and HP animation begin immediately, while the next state waits two seconds.
6. Confirm final victory and defeat panels appear only after the intermediate banner delay.
7. Confirm the board is separated from the background by a restrained glow.
8. Confirm the loaded handle breathes, reacts to hover, brightens while pulled, and pulses on launch with no text prompt.
9. Confirm collider behavior, pull motion, launch direction, and pinball physics are unchanged.

### Full Verification

- Run all Unity EditMode tests.
- Run `git diff --check` and inspect the final file scope.
- Run the project's configured WebGL build path without changing packages or build profiles.
- Record actual test/build outcomes in the project AI usage log during implementation.

## Implementation Order

1. Roster rules and permanent allied death.
2. Explicit `Resolving` state and delayed result lifecycle.
3. Result banner and stronger HP/gold feedback.
4. Remove roster dependency from pinball launch and preserve launch-cost UI.
5. Board and launcher-handle glow.
6. Focused tests, full EditMode suite, WebGL build, Play Mode checklist, and AI usage record.

Tutorial work is not part of this implementation order because it is explicitly deferred.
