# Reactive and Async Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `ItemManager` 외 Manager 상태를 UniRx로 노출하고 프로젝트 소유 코루틴 세 개를 UniTask로 교체한다.

**Architecture:** 상태의 소유자는 `ReactiveProperty<T>`를 private으로 유지하고 외부에는 `IReadOnlyReactiveProperty<T>`만 제공한다. UI와 Manager 구독은 `AddTo`로 수명을 묶는다. 전투 효과 지연은 전투가 `Active`일 때만 시간을 누적하고, 아이템 지연은 기존 `WaitForSeconds`와 같은 scaled time으로 처리한다.

**Tech Stack:** Unity 6000.0.79f1, C#, UniRx, UniTask, NUnit/EditMode tests

## Global Constraints

- `ItemManager`의 `IItemEventListener` 및 아이템별 구독 구조는 유지한다.
- `ShopSlot`의 로컬 `Action<Item>`과 Unity Button 리스너는 유지한다.
- 새 외부 패키지는 추가하지 않는다. 저장소의 `Assets/Plugins/UniRx`와 manifest의 UniTask를 사용한다.
- `[SerializeField]` 필드에는 underscore를 사용하지 않는다.
- 이 계획에서는 전투 규칙·합성·스킬 ID 분기를 변경하지 않는다.

---

### Task 1: BattleManager 상태를 읽기 전용 ReactiveProperty로 전환

**Files:**
- Create: `Assets/02. Scripts/Editor/Tests/ReactiveManagerTests.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`

**Interfaces:**
- Produces: `IReadOnlyReactiveProperty<EWaveState> State`
- Produces: `IReadOnlyReactiveProperty<int> WaveIndex`
- Produces: `IReadOnlyReactiveProperty<int> PlayerHp`
- Produces: `IReadOnlyReactiveProperty<int> Gold`
- Preserves: `TrySpendGold(int)`, `AddGold(int)`, `StartWave()`

- [ ] **Step 1: Write the failing initial-value and mutation tests**

```csharp
using NUnit.Framework;
using UniRx;
using UnityEngine;

public class ReactiveManagerTests
{
    [Test]
    public void BattleManagerPublishesInitialAndChangedGold()
    {
        var go = new GameObject("BattleManagerTest");
        go.SetActive(false);
        var manager = go.AddComponent<BattleManager>();
        manager.startingGold = 10;
        manager.playerMaxHp = 20;
        go.SetActive(true);

        var observed = -1;
        using var subscription = manager.Gold.Subscribe(value => observed = value);
        Assert.That(observed, Is.EqualTo(10));

        Assert.That(manager.TrySpendGold(3), Is.True);
        Assert.That(manager.Gold.Value, Is.EqualTo(7));
        Assert.That(observed, Is.EqualTo(7));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void BattleManagerStartsInPendingWithMaxHp()
    {
        var go = new GameObject("BattleManagerTest");
        go.SetActive(false);
        var manager = go.AddComponent<BattleManager>();
        manager.playerMaxHp = 27;
        go.SetActive(true);

        Assert.That(manager.State.Value, Is.EqualTo(EWaveState.Pending));
        Assert.That(manager.PlayerHp.Value, Is.EqualTo(27));
        Assert.That(manager.WaveIndex.Value, Is.Zero);
        Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: Run the focused tests and confirm failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter ReactiveManagerTests -testResults "Temp/reactive-tests.xml" -logFile "Temp/reactive-tests.log"
```

Expected: compile failure because `State`, `WaveIndex`, `PlayerHp`, and reactive `Gold` do not exist.

- [ ] **Step 3: Replace Action events and scalar backing fields**

```csharp
using UniRx;

private readonly ReactiveProperty<EWaveState> state = new();
private readonly IntReactiveProperty waveIndex = new();
private readonly IntReactiveProperty playerHp = new();
private readonly IntReactiveProperty gold = new();

public IReadOnlyReactiveProperty<EWaveState> State => state;
public IReadOnlyReactiveProperty<int> WaveIndex => waveIndex;
public IReadOnlyReactiveProperty<int> PlayerHp => playerHp;
public IReadOnlyReactiveProperty<int> Gold => gold;

public BattleWaveData CurrentWave => waveList[waveIndex.Value];
public int CurrentWaveNumber => waveIndex.Value + 1;
```

Update every mutation to assign `.Value`, remove `OnStateChanged`, `OnWaveChanged`, `OnHpChanged`, `OnGoldChanged`, and dispose all four properties before `base.OnDestroy()`.

- [ ] **Step 4: Run the focused tests and verify pass**

Run the Step 2 command. Expected: both tests pass.

- [ ] **Step 5: Commit the BattleManager reactive state**

```powershell
git add -- "Assets/02. Scripts/Battle/BattleManager.cs" "Assets/02. Scripts/Editor/Tests/ReactiveManagerTests.cs"
git commit -m "refactor: expose battle state with UniRx"
```

### Task 2: Pinball 상태와 기존 구독자를 UniRx로 전환

**Files:**
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/ShopPanel.cs`
- Modify: `Assets/02. Scripts/Editor/Tests/ReactiveManagerTests.cs`

**Interfaces:**
- Consumes: `BattleManager.State`, `WaveIndex`, `PlayerHp`, `Gold`
- Produces: `IReadOnlyReactiveProperty<EPinballState> PinballManager.State`
- Produces: `bool PinballManager.IsIdle`

- [ ] **Step 1: Add a failing PinballManager state-surface test**

```csharp
[Test]
public void PinballManagerStartsIdle()
{
    var go = new GameObject("PinballManagerTest");
    var manager = go.AddComponent<PinballManager>();

    Assert.That(manager.State.Value, Is.EqualTo(EPinballState.Idle));
    Assert.That(manager.IsIdle, Is.True);
    Object.DestroyImmediate(go);
}
```

- [ ] **Step 2: Run ReactiveManagerTests and confirm compile failure**

Run the Task 1 Step 2 command. Expected: `PinballManager.State` and `IsIdle` are missing.

- [ ] **Step 3: Add the reactive pinball state and replace event invocations**

```csharp
using UniRx;

private readonly ReactiveProperty<EPinballState> state =
    new(EPinballState.Idle);

public IReadOnlyReactiveProperty<EPinballState> State => state;
public bool IsIdle => _activeBalls.Count == 0;
```

Set `state.Value = EPinballState.Launched` after a ball becomes active and `state.Value = EPinballState.Idle` after the final ball is released. Dispose `state` in `OnDestroy`.

- [ ] **Step 4: Convert all five consumers to subscription lifetime management**

```csharp
_battleManager.State
    .Subscribe(OnStateChanged)
    .AddTo(this);

_battleManager.Gold
    .Subscribe(_ => RefreshPurchaseStates())
    .AddTo(this);
```

Use direct subscriptions for `StatusPanel`. In `WavePanel`, combine battle and pinball state and set `startButton.interactable`; this plan uses only `Pending && Idle`, and plan 03 adds the promotion-pending term.

```csharp
Observable.CombineLatest(
        _battleManager.State,
        _pinballManager.State,
        (battle, pinball) =>
            battle == EWaveState.Pending && pinball == EPinballState.Idle)
    .Subscribe(canStart => startButton.interactable = canStart)
    .AddTo(this);
```

Replace `_battleManager.Gold` scalar reads with `_battleManager.Gold.Value`. Remove all matching `+=`/`-=` code.

- [ ] **Step 5: Run focused tests and Unity compilation**

Run the Task 1 Step 2 command. Expected: all `ReactiveManagerTests` pass and no removed Action event references remain.

- [ ] **Step 6: Commit the reactive consumers**

```powershell
git add -- "Assets/02. Scripts/Pinball/PinballManager.cs" "Assets/02. Scripts/Battle/UnitManager.cs" "Assets/02. Scripts/03. UI/WavePanel.cs" "Assets/02. Scripts/03. UI/StatusPanel.cs" "Assets/02. Scripts/03. UI/ShopPanel.cs" "Assets/02. Scripts/Editor/Tests/ReactiveManagerTests.cs"
git commit -m "refactor: observe manager state with UniRx"
```

### Task 3: 프로젝트 코루틴을 UniTask로 교체

**Files:**
- Create: `Assets/02. Scripts/Editor/Tests/AsyncPolicyTests.cs`
- Modify: `Assets/02. Scripts/Item/ItemManager.cs`
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs`

**Interfaces:**
- Preserves: `ItemManager.RaiseDelayed(EItem, float)`
- Preserves: `UnitBase.ApplyDamageOverTime(float, float, float)`
- Preserves: `UnitBase.ApplySlowAfterDelay(float, float, float, float)`
- Produces: private `UniTask WaitForActiveCombatSecondsAsync(float, CancellationToken)`

- [ ] **Step 1: Write a failing source-policy test for project-owned scripts**

```csharp
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class AsyncPolicyTests
{
    [TestCase("Item/ItemManager.cs")]
    [TestCase("Battle/UnitBase.cs")]
    public void ProjectAsyncCodeDoesNotUseCoroutines(string relativePath)
    {
        var path = Path.Combine(Application.dataPath, "02. Scripts", relativePath);
        var source = File.ReadAllText(path);

        StringAssert.DoesNotContain("StartCoroutine", source);
        StringAssert.DoesNotContain("IEnumerator", source);
        StringAssert.DoesNotContain("yield return", source);
        StringAssert.DoesNotContain("WaitForSeconds", source);
    }
}
```

- [ ] **Step 2: Run AsyncPolicyTests and confirm failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter AsyncPolicyTests -testResults "Temp/async-tests.xml" -logFile "Temp/async-tests.log"
```

Expected: both source tests fail on the existing coroutine APIs.

- [ ] **Step 3: Convert ItemManager delayed dispatch**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

private CancellationTokenSource delayedEventCancellation = new();

public void RaiseDelayed(EItem item, float delaySeconds)
{
    if (delaySeconds <= 0f)
    {
        Raise(item);
        return;
    }

    RaiseAfterDelayAsync(item, delaySeconds, delayedEventCancellation.Token)
        .Forget(Debug.LogException);
}

private async UniTask RaiseAfterDelayAsync(
    EItem item,
    float delaySeconds,
    CancellationToken cancellationToken)
{
    var canceled = await UniTask.Delay(
        TimeSpan.FromSeconds(delaySeconds),
        DelayType.DeltaTime,
        PlayerLoopTiming.Update,
        cancellationToken)
        .SuppressCancellationThrow();
    if (canceled) return;

    Raise(item);
}
```

`Clear()` cancels and disposes the current source, then creates a fresh source. `OnDestroy()` cancels and disposes it before `base.OnDestroy()`.

- [ ] **Step 4: Convert UnitBase delayed status work and preserve Pending pause**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

private async UniTask WaitForActiveCombatSecondsAsync(
    float seconds,
    CancellationToken cancellationToken)
{
    var elapsed = 0f;
    while (elapsed < seconds)
    {
        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        if (_battleManager.State.Value == EWaveState.Active)
        {
            elapsed += Time.deltaTime;
        }
    }
}
```

Convert damage-over-time and delayed-slow loops to `async UniTask`, pass `this.GetCancellationTokenOnDestroy()`, keep `_damageOverTimeVersion`, and catch only `OperationCanceledException` at the fire-and-forget boundary. Do not change their public signatures or numerical formulas.

Add `private BattleManager _battleManager;` to `UnitBase` and assign it with `App.Get<BattleManager>()` in `Initialize` before either async effect can start. Plan 03 later subscribes to the same reactive state to shift absolute combat deadlines.

- [ ] **Step 5: Run source-policy tests and grep the whole project-owned script tree**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter AsyncPolicyTests -testResults "Temp/async-tests.xml" -logFile "Temp/async-tests.log"
rg -n "StartCoroutine|StopCoroutine|IEnumerator|yield return|WaitForSeconds" "Assets/02. Scripts" -g "*.cs"
```

Expected: tests pass and `rg` returns no matches.

- [ ] **Step 6: Commit the UniTask conversion**

```powershell
git add -- "Assets/02. Scripts/Item/ItemManager.cs" "Assets/02. Scripts/Battle/UnitBase.cs" "Assets/02. Scripts/Editor/Tests/AsyncPolicyTests.cs"
git commit -m "refactor: replace project coroutines with UniTask"
```

### Task 4: Run the complete foundation verification

**Files:**
- Verify only

**Interfaces:**
- Produces stable interfaces consumed by plans 02 and 03.

- [ ] **Step 1: Run all EditMode tests**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults "Temp/foundation-tests.xml" -logFile "Temp/foundation-tests.log"
```

Expected: process exit code 0 and no compiler errors in `Temp/foundation-tests.log`.

- [ ] **Step 2: Audit removed event and coroutine APIs**

```powershell
rg -n "public event Action<(EWaveState|EPinballState|int)>|On(State|Wave|Hp|Gold)Changed\?\.Invoke" "Assets/02. Scripts" -g "*.cs"
rg -n "StartCoroutine|StopCoroutine|IEnumerator|yield return|WaitForSeconds" "Assets/02. Scripts" -g "*.cs"
```

Expected: neither command finds a project-owned occurrence. Callback method names such as `OnStateChanged` may remain as UniRx subscription handlers.

- [ ] **Step 3: Confirm a clean task boundary**

```powershell
git status --short
```

Expected: no uncommitted files from plan 01.
