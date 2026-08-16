# Ally Preparation Placement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restrict ally preparation placement to the map's right half, fill new summons into a horizontal-first grid, and restore the saved preparation layout after every wave.

**Architecture:** `BattleAreaBounds` remains the world-boundary authority and adds right-half placement/grid calculations without changing full-area combat clamping. `UnitManager` owns runtime preparation positions for the owned roster, while `AllyUnit` only forwards successful drag results and `UnitSpawner` stops imposing a vertical formation.

**Tech Stack:** Unity 6, C#, Unity UI `RectTransform`, 2D physics, NUnit EditMode tests

## Global Constraints

- Ally preparation placement uses only the right half of the full battle map.
- Collider padding keeps the complete ally body inside the midpoint and outer map edges.
- Automatic placement scans X first, then moves down one row.
- Preparation positions are runtime-only and are not added to save data.
- Wave completion restores saved positions; it never recomputes the old vertical formation.
- Merge results inherit the merge target position.
- Combat movement and enemies continue to use the full battle-area bounds.
- Existing roster limits, pooling, scene references, and public behavior outside placement remain unchanged.
- `[SerializeField]` field names do not use underscores.
- No external packages, top-level folders, file moves, broad refactors, or unrelated formatting.

## File Map

- Modify: `Assets/02. Scripts/Battle/BattleAreaBounds.cs` — right-half contains/clamp and deterministic grid calculation.
- Create: `Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs` — EditMode boundary and grid tests.
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs` — remove ally spawn-order vertical offsets.
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs` — saved preparation-position lifecycle, slot selection, restoration, and merge inheritance.
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs` — right-half drag clamp and successful-drop notification.
- Modify: `Assets/01. Scenes/02. Game.unity` — extend `Panel_BattleArea` to the full intended horizontal map extent.
- Modify: `.github/ai-use-log.md` — project-required factual AI work record.

---

### Task 1: Right-half placement bounds and grid math

**Files:**
- Modify: `Assets/02. Scripts/Battle/BattleAreaBounds.cs`
- Create: `Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs`

**Interfaces:**
- Produces: `bool ContainsAllyPlacement(Vector3 worldPosition, float padding)`
- Produces: `Vector3 ClampAllyPlacement(Vector3 worldPosition, float padding)`
- Produces: `bool TryGetAllyGridPosition(int gridIndex, float padding, out Vector3 position)`
- Preserves: `Contains` and `Clamp` as full-area combat operations.

- [ ] **Step 1: Write failing right-half boundary tests**

Create `AllyPreparationPlacementTests.cs`. Invoke private static calculation helpers through reflection, matching the existing `BattleCameraControllerTests` pattern.

```csharp
#if UNITY_EDITOR
using System.Reflection;

using NUnit.Framework;
using UnityEngine;

public class AllyPreparationPlacementTests
{
    private static MethodInfo GetMethod(string name)
    {
        return typeof(BattleAreaBounds).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
    }

    [TestCase(5.49f, false)]
    [TestCase(5.5f, true)]
    [TestCase(9.5f, true)]
    [TestCase(9.51f, false)]
    public void ContainsAllyPlacement_UsesPaddedRightHalf(
        float x,
        bool expected)
    {
        MethodInfo method = GetMethod("ContainsAllyPlacement");
        Assert.That(method, Is.Not.Null);

        var result = (bool)method.Invoke(
            null,
            new object[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 8f),
                new Vector3(x, 4f, 0f),
                0.5f
            });

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ClampAllyPlacement_ReachesPaddedRightEdge()
    {
        MethodInfo method = GetMethod("ClampAllyPlacement");
        Assert.That(method, Is.Not.Null);

        var result = (Vector3)method.Invoke(
            null,
            new object[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 8f),
                new Vector3(20f, 4f, 3f),
                0.5f
            });

        Assert.That(result, Is.EqualTo(new Vector3(9.5f, 4f, 3f)));
    }
}
#endif
```

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter 'AllyPreparationPlacementTests' -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\ally-placement-results.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\ally-placement.log'
```

Expected: failure because the calculation helpers do not exist.

- [ ] **Step 3: Add the minimal right-half calculation helpers**

Add private static overloads used by the public instance methods. The midpoint padding applies inward from the midpoint, so the valid center range starts at `midpoint + padding`.

```csharp
private const float AllyGridGap = 0.15f;

private static bool ContainsAllyPlacement(
    Vector2 worldMin,
    Vector2 worldMax,
    Vector3 worldPosition,
    float padding)
{
    float safePadding = Mathf.Max(0f, padding);
    float midpoint = (worldMin.x + worldMax.x) * 0.5f;
    return worldPosition.x >= midpoint + safePadding &&
           worldPosition.x <= worldMax.x - safePadding &&
           worldPosition.y >= worldMin.y + safePadding &&
           worldPosition.y <= worldMax.y - safePadding;
}

private static Vector3 ClampAllyPlacement(
    Vector2 worldMin,
    Vector2 worldMax,
    Vector3 worldPosition,
    float padding)
{
    float safePadding = Mathf.Max(0f, padding);
    float midpoint = (worldMin.x + worldMax.x) * 0.5f;
    worldPosition.x = Mathf.Clamp(
        worldPosition.x,
        midpoint + safePadding,
        worldMax.x - safePadding);
    worldPosition.y = Mathf.Clamp(
        worldPosition.y,
        worldMin.y + safePadding,
        worldMax.y - safePadding);
    return worldPosition;
}
```

The instance methods return `false`/the input when `IsValid` is false, otherwise delegate to these helpers.

- [ ] **Step 4: Add failing horizontal-first grid tests**

Append tests that reflect `TryGetAllyGridPosition(Vector2, Vector2, int, float, out Vector3)` using a 10x8 test area and `0.5` padding. Assert that index 1 increases X while keeping Y, and that index `columnCount` resets X and decreases Y. Add an out-of-range case whose row falls below `worldMin.y + padding` and assert `false`.

```csharp
Assert.That(first.y, Is.EqualTo(second.y));
Assert.That(second.x, Is.GreaterThan(first.x));
Assert.That(nextRow.x, Is.EqualTo(first.x));
Assert.That(nextRow.y, Is.LessThan(first.y));
```

- [ ] **Step 5: Implement deterministic grid calculation**

Use `step = max(2 * padding + AllyGridGap, AllyGridGap)`, start at the padded midpoint/top corner, calculate `columnCount`, then use `%` and `/` so X advances first. Return `false` when the calculated Y is below the padded bottom. Preserve the requested Z value as `0f` for preparation positions.

- [ ] **Step 6: Run focused tests and verify pass**

Run Step 2 again. Expected: exit code `0`; boundary, right-edge, grid-order, and capacity cases pass.

- [ ] **Step 7: Commit boundary math and tests**

```powershell
git add -- 'Assets/02. Scripts/Battle/BattleAreaBounds.cs' 'Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs' 'Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs.meta'
git commit -m "feat: add right-half ally placement bounds"
```

---

### Task 2: Saved preparation positions and non-vertical spawning

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`

**Interfaces:**
- Consumes: Task 1 `TryGetAllyGridPosition` and `ContainsAllyPlacement`.
- Produces: `void SaveAllyPreparationPosition(AllyUnit ally)`.
- Owns: `Dictionary<AllyUnit, Vector3> _allyPreparationPositions`.

- [ ] **Step 1: Add failing slot-occupancy tests**

Add a private static `IsGridPositionOccupied(Vector3 candidate, IEnumerable<Vector3> occupiedPositions, float minimumDistance)` helper to the intended API and test it through reflection. Cover an exact match, a point inside the minimum distance, and a point outside it.

```csharp
[TestCase(5f, 4f, true)]
[TestCase(5.4f, 4f, true)]
[TestCase(6f, 4f, false)]
public void IsGridPositionOccupied_UsesMinimumDistance(
    float x,
    float y,
    bool expected)
```

Expected before implementation: reflected method is null.

- [ ] **Step 2: Remove vertical ally formation state from UnitSpawner**

Remove `FormationSpacing`, `_allySpawnIndex`, `ResetAllySpawnOrder`, and `GetAllyPreparationPosition`. Change ally activation to use spawn index `0`, while preserving enemy spawn indices and their current Y formation. Split ally positioning from enemy positioning so the enemy behavior remains byte-for-byte equivalent in effect; do not remove `GetFormationOffset` because enemies still use it.

- [ ] **Step 3: Add preparation-position ownership to UnitManager**

```csharp
private readonly Dictionary<AllyUnit, Vector3>
    _allyPreparationPositions = new();
```

After `_spawner.SpawnAlly` succeeds, compute collider padding, scan grid indices from `0` upward, skip candidates occupied by saved positions, move the ally to the first candidate, record it, then call `AddOwnedAlly`. If no slot exists, warn, return the ally to the pool, and return `null` without changing roster counts.

- [ ] **Step 4: Implement occupied-slot and padding helpers**

Use the same maximum collider extent already used by `IsValidAllyPlacement`. Centralize it as `GetAllyPlacementPadding(AllyUnit ally)` so spawn, drag validation, and grid spacing cannot disagree.

```csharp
private static bool IsGridPositionOccupied(
    Vector3 candidate,
    IEnumerable<Vector3> occupiedPositions,
    float minimumDistance)
{
    foreach (Vector3 occupied in occupiedPositions)
    {
        if (Vector2.Distance(candidate, occupied) < minimumDistance)
        {
            return true;
        }
    }

    return false;
}
```

Use `minimumDistance = padding * 2f + 0.15f`, matching Task 1's grid gap.

- [ ] **Step 5: Maintain saved-position lifecycle**

```csharp
public void SaveAllyPreparationPosition(AllyUnit ally)
{
    if (!CanDragAlly(ally) ||
        !IsValidAllyPlacement(ally, ally.transform.position)) return;

    _allyPreparationPositions[ally] = ally.transform.position;
}
```

Remove the entry in `ReleaseUnit`. In `SpawnMergedAlly`, let normal spawning register a temporary grid slot, then overwrite both the transform and saved entry with the merge target position. Ensure `ConsumeReservedInputs` captures the target position before releasing either input, as the current call sites already do.

- [ ] **Step 6: Restore saved positions after waves**

Replace `ResetAllySpawnOrder` and `GetAllyPreparationPosition(i)` in `RestoreAlliesForPreparation`. For each owned ally, use its saved position. Only if an entry is missing, find and save the first free grid position before calling `RestoreForPreparation`. Preserve mana reset, active-list rebuild, and item-modifier refresh.

- [ ] **Step 7: Run focused tests and verify pass**

Run Task 1 Step 2. Expected: all placement tests pass and there are no compilation errors.

- [ ] **Step 8: Commit spawn and persistence behavior**

```powershell
git add -- 'Assets/02. Scripts/Battle/UnitSpawner.cs' 'Assets/02. Scripts/Battle/UnitManager.cs' 'Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs'
git commit -m "feat: preserve ally preparation positions"
```

---

### Task 3: Drag flow and full-width scene bounds

**Files:**
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: Task 1 `ClampAllyPlacement`.
- Consumes: Task 2 `SaveAllyPreparationPosition`.

- [ ] **Step 1: Use right-half clamp while dragging**

Replace the current full-area clamp in `OnMouseDrag`:

```csharp
worldPosition = _unitManager.BattleArea.ClampAllyPlacement(
    worldPosition,
    GetPlacementPadding());
```

Keep camera conversion and Z preservation unchanged.

- [ ] **Step 2: Save only completed non-merge drops**

After overlap detection, keep the existing merge path and return immediately after calling `TryMergeAllies`. When no target exists, call:

```csharp
_unitManager.SaveAllyPreparationPosition(this);
```

Rejected drops continue to restore `_dragStartPosition`, so they never modify saved state.

- [ ] **Step 3: Extend the scene battle-area rectangle**

In the `Panel_BattleArea` `RectTransform` block, change only:

```yaml
m_AnchorMax: {x: 1, y: 0.9}
```

from the current X value `0.7`. Preserve its vertical anchors, pivot, hierarchy, and `BattleAreaBounds` references. This removes the artificial right-edge cutoff; Task 1's midpoint logic then limits allies to the right half of the resulting full width.

- [ ] **Step 4: Run focused tests and inspect the scene diff**

Run Task 1 Step 2, then:

```powershell
git diff -- 'Assets/01. Scenes/02. Game.unity'
```

Expected: tests pass and the scene diff contains only the intended `m_AnchorMax.x` change for `Panel_BattleArea`.

- [ ] **Step 5: Perform focused Play Mode verification**

1. Drag an ally left and verify its body stops at the padded midpoint.
2. Drag an ally right and verify its body reaches the visible map edge without crossing it.
3. Summon six allies and verify X advances before Y with no unit outside the map.
4. Arrange a distinct pattern, complete a wave, and verify exact positions return.
5. Merge two allies, complete a wave, and verify the result returns to the target position.
6. Verify enemies and fighting units can still traverse the full battle area.

- [ ] **Step 6: Commit drag and scene changes**

```powershell
git add -- 'Assets/02. Scripts/Battle/AlllyUnit.cs' 'Assets/01. Scenes/02. Game.unity'
git commit -m "feat: constrain ally preparation to right half"
```

---

### Task 4: Full verification and AI usage record

**Files:**
- Modify: `.github/ai-use-log.md`

**Interfaces:**
- Consumes: Tasks 1-3 code and verification results.
- Produces: factual repository-required AI usage record.

- [ ] **Step 1: Run all EditMode tests**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\editmode-results.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\editmode.log'
```

Expected: exit code `0`, all EditMode tests pass, and the log has no C# compilation errors.

- [ ] **Step 2: Verify WebGL compilation using the existing project build path**

Use the configured Unity Build Profile or existing batch build entry point without installing packages or changing build settings. Record the exact path and result; if the project exposes no batch entry point, report that limitation and use Unity's existing WebGL Build Profile manually.

- [ ] **Step 3: Append the factual AI usage record**

Record:

```markdown
## 2026-08-10 아군 준비 배치 제한 및 복원

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 오른쪽 절반 배치, 오른쪽 끝 사용, 가로 우선 자동 소환, 웨이브 종료 후 기존 배치 복원
- AI 제안 내용: 전체 맵 경계와 아군 준비 경계를 분리하고 UnitManager가 캐릭터별 준비 위치를 보존
- AI 실제 수정 영역: 실제 변경한 C# 파일, EditMode 테스트, Game 씬, AI 사용 기록
- 사용자 직접 결정/수정 필요 영역: 사용자가 오른쪽 절반과 가로 우선 격자 및 권장 저장 방식을 결정; 최종 체감 간격은 직접 확인 가능
- 중요한 프롬프트/지시: 기존 구조 보존, 최소 수정, Inspector 참조, 풀링 유지, SerializeField underscore 금지
- 테스트/검증 결과: 실제 EditMode, Play Mode, WebGL 결과와 알려진 제한
```

- [ ] **Step 4: Check scope, whitespace, and repository state**

```powershell
git diff --check
git status --short
```

Expected: only approved implementation/test/scene/log files and Unity-generated test metadata are present; unrelated user changes remain untouched.

- [ ] **Step 5: Commit the verified AI record**

```powershell
git add -- '.github/ai-use-log.md'
git commit -m "docs: record ally placement AI usage"
```

## Plan Self-Review

- Spec coverage: right-half restriction, padded right edge, horizontal-first summon grid, occupied-slot skipping, per-ally saved positions, wave restoration, merge inheritance, scene bounds, combat-bound preservation, tests, WebGL, and AI logging each map to an explicit task.
- Placeholder scan: no TBD, TODO, undefined helper, or unspecified test remains.
- Type consistency: Task 1 instance APIs are consumed under the same names in Tasks 2-3; Task 2 position-save API is consumed unchanged in Task 3.
- Scope check: the plan adds no save-data change, package, new top-level folder, runtime-created core component, broad refactor, or unrelated cleanup.
