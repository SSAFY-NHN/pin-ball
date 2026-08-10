# Item Pruning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the nine approved obsolete items from runtime data, code, tests, and the source workbook while preserving all retained item behavior and stable numeric keys.

**Architecture:** Keep `ItemManager` and the three-category data schema unchanged. Prune each removed item's subscriptions and state from the existing behavior owners, preserve retained `EItem` numeric identities with explicit assignments, then compact only the active worksheet data block through `@oai/artifact-tool` and verify the workbook visually.

**Tech Stack:** Unity 6.0.79f1, C#, NUnit Edit Mode tests, JSON Resources data, Excel `.xlsx`, `@oai/artifact-tool` 2.8.6+

## Global Constraints

- Remove exactly `PrecisionAimRail`, `WeightedCore`, `ElasticCoating`, `RecoveryInsurance`, `ReinforcedBumper`, `WidePocket`, `SafetyNet`, `AttackManual`, and `DuplicationSeal`.
- Preserve `EItemCategory.Ball = 0`, `EItemCategory.Board = 1`, and `EItemCategory.Battle = 2`.
- Preserve retained item keys `4, 5, 6, 7, 9, 11, 13, 14, 15, 17, 18, 20, 21`; do not reindex them.
- Leave item icon assets and `ItemGroupTable(밸런스x, 컨셉 위주 작성)` unchanged.
- Do not modify unrelated working-tree changes in `02. Game.unity`, `BottomTabPanel.cs`, `TitleBloomProfile.asset`, `Assets/03. Images/UI/Portraits.meta`, or `Assets/03. Images/UI/Portraits/`.
- Do not design or implement replacement items in this plan.
- Use the bundled Node.js runtime and `@oai/artifact-tool`; do not install packages or use alternate spreadsheet libraries.

---

## File map

- `pin-ball/Assets/02. Scripts/00. Core/Enum.cs` — retained item enum values and stable numeric keys.
- `pin-ball/Assets/Resources/Data/ItemData.json` — runtime catalog loaded by `TitleData`.
- `pin-ball/Assets/02. Scripts/Pinball/PinballManager.cs` — Ball and Board item subscriptions and effects.
- `pin-ball/Assets/02. Scripts/Pinball/Pinball.cs` — per-ball state consumed by removed effects.
- `pin-ball/Assets/02. Scripts/Battle/UnitManager.cs` — Battle item subscriptions and duplication trigger.
- `pin-ball/Assets/02. Scripts/Battle/Units/BattleUnitModifiers.cs` — retained Battle item modifiers.
- `pin-ball/Assets/02. Scripts/Battle/Editor/BattleUnitModifiersTests.cs` — retained modifier behavior tests.
- `pin-ball/Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs` — active catalog count, IDs, categories, and stable-key regression tests.
- `DataTable/ItemDataTable.xlsx` — source item workbook; only `ItemTable(재민)` is compacted.
- `pin-ball/docs/2026-08-10-item-pruning-ai-usage.md` — project-required AI usage record.

---

### Task 1: Add removal regression tests

**Files:**
- Create: `pin-ball/Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs`
- Create: `pin-ball/Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs.meta` (Unity-generated)
- Create: `pin-ball/Assets/02. Scripts/Item/Editor.meta` (Unity-generated if the folder does not exist)
- Modify: `pin-ball/Assets/02. Scripts/Battle/Editor/BattleUnitModifiersTests.cs`

**Interfaces:**
- Consumes: `JsonUtilityHelper.FromJson<ItemData>(string)`, `EItem`, `EItemCategory`, and `BattleUnitModifiers.Apply(EItem, float, float, float)`.
- Produces: regression coverage for the thirteen retained catalog records and retained Battle modifiers.

- [ ] **Step 1: Create the failing runtime-catalog test**

Create `ItemCatalogTests.cs` with this content:

```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using UnityEngine;

public class ItemCatalogTests
{
    private static readonly IReadOnlyDictionary<string, EItem> ExpectedItems =
        new Dictionary<string, EItem>
        {
            ["golden_ball"] = EItem.GoldenBall,
            ["auto_ball_feeder"] = EItem.AutoBallFeeder,
            ["target_magnet"] = EItem.TargetMagnet,
            ["split_capsule"] = EItem.SplitCapsule,
            ["golden_bumper"] = EItem.GoldenBumper,
            ["focused_pocket"] = EItem.FocusedPocket,
            ["swap_lever"] = EItem.SwapLever,
            ["charged_pin"] = EItem.ChargedPin,
            ["overload_bumper"] = EItem.OverloadBumper,
            ["battle_clock"] = EItem.BattleClock,
            ["field_armor"] = EItem.FieldArmor,
            ["diversity_emblem"] = EItem.DiversityEmblem,
            ["barrier_reinforcement"] = EItem.BarrierReinforcement,
        };

    [Test]
    public void RuntimeCatalog_ContainsOnlyRetainedItemsWithStableKeys()
    {
        var asset = Resources.Load<TextAsset>("Data/ItemData");
        Assert.That(asset, Is.Not.Null);

        ItemData[] items = JsonUtilityHelper.FromJson<ItemData>(asset.text);
        Assert.That(items, Is.Not.Null);
        Assert.That(items.Length, Is.EqualTo(ExpectedItems.Count));
        Assert.That(items.Select(item => item.id), Is.Unique);

        var actual = items.ToDictionary(item => item.id, item => item);
        CollectionAssert.AreEquivalent(ExpectedItems.Keys, actual.Keys);

        foreach (var pair in ExpectedItems)
        {
            Assert.That(actual[pair.Key].key, Is.EqualTo((int)pair.Value), pair.Key);
            Assert.That(actual[pair.Key].type, Is.InRange(0, 2), pair.Key);
        }
    }

    [Test]
    public void RetainedEnums_PreserveKeysAndCategories()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)EItem.GoldenBall, Is.EqualTo(4));
            Assert.That((int)EItem.AutoBallFeeder, Is.EqualTo(5));
            Assert.That((int)EItem.TargetMagnet, Is.EqualTo(6));
            Assert.That((int)EItem.SplitCapsule, Is.EqualTo(7));
            Assert.That((int)EItem.GoldenBumper, Is.EqualTo(9));
            Assert.That((int)EItem.FocusedPocket, Is.EqualTo(11));
            Assert.That((int)EItem.SwapLever, Is.EqualTo(13));
            Assert.That((int)EItem.ChargedPin, Is.EqualTo(14));
            Assert.That((int)EItem.OverloadBumper, Is.EqualTo(15));
            Assert.That((int)EItem.BattleClock, Is.EqualTo(17));
            Assert.That((int)EItem.FieldArmor, Is.EqualTo(18));
            Assert.That((int)EItem.DiversityEmblem, Is.EqualTo(20));
            Assert.That((int)EItem.BarrierReinforcement, Is.EqualTo(21));
            Assert.That((int)EItemCategory.Ball, Is.Zero);
            Assert.That((int)EItemCategory.Board, Is.EqualTo(1));
            Assert.That((int)EItemCategory.Battle, Is.EqualTo(2));
        });
    }
}
#endif
```

- [ ] **Step 2: Narrow Battle modifier tests to retained behavior**

Replace `GetRosterSnapshot_AppliesBattleMultipliers` with:

```csharp
[Test]
public void GetRosterSnapshot_AppliesRetainedBattleMultipliers()
{
    var modifiers = new BattleUnitModifiers();
    modifiers.Apply(EItem.BattleClock, 0.12f, 0f, 0f);
    modifiers.Apply(EItem.FieldArmor, 0.15f, 0f, 0f);

    UnitModifierSnapshot snapshot = modifiers.GetRosterSnapshot(5);

    Assert.That(snapshot.AttackMultiplier, Is.EqualTo(1f));
    Assert.That(snapshot.AttackRateMultiplier, Is.EqualTo(1.12f));
    Assert.That(snapshot.HpMultiplier, Is.EqualTo(1.15f));
}
```

Delete the three `ShouldDuplicate_*` tests and their two helper methods. Keep `GetRosterSnapshot_CapsDiversityBonus` unchanged.

- [ ] **Step 3: Run the focused tests and confirm the catalog test fails for the old 22-item data**

Run:

```powershell
$ItemPruningUnity = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
& $ItemPruningUnity -batchmode -nographics -quit -projectPath "$PWD\pin-ball" -runTests -testPlatform EditMode -testFilter 'ItemCatalogTests|BattleUnitModifiersTests' -testResults "$PWD\pin-ball\TestResults-item-pruning-red.xml" -logFile "$PWD\pin-ball\TestLog-item-pruning-red.txt"
```

Expected: `RuntimeCatalog_ContainsOnlyRetainedItemsWithStableKeys` fails because the runtime catalog still contains 22 records; retained Battle modifier tests pass.

---

### Task 2: Remove runtime item data and behavior

**Files:**
- Modify: `pin-ball/Assets/02. Scripts/00. Core/Enum.cs`
- Modify: `pin-ball/Assets/Resources/Data/ItemData.json`
- Modify: `pin-ball/Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `pin-ball/Assets/02. Scripts/Pinball/Pinball.cs`
- Modify: `pin-ball/Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `pin-ball/Assets/02. Scripts/Battle/Units/BattleUnitModifiers.cs`
- Test: `pin-ball/Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs`
- Test: `pin-ball/Assets/02. Scripts/Battle/Editor/BattleUnitModifiersTests.cs`

**Interfaces:**
- Consumes: the retained-item expectations from Task 1.
- Produces: a thirteen-member sparse `EItem` enum, a thirteen-record runtime catalog, and managers with no removed-item subscriptions or state.

- [ ] **Step 1: Make retained enum keys explicit**

Replace `EItem` with:

```csharp
public enum EItem
{
    GoldenBall = 4,
    AutoBallFeeder = 5,
    TargetMagnet = 6,
    SplitCapsule = 7,
    GoldenBumper = 9,
    FocusedPocket = 11,
    SwapLever = 13,
    ChargedPin = 14,
    OverloadBumper = 15,
    BattleClock = 17,
    FieldArmor = 18,
    DiversityEmblem = 20,
    BarrierReinforcement = 21,
}
```

- [ ] **Step 2: Prune the runtime JSON catalog**

Remove the objects whose `id` is one of:

```text
precision_aim_rail
weighted_core
elastic_coating
recovery_insurance
reinforced_bumper
wide_pocket
safety_net
attack_manual
duplication_seal
```

Do not modify any retained object's values, cost, name, description, type, or key. Parse the result with PowerShell:

```powershell
$ItemPruningJson = Get-Content -Raw -Encoding UTF8 -LiteralPath 'pin-ball\Assets\Resources\Data\ItemData.json' | ConvertFrom-Json
$ItemPruningJson.Count
```

Expected: `13`.

- [ ] **Step 3: Remove obsolete PinballManager subscriptions, fields, and handlers**

Set `SupportedItems` to exactly:

```csharp
private static readonly EItem[] SupportedItems =
{
    EItem.GoldenBall,
    EItem.AutoBallFeeder,
    EItem.TargetMagnet,
    EItem.SplitCapsule,
    EItem.GoldenBumper,
    EItem.FocusedPocket,
    EItem.SwapLever,
    EItem.ChargedPin,
    EItem.OverloadBumper
};
```

Remove the fields owned only by the deleted effects:

```text
_precisionSpeedMultiplier
_precisionRangeBonus
_horizontalChangeReduction
_collisionRetentionBonus
_maxCollisionRetention
_recoveryRefundRatio
_bumperForceBonus
_widePocketBonus
_safetyNetCount
_remainingSafetyNetCount
```

Reduce collision routing to the retained effects:

```csharp
internal void OnBallHit(Pinball ball, EPinballObstacle obstacle)
{
    if (ball == null) return;

    if (obstacle == EPinballObstacle.SmallPin)
    {
        ball.SmallPinHitCount++;
        ApplyGoldenBall(ball);
        return;
    }

    ball.BigBumperHitCount++;
    ApplyGoldenBumper(ball);
    ApplySplitCapsule(ball);
}
```

Delete `ApplyCollisionRetention`, `ApplyWeightedCore`, and `ApplyReinforcedBumper`. Replace the missed-ball path with:

```csharp
public void OnMissedBall(Pinball ball)
{
    if (ball == null) return;
    ReleaseBall(ball);
}
```

Remove the deleted-item cases from `OnItemEvent`. Start goal sizing at the unchanged base width:

```csharp
var multiplier = 1f;
if (_focusedPocketBonus > 0f)
{
    multiplier += i == _selectedGoalIndex
        ? _focusedPocketBonus
        : -_otherPocketPenalty;
}
```

Remove safety-net resets from `OnBattleStateChanged`.

- [ ] **Step 4: Remove obsolete per-ball state**

In `Pinball`, remove `PaidLaunchCost`, `WasRescued`, and `PreviousVelocity`, remove the call to `ApplyCollisionRetention`, and simplify the activation signatures to:

```csharp
internal void Activate(
    Vector2 worldPosition,
    Vector2 launchDirection,
    bool isClone)
```

```csharp
internal void LaunchLoaded(Vector2 launchVelocity)
```

```csharp
private void ResetRunState(bool isClone)
```

Update all calls in `PinballManager`: normal launches pass only velocity, clones pass `(position, direction, true)`, and loaded balls call `ResetRunState(false)`. Keep hit counters, clone/split flags, gold caps, target-magnet uses, and overload uses unchanged.

- [ ] **Step 5: Remove AttackManual and DuplicationSeal battle behavior**

In `UnitManager`, remove both subscriptions and unsubscriptions, delete `TryDuplicateAlly`, and remove `_unitManager.TryDuplicateAlly(unitData)` from `PinballManager.OnGoalBall`.

Reduce `BattleUnitModifiers` to:

```csharp
public sealed class BattleUnitModifiers
{
    private float _attackRateMultiplier = 1f;
    private float _hpMultiplier = 1f;
    private float _diversityBonusPerType;
    private float _diversityMaxBonus;

    public void Apply(EItem key, float value1, float value2, float value3)
    {
        switch (key)
        {
            case EItem.BattleClock:
                _attackRateMultiplier = 1f + value1;
                break;
            case EItem.FieldArmor:
                _hpMultiplier = 1f + value1;
                break;
            case EItem.DiversityEmblem:
                _diversityBonusPerType = value1;
                _diversityMaxBonus = value2;
                break;
        }
    }

    public UnitModifierSnapshot GetRosterSnapshot(int distinctUnitTypeCount)
    {
        float diversityBonus = Mathf.Min(
            _diversityMaxBonus,
            Mathf.Max(0, distinctUnitTypeCount) * _diversityBonusPerType);
        return new UnitModifierSnapshot(
            1f + diversityBonus,
            _attackRateMultiplier,
            _hpMultiplier + diversityBonus);
    }
}
```

- [ ] **Step 6: Run focused Edit Mode tests**

Run:

```powershell
$ItemPruningUnity = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
& $ItemPruningUnity -batchmode -nographics -quit -projectPath "$PWD\pin-ball" -runTests -testPlatform EditMode -testFilter 'ItemCatalogTests|BattleUnitModifiersTests|PinballMotionTests' -testResults "$PWD\pin-ball\TestResults-item-pruning.xml" -logFile "$PWD\pin-ball\TestLog-item-pruning.txt"
```

Expected: process exits `0`; all selected tests pass with no C# compilation errors.

- [ ] **Step 7: Commit runtime pruning**

```powershell
git add -- 'pin-ball/Assets/02. Scripts/00. Core/Enum.cs' 'pin-ball/Assets/Resources/Data/ItemData.json' 'pin-ball/Assets/02. Scripts/Pinball/PinballManager.cs' 'pin-ball/Assets/02. Scripts/Pinball/Pinball.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/BattleUnitModifiers.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleUnitModifiersTests.cs' 'pin-ball/Assets/02. Scripts/Item/Editor.meta' 'pin-ball/Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs' 'pin-ball/Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs.meta'
git commit -m 'refactor: remove obsolete item behaviors'
```

---

### Task 3: Prune and verify the source workbook

**Files:**
- Modify: `DataTable/ItemDataTable.xlsx`
- Create temporarily: `C:/Users/SSAFY/.codex/visualizations/2026/08/10/019fea21-73e8-7a61-9ad6-697122563298/prune_items.mjs`

**Interfaces:**
- Consumes: source range `ItemTable(재민)!B11:K32` and the approved removed IDs.
- Produces: compacted retained range `ItemTable(재민)!B11:K23`, blank reserved rows `B24:K32`, and an unchanged second worksheet.

- [ ] **Step 1: Load bundled spreadsheet dependencies and prepare the local module junction**

Use `codex_app__load_workspace_dependencies`. Confirm the bundled Node.js executable and `node_modules` path. Ensure this existing junction points to the loader-provided modules:

```powershell
Get-Item -LiteralPath 'C:\Users\SSAFY\.codex\visualizations\2026\08\10\019fea21-73e8-7a61-9ad6-697122563298\node_modules'
```

Expected: a Windows junction targeting the bundled dependency directory; do not modify the target directory.

- [ ] **Step 2: Create the artifact-tool pruning script**

Create `prune_items.mjs` with:

```javascript
import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const sourcePath = "C:/Users/SSAFY/Documents/GitHub/pin-ball/DataTable/ItemDataTable.xlsx";
const workDir = "C:/Users/SSAFY/.codex/visualizations/2026/08/10/019fea21-73e8-7a61-9ad6-697122563298/item-pruning";
const stagedPath = `${workDir}/ItemDataTable.xlsx`;
const removedIds = new Set([
  "precision_aim_rail",
  "weighted_core",
  "elastic_coating",
  "recovery_insurance",
  "reinforced_bumper",
  "wide_pocket",
  "safety_net",
  "attack_manual",
  "duplication_seal",
]);
const retainedKeys = [4, 5, 6, 7, 9, 11, 13, 14, 15, 17, 18, 20, 21];

await fs.mkdir(workDir, { recursive: true });
const input = await FileBlob.load(sourcePath);
const workbook = await SpreadsheetFile.importXlsx(input);
const itemSheet = workbook.worksheets.getItem("ItemTable(재민)");
const sourceRows = itemSheet.getRange("B11:K32").values;
const retainedRows = sourceRows.filter(row => {
  const id = typeof row[1] === "string" ? row[1] : "";
  return id && !removedIds.has(id);
});

if (retainedRows.length !== 13) {
  throw new Error(`Expected 13 retained rows, found ${retainedRows.length}`);
}
if (retainedRows.some((row, index) => row[2] !== retainedKeys[index])) {
  throw new Error("Retained item keys or row order changed unexpectedly");
}

itemSheet.getRange("B11:K23").values = retainedRows;
itemSheet.getRange("B24:K32").clear({ applyTo: "contents" });

const check = await workbook.inspect({
  kind: "table",
  sheetId: "ItemTable(재민)",
  range: "B7:K32",
  include: "values,formulas",
  tableMaxRows: 30,
  tableMaxCols: 10,
  maxChars: 30000,
});
console.log(check.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "final formula error scan",
});
console.log(errors.ndjson);

const renders = [
  ["ItemTable(재민)", "A1:L33", "items.png"],
  ["ItemGroupTable(밸런스x, 컨셉 위주 작성)", "A1:F40", "groups-top.png"],
  ["ItemGroupTable(밸런스x, 컨셉 위주 작성)", "A680:F692", "groups-bottom.png"],
];
for (const [sheetName, range, fileName] of renders) {
  const preview = await workbook.render({ sheetName, range, scale: 1.5, format: "png" });
  await fs.writeFile(`${workDir}/${fileName}`, new Uint8Array(await preview.arrayBuffer()));
}

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(stagedPath);
```

- [ ] **Step 3: Run the script and inspect all rendered images**

Run:

```powershell
& 'C:\Users\SSAFY\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' 'C:\Users\SSAFY\.codex\visualizations\2026\08\10\019fea21-73e8-7a61-9ad6-697122563298\prune_items.mjs'
```

Expected: the inspected table shows thirteen retained rows in `B11:K23`, `B24:K32` is blank, and the formula-error search reports zero matches. Use `view_image` on `items.png`, `groups-top.png`, and `groups-bottom.png`; verify readable text, intact headers, unchanged legacy-sheet values, and no clipping introduced by the edit.

- [ ] **Step 4: Replace the repository workbook with the verified export**

Run:

```powershell
Copy-Item -LiteralPath 'C:\Users\SSAFY\.codex\visualizations\2026\08\10\019fea21-73e8-7a61-9ad6-697122563298\item-pruning\ItemDataTable.xlsx' -Destination 'C:\Users\SSAFY\Documents\GitHub\pin-ball\DataTable\ItemDataTable.xlsx' -Force
```

Expected: only the tracked source workbook is replaced; the verified staging file remains outside the repository.

- [ ] **Step 5: Re-import the repository workbook and confirm the saved result**

Rerun the unchanged script. Its `sourcePath` already points to the repository workbook. Confirm the second run remains idempotent: thirteen retained rows, the same stable keys, no formula errors, and visually identical renders.

- [ ] **Step 6: Commit workbook pruning**

```powershell
git add -- 'DataTable/ItemDataTable.xlsx'
git commit -m 'data: prune obsolete item catalog'
```

---

### Task 4: Run final verification and record AI usage

**Files:**
- Create: `pin-ball/docs/2026-08-10-item-pruning-ai-usage.md`
- Verify: all files modified in Tasks 1–3.

**Interfaces:**
- Consumes: the pruned runtime catalog, behavior code, tests, and workbook.
- Produces: final verification evidence and the project-required AI usage record.

- [ ] **Step 1: Confirm removed symbols and IDs are absent from production code and runtime data**

Run:

```powershell
rg -n -g '*.cs' -g 'ItemData.json' 'PrecisionAimRail|WeightedCore|ElasticCoating|RecoveryInsurance|ReinforcedBumper|WidePocket|SafetyNet|AttackManual|DuplicationSeal|precision_aim_rail|weighted_core|elastic_coating|recovery_insurance|reinforced_bumper|wide_pocket|safety_net|attack_manual|duplication_seal' 'pin-ball\Assets\02. Scripts' 'pin-ball\Assets\Resources\Data\ItemData.json'
```

Expected: exit code `1` with no matches. Icon assets and historical design/plan documents are intentionally outside this search.

- [ ] **Step 2: Run the final focused Unity suite**

Run:

```powershell
$ItemPruningUnity = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
& $ItemPruningUnity -batchmode -nographics -quit -projectPath "$PWD\pin-ball" -runTests -testPlatform EditMode -testFilter 'ItemCatalogTests|BattleUnitModifiersTests|PinballMotionTests|BattleEconomyTests' -testResults "$PWD\pin-ball\TestResults-item-pruning-final.xml" -logFile "$PWD\pin-ball\TestLog-item-pruning-final.txt"
```

Expected: process exits `0`; all selected tests pass and Unity reports no compilation errors.

- [ ] **Step 3: Check repository scope and formatting**

Run:

```powershell
git diff --check HEAD~2..HEAD
git status --short
```

Expected: no whitespace errors. Existing unrelated user changes remain present and unmodified; item-pruning changes are limited to the files listed by this plan.

- [ ] **Step 4: Create the AI usage record with observed results**

Create `pin-ball/docs/2026-08-10-item-pruning-ai-usage.md` with this structure and replace the test-result sentence only if the observed command differs:

```markdown
# 아이템 9종 제거 AI 활용 기록

- 사용 도구/모델: OpenAI Codex
- 사용자 요청: 현재 게임에 불필요한 아이템 9종을 데이터와 구현 코드에서 제거
- AI 제안: Ball 4종, Board 3종, Battle 2종을 제거하고 유지 아이템의 숫자 키와 세 가지 타입을 보존
- AI 수정 영역: ItemDataTable.xlsx 활성 아이템 표, ItemData.json, EItem, PinballManager, Pinball, UnitManager, BattleUnitModifiers 및 관련 Edit Mode 테스트
- 사용자 결정 영역: 제거할 9종 승인, 적극 교체 방향, Ball/Board/Battle 타입 유지, 신규 아이템은 별도 기획
- 중요 지시: 아이콘과 레거시 ItemGroupTable은 삭제하지 않고, 관련 없는 작업 트리 변경을 보존
- 검증 결과: ItemCatalogTests, BattleUnitModifiersTests, PinballMotionTests, BattleEconomyTests 통과. 제거 대상 심볼과 런타임 ID 검색 결과 0건. 워크북 두 시트 렌더 검증 완료.
```

- [ ] **Step 5: Commit the AI usage record**

```powershell
git add -- 'pin-ball/docs/2026-08-10-item-pruning-ai-usage.md'
git commit -m 'docs: record item pruning AI usage'
```

- [ ] **Step 6: Report the manual play check**

Manual verification for the user:

1. Open the Game scene and enter the first preparation phase.
2. Repeatedly use the free shop reroll and confirm none of the nine removed items appears.
3. Purchase one retained item from each available category over multiple rerolls.
4. Launch balls and confirm retained gold, magnet, split, focused-pocket, swap, charged-pin, and overload effects still behave as before.
5. Start a wave and confirm retained attack-speed, health, diversity, and barrier effects still apply.

Expected: no missing-enum serialization errors, no removed shop entries, and no regression in retained effects.
