# Battle Domain Object-Oriented Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve the current pinball-to-battle gameplay while splitting the battle domain into focused, testable objects and removing only code proven unused by C#, Unity serialization, and JSON data.

**Architecture:** Keep `BattleManager`, `UnitManager`, `UnitSpawner`, and `UnitBase` as thin Unity-facing coordinators. Move wave state, economy, roster, creation, placement, merge, targeting, health, status, movement, attack, and skill rules into constructor-injected plain C# objects. Preserve current event order and public behavior until every caller has migrated, then remove compatibility code.

**Tech Stack:** Unity 6000.0.79f1, C#, NUnit Edit Mode tests, Unity Test Framework 1.6.0, URP 17.3.0, PC WebGL, existing JSON data under `Assets/Resources/Data`.

## Global Constraints

- Preserve all current gameplay rules, JSON fields, balance values, event timing, and scene-visible behavior.
- Inspector rewiring and API changes are allowed only when the final behavior remains equivalent.
- Keep all new runtime code under `Assets/02. Scripts/Battle`; do not create a new repository or Unity top-level folder.
- Do not install or replace packages.
- Keep `[SerializeField]` names without a leading underscore.
- Use `*Manager` only for a major feature coordinator; use focused domain nouns or `*Service` for extracted rules approved by the design.
- Keep core components scene/prefab placed; do not introduce runtime component creation as the initialization strategy.
- Preserve SetActive-based pooling.
- Do not stage or commit the user's existing changes to `SceneManager.cs`, `ArcaneVfxCatalog.asset`, `ShaderGraphSettings.asset`, or the three untracked Korean guidance documents.
- Before each commit, run the focused test, `dotnet build`, and `git diff --check` for that task.
- Unity must generate and track a `.meta` file for every new asset before the task commit.

---

## Planned File Structure

### Battle run rules

- Create `Assets/02. Scripts/Battle/Runtime/BattleEconomy.cs`: gold balance, spend, and reward behavior.
- Create `Assets/02. Scripts/Battle/Runtime/BattleRunState.cs`: wave index, player HP, and wave state.
- Create `Assets/02. Scripts/Battle/Runtime/BarrierDamageCalculator.cs`: pure breach damage calculation.
- Modify `Assets/02. Scripts/Battle/BattleManager.cs`: delegate state and economy rules while preserving its public events.

### Unit collection and use cases

- Create `Assets/02. Scripts/Battle/Units/UnitRoster.cs`: owned allies and active team lists.
- Create `Assets/02. Scripts/Battle/Units/UnitTargetFinder.cs`: closest, farthest, highest-HP, radius, and line queries.
- Create `Assets/02. Scripts/Battle/Units/IUnitDataSource.cs`: narrow data lookup contract implemented by `TitleData`.
- Create `Assets/02. Scripts/Battle/Units/UnitStatsValidator.cs`: stat validity rules.
- Create `Assets/02. Scripts/Battle/Units/UnitCreationService.cs`: ally and enemy stat creation.
- Create `Assets/02. Scripts/Battle/Units/BattleUnitModifiers.cs`: battle item and diversity modifiers.
- Create `Assets/02. Scripts/Battle/Units/UnitPlacementService.cs`: preparation positions and free grid slots.
- Create `Assets/02. Scripts/Battle/Units/UnitMergeDecision.cs`: explicit merge result value types.
- Create `Assets/02. Scripts/Battle/Units/UnitMergeService.cs`: merge reservations and evolution selection.
- Modify `Assets/02. Scripts/Battle/UnitManager.cs`: compose and expose the extracted unit use cases.
- Modify `Assets/02. Scripts/02. Data/TitleData.cs`: implement `IUnitDataSource` without changing JSON loading.

### Per-unit runtime

- Create `Assets/02. Scripts/Battle/Units/UnitHealth.cs`: HP, shield, healing, and death result.
- Create `Assets/02. Scripts/Battle/Units/UnitDamageResult.cs`: immutable damage result.
- Create `Assets/02. Scripts/Battle/Units/UnitStatusEffects.cs`: timed combat multipliers and control effects.
- Create `Assets/02. Scripts/Battle/Units/UnitEffectScheduler.cs`: delayed slow and replacement damage-over-time scheduling.
- Create `Assets/02. Scripts/Battle/Units/UnitMovement.cs`: next-position and knockback calculation.
- Create `Assets/02. Scripts/Battle/Units/UnitAttack.cs`: attack cooldown calculation.
- Create `Assets/02. Scripts/Battle/Units/UnitCombatContext.cs`: explicit target finder, battle bounds, and death callback dependencies.
- Modify `Assets/02. Scripts/Battle/UnitBase.cs`: Unity Actor coordinating the focused runtime objects.
- Modify `Assets/02. Scripts/Battle/UnitSpawner.cs`: inject `UnitCombatContext` into spawned and pooled units.

### Skills

- Create `Assets/02. Scripts/Battle/Skills/IUnitSkill.cs`: base and trigger-specific skill contracts.
- Create `Assets/02. Scripts/Battle/Skills/IEnemyBattleActions.cs`: narrow reinforcement and enemy-team buff actions.
- Create `Assets/02. Scripts/Battle/Skills/UnitSkillContext.cs`: caster, primary target, target finder, roster actions, and reusable target buffer.
- Create `Assets/02. Scripts/Battle/Skills/UnitSkillValueReader.cs`: safe effect-value and percent conversion.
- Create `Assets/02. Scripts/Battle/Skills/UnitSkillRegistry.cs`: ID-to-factory registration.
- Create `Assets/02. Scripts/Battle/Skills/Ally/AllySkillController.cs`: mana and active-skill timing.
- Create eight ally skill files under `Assets/02. Scripts/Battle/Skills/Ally`.
- Create `Assets/02. Scripts/Battle/Skills/Enemy/EnemySkillController.cs`: enemy trigger dispatch and per-spawn skill state.
- Create twelve enemy skill files under `Assets/02. Scripts/Battle/Skills/Enemy`.
- Modify `Assets/02. Scripts/Battle/AlllyUnit.cs`: preparation input and ally skill-controller delegation.
- Modify `Assets/02. Scripts/Battle/EnemyUnit.cs`: enemy trigger delegation.

### Tests and verification

- Create focused test files under `Assets/02. Scripts/Battle/Editor` for every extracted rule object.
- Modify `Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs`: test `UnitPlacementService` rather than `UnitManager` private methods.
- Create `Assets/02. Scripts/Battle/Editor/BattleSceneWiringTests.cs`: required scene and prefab references.
- Create `Assets/02. Scripts/Battle/Editor/BattleRefactorBuild.cs`: reusable command-line WebGL Development Build entry point.
- Modify `.github/ai-use-log.md`: factual record of the completed refactor and verification.

## Verification Command Template

Run focused Edit Mode tests from the repository root in an isolated worktree. Replace only the concrete test class and output file names shown in each task.

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'ConcreteTestClass' -testResults (Join-Path $unityProject 'Temp\ConcreteTestClass.xml') -logFile (Join-Path $unityProject 'Temp\ConcreteTestClass.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Compile after Unity has imported new files:

```powershell
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
```

Expected successful compile: exit code `0`, `0 Error(s)`. Existing package-reference warnings may remain but must not increase because of this refactor.

---

### Task 1: Lock Existing Data and Deployment Rules

**Files:**
- Create: `Assets/02. Scripts/Battle/Editor/BattleDataCharacterizationTests.cs`
- Verify: `Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs`
- Verify: `Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs`

**Interfaces:**
- Consumes: `AllyUnitData.CreateStats(int level, int classLevel)`, `EnemyUnitData.CreateStats(int wave, EnemyCommonData common)`, existing deployment-limit methods.
- Produces: numeric characterization tests that every later task must keep green.

- [ ] **Step 1: Run the existing full Edit Mode suite and save the baseline result**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testResults (Join-Path $unityProject 'Temp\BattleRefactorBaseline.xml') -logFile (Join-Path $unityProject 'Temp\BattleRefactorBaseline.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Expected: all currently discovered Edit Mode tests pass. Record the exact test count from `BattleRefactorBaseline.xml` in the task notes; do not copy an assumed count.

- [ ] **Step 2: Add ally and enemy stat characterization tests**

```csharp
#if UNITY_EDITOR
using NUnit.Framework;

public class BattleDataCharacterizationTests
{
    [Test]
    public void AllyCreateStats_AppliesGrowthFromBaseLevel()
    {
        var data = new AllyUnitData
        {
            previousJob = string.Empty,
            health = 180,
            attack = 18,
            defense = 10,
            moveSpeed = 2.5f,
            attackSpeed = 0.85f,
            attackRange = 1.1f,
            mana = 0,
            healthGrowth = 24,
            attackGrowth = 3,
            defenseGrowth = 2,
            attackSpeedGrowth = 0.03f
        };

        BattleUnitStats stats = data.CreateStats(3, 5);

        Assert.That(stats.MaxHp, Is.EqualTo(228f));
        Assert.That(stats.AttackDamage, Is.EqualTo(24f));
        Assert.That(stats.Defense, Is.EqualTo(14f));
        Assert.That(stats.MoveSpeed, Is.EqualTo(2.5f));
        Assert.That(stats.AttackRate, Is.EqualTo(0.91f).Within(0.0001f));
        Assert.That(stats.AttackRange, Is.EqualTo(1.1f));
        Assert.That(stats.MaxMana, Is.Zero);
    }

    [Test]
    public void EnemyCreateStats_AppliesWaveGrowthAndFlooring()
    {
        var common = new EnemyCommonData
        {
            baseWave = 1,
            healthGrowthPerWave = 0.1f,
            attackGrowthPerWave = 0.2f,
            defenseGrowthInterval = 2,
            defenseGrowthValue = 3,
            moveSpeedGrowthPerWave = 0.05f,
            attackSpeedGrowthPerWave = 0.1f
        };
        var data = new EnemyUnitData
        {
            health = 101,
            attack = 11,
            defense = 4,
            moveSpeed = 2f,
            attackSpeed = 1f,
            attackRange = 1.5f
        };

        BattleUnitStats stats = data.CreateStats(3, common);

        Assert.That(stats.MaxHp, Is.EqualTo(121f));
        Assert.That(stats.AttackDamage, Is.EqualTo(15f));
        Assert.That(stats.Defense, Is.EqualTo(7f));
        Assert.That(stats.MoveSpeed, Is.EqualTo(2.2f).Within(0.0001f));
        Assert.That(stats.AttackRate, Is.EqualTo(1.2f).Within(0.0001f));
        Assert.That(stats.AttackRange, Is.EqualTo(1.5f));
    }
}
#endif
```

- [ ] **Step 3: Run the new characterization test**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'BattleDataCharacterizationTests' -testResults (Join-Path $unityProject 'Temp\BattleDataCharacterizationTests.xml') -logFile (Join-Path $unityProject 'Temp\BattleDataCharacterizationTests.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Expected: `2` tests pass.

- [ ] **Step 4: Compile and inspect whitespace**

```powershell
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleDataCharacterizationTests.cs'
```

Expected: build exit `0`; `git diff --check` prints nothing.

- [ ] **Step 5: Commit the baseline tests**

```powershell
git add -- 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleDataCharacterizationTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleDataCharacterizationTests.cs.meta'
git commit -m 'test: characterize battle data rules'
```

---

### Task 2: Extract Battle Run State, Economy, and Barrier Damage

**Files:**
- Create: `Assets/02. Scripts/Battle/Runtime/BattleEconomy.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/BattleRunState.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/BarrierDamageCalculator.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleEconomyTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BarrierDamageCalculatorTests.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs:14-240`

**Interfaces:**
- Consumes: `IReadOnlyList<BattleWaveData>`, starting gold, maximum HP, existing `EWaveState`.
- Produces: `BattleEconomy.TrySpend(int)`, `BattleEconomy.Add(int)`, `BattleRunState.ChangeState(EWaveState)`, `BattleRunState.AdvanceWave()`, `BattleRunState.ApplyPlayerDamage(int)`, `BarrierDamageCalculator.Calculate(int,int,int)`.

- [ ] **Step 1: Write failing tests for exact economy behavior**

```csharp
#if UNITY_EDITOR
using NUnit.Framework;

public class BattleEconomyTests
{
    [Test]
    public void TrySpend_ClampsNegativeAmountToSuccessfulZeroSpend()
    {
        var economy = new BattleEconomy(100);
        Assert.That(economy.TrySpend(-10), Is.True);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }

    [Test]
    public void TrySpend_RejectsInsufficientBalanceWithoutMutation()
    {
        var economy = new BattleEconomy(100);
        Assert.That(economy.TrySpend(101), Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }

    [Test]
    public void Add_IgnoresNonPositiveReward()
    {
        var economy = new BattleEconomy(100);
        economy.Add(0);
        economy.Add(-20);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }
}
#endif
```

- [ ] **Step 2: Write failing tests for run state and barrier damage**

```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class BattleRunStateTests
{
    [Test]
    public void AdvanceWave_MovesIndexUntilLastWaveOnly()
    {
        var waves = new List<BattleWaveData>
        {
            new(),
            new()
        };
        var state = new BattleRunState(waves, true, 20);

        Assert.That(state.AdvanceWave(), Is.True);
        Assert.That(state.CurrentWaveIndex, Is.EqualTo(1));
        Assert.That(state.AdvanceWave(), Is.False);
        Assert.That(state.CurrentWaveIndex, Is.EqualTo(1));
    }

    [Test]
    public void ApplyPlayerDamage_ClampsHpAtZero()
    {
        var state = new BattleRunState(new[] { new BattleWaveData() }, true, 20);
        Assert.That(state.ApplyPlayerDamage(30), Is.True);
        Assert.That(state.PlayerHp, Is.Zero);
    }
}

public class BarrierDamageCalculatorTests
{
    [TestCase(8, 3, 1, 5)]
    [TestCase(2, 5, 1, 1)]
    [TestCase(0, 0, 1, 1)]
    public void Calculate_AppliesReductionAndMinimum(
        int breach,
        int reduction,
        int minimum,
        int expected)
    {
        Assert.That(
            BarrierDamageCalculator.Calculate(breach, reduction, minimum),
            Is.EqualTo(expected));
    }
}
#endif
```

- [ ] **Step 3: Run the tests and confirm RED**

Run the Verification Command Template once with `ConcreteTestClass` set to `BattleEconomyTests` and once with it set to `BattleRunStateTests`.

Expected: compilation fails because `BattleEconomy`, `BattleRunState`, and `BarrierDamageCalculator` do not exist.

- [ ] **Step 4: Implement `BattleEconomy`**

```csharp
using System;

public sealed class BattleEconomy
{
    public int Gold { get; private set; }

    public BattleEconomy(int startingGold)
    {
        Gold = Math.Max(0, startingGold);
    }

    public bool TrySpend(int amount)
    {
        int safeAmount = Math.Max(0, amount);
        if (safeAmount == 0) return true;
        if (Gold < safeAmount) return false;
        Gold -= safeAmount;
        return true;
    }

    public bool Add(int amount)
    {
        if (amount <= 0) return false;
        Gold += amount;
        return true;
    }
}
```

- [ ] **Step 5: Implement `BattleRunState` with mutation-result methods**

```csharp
using System;
using System.Collections.Generic;

public sealed class BattleRunState
{
    private readonly IReadOnlyList<BattleWaveData> _waves;
    private readonly bool _hasValidRun;

    public int CurrentWaveIndex { get; private set; }
    public int CurrentWaveNumber => CurrentWaveIndex + 1;
    public int TotalWaveCount => _waves?.Count ?? 0;
    public int PlayerHp { get; private set; }
    public EWaveState State { get; private set; } = EWaveState.Pending;
    public bool HasValidCurrentWave =>
        _hasValidRun &&
        _waves != null &&
        CurrentWaveIndex >= 0 &&
        CurrentWaveIndex < _waves.Count &&
        _waves[CurrentWaveIndex] != null;
    public BattleWaveData CurrentWave =>
        HasValidCurrentWave ? _waves[CurrentWaveIndex] : null;

    public BattleRunState(
        IReadOnlyList<BattleWaveData> waves,
        bool hasValidRun,
        int maximumHp)
    {
        _waves = waves ?? Array.Empty<BattleWaveData>();
        _hasValidRun = hasValidRun;
        PlayerHp = Math.Max(1, maximumHp);
    }

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

    public bool ApplyPlayerDamage(int amount)
    {
        int nextHp = Math.Max(0, PlayerHp - Math.Max(0, amount));
        if (nextHp == PlayerHp) return false;
        PlayerHp = nextHp;
        return true;
    }
}
```

- [ ] **Step 6: Implement the pure barrier calculator**

```csharp
using System;

public static class BarrierDamageCalculator
{
    public static int Calculate(
        int totalBreachDamage,
        int damageReduction,
        int minimumDamage)
    {
        return Math.Max(
            Math.Max(1, minimumDamage),
            Math.Max(0, totalBreachDamage) - Math.Max(0, damageReduction));
    }
}
```

- [ ] **Step 7: Run the focused rule tests and confirm GREEN**

Run the Verification Command Template for `BattleEconomyTests`, `BattleRunStateTests`, and `BarrierDamageCalculatorTests`.

Expected: `3`, `2`, and `3` cases pass respectively.

- [ ] **Step 8: Replace `BattleManager` fields with the extracted objects**

Keep the existing public properties and events. Change their implementations to delegate:

```csharp
public BattleWaveData CurrentWave => _runState?.CurrentWave;
public int CurrentWaveNumber => _runState?.CurrentWaveNumber ?? 1;
public int TotalWaveCount => _runState?.TotalWaveCount ?? 0;
public int PlayerHp => _runState?.PlayerHp ?? playerMaxHp;
public int Gold => _economy?.Gold ?? 0;
public EWaveState State => _runState?.State ?? EWaveState.Pending;

private BattleRunState _runState;
private BattleEconomy _economy;
private int _barrierDamageReduction;
private int _minimumBarrierDamage = 1;
```

Construct both objects in `Start` after `TitleData` is available. Preserve initialization event order exactly: `OnInitialized`, `OnStateChanged`, `OnHpChanged`, `OnGoldChanged`, `OnWaveChanged`.

- [ ] **Step 9: Route state, economy, and defeat calculations through the new objects**

Use these exact rules:

```csharp
public bool TrySpendGold(int amount)
{
    int previousGold = _economy.Gold;
    if (!_economy.TrySpend(amount)) return false;
    if (_economy.Gold != previousGold)
    {
        OnGoldChanged?.Invoke(_economy.Gold);
    }
    return true;
}

public void AddGold(int amount)
{
    if (!_economy.Add(amount)) return;
    OnGoldChanged?.Invoke(_economy.Gold);
}
```

In `DefeatWave`, calculate damage with `BarrierDamageCalculator.Calculate`, call `_runState.ApplyPlayerDamage`, and emit `OnHpChanged` once with the new value. In `CompleteWave`, call `_runState.AdvanceWave` before `OnWaveChanged`. In `ChangeState`, emit only when `_runState.ChangeState` returns `true`.

- [ ] **Step 10: Run BattleManager-related tests and compile**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'Battle' -testResults (Join-Path $unityProject 'Temp\BattleRuntimeTests.xml') -logFile (Join-Path $unityProject 'Temp\BattleRuntimeTests.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle'
```

Expected: focused Battle tests pass; compile has `0 Error(s)`; diff check is empty.

- [ ] **Step 11: Commit the battle runtime extraction**

```powershell
git add -- 'pin-ball/Assets/02. Scripts/Battle/Runtime' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleEconomyTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleEconomyTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/BarrierDamageCalculatorTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BarrierDamageCalculatorTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/BattleManager.cs'
git commit -m 'refactor: extract battle run rules'
```

---

### Task 3: Extract Unit Roster and Target Queries

**Files:**
- Create: `Assets/02. Scripts/Battle/Units/UnitRoster.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitTargetFinder.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitTargetFinderTests.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:18-37,252-353,631-803`

**Interfaces:**
- Consumes: `AllyUnit`, `EnemyUnit`, `UnitBase`, current actor state and transform positions.
- Produces: `UnitRoster.OwnedAllies`, active counts and snapshots; `UnitTargetFinder` query methods used by `UnitManager` compatibility delegates and later skills.

- [ ] **Step 1: Write failing roster tests**

```csharp
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class UnitRosterTests
{
    private GameObject _allyObject;
    private GameObject _enemyObject;

    [TearDown]
    public void TearDown()
    {
        if (_allyObject != null) Object.DestroyImmediate(_allyObject);
        if (_enemyObject != null) Object.DestroyImmediate(_enemyObject);
    }

    [Test]
    public void AddOwnedAlly_RegistersOwnedAndActiveOnce()
    {
        _allyObject = new GameObject("ally");
        var ally = _allyObject.AddComponent<AllyUnit>();
        var roster = new UnitRoster();

        Assert.That(roster.AddOwnedAlly(ally), Is.True);
        Assert.That(roster.AddOwnedAlly(ally), Is.False);
        Assert.That(roster.OwnedAllyCount, Is.EqualTo(1));
        Assert.That(roster.ActiveAllyCount, Is.EqualTo(1));
    }

    [Test]
    public void RemoveUnit_RemovesOwnedAllyFromBothLists()
    {
        _allyObject = new GameObject("ally");
        var ally = _allyObject.AddComponent<AllyUnit>();
        var roster = new UnitRoster();
        roster.AddOwnedAlly(ally);

        Assert.That(roster.RemoveUnit(ally), Is.True);
        Assert.That(roster.OwnedAllyCount, Is.Zero);
        Assert.That(roster.ActiveAllyCount, Is.Zero);
    }
}
#endif
```

- [ ] **Step 2: Write failing closest, line, and highest-HP target tests**

Create a top-level test-only `UnitTargetTestUnit : UnitBase` in `UnitTargetFinderTests.cs` with a configurable team and empty `Tick`. Create target GameObjects at `(1,0)`, `(2,0)`, and `(2,2)`. Use reflection only in the test helper to set the `CurrentHp` backing field until Task 7 gives `UnitHealth` a direct test seam.

Assert these exact results:

```csharp
Assert.That(finder.FindClosestAliveEnemy(Vector3.zero, 10f), Is.SameAs(near));
Assert.That(finder.FindFarthestAliveAlly(Vector3.zero), Is.SameAs(far));
Assert.That(finder.FindHighestHpAliveAlly(), Is.SameAs(highestHp));
Assert.That(lineTargets, Is.EqualTo(new[] { near, farInLine }));
```

- [ ] **Step 3: Run both tests and confirm RED**

Run the Verification Command Template for `UnitRosterTests` and `UnitTargetFinderTests`.

Expected: compilation fails because the two extracted types do not exist.

- [ ] **Step 4: Implement `UnitRoster` as the only owner of unit lists**

Expose these exact members:

```csharp
public IReadOnlyList<AllyUnit> OwnedAllies { get; }
public IReadOnlyList<UnitBase> ActiveAllies { get; }
public IReadOnlyList<UnitBase> ActiveEnemies { get; }
public int OwnedAllyCount { get; }
public int ActiveAllyCount { get; }
public int ActiveEnemyCount { get; }
public bool AddOwnedAlly(AllyUnit ally);
public bool AddActiveAlly(AllyUnit ally);
public bool AddEnemy(UnitBase enemy);
public bool NotifyUnitDied(UnitBase unit);
public bool RemoveUnit(UnitBase unit);
public void CleanupDestroyedUnits();
public UnitBase[] DrainEnemies();
public void ClearActiveAllies();
```

Return `false` when an input is null or would not change membership. `NotifyUnitDied` removes allies only from the active list and enemies only from the active list, matching current behavior.

- [ ] **Step 5: Implement `UnitTargetFinder` against a constructor-injected roster**

Expose the current query surface without Unity scene lookup:

```csharp
public sealed class UnitTargetFinder
{
    public UnitTargetFinder(UnitRoster roster);
    public UnitBase FindClosestAliveEnemy(Vector3 fromPosition, float maxDistance);
    public UnitBase FindClosestAliveAlly(Vector3 fromPosition, float maxDistance);
    public UnitBase FindFarthestAliveAlly(Vector3 fromPosition);
    public UnitBase FindHighestHpAliveAlly();
    public void GetAliveEnemiesInRadius(Vector3 center, float radius, List<UnitBase> result);
    public void GetAliveAlliesInRadius(Vector3 center, float radius, List<UnitBase> result);
    public void GetEnemiesInLine(Vector3 origin, Vector3 direction, float distance, float halfWidth, List<UnitBase> result);
}
```

Move the existing algorithms without changing comparison operators, fallback direction, distance sorting, or null/dead filtering.

- [ ] **Step 6: Run roster and target tests and confirm GREEN**

Run the Verification Command Template for `UnitRosterTests` and `UnitTargetFinderTests`.

Expected: all new roster and target cases pass.

- [ ] **Step 7: Compose both objects in `UnitManager.Awake`**

```csharp
private UnitRoster _roster;
private UnitTargetFinder _targetFinder;

protected override void Awake()
{
    base.Awake();
    _spawner = GetComponent<UnitSpawner>();
    _roster = new UnitRoster();
    _targetFinder = new UnitTargetFinder(_roster);
}
```

Delegate existing properties and methods so UI, pinball, `UnitBase`, `AllyUnit`, and `EnemyUnit` compile unchanged during this task.

- [ ] **Step 8: Replace list mutation and query bodies with roster/finder calls**

Move `AddOwnedAlly`, `AddEnemy`, `NotifyUnitDied`, `ReleaseUnit`, enemy draining, ally-active restoration, and every target query to the extracted objects. Keep `OnDeployedAllyCountChanged` in `UnitManager` and emit it only when `UnitRoster.AddOwnedAlly` or `UnitRoster.RemoveUnit` changes the owned count.

- [ ] **Step 9: Run affected existing tests and compile**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'Unit' -testResults (Join-Path $unityProject 'Temp\UnitRosterIntegration.xml') -logFile (Join-Path $unityProject 'Temp\UnitRosterIntegration.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle'
```

Expected: unit and deployment tests pass; compile has `0 Error(s)`.

- [ ] **Step 10: Commit roster and targeting extraction**

```powershell
git add -- 'pin-ball/Assets/02. Scripts/Battle/Units/UnitRoster.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitRoster.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitTargetFinder.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitTargetFinder.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitTargetFinderTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitTargetFinderTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs'
git commit -m 'refactor: extract unit roster and targeting'
```

---

### Task 4: Extract Unit Creation and Battle Item Modifiers

**Files:**
- Create: `Assets/02. Scripts/Battle/Units/IUnitDataSource.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitStatsValidator.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitCreationService.cs`
- Create: `Assets/02. Scripts/Battle/Units/BattleUnitModifiers.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitCreationServiceTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleUnitModifiersTests.cs`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs:20,192-257`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:84-250,805-856`

**Interfaces:**
- Consumes: ally/enemy JSON models through `IUnitDataSource`, `BattleUnitSpawnData`, `Item`.
- Produces: validated ally/enemy stat creation and deterministic modifier/duplication decisions.

- [ ] **Step 1: Define the data-source contract in failing tests**

```csharp
public interface IUnitDataSource
{
    AllyCommonData AllyCommon { get; }
    EnemyCommonData EnemyCommon { get; }
    bool TryGetAllyUnit(string id, out AllyUnitData result);
    bool TryGetEnemyUnit(string id, out EnemyUnitData result);
    bool TryGetRootAllyJob(string unitId, out AllyUnitData rootJob);
    void GetNextAllyJobs(string previousJobId, List<AllyUnitData> result);
}
```

In `UnitCreationServiceTests`, implement `FakeUnitDataSource` with dictionaries and fixed common data. Write tests for base ally stats, advanced-class minimum level, merge/equipment bonuses, temporary charged-pin attack bonus, missing unit ID, and enemy wave stats.

- [ ] **Step 2: Write failing modifier tests**

Test these exact outcomes:

```csharp
var modifiers = new BattleUnitModifiers();
modifiers.Apply(EItem.AttackManual, 0.1f, 0f, 0f);
modifiers.Apply(EItem.BattleClock, 0.12f, 0f, 0f);
modifiers.Apply(EItem.FieldArmor, 0.15f, 0f, 0f);
UnitModifierSnapshot snapshot = modifiers.GetRosterSnapshot(5);
Assert.That(snapshot.AttackMultiplier, Is.EqualTo(1.1f));
Assert.That(snapshot.AttackRateMultiplier, Is.EqualTo(1.12f));
Assert.That(snapshot.HpMultiplier, Is.EqualTo(1.15f));
```

Also assert that duplication returns false for the wrong merge tier, false when roll is greater than chance, and true when roll is equal to chance.

- [ ] **Step 3: Run both test classes and confirm RED**

Run the Verification Command Template for `UnitCreationServiceTests` and `BattleUnitModifiersTests`.

Expected: compilation fails because the contracts and services do not exist.

- [ ] **Step 4: Implement `IUnitDataSource` and make `TitleData` implement it**

Change only the class declaration:

```csharp
public class TitleData : AppService, IUnitDataSource
```

Existing public properties and lookup methods already satisfy the interface. Do not change JSON paths, validation, or collection construction.

- [ ] **Step 5: Implement `UnitStatsValidator` and `UnitCreationService`**

```csharp
public static class UnitStatsValidator
{
    public static bool IsValid(BattleUnitStats stats)
    {
        return stats.MaxHp > 0f &&
               stats.AttackDamage >= 0f &&
               stats.AttackRate > 0f &&
               stats.AttackRange > 0f &&
               stats.MoveSpeed >= 0f;
    }
}
```

Expose:

```csharp
public UnitCreationService(IUnitDataSource dataSource);
public bool TryCreateAlly(
    BattleUnitSpawnData spawnData,
    float temporaryAttackBonus,
    out AllyUnitData allyData,
    out BattleUnitStats stats);
public bool TryCreateEnemy(
    string enemyId,
    int wave,
    out EnemyUnitData enemyData,
    out BattleUnitStats stats);
```

Move `TryBuildUnitStats` exactly, including mutation of `spawnData.Level`, class minimum level, merge-tier multipliers, equipment additions, and final validation. Apply `temporaryAttackBonus` after merge/equipment calculations.

- [ ] **Step 6: Implement `BattleUnitModifiers` with immutable snapshots**

```csharp
public readonly struct UnitModifierSnapshot
{
    public float AttackMultiplier { get; }
    public float AttackRateMultiplier { get; }
    public float HpMultiplier { get; }

    public UnitModifierSnapshot(float attack, float attackRate, float hp)
    {
        AttackMultiplier = attack;
        AttackRateMultiplier = attackRate;
        HpMultiplier = hp;
    }
}
```

Expose `Apply(EItem key, float value1, float value2, float value3)`, `GetRosterSnapshot(int distinctUnitTypeCount)`, and `ShouldDuplicate(BattleUnitSpawnData data, float randomRoll, out int count)`. Preserve the current `1 + item.Value1` conversions and diversity maximum cap.

- [ ] **Step 7: Run creation and modifier tests and confirm GREEN**

Run the Verification Command Template for `UnitCreationServiceTests` and `BattleUnitModifiersTests`.

Expected: all explicit creation, validation, snapshot, and duplication cases pass.

- [ ] **Step 8: Integrate both services into `UnitManager`**

Construct `UnitCreationService` in `Start` after `TitleData` is available. Construct `BattleUnitModifiers` in `Awake`. Replace `TryBuildUnitStats`, `IsValidStats`, enemy stat creation, item fields, and the item `switch` with delegates. Continue using `UnityEngine.Random.value` only at the `UnitManager.TryDuplicateAlly` boundary and pass the roll into `ShouldDuplicate`.

- [ ] **Step 9: Preserve roster modifier refresh semantics**

Count distinct active ally types with the existing `ally.name` value, obtain one `UnitModifierSnapshot`, and call `ApplyItemModifiers` on every active ally. Preserve HP ratio when maximum HP changes.

- [ ] **Step 10: Run focused tests, compile, and commit**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'UnitCreationServiceTests' -testResults (Join-Path $unityProject 'Temp\UnitCreationServiceTests.xml') -logFile (Join-Path $unityProject 'Temp\UnitCreationServiceTests.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle' 'pin-ball/Assets/02. Scripts/02. Data/TitleData.cs'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Units/IUnitDataSource.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/IUnitDataSource.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitStatsValidator.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitStatsValidator.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitCreationService.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitCreationService.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/BattleUnitModifiers.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/BattleUnitModifiers.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitCreationServiceTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitCreationServiceTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleUnitModifiersTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleUnitModifiersTests.cs.meta' 'pin-ball/Assets/02. Scripts/02. Data/TitleData.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs'
git commit -m 'refactor: extract unit creation and modifiers'
```

---

### Task 5: Extract Preparation Placement

**Files:**
- Create: `Assets/02. Scripts/Battle/Units/UnitPlacementService.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitPlacementServiceTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs:88-113`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:23-24,353-418,571-621`

**Interfaces:**
- Consumes: `BattleAreaBounds`, `AllyUnit`, collider-derived padding.
- Produces: one owner of saved preparation positions and free-grid placement.

- [ ] **Step 1: Write failing placement-service tests**

Move the existing `IsGridPositionOccupied_UsesMinimumDistance` test to target `UnitPlacementService`. Add tests that `Remove` frees a saved position and that `TryGetSavedPosition` returns the exact saved vector.

Use this required surface:

```csharp
public UnitPlacementService(BattleAreaBounds battleArea);
public bool IsValidPlacement(AllyUnit ally, Vector3 position);
public bool TrySave(AllyUnit ally, Vector3 position);
public bool TryGetSavedPosition(AllyUnit ally, out Vector3 position);
public bool TryPlaceInFreeGridSlot(AllyUnit ally);
public void Remove(AllyUnit ally);
public static float GetPadding(AllyUnit ally);
```

- [ ] **Step 2: Run `UnitPlacementServiceTests` and confirm RED**

Run the Verification Command Template for `UnitPlacementServiceTests`.

Expected: compilation fails because `UnitPlacementService` does not exist.

- [ ] **Step 3: Implement the placement service**

Move `_allyPreparationPositions`, `TryPlaceAllyInFreeGridSlot`, `IsGridPositionOccupied`, and `GetAllyPlacementPadding` into the new class. Keep the exact `padding * 2f + 0.15f` minimum distance and horizontal-first grid order supplied by `BattleAreaBounds`.

- [ ] **Step 4: Run placement tests and confirm GREEN**

Run the Verification Command Template for `UnitPlacementServiceTests` and `AllyPreparationPlacementTests`.

Expected: saved-position tests and all existing boundary/grid cases pass.

- [ ] **Step 5: Integrate into `UnitManager`**

Construct the service in `Awake` with serialized `battleArea`. Keep `CanDragAlly` in `UnitManager` because it combines battle preparation state, roster membership, and merge reservation. Delegate `IsValidAllyPlacement`, `SaveAllyPreparationPosition`, spawn placement, release removal, merge result position storage, and wave restoration.

- [ ] **Step 6: Preserve restoration behavior**

During `RestoreAlliesForPreparation`, clear only active roster membership. For each owned ally, use its saved position or allocate a new free grid slot, then call `RestoreForPreparation` and `ResetMana` before adding it back to the active roster.

- [ ] **Step 7: Run focused tests, compile, and commit**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'Placement' -testResults (Join-Path $unityProject 'Temp\PlacementTests.xml') -logFile (Join-Path $unityProject 'Temp\PlacementTests.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Units/UnitPlacementService.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitPlacementService.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitPlacementServiceTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitPlacementServiceTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/AllyPreparationPlacementTests.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs'
git commit -m 'refactor: extract ally placement service'
```

---

### Task 6: Extract Merge and Evolution Decisions

**Files:**
- Create: `Assets/02. Scripts/Battle/Units/UnitMergeDecision.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitMergeService.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitMergeServiceTests.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:21-22,49-52,381-569,623-629`

**Interfaces:**
- Consumes: `IUnitDataSource`, `AllyUnit` identity/level/position, maximum and class levels.
- Produces: explicit rejected, immediate, and evolution-required decisions; owned merge reservations; evolution completion.

- [ ] **Step 1: Write failing decision tests**

Define these exact result types in the tests:

```csharp
public enum UnitMergeDecisionType
{
    Rejected,
    Immediate,
    EvolutionRequired
}
```

Test rejection for null/same units, different root jobs, and maximum level. Test an immediate base merge below class level. Test two base units reaching class level return exactly two sorted evolution choices. Test advanced units keep the advanced job ID. Test an invalid evolution ID leaves the pending decision intact.

- [ ] **Step 2: Run `UnitMergeServiceTests` and confirm RED**

Run the Verification Command Template for `UnitMergeServiceTests`.

Expected: compilation fails because merge decision types do not exist.

- [ ] **Step 3: Implement immutable decision values**

`UnitMergeDecision` must expose:

```csharp
public UnitMergeDecisionType Type { get; }
public AllyUnit Source { get; }
public AllyUnit Target { get; }
public string ResultUnitId { get; }
public int ResultLevel { get; }
public Vector3 ResultPosition { get; }
public AllyUnitData FirstChoice { get; }
public AllyUnitData SecondChoice { get; }
public bool RestoreSourcePosition { get; }
```

Provide these named factory methods; do not expose setters:

```csharp
public static UnitMergeDecision Rejected(bool restoreSourcePosition);
public static UnitMergeDecision Immediate(
    AllyUnit source,
    AllyUnit target,
    string resultUnitId,
    int resultLevel,
    Vector3 resultPosition);
public static UnitMergeDecision EvolutionRequired(
    AllyUnit source,
    AllyUnit target,
    int resultLevel,
    Vector3 resultPosition,
    AllyUnitData firstChoice,
    AllyUnitData secondChoice);
```

In `UnitMergeServiceTests`, create lightweight `AllyUnit` GameObjects and use reflection only in the test helper to set `<UnitId>k__BackingField` and `<Level>k__BackingField`. Do not add production setters solely for tests.

- [ ] **Step 4: Implement `UnitMergeService`**

Expose:

```csharp
public UnitMergeService(IUnitDataSource dataSource);
public bool IsReserved(AllyUnit ally);
public UnitMergeDecision TryBegin(AllyUnit source, AllyUnit target);
public bool TryChooseEvolution(string unitId, out UnitMergeDecision decision);
public void Complete(UnitMergeDecision decision);
public void CancelPendingEvolution();
```

Move current root-job validation, highest-level increment, `GetMergeResultJobId`, exactly-two-candidate validation, and pending evolution state. Preserve the current special restoration behavior: set `RestoreSourcePosition` only when reservation succeeded but the candidate count is not two.

- [ ] **Step 5: Run merge tests and confirm GREEN**

Run the Verification Command Template for `UnitMergeServiceTests`.

Expected: all rejection, immediate, evolution, reservation, and invalid-choice cases pass.

- [ ] **Step 6: Integrate merge decisions into `UnitManager`**

Keep `TryMergeAllies` and `ChooseEvolution` as Facade methods. `TryMergeAllies` performs preparation/roster checks, asks the service for a decision, then:

- returns false for `Rejected`, restoring `sourceOriginalPosition` only when requested;
- consumes and spawns immediately for `Immediate`;
- hides/reserves both inputs, locks preparation, and emits `OnEvolutionRequested` for `EvolutionRequired`.

`ChooseEvolution` validates through the service, consumes both inputs, spawns the result at the stored position, completes the decision, unlocks preparation, and returns true.

- [ ] **Step 7: Add a cleanup path for manager destruction**

Before unregistering `UnitManager`, cancel pending evolution and clear the preparation lock only when `BattleManager` still exists. This prevents hidden reserved allies from leaving a stale lock during scene teardown.

- [ ] **Step 8: Run merge, deployment, and placement tests**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'Merge' -testResults (Join-Path $unityProject 'Temp\MergeTests.xml') -logFile (Join-Path $unityProject 'Temp\MergeTests.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
```

Expected: merge tests pass and compile has `0 Error(s)`.

- [ ] **Step 9: Commit merge extraction**

```powershell
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Units/UnitMergeDecision.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitMergeDecision.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitMergeService.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitMergeService.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitMergeServiceTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitMergeServiceTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs'
git commit -m 'refactor: extract unit merge service'
```

---

### Task 7: Extract Health, Status Effects, and Timed Effect Scheduling

**Files:**
- Create: `Assets/02. Scripts/Battle/Units/UnitDamageResult.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitHealth.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitStatusEffects.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitEffectScheduler.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitHealthTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitStatusEffectsTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitEffectSchedulerTests.cs`
- Modify later in Task 8: `Assets/02. Scripts/Battle/UnitBase.cs`

**Interfaces:**
- Consumes: maximum HP, current time supplied by caller, raw damage, defense, armor ignore, effect values.
- Produces: deterministic health and timed-effect objects with no `MonoBehaviour`, coroutine, or global time lookup.

- [ ] **Step 1: Write failing health tests**

Test these exact rules: defense formula floors damage; armor ignore is clamped; damage reduction applies after defense; shield absorbs final damage; stronger/longer shield uses maximum amount and expiry; healing caps at maximum HP; maximum HP rescaling preserves current ratio; damage cannot reduce HP below zero.

```csharp
UnitDamageResult result = health.TakeDamage(
    incomingDamage: 100f,
    defense: 100f,
    armorIgnoreRatio: 0f,
    damageReduction: 0f,
    now: 0f);
Assert.That(result.AppliedDamage, Is.EqualTo(50f));
Assert.That(health.CurrentHp, Is.EqualTo(50f));
```

- [ ] **Step 2: Write failing status-effect tests**

Apply attack rate, attack damage, defense, movement, stun, damage reduction, and knockback immunity at known times. Assert the current value before expiration and the exact neutral value at `now >= expiresAt`. Assert a lower damage-reduction application cannot replace a stronger active one.

- [ ] **Step 3: Write failing scheduler tests**

Assert damage-over-time uses `ceil(duration)` ticks, first tick occurs after `duration / tickCount`, a new DOT replaces the old version, and delayed slow fires once after its delay. Advance the scheduler with explicit `now` values; do not use `WaitForSeconds` in these tests.

- [ ] **Step 4: Run all three classes and confirm RED**

Run the Verification Command Template for `UnitHealthTests`, `UnitStatusEffectsTests`, and `UnitEffectSchedulerTests`.

Expected: compilation fails because the runtime types do not exist.

- [ ] **Step 5: Implement immutable damage results**

```csharp
public readonly struct UnitDamageResult
{
    public float AppliedDamage { get; }
    public float AbsorbedDamage { get; }
    public bool Died { get; }

    public UnitDamageResult(float appliedDamage, float absorbedDamage, bool died)
    {
        AppliedDamage = appliedDamage;
        AbsorbedDamage = absorbedDamage;
        Died = died;
    }
}
```

- [ ] **Step 6: Implement `UnitHealth`**

Expose this exact surface:

```csharp
public float CurrentHp { get; }
public float MaxHp { get; }
public float LastDamagedTime { get; }
public float HpRatio { get; }
public bool IsDead { get; }
public void Reset(float maximumHp);
public void MarkDead();
public UnitDamageResult TakeDamage(
    float incomingDamage,
    float defense,
    float armorIgnoreRatio,
    float damageReduction,
    float now);
public void Heal(float amount);
public void ApplyShield(float amount, float duration, float now);
public void Refresh(float now);
public void ScaleMaximumHp(float multiplier);
```

Copy the current calculation order exactly: subclass modifier occurs before this object is called; defense and flooring; damage reduction; shield; HP and death.

- [ ] **Step 7: Implement `UnitStatusEffects`**

Expose neutral defaults and explicit application methods:

```csharp
public float AttackRateMultiplier { get; }
public float AttackDamageMultiplier { get; }
public float DefenseMultiplier { get; }
public float MoveSpeedMultiplier { get; }
public float DamageReduction { get; }
public bool IsStunned(float now);
public bool IsKnockbackImmune(float now);
public void ApplyAttackRateMultiplier(float multiplier, float duration, float now);
public void ApplyAttackDamageMultiplier(float multiplier, float duration, float now);
public void ApplyDefenseMultiplier(float multiplier, float duration, float now);
public void ApplyMoveSpeedMultiplier(float multiplier, float duration, float now);
public void ApplyDamageReduction(float ratio, float duration, float now);
public void ApplyStun(float duration, float now);
public void ApplyKnockbackImmunity(float duration, float now);
public void Refresh(float now);
public void Reset();
```

Preserve each current clamping rule and `Math.Max(existingExpiry, now + duration)` behavior.

- [ ] **Step 8: Implement `UnitEffectScheduler`**

Use one replaceable DOT state and a list of delayed slows with this exact surface:

```csharp
public void ScheduleDamageOverTime(
    float totalDamage,
    float duration,
    float armorIgnoreRatio,
    float now);
public void ScheduleSlow(
    float moveSpeedMultiplier,
    float attackRateMultiplier,
    float duration,
    float delay,
    float now);
public void Tick(
    float now,
    Action<float, float> applyDamage,
    Action<float, float, float> applySlow);
public void Reset();
```

The damage callback receives damage-per-tick and armor-ignore ratio. The slow callback receives move multiplier, attack-rate multiplier, and duration. `Tick` applies at most one DOT tick per Unity frame and schedules the next tick from the actual application time, matching coroutine drift rather than catching up multiple ticks in one frame.

- [ ] **Step 9: Run runtime tests and confirm GREEN**

Run the Verification Command Template for the three new test classes.

Expected: health, status, and scheduler cases pass without a scene.

- [ ] **Step 10: Compile and commit the standalone runtime objects**

```powershell
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle/Units'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Units/UnitDamageResult.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitDamageResult.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitHealth.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitHealth.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitStatusEffects.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitStatusEffects.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitEffectScheduler.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitEffectScheduler.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitHealthTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitHealthTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitStatusEffectsTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitStatusEffectsTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitEffectSchedulerTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitEffectSchedulerTests.cs.meta'
git commit -m 'refactor: add unit health and status rules'
```

---

### Task 8: Extract Movement and Attack, Then Slim `UnitBase`

**Files:**
- Create: `Assets/02. Scripts/Battle/Units/UnitMovement.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitAttack.cs`
- Create: `Assets/02. Scripts/Battle/Units/UnitCombatContext.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitMovementTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitAttackTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/UnitPoolResetTests.cs`
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs:8-536`
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs:24-119`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs` composition and spawn calls

**Interfaces:**
- Consumes: `UnitTargetFinder`, `BattleAreaBounds`, `Action<UnitBase>` death callback, explicit `now` and `deltaTime`.
- Produces: `UnitBase.Initialize(BattleUnitStats, UnitCombatContext)` and a reset-safe pooled Actor with no `App.Get<T>()`.

- [ ] **Step 1: Write failing movement and attack tests**

```csharp
[Test]
public void CalculateNextPosition_MovesTowardTargetBySpeedTimesDelta()
{
    Vector3 result = UnitMovement.CalculateNextPosition(
        Vector3.zero,
        Vector3.right * 10f,
        2f,
        0.5f);
    Assert.That(result, Is.EqualTo(Vector3.right));
}

[Test]
public void TrySchedule_UsesInverseAttackRate()
{
    var attack = new UnitAttack();
    Assert.That(attack.TrySchedule(5f, 2f), Is.True);
    Assert.That(attack.NextAttackTime, Is.EqualTo(5.5f));
    Assert.That(attack.TrySchedule(5.25f, 2f), Is.False);
}
```

- [ ] **Step 2: Write a failing pooled-reset integration test**

Create a test `UnitBase` subclass, initialize it with deterministic stats and a `UnitCombatContext`, apply damage and every timed effect, force a target, then call `MarkReturnedToPool` followed by `RestoreForPreparation`. Assert full HP, idle state, null target, neutral multipliers, no scheduled DOT/slow, and `IsInPool == false`.

Use reflection only inside the test helper to inspect `_currentTarget`, the composed status object, and the scheduler's pending-state properties. Do not add public production API solely for this assertion.

- [ ] **Step 3: Run the three tests and confirm RED**

Run the Verification Command Template for `UnitMovementTests`, `UnitAttackTests`, and `UnitPoolResetTests`.

Expected: new types or the new initialization signature are absent.

- [ ] **Step 4: Implement movement and attack rules**

`UnitMovement.CalculateNextPosition` uses `Vector2.MoveTowards` exactly. `UnitMovement.ApplyKnockback` returns unchanged position when immune or direction magnitude is at most `0.001f`; otherwise it adds normalized direction times distance. `UnitAttack` owns only `NextAttackTime`, `Reset`, and `TrySchedule(now, effectiveAttackRate)` with a minimum rate of `0.01f`.

- [ ] **Step 5: Implement explicit combat context**

```csharp
using System;

public sealed class UnitCombatContext
{
    public UnitTargetFinder TargetFinder { get; }
    public BattleAreaBounds BattleArea { get; }
    public Action<UnitBase> NotifyUnitDied { get; }

    public UnitCombatContext(
        UnitTargetFinder targetFinder,
        BattleAreaBounds battleArea,
        Action<UnitBase> notifyUnitDied)
    {
        TargetFinder = targetFinder ?? throw new ArgumentNullException(nameof(targetFinder));
        BattleArea = battleArea ?? throw new ArgumentNullException(nameof(battleArea));
        NotifyUnitDied = notifyUnitDied ?? throw new ArgumentNullException(nameof(notifyUnitDied));
    }
}
```

- [ ] **Step 6: Replace `UnitBase` state fields with composed objects**

Keep public properties and subclass hooks stable. Delegate HP, ratio, damage time, status multipliers, stun, shield, movement, attack scheduling, delayed effects, and pool reset to the new objects. `UnitBase.Update` supplies `Time.time` and `Time.deltaTime`; no extracted object reads global time.

- [ ] **Step 7: Remove `App.Get<UnitManager>()` from `UnitBase`**

Change initialization to:

```csharp
public void Initialize(BattleUnitStats stats, UnitCombatContext context)
```

Use `context.TargetFinder` for acquisition, `context.BattleArea` for clamping, and `context.NotifyUnitDied(this)` on death. Keep ally death deactivation and enemy pool return timing unchanged through `UnitManager.NotifyUnitDied`.

- [ ] **Step 8: Inject context through `UnitSpawner`**

Add `UnitCombatContext context` to `SpawnAlly`, `SpawnEnemy`, and private `ActivateUnit`, then call `unit.Initialize(stats, context)`. Construct one context in `UnitManager.Start` after the roster/finder exists and pass it to every spawn path, including reinforcements and merged allies.

- [ ] **Step 9: Replace coroutine bodies with scheduler callbacks**

`ApplyDamageOverTime` and `ApplySlowAfterDelay` schedule on `UnitEffectScheduler`. During `Update`, call scheduler `Tick` with callbacks to `TakeDamage`, `ApplyMoveSpeedMultiplier`, and `ApplyAttackRateMultiplier`. `ResetCombatState` and `MarkReturnedToPool` call scheduler `Reset`.

- [ ] **Step 10: Run runtime, pooling, health-bar, and existing combat tests**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'Unit' -testResults (Join-Path $unityProject 'Temp\UnitRuntimeIntegration.xml') -logFile (Join-Path $unityProject 'Temp\UnitRuntimeIntegration.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
```

Expected: unit runtime, pool reset, placement, targeting, creation, and world-health-bar tests pass; compile has `0 Error(s)`.

- [ ] **Step 11: Commit the `UnitBase` composition refactor**

```powershell
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Units/UnitMovement.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitMovement.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitAttack.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitAttack.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitCombatContext.cs' 'pin-ball/Assets/02. Scripts/Battle/Units/UnitCombatContext.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitMovementTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitMovementTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitAttackTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitAttackTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitPoolResetTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/UnitPoolResetTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/UnitBase.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitSpawner.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs'
git commit -m 'refactor: compose focused unit runtime objects'
```

---

### Task 9: Replace Ally Skill Switch with Active Skill Objects

**Files:**
- Create: `Assets/02. Scripts/Battle/Skills/IUnitSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/IEnemyBattleActions.cs`
- Create: `Assets/02. Scripts/Battle/Skills/UnitSkillContext.cs`
- Create: `Assets/02. Scripts/Battle/Skills/UnitSkillValueReader.cs`
- Create: `Assets/02. Scripts/Battle/Skills/UnitSkillRegistry.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/AllySkillController.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/ShieldJudgmentSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/BloodWhirlwindSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/ArrowRainSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/PiercingShotSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/ExplosiveFireballSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/FrostStormSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/PiercingChargeSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/PhalanxFormationSkill.cs`
- Create: `Assets/02. Scripts/Battle/Editor/AllySkillRegistryTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/AllySkillBehaviorTests.cs`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs:13-50,148-445`

**Interfaces:**
- Consumes: `AllySkillData`, caster/target actors, `UnitTargetFinder`, current mana rules.
- Produces: `IActiveUnitSkill`, complete ally registry, `AllySkillController` with no ID switch.

- [ ] **Step 1: Write failing registry completeness tests**

Assert the registry creates a non-null `IActiveUnitSkill` for exactly these IDs:

```csharp
string[] ids =
{
    "shield_judgment",
    "blood_whirlwind",
    "arrow_rain",
    "piercing_shot",
    "explosive_fireball",
    "frost_storm",
    "piercing_charge",
    "phalanx_formation"
};
```

Assert unknown ID lookup returns false.

- [ ] **Step 2: Write failing mana-controller tests**

Test starting mana clamp, basic attack gain, hit gain cooldown, maximum mana cap, cast reset to zero, and a unit with zero maximum mana never casts.

- [ ] **Step 3: Write one failing behavior case for each ally skill**

Use deterministic test actors and explicit `AllySkillEffectData` arrays. Assert:

- Shield Judgment damages the primary target and applies a shield to the caster.
- Blood Whirlwind damages all radius targets and heals by capped hit count.
- Arrow Rain damages and slows radius targets.
- Piercing Shot hits at most four sorted line targets with per-index multipliers.
- Explosive Fireball applies immediate damage and replacement DOT.
- Frost Storm applies damage, stun, and delayed slow.
- Piercing Charge hits at most configured targets and moves the caster by configured distance.
- Phalanx Formation damages/stuns line targets and reduces nearby ally damage.

- [ ] **Step 4: Run ally skill tests and confirm RED**

Run the Verification Command Template for `AllySkillRegistryTests` and `AllySkillBehaviorTests`.

Expected: skill infrastructure and implementations are absent.

- [ ] **Step 5: Implement base contracts and context**

```csharp
public interface IUnitSkill
{
    string Id { get; }
}

public interface IActiveUnitSkill : IUnitSkill
{
    void Execute(UnitSkillContext context, AllySkillData data);
}

public interface IEnemyBattleActions
{
    void SpawnEnemyReinforcement(string enemyId, int count, Vector3 center);
    void ApplyEnemySpeedBuff(
        float moveSpeedMultiplier,
        float attackRateMultiplier);
}
```

`UnitSkillContext` exposes `UnitBase Caster`, `UnitBase PrimaryTarget`, `UnitTargetFinder TargetFinder`, optional `IEnemyBattleActions EnemyActions`, and one reusable `List<UnitBase> Targets`. Its constructor is `UnitSkillContext(UnitBase caster, UnitBase primaryTarget, UnitTargetFinder targetFinder, IEnemyBattleActions enemyActions = null)`. Reject null caster/finder in the constructor; ally contexts leave `EnemyActions` null, while enemy contexts always supply it.

- [ ] **Step 6: Implement safe value reading**

`UnitSkillValueReader.Get(AllySkillData,int,int)` returns `value1`, `value2`, or `value3` and returns zero for invalid data/index. `Percent(float)` returns `value * 0.01f`. Add direct tests for invalid and valid indexes.

- [ ] **Step 7: Implement registry factories**

Use `Dictionary<string, Func<IUnitSkill>>`, reject duplicate IDs in the constructor, and expose `bool TryCreate(string id, out IUnitSkill skill)`. Return a new object for each call. Register all eight ally skills explicitly in `CreateDefault()`.

- [ ] **Step 8: Implement `AllySkillController`**

Move `CurrentMana`, `_nextHitManaTime`, starting/basic/hit mana rules, cooldown, and cast reset from `AllyUnit`. Use this surface:

```csharp
public float CurrentMana { get; }
public bool CanCast(float maxMana);
public void Initialize(AllyCommonData common, AllySkillData skill, float maxMana);
public void Reset(float maxMana);
public void GainFromBasicAttack(float maxMana);
public void GainFromDamage(float now, float maxMana);
public bool TryCast(
    UnitSkillContext context,
    float maxMana,
    Action<string> warn);
```

The controller receives explicit `now` values. It asks `UnitSkillRegistry` for the current `AllySkillData.id`; when absent, it invokes the supplied warning callback once per cast attempt and returns false. Preserve the current ordering by resetting mana to zero before registry lookup or skill execution whenever `CanCast` is true.

- [ ] **Step 9: Implement Shield Judgment and Blood Whirlwind**

Move the exact bodies of `CastShieldJudgment` and `CastBloodWhirlwind` into their named classes. Replace `_currentTarget`, `_unitManager`, `_targets`, and `this` with `UnitSkillContext` members; keep effect indexes and calculation order unchanged.

- [ ] **Step 10: Implement Arrow Rain and Piercing Shot**

Move `CastArrowRain` and `CastPiercingShot` into the corresponding files. Preserve the `AttackRange * 2f` line distance, `0.5f` half-width, maximum four targets, per-target effect index, and armor-ignore index `4`.

- [ ] **Step 11: Implement Explosive Fireball and Frost Storm**

Move both method bodies. Preserve armor-ignore indexes, `Mathf.Max(1f, effect.value3)` DOT duration, stun-before-delayed-slow order, and slow/delay values.

- [ ] **Step 12: Implement Piercing Charge and Phalanx Formation**

Move both method bodies. Preserve `0.6f` charge line half-width, maximum-target rounding, direct caster displacement, `2.5f` phalanx line distance, `1.5f` half-width, and ally buff radius/effect indexes.

- [ ] **Step 13: Run ally registry and behavior tests and confirm GREEN**

Run the Verification Command Template for `AllySkillRegistryTests` and `AllySkillBehaviorTests`.

Expected: registry completeness, mana, and all eight behavior cases pass.

- [ ] **Step 14: Slim `AllyUnit`**

Inject `UnitManager` and a default skill registry through `SetData`; remove `App.Get<UnitManager>()`. Keep preparation input, drag state, identity, level, and common data. Delegate mana events and cast execution to `AllySkillController`. Delete the eight private cast methods, `Value`, `Percent`, and the ally ID `switch` only after all tests pass.

- [ ] **Step 15: Run unit and ally tests, compile, and commit**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'Ally' -testResults (Join-Path $unityProject 'Temp\AllyRefactorTests.xml') -logFile (Join-Path $unityProject 'Temp\AllyRefactorTests.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Skills' 'pin-ball/Assets/02. Scripts/Battle/Editor/AllySkillRegistryTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/AllySkillRegistryTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/AllySkillBehaviorTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/AllySkillBehaviorTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/AlllyUnit.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitSpawner.cs'
git commit -m 'refactor: extract ally skill objects'
```

---

### Task 10: Replace Enemy Skill Conditionals with Trigger Objects

**Files:**
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/EnemySkillController.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/WolfSprintSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/FocusedFireSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/ShieldBlockSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/OrcRageSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/DarkBlastSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/ShadowLeapSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/TrollRegenerationSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/GroundSlamSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/WeakeningCurseSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/SummonMinionsSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/KingSlamSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Enemy/FinalOrderSkill.cs`
- Create: `Assets/02. Scripts/Battle/Editor/EnemySkillRegistryTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/EnemySkillBehaviorTests.cs`
- Modify: `Assets/02. Scripts/Battle/Skills/IUnitSkill.cs`
- Modify: `Assets/02. Scripts/Battle/Skills/UnitSkillRegistry.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Battle/EnemyUnit.cs:10-369`
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs`

**Interfaces:**
- Consumes: enemy skill arrays, explicit combat triggers, `UnitManager` reinforcement and team-buff Facade actions.
- Produces: per-spawn stateful enemy skill objects and an `EnemyUnit` with no unit-ID branches.

- [ ] **Step 1: Add trigger-specific skill contracts in failing tests**

Define these exact interfaces, all derived from `IUnitSkill`:

```csharp
public interface IBattleStartSkill : IUnitSkill
{
    void OnBattleStart(UnitSkillContext context, EnemySkillData data);
}

public interface IUnitTickSkill : IUnitSkill
{
    void Tick(UnitSkillContext context, EnemySkillData data, float now);
}

public interface IBasicAttackDamageSkill : IUnitSkill
{
    float ModifyDamage(
        UnitSkillContext context,
        EnemySkillData data,
        UnitBase target,
        float damage);
}

public interface IBasicAttackHitSkill : IUnitSkill
{
    void OnBasicAttackHit(
        UnitSkillContext context,
        EnemySkillData data,
        UnitBase target,
        int basicAttackCount);
}

public interface IUnitDamagedSkill : IUnitSkill
{
    void OnDamaged(UnitSkillContext context, EnemySkillData data);
}

public interface IIncomingDamageSkill : IUnitSkill
{
    float ModifyIncomingDamage(
        UnitSkillContext context,
        EnemySkillData data,
        float damage,
        UnitBase source);
}

public interface ICrowdControlDurationSkill : IUnitSkill
{
    float ModifyDuration(
        UnitSkillContext context,
        EnemySkillData data,
        float duration);
}
```

- [ ] **Step 2: Write failing registry completeness tests**

Assert factories exist for all twelve IDs:

```csharp
string[] ids =
{
    "wolf_sprint",
    "focused_fire",
    "shield_block",
    "orc_rage",
    "dark_blast",
    "shadow_leap",
    "troll_regeneration",
    "ground_slam",
    "weakening_curse",
    "summon_minions",
    "king_slam",
    "final_order"
};
```

Assert two factory calls return distinct instances so pooled enemies do not share runtime state.

- [ ] **Step 3: Write one failing behavior case for each trigger object**

Assert:

- Wolf Sprint applies its move multiplier for its configured duration.
- Focused Fire resets stacks on target change and caps stacks.
- Shield Block reduces only front-facing incoming damage and reduces crowd-control duration.
- Orc Rage activates once at or below 50% HP.
- Dark Blast runs every fourth basic hit and applies radius damage plus attack-rate reduction.
- Shadow Leap selects the farthest ally, repositions, damages, and forces that target.
- Troll Regeneration heals each second with in-combat or out-of-combat value.
- Ground Slam runs every third hit and applies radius damage, stun, and knockback.
- Weakening Curse runs every fourth hit and selects highest-HP ally.
- Summon Minions fires once at each 75%, 50%, and 25% boss threshold.
- King Slam runs every fourth hit and applies primary plus radius damage.
- Final Order fires once at or below 25% HP and applies both team speed multipliers.

- [ ] **Step 4: Run enemy skill tests and confirm RED**

Run the Verification Command Template for `EnemySkillRegistryTests` and `EnemySkillBehaviorTests`.

Expected: enemy trigger infrastructure and classes do not exist.

- [ ] **Step 5: Implement trigger contracts and enemy controller**

`EnemySkillController.Initialize(EnemyUnitData data, UnitSkillRegistry registry)` creates new skill objects for every valid data skill, resets `BasicAttackCount` to zero, and stores them by trigger interface. Expose `OnBattleStart`, `Tick`, `ModifyBasicAttackDamage`, `OnBasicAttackHit`, `OnDamaged`, `ModifyIncomingDamage`, and `ModifyCrowdControlDuration`.

- [ ] **Step 6: Implement Wolf Sprint, Focused Fire, and Shield Block**

Move `ApplyBattleStartSkill` sprint logic, `UpdateFocusedFire` plus damage modification, and both shield modifiers. `FocusedFireSkill` owns target and stack state. `ShieldBlockSkill` uses the caster's current target for the facing vector and keeps the current `Vector3.Dot > 0f` front test.

- [ ] **Step 7: Implement Orc Rage, Dark Blast, and Shadow Leap**

Move `_rageActivated`, `ActivateRage`, `CastDarkBlast`, and shadow leap start behavior into the three classes. Preserve infinite rage duration, fourth-hit interval, farthest-target selection, `0.5f` landing offset, immediate damage, and infinite forced target.

- [ ] **Step 8: Implement Troll Regeneration, Ground Slam, and Weakening Curse**

Move `_nextRegenerationTime`, one-second cadence, out-of-combat threshold, third-hit interval, fourth-hit interval, radius values, and effect order into their respective objects.

- [ ] **Step 9: Implement Summon Minions, King Slam, and Final Order**

`SummonMinionsSkill` owns phase `0..2` and thresholds `0.75f`, `0.5f`, `0.25f`; it calls `context.EnemyActions.SpawnEnemyReinforcement` to spawn goblins and wolves. `FinalOrderSkill` owns its one-shot flag and calls `ApplyEnemySpeedBuff`. Preserve King Slam's fourth-hit interval and primary-before-radius order. Missing enemy actions are an initialization error caught by the controller and scene wiring tests, not a silent fallback.

- [ ] **Step 10: Run registry and all twelve behavior cases and confirm GREEN**

Run the Verification Command Template for `EnemySkillRegistryTests` and `EnemySkillBehaviorTests`.

Expected: registry isolation and all listed behavior cases pass.

- [ ] **Step 11: Slim `EnemyUnit` and inject dependencies**

Keep identity, rank, breach damage, and the common unit tick hook. Make `UnitManager` implement `IEnemyBattleActions`; inject it and a default registry in `SetData`, and put it into every enemy `UnitSkillContext`. Delegate every trigger to `EnemySkillController`. Delete `_targets`, all per-skill state fields, `FindSkill`, `GetSummonThreshold`, `Value`, `Percent`, unit-ID comparisons, and private skill methods after behavior tests pass.

- [ ] **Step 12: Run all enemy and unit tests, compile, and commit**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testFilter 'Enemy' -testResults (Join-Path $unityProject 'Temp\EnemyRefactorTests.xml') -logFile (Join-Path $unityProject 'Temp\EnemyRefactorTests.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check -- 'pin-ball/Assets/02. Scripts/Battle'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Skills/IUnitSkill.cs' 'pin-ball/Assets/02. Scripts/Battle/Skills/UnitSkillRegistry.cs' 'pin-ball/Assets/02. Scripts/Battle/Skills/Enemy' 'pin-ball/Assets/02. Scripts/Battle/Editor/EnemySkillRegistryTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/EnemySkillRegistryTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/Editor/EnemySkillBehaviorTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/EnemySkillBehaviorTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs' 'pin-ball/Assets/02. Scripts/Battle/EnemyUnit.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitSpawner.cs'
git commit -m 'refactor: extract enemy skill objects'
```

---

### Task 11: Integrate Facades, Validate Wiring, and Remove Proven Dead Code

**Files:**
- Create: `Assets/02. Scripts/Battle/Editor/BattleSceneWiringTests.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs`
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/EnemyUnit.cs`
- Modify if required for API compilation only: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify if required for API compilation only: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Modify if required for API compilation only: `Assets/02. Scripts/03. UI/StatusPanel.cs`
- Modify if required for API compilation only: `Assets/02. Scripts/03. UI/EvolutionPanel.cs`
- Modify if required for API compilation only: `Assets/02. Scripts/03. UI/AllyDetailPanel.cs`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs` unused import only

**Interfaces:**
- Consumes: all extracted services and current UI/pinball Facade calls.
- Produces: final thin managers/actors, validated scene/prefab references, and an evidence-backed dead-code list.

- [ ] **Step 1: Write failing scene and prefab wiring tests**

Load `Assets/01. Scenes/02. Game.unity` through `EditorSceneManager.OpenScene` and assert:

- exactly one active `BattleManager`, `UnitManager`, and `PinballManager` exists;
- `UnitManager` has non-null serialized `battleArea` and a co-located `UnitSpawner`;
- `UnitSpawner` has non-null ally/enemy prefabs and spawn points;
- ally and enemy prefabs each contain their actor type, `BattleUnitVisual`, collider, renderer, and `WorldHealthBarController`;
- every `MonoBehaviour` in Game scene and the two unit prefabs has a non-null script.

Close the opened scene in test teardown without saving.

- [ ] **Step 2: Run `BattleSceneWiringTests` before cleanup**

Run the Verification Command Template for `BattleSceneWiringTests`.

Expected: current wiring passes or reports the exact missing field that must be repaired during this task.

- [ ] **Step 3: Add explicit initialization validation**

`UnitManager.Awake` must report and disable itself when `battleArea` or co-located `UnitSpawner` is missing. `UnitSpawner.Awake` must validate both prefabs and spawn points. Error messages include component type, GameObject name, and field name. Do not substitute runtime-created objects.

- [ ] **Step 4: Migrate every external caller to the final Facade API**

Search and inspect all calls:

```powershell
rg -n 'App\.Get<(BattleManager|UnitManager)>|\bUnitManager\.|\bBattleManager\.' '.\pin-ball\Assets\02. Scripts' -g '*.cs'
```

Keep public methods used by pinball and UI unless renaming materially improves the final contract. When renamed, update all callers in the same step and add a focused compile check. Do not leave obsolete forwarding methods after callers move.

- [ ] **Step 5: Verify dead-code candidates across all reference channels**

```powershell
rg -n '\bAddAlly\b|\bForceRemove\b|UnityEngine\.Serialization' '.\pin-ball\Assets' -g '*.cs' -g '*.unity' -g '*.prefab' -g '*.json'
rg -n 'm_MethodName: (AddAlly|ForceRemove)' '.\pin-ball\Assets' -g '*.unity' -g '*.prefab'
```

Expected before deletion: `UnitManager.AddAlly(UnitBase)` and `UnitBase.ForceRemove()` have declarations but no callers or serialized method references; `UnityEngine.Serialization` has no attribute use in `UnitManager` or `TitleData`.

- [ ] **Step 6: Remove only confirmed unused members and imports**

Remove `UnitManager.AddAlly(UnitBase)`, `UnitBase.ForceRemove()`, unused `using UnityEngine.Serialization` directives in `UnitManager` and `TitleData`, and fields/methods made unreachable by completed extraction. Do not remove pinball precision fields in this battle task; report them as an out-of-scope incomplete feature.

- [ ] **Step 7: Verify no subordinate combat actor performs global lookup**

```powershell
rg -n 'App\.Get<' '.\pin-ball\Assets\02. Scripts\Battle\UnitBase.cs' '.\pin-ball\Assets\02. Scripts\Battle\AlllyUnit.cs' '.\pin-ball\Assets\02. Scripts\Battle\EnemyUnit.cs' '.\pin-ball\Assets\02. Scripts\Battle\Skills' '.\pin-ball\Assets\02. Scripts\Battle\Units'
```

Expected: no matches. Scene-level managers/controllers may still use `App.Get<T>()` where already permitted.

- [ ] **Step 8: Run wiring and full Edit Mode tests**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testResults (Join-Path $unityProject 'Temp\BattleRefactorFullEditMode.xml') -logFile (Join-Path $unityProject 'Temp\BattleRefactorFullEditMode.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
```

Expected: every Edit Mode test passes; compile has `0 Error(s)`.

- [ ] **Step 9: Compare manager and actor responsibilities**

Run:

```powershell
rg -n 'private (void|bool|float|int|string)|protected override|public (void|bool)' '.\pin-ball\Assets\02. Scripts\Battle\BattleManager.cs' '.\pin-ball\Assets\02. Scripts\Battle\UnitManager.cs' '.\pin-ball\Assets\02. Scripts\Battle\UnitBase.cs' '.\pin-ball\Assets\02. Scripts\Battle\AlllyUnit.cs' '.\pin-ball\Assets\02. Scripts\Battle\EnemyUnit.cs'
```

Inspect each remaining method and confirm it is orchestration, Unity adaptation, preparation input, or team-specific trigger timing. Move any remaining rule calculation to the already responsible extracted object before committing.

- [ ] **Step 10: Commit integration and dead-code removal**

```powershell
git diff --check -- 'pin-ball/Assets/02. Scripts'
git add -- 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleSceneWiringTests.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleSceneWiringTests.cs.meta' 'pin-ball/Assets/02. Scripts/Battle/BattleManager.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitSpawner.cs' 'pin-ball/Assets/02. Scripts/Battle/UnitBase.cs' 'pin-ball/Assets/02. Scripts/Battle/AlllyUnit.cs' 'pin-ball/Assets/02. Scripts/Battle/EnemyUnit.cs' 'pin-ball/Assets/02. Scripts/02. Data/TitleData.cs'
git diff --cached --name-only
git commit -m 'refactor: finalize battle domain facades'
```

If Step 4 required a pinball or UI caller change, append only that exact changed path to the `git add` command. Before committing, inspect `git diff --cached --name-only` and unstage any unchanged or unrelated file. Confirm none of the user's pre-existing dirty files is staged.

---

### Task 12: Add Build Verification, Run End-to-End Checks, and Record AI Use

**Files:**
- Create: `Assets/02. Scripts/Battle/Editor/BattleRefactorBuild.cs`
- Modify: `.github/ai-use-log.md`
- Verify: `Assets/01. Scenes/00. Developer.unity`
- Verify: `Assets/01. Scenes/01. Title.unity`
- Verify: `Assets/01. Scenes/02. Game.unity`
- Verify: `Assets/04. Prefabs/AllyUnit.prefab`
- Verify: `Assets/04. Prefabs/EnemyUnit.prefab`

**Interfaces:**
- Consumes: final scenes, all Edit Mode tests, Unity BuildPipeline.
- Produces: repeatable WebGL Development Build command, verification evidence, and factual AI-use record.

- [ ] **Step 1: Implement the WebGL build entry point**

```csharp
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BattleRefactorBuild
{
    public static void BuildWebGL()
    {
        string output = Environment.GetEnvironmentVariable(
            "PINBALL_BATTLE_REFACTOR_WEBGL_PATH");
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                "PINBALL_BATTLE_REFACTOR_WEBGL_PATH is required.");
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"WebGL build failed: {report.summary.result}");
        }
    }
}
#endif
```

- [ ] **Step 2: Run the complete Edit Mode suite again**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
& $unityEditor -batchmode -nographics -projectPath $unityProject -runTests -testPlatform EditMode -testResults (Join-Path $unityProject 'Temp\BattleRefactorFinalEditMode.xml') -logFile (Join-Path $unityProject 'Temp\BattleRefactorFinalEditMode.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Expected: all tests pass; record exact passed, failed, and skipped counts from the XML.

- [ ] **Step 3: Run the WebGL Development Build**

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe'
$unityProject = (Resolve-Path '.\pin-ball').Path
$env:PINBALL_BATTLE_REFACTOR_WEBGL_PATH = Join-Path $env:TEMP 'pin-ball-battle-refactor-webgl'
& $unityEditor -batchmode -nographics -projectPath $unityProject -buildTarget WebGL -executeMethod BattleRefactorBuild.BuildWebGL -logFile (Join-Path $unityProject 'Temp\BattleRefactorWebGL.log') -quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Expected: exit code `0`, log contains `Build completed with a result of 'Succeeded'`, and the temporary output contains `index.html`, `Build`, and `TemplateData`.

- [ ] **Step 4: Run serialized reference and missing-script checks**

```powershell
rg -n 'm_Script: \{fileID: 0\}|m_Script: \{fileID: 11500000, guid: 00000000000000000000000000000000' '.\pin-ball\Assets\01. Scenes' '.\pin-ball\Assets\04. Prefabs' -g '*.unity' -g '*.prefab'
```

Expected: no matches. Confirm `BattleSceneWiringTests` passes in the final XML.

- [ ] **Step 5: Perform the manual Play Mode checklist**

Use Developer scene as the entry point and record pass/fail for every item:

1. Developer transitions through Title to Game.
2. Each of the four pinball goals spawns the configured base ally.
3. Ally count rules are unchanged at 5, 6, and 7 units.
4. Drag placement, merge, and both evolution choices work.
5. Wave start spawns the configured enemies.
6. Allies and enemies move, attack, and cast every reachable skill.
7. Ally defeat applies breach damage and restores preparation state when HP remains.
8. Wave clear grants the current JSON reward and advances once.
9. Final boss defeat reaches Victory.
10. Returning units to pools and replaying a wave shows no stale HP, target, mana, buff, or skill phase.

Do not mark an item passed without observing it. List unobserved items as manual follow-up.

- [ ] **Step 6: Record the final dead-code evidence**

Add a task note containing each removed member/import, the reference searches used, and why it was safe. Add a separate retained-candidate list containing pinball precision fields and any ambiguous reflection/serialization entry point.

- [ ] **Step 7: Update the AI-use log with facts only**

Append a section titled `## 2026-08-10 전투 도메인 객체 지향 리팩터링` containing:

- used tool/model: Codex, GPT-5 family;
- user request: battle-domain responsibility split and proven unused-code removal;
- AI proposal: thin Unity Facades plus plain C# rule objects and skill strategies;
- actual files/areas changed;
- user decisions: Inspector/API rewiring allowed, behavior preservation required, conservative dead-code deletion;
- important instructions: existing structure, no external packages, scene/prefab wiring, pooling preservation;
- exact automated test counts, compile result, WebGL result, manual items observed, and any limitations.

- [ ] **Step 8: Run final repository checks**

```powershell
dotnet build '.\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
git diff --check
git -c core.quotePath=false status --short
```

Expected: build has `0 Error(s)`; diff check is empty; status shows only intended refactor files plus the user's pre-existing unstaged changes.

- [ ] **Step 9: Commit verification tooling and documentation**

```powershell
git add -- 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleRefactorBuild.cs' 'pin-ball/Assets/02. Scripts/Battle/Editor/BattleRefactorBuild.cs.meta' 'pin-ball/.github/ai-use-log.md'
git diff --cached --name-only
git commit -m 'test: verify battle refactor in WebGL'
```

Expected staged names: only the build verifier, its meta file, and AI-use log.

- [ ] **Step 10: Prepare the completion report**

Report implementation summary, files grouped by responsibility, exact test/build evidence, manual Play Mode results, removed code with evidence, retained ambiguous candidates, known limitations, and the unchanged user worktree files. Do not claim unexecuted manual checks.
