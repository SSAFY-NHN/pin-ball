# Item Pruning Design

## Goal

Remove nine approved items from both the item data and their runtime implementation while preserving the three existing item categories and all retained item behavior. This phase intentionally leaves a 13-item interim pool; replacement items will be designed and implemented separately.

## Approved removal set

### Ball

- `PrecisionAimRail` / `precision_aim_rail`
- `WeightedCore` / `weighted_core`
- `ElasticCoating` / `elastic_coating`
- `RecoveryInsurance` / `recovery_insurance`

### Board

- `ReinforcedBumper` / `reinforced_bumper`
- `WidePocket` / `wide_pocket`
- `SafetyNet` / `safety_net`

### Battle

- `AttackManual` / `attack_manual`
- `DuplicationSeal` / `duplication_seal`

## Data design

Remove the nine rows from the `ItemTable(재민)` data block in `DataTable/ItemDataTable.xlsx` and compact the thirteen retained rows without changing the workbook's headers, column structure, formatting conventions, or second worksheet. Remove the matching nine objects from `Assets/Resources/Data/ItemData.json`.

Retained items keep their existing numeric `key` values. `EItem` will therefore use explicit numeric assignments and retain the approved gaps instead of reindexing surviving entries. This prevents an existing item from being interpreted as a different item and reserves the vacated keys for the later replacement-item design.

The three values of `EItemCategory` remain unchanged:

- `Ball = 0`
- `Board = 1`
- `Battle = 2`

Item icon assets are not deleted in this phase. The request covers item data and runtime implementation; keeping unused source art avoids an irreversible asset deletion before replacement icons are decided.

## Runtime design

### Shared enum

Remove the nine `EItem` members and assign every retained member its original numeric value explicitly.

### Pinball item behavior

`PinballManager` will stop subscribing to the seven removed Ball/Board items and will remove their state, switch cases, collision hooks, refund behavior, goal-width bonus, and wave-reset bookkeeping.

The related `Pinball` state will also be removed when no retained behavior consumes it:

- paid launch cost tracking used only by `RecoveryInsurance`;
- rescue state used only by `SafetyNet`;
- previous-velocity tracking and collision callback used only by `WeightedCore` and `ElasticCoating`.

Retained pinball items, launch-cost escalation, goal selection, focused-pocket sizing, split balls, gold rewards, charged-pin bonuses, overload spawning, and swap behavior remain unchanged.

### Battle item behavior

`UnitManager` will stop subscribing to `AttackManual` and `DuplicationSeal`. The duplication request after a goal spawn and the now-unused duplication helper will be removed.

`BattleUnitModifiers` will remove the unconditional attack multiplier and all duplication state and roll logic. Its retained responsibilities are:

- `BattleClock` attack-rate multiplier;
- `FieldArmor` maximum-health multiplier;
- `DiversityEmblem` attack and health bonus by distinct unit type.

`BarrierReinforcement` remains owned by `BattleManager` and is unaffected.

## Data flow after removal

1. `TitleData` loads only the thirteen retained JSON item records.
2. `ItemManager` constructs and offers only those retained items in the existing three-slot shop.
3. Managers subscribe only to retained `EItem` values.
4. Pinball and battle flows no longer carry state that existed solely for a removed item.
5. Vacant enum keys remain unused until replacement items are approved.

## Error handling and compatibility

- Do not reindex retained enum keys.
- Do not change the item data schema or category enum.
- Do not modify `ItemGroupTable(밸런스x, 컨셉 위주 작성)` because it is outside the active item-data block and its legacy numeric IDs are not used by the current runtime item loader.
- Do not delete icons or unrelated user changes.
- JSON and workbook item IDs must remain unique after compaction.
- The retained JSON keys must all resolve to defined `EItem` values.

## Verification

### Static checks

- Search production code and runtime JSON for all nine removed enum names and IDs; expect no matches.
- Confirm the workbook and runtime JSON each contain exactly thirteen active item records.
- Confirm the retained keys are `4, 5, 6, 7, 9, 11, 13, 14, 15, 17, 18, 20, 21`.
- Confirm category values remain `0`, `1`, and `2`.

### Automated checks

- Update `BattleUnitModifiersTests` so retained attack-rate, health, and diversity behavior remains covered while removed attack and duplication tests disappear.
- Run the focused Unity Edit Mode tests for item modifiers and pinball motion.
- Run a Unity script-compilation check or broader Edit Mode suite if available.

### Workbook verification

- Inspect the compacted item range for values and formulas.
- Scan for spreadsheet formula errors.
- Render both worksheets and visually confirm that the active item table remains legible and that the untouched legacy worksheet still renders correctly.

## Out of scope

- Designing or implementing replacement items.
- Rebalancing retained item values or costs.
- Changing shop behavior, category structure, item stacking, or rarity.
- Deleting item icon assets.
- Editing legacy item-group data.
