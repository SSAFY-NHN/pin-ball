# Gameplay Feedback Milestone Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the ally ownership cap while keeping a five-ally battle-entry cap, make allied deaths permanent, add an explicit two-second wave-resolution phase with stronger resource feedback, and make the pinball board and launcher handle visibly interactive.

**Architecture:** `UnitManager` remains the roster authority, while `BattleManager` owns an explicit `Resolving` state backed by small testable resolution-domain objects. UI observes resolution events without owning progression, and the launcher delegates visual affordance to a focused glow controller that reuses the existing mask/additive/Bloom path.

**Tech Stack:** Unity 6.0.0.79f1, C#, Unity Test Framework/NUnit EditMode tests, TextMesh Pro, Unity UI, DOTween, URP 2D, PC WebGL

## Global Constraints

- Allow any non-negative allied-unit count to use pinball preparation actions when all existing state, ball, and gold conditions pass.
- Allow wave start only for `1..5` owned allies.
- Permanently remove a dead ally from active roster, owned roster, saved placement, and the scene by returning it to the existing pool.
- Use `Pending -> Active -> Resolving -> Pending | Victory | Defeat`.
- Hold `Resolving` for exactly `2f` seconds of normal game time.
- Apply clear gold or failure HP damage immediately when `Resolving` begins and never more than once.
- Preserve `발사 {CurrentLaunchCost}G` and its insufficient-gold color behavior.
- Use `웨이브 클리어` and `방어 실패` for the intermediate result banner.
- Reuse the existing arcane mask image, additive material, HDR camera, and Bloom; do not add `Light2D` or runtime-created renderer objects.
- Do not add tutorial behavior, golden unit light pools, or ground-shadow changes.
- Do not install packages, change save-data formats, create a new top-level folder, or refactor unrelated code.
- Keep serialized field names in the project's existing non-underscore style.
- Preserve pinball colliders, physics, pull geometry, launch direction, UI anchors outside touched elements, and unrelated artwork.

## File Map

### Create

- `Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs` — one pending resolution and its deadline.
- `Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs` — pure wipe-priority and next-state decisions.
- `Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs` — resolution timing and policy tests.
- `Assets/02. Scripts/03. UI/WaveResultPanel.cs` — presentation-only intermediate result banner.
- `Assets/02. Scripts/03. UI/Editor/WaveResultPanelTests.cs` — result-copy tests.
- `Assets/02. Scripts/Pinball/PinballLauncherGlowController.cs` — handle glow presentation state.
- `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs` — scene reference regression tests.

Unity creates a matching `.meta` file for each new asset. Commit it with its asset.

### Modify

- `Assets/02. Scripts/00. Core/Enum.cs`
- `Assets/02. Scripts/Battle/Units/UnitRoster.cs`
- `Assets/02. Scripts/Battle/UnitManager.cs`
- `Assets/02. Scripts/Battle/BattleManager.cs`
- `Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs`
- `Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs`
- `Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs`
- `Assets/02. Scripts/Pinball/PinballManager.cs`
- `Assets/02. Scripts/03. UI/WavePanel.cs`
- `Assets/02. Scripts/03. UI/StatusPanel.cs`
- `Assets/02. Scripts/Visual/ArcaneMaskGlowController.cs`
- `Assets/02. Scripts/Pinball/Editor/ArcaneGlowMathTests.cs`
- `Assets/02. Scripts/Pinball/PinballLauncherController.cs`
- `Assets/01. Scenes/02. Game.unity`
- `.github/ai-use-log.md`

---

### Task 1: Unlimited Ownership and Permanent Allied Death

**Files:**
- Modify: `Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitRoster.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:30-36,218-295`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs:153-179`
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs:92-137`

**Interfaces:**
- Preserves: `public const int UnitManager.MaxDeployedAllyCount = 5`
- Preserves: `public static bool UnitManager.CanStartWaveWithAllyCount(int count)`
- Removes: `UnitManager.CanLaunchPinballWithCurrentRoster`
- Removes: `UnitManager.CanLaunchPinballWithAllyCount(int count)`
- Produces: `public static bool WavePanel.IsLaunchAvailable(bool canUsePreparation, EPinballState pinballState, bool hasAvailableBall, bool canAffordLaunch)`
- Changes: `UnitRoster.NotifyUnitDied(UnitBase unit)` removes an allied unit from active and owned lists; enemy death removes only active enemy membership.

- [ ] **Step 1: Replace the obsolete launch-limit test and add permanent-death roster tests**

Delete `CanLaunchPinballWithAllyCount_AllowsExactlySix` from `AllyDeploymentLimitTests.cs` and add:

```csharp
[TestCase(true, EPinballState.Idle, true, true, true)]
[TestCase(false, EPinballState.Idle, true, true, false)]
[TestCase(true, EPinballState.Launched, true, true, false)]
[TestCase(true, EPinballState.Idle, false, true, false)]
[TestCase(true, EPinballState.Idle, true, false, false)]
public void IsLaunchAvailable_UsesPreparationBallStateAndGoldOnly(
    bool canUsePreparation,
    EPinballState pinballState,
    bool hasAvailableBall,
    bool canAffordLaunch,
    bool expected)
{
    Assert.That(
        WavePanel.IsLaunchAvailable(
            canUsePreparation,
            pinballState,
            hasAvailableBall,
            canAffordLaunch),
        Is.EqualTo(expected));
}
```

Add to `UnitRosterTests.cs`:

```csharp
[Test]
public void NotifyUnitDied_AllyIsPermanentlyRemoved()
{
    _allyObject = new GameObject("ally");
    var ally = _allyObject.AddComponent<AllyUnit>();
    var roster = new UnitRoster();
    roster.AddOwnedAlly(ally);

    Assert.That(roster.NotifyUnitDied(ally), Is.True);
    Assert.That(roster.OwnedAllyCount, Is.Zero);
    Assert.That(roster.ActiveAllyCount, Is.Zero);
}

[Test]
public void NotifyUnitDied_EnemyDoesNotTouchOwnedAllies()
{
    _allyObject = new GameObject("ally");
    _enemyObject = new GameObject("enemy");
    var ally = _allyObject.AddComponent<AllyUnit>();
    var enemy = _enemyObject.AddComponent<EnemyUnit>();
    var roster = new UnitRoster();
    roster.AddOwnedAlly(ally);
    roster.AddEnemy(enemy);

    Assert.That(roster.NotifyUnitDied(enemy), Is.True);
    Assert.That(roster.OwnedAllyCount, Is.EqualTo(1));
    Assert.That(roster.ActiveEnemyCount, Is.Zero);
}
```

- [ ] **Step 2: Run focused tests and verify the new API/semantics fail**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball' `
  -runTests -testPlatform EditMode `
  -testFilter 'AllyDeploymentLimitTests' `
  -testResults 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\ally-limit-results.xml' `
  -logFile 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\ally-limit.log'
```

Repeat with `UnitRosterTests`, `unit-roster-results.xml`, and `unit-roster.log`.

Expected: the first filter fails to compile because `WavePanel.IsLaunchAvailable` does not exist. After that symbol exists, the allied-death test fails because owned membership remains.

- [ ] **Step 3: Make allied death remove owned membership at the roster boundary**

Replace `UnitRoster.NotifyUnitDied` with:

```csharp
public bool NotifyUnitDied(UnitBase unit)
{
    if (unit == null) return false;

    return unit.Team == EBattleTeam.Ally
        ? RemoveUnit(unit)
        : _activeEnemies.Remove(unit);
}
```

- [ ] **Step 4: Make `UnitManager` perform all permanent-death cleanup once**

Remove both pinball-count members and add:

```csharp
private bool RemoveOwnedAlly(AllyUnit ally)
{
    if (ally == null) return false;

    int previousOwnedCount = _roster.OwnedAllyCount;
    _roster.RemoveUnit(ally);
    _placementService.Remove(ally);
    bool removed = _roster.OwnedAllyCount != previousOwnedCount;
    if (removed)
    {
        OnDeployedAllyCountChanged?.Invoke(DeployedAllyCount);
    }

    return removed;
}
```

Use:

```csharp
public void NotifyUnitDied(UnitBase unit)
{
    if (unit == null) return;

    if (unit is AllyUnit ally)
    {
        RemoveOwnedAlly(ally);
        RefreshAllyItemModifiers();
        _spawner.ReturnUnit(ally);
        return;
    }

    _roster.NotifyUnitDied(unit);
    _spawner.ReturnUnit(unit);
}
```

Call `RemoveOwnedAlly(ally)` from `ReleaseUnit` for ally merge/manual releases. Preserve enemy removal and `_spawner.ReturnUnit(unit)`. Leave `RestoreAlliesForPreparation` unchanged so it naturally restores survivors only.

- [ ] **Step 5: Remove allied-count checks from launch execution and UI availability**

Delete only this block from `PinballManager.TryLaunchLoadedBall`:

```csharp
if (_unitManager == null ||
    !_unitManager.CanLaunchPinballWithCurrentRoster) return false;
```

Remove `_unitManager` if it has no remaining consumer. Add to `WavePanel`:

```csharp
public static bool IsLaunchAvailable(
    bool canUsePreparation,
    EPinballState pinballState,
    bool hasAvailableBall,
    bool canAffordLaunch)
{
    return canUsePreparation &&
           pinballState == EPinballState.Idle &&
           hasAvailableBall &&
           canAffordLaunch;
}
```

Use it in `RefreshButtons`:

```csharp
launchButton.interactable = IsLaunchAvailable(
    canUsePreparation,
    _pinballState,
    hasAvailableBall,
    canAffordLaunch);
```

Delete `canLaunchWithRoster`. Keep roster subscriptions because they still refresh start-button and ally-count context.

- [ ] **Step 6: Run focused tests and compile**

Run both filters from Step 2, then:

```powershell
dotnet build 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Assembly-CSharp-Editor.csproj' --no-restore
```

Expected: tests and compile exit `0`; `rg -n "CanLaunchPinballWith" 'pin-ball/Assets/02. Scripts' -g '*.cs'` finds no runtime or test reference.

- [ ] **Step 7: Commit roster and launch-rule changes**

```powershell
git add -- `
  'pin-ball/Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs' `
  'pin-ball/Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs' `
  'pin-ball/Assets/02. Scripts/Battle/Units/UnitRoster.cs' `
  'pin-ball/Assets/02. Scripts/Battle/UnitManager.cs' `
  'pin-ball/Assets/02. Scripts/Pinball/PinballManager.cs' `
  'pin-ball/Assets/02. Scripts/03. UI/WavePanel.cs'
git commit -m "feat: make ally ownership unlimited and deaths permanent"
```

---

### Task 2: Explicit Two-Second Wave Resolution

**Files:**
- Create: `Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs.meta`
- Create: `Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs.meta`
- Create: `Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs.meta`
- Modify: `Assets/02. Scripts/00. Core/Enum.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs:12-37,73-85,160-218`
- Modify: `Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs`

**Interfaces:**
- Produces: `EWaveState.Resolving`
- Produces: `public enum EWaveResolutionResult { Cleared, Failed }`
- Produces: `WaveResolutionState.TryBegin(EWaveResolutionResult result, int waveNumber, float now, float duration)`, `IsElapsed(float now)`, and `Clear()`
- Produces: `BattleResolutionPolicy.TryDetectWipe(...)` and `ResolveNextState(...)`
- Produces: `BattleManager.OnWaveResolutionStarted` with signature `Action<EWaveResolutionResult, int>`

- [ ] **Step 1: Write failing resolution-domain tests**

Create `WaveResolutionTests.cs`:

```csharp
#if UNITY_EDITOR
using NUnit.Framework;

public class WaveResolutionTests
{
    [Test]
    public void TryBegin_StoresResultAndRequiresFullDelay()
    {
        var state = new WaveResolutionState();
        Assert.That(
            state.TryBegin(EWaveResolutionResult.Cleared, 3, 10f, 2f),
            Is.True);
        Assert.That(state.Result, Is.EqualTo(EWaveResolutionResult.Cleared));
        Assert.That(state.WaveNumber, Is.EqualTo(3));
        Assert.That(state.IsElapsed(11.999f), Is.False);
        Assert.That(state.IsElapsed(12f), Is.True);
    }

    [Test]
    public void TryBegin_RejectsDuplicateUntilCleared()
    {
        var state = new WaveResolutionState();
        Assert.That(
            state.TryBegin(EWaveResolutionResult.Failed, 1, 0f, 2f),
            Is.True);
        Assert.That(
            state.TryBegin(EWaveResolutionResult.Cleared, 1, 0f, 2f),
            Is.False);
        state.Clear();
        Assert.That(
            state.TryBegin(EWaveResolutionResult.Cleared, 2, 3f, 2f),
            Is.True);
    }

    [TestCase(0, 0, true, EWaveResolutionResult.Cleared)]
    [TestCase(0, 2, true, EWaveResolutionResult.Failed)]
    [TestCase(2, 0, true, EWaveResolutionResult.Cleared)]
    [TestCase(2, 2, false, EWaveResolutionResult.Cleared)]
    public void TryDetectWipe_UsesEnemyFirstTiePriority(
        int allies,
        int enemies,
        bool expectedDetected,
        EWaveResolutionResult expectedResult)
    {
        bool detected = BattleResolutionPolicy.TryDetectWipe(
            allies,
            enemies,
            out EWaveResolutionResult result);
        Assert.That(detected, Is.EqualTo(expectedDetected));
        if (detected) Assert.That(result, Is.EqualTo(expectedResult));
    }

    [TestCase(EWaveResolutionResult.Cleared, false, 20, EWaveState.Pending)]
    [TestCase(EWaveResolutionResult.Cleared, true, 20, EWaveState.Victory)]
    [TestCase(EWaveResolutionResult.Failed, false, 1, EWaveState.Pending)]
    [TestCase(EWaveResolutionResult.Failed, false, 0, EWaveState.Defeat)]
    public void ResolveNextState_UsesOutcomeWavePositionAndHp(
        EWaveResolutionResult result,
        bool isFinalWave,
        int playerHp,
        EWaveState expected)
    {
        Assert.That(
            BattleResolutionPolicy.ResolveNextState(
                result,
                isFinalWave,
                playerHp),
            Is.EqualTo(expected));
    }
}
#endif
```

Add to `BattleRunStateTests.cs`:

```csharp
[Test]
public void ChangeState_AcceptsExplicitResolvingState()
{
    var state = new BattleRunState(
        new[] { new BattleWaveData() },
        true,
        20);
    Assert.That(state.ChangeState(EWaveState.Active), Is.True);
    Assert.That(state.ChangeState(EWaveState.Resolving), Is.True);
    Assert.That(state.State, Is.EqualTo(EWaveState.Resolving));
}
```

- [ ] **Step 2: Run the focused test and verify compilation fails**

Use the Task 1 Unity command with `WaveResolutionTests`, `wave-resolution-results.xml`, and `wave-resolution.log`.

Expected: compilation fails because the enum values and classes do not exist.

- [ ] **Step 3: Add the explicit enums**

```csharp
public enum EWaveState
{
    Pending,
    Active,
    Resolving,
    Victory,
    Defeat
}

public enum EWaveResolutionResult
{
    Cleared,
    Failed
}
```

- [ ] **Step 4: Implement the one-shot resolution clock**

Create `WaveResolutionState.cs`:

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

- [ ] **Step 5: Implement pure wipe and next-state policy**

Create `BattleResolutionPolicy.cs`:

```csharp
public static class BattleResolutionPolicy
{
    public static bool TryDetectWipe(
        int allyCount,
        int enemyCount,
        out EWaveResolutionResult result)
    {
        if (enemyCount <= 0)
        {
            result = EWaveResolutionResult.Cleared;
            return true;
        }

        if (allyCount <= 0)
        {
            result = EWaveResolutionResult.Failed;
            return true;
        }

        result = default;
        return false;
    }

    public static EWaveState ResolveNextState(
        EWaveResolutionResult result,
        bool isFinalWave,
        int playerHp)
    {
        if (result == EWaveResolutionResult.Failed)
        {
            return playerHp <= 0
                ? EWaveState.Defeat
                : EWaveState.Pending;
        }

        return isFinalWave
            ? EWaveState.Victory
            : EWaveState.Pending;
    }
}
```

- [ ] **Step 6: Run domain tests and verify they pass**

Run `WaveResolutionTests` and `BattleRunStateTests` separately.

Expected: both exit `0`; duplicate start is rejected, `11.999f` is early, `12f` is elapsed, simultaneous wipe clears, and `Resolving` is a real state.

- [ ] **Step 7: Replace immediate completion with the delayed manager lifecycle**

Add to `BattleManager`:

```csharp
[SerializeField, Min(0f)] private float waveResolutionDuration = 2f;
public event Action<EWaveResolutionResult, int> OnWaveResolutionStarted;
private readonly WaveResolutionState _waveResolution = new();
```

Replace `Update`:

```csharp
private void Update()
{
    if (State == EWaveState.Active)
    {
        if (BattleResolutionPolicy.TryDetectWipe(
                _unitManager.RemainingAllyCount,
                _unitManager.RemainingEnemyCount,
                out EWaveResolutionResult result))
        {
            BeginWaveResolution(result);
        }
        return;
    }

    if (State == EWaveState.Resolving &&
        _waveResolution.IsElapsed(Time.time))
    {
        FinishWaveResolution();
    }
}
```

Replace `DefeatWave` and `CompleteWave` with:

```csharp
private void BeginWaveResolution(EWaveResolutionResult result)
{
    if (State != EWaveState.Active ||
        !_waveResolution.TryBegin(
            result,
            CurrentWaveNumber,
            Time.time,
            waveResolutionDuration)) return;

    BattleWaveData wave = CurrentWave;
    bool isFinalWave =
        _runState.CurrentWaveIndex + 1 >= _runState.TotalWaveCount;
    ChangeState(EWaveState.Resolving);

    if (result == EWaveResolutionResult.Cleared)
    {
        if (wave != null)
        {
            AddGold(isFinalWave
                ? wave.FinalClearGoldReward
                : wave.WaveClearGoldReward);
        }
    }
    else
    {
        int damage = BarrierDamageCalculator.Calculate(
            _unitManager.CalculateRemainingBreachDamage(),
            _barrierDamageReduction,
            _minimumBarrierDamage);
        _runState.ApplyPlayerDamage(damage);
        OnHpChanged?.Invoke(_runState.PlayerHp);
        if (_runState.PlayerHp > 0 && wave != null)
        {
            AddGold(wave.RetryGoldReward);
        }
    }

    OnWaveResolutionStarted?.Invoke(result, CurrentWaveNumber);
}

private void FinishWaveResolution()
{
    if (State != EWaveState.Resolving || !_waveResolution.IsPending) return;

    EWaveResolutionResult result = _waveResolution.Result;
    bool isFinalWave =
        _runState.CurrentWaveIndex + 1 >= _runState.TotalWaveCount;
    bool hasValidWave = CurrentWave != null;
    _unitManager.ResolveWaveResult();
    _waveResolution.Clear();

    if (!hasValidWave)
    {
        ChangeState(EWaveState.Defeat);
        return;
    }

    EWaveState nextState = BattleResolutionPolicy.ResolveNextState(
        result,
        isFinalWave,
        _runState.PlayerHp);
    if (result == EWaveResolutionResult.Cleared &&
        nextState == EWaveState.Pending)
    {
        _runState.AdvanceWave();
        OnWaveChanged?.Invoke(_runState.CurrentWaveIndex);
    }

    ChangeState(nextState);
}
```

Delete old immediate reward/damage/state-transition code. Preserve start guards, economy, item behavior, and teardown.

- [ ] **Step 8: Run focused tests and compile**

Run `WaveResolutionTests`, `BattleRunStateTests`, and Task 1's `dotnet build` command.

Expected: all pass; `rg -n "DefeatWave|CompleteWave" 'pin-ball/Assets/02. Scripts/Battle/BattleManager.cs'` finds no old definitions.

- [ ] **Step 9: Commit explicit wave resolution**

```powershell
git add -- `
  'pin-ball/Assets/02. Scripts/00. Core/Enum.cs' `
  'pin-ball/Assets/02. Scripts/Battle/BattleManager.cs' `
  'pin-ball/Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs' `
  'pin-ball/Assets/02. Scripts/Battle/Runtime/WaveResolutionState.cs.meta' `
  'pin-ball/Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs' `
  'pin-ball/Assets/02. Scripts/Battle/Runtime/BattleResolutionPolicy.cs.meta' `
  'pin-ball/Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs' `
  'pin-ball/Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs.meta' `
  'pin-ball/Assets/02. Scripts/Battle/Editor/BattleRunStateTests.cs'
git commit -m "feat: add delayed wave resolution state"
```

---

### Task 3: Result Banner and Stronger Resource Feedback

**Files:**
- Create: `Assets/02. Scripts/03. UI/WaveResultPanel.cs`
- Create: `Assets/02. Scripts/03. UI/WaveResultPanel.cs.meta`
- Create: `Assets/02. Scripts/03. UI/Editor/WaveResultPanelTests.cs`
- Create: `Assets/02. Scripts/03. UI/Editor/WaveResultPanelTests.cs.meta`
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs:60-86,90-188,266-285`

**Interfaces:**
- Consumes: `BattleManager.OnWaveResolutionStarted(EWaveResolutionResult result, int waveNumber)`
- Produces: `public static string WaveResultPanel.ResolveCopy(EWaveResolutionResult result, string clearedCopy, string failedCopy)`
- Preserves: existing final `ResultPanel`, `StatusPanel.ShouldWarnAllyCount(int)`, and `EmphasizeAllyCount()`

- [ ] **Step 1: Write failing result-copy tests**

Create `WaveResultPanelTests.cs`:

```csharp
#if UNITY_EDITOR
using NUnit.Framework;

public class WaveResultPanelTests
{
    [TestCase(EWaveResolutionResult.Cleared, "웨이브 클리어")]
    [TestCase(EWaveResolutionResult.Failed, "방어 실패")]
    public void ResolveCopy_ReturnsOutcomeSpecificText(
        EWaveResolutionResult result,
        string expected)
    {
        Assert.That(
            WaveResultPanel.ResolveCopy(
                result,
                "웨이브 클리어",
                "방어 실패"),
            Is.EqualTo(expected));
    }
}
#endif
```

- [ ] **Step 2: Run the focused test and verify failure**

Use Task 1's Unity command with `WaveResultPanelTests`, `wave-result-panel-results.xml`, and `wave-result-panel.log`.

Expected: compilation fails because `WaveResultPanel` does not exist.

- [ ] **Step 3: Implement the presentation-only result panel**

Create `WaveResultPanel.cs`:

```csharp
using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class WaveResultPanel : UIBase
{
    public override bool IsManagedByStack => false;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private string clearedCopy = "웨이브 클리어";
    [SerializeField] private string failedCopy = "방어 실패";
    [SerializeField] private Color clearedColor =
        new(1f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color failedColor =
        new(1f, 0.28f, 0.24f, 1f);

    private BattleManager _battleManager;
    private Sequence _sequence;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        if (panelRect == null || canvasGroup == null || resultText == null)
        {
            Debug.LogError(
                "[WaveResultPanel] Missing serialized UI reference.");
            enabled = false;
            return;
        }

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnWaveResolutionStarted += OnWaveResolutionStarted;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelRect.localScale = Vector3.one;
    }

    public static string ResolveCopy(
        EWaveResolutionResult result,
        string clearedCopy,
        string failedCopy)
    {
        return result == EWaveResolutionResult.Cleared
            ? clearedCopy
            : failedCopy;
    }

    private void OnWaveResolutionStarted(
        EWaveResolutionResult result,
        int _)
    {
        _sequence?.Kill();
        transform.SetAsLastSibling();
        resultText.text = ResolveCopy(result, clearedCopy, failedCopy);
        resultText.color = result == EWaveResolutionResult.Cleared
            ? clearedColor
            : failedColor;
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.78f;

        _sequence = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, 0.15f))
            .Join(panelRect.DOScale(1f, 0.22f)
                .SetEase(Ease.OutBack))
            .AppendInterval(1.53f)
            .Append(canvasGroup.DOFade(0f, 0.2f))
            .OnComplete(() =>
            {
                panelRect.localScale = Vector3.one;
                _sequence = null;
            });
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        if (_battleManager != null)
        {
            _battleManager.OnWaveResolutionStarted -= OnWaveResolutionStarted;
        }
    }
}
```

The animation totals `1.95f`, stays inside the manager-owned `2f` window, and never changes wave state.

- [ ] **Step 4: Split HP and gold animation configuration in `StatusPanel`**

Add:

```csharp
[Header("Resource Feedback")]
[SerializeField] private Color hpFlashColor =
    new(1f, 0.2f, 0.2f, 1f);
[SerializeField] private Color goldFlashColor =
    new(1f, 0.82f, 0.2f, 1f);
[SerializeField, Min(0f)] private float resourceFeedbackDuration = 0.42f;

private Color _hpBaseColor;
private Color _goldBaseColor;
private Vector3 _hpBaseScale;
private Vector3 _goldBaseScale;
```

Capture base values during `Initialize`:

```csharp
_hpBaseColor = playerHpText.color;
_goldBaseColor = goldText.color;
_hpBaseScale = playerHpText.rectTransform.localScale;
_goldBaseScale = goldText.rectTransform.localScale;
```

Change `OnHpChanged` and `OnGoldChanged` to call `EmphasizeHp()` and `EmphasizeGold()`. Keep `_hasDisplayedHp` and `_hasDisplayedGold` so initialization does not animate.

- [ ] **Step 5: Implement resource-specific normalized tweens**

Add:

```csharp
private void EmphasizeHp()
{
    PlayResourceFeedback(
        playerHpText,
        _hpBaseColor,
        hpFlashColor,
        _hpBaseScale,
        true);
}

private void EmphasizeGold()
{
    PlayResourceFeedback(
        goldText,
        _goldBaseColor,
        goldFlashColor,
        _goldBaseScale,
        false);
}

private void PlayResourceFeedback(
    TextMeshProUGUI text,
    Color baseColor,
    Color flashColor,
    Vector3 baseScale,
    bool shake)
{
    if (text == null) return;

    RectTransform rect = text.rectTransform;
    rect.DOKill();
    text.DOKill();
    rect.localScale = baseScale;
    text.color = baseColor;

    Sequence sequence = DOTween.Sequence();
    sequence.Join(rect.DOPunchScale(
        Vector3.one * 0.24f,
        resourceFeedbackDuration,
        8,
        0.55f));
    sequence.Join(text.DOColor(flashColor, 0.1f)
        .SetLoops(2, LoopType.Yoyo));
    if (shake)
    {
        sequence.Join(rect.DOShakeAnchorPos(
            resourceFeedbackDuration,
            13f,
            18,
            90f,
            false,
            true));
    }

    sequence.OnComplete(() =>
    {
        if (rect != null) rect.localScale = baseScale;
        if (text != null) text.color = baseColor;
    });
}
```

Keep the existing smaller `Emphasize(TextMeshProUGUI)` for ally-count rejection. In `OnDestroy`, kill rect and text tweens and restore base colors/scales when references exist.

```csharp
playerHpText?.rectTransform.DOKill();
playerHpText?.DOKill();
goldText?.rectTransform.DOKill();
goldText?.DOKill();

if (playerHpText != null)
{
    playerHpText.color = _hpBaseColor;
    playerHpText.rectTransform.localScale = _hpBaseScale;
}

if (goldText != null)
{
    goldText.color = _goldBaseColor;
    goldText.rectTransform.localScale = _goldBaseScale;
}
```

- [ ] **Step 6: Run focused tests and compile**

Run `WaveResultPanelTests`, `AllyDeploymentLimitTests`, and Task 1's `dotnet build` command.

Expected: all pass; final `ResultPanel` remains untouched and initialization still does not animate.

- [ ] **Step 7: Commit result presentation code**

```powershell
git add -- `
  'pin-ball/Assets/02. Scripts/03. UI/WaveResultPanel.cs' `
  'pin-ball/Assets/02. Scripts/03. UI/WaveResultPanel.cs.meta' `
  'pin-ball/Assets/02. Scripts/03. UI/Editor/WaveResultPanelTests.cs' `
  'pin-ball/Assets/02. Scripts/03. UI/Editor/WaveResultPanelTests.cs.meta' `
  'pin-ball/Assets/02. Scripts/03. UI/StatusPanel.cs'
git commit -m "feat: add wave result and resource feedback"
```

---

### Task 4: Board and Launcher Interaction Glow

**Files:**
- Modify: `Assets/02. Scripts/Pinball/Editor/ArcaneGlowMathTests.cs`
- Modify: `Assets/02. Scripts/Visual/ArcaneMaskGlowController.cs`
- Create: `Assets/02. Scripts/Pinball/PinballLauncherGlowController.cs`
- Create: `Assets/02. Scripts/Pinball/PinballLauncherGlowController.cs.meta`
- Modify: `Assets/02. Scripts/Pinball/PinballLauncherController.cs:6-36,70-115,127-177`

**Interfaces:**
- Produces: `ArcaneGlowMath.CalculateLauncherIntensity(...)`
- Produces: `ArcaneMaskGlowController.SetScaleMultiplier(float multiplier)`
- Produces: `PinballLauncherGlowController.SetLoaded(bool)`, `SetHovered(bool)`, `SetPullRatio(float)`, `PlayLaunch()`, and `ResetInteraction()`
- Consumes: existing `ArcaneMaskGlowController.SetActiveIntensity(float)` and `Pulse(float, float)`

- [ ] **Step 1: Write failing launcher-intensity ordering tests**

Add to `ArcaneGlowMathTests.cs`:

```csharp
[Test]
public void CalculateLauncherIntensity_OrdersInteractionStates()
{
    float unloaded = ArcaneGlowMath.CalculateLauncherIntensity(
        false, false, 0f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);
    float loadedIdle = ArcaneGlowMath.CalculateLauncherIntensity(
        true, false, 0f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);
    float hover = ArcaneGlowMath.CalculateLauncherIntensity(
        true, true, 0f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);
    float fullPull = ArcaneGlowMath.CalculateLauncherIntensity(
        true, true, 1f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);

    Assert.That(unloaded, Is.LessThan(loadedIdle));
    Assert.That(loadedIdle, Is.LessThan(hover));
    Assert.That(hover, Is.LessThan(fullPull));
}

[TestCase(-1f, 0f)]
[TestCase(0.5f, 0.5f)]
[TestCase(2f, 1f)]
public void CalculateLauncherIntensity_ClampsPullRatio(
    float pullRatio,
    float expectedPull)
{
    float result = ArcaneGlowMath.CalculateLauncherIntensity(
        true, false, pullRatio, 0f, 0.2f, 1f, 1.5f, 2f, 0f);
    Assert.That(result, Is.EqualTo(Mathf.Lerp(1f, 2f, expectedPull)));
}
```

- [ ] **Step 2: Run the focused test and verify failure**

Use Task 1's Unity command with `ArcaneGlowMathTests`, `launcher-glow-results.xml`, and `launcher-glow.log`.

Expected: compilation fails because `CalculateLauncherIntensity` does not exist.

- [ ] **Step 3: Add pure launcher glow math**

Add to `ArcaneGlowMath`:

```csharp
public static float CalculateLauncherIntensity(
    bool loaded,
    bool hovered,
    float pullRatio,
    float breathing01,
    float unloadedIntensity,
    float loadedIntensity,
    float hoverIntensity,
    float fullPullIntensity,
    float breathingAmplitude)
{
    if (!loaded) return Mathf.Max(0f, unloadedIntensity);

    float idle = hovered ? hoverIntensity : loadedIntensity;
    idle += Mathf.Clamp01(breathing01) * Mathf.Max(0f, breathingAmplitude);
    return Mathf.Lerp(
        Mathf.Max(0f, idle),
        Mathf.Max(0f, fullPullIntensity),
        Mathf.Clamp01(pullRatio));
}
```

- [ ] **Step 4: Add a visual-only scale multiplier to the mask controller**

Add:

```csharp
private float scaleMultiplier = 1f;

public void SetScaleMultiplier(float multiplier)
{
    scaleMultiplier = Mathf.Max(0.01f, multiplier);
}
```

In `SyncToSource`, calculate:

```csharp
var scale = ArcaneGlowMath.CalculateMaskScale(
    sourceRenderer.sprite.bounds.size,
    glowRenderer.sprite.bounds.size) * scaleMultiplier;
```

Keep center-offset recalculation based on this scale so enlargement stays centered. Do not change the source renderer or collider.

- [ ] **Step 5: Implement focused launcher glow state**

Create `PinballLauncherGlowController.cs`:

```csharp
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PinballLauncherGlowController : MonoBehaviour
{
    [SerializeField] private ArcaneMaskGlowController glow;
    [SerializeField, Min(0f)] private float unloadedIntensity = 0.2f;
    [SerializeField, Min(0f)] private float loadedIntensity = 1.15f;
    [SerializeField, Min(0f)] private float hoverIntensity = 1.55f;
    [SerializeField, Min(0f)] private float fullPullIntensity = 2.1f;
    [SerializeField, Min(0f)] private float launchPulseIntensity = 2.6f;
    [SerializeField, Min(0f)] private float breathingAmplitude = 0.12f;
    [SerializeField, Min(0.01f)] private float breathingSpeed = 2.2f;
    [SerializeField, Range(1f, 1.2f)] private float hoverScale = 1.08f;

    private bool _loaded;
    private bool _hovered;
    private float _pullRatio;

    private void Update()
    {
        if (glow == null) return;

        float breathing01 =
            (Mathf.Sin(Time.unscaledTime * breathingSpeed) + 1f) * 0.5f;
        glow.SetActiveIntensity(ArcaneGlowMath.CalculateLauncherIntensity(
            _loaded,
            _hovered,
            _pullRatio,
            breathing01,
            unloadedIntensity,
            loadedIntensity,
            hoverIntensity,
            fullPullIntensity,
            breathingAmplitude));
        glow.SetScaleMultiplier(
            _loaded && _hovered
                ? Mathf.Lerp(hoverScale, 1.12f, _pullRatio)
                : Mathf.Lerp(1f, 1.12f, _pullRatio));
    }

    public void SetLoaded(bool loaded)
    {
        _loaded = loaded;
        if (!loaded) ResetInteraction();
    }

    public void SetHovered(bool hovered) => _hovered = hovered;
    public void SetPullRatio(float pullRatio) =>
        _pullRatio = Mathf.Clamp01(pullRatio);
    public void PlayLaunch() =>
        glow?.Pulse(launchPulseIntensity, 0.24f);

    public void ResetInteraction()
    {
        _hovered = false;
        _pullRatio = 0f;
        glow?.SetScaleMultiplier(1f);
    }
}
```

- [ ] **Step 6: Connect launcher input events without changing mechanics**

Add to `PinballLauncherController`:

```csharp
[SerializeField] private PinballLauncherGlowController glowController;

private void OnMouseEnter()
{
    glowController?.SetHovered(_hasLoadedBall);
}

private void OnMouseExit()
{
    if (!_isDragging) glowController?.SetHovered(false);
}
```

After calculating `pullRatio` in `ApplyVisualPull`, call:

```csharp
glowController?.SetPullRatio(pullRatio);
```

On successful launch, call `glowController?.PlayLaunch()` before marking unloaded. Forward loaded state:

```csharp
public void SetLoaded(bool isLoaded)
{
    _hasLoadedBall = isLoaded;
    glowController?.SetLoaded(isLoaded);
}
```

At the end of `ResetVisuals`, call `glowController?.SetPullRatio(0f)`. In `OnDisable`, call `glowController?.ResetInteraction()`. Do not change pull distance, lever angle, piston movement, spring compression, or launch arguments.

- [ ] **Step 7: Run focused tests and compile**

Run `ArcaneGlowMathTests`, `PinballMotionTests`, and Task 1's `dotnet build` command.

Expected: all pass; intensity ordering is enforced and existing compression/input tests remain green.

- [ ] **Step 8: Commit glow behavior code**

```powershell
git add -- `
  'pin-ball/Assets/02. Scripts/Pinball/Editor/ArcaneGlowMathTests.cs' `
  'pin-ball/Assets/02. Scripts/Visual/ArcaneMaskGlowController.cs' `
  'pin-ball/Assets/02. Scripts/Pinball/PinballLauncherGlowController.cs' `
  'pin-ball/Assets/02. Scripts/Pinball/PinballLauncherGlowController.cs.meta' `
  'pin-ball/Assets/02. Scripts/Pinball/PinballLauncherController.cs'
git commit -m "feat: add pinball interaction glow feedback"
```

---

### Task 5: Scene-Place Result UI and Glow Renderers

**Files:**
- Create: `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs`
- Create: `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs.meta`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: `WaveResultPanel`, `ArcaneMaskGlowController`, `PinballLauncherGlowController`, and `PinballLauncherController.glowController`
- Reuses: `Assets/03. Images/Pinball/Arcane/pinball_board_arcane_mask.png`
- Reuses: `Assets/09. Materials/Pinball/ArcaneDeviceAdditive.mat`
- Preserves: existing `WavePanel.launchCostText` and runtime `발사 {cost}G` copy

- [ ] **Step 1: Write the failing scene-wiring test**

Create `GameplayFeedbackSceneTests.cs`:

```csharp
#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class GameplayFeedbackSceneTests
{
    [Test]
    public void GameScene_WiresResultCostAndInteractionGlow()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");

        var resultPanel = Object.FindFirstObjectByType<WaveResultPanel>(
            FindObjectsInactive.Include);
        Assert.That(resultPanel, Is.Not.Null);
        AssertReference(resultPanel, "panelRect");
        AssertReference(resultPanel, "canvasGroup");
        AssertReference(resultPanel, "resultText");

        var wavePanel = Object.FindFirstObjectByType<WavePanel>(
            FindObjectsInactive.Include);
        Assert.That(
            ReadReference<TextMeshProUGUI>(wavePanel, "launchCostText"),
            Is.Not.Null);

        GameObject boardGlow = GameObject.Find("BoardGlow");
        Assert.That(boardGlow, Is.Not.Null);
        var boardRenderer = boardGlow.GetComponent<SpriteRenderer>();
        Assert.That(boardRenderer, Is.Not.Null);
        Assert.That(
            AssetDatabase.GetAssetPath(boardRenderer.sprite),
            Is.EqualTo(
                "Assets/03. Images/Pinball/Arcane/" +
                "pinball_board_arcane_mask.png"));
        Assert.That(
            AssetDatabase.GetAssetPath(boardRenderer.sharedMaterial),
            Is.EqualTo(
                "Assets/09. Materials/Pinball/ArcaneDeviceAdditive.mat"));

        GameObject lever = GameObject.Find("PlungerLever");
        Assert.That(lever, Is.Not.Null);
        var launcher = lever.GetComponent<PinballLauncherController>();
        var launcherGlow =
            lever.GetComponent<PinballLauncherGlowController>();
        Assert.That(launcher, Is.Not.Null);
        Assert.That(launcherGlow, Is.Not.Null);
        AssertReference(launcher, "glowController");
        AssertReference(launcherGlow, "glow");
        Assert.That(GameObject.Find("PlungerLeverGlow"), Is.Not.Null);
        Assert.That(
            lever.GetComponentInChildren<TMP_Text>(true),
            Is.Null,
            "Launcher must not contain an instructional text prompt.");
    }

    private static void AssertReference(Object target, string propertyName)
    {
        Assert.That(ReadReference<Object>(target, propertyName), Is.Not.Null);
    }

    private static T ReadReference<T>(Object target, string propertyName)
        where T : Object
    {
        Assert.That(target, Is.Not.Null, propertyName);
        var property = new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.objectReferenceValue as T;
    }
}
#endif
```

- [ ] **Step 2: Run the scene test and verify failure**

Use Task 1's Unity command with `GameplayFeedbackSceneTests`, `gameplay-feedback-scene-results.xml`, and `gameplay-feedback-scene.log`.

Expected: FAIL because `WaveResultPanel`, `BoardGlow`, `PlungerLeverGlow`, and launcher glow references are absent.

- [ ] **Step 3: Place and wire the intermediate result banner**

Under the existing game Canvas/UI hierarchy, create `WaveResultPanel` with:

- centered anchors and pivot, anchored position `(0, 40)`, size approximately `(620, 150)`;
- `CanvasGroup` alpha `0`, interactable `false`, blocks raycasts `false`;
- an `Image` using existing `ui_result_panel_content.png`, tinted dark translucent navy;
- `WaveResultPanel.panelRect` and `canvasGroup` referencing the root;
- child `ResultText` with centered `TextMeshProUGUI`, the existing bold project font, text `웨이브 클리어`, and raycast target disabled;
- `WaveResultPanel.resultText` referencing `ResultText`.

Keep the object active so `UIManager` initializes this unmanaged panel. The component hides it through alpha.

- [ ] **Step 4: Place the board mask glow**

Under the existing arcane board visual transform, add `BoardGlow`:

- sprite: `pinball_board_arcane_mask.png`;
- shared material: `ArcaneDeviceAdditive.mat`;
- a restrained cyan-gold tint;
- no collider or input component.

Add `ArcaneMaskGlowController` to existing `BoardVisual` or its nearest moving board-source object:

- `sourceRenderer`: existing full board renderer;
- `glowRenderer`: `BoardGlow` renderer;
- `baseIntensity`: `0.72`.

Use the controller's scale/center alignment. Do not change board transforms or colliders.

- [ ] **Step 5: Place and wire the launcher-handle glow**

Under `PlungerLever`, add `PlungerLeverGlow`:

- local position `(0, 0, -0.01)`, identity rotation, initial scale `(1, 1, 1)`;
- sprite: the same handle sprite as `PlungerLever`;
- shared material: `ArcaneDeviceAdditive.mat`;
- no collider, text, or input component.

On `PlungerLever`:

- add `ArcaneMaskGlowController` with the existing handle renderer as source, the child as glow, and base intensity `0.2`;
- add `PinballLauncherGlowController` and assign its `glow` reference;
- assign it to `PinballLauncherController.glowController`;
- keep all existing load-point, piston, spring, direction, distance, angle, travel, collider, and transform values unchanged.

- [ ] **Step 6: Run scene, UI, cost, and glow tests**

Run these filters separately with unique result/log filenames:

```text
GameplayFeedbackSceneTests
WaveResultPanelTests
ArcaneGlowMathTests
AllyDeploymentLimitTests
```

Expected: every command exits `0`; the result panel and references exist, board uses the exact existing mask/material, launcher has no instruction text, launch cost remains assigned, and glow ordering passes.

- [ ] **Step 7: Perform focused Play Mode checks**

In Unity 6.0.0.79f1:

1. Result banner starts invisible and never blocks raycasts.
2. Board glow remains restrained and moves with the preparation board.
3. Loaded handle breathes, hover is brighter, pull increases intensity, and successful launch pulses.
4. Handle collider, pivot, rotation, piston, spring, loaded ball, and launch direction behave as before.
5. No `당겨서 발사` or other launcher instruction text exists.

- [ ] **Step 8: Commit scene wiring and regression test**

```powershell
git add -- `
  'pin-ball/Assets/01. Scenes/02. Game.unity' `
  'pin-ball/Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs' `
  'pin-ball/Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs.meta'
git commit -m "feat: wire gameplay result and pinball glow visuals"
```

---

### Task 6: End-to-End Verification and AI Usage Record

**Files:**
- Modify: `.github/ai-use-log.md`

**Interfaces:**
- Consumes: all Task 1-5 code, scene wiring, tests, and actual verification results.
- Produces: factual project record; never claim an unrun test, build, or Play Mode check.

- [ ] **Step 1: Run the complete EditMode suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball' `
  -runTests -testPlatform EditMode `
  -testResults 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\gameplay-feedback-all-results.xml' `
  -logFile 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\gameplay-feedback-all.log'
```

Expected: exit `0`, all EditMode tests pass, and no C# compilation error appears.

- [ ] **Step 2: Compile the editor assembly once more**

Run Task 1's `dotnet build ... --no-restore` command.

Expected: exit `0`. Existing package warnings may remain; no errors are allowed.

- [ ] **Step 3: Verify the complete gameplay loop in Play Mode**

Record each result:

1. At `0` allies, launch is available when ball/gold/state allow and wave start is rejected.
2. At `1` and `5` allies, wave start succeeds.
3. At `6+` allies, further pinball launches remain possible and wave start is rejected with ally-count emphasis.
4. Kill one ally and finish the wave; it never returns, while survivors return to saved preparation positions with reset combat state.
5. Clear a non-final wave; state becomes `Resolving`, `웨이브 클리어` and gold feedback begin immediately, preparation stays locked for two seconds, then the next wave is `Pending`.
6. Fail a survivable wave; `방어 실패`, HP feedback, and retry-gold feedback begin immediately, then preparation returns after two seconds.
7. Clear the final wave and lose all HP in separate runs; intermediate banner precedes final `ResultPanel` by the full resolution delay.
8. Verify board/handle glow behavior from Task 5 with no instruction text.
9. Verify launch cost still updates `50G -> 80G -> 110G` under default data and changes to unavailable color when unaffordable.

- [ ] **Step 4: Verify the configured WebGL path**

The repository has no batch `BuildPipeline.BuildPlayer` entry point and no checked-in Build Profile asset. Use Unity Build Profiles without installing packages or changing scene order:

1. Select WebGL.
2. Keep the three enabled scenes from `ProjectSettings/EditorBuildSettings.asset`.
3. Make a Development Build in an ignored disposable path such as `pin-ball/Builds/WebGL-GameplayFeedback`.
4. Record success or the exact blocking error.

Expected: WebGL build succeeds. If environment or license blocks it, record that exact limitation instead of reporting success.

- [ ] **Step 5: Append the factual AI usage record**

Append this structure, replacing only factual file/result details with what actually occurred:

```markdown
## 2026-08-10 게임플레이 피드백 마일스톤

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군 보유 제한 해제와 5명 전투 제한, 아군 영구 사망, 2초 웨이브 결과 대기, 강화된 HP/골드 피드백, 발사 비용 유지, 보드/손잡이 발광
- AI 제안 내용: 명시적 Resolving 상태, UnitManager 영구 사망 정리, UI 비권위 결과 이벤트, 기존 mask/additive/Bloom 재사용
- AI 실제 수정 영역: 실제 변경한 C# 런타임/테스트, Game 씬, AI 사용 기록 파일 목록
- 사용자 직접 결정/수정 필요 영역: 사용자가 2초 결과 지연, 전용 종료 상태, 텍스트 없는 손잡이 시각 피드백, 금색 유닛 조명/그림자/튜토리얼 제외를 결정; 최종 강도와 배너 위치는 Game View에서 조정 가능
- 중요한 프롬프트/지침: 기존 구조 보존, Inspector 참조, SetActive/풀링, SerializeField underscore 금지, 최소 변경, WebGL 경량 VFX
- 테스트/검증 결과: 실제 EditMode 개수/결과, dotnet build 결과, Play Mode 확인 범위, WebGL 빌드 결과 또는 정확한 제한
```

- [ ] **Step 6: Check final scope and whitespace**

```powershell
git diff --check
git status --short
git diff --stat
```

Expected: only approved implementation, generated `.meta` files, Game scene, docs, AI log, and pre-existing user-owned files appear. Leave `.codex_tmp/` and unrelated changes untouched.

- [ ] **Step 7: Commit the verification record**

```powershell
git add -- '.github/ai-use-log.md'
git commit -m "docs: record gameplay feedback AI usage"
```

## Plan Self-Review

- Spec coverage: unlimited ownership, wave-only five-unit cap, permanent death, explicit `Resolving`, two-second hold, immediate reward/damage, result banner, stronger HP/gold feedback, launch-cost preservation, board glow, handle affordance, scene wiring, Play Mode checks, EditMode tests, WebGL, and AI logging each map to a task.
- Deferred scope: tutorial, golden unit light pool, and ground-shadow work are absent from implementation tasks.
- Completeness scan: every step names concrete files, symbols, commands, expected failures, and expected passing results.
- Type consistency: `EWaveResolutionResult`, `WaveResolutionState`, `BattleResolutionPolicy`, `OnWaveResolutionStarted`, `WavePanel.IsLaunchAvailable`, and launcher-glow names match across producers, consumers, tests, and scene wiring.
- Timing consistency: manager duration is exactly `2f`; banner animation totals `1.95f` and never owns progression.
- Rule consistency: pinball availability has no ally-count input; wave start remains `1..5`; simultaneous wipe remains clear-first.
- Safety: no package, top-level folder, data-format, physics, collider, ground-shadow, tutorial, or golden-unit-light change is included.
