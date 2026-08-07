# Battle Preparation, Merge, and Results Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투 상태를 안전하게 종료·정지하고, Pending 전용 입력·드래그 합성·5레벨 승급·결과 UI를 완성한다.

**Architecture:** `BattleManager`는 상태와 플레이어 자원의 단일 소유자로 유지하고 순수 `BattlePhaseRules`로 시작·패배 결정을 검증한다. `UnitManager`가 합성과 승급 요청을 관리하며 `AllyDragController`는 입력만 전달한다. `PromotionPanel`과 `BattleResultPanel`은 Game 씬에 사전 배치되어 UniRx 스트림을 구독한다.

**Tech Stack:** Unity 6000.0.79f1, C#, UniRx, UGUI, TextMeshPro, NUnit/EditMode tests

## Global Constraints

- 아군 전멸 후 플레이어 HP가 남으면 `Pending`으로 전환한다.
- 돌파 피해 계산 후 생존 적을 모두 제거하고 상태를 바꾼다.
- `Pending`에서만 핀볼·상점·목표 선택·드래그 합성을 허용한다.
- 활성 핀볼 또는 미완료 승급이 있으면 웨이브를 시작하지 않는다.
- 합성은 같은 `UnitId`와 같은 `Level`의 서로 다른 유닛 두 개만 허용한다.
- 승급은 5레벨에 `previousJob` 후보 중 하나를 반드시 선택한다.
- 승급 후 같은 직업·같은 레벨끼리 10레벨까지 합성한다.
- 체력·마나 비율과 대상 유닛의 modifier/핀볼 공격 보너스를 보존한다.
- Title 씬 내부 로직은 변경하지 않는다.

---

### Task 1: 전투 단계와 패배 계산을 순수 규칙으로 고정

**Files:**
- Create: `Assets/02. Scripts/Battle/BattlePhaseRules.cs`
- Create: `Assets/02. Scripts/Editor/Tests/BattlePhaseRulesTests.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`

**Interfaces:**
- Produces: `BattlePhaseRules.CanUsePreparation(EWaveState, bool)`
- Produces: `BattlePhaseRules.CanStartWave(EWaveState, EPinballState, bool)`
- Produces: `BattlePhaseRules.ResolveBattleEnd(int, int)`
- Produces: `BattlePhaseRules.ResolveFailedWave(int, int, int, int)`
- Produces: `UnitManager.ClearRemainingEnemies()`

- [ ] **Step 1: Write failing phase and defeat tests**

```csharp
using NUnit.Framework;

public class BattlePhaseRulesTests
{
    [TestCase(EWaveState.Pending, EPinballState.Idle, false, true)]
    [TestCase(EWaveState.Pending, EPinballState.Launched, false, false)]
    [TestCase(EWaveState.Pending, EPinballState.Idle, true, false)]
    [TestCase(EWaveState.Active, EPinballState.Idle, false, false)]
    public void CanStartWaveRequiresSafePending(
        EWaveState battle,
        EPinballState pinball,
        bool promotionPending,
        bool expected)
    {
        Assert.That(
            BattlePhaseRules.CanStartWave(battle, pinball, promotionPending),
            Is.EqualTo(expected));
    }

    [Test]
    public void FailedWaveReturnsPendingWhenHpRemains()
    {
        var result = BattlePhaseRules.ResolveFailedWave(
            playerHp: 20,
            breachDamage: 8,
            barrierReduction: 3,
            minimumDamage: 1);

        Assert.That(result.Damage, Is.EqualTo(5));
        Assert.That(result.PlayerHp, Is.EqualTo(15));
        Assert.That(result.NextState, Is.EqualTo(EWaveState.Pending));
    }

    [Test]
    public void FailedWaveReturnsDefeatAtZeroHp()
    {
        var result = BattlePhaseRules.ResolveFailedWave(4, 10, 0, 1);
        Assert.That(result.PlayerHp, Is.Zero);
        Assert.That(result.NextState, Is.EqualTo(EWaveState.Defeat));
    }

    [Test]
    public void AllyWipeWinsPriorityWhenBothTeamsReachZero()
    {
        Assert.That(
            BattlePhaseRules.ResolveBattleEnd(
                remainingAllies: 0,
                remainingEnemies: 0),
            Is.EqualTo(EBattleEndReason.AllyDefeated));
    }
}
```

- [ ] **Step 2: Run focused tests and confirm compile failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter BattlePhaseRulesTests -testResults "Temp/battle-phase-tests.xml" -logFile "Temp/battle-phase-tests.log"
```

Expected: `BattlePhaseRules` and result type are missing.

- [ ] **Step 3: Implement the exact rules**

```csharp
public readonly struct FailedWaveResult
{
    public int Damage { get; }
    public int PlayerHp { get; }
    public EWaveState NextState { get; }

    public FailedWaveResult(int damage, int playerHp, EWaveState nextState)
    {
        Damage = damage;
        PlayerHp = playerHp;
        NextState = nextState;
    }
}

public static class BattlePhaseRules
{
    public static EBattleEndReason ResolveBattleEnd(
        int remainingAllies,
        int remainingEnemies)
    {
        if (remainingAllies <= 0)
        {
            return EBattleEndReason.AllyDefeated;
        }

        return remainingEnemies <= 0
            ? EBattleEndReason.EnemiesDefeated
            : EBattleEndReason.None;
    }

    public static bool CanUsePreparation(
        EWaveState state,
        bool modalOpen) =>
        state == EWaveState.Pending && !modalOpen;

    public static bool CanStartWave(
        EWaveState state,
        EPinballState pinballState,
        bool promotionPending) =>
        state == EWaveState.Pending &&
        pinballState == EPinballState.Idle &&
        !promotionPending;

    public static FailedWaveResult ResolveFailedWave(
        int playerHp,
        int breachDamage,
        int barrierReduction,
        int minimumDamage)
    {
        var damage = Mathf.Max(
            Mathf.Max(1, minimumDamage),
            Mathf.Max(0, breachDamage) - Mathf.Max(0, barrierReduction));
        var nextHp = Mathf.Max(0, playerHp - damage);
        return new FailedWaveResult(
            damage,
            nextHp,
            nextHp == 0 ? EWaveState.Defeat : EWaveState.Pending);
    }
}
```

Define `EBattleEndReason` in the same file with `None`, `AllyDefeated`, and `EnemiesDefeated`. Replace `BattleManager.Update`'s enemy-first `if` chain with a switch over `ResolveBattleEnd`, ensuring simultaneous annihilation follows the fixed ally-wipe rule.

- [ ] **Step 4: Guard StartWave and order failed-wave cleanup**

Cache `PinballManager` and `UnitManager` in `BattleManager.Start`. At this task boundary `StartWave()` returns unless `CanStartWave(State.Value, pinballManager.State.Value, false)` is true. Task 3 replaces the final argument with `unitManager.HasPendingPromotion.Value` after the promotion stream exists.

```csharp
private void DefeatWave()
{
    if (state.Value != EWaveState.Active) return;

    var breachDamage = _unitManager.CalculateRemainingBreachDamage();
    var result = BattlePhaseRules.ResolveFailedWave(
        playerHp.Value,
        breachDamage,
        _barrierDamageReduction,
        _minimumBarrierDamage);

    _unitManager.ClearRemainingEnemies();
    playerHp.Value = result.PlayerHp;
    ChangeState(result.NextState);
}
```

`ClearRemainingEnemies()` delegates to the existing private clear loop and guarantees the active enemy list is empty.

- [ ] **Step 5: Run phase tests and verify pass**

Run the Step 2 command. Expected: all phase and damage calculations pass.

- [ ] **Step 6: Commit battle resolution rules**

```powershell
git add -- "Assets/02. Scripts/Battle/BattlePhaseRules.cs" "Assets/02. Scripts/Battle/BattleManager.cs" "Assets/02. Scripts/Battle/UnitManager.cs" "Assets/02. Scripts/Editor/Tests/BattlePhaseRulesTests.cs"
git commit -m "fix: resolve failed waves before pending"
```

### Task 2: Pending 밖의 전투·핀볼·상점 동작 차단

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballGoal.cs`
- Modify: `Assets/02. Scripts/03. UI/ShopPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/ShopSlot.cs`
- Modify: `Assets/02. Scripts/Editor/Tests/BattlePhaseRulesTests.cs`

**Interfaces:**
- Consumes: `BattlePhaseRules.CanUsePreparation`
- Consumes: `BattleManager.State`

- [ ] **Step 1: Add failing preparation-state cases**

```csharp
[TestCase(EWaveState.Pending, false, true)]
[TestCase(EWaveState.Pending, true, false)]
[TestCase(EWaveState.Active, false, false)]
[TestCase(EWaveState.Victory, false, false)]
[TestCase(EWaveState.Defeat, false, false)]
public void PreparationRequiresPendingWithoutModal(
    EWaveState state,
    bool modalOpen,
    bool expected)
{
    Assert.That(
        BattlePhaseRules.CanUsePreparation(state, modalOpen),
        Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run focused tests and confirm the added cases fail before implementation**

Expected: missing `CanUsePreparation` behavior or incorrect cases fail.

- [ ] **Step 3: Freeze UnitBase updates and absolute deadlines outside Active**

Subscribe to `BattleManager.State` in `Initialize`. On leaving Active, record `Time.time`. On re-entering Active, shift finite combat deadlines by the paused duration, including attack, hit, stun, shield, forced target, all modifier deadlines, knockback immunity, and `LastDamagedTime`. Add a protected virtual `OnCombatResumed(float pausedDuration)` so `AllyUnit` can shift its hit-mana cooldown.

```csharp
private void Update()
{
    if (!IsAlive || _battleManager.State.Value != EWaveState.Active)
    {
        return;
    }

    RefreshTimedEffects();
    _skillController?.Tick(Time.deltaTime);
    if (Time.time < _stunnedUntil)
    {
        _state = EBattleUnitState.Idle;
    }
    else
    {
        Tick();
    }

    if (_state == EBattleUnitState.Hit && Time.time > _hitUntilTime)
    {
        _state = EBattleUnitState.Idle;
    }

    UpdateLabel();
    UpdateVisual();
}
```

The plan 01 UniTask combat delay helper already accumulates time only while Active, so DOT and delayed slow remain paused without deadline shifting.

- [ ] **Step 4: Add manager-level guards before any mutation**

In `PinballManager`, return before movement, launch cost spending, goal selection, or goal swap unless `BattlePhaseRules.CanUsePreparation(_battleManager.State.Value, false)` is true. Task 3 replaces `false` with the promotion-pending value. Ball hit/miss/release continues to work so an already active ball can always resolve.

In `ShopPanel`, return before reroll cost spending or purchase cost spending unless preparation is allowed. At this task boundary the phase uses `BattlePhaseRules.CanUsePreparation(state, false)`; Task 3 adds promotion pending. Track the reactive phase and pass it into `ShopSlot.RefreshState`.

```csharp
public void RefreshState(
    int currentGold,
    bool isPurchased,
    bool preparationAllowed)
{
    var canPurchase =
        preparationAllowed &&
        Item != null &&
        !isPurchased &&
        currentGold >= Item.Cost;
    purchaseButton.interactable = canPurchase;
    costText.color = canPurchase ? availableCostColor : unavailableCostColor;
}
```

- [ ] **Step 5: Run phase tests and Unity compilation**

Expected: phase tests pass, UI signatures compile, and Active-state calls cannot spend gold or launch balls.

- [ ] **Step 6: Commit state gating and freeze behavior**

```powershell
git add -- "Assets/02. Scripts/Battle/UnitBase.cs" "Assets/02. Scripts/Pinball/PinballManager.cs" "Assets/02. Scripts/Pinball/PinballGoal.cs" "Assets/02. Scripts/03. UI/ShopPanel.cs" "Assets/02. Scripts/03. UI/ShopSlot.cs" "Assets/02. Scripts/Editor/Tests/BattlePhaseRulesTests.cs"
git commit -m "fix: restrict preparation actions to pending"
```

### Task 3: 합성 규칙과 유닛 진행 데이터 구현

**Files:**
- Create: `Assets/02. Scripts/Battle/AllyMergeRules.cs`
- Create: `Assets/02. Scripts/Editor/Tests/AllyMergeRulesTests.cs`
- Modify: `Assets/02. Scripts/Battle/BattleDataTypes.cs`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/03. UI/ShopPanel.cs`

**Interfaces:**
- Produces: `BattleUnitSpawnData Clone()`
- Produces: `AllyMergeRules.CanMerge(...)`
- Produces: `UnitManager.TryMergeAllies(AllyUnit source, AllyUnit target)`
- Produces: `UnitManager.TryPromote(AllyUnit unit, string nextUnitId)`
- Produces: `IObservable<PromotionRequest> PromotionRequested`
- Produces: `IReadOnlyReactiveProperty<bool> HasPendingPromotion`
- Produces: `bool UnitManager.CanUsePreparation`
- Produces: `TitleData.GetPromotionCandidates(string, List<AllyUnitData>)`

- [ ] **Step 1: Write failing merge-rule tests**

```csharp
using NUnit.Framework;

public class AllyMergeRulesTests
{
    [TestCase("warrior", 1, "warrior", 1, 10, true)]
    [TestCase("warrior", 1, "archer", 1, 10, false)]
    [TestCase("warrior", 1, "warrior", 2, 10, false)]
    [TestCase("knight", 10, "knight", 10, 10, false)]
    public void MergeRequiresSameIdAndLevelBelowMax(
        string sourceId,
        int sourceLevel,
        string targetId,
        int targetLevel,
        int maxLevel,
        bool expected)
    {
        Assert.That(
            AllyMergeRules.CanMerge(
                sourceId,
                sourceLevel,
                targetId,
                targetLevel,
                maxLevel),
            Is.EqualTo(expected));
    }

    [Test]
    public void LevelFiveRequiresPromotionCandidates()
    {
        Assert.That(
            AllyMergeRules.CanFinishMerge(
                nextLevel: 5,
                classLevel: 5,
                promotionCandidateCount: 0),
            Is.False);
    }
}
```

- [ ] **Step 2: Run merge tests and confirm compile failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter AllyMergeRulesTests -testResults "Temp/merge-tests.xml" -logFile "Temp/merge-tests.log"
```

Expected: `AllyMergeRules` is missing.

- [ ] **Step 3: Implement pure merge predicates and spawn-data copy**

```csharp
public static bool CanMerge(
    string sourceId,
    int sourceLevel,
    string targetId,
    int targetLevel,
    int maxLevel) =>
    !string.IsNullOrEmpty(sourceId) &&
    sourceId == targetId &&
    sourceLevel == targetLevel &&
    sourceLevel < maxLevel;

public static bool CanFinishMerge(
    int nextLevel,
    int classLevel,
    int promotionCandidateCount) =>
    nextLevel != classLevel || promotionCandidateCount > 0;
```

`BattleUnitSpawnData.Clone()` copies `UnitId`, `Level`, and every `BattleUnitModifier` field so a spawned unit never mutates `PinballGoal.UnitData`.

Define the pure target projection beside `AllyMergeRules`:

```csharp
public readonly struct AllyMergeCandidate
{
    public string UnitId { get; }
    public int Level { get; }
    public Vector3 Position { get; }
    public bool IsSource { get; }

    public AllyMergeCandidate(
        string unitId,
        int level,
        Vector3 position,
        bool isSource)
    {
        UnitId = unitId;
        Level = level;
        Position = position;
        IsSource = isSource;
    }
}
```

Add the request payload beside the other battle DTOs:

```csharp
public sealed class PromotionRequest
{
    public AllyUnit Unit { get; }
    public IReadOnlyList<AllyUnitData> Candidates { get; }

    public PromotionRequest(
        AllyUnit unit,
        IReadOnlyList<AllyUnitData> candidates)
    {
        Unit = unit;
        Candidates = candidates;
    }
}
```

- [ ] **Step 4: Add safe stat replacement to UnitBase and AllyUnit**

`UnitBase.ReplaceBaseStats(BattleUnitStats, float hpRatio)` replaces `_stats` and `_initialStats`, preserves the supplied HP ratio, and clears only transient target/attack state that cannot survive a progression change.

`AllyUnit` stores its cloned spawn data and persistent pinball attack bonus. Add:

```csharp
public BattleUnitSpawnData RuntimeData { get; private set; }

public void ApplyProgression(
    BattleUnitSpawnData data,
    AllyUnitData unitData,
    AllyCommonData common,
    BattleUnitStats stats,
    IReadOnlyList<SkillGraph> graphs)
```

Capture HP and mana ratios before replacement, preserve position, update `UnitId`/`Level`, replace stats, restore mana ratio, and bind the new graphs without firing BattleStart while Pending.

- [ ] **Step 5: Implement authoritative merge and promotion in UnitManager**

```csharp
private readonly Subject<PromotionRequest> promotionRequested = new();
private readonly BoolReactiveProperty hasPendingPromotion = new(false);

public IObservable<PromotionRequest> PromotionRequested => promotionRequested;
public IReadOnlyReactiveProperty<bool> HasPendingPromotion => hasPendingPromotion;
public bool CanUsePreparation => BattlePhaseRules.CanUsePreparation(
    _battleManager.State.Value,
    hasPendingPromotion.Value);
```

`TitleData.GetPromotionCandidates` clears the caller-provided list and adds entries where `previousJob == currentUnitId`. `TryMergeAllies` checks preparation state, distinct non-null live units, active-list membership, the pure merge predicate, and promotion candidate availability before mutation. Rebuild target stats from its cloned runtime data, apply progression, then `ForceRemove` the source. If the new level is 5, set pending state and publish one `PromotionRequest` containing the target and a copied candidate array.

`TryPromote` accepts only the current pending unit and a candidate in that request, changes `UnitId` while keeping level 5, applies progression, clears pending state, and returns true. Dispose the Subject and ReactiveProperty in `OnDestroy`.

Finally update `BattleManager.StartWave`, `PinballManager`, and `ShopPanel` to use `HasPendingPromotion.Value`/`UnitManager.CanUsePreparation` instead of the temporary `false` argument introduced by Tasks 1 and 2.

- [ ] **Step 6: Run merge-rule and full skill tests**

Expected: merge predicates pass and graph rebinding still passes plan 02 tests.

- [ ] **Step 7: Commit merge domain behavior**

```powershell
git add -- "Assets/02. Scripts/Battle/AllyMergeRules.cs" "Assets/02. Scripts/Battle/BattleDataTypes.cs" "Assets/02. Scripts/Battle/AlllyUnit.cs" "Assets/02. Scripts/Battle/UnitBase.cs" "Assets/02. Scripts/Battle/UnitManager.cs" "Assets/02. Scripts/Battle/UnitSpawner.cs" "Assets/02. Scripts/02. Data/TitleData.cs" "Assets/02. Scripts/Battle/BattleManager.cs" "Assets/02. Scripts/Pinball/PinballManager.cs" "Assets/02. Scripts/03. UI/ShopPanel.cs" "Assets/02. Scripts/Editor/Tests/AllyMergeRulesTests.cs"
git commit -m "feat: merge matching allies and request promotion"
```

### Task 4: 월드 드래그 입력 구현

**Files:**
- Create: `Assets/02. Scripts/Battle/AllyDragController.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify later in Task 6: `Assets/04. Prefabs/AllyUnit.prefab`

**Interfaces:**
- Consumes: `UnitManager.TryMergeAllies`
- Produces: `UnitManager.FindClosestMergeTarget(AllyUnit, Vector3)`

- [ ] **Step 1: Add failing target-selection tests to AllyMergeRulesTests**

Create three eligible-position records at distances 2, 0.5, and 1 from the drop point and assert `AllyMergeRules.FindClosestEligibleIndex` returns the index at distance 0.5. Add cases for no eligible candidate and the source itself.

- [ ] **Step 2: Run merge tests and confirm the selector is missing**

Run Task 3 Step 2. Expected: compile failure for `FindClosestEligibleIndex`.

- [ ] **Step 3: Implement deterministic closest selection**

The pure selector receives source identity, drop position, candidate projections, and max level; it skips entries marked `IsSource` and returns `-1` when none qualify. `UnitManager.FindClosestMergeTarget` projects `_activeAllies` into this rule and returns the selected `AllyUnit`.

```csharp
public static int FindClosestEligibleIndex(
    string sourceId,
    int sourceLevel,
    Vector3 dropPosition,
    IReadOnlyList<AllyMergeCandidate> candidates,
    int maxLevel)
{
    var result = -1;
    var bestSqrDistance = float.MaxValue;
    for (var i = 0; i < candidates.Count; i++)
    {
        var candidate = candidates[i];
        if (candidate.IsSource ||
            !CanMerge(
                sourceId,
                sourceLevel,
                candidate.UnitId,
                candidate.Level,
                maxLevel))
        {
            continue;
        }

        var sqrDistance =
            (candidate.Position - dropPosition).sqrMagnitude;
        if (sqrDistance < bestSqrDistance)
        {
            bestSqrDistance = sqrDistance;
            result = i;
        }
    }

    return result;
}
```

- [ ] **Step 4: Implement the prefab-side input controller**

```csharp
[RequireComponent(typeof(BoxCollider2D))]
public sealed class AllyDragController : MonoBehaviour
{
    private AllyUnit unit;
    private UnitManager unitManager;
    private Vector3 origin;
    private bool dragging;

    private void OnMouseDown()
    {
        if (!unitManager.CanUsePreparation) return;
        origin = transform.position;
        dragging = true;
    }

    private void OnMouseUp()
    {
        if (!dragging) return;
        dragging = false;
        var target = unitManager.FindClosestMergeTarget(unit, transform.position);
        if (target == null || !unitManager.TryMergeAllies(unit, target))
        {
            transform.position = origin;
        }
    }
}
```

`OnMouseDrag` converts `Input.mousePosition` through a cached `Camera.main`, preserves Z, and immediately restores origin if the phase becomes invalid mid-drag. The controller never changes level, stats, or lists directly.

- [ ] **Step 5: Run merge tests and Unity compilation**

Expected: deterministic target selection passes and the drag component compiles.

- [ ] **Step 6: Commit drag behavior**

```powershell
git add -- "Assets/02. Scripts/Battle/AllyDragController.cs" "Assets/02. Scripts/Battle/UnitManager.cs" "Assets/02. Scripts/Editor/Tests/AllyMergeRulesTests.cs"
git commit -m "feat: drag allies to request merge"
```

### Task 5: 승급 및 전투 결과 UI 스크립트 구현

**Files:**
- Create: `Assets/02. Scripts/03. UI/PromotionPanel.cs`
- Create: `Assets/02. Scripts/03. UI/BattleResultPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/UIBase.cs`
- Modify: `Assets/02. Scripts/01. Manager/UIManager.cs`
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`

**Interfaces:**
- Consumes: `UnitManager.PromotionRequested`, `HasPendingPromotion`, `TryPromote`
- Consumes: `BattleManager.State`
- Produces: `UIBase.CanCloseWithBack`

- [ ] **Step 1: Add a failing non-dismissible modal test**

Add to `ReactiveManagerTests.cs` a small `TestModalPanel : UIBase` that overrides `CanCloseWithBack` to false and assert the property is false. This test fails until the base property exists.

- [ ] **Step 2: Run ReactiveManagerTests and confirm compile failure**

Expected: `CanCloseWithBack` is missing.

- [ ] **Step 3: Add modal back-button policy**

```csharp
public virtual bool CanCloseWithBack => true;
```

`UIManager.Back()` returns when `TopPanel.CanCloseWithBack` is false. `PromotionPanel` and `BattleResultPanel` override the property to false.

- [ ] **Step 4: Implement PromotionPanel with two pre-placed option slots**

```csharp
[SerializeField] private Button[] optionButtons;
[SerializeField] private TextMeshProUGUI[] optionNameTexts;
[SerializeField] private TextMeshProUGUI[] optionDescriptionTexts;
```

Require array lengths of 2 because every current base class has exactly two validated promotion candidates. Subscribe to `PromotionRequested`, copy the request, fill both slots, and call `OpenPanel()`. Each button passes its captured candidate ID to `TryPromote`; close only after true. No cancel listener is added.

```csharp
private void ShowPromotion(PromotionRequest request)
{
    currentRequest = request;
    for (var i = 0; i < optionButtons.Length; i++)
    {
        var candidate = request.Candidates[i];
        optionNameTexts[i].text = candidate.name;
        optionDescriptionTexts[i].text = candidate.role;
        optionButtons[i].interactable = true;
    }

    OpenPanel();
}
```

- [ ] **Step 5: Implement BattleResultPanel**

```csharp
_battleManager.State
    .Where(state =>
        state == EWaveState.Victory || state == EWaveState.Defeat)
    .Subscribe(ShowResult)
    .AddTo(this);

titleButton.onClick.AddListener(() =>
    App.Get<SceneManager>().Load(ESceneName.Title));
```

Set the label to `VICTORY` or `DEFEAT`, then open the panel. Do not change `01. Title.unity`.

- [ ] **Step 6: Extend WavePanel start interactability**

Combine `BattleManager.State`, `PinballManager.State`, and `UnitManager.HasPendingPromotion`. The start button is interactable only for Pending + Idle + no pending promotion. Keep `BattleManager.StartWave` as the authoritative second guard.

- [ ] **Step 7: Run UI-related tests and compilation**

Expected: non-dismissible policy passes and both panel scripts compile with no runtime-created panel objects.

- [ ] **Step 8: Commit UI scripts**

```powershell
git add -- "Assets/02. Scripts/03. UI/PromotionPanel.cs" "Assets/02. Scripts/03. UI/BattleResultPanel.cs" "Assets/02. Scripts/03. UI/UIBase.cs" "Assets/02. Scripts/01. Manager/UIManager.cs" "Assets/02. Scripts/03. UI/WavePanel.cs" "Assets/02. Scripts/Editor/Tests/ReactiveManagerTests.cs"
git commit -m "feat: add promotion and battle result panels"
```

### Task 6: Game 씬과 AllyUnit 프리팹을 사전 배치 방식으로 연결

**Files:**
- Create temporarily: `Assets/02. Scripts/Editor/BattleSceneSetup.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`
- Modify: `Assets/04. Prefabs/AllyUnit.prefab`
- Delete after execution: `Assets/02. Scripts/Editor/BattleSceneSetup.cs` and generated `.meta`

**Interfaces:**
- Wires: `AllyDragController` + `BoxCollider2D` on AllyUnit prefab
- Wires: `PromotionPanel` and `BattleResultPanel` beneath existing UIManager

- [ ] **Step 1: Create an idempotent editor setup method**

`BattleSceneSetup.Configure()` must:

1. Load `Assets/04. Prefabs/AllyUnit.prefab`, add one `BoxCollider2D` sized to the visible unit sprite, add one `AllyDragController`, and save the prefab.
2. Open `Assets/01. Scenes/02. Game.unity`.
3. Find the existing `UIManager` component.
4. Create inactive `PromotionPanel` and `BattleResultPanel` roots only when absent.
5. Add opaque raycast-blocking backgrounds, TMP labels, two promotion buttons, and one Title button.
6. Assign every serialized field with `SerializedObject`.
7. Save the scene.

Use the font asset from an existing `TextMeshProUGUI` in the same scene. Use existing Canvas and EventSystem; do not create replacements.

- [ ] **Step 2: Run the setup through Unity**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -executeMethod BattleSceneSetup.Configure -quit -logFile "Temp/battle-scene-setup.log"
```

Expected: exit code 0, both panels serialized in Game scene, and AllyUnit prefab contains the two required components exactly once.

- [ ] **Step 3: Remove the temporary setup script**

Delete `BattleSceneSetup.cs` and its generated `.meta` using `apply_patch`. Do not delete or modify any other generated meta file.

- [ ] **Step 4: Open and resave the Game scene in batch mode**

Run Unity with `-batchmode -nographics -projectPath "$PWD" -quit -logFile "Temp/game-scene-import.log"`. Expected: no missing-script or missing-reference error.

- [ ] **Step 5: Commit scene and prefab wiring**

```powershell
git add -- "Assets/01. Scenes/02. Game.unity" "Assets/04. Prefabs/AllyUnit.prefab" "Assets/04. Prefabs/AllyUnit.prefab.meta"
git commit -m "feat: wire merge and result UI in game scene"
```

### Task 7: Run battle preparation integration verification

**Files:**
- Verify only

**Interfaces:**
- Produces a complete battle flow consumed by plan 04 verification.

- [ ] **Step 1: Run all EditMode tests**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults "Temp/battle-integration-tests.xml" -logFile "Temp/battle-integration-tests.log"
```

Expected: exit code 0.

- [ ] **Step 2: Perform the Game scene smoke checklist**

Open `02. Game` in the editor and verify:

1. Pending allows pinball, goal selection, shop, and ally drag.
2. Start stays disabled while a ball is active.
3. Active blocks new pinballs, shop mutations, and dragging.
4. Matching level-4 units open promotion after merge; mismatches return to origin.
5. Promotion choice changes the unit class and enables Start.
6. Ally wipe calculates breach once, removes enemies, and returns to Pending when HP remains.
7. Victory/Defeat opens the result modal and Title button requests Title scene.

- [ ] **Step 3: Confirm a clean task boundary**

```powershell
git status --short
```

Expected: no uncommitted files from plan 03.
