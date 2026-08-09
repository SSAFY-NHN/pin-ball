# Arcane Game UI Application Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the approved arcane top HUD and bottom-panel art to `02. Game.unity` while preserving all existing UI and gameplay behavior.

**Architecture:** An editor-only setup utility constructs and serializes the required scene hierarchy, assigns approved Sprite assets, and preserves existing functional components. `StatusPanel` receives the only runtime behavior change: it drives ten serialized wave nodes, nine connectors, and dynamic number labels from existing `BattleManager` events. Existing `WavePanel`, `BottomTabPanel`, `ShopPanel`, and `ItemPanel` remain behavior owners.

**Tech Stack:** Unity `6000.0.79f1`, C#, UGUI, TextMesh Pro, URP 2D Renderer, PC WebGL.

## Global Constraints

- Modify `Assets/01. Scenes/02. Game.unity`; do not modify `Assets/04. Prefabs/UI.prefab`.
- Pre-place UI in the scene and wire it through serialized Inspector references; do not create runtime fallback UI.
- Preserve existing wave, launch, shop, item, tooltip, and pinball-slide behavior.
- Use exactly ten wave nodes and nine connectors.
- Wave 5 is elite, wave 9 is pre-boss elite, and wave 10 is the `goblin_king` boss node.
- Keep numbers, costs, and button wording in TMP rather than baked into Sprite assets.
- Do not modify pinball, background, character, combat, economy, item, or wave rules.
- Do not install packages or introduce unrelated refactoring.

---

## File structure

- Modify `Assets/02. Scripts/03. UI/StatusPanel.cs`: validate and update dynamic HUD text, ten nodes, nine connectors, and wave-number labels.
- Create `Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs`: editor-only scene construction, Sprite assignment, serialized reference wiring, scene validation, and batch entry points.
- Create corresponding Unity `.meta` files through Unity import.
- Modify `Assets/01. Scenes/02. Game.unity`: serialized top HUD and bottom-panel hierarchy produced by the setup utility.
- Use existing assets under `Assets/03. Images/UI/ArcaneHud` and `Assets/03. Images/UI` without editing their pixels.

---

### Task 1: Dynamic ten-node wave progress

**Files:**
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs`

**Interfaces:**
- Consumes: `BattleManager.OnInitialized`, `BattleManager.OnWaveChanged`, `BattleManager.OnHpChanged`, `BattleManager.OnGoldChanged`.
- Produces: serialized fields `Image[] waveNodes`, `Image[] waveConnectors`, `TextMeshProUGUI[] waveNumberTexts`, and Sprite references for idle, current, complete, elite 05, elite 09, boss 10, idle connector, and complete connector.

- [ ] **Step 1: Add explicit validation requirements before changing rendering**

Add constants and a validator in `StatusPanel`:

```csharp
private const int WaveNodeCount = 10;
private const int WaveConnectorCount = WaveNodeCount - 1;

private bool ValidateHudReferences()
{
    bool valid = waveNodes != null && waveNodes.Length == WaveNodeCount;
    valid &= waveConnectors != null &&
             waveConnectors.Length == WaveConnectorCount;
    valid &= waveNumberTexts != null &&
             waveNumberTexts.Length == WaveNodeCount;
    if (!valid)
    {
        Debug.LogError("[StatusPanel] Wave HUD requires 10 nodes, 9 connectors, and 10 labels.");
    }
    return valid;
}
```

- [ ] **Step 2: Run Unity compilation before scene wiring and verify validation cannot yet succeed**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -logFile 'Temp\arcane-ui-compile-before.log'
```

Expected: C# compilation succeeds, but no completion claim is made because the new serialized arrays are not yet assigned in the scene.

- [ ] **Step 3: Implement minimal dynamic rendering**

Keep HP and gold event subscriptions. Remove the visible `Wave: n/10` format and implement:

```csharp
private void OnWaveChanged(int waveIndex)
{
    int currentWave = Mathf.Clamp(
        waveIndex + 1,
        1,
        Mathf.Max(1, _totalWaveCount));
    RefreshWaveProgress(currentWave);
}

private Sprite GetCurrentWaveSprite(int waveNumber)
{
    return waveNumber switch
    {
        5 => elite05NodeSprite,
        9 => elite09NodeSprite,
        10 => boss10NodeSprite,
        _ => currentNodeSprite,
    };
}
```

`RefreshWaveProgress` assigns completed Sprites before the current wave, the special/current Sprite at the current wave, idle Sprites after it, completed connectors before it, and idle connectors after it. Each label is set to `(index + 1).ToString()`. HP text becomes `$"{current}/{max}"`; gold text becomes the non-negative value without a baked label.

- [ ] **Step 4: Run Unity compilation**

Run the batch command from Step 2.

Expected: exit code `0` and no `CS` compiler errors in `Temp/arcane-ui-compile-before.log`.

- [ ] **Step 5: Commit the behavior change**

```powershell
git add -- 'Assets/02. Scripts/03. UI/StatusPanel.cs'
git commit -m "feat: drive ten-node wave HUD"
```

---

### Task 2: Editor-authored top HUD

**Files:**
- Create: `Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: existing `StatusPanel` and `WavePanel` components plus approved Sprites under `Assets/03. Images/UI/ArcaneHud`.
- Produces: `ArcaneGameUiSetup.Apply()` and `ArcaneGameUiSetup.Validate()` batch entry points and a serialized `ArcaneTopHud` hierarchy.

- [ ] **Step 1: Write the editor validation entry point first**

`Validate()` opens `Assets/01. Scenes/02. Game.unity` and fails with `EditorApplication.Exit(1)` unless all of the following are true:

```text
ArcaneTopHud exists
StatusPanel has 10 nodes, 9 connectors, and 10 number labels
WavePanel startButton and launchButton remain assigned
All node, connector, icon, and button Images have non-null Sprites
No runtime-generated UI marker or fallback object exists
```

- [ ] **Step 2: Run validation and verify it fails before construction**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -executeMethod ArcaneGameUiSetup.Validate -logFile 'Temp\arcane-ui-validate-before.log'
```

Expected: non-zero exit because `ArcaneTopHud` does not exist.

- [ ] **Step 3: Implement editor-only hierarchy construction**

`Apply()` must:

1. Open `02. Game.unity` with `EditorSceneManager.OpenScene`.
2. Find existing `StatusPanel` and `WavePanel`; throw a descriptive exception if missing.
3. Create scene children only in the editor using `Undo.AddComponent` and `new GameObject` inside the `Editor` assembly.
4. Build a top-safe-area root anchored `x=0..1`, `y=0.84..1`.
5. Add the composite frame Image as the background.
6. Add separate HP and gold icons plus TMP values.
7. Add ten node Images, nine connector Images, and ten centered TMP labels.
8. Reuse the existing WavePanel Buttons as the wave-start and launch controls; assign Sprite Swap Normal, Pressed, and Disabled assets.
9. Add settings artwork as a non-raycast Image, not a functional Button.
10. Use `SerializedObject` to assign every private `StatusPanel` array and Sprite field.
11. Mark the scene dirty and save it.

The editor utility must be idempotent: delete and rebuild only its own `ArcaneTopHud` generated root while leaving existing unrelated objects untouched.

- [ ] **Step 4: Run the setup method**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -executeMethod ArcaneGameUiSetup.Apply -logFile 'Temp\arcane-ui-apply-top.log'
```

Expected: exit code `0`, scene saved, `ArcaneTopHud` present.

- [ ] **Step 5: Run validation**

Run the validation command from Step 2.

Expected: exit code `0` and an explicit `Arcane UI validation passed` log line.

- [ ] **Step 6: Commit the top HUD**

```powershell
git add -- 'Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs' 'Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs.meta' 'Assets/01. Scenes/02. Game.unity'
git commit -m "feat: apply arcane top HUD"
```

---

### Task 3: Shared bottom item and shop frame

**Files:**
- Modify: `Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: existing `BottomTabPanel`, `ItemPanel`, `ShopPanel`, their serialized references, and approved bottom UI Sprites.
- Produces: serialized `ArcaneBottomPanel` visual root around the existing functional content.

- [ ] **Step 1: Extend validation before bottom construction**

Require:

```text
ArcaneBottomPanel exists
frame, content, left gem, and right gem Images have assigned Sprites
BottomTabPanel itemsButton, shopButton, itemsContent, and shopContent remain assigned
exactly one of itemsContent and shopContent is active after BottomTabPanel initialization
ShopPanel and ItemPanel components still exist under their original content roots
```

- [ ] **Step 2: Run validation and verify the new bottom requirement fails**

Run `ArcaneGameUiSetup.Validate`.

Expected: non-zero exit identifying the missing `ArcaneBottomPanel`.

- [ ] **Step 3: Extend Apply with bottom-panel construction**

Create a lower-left safe-area visual root using the approved outer frame, content surface, and separate gem Images. Reparent only the existing item and shop content roots into the shared content container while preserving their components, local order, Buttons, slots, and tooltips. Keep `BottomTabPanel` as the sole tab-state owner and reassign its four serialized references if reparenting changes object references.

Do not create additional item slots, shop slots, reroll controls, or tooltips.

- [ ] **Step 4: Run Apply and Validate**

Run `ArcaneGameUiSetup.Apply`, then `ArcaneGameUiSetup.Validate`.

Expected: both exit `0`; validation confirms the top and bottom UI requirements.

- [ ] **Step 5: Commit the bottom UI**

```powershell
git add -- 'Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs' 'Assets/01. Scenes/02. Game.unity'
git commit -m "feat: apply shared arcane bottom panel"
```

---

### Task 4: Final static and Unity verification

**Files:**
- Verify only: all files changed in Tasks 1–3.

**Interfaces:**
- Consumes: completed scene and scripts.
- Produces: evidence-backed verification report; no new feature code.

- [ ] **Step 1: Run full Unity batch compilation and UI validation**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -executeMethod ArcaneGameUiSetup.Validate -logFile 'Temp\arcane-ui-final.log'
```

Expected: exit `0`, no compiler errors, validation passed.

- [ ] **Step 2: Run repository static checks**

```powershell
rg -n "new GameObject|AddComponent" 'Assets/02. Scripts/03. UI' -g '*.cs' -g '!Editor/**'
git diff --check
git diff --name-only HEAD~3..HEAD
```

Expected: no new runtime UI creation, no whitespace errors, and no `Assets/04. Prefabs/UI.prefab` change.

- [ ] **Step 3: Verify scene serialization integrity**

Run a read-only script that parses scene document headers, verifies unique fileIDs, checks that every local `{fileID: ...}` reference resolves when required, and verifies all approved Sprite GUIDs referenced by the generated UI exist exactly once in `.meta` files.

Expected: zero duplicate fileIDs, zero duplicate GUIDs, and zero missing required UI references.

- [ ] **Step 4: Record Unity play-mode checks still required from the user**

Report these as manual checks unless play mode is explicitly run:

```text
Preparation layout at 1920x1080
HP and gold event updates
waves 1, 5, 9, and 10 visual states
launch cost and insufficient-gold red state
start and launch disabled states
item/shop tab switching
shop purchase and reroll
item/shop tooltips
pinball slide-out and combat layout
```

- [ ] **Step 5: Commit any verification-only editor validation corrections**

Only if validation code required a correction:

```powershell
git add -- 'Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs'
git commit -m "test: validate arcane game UI scene"
```
