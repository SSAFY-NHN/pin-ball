# Finite Defense-Line Wave Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the infinite stage loop with a manual-start, fixed 10-wave run whose wave outcomes are decided by destroying either team's defense line, while retaining paid ally purchases and tactical reinforcement.

**Architecture:** Restore the finite wave state machine and data-driven enemy compositions without reverting whole files to `Dev`. Add a pure `BattleDefenseLineController` for per-attempt line HP, route both teams toward the opposing line when no unit target exists, and let `BattleManager` resolve clear, retry, victory, and defeat from line destruction. Restore legacy wave/result UI selectively while preserving automatic pinball, purchases, upgrades, metrics, restart, and tactical reinforcement.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity scene YAML, JSON data

**Spec:** `docs/designs/2026-08-21/2026-08-21-finite-defense-line-wave-flow-design.md`

## Global Constraints

- Keep the existing repository structure; do not move or rename existing files. In particular, keep the existing `AlllyUnit.cs` filename.
- Use scene placement and Inspector references for both defense lines; do not create them at runtime.
- Keep `[SerializeField]` names without a leading underscore.
- Keep `App.Get<T>()` usage allowed.
- Preserve automatic pinball, production upgrades, jackpot, ally purchases, tactical reinforcement, battle upgrades, prototype metrics, and restart behavior.
- Do not restore manual pinball launch, goal-pocket ally rewards, or preparation/battle camera sliding.
- Do not add packages, generalized frameworks, or unrelated refactors.
- Use `SetActive` and existing pooling rather than new Instantiate/Destroy loops.
- Run focused EditMode tests after each task and the complete EditMode suite at the final gate.
- Record factual AI usage under `docs/ai-usage/2026-08-21/` when implementation ends.

---

### Task 1: Restore finite wave state and remove wave gold rewards

**Files:**
- Modify: `Assets/02. Scripts/00. Core/Enum.cs:16`
- Modify: `Assets/02. Scripts/Battle/BattleDataTypes.cs:44`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs:107`
- Modify: `Assets/Resources/Data/BattleWaveData.json`
- Modify: `Assets/02. Scripts/Battle/Runtime/BattleRunState.cs`
- Modify: `Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs.meta`
- Create: `Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs.meta`
- Create: `Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs.meta`

**Interfaces:**
- Consumes: `BattleWaveData`, `EWaveResolutionResult`.
- Produces: `BattleRunState.CurrentWave`, `CurrentWaveNumber`, `TotalWaveCount`, `PlayerHp`, `MaximumPlayerHp`, `State`, `ChangeState`, `AdvanceWave`, `ConsumeChance`; `BattleResolutionPolicy.ResolveNextState`; `WaveResolutionState`.

- [ ] **Step 1: Restore characterization tests for waves, chances, and resolution timing**

Create tests with these exact expectations:

```csharp
[Test]
public void RunState_StartsAtFirstWaveWithThreeChances()
{
    var waves = new[] { new BattleWaveData(), new BattleWaveData() };
    var state = new BattleRunState(waves, true, 3);

    Assert.That(state.CurrentWaveNumber, Is.EqualTo(1));
    Assert.That(state.TotalWaveCount, Is.EqualTo(2));
    Assert.That(state.PlayerHp, Is.EqualTo(3));
    Assert.That(state.MaximumPlayerHp, Is.EqualTo(3));
    Assert.That(state.State, Is.EqualTo(EWaveState.Pending));
}

[Test]
public void ConsumeChance_StopsAtZeroWithoutChangingWave()
{
    var state = new BattleRunState(
        new[] { new BattleWaveData(), new BattleWaveData() },
        true,
        3);

    Assert.That(state.ConsumeChance(), Is.True);
    Assert.That(state.ConsumeChance(), Is.True);
    Assert.That(state.ConsumeChance(), Is.True);
    Assert.That(state.ConsumeChance(), Is.False);
    Assert.That(state.PlayerHp, Is.Zero);
    Assert.That(state.CurrentWaveNumber, Is.EqualTo(1));
}

[TestCase(EWaveResolutionResult.Cleared, false, 2, EWaveState.Pending)]
[TestCase(EWaveResolutionResult.Cleared, true, 2, EWaveState.Victory)]
[TestCase(EWaveResolutionResult.Failed, false, 2, EWaveState.Pending)]
[TestCase(EWaveResolutionResult.Failed, false, 0, EWaveState.Defeat)]
public void ResolveNextState_UsesResultFinalWaveAndChances(
    EWaveResolutionResult result,
    bool isFinalWave,
    int remainingChances,
    EWaveState expected)
{
    Assert.That(
        BattleResolutionPolicy.ResolveNextState(
            result,
            isFinalWave,
            remainingChances),
        Is.EqualTo(expected));
}
```

Create `WaveResolutionTests` with the exact timing checks:

```csharp
[Test]
public void TryBegin_TracksOnePendingResultAndDeadline()
{
    var state = new WaveResolutionState();

    Assert.That(state.TryBegin(
        EWaveResolutionResult.Cleared,
        3,
        10f,
        2f), Is.True);
    Assert.That(state.TryBegin(
        EWaveResolutionResult.Failed,
        3,
        10f,
        2f), Is.False);
    Assert.That(state.Result, Is.EqualTo(EWaveResolutionResult.Cleared));
    Assert.That(state.WaveNumber, Is.EqualTo(3));
    Assert.That(state.EndsAt, Is.EqualTo(12f));
    Assert.That(state.IsElapsed(11.99f), Is.False);
    Assert.That(state.IsElapsed(12f), Is.True);
}

[Test]
public void Clear_RemovesPendingResolution()
{
    var state = new WaveResolutionState();
    state.TryBegin(EWaveResolutionResult.Failed, 2, 5f, 1f);

    state.Clear();

    Assert.That(state.IsPending, Is.False);
    Assert.That(state.WaveNumber, Is.Zero);
    Assert.That(state.EndsAt, Is.Zero);
}
```

Use meta GUID `d2b4d8dba69e3e34d83bec982f797678` for `BattleRunStateTests` and `a1100000000000000000000000000003` for `WaveResolutionTests`.

- [ ] **Step 2: Run focused tests and confirm the expected compile failure**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter 'BattleRunStateTests|WaveResolutionTests' -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\finite-wave-task1-red.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\finite-wave-task1-red.log'
```

Expected: FAIL because `Pending`, `Victory`, `Defeat`, the wave-list constructor, or `ConsumeChance()` does not exist yet.

- [ ] **Step 3: Add finite states without breaking current consumers**

Use this transitional superset so the project compiles until Tasks 4, 5, and 7 replace every infinite-state consumer:

```csharp
public enum EWaveState
{
    Pending,
    Starting,
    Active,
    Resolving,
    Advancing,
    Recovering,
    Victory,
    Defeat,
    Ended
}
```

Task 7 removes `Starting`, `Advancing`, `Recovering`, and `Ended` after their final consumers are deleted.

Implement `BattleRunState` with the following ownership and behavior:

```csharp
private readonly IReadOnlyList<BattleWaveData> waves;
private readonly bool hasValidRun;

public int CurrentWaveIndex { get; private set; }
public int CurrentWaveNumber => CurrentWaveIndex + 1;
public int TotalWaveCount => waves?.Count ?? 0;
public int MaximumPlayerHp { get; private set; }
public int PlayerHp { get; private set; }
public EWaveState State { get; private set; } = EWaveState.Pending;
public bool HasValidCurrentWave =>
    hasValidRun && CurrentWaveIndex >= 0 && CurrentWaveIndex < TotalWaveCount &&
    waves[CurrentWaveIndex] != null;
public BattleWaveData CurrentWave =>
    HasValidCurrentWave ? waves[CurrentWaveIndex] : null;

public BattleRunState(
    IReadOnlyList<BattleWaveData> waves,
    bool hasValidRun,
    int maximumChances)
{
    this.waves = waves ?? Array.Empty<BattleWaveData>();
    this.hasValidRun = hasValidRun;
    MaximumPlayerHp = Math.Max(1, maximumChances);
    PlayerHp = MaximumPlayerHp;
}

// Transitional overload used only by the current infinite BattleManager.
// Task 7 removes it after Task 4 migrates BattleManager.
public BattleRunState(int maximumHp)
    : this(Array.Empty<BattleWaveData>(), false, maximumHp)
{
}

public bool ConsumeChance()
{
    if (PlayerHp <= 0) return false;
    PlayerHp--;
    return true;
}
```

Implement the remaining transitions exactly:

```csharp
public bool ChangeState(EWaveState nextState)
{
    if (State == nextState) return false;
    State = nextState;
    return true;
}

public bool AdvanceWave()
{
    if (CurrentWaveIndex + 1 >= TotalWaveCount) return false;
    CurrentWaveIndex++;
    return true;
}
```

Keep the current `ApplyPlayerDamage`, `RestorePlayerHp`, and `IncreaseMaximumPlayerHp` methods through Task 3 so the pre-migration `BattleManager` compiles. Task 4 removes every call, and Task 7 deletes those compatibility methods and the one-argument constructor. New finite-wave code uses `ConsumeChance()` only.

- [ ] **Step 4: Restore result timing and result policy**

Implement `WaveResolutionState` with this exact state machine, using meta GUID `a1100000000000000000000000000001`:

```csharp
using UnityEngine;

public sealed class WaveResolutionState
{
    public bool IsPending { get; private set; }
    public EWaveResolutionResult Result { get; private set; }
    public int WaveNumber { get; private set; }
    public float EndsAt { get; private set; }

    public bool TryBegin(
        EWaveResolutionResult result,
        int waveNumber,
        float now,
        float duration)
    {
        if (IsPending) return false;
        IsPending = true;
        Result = result;
        WaveNumber = Mathf.Max(1, waveNumber);
        EndsAt = now + Mathf.Max(0f, duration);
        return true;
    }

    public bool IsElapsed(float now) => IsPending && now >= EndsAt;

    public void Clear()
    {
        IsPending = false;
        WaveNumber = 0;
        EndsAt = 0f;
    }
}
```

Replace unit-count wipe detection with this policy:

```csharp
public static EWaveState ResolveNextState(
    EWaveResolutionResult result,
    bool isFinalWave,
    int remainingChances)
{
    if (result == EWaveResolutionResult.Failed)
    {
        return remainingChances <= 0
            ? EWaveState.Defeat
            : EWaveState.Pending;
    }

    return isFinalWave
        ? EWaveState.Victory
        : EWaveState.Pending;
}
```

Keep the existing `TryDetectWipe` method through Task 3 because the current `BattleManager` still references it. Task 4 removes that subscription and call; Task 7 deletes `TryDetectWipe`.

- [ ] **Step 5: Remove every wave reward field from code and JSON**

Delete `RetryGoldReward`, `WaveClearGoldReward`, and `FinalClearGoldReward` from `BattleWaveData`. Delete their negative-value validation from `TitleData`. Remove all three properties from every wave object in `BattleWaveData.json`; keep enemy composition, `WaveName`, `IsElite`, and `IsBoss` unchanged.

Add this reflection assertion to `BattleRunStateTests`:

```csharp
[TestCase("RetryGoldReward")]
[TestCase("WaveClearGoldReward")]
[TestCase("FinalClearGoldReward")]
public void BattleWaveData_DoesNotExposeGoldRewardField(string fieldName)
{
    Assert.That(typeof(BattleWaveData).GetField(fieldName), Is.Null);
}
```

- [ ] **Step 6: Run focused tests and inspect JSON residue**

Run the Task 1 Unity command again with result path `finite-wave-task1-green.xml`. Expected: PASS.

Run:

```powershell
rg -n 'RetryGoldReward|WaveClearGoldReward|FinalClearGoldReward' Assets
```

Expected: no matches.

- [ ] **Step 7: Commit Task 1**

```powershell
git add -- 'Assets/02. Scripts/00. Core/Enum.cs' 'Assets/02. Scripts/Battle/BattleDataTypes.cs' 'Assets/02. Scripts/02. Data/TitleData.cs' 'Assets/Resources/Data/BattleWaveData.json' 'Assets/02. Scripts/Battle/Runtime/BattleRunState.cs' 'Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs' 'Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs' 'Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs.meta' 'Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs' 'Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs.meta' 'Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs' 'Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs.meta'
git commit -m "refactor(battle): restore finite wave state"
```

---

### Task 2: Add independent ally and enemy defense-line HP

**Files:**
- Create: `Assets/02. Scripts/Battle/Runtime/BattleDefenseLineController.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/BattleDefenseLineController.cs.meta`
- Create: `Assets/02. Scripts/Battle/Editor/BattleDefenseLineControllerTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleDefenseLineControllerTests.cs.meta`

**Interfaces:**
- Consumes: `EBattleTeam`.
- Produces: `BattleDefenseLineController(int allyMaximumHp, int enemyMaximumHp)`, `ResetForWave()`, `ApplyDamage(EBattleTeam, int)`, `IncreaseAllyMaximumHp(int)`, `GetCurrentHp(EBattleTeam)`, `GetMaximumHp(EBattleTeam)`, `IsDestroyed(EBattleTeam)`.

- [ ] **Step 1: Write defense-line domain tests**

```csharp
[Test]
public void ResetForWave_RestoresBothLinesToMaximum()
{
    var controller = new BattleDefenseLineController(20, 20);
    controller.ApplyDamage(EBattleTeam.Ally, 7);
    controller.ApplyDamage(EBattleTeam.Enemy, 9);

    controller.ResetForWave();

    Assert.That(controller.GetCurrentHp(EBattleTeam.Ally), Is.EqualTo(20));
    Assert.That(controller.GetCurrentHp(EBattleTeam.Enemy), Is.EqualTo(20));
}

[Test]
public void ApplyDamage_ClampsAtZeroAndReportsDestroyedLine()
{
    var controller = new BattleDefenseLineController(20, 20);

    Assert.That(controller.ApplyDamage(EBattleTeam.Enemy, 25), Is.True);
    Assert.That(controller.GetCurrentHp(EBattleTeam.Enemy), Is.Zero);
    Assert.That(controller.IsDestroyed(EBattleTeam.Enemy), Is.True);
    Assert.That(controller.ApplyDamage(EBattleTeam.Enemy, 1), Is.False);
}

[Test]
public void IncreaseAllyMaximumHp_DoesNotChangeEnemyMaximum()
{
    var controller = new BattleDefenseLineController(20, 20);

    Assert.That(controller.IncreaseAllyMaximumHp(10), Is.True);
    Assert.That(controller.GetMaximumHp(EBattleTeam.Ally), Is.EqualTo(30));
    Assert.That(controller.GetCurrentHp(EBattleTeam.Ally), Is.EqualTo(30));
    Assert.That(controller.GetMaximumHp(EBattleTeam.Enemy), Is.EqualTo(20));
}
```

- [ ] **Step 2: Run the new tests and confirm missing-type failure**

Use Unity EditMode `-testFilter BattleDefenseLineControllerTests`, results `finite-wave-task2-red.xml`. Expected: FAIL because `BattleDefenseLineController` is missing.

- [ ] **Step 3: Implement the controller without MonoBehaviour dependencies**

```csharp
using System;

public sealed class BattleDefenseLineController
{
    public int AllyMaximumHp { get; private set; }
    public int EnemyMaximumHp { get; }
    private int allyHp;
    private int enemyHp;

    public BattleDefenseLineController(int allyMaximumHp, int enemyMaximumHp)
    {
        AllyMaximumHp = Math.Max(1, allyMaximumHp);
        EnemyMaximumHp = Math.Max(1, enemyMaximumHp);
        ResetForWave();
    }

    public void ResetForWave()
    {
        allyHp = AllyMaximumHp;
        enemyHp = EnemyMaximumHp;
    }

    public bool ApplyDamage(EBattleTeam team, int amount)
    {
        if (amount <= 0 || IsDestroyed(team)) return false;
        if (team == EBattleTeam.Ally)
            allyHp = Math.Max(0, allyHp - amount);
        else
            enemyHp = Math.Max(0, enemyHp - amount);
        return true;
    }

    public bool IncreaseAllyMaximumHp(int amount)
    {
        if (amount <= 0) return false;
        AllyMaximumHp += amount;
        allyHp += amount;
        return true;
    }

    public int GetCurrentHp(EBattleTeam team) =>
        team == EBattleTeam.Ally ? allyHp : enemyHp;

    public int GetMaximumHp(EBattleTeam team) =>
        team == EBattleTeam.Ally ? AllyMaximumHp : EnemyMaximumHp;

    public bool IsDestroyed(EBattleTeam team) => GetCurrentHp(team) <= 0;
}
```

Create meta GUID `c821d92cdfe04f7890b1c371026d6b2a` for runtime and `b23af614a6444e329e2bf0c9f54d7ad1` for tests.

- [ ] **Step 4: Run Task 2 tests**

Use Unity EditMode `-testFilter BattleDefenseLineControllerTests`, results `finite-wave-task2-green.xml`. Expected: PASS.

- [ ] **Step 5: Commit Task 2**

```powershell
git add -- 'Assets/02. Scripts/Battle/Runtime/BattleDefenseLineController.cs' 'Assets/02. Scripts/Battle/Runtime/BattleDefenseLineController.cs.meta' 'Assets/02. Scripts/Battle/Editor/BattleDefenseLineControllerTests.cs' 'Assets/02. Scripts/Battle/Editor/BattleDefenseLineControllerTests.cs.meta'
git commit -m "feat(battle): add dual defense-line state"
```

---

### Task 3: Make both teams attack the opposing defense line

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs:30-490`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs:56`
- Modify: `Assets/02. Scripts/Battle/EnemyUnit.cs:8-68`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:33-390`
- Modify: `Assets/02. Scripts/Battle/DefenseLineTrigger.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/DefenseLineBreachTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitTargetFinderTests.cs`

**Interfaces:**
- Consumes: Task 2 team/line concepts only.
- Produces: `UnitBase.HasReachedDefenseLine`, `ReachDefenseLine(EBattleTeam)`, `LeaveDefenseLine()`, `TryMoveOrAttackDefenseLine(UnitManager)`; `UnitManager.TryGetOpposingDefenseLinePosition`, `IsActiveUnit`, `RequestDefenseLineAttack`, internal `OnDefenseLineAttackRequested`; team-aware `DefenseLineTrigger`.

- [ ] **Step 1: Replace unit-count wipe tests with symmetric line-target tests**

Keep the existing reinforcement-retargeting case and add:

```csharp
[Test]
public void ReachDefenseLine_IgnoresOwnLineAndAcceptsOpposingLine()
{
    var allyObject = new GameObject("ally");
    try
    {
        var ally = allyObject.AddComponent<AllyUnit>();

        ally.ReachDefenseLine(EBattleTeam.Ally);
        Assert.That(ally.HasReachedDefenseLine, Is.False);

        ally.ReachDefenseLine(EBattleTeam.Enemy);
        Assert.That(ally.HasReachedDefenseLine, Is.True);
    }
    finally
    {
        Object.DestroyImmediate(allyObject);
    }
}

[Test]
public void EnemyReachDefenseLine_NewAllyMakesEnemyLeaveLine()
{
    var enemyObject = new GameObject("enemy");
    var allyObject = new GameObject("ally");
    try
    {
        var roster = new UnitRoster();
        var finder = new UnitTargetFinder(roster);
        var context = new UnitCombatContext(finder, null, null, null);
        var enemy = enemyObject.AddComponent<TestEnemyUnit>();
        var ally = allyObject.AddComponent<AllyUnit>();
        var stats = new BattleUnitStats
        {
            MaxHp = 10f,
            AttackDamage = 1f,
            AttackRate = 1f,
            AttackRange = 1f,
            MoveSpeed = 1f
        };
        enemy.Initialize(stats, context);
        ally.Initialize(stats, context);
        enemy.SetData(new EnemyUnitData { id = "goblin" });
        roster.AddEnemy(enemy);
        enemy.ReachDefenseLine(EBattleTeam.Ally);
        roster.AddOwnedAlly(ally);

        enemy.InvokeTick();

        Assert.That(enemy.HasReachedDefenseLine, Is.False);
        Assert.That(enemy.CurrentTarget, Is.SameAs(ally));
    }
    finally
    {
        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(allyObject);
    }
}
```

- [ ] **Step 2: Run focused tests and observe signature failures**

Use `-testFilter DefenseLineBreachTests`, results `finite-wave-task3-red.xml`. Expected: FAIL because the team-aware line API does not exist.

- [ ] **Step 3: Move shared reached-line state into UnitBase**

Add:

```csharp
public bool HasReachedDefenseLine { get; private set; }

public void ReachDefenseLine(EBattleTeam defenseTeam)
{
    if (!IsAlive || IsInPool || defenseTeam == Team) return;
    HasReachedDefenseLine = true;
    ClearTarget();
}

protected void LeaveDefenseLine()
{
    HasReachedDefenseLine = false;
}

protected bool TryMoveOrAttackDefenseLine(UnitManager unitManager)
{
    EBattleTeam defenseTeam = Team == EBattleTeam.Ally
        ? EBattleTeam.Enemy
        : EBattleTeam.Ally;

    if (HasReachedDefenseLine)
    {
        ClearTarget();
        if (TryScheduleBasicAttack())
        {
            unitManager.RequestDefenseLineAttack(
                this,
                defenseTeam,
                GetBasicAttackDamage(null));
        }
        return true;
    }

    if (unitManager != null &&
        unitManager.TryGetOpposingDefenseLinePosition(Team, out Vector3 position))
    {
        MoveTowardsPosition(position);
        ClearTarget();
        return true;
    }

    return false;
}
```

Set `_isBattleActive = true` and `HasReachedDefenseLine = false` inside `ResetCombatState()` so preparation restoration resumes ticking and pooled units never retain line state. Keep `StopBattle()` responsible for setting `_isBattleActive = false` while a result panel is active.

- [ ] **Step 4: Update AllyUnit and EnemyUnit tick priorities**

Both ticks must follow this exact order:

```csharp
if (TryKeepOrAcquireTarget())
{
    LeaveDefenseLine();
    // Preserve the class's existing skill or MoveOrAttackTarget behavior.
    return;
}

if (TryMoveOrAttackDefenseLine(_unitManager)) return;
_state = EBattleUnitState.Idle;
ClearTarget();
```

Remove the enemy-only `HasReachedDefenseLine`, `ReachDefenseLine`, and `TryAttackDefenseLine` members after the shared implementation exists. Preserve enemy skill ticking and ally skill casting unchanged.

- [ ] **Step 5: Add team-aware scene references in UnitManager and trigger filtering**

Replace the single `defenseLine` field with:

```csharp
[SerializeField] private DefenseLineTrigger allyDefenseLine;
[SerializeField] private DefenseLineTrigger enemyDefenseLine;

public bool TryGetOpposingDefenseLinePosition(
    EBattleTeam attackerTeam,
    out Vector3 position)
{
    DefenseLineTrigger target = attackerTeam == EBattleTeam.Ally
        ? enemyDefenseLine
        : allyDefenseLine;
    if (target == null)
    {
        position = default;
        return false;
    }

    position = target.transform.position;
    return true;
}
```

Add `UnitManager.IsActiveUnit(UnitBase unit)` by checking `ActiveAllies` for allies and `ActiveEnemies` for enemies. Route attacks without creating a BattleManager dependency in `UnitBase`:

```csharp
internal event Action<UnitBase, EBattleTeam, float>
    OnDefenseLineAttackRequested;

public void RequestDefenseLineAttack(
    UnitBase attacker,
    EBattleTeam defenseTeam,
    float attackDamage)
{
    if (attacker == null || attacker.Team == defenseTeam ||
        !IsActiveUnit(attacker)) return;
    OnDefenseLineAttackRequested?.Invoke(
        attacker,
        defenseTeam,
        attackDamage);
}
```

Change `DefenseLineTrigger` to serialize `EBattleTeam defenseTeam`, expose `DefenseTeam`, resolve `UnitBase`, ignore same-team units, and call `unit.ReachDefenseLine(defenseTeam)`.

- [ ] **Step 6: Run the focused combat tests**

Run Unity EditMode tests with filter `DefenseLineBreachTests|UnitTargetFinderTests|UnitPoolResetTests`, results `finite-wave-task3-green.xml`. Expected: PASS.

- [ ] **Step 7: Commit Task 3**

```powershell
git add -- 'Assets/02. Scripts/Battle/UnitBase.cs' 'Assets/02. Scripts/Battle/AlllyUnit.cs' 'Assets/02. Scripts/Battle/EnemyUnit.cs' 'Assets/02. Scripts/Battle/UnitManager.cs' 'Assets/02. Scripts/Battle/DefenseLineTrigger.cs' 'Assets/02. Scripts/Battle/Editor/DefenseLineBreachTests.cs' 'Assets/02. Scripts/Battle/Editor/UnitTargetFinderTests.cs'
git commit -m "feat(battle): target opposing defense lines"
```

---

### Task 4: Rebuild BattleManager around manual fixed waves

**Files:**
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs:8-552`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:128-430`
- Modify: `Assets/02. Scripts/Battle/Units/UnitSpawnController.cs:62`
- Modify: `Assets/02. Scripts/Battle/Editor/BattleEconomyTests.cs`
- Modify: `Assets/02. Scripts/PrototypeMetricsController.cs`
- Modify: `Assets/02. Scripts/03. UI/PrototypeMetricsDisplayController.cs`

**Interfaces:**
- Consumes: Task 1 run/result state, Task 2 line controller, Task 3 unit line attacks.
- Produces: `CanStartCurrentWave`, `TryStartWave`, `TryApplyDefenseLineAttack(UnitBase, EBattleTeam, float)`, `GetDefenseLineHp(EBattleTeam)`, `GetDefenseLineMaximumHp(EBattleTeam)`, `Action<EBattleTeam, int, int> OnDefenseLineHpChanged`, `OnWaveStarted`, `OnWaveResolved`, `BattleWaveStartedData`, `BattleWaveResolvedData`; `UnitManager.BeginWave` and `ResolveWaveResult`.

- [ ] **Step 1: Add a no-reward economy regression test**

The result policy cases already live in `BattleRunStateTests`. Add this economy guard to `BattleEconomyTests`:

```csharp
[Test]
public void WaveResolutionPolicy_DoesNotMutateEconomy()
{
    var economy = new BattleEconomy(25);

    BattleResolutionPolicy.ResolveNextState(
        EWaveResolutionResult.Cleared,
        false,
        3);
    BattleResolutionPolicy.ResolveNextState(
        EWaveResolutionResult.Failed,
        false,
        2);

    Assert.That(economy.Gold, Is.EqualTo(25));
}
```

- [ ] **Step 2: Replace stage fields with wave and line fields**

Use these serialized values:

```csharp
[Header("Run Chances")]
[SerializeField, Min(1)] public int playerMaxHp = 3;
[Header("Defense Lines")]
[SerializeField, Min(1)] private int allyDefenseLineMaxHp = 20;
[SerializeField, Min(1)] private int enemyDefenseLineMaxHp = 20;
[Header("Wave Resolution")]
[SerializeField, Min(0f)] private float waveResolutionDuration = 2f;
```

Remove `stageTransitionDuration`, stage enemy scaling fields, and recurring boss interval/multiplier fields.

Expose finite-wave properties from `BattleRunState`:

```csharp
public BattleWaveData CurrentWave => runState?.CurrentWave;
public int CurrentWaveNumber => runState?.CurrentWaveNumber ?? 1;
public int TotalWaveCount => runState?.TotalWaveCount ?? 0;
public int PlayerHp => runState?.PlayerHp ?? playerMaxHp;
public int MaximumPlayerHp => runState?.MaximumPlayerHp ?? playerMaxHp;
public bool IsCurrentWaveBoss => CurrentWave?.IsBoss ?? false;
public bool IsPreparationPhase => State == EWaveState.Pending;
public bool IsRunEnded => State is EWaveState.Victory or EWaveState.Defeat;
public bool CanStartCurrentWave =>
    CanUsePreparationActions && CurrentWave != null &&
    unitManager != null && unitManager.CanStartWaveWithCurrentRoster;
```

Keep these two read-only compatibility aliases until Task 5 migrates `StatusPanel`; Task 5 removes them immediately afterward:

```csharp
public int CurrentStageNumber => CurrentWaveNumber;
public bool IsCurrentStageBoss => IsCurrentWaveBoss;
```

Do not keep `BattleStageController` or `EnemyStageScalingController` fields in `BattleManager`.

- [ ] **Step 3: Initialize but do not auto-start the first wave**

Construct:

```csharp
runState = new BattleRunState(
    titleData.BattleWaves,
    titleData.HasValidBattleRun,
    playerMaxHp);
defenseLineController = new BattleDefenseLineController(
    allyDefenseLineMaxHp,
    enemyDefenseLineMaxHp);
waveResolution = new WaveResolutionState();
```

Subscribe `unitManager.OnDefenseLineAttackRequested += TryApplyDefenseLineAttack` during initialization and unsubscribe in `OnDestroy`. Keep economy, battle upgrades, unit purchases, tactical reinforcement, pinball subscriptions, and item subscriptions. Remove `StartCurrentStage()` from initialization. Emit initialized, state, chances, gold, wave number, and both line HP values.

- [ ] **Step 4: Restore manual wave start with data-driven enemy spawning**

Implement:

```csharp
public bool TryStartWave()
{
    if (!CanStartCurrentWave) return false;

    defenseLineController.ResetForWave();
    NotifyAllDefenseLineHp();
    int spawnedCount = unitManager.BeginWave(
        CurrentWave,
        CurrentWaveNumber);
    if (spawnedCount <= 0) return false;

    currentWaveStartedAt = Time.time;
    currentWaveAllyDefenseDamage = 0;
    ChangeState(EWaveState.Active);
    OnWaveStarted?.Invoke(new BattleWaveStartedData(
        CurrentWaveNumber,
        IsCurrentWaveBoss,
        spawnedCount));
    SoundManager.PlaySFXIfAvailable(SoundName.WaveStart);
    return true;
}
```

Implement the data-driven spawn loop:

```csharp
public int BeginWave(BattleWaveData wave, int waveNumber)
{
    ReturnAllEnemies();
    CleanupDestroyedUnits();
    if (wave?.Enemies == null) return 0;

    _spawnController.BeginEnemyWave();
    int spawnedCount = 0;
    foreach (BattleEnemySpawnData entry in wave.Enemies)
    {
        if (entry == null || string.IsNullOrEmpty(entry.EnemyId)) continue;
        for (int count = 0; count < Mathf.Max(1, entry.Count); count++)
        {
            if (SpawnEnemy(entry.EnemyId, waveNumber, null) != null)
                spawnedCount++;
        }
    }
    return spawnedCount;
}
```

Use `SpawnEnemy(string enemyId, int waveNumber, Vector3? spawnPosition)` and remove health/attack multiplier parameters from `UnitSpawnController.SpawnEnemy` because recurring boss multipliers no longer exist.

- [ ] **Step 5: Resolve waves only from defense-line destruction**

Implement line damage validation:

```csharp
public void TryApplyDefenseLineAttack(
    UnitBase attacker,
    EBattleTeam defenseTeam,
    float attackDamage)
{
    if (State != EWaveState.Active || attacker == null ||
        attacker.Team == defenseTeam || !unitManager.IsActiveUnit(attacker))
        return;

    int damage = defenseTeam == EBattleTeam.Ally
        ? BarrierDamageCalculator.Calculate(
            Mathf.RoundToInt(attackDamage),
            barrierDamageReduction,
            minimumBarrierDamage)
        : Mathf.Max(1, Mathf.RoundToInt(attackDamage));

    int previousHp = defenseLineController.GetCurrentHp(defenseTeam);
    if (!defenseLineController.ApplyDamage(defenseTeam, damage)) return;
    if (defenseTeam == EBattleTeam.Ally)
        currentWaveAllyDefenseDamage +=
            previousHp - defenseLineController.GetCurrentHp(defenseTeam);
    NotifyDefenseLineHp(defenseTeam);

    if (!defenseLineController.IsDestroyed(defenseTeam)) return;
    BeginWaveResolution(defenseTeam == EBattleTeam.Enemy
        ? EWaveResolutionResult.Cleared
        : EWaveResolutionResult.Failed);
}
```

Delete the `OnBattleRosterChanged` resolution subscription. Unit death changes the roster only; it never resolves a wave.

- [ ] **Step 6: Restore delayed clear, retry, victory, and defeat transitions**

`BeginWaveResolution` must:

- begin `WaveResolutionState` only from `Active`;
- consume one chance only for `Failed`;
- change state to `Resolving`;
- emit chance change after failure;
- emit `OnWaveResolutionStarted` and `OnWaveResolved`;
- play clear/fail SFX;
- start the resolution coroutine;
- never call `AddGold`.

Implement it as:

```csharp
private void BeginWaveResolution(EWaveResolutionResult result)
{
    if (State != EWaveState.Active ||
        !waveResolution.TryBegin(
            result,
            CurrentWaveNumber,
            Time.time,
            waveResolutionDuration)) return;

    if (result == EWaveResolutionResult.Failed)
    {
        runState.ConsumeChance();
        OnHpChanged?.Invoke(PlayerHp);
    }

    float duration = Mathf.Max(0f, Time.time - currentWaveStartedAt);
    unitManager.StopBattle();
    ChangeState(EWaveState.Resolving);
    OnWaveResolutionStarted?.Invoke(result, CurrentWaveNumber);
    OnWaveResolved?.Invoke(new BattleWaveResolvedData(
        CurrentWaveNumber,
        result,
        duration,
        currentWaveAllyDefenseDamage,
        IsCurrentWaveBoss));
    SoundManager.PlaySFXIfAvailable(
        result == EWaveResolutionResult.Cleared
            ? SoundName.WaveWin
            : SoundName.WaveFailed);
    waveResolutionCoroutine = StartCoroutine(WaitForWaveResolution());
}

private IEnumerator WaitForWaveResolution()
{
    yield return new WaitForSeconds(waveResolutionDuration);
    waveResolutionCoroutine = null;
    FinishWaveResolution();
}
```

Expose line state and events with these exact signatures:

```csharp
public event Action<EBattleTeam, int, int> OnDefenseLineHpChanged;

public int GetDefenseLineHp(EBattleTeam team) =>
    defenseLineController?.GetCurrentHp(team) ?? 20;

public int GetDefenseLineMaximumHp(EBattleTeam team) =>
    defenseLineController?.GetMaximumHp(team) ?? 20;

private void NotifyDefenseLineHp(EBattleTeam team)
{
    OnDefenseLineHpChanged?.Invoke(
        team,
        GetDefenseLineHp(team),
        GetDefenseLineMaximumHp(team));
}

private void NotifyAllDefenseLineHp()
{
    NotifyDefenseLineHp(EBattleTeam.Ally);
    NotifyDefenseLineHp(EBattleTeam.Enemy);
}
```

Use this completion order:

```csharp
private void FinishWaveResolution()
{
    if (State != EWaveState.Resolving || !waveResolution.IsPending) return;

    EWaveResolutionResult result = waveResolution.Result;
    bool isFinalWave = runState.CurrentWaveIndex + 1 >= runState.TotalWaveCount;
    unitManager.ResolveWaveResult();
    waveResolution.Clear();

    EWaveState nextState = BattleResolutionPolicy.ResolveNextState(
        result,
        isFinalWave,
        PlayerHp);
    if (result == EWaveResolutionResult.Cleared &&
        nextState == EWaveState.Pending)
    {
        runState.AdvanceWave();
        OnWaveChanged?.Invoke(CurrentWaveNumber);
    }

    ChangeState(nextState);
    if (nextState is EWaveState.Victory or EWaveState.Defeat)
        OnRunEnded?.Invoke(CurrentWaveNumber);
}
```

Rename `ResolveStageResult` to `ResolveWaveResult`:

```csharp
public void ResolveWaveResult()
{
    ReturnAllEnemies();
    RestoreAlliesForPreparation();
}
```

Rename `RestoreAlliesForStage` to `RestoreAlliesForPreparation` without changing its existing iteration, pooling, saved-position, item-modifier, or roster-event behavior. Dead allies remain removed from ownership, preserving the current replacement economy.

- [ ] **Step 7: Redirect defense HP upgrades away from player chances**

Replace `runState.IncreaseMaximumPlayerHp` with:

```csharp
int increase = Mathf.RoundToInt(defenseLineHpSettings.EffectPerLevel);
if (defenseLineController.IncreaseAllyMaximumHp(increase))
{
    NotifyDefenseLineHp(EBattleTeam.Ally);
}
```

Keep ally attack upgrades unchanged.

- [ ] **Step 8: Add wave event payloads for metrics consumers**

Replace stage payloads with:

```csharp
public readonly struct BattleWaveStartedData
{
    public int Wave { get; }
    public bool IsBoss { get; }
    public int SpawnedEnemyCount { get; }

    public BattleWaveStartedData(int wave, bool isBoss, int spawnedEnemyCount)
    {
        Wave = wave;
        IsBoss = isBoss;
        SpawnedEnemyCount = spawnedEnemyCount;
    }
}

public readonly struct BattleWaveResolvedData
{
    public int Wave { get; }
    public EWaveResolutionResult Result { get; }
    public float Duration { get; }
    public int AllyDefenseLineDamage { get; }
    public bool IsBoss { get; }

    public BattleWaveResolvedData(
        int wave,
        EWaveResolutionResult result,
        float duration,
        int allyDefenseLineDamage,
        bool isBoss)
    {
        Wave = wave;
        Result = result;
        Duration = duration;
        AllyDefenseLineDamage = allyDefenseLineDamage;
        IsBoss = isBoss;
    }
}
```

Declare `public event Action<BattleWaveStartedData> OnWaveStarted;` and
`public event Action<BattleWaveResolvedData> OnWaveResolved;` next to the
existing battle events.

Events are `Action<BattleWaveStartedData> OnWaveStarted` and `Action<BattleWaveResolvedData> OnWaveResolved`.

Update `PrototypeMetricsController` in the same task so the event rename never breaks compilation:

- `CurrentStageElapsed` becomes `CurrentWaveElapsed`;
- `CurrentStage` becomes `CurrentWave`;
- `CurrentStageRetryCount` becomes `CurrentWaveRetryCount`;
- `LastStageDuration` becomes `LastWaveDuration`;
- `recentStages` becomes `recentWaves`;
- subscriptions use `OnWaveStarted` and `OnWaveResolved`;
- `BattleWaveResolvedData.AllyDefenseLineDamage` feeds line-damage metrics;
- a failed result increments current and total retry counts.

Update `PrototypeMetricsDisplayController` labels and property reads from `STAGE` to `WAVE`. Keep ball, gold, jackpot, clone, purchase, retry, boss-reach, and boss-defeat metrics unchanged.

- [ ] **Step 9: Run battle domain tests**

Run filters `BattleRunStateTests|WaveResolutionTests|BattleDefenseLineControllerTests|BattleEconomyTests|UnitPurchaseControllerTests|TacticalReinforcementControllerTests`, results `finite-wave-task4-green.xml`. Expected: PASS.

- [ ] **Step 10: Commit Task 4**

```powershell
git add -- 'Assets/02. Scripts/Battle/BattleManager.cs' 'Assets/02. Scripts/Battle/UnitManager.cs' 'Assets/02. Scripts/Battle/Units/UnitSpawnController.cs' 'Assets/02. Scripts/Battle/Editor/BattleEconomyTests.cs' 'Assets/02. Scripts/PrototypeMetricsController.cs' 'Assets/02. Scripts/03. UI/PrototypeMetricsDisplayController.cs'
git commit -m "feat(battle): restore manual fixed waves"
```

---

### Task 5: Restore wave HUD, wave controls, and result art

**Files:**
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/StatusFeedbackController.cs`
- Create: `Assets/02. Scripts/03. UI/StatusWaveHudController.cs`
- Create: `Assets/02. Scripts/03. UI/StatusWaveHudController.cs.meta`
- Modify: `Assets/02. Scripts/03. UI/WaveResultPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/ResultPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/GameSpeedController.cs`
- Modify: `Assets/02. Scripts/03. UI/ShopPanel.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Create: `Assets/02. Scripts/03. UI/Editor/WaveHudStateTests.cs`
- Create: `Assets/02. Scripts/03. UI/Editor/WaveHudStateTests.cs.meta`
- Modify: `Assets/02. Scripts/03. UI/Editor/WaveResultPanelTests.cs`

**Interfaces:**
- Consumes: finite `EWaveState`, one-based `OnWaveChanged`, chance and defense-line events from Task 4.
- Produces: manual start button, 10-node HUD, chance/line formatting, outcome popup, victory/defeat art with restart/title controls.

- [ ] **Step 1: Restore pure HUD tests and add status-format tests**

Create `WaveHudStateTests` with meta GUID `fe0d40584be24b744a95df7ad608a230` and these exact cases:

```csharp
[TestCase(1, 1, EWaveHudNodeState.Current)]
[TestCase(1, 2, EWaveHudNodeState.Locked)]
[TestCase(2, 1, EWaveHudNodeState.Complete)]
[TestCase(5, 5, EWaveHudNodeState.Elite05)]
[TestCase(9, 9, EWaveHudNodeState.Elite09)]
[TestCase(10, 10, EWaveHudNodeState.Boss10)]
public void ResolveNodeState_ReturnsExpectedState(
    int currentWave,
    int nodeWave,
    EWaveHudNodeState expected)
{
    Assert.That(
        new WaveHudState().ResolveNodeState(currentWave, nodeWave),
        Is.EqualTo(expected));
}

[TestCase(1, 1, false)]
[TestCase(2, 1, true)]
[TestCase(10, 9, true)]
public void IsConnectorComplete_CompletesOnlyBeforeCurrentWave(
    int currentWave,
    int connectorAfterWave,
    bool expected)
{
    Assert.That(
        new WaveHudState().IsConnectorComplete(
            currentWave,
            connectorAfterWave),
        Is.EqualTo(expected));
}

[TestCase(10, true)]
[TestCase(9, false)]
[TestCase(11, false)]
public void IsSupportedWaveCount_AcceptsExactlyTen(
    int waveCount,
    bool expected)
{
    Assert.That(
        new WaveHudState().IsSupportedWaveCount(waveCount),
        Is.EqualTo(expected));
}
```

Add static formatting tests:

```csharp
[Test]
public void FormatChances_LabelsPlayerHpAsChances()
{
    Assert.That(StatusPanel.FormatChances(2, 3), Is.EqualTo("기회 2/3"));
}

[Test]
public void FormatDefenseLines_ShowsBothTeams()
{
    Assert.That(
        StatusPanel.FormatDefenseLines(12, 20, 7, 20),
        Is.EqualTo("아군 12/20 | 적 7/20"));
}
```

Update `WaveResultPanelTests` to expect `웨이브 클리어` for `Cleared` and `방어 실패` for `Failed`.

- [ ] **Step 2: Run UI tests and confirm missing HUD/status APIs**

Run filters `WaveHudStateTests|WaveResultPanelTests`, results `finite-wave-task5-red.xml`. Expected: FAIL until restored UI code compiles.

- [ ] **Step 3: Restore only the manual wave start control**

`WavePanel` must subscribe to `BattleManager.OnStateChanged`, assign `startButton.onClick` to `TryStartWave`, and refresh as follows:

```csharp
private void Refresh()
{
    bool show = battleManager.State == EWaveState.Pending;
    startButton.gameObject.SetActive(show);
    startButton.interactable = show && battleManager.CanStartCurrentWave;
    if (launchButton != null) launchButton.gameObject.SetActive(false);
    if (launchCostText != null) launchCostText.gameObject.SetActive(false);
}
```

Do not restore `PinballManager` launch subscriptions or `WaveButtonStateController` launch logic.

- [ ] **Step 4: Restore the 10-node HUD and display chances/line HP**

Add this pure state model to `StatusPanel.cs`:

```csharp
public enum EWaveHudNodeState
{
    Idle,
    Locked,
    Current,
    Complete,
    Elite05,
    Elite09,
    Boss10
}

public sealed class WaveHudState
{
    public EWaveHudNodeState ResolveNodeState(int currentWave, int nodeWave)
    {
        if (nodeWave < currentWave) return EWaveHudNodeState.Complete;
        if (nodeWave > currentWave) return EWaveHudNodeState.Locked;
        return nodeWave switch
        {
            5 => EWaveHudNodeState.Elite05,
            9 => EWaveHudNodeState.Elite09,
            10 => EWaveHudNodeState.Boss10,
            _ => EWaveHudNodeState.Current
        };
    }

    public bool IsConnectorComplete(int currentWave, int connectorAfterWave) =>
        connectorAfterWave < currentWave;

    public bool IsSupportedWaveCount(int waveCount) => waveCount == 10;
}
```

Recreate `StatusWaveHudController` using meta GUID `d79429fe0ce049d49aed8241a6505c32`. It owns arrays of 10 node Images and 9 connector Images, the seven node sprites, two connector sprites, and one `WaveHudState`. `ValidateReferences()` requires every array slot and sprite. `Display(int currentWave)` assigns every node from `ResolveNodeState` and every connector from `IsConnectorComplete`; sprite mapping is Current, Complete, Elite05, Elite09, Boss10, Locked, then Idle fallback.

`StatusPanel` must:

- validate exactly 10 nodes, 9 connectors, and all legacy sprites;
- display the one-based current wave directly;
- format `playerHpText` using `FormatChances`;
- repurpose the current `stageText` reference as `defenseLineText` and format both line HP pairs;
- subscribe to `OnDefenseLineHpChanged` and refresh only the affected values;
- keep gold feedback unchanged;
- make legacy wave node/connector objects active instead of hiding them.

Use these formatters:

```csharp
public static string FormatChances(int current, int maximum) =>
    $"기회 {Mathf.Max(0, current)}/{Mathf.Max(1, maximum)}";

public static string FormatDefenseLines(
    int allyCurrent,
    int allyMaximum,
    int enemyCurrent,
    int enemyMaximum) =>
    $"아군 {Mathf.Max(0, allyCurrent)}/{Mathf.Max(1, allyMaximum)} | " +
    $"적 {Mathf.Max(0, enemyCurrent)}/{Mathf.Max(1, enemyMaximum)}";
```

Rename `StatusFeedbackController`'s stage-specific field/method to defense-line feedback and pulse it when either line HP changes. Do not change HP/gold animation values.

- [ ] **Step 5: Restore both wave outcome popups**

Remove the current early return that suppresses `Cleared` in `WaveResultPanel`. Restore copies to `웨이브 클리어` and `방어 실패`; keep its current tween implementation.

- [ ] **Step 6: Merge legacy result art with current buttons**

Restore the legacy text/art serialized fields and state-driven sprite selection from `Dev`. Keep `restartButton` and `titleButton`. Subscribe to `OnStateChanged` instead of only `OnRunEnded`.

```csharp
private void OnBattleStateChanged(EWaveState state)
{
    if (state is not EWaveState.Victory and not EWaveState.Defeat)
    {
        gameObject.SetActive(false);
        return;
    }

    bool victory = state == EWaveState.Victory;
    titleText.text = victory ? victoryTitle : defeatTitle;
    messageText.text = victory ? victoryMessage : defeatMessage;
    overlayImage.sprite = victory ? victoryOverlaySprite : defeatOverlaySprite;
    titleImage.sprite = victory ? victoryTitleSprite : defeatTitleSprite;
    iconImage.sprite = victory ? victoryIconSprite : defeatIconSprite;
    buttonAccentImage.sprite = victory
        ? victoryButtonAccentSprite
        : defeatButtonAccentSprite;
    gameObject.SetActive(true);
    transform.SetAsLastSibling();
}
```

Keep restart loading `ESceneName.Game` and title loading `ESceneName.Title`.

- [ ] **Step 7: Update remaining finite-state consumers**

Use `EWaveState.Pending` in `GameSpeedController` fallback and `ShopPanel` preparation refresh. Do not restore `GameLayoutController` or `BattleCameraController` state-based sliding.

After `StatusPanel` uses `CurrentWaveNumber` and `IsCurrentWaveBoss`, delete the transitional `CurrentStageNumber` and `IsCurrentStageBoss` aliases from `BattleManager`.

- [ ] **Step 8: Run focused UI tests**

Run filters `WaveHudStateTests|WaveResultPanelTests`, results `finite-wave-task5-green.xml`. Expected: PASS.

- [ ] **Step 9: Commit Task 5**

```powershell
git add -- 'Assets/02. Scripts/03. UI/WavePanel.cs' 'Assets/02. Scripts/03. UI/StatusPanel.cs' 'Assets/02. Scripts/03. UI/StatusFeedbackController.cs' 'Assets/02. Scripts/03. UI/StatusWaveHudController.cs' 'Assets/02. Scripts/03. UI/StatusWaveHudController.cs.meta' 'Assets/02. Scripts/03. UI/WaveResultPanel.cs' 'Assets/02. Scripts/03. UI/ResultPanel.cs' 'Assets/02. Scripts/03. UI/GameSpeedController.cs' 'Assets/02. Scripts/03. UI/ShopPanel.cs' 'Assets/02. Scripts/03. UI/Editor/WaveHudStateTests.cs' 'Assets/02. Scripts/03. UI/Editor/WaveHudStateTests.cs.meta' 'Assets/02. Scripts/03. UI/Editor/WaveResultPanelTests.cs' 'Assets/02. Scripts/Battle/BattleManager.cs'
git commit -m "feat(ui): restore finite wave feedback"
```

---

### Task 6: Wire two defense lines and restored UI in the game scene

**Files:**
- Modify: `Assets/01. Scenes/02. Game.unity`
- Modify: `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/DefenseLineSceneTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/DefenseLineSceneTests.cs.meta`
- Modify: `Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs`

**Interfaces:**
- Consumes: `DefenseLineTrigger.DefenseTeam`, `UnitManager.allyDefenseLine/enemyDefenseLine`, restored UI fields.
- Produces: scene-placed `AllyDefenseLine` at `(6, -1, 0)`, `EnemyDefenseLine` at `(-6, -1, 0)`, fully wired HUD/results/start controls.

- [ ] **Step 1: Write scene wiring tests before editing YAML**

Create `DefenseLineSceneTests`:

```csharp
[Test]
public void GameScene_WiresOneDefenseLinePerTeam()
{
    EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
    DefenseLineTrigger[] lines = Object.FindObjectsByType<DefenseLineTrigger>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);

    Assert.That(lines, Has.Length.EqualTo(2));
    Assert.That(lines.Count(line => line.DefenseTeam == EBattleTeam.Ally),
        Is.EqualTo(1));
    Assert.That(lines.Count(line => line.DefenseTeam == EBattleTeam.Enemy),
        Is.EqualTo(1));

    UnitManager manager = Object.FindFirstObjectByType<UnitManager>();
    AssertReference(manager, "allyDefenseLine");
    AssertReference(manager, "enemyDefenseLine");
}
```

Add `using System.Linq;`, `using UnityEditor;`, and `using UnityEditor.SceneManagement;` at the top of `DefenseLineSceneTests.cs`.

Use this helper in `DefenseLineSceneTests` and meta GUID `ab31c58ab1de4dd5b908ae8f6222a39c`:

```csharp
private static void AssertReference(Object target, string propertyName)
{
    var property = new SerializedObject(target).FindProperty(propertyName);
    Assert.That(property, Is.Not.Null, propertyName);
    Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
}
```

Expand `GameplayFeedbackSceneTests` to assert:

- `WavePanel.startButton` exists;
- `StatusPanel` has 10 non-null `waveNodes`, 9 non-null `waveConnectors`, and every legacy wave sprite reference;
- `ResultPanel` has title/message, overlay/title/icon/button images, all eight result sprites, restart button, and title button.

- [ ] **Step 2: Run scene tests and confirm missing references**

Run filters `DefenseLineSceneTests|GameplayFeedbackSceneTests|AllyPurchaseUiSceneTests`, results `finite-wave-task6-red.xml`. Expected: FAIL because the scene has one defense line and missing restored UI references.

- [ ] **Step 3: Replace the current line with two explicit scene objects**

Edit only the relevant YAML blocks:

- rename current `DefenseLine` to `AllyDefenseLine`;
- retain position `{x: 6, y: -1, z: 0}` and BoxCollider2D trigger;
- add `defenseTeam: 0` to its trigger;
- duplicate the Transform/MonoBehaviour/BoxCollider2D structure as `EnemyDefenseLine`;
- place enemy line at `{x: -6, y: -1, z: 0}`;
- set `defenseTeam: 1`;
- assign the two trigger component file IDs to `UnitManager.allyDefenseLine` and `enemyDefenseLine`;
- add both line Transforms under the existing battle-root parent.

Use new file IDs in the `9000000000000000011` through `9000000000000000014` range so they do not collide with the existing ally line IDs.

- [ ] **Step 4: Apply BattleManager and UI serialized values**

Set:

```yaml
playerMaxHp: 3
allyDefenseLineMaxHp: 20
enemyDefenseLineMaxHp: 20
waveResolutionDuration: 2
```

Remove all infinite-stage serialized keys listed in the spec.

Restore `StatusPanel` wave sprite references and arrays from the `Dev` scene block, keeping the current gold, chance, and defense-line text objects. Restore `ResultPanel` art/image/sprite references from `Dev`, then keep current restart/title button references. Ensure the start button object remains active in scene; runtime state controls visibility.

Do not alter automatic pinball objects, production upgrade UI, purchase cards, tactical notice, metrics panel, prefabs, or unrelated scene file IDs.

- [ ] **Step 5: Run scene tests**

Run filters `DefenseLineSceneTests|GameplayFeedbackSceneTests|AllyPurchaseUiSceneTests`, results `finite-wave-task6-green.xml`. Expected: PASS.

- [ ] **Step 6: Commit Task 6**

```powershell
git add -- 'Assets/01. Scenes/02. Game.unity' 'Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs' 'Assets/02. Scripts/Battle/Editor/DefenseLineSceneTests.cs' 'Assets/02. Scripts/Battle/Editor/DefenseLineSceneTests.cs.meta' 'Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs'
git commit -m "feat(scene): wire dual defense-line waves"
```

---

### Task 7: Remove infinite-stage implementation and compatibility code

**Files:**
- Delete: `Assets/02. Scripts/Battle/Runtime/BattleStageController.cs`
- Delete: `Assets/02. Scripts/Battle/Runtime/BattleStageController.cs.meta`
- Delete: `Assets/02. Scripts/Battle/Runtime/EnemyStageScalingController.cs`
- Delete: `Assets/02. Scripts/Battle/Runtime/EnemyStageScalingController.cs.meta`
- Delete: `Assets/02. Scripts/Battle/Editor/BattleStageControllerTests.cs`
- Delete: `Assets/02. Scripts/Battle/Editor/BattleStageControllerTests.cs.meta`
- Modify: `Assets/02. Scripts/00. Core/Enum.cs`
- Modify: `Assets/02. Scripts/Battle/Runtime/BattleRunState.cs`
- Modify: `Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs`
- Modify: `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs`

**Interfaces:**
- Consumes: finite event consumers already migrated in Tasks 4 and 5.
- Produces: no runtime, enum, policy, test, or scene references to the infinite-stage implementation.

- [ ] **Step 1: Delete infinite-stage files and direct references**

Delete the two runtime controllers, their meta files, and `BattleStageControllerTests`. Do not delete tactical reinforcement files or tests.

Reduce `EWaveState` to the final `Pending`, `Active`, `Resolving`, `Victory`, and `Defeat` set. Remove the transitional one-argument `BattleRunState` constructor, `ApplyPlayerDamage`, `RestorePlayerHp`, and `IncreaseMaximumPlayerHp`; then change `MaximumPlayerHp` to a getter-only property. Remove `BattleResolutionPolicy.TryDetectWipe`; keep only `ResolveNextState`.

- [ ] **Step 2: Run stale-symbol scans**

```powershell
rg -n 'BattleStageController|EnemyStageScalingController|CurrentStageNumber|IsCurrentStageBoss|OnStageStarted|OnStageResolved|BattleStageStartedData|BattleStageResolvedData|stageTransitionDuration|bossStageInterval|baseEnemyCount|maximumEnemyCount' Assets
```

Expected: no matches. `docs/` historical records may retain old names and are not edited.

Run:

```powershell
rg -n 'TacticalReinforcementController|HasTacticalReinforcement|OnTacticalReinforcementChanged' Assets
```

Expected: runtime, UI, and test matches remain.

- [ ] **Step 3: Run metrics and retained-feature tests**

Run filters `GameplayFeedbackSceneTests|TacticalReinforcementControllerTests|UnitPurchaseControllerTests|PinballAutoCycleControllerTests|PinballProductionUpgradeControllerTests`, results `finite-wave-task7-green.xml`. Expected: PASS.

- [ ] **Step 4: Commit Task 7**

```powershell
git add -A -- 'Assets/02. Scripts/00. Core/Enum.cs' 'Assets/02. Scripts/Battle/Runtime/BattleRunState.cs' 'Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs' 'Assets/02. Scripts/Battle/Runtime/BattleStageController.cs' 'Assets/02. Scripts/Battle/Runtime/BattleStageController.cs.meta' 'Assets/02. Scripts/Battle/Runtime/EnemyStageScalingController.cs' 'Assets/02. Scripts/Battle/Runtime/EnemyStageScalingController.cs.meta' 'Assets/02. Scripts/Battle/Editor/BattleStageControllerTests.cs' 'Assets/02. Scripts/Battle/Editor/BattleStageControllerTests.cs.meta' 'Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs'
git commit -m "refactor(battle): remove infinite stage loop"
```

---

### Task 8: Write removal inventory, AI record, and run final gates

**Files:**
- Create: `docs/plans/2026-08-21/2026-08-21-temp-branch-removal-inventory.md`
- Create: `docs/ai-usage/2026-08-21/2026-08-21-finite-defense-line-wave-restoration-ai-usage.md`
- Modify: only files required to fix failures proven by final task tests.

**Interfaces:**
- Consumes: final `Dev...HEAD` Git diff, commit history, all previous task results.
- Produces: actionable future-removal inventory and factual AI usage record.

- [ ] **Step 1: Generate source evidence for the inventory**

Run:

```powershell
git diff --name-status --find-renames Dev...HEAD
git log --reverse --format='%h`t%s' Dev..HEAD
git diff --stat Dev...HEAD
```

Classify every remaining difference under these concrete feature groups:

1. automatic pinball circulation and split permanent/clone pooling;
2. production upgrades and production UI;
3. combo multiplier, golden ball, jackpot, and feedback;
4. combat upgrades;
5. dual defense-line fixed wave flow from this task;
6. prototype metrics;
7. selectable ally purchases and per-unit ownership;
8. tactical reinforcement;
9. economy and enemy-stat balance changes;
10. result, status, and purchase UI changes;
11. still-removed Dev behaviors: manual pinball launch, goal-pocket ally creation, camera/panel sliding, and legacy tutorial compatibility.

- [ ] **Step 2: Write the removal inventory as an executable cleanup map**

Use this document structure for every group:

```markdown
## 기능명

- 상태: 유지 / 이번 작업에서 대체 / Dev 대비 계속 삭제 상태
- 관련 커밋: `<hash> <subject>`
- 추가 파일: 정확한 경로 목록
- 수정 파일과 심볼: `path` — `Class.Method`
- 씬·프리팹·데이터: 정확한 오브젝트 또는 필드
- 의존 기능: 먼저 유지하거나 제거해야 하는 기능
- 제거 순서: 테스트 → UI/씬 참조 → 런타임 연결 → 도메인 파일 → 데이터/문서
- 검증: 제거 후 실행할 기존 테스트 클래스와 `rg` 검색어
```

Mark infinite continuous stages as replaced by fixed dual-line waves. Mark tactical reinforcement as retained by explicit user decision.

- [ ] **Step 3: Run the complete EditMode suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\finite-wave-full-editmode.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\finite-wave-full-editmode.log'
```

Expected: Unity exits 0 and XML reports zero failed tests. If a failure is unrelated to the task, record the exact test and error without changing unrelated code.

- [ ] **Step 4: Run final static gates**

```powershell
git diff --check
rg -n 'RetryGoldReward|WaveClearGoldReward|FinalClearGoldReward|BattleStageController|EnemyStageScalingController' Assets
rg -n 'TacticalReinforcementController|HasTacticalReinforcement' Assets
git status --short
```

Expected:

- `git diff --check`: no output;
- removed reward/infinite-stage scan: no output;
- tactical reinforcement scan: expected runtime and tests remain;
- status: only planned documentation or final test-fix files remain uncommitted.

- [ ] **Step 5: Write the factual AI usage record**

Record:

- AI tool/model used;
- user's requested finite wave, dual defense lines, three chances, retained tactical reinforcement, and removed rewards;
- proposed architecture;
- exact code, scene, test, data, and documentation files modified;
- user-decided values: 10 waves, 3 chances, 20 HP per line, ally line upgrade +10;
- exact focused and full test commands with pass/fail counts;
- any Unity scene visual checks not performed;
- remaining limitations.

- [ ] **Step 6: Commit documentation and any proven final fixes**

```powershell
git add -- 'docs/plans/2026-08-21/2026-08-21-temp-branch-removal-inventory.md' 'docs/ai-usage/2026-08-21/2026-08-21-finite-defense-line-wave-restoration-ai-usage.md'
git commit -m "docs: record finite wave restoration"
```

- [ ] **Step 7: Confirm clean handoff state**

Run:

```powershell
git status --short --branch
git log -8 --oneline
```

Expected: no uncommitted planned changes; recent commits correspond to Tasks 1-8.
