# Disabled Feature Code Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove code left by features intentionally disabled on `temp`, in strict function → variable → class → script order, without changing retained gameplay.

**Architecture:** Use reference-driven subtraction. Each pass removes one symbol category, recompiles, and commits before the next pass. Unity scene and GUID cleanup happens only when the associated class or script is removed.

**Tech Stack:** Unity 6, C#, Unity YAML scenes, NUnit EditMode tests, Git

**Spec:** `docs/designs/2026-08-22/2026-08-22-disabled-feature-code-cleanup-design.md`

## Global Constraints

- Work only on the `temp` branch.
- Preserve fixed 10-wave flow, retries, both defense lines, tactical reinforcement, ally purchasing, ally position dragging, and automatic pinball production.
- Preserve `BattleCameraController`; goal and battle impact shake still use it.
- Do not remove `PinballGoal`, `PinballGoalController`, or launcher runtime unless later evidence proves them unreachable; current scene/tests still reference them.
- Do not modify or stage `Assets/05. Animations/Rabbit/Rabbit1_Mage_Attack.anim` or `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset`.
- Do not add packages, folders, abstractions, or unrelated formatting.
- A symbol with uncertain Unity serialization or event usage remains in code and is recorded in the removal inventory.

---

### Task 1: Remove unused functions

**Files:**
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitPreparationController.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitCreationService.cs`
- Modify: `Assets/02. Scripts/Tutorial/TutorialManager.cs`
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Modify: `Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/AllyInteractionPolicyTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitCreationServiceTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitAttackEffectPlayerTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs`

**Interfaces:**
- Consumes: Existing position-drag flow ending in `UnitManager.SaveAllyPreparationPosition(AllyUnit)`.
- Produces: No callable merge/evolution path, no legacy tutorial subscriptions, no hidden launch-button setup path.

- [ ] **Step 1: Record baseline and verify preserved behavior tests exist**

Run:

```powershell
git status --short
rg -n "TryStartWave|TryApplyDefenseLineAttack|TryPurchaseAlly|TacticalReinforcement|SaveAllyPreparationPosition" "Assets/02. Scripts"
```

Expected: retained flows have production and test references; only the two excluded user files are dirty.

- [ ] **Step 2: Remove drag-to-merge entry functions while keeping drag placement**

Change `AllyUnit.OnMouseUp()` to validate placement and then call only:

```csharp
_unitManager.SaveAllyPreparationPosition(this);
```

Remove `SetMergeReserved(bool)`, `SetLineageHighlighted(bool)`, overlap target discovery, and calls to `BeginAllyDragHighlight`, `EndAllyDragHighlight`, `ShouldAttemptAllyMergeOnDrop`, and `TryMergeAllies`. Keep `OnMouseDown`, `OnMouseDrag`, `OnMouseUp`, placement validation, and position saving.

- [ ] **Step 3: Remove merge/evolution functions from managers and services**

From `UnitManager`, remove:

```text
ShouldAttemptAllyMergeOnDrop
BeginAllyDragHighlight
EndAllyDragHighlight
TryMergeAllies
ChooseEvolution
CompleteEvolution
ConsumeReservedInputs
```

From `UnitPreparationController`, remove:

```text
BeginDragHighlight
EndDragHighlight
TryBeginMerge
TryChooseEvolution
Complete
```

Remove corresponding method bodies from `UnitMergeService` and factory methods from `UnitMergeDecision`, leaving class/type deletion for Tasks 3–4.

- [ ] **Step 4: Remove legacy tutorial execution functions**

Remove `TutorialManager.Start`, all event handlers, display-step functions, completion coroutine, unsubscribe logic, and `OnDestroy`. Leave type and fields until Tasks 2–3. This is safe because scene component is already `m_Enabled: 0` and the tutorial depends on removed manual launch/merge behavior.

- [ ] **Step 5: Remove hidden manual-launch UI functions**

Remove `launchButton` and `launchCostText` handling from `WavePanel.Refresh()`. Remove their validation/layout code from `ArcaneGameUiSetup`; do not remove `PinballLauncherController`, `PinballLaunchState`, or goal handling in this cleanup.

- [ ] **Step 6: Update characterization tests**

Delete assertions that require merge/evolution wiring, including `UnitAttackEffectPlayerTests` checks for `evolutionGlowEffect`, `UnitCreationServiceTests` expectations derived from `MergeTier`, and obsolete merge-policy assertions. Keep or add assertions that purchased allies remain draggable and `SaveAllyPreparationPosition` remains reachable. Keep `BattleRunStateTests` negative reflection checks proving reward fields are absent.

- [ ] **Step 7: Verify function-pass references and compile**

Run:

```powershell
rg -n "TryMergeAllies|ChooseEvolution|CompleteEvolution|ConsumeReservedInputs|BeginAllyDragHighlight|EndAllyDragHighlight|OnAlliesMerged|OnEvolutionRequested" Assets -g "*.cs"
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
git diff --check
```

Expected: search returns only still-declared events/fields scheduled for Task 2 or empty class shells scheduled for Task 3; both projects build with 0 errors.

- [ ] **Step 8: Commit function pass**

```powershell
git add -- "Assets/02. Scripts"
git commit -m "refactor: remove disabled feature functions"
```

---

### Task 2: Remove unused variables and serialized data

**Files:**
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Battle/BattleDataTypes.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitPreparationController.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitCreationService.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: Function-free class shells from Task 1.
- Produces: No merge/evolution/tutorial/manual-launch UI fields or serialized values; defense-line Trigger and Kinematic body remain intact.

- [ ] **Step 1: Remove merge/evolution state variables**

Remove from `AllyUnit`: `_lineageHighlight`, `_isMergeReserved`, and merge-only initialization/reset logic. Remove from `UnitManager`: `OnEvolutionRequested`, `OnAlliesMerged`, and `evolutionGlowEffect`. Remove `_mergeService` from `UnitPreparationController` and its constructor initialization.

- [ ] **Step 2: Remove merge stat data**

Remove `BattleUnitModifier.MergeTier`. Remove both `MergeTier` multipliers from `UnitCreationService`. Remove all serialized `MergeTier: 0` entries from `02. Game.unity`.

- [ ] **Step 3: Remove dead tutorial and manual-launch UI fields**

Remove all remaining fields from the now-empty `TutorialManager`. Remove `launchButton` and `launchCostText` from `WavePanel`. Remove `PinballManager.OnGoalReached` only if its reference count is limited to the disabled tutorial; keep all goal reward/VFX processing.

- [ ] **Step 4: Disable unnecessary full kinematic contacts**

In `02. Game.unity`, change only the `AllyDefenseLine` and `EnemyDefenseLine` Rigidbody2D entries:

```yaml
m_UseFullKinematicContacts: 0
```

Keep `m_BodyType: 1`, `m_Simulated: 1`, `m_GravityScale: 0`, and trigger colliders unchanged.

- [ ] **Step 5: Verify variable pass and compile**

Run:

```powershell
rg -n "MergeTier|_isMergeReserved|_lineageHighlight|_mergeService|evolutionGlowEffect|OnAlliesMerged|OnEvolutionRequested|launchButton|launchCostText" Assets -g "*.cs" -g "*.unity"
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
git diff --check
```

Expected: no production or scene matches; build completes with 0 errors.

- [ ] **Step 6: Commit variable pass**

```powershell
git add -- "Assets/02. Scripts" "Assets/01. Scenes/02. Game.unity"
git commit -m "refactor: remove disabled feature state"
```

---

### Task 3: Remove unused classes and scene components

**Files:**
- Modify: `Assets/02. Scripts/Battle/Units/UnitMergeService.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitMergeDecision.cs`
- Modify: `Assets/02. Scripts/Battle/AllyDragLineageHighlight.cs`
- Modify: `Assets/02. Scripts/Battle/EvolutionGlowEffect.cs`
- Modify: `Assets/02. Scripts/03. UI/EvolutionPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/EvolutionChoiceView.cs`
- Modify: `Assets/02. Scripts/03. UI/GameLayoutController.cs`
- Modify: `Assets/02. Scripts/Tutorial/TutorialManager.cs`
- Modify: `Assets/02. Scripts/Tutorial/TutorialProgress.cs`
- Modify: `Assets/02. Scripts/Tutorial/TutorialUIController.cs`
- Modify: `Assets/02. Scripts/Tutorial/TutorialInteractionController.cs`
- Modify: `Assets/02. Scripts/Tutorial/TutorialGameRuleController.cs`
- Modify: `Assets/02. Scripts/Tutorial/TutorialFocusIndicator.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: Empty or unreferenced types produced by Tasks 1–2.
- Produces: No unused class declarations and no scene MonoBehaviour components pointing at those classes.

- [ ] **Step 1: Capture script GUIDs and prove exact scene ownership**

Run:

```powershell
$targets = @(
  "UnitMergeService", "UnitMergeDecision", "AllyDragLineageHighlight",
  "EvolutionGlowEffect", "EvolutionPanel", "EvolutionChoiceView",
  "GameLayoutController", "TutorialManager", "TutorialProgress",
  "TutorialUIController", "TutorialInteractionController",
  "TutorialGameRuleController", "TutorialFocusIndicator"
)
foreach ($target in $targets) {
  Get-ChildItem "Assets/02. Scripts" -Recurse -Filter "$target.cs.meta" |
    ForEach-Object { Select-String -Path $_.FullName -Pattern "^guid:" }
}
```

Expected: every target has one GUID; GUID searches identify only its own scene component/object or no serialized reference.

- [ ] **Step 2: Remove scene objects/components owned only by deleted classes**

Remove the inactive legacy tutorial component and tutorial-only overlay/focus objects, `EvolutionPanel`, `EvolutionGlowEffect`, and any associated serialized references from `02. Game.unity`. Remove a `GameLayoutController` component only if its captured GUID is present; current search shows no scene GUID reference.

- [ ] **Step 3: Remove class declarations but retain files**

Remove the complete declarations of all 13 target types listed in Step 1. Leave their `.cs` and `.meta` files present until Task 4. Do not remove `BattleCameraController`, `PinballGoal`, `PinballGoalController`, `PinballLauncherController`, or `DefenseLineTrigger`.

- [ ] **Step 4: Verify class pass**

Run:

```powershell
rg -n "class (UnitMergeService|UnitMergeDecision|AllyDragLineageHighlight|EvolutionGlowEffect|EvolutionPanel|EvolutionChoiceView|GameLayoutController|TutorialManager|TutorialProgress|TutorialUIController|TutorialInteractionController|TutorialGameRuleController|TutorialFocusIndicator)|enum UnitMergeDecisionType" "Assets/02. Scripts"
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
git diff --check
```

Expected: type search has no matches; build completes with 0 errors.

- [ ] **Step 5: Commit class pass**

```powershell
git add -- "Assets/02. Scripts" "Assets/01. Scenes/02. Game.unity"
git commit -m "refactor: remove disabled feature classes"
```

---

### Task 4: Remove unused scripts and finalize documentation

**Files:**
- Delete: The 13 `.cs` files listed in Task 3 and their `.meta` files
- Delete: `Assets/02. Scripts/Battle/Editor/UnitMergeServiceTests.cs`
- Delete: `Assets/02. Scripts/Battle/Editor/UnitMergeServiceTests.cs.meta`
- Modify: `docs/plans/2026-08-21/2026-08-21-temp-branch-removal-inventory.md`
- Create: `docs/ai-usage/2026-08-22/2026-08-22-disabled-feature-code-cleanup-ai-usage.md`

**Interfaces:**
- Consumes: Empty script assets and cleaned scene GUID references from Task 3.
- Produces: Repository with no disabled-feature script assets, no orphan GUID references, and an auditable removal record.

- [ ] **Step 1: Delete empty runtime scripts and dedicated merge test**

Delete each Task 3 `.cs` file with its matching `.meta`, plus `UnitMergeServiceTests.cs` and its `.meta`. Do not delete mixed-responsibility tests; edit them only where obsolete assertions were removed in Tasks 1–2.

- [ ] **Step 2: Check deleted GUIDs and Missing Script risk**

Rebuild the deleted script GUID list from the parent commit, then search every GUID:

```powershell
$deletedScriptNames = @(
  "UnitMergeService", "UnitMergeDecision", "AllyDragLineageHighlight",
  "EvolutionGlowEffect", "EvolutionPanel", "EvolutionChoiceView",
  "GameLayoutController", "TutorialManager", "TutorialProgress",
  "TutorialUIController", "TutorialInteractionController",
  "TutorialGameRuleController", "TutorialFocusIndicator",
  "UnitMergeServiceTests"
)
foreach ($name in $deletedScriptNames) {
  $metaPath = git ls-tree -r --name-only HEAD |
    Select-String "/$name.cs.meta$" |
    Select-Object -ExpandProperty Line
  $guidLine = git show "HEAD:$metaPath" | Select-String "^guid:"
  $guid = ($guidLine.Line -split ":", 2)[1].Trim()
  rg -n $guid Assets -g "*.unity" -g "*.prefab" -g "*.asset"
}
```

Expected: no matches. Also search scene YAML for empty MonoBehaviour script references and inspect every result before completion.

- [ ] **Step 3: Run final retained-feature reference gates**

Run:

```powershell
rg -n "TryStartWave|TryApplyDefenseLineAttack|DefenseLineTrigger|TryPurchaseAlly|TacticalReinforcement|SaveAllyPreparationPosition|BattleCameraController" Assets -g "*.cs" -g "*.unity"
rg -n "UnitMerge|MergeTier|EvolutionPanel|EvolutionGlowEffect|TutorialManager|GameLayoutController|launchButton|WaveClearGoldReward|RetryGoldReward|FinalClearGoldReward" Assets -g "*.cs" -g "*.unity" -g "*.prefab" -g "*.asset"
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
git diff --check
```

Expected: retained symbols remain referenced; removed-feature search returns only intentional negative tests or documentation; builds complete with 0 errors.

- [ ] **Step 4: Run Unity EditMode tests when licensing is available**

Run the project EditMode suite and specifically confirm:

```text
BattleRunStateTests
WaveResolutionTests
BattleDefenseLineControllerTests
DefenseLineBreachTests
DefenseLineSceneTests
UnitPurchaseControllerTests
TacticalReinforcementControllerTests
AllyPreparationPlacementTests
PinballAutoCycleControllerTests
```

Expected: all pass. If Unity reports a LicensingClient failure, record tests as not run; do not report them as passing.

- [ ] **Step 5: Update inventory and AI usage record**

Update the removal inventory with exact removed symbols/files, retained uncertain candidates, commit IDs, and verification results. Create the required AI usage record containing model/tool, request, proposal, actual modifications, user decisions, important instructions, tests, and limitations.

- [ ] **Step 6: Commit script and documentation pass**

```powershell
git add -- "Assets/02. Scripts" "Assets/01. Scenes/02. Game.unity" "docs/plans/2026-08-21/2026-08-21-temp-branch-removal-inventory.md" "docs/ai-usage/2026-08-22"
git commit -m "refactor: remove disabled feature scripts"
```

- [ ] **Step 7: Final worktree audit**

Run:

```powershell
git status --short
git log -5 --oneline
```

Expected: only the two pre-existing user-owned files remain dirty; four cleanup commits appear after the plan commit.
