# Idle Pinball Board Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 런처·마그넷 중심 보드를 MoonlitWorkshop 테마의 자동 순환형 핀볼 보드로 교체하고, 5개 골 라인에서 `30 / 50 / 100 / 50 / 30` Gold를 지급하며 FocusedPocket 선택 라인에는 3배 보상을 적용한다.

**Architecture:** `PinballManager`의 기존 자동 스폰·풀·재진입 흐름은 유지하고, 골 선택과 보상 계산은 `PinballGoalController`와 순수 계산 함수가 담당한다. 씬은 런타임에 생성하지 않고 에디터 전용 authoring builder로 한 번 재구성하여 `02. Game.unity`에 직렬화한다. 런처·마그넷·삭제 대상 아이템 코드는 참조까지 제거하고, 튜토리얼은 재제작하지 않은 채 레거시 진행만 안전하게 비활성화한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity Editor scene authoring, TextMesh Pro

**Spec:** `docs/designs/2026-08-26/2026-08-26-idle-pinball-board-redesign-design.md`

## Global Constraints

- 핀볼 공의 스프라이트와 공 전용 Arcane 이펙트는 유지한다.
- 보드판, 핀, 범퍼, 골 등 보드 오브젝트는 MoonlitWorkshop 리소스를 사용한다.
- 가로 폭은 유지하고, 보드 하단은 거의 고정한 채 세로 높이를 약 12% 늘린다. `BoardVisual` 상단과 상단 HUD 하단의 월드 간격은 `0.00~0.05`를 목표로 한다.
- 일반 공과 분열된 clone 공 모두 골 보상을 받는다.
- 골 보상은 `baseGold × (선택된 FocusedPocket 라인이면 3, 아니면 1)`만 적용한다. GoldenBall, 콤보, 범퍼 보너스는 곱하지 않는다.
- 작은 핀은 직접 Gold를 지급하지 않는다. 큰 범퍼 5개(특수 1, 일반 2, 스피너 외형 일반 범퍼 2)는 기존 범퍼 수익 흐름을 사용한다.
- FocusedPocket 선택은 준비 단계에서만 변경할 수 있고 전투 중에는 잠긴다. 선택은 다음 준비 단계까지 유지된다.
- 삭제 아이템의 기존 enum 숫자는 재사용하지 않는다. 남는 enum 값의 명시적 숫자를 그대로 보존한다.
- 튜토리얼 콘텐츠는 재제작하지 않는다. 삭제 API 참조를 끊고 기존 튜토리얼이 실행되지 않게 하는 것만 이 범위에 포함한다.
- 현재 worktree의 사용자 변경을 되돌리거나 광범위하게 stage하지 않는다. 겹치는 파일은 수정 전후 diff를 확인하고, 커밋 시 `git add -p -- <file>`로 이 작업의 hunk만 선택한다. `git add -A`를 사용하지 않는다.
- 새 런타임 패키지를 추가하지 않는다.

---

### Task 1: 골 보상 순수 규칙을 테스트로 고정

**Files:**
- Create: `Assets/02. Scripts/Pinball/PinballGoalRewardCalculator.cs`
- Create: `Assets/02. Scripts/Pinball/Editor/PinballGoalRewardCalculatorTests.cs`

**Interfaces:**
- Produces: `PinballGoalRewardCalculator.Calculate(int baseGoldReward, bool isFocused, int focusedMultiplier): int`

- [ ] `PinballGoalRewardCalculatorTests`에 기본 보상, 선택 라인 3배, 비선택 라인 1배, 음수 입력 방어 테스트를 작성한다.

```csharp
[TestCase(30, false, 3, 30)]
[TestCase(50, true, 3, 150)]
[TestCase(100, true, 3, 300)]
[TestCase(-10, false, 3, 0)]
[TestCase(30, true, 0, 30)]
public void Calculate_ReturnsOnlyBaseTimesFocusedMultiplier(
    int baseGold, bool focused, int multiplier, int expected)
{
    Assert.That(
        PinballGoalRewardCalculator.Calculate(baseGold, focused, multiplier),
        Is.EqualTo(expected));
}
```

- [ ] 해당 테스트만 실행해 타입이 아직 없어 컴파일 또는 테스트가 실패하는 것을 확인한다.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath "H:\unitygame\pin-ball\pin-ball" `
  -runTests -testPlatform EditMode `
  -testFilter "PinballGoalRewardCalculatorTests" `
  -testResults "H:\unitygame\pin-ball\pin-ball\Temp\goal-reward-tests.xml" `
  -logFile "H:\unitygame\pin-ball\pin-ball\Temp\goal-reward-tests.log" -quit
```

- [ ] 최소 구현을 추가한다. 곱셈 전 입력을 보정하고 오버플로는 `int.MaxValue`로 제한한다.

```csharp
using UnityEngine;

public static class PinballGoalRewardCalculator
{
    public static int Calculate(int baseGoldReward, bool isFocused, int focusedMultiplier)
    {
        int safeBase = Mathf.Max(0, baseGoldReward);
        int multiplier = isFocused ? Mathf.Max(1, focusedMultiplier) : 1;
        long reward = (long)safeBase * multiplier;
        return reward > int.MaxValue ? int.MaxValue : (int)reward;
    }
}
```

- [ ] 같은 테스트를 다시 실행해 모두 통과하는 것을 확인한다.
- [ ] 새 파일만 stage하고 staged diff를 확인한 뒤 커밋한다.

```powershell
git add -- "pin-ball/Assets/02. Scripts/Pinball/PinballGoalRewardCalculator.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/PinballGoalRewardCalculator.cs.meta" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballGoalRewardCalculatorTests.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballGoalRewardCalculatorTests.cs.meta"
git diff --cached --check
git commit -m "feat(pinball): define fixed goal reward calculation"
```

### Task 2: 5개 골과 FocusedPocket 선택 상태 구현

**Files:**
- Modify: `Assets/02. Scripts/Pinball/PinballGoal.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballGoalController.cs`
- Create: `Assets/02. Scripts/Pinball/Editor/PinballGoalControllerTests.cs`

**Interfaces:**
- Produces: `PinballGoal.BaseGoldReward`, `PinballGoal.IsFocused`, `PinballGoal.SetFocused(bool)`
- Produces: `PinballGoalController.SelectedGoal`, `SetFocusedPocketMultiplier(int)`, `Select(PinballGoal): bool`, `CalculateReward(PinballGoal): int`, `ResetForNewRun()`
- Removes: 유닛 선택·교환·골 너비 보정 API

- [ ] 두 골을 등록했을 때 선택 골만 3배, 다른 골은 기본 금액이며 선택 변경이 유지되는 테스트를 작성한다. `SerializedObject`로 각 골의 `baseGoldReward`를 지정한다.

```csharp
[Test]
public void Select_WithFocusedPocket_TriplesOnlySelectedGoal()
{
    PinballGoalController controller = new PinballGoalController();
    PinballGoal left = CreateGoal("Goal_01", 30);
    PinballGoal center = CreateGoal("Goal_03", 100);

    controller.Register(left);
    controller.Register(center);
    controller.SetFocusedPocketMultiplier(3);

    Assert.That(controller.Select(center), Is.True);
    Assert.That(controller.SelectedGoal, Is.SameAs(center));
    Assert.That(controller.CalculateReward(left), Is.EqualTo(30));
    Assert.That(controller.CalculateReward(center), Is.EqualTo(300));
    Assert.That(center.IsFocused, Is.True);
    Assert.That(left.IsFocused, Is.False);
}
```

- [ ] FocusedPocket 미보유 상태에서는 `Select`가 `false`이고 보상이 변하지 않는 테스트, 등록되지 않은 골 선택을 거절하는 테스트, `ResetForNewRun`이 선택과 배율을 초기화하는 테스트를 추가한다.
- [ ] 테스트를 실행해 기존 유닛 기반 컨트롤러 API 때문에 실패하는 것을 확인한다.
- [ ] `PinballGoal`의 `BattleUnitSpawnData unitData`와 유닛 선택 코드를 제거하고 `baseGoldReward`, `rewardPopup`, `IsFocused`를 추가한다. `SetFocused`는 선택된 골의 룬·글로우 색과 강도를 명확하게 유지하고, 해제 시 원래 Moonlit 색으로 복구한다.

```csharp
[SerializeField, Min(0)] private int baseGoldReward = 30;
[SerializeField] private PinballGoldPopup rewardPopup;

public int BaseGoldReward => Mathf.Max(0, baseGoldReward);
public bool IsFocused { get; private set; }

internal void SetFocused(bool focused)
{
    IsFocused = focused;
    RefreshFocusedVisual();
}
```

- [ ] `OnMouseUpAsButton`은 직접 상태를 바꾸지 않고 `PinballManager.TrySelectFocusedGoal(this)`만 호출하도록 변경한다. 우클릭과 SwapLever용 분기는 삭제한다.
- [ ] `PinballGoalController`를 등록 골 목록, 선택 골, 정수 배율만 관리하도록 축소하고 보상 계산은 Task 1 계산기로 위임한다.
- [ ] 테스트를 다시 실행해 통과하는 것을 확인한다.
- [ ] 겹치는 `PinballGoal.cs`의 기존 Moonlit VFX 변경을 보존하고, 이 작업 hunk만 골라 커밋한다.

```powershell
git add -p -- "pin-ball/Assets/02. Scripts/Pinball/PinballGoal.cs"
git add -- "pin-ball/Assets/02. Scripts/Pinball/PinballGoalController.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballGoalControllerTests.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballGoalControllerTests.cs.meta"
git diff --cached --check
git commit -m "feat(pinball): add selectable five-lane goal rewards"
```

### Task 3: 모든 공의 골 보상을 실제 Gold 흐름에 연결

**Files:**
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballGoal.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballItemModifiers.cs`
- Create: `Assets/02. Scripts/Pinball/PinballGoalRewardData.cs`
- Create: `Assets/02. Scripts/Pinball/Editor/PinballGoalRewardIntegrationTests.cs`

**Interfaces:**
- Produces: `PinballManager.OnGoalRewarded: Action<PinballGoalRewardData>`
- Produces: `PinballManager.TrySelectFocusedGoal(PinballGoal): bool`
- Changes: `PinballManager.OnGoalBall(Pinball, PinballGoal)` awards Gold before returning the ball to the pool
- Changes: `PinballItemModifiers.FocusedPocketMultiplier` defaults to `1` and becomes `3` from item data

- [ ] `BattleManager`와 `PinballManager`를 구성해 일반 공이 30 Gold를 지급하는 테스트를 작성한다. 공을 풀에 등록하지 않은 테스트 구성에서도 보상 이후 반환이 안전하게 종료되도록 기존 `ReleaseBall`의 null guard를 유지한다.
- [ ] clone 공을 활성화하고 `OnGoalBall`을 호출했을 때 같은 보상을 지급하며 이벤트의 `IsClone`이 `true`인 테스트를 작성한다.
- [ ] 준비 상태에서는 FocusedPocket 골 선택이 성공하고 전투 상태에서는 거절되는 테스트를 작성한다.
- [ ] 테스트를 실행해 Gold가 지급되지 않아 실패하는 것을 확인한다.
- [ ] 이벤트 데이터는 골, 지급 Gold, clone 여부, Focused 여부를 불변 값으로 전달한다.

```csharp
public readonly struct PinballGoalRewardData
{
    public PinballGoal Goal { get; }
    public int Gold { get; }
    public bool IsClone { get; }
    public bool IsFocused { get; }

    public PinballGoalRewardData(PinballGoal goal, int gold, bool isClone, bool isFocused)
    {
        Goal = goal;
        Gold = gold;
        IsClone = isClone;
        IsFocused = isFocused;
    }
}
```

- [ ] `OnGoalBall`에 clone 예외 없이 보상 계산·`BattleManager.AddGold`·골 팝업·이벤트·공 반환 순서를 구현한다. 골 진입 이펙트는 한 번만 재생한다.

```csharp
internal void OnGoalBall(Pinball ball, PinballGoal goal)
{
    int reward = _goalController.CalculateReward(goal);
    bool focused = ReferenceEquals(_goalController.SelectedGoal, goal);

    if (reward > 0)
        _battleManager.AddGold(reward);

    goal.PlayRewardEffect(ball.transform.position, reward, focused);
    OnGoalRewarded?.Invoke(new PinballGoalRewardData(goal, reward, ball.IsClone, focused));
    ReleaseBall(ball);
}
```

- [ ] `TrySelectFocusedGoal`은 `_battleManager.CanUsePreparationActions`와 FocusedPocket 배율을 확인하고, 성공한 경우에만 컨트롤러 선택을 바꾼다. 전투 시작 시 선택을 지우지 않으며 새 run 초기화에서만 `ResetForNewRun`을 호출한다.
- [ ] `PinballGoalRewardIntegrationTests`와 Task 1~2 테스트를 함께 실행해 통과하는 것을 확인한다.
- [ ] 작업 hunk만 stage해 커밋한다.

```powershell
git add -p -- "pin-ball/Assets/02. Scripts/Pinball/PinballManager.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/PinballGoal.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/PinballItemModifiers.cs"
git add -- "pin-ball/Assets/02. Scripts/Pinball/PinballGoalRewardData.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/PinballGoalRewardData.cs.meta" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballGoalRewardIntegrationTests.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/PinballGoalRewardIntegrationTests.cs.meta"
git diff --cached --check
git commit -m "feat(pinball): award gold for every goal ball"
```

### Task 4: 삭제 대상 아이템과 레거시 효과 정리

**Files:**
- Modify: `Assets/02. Scripts/00. Core/Enum.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballItemModifiers.cs`
- Modify: `Assets/02. Scripts/Pinball/Pinball.cs`
- Modify: `Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs`
- Modify: `Assets/Resources/Data/ItemData.json`
- Delete: `Assets/Resources/ItemIcons/auto_ball_feeder.png` and `.meta`
- Delete: `Assets/Resources/ItemIcons/target_magnet.png` and `.meta`
- Delete: `Assets/Resources/ItemIcons/swap_lever.png` and `.meta`
- Delete: `Assets/Resources/ItemIcons/charged_pin.png` and `.meta`
- Delete: `Assets/Resources/ItemIcons/overload_bumper.png` and `.meta`

**Interfaces:**
- Removes: `EItem.AutoBallFeeder`, `TargetMagnet`, `SwapLever`, `ChargedPin`, `OverloadBumper`
- Retains: `EItem.FocusedPocket = 11` and all other surviving numeric values
- Removes: magnet usage counters, charged-pin bonus and overload branches

- [ ] `ItemCatalogTests`의 기대 목록을 생존 아이템 10개로 바꾸고, 삭제 enum 이름과 JSON id가 없으며 FocusedPocket의 `Value1 == 3`, `Value2 == 0`임을 검증한다.

```csharp
string[] removedNames =
{
    "AutoBallFeeder", "TargetMagnet", "SwapLever", "ChargedPin", "OverloadBumper"
};
foreach (string removedName in removedNames)
    Assert.That(Enum.GetNames(typeof(EItem)), Does.Not.Contain(removedName));

ItemData focused = items.Single(item => item.id == "focused_pocket");
Assert.That(focused.value1, Is.EqualTo(3f));
Assert.That(focused.value2, Is.Zero);
```

- [ ] 테스트를 실행해 현재 카탈로그에 삭제 아이템이 남아 있어 실패하는 것을 확인한다.
- [ ] enum 멤버 이름만 삭제하고 생존 값 `GoldenBall=4`, `SplitCapsule=7`, `GoldenBumper=9`, `FocusedPocket=11`, `BattleClock=17`, `FieldArmor=18`, `DiversityEmblem=20`, `BarrierReinforcement=21`, `PersonalHealingPotion=22`, `PartyHealingPotion=23`을 유지한다.
- [ ] `ItemData.json`에서 5개 레코드를 삭제하고 FocusedPocket 설명을 “준비 단계에서 선택한 골 라인의 Gold 보상이 3배가 됩니다.”로 바꾼다.
- [ ] `PinballManager`, `PinballItemModifiers`, `Pinball`에서 삭제 enum 분기와 전용 필드·카운터·계산 함수를 제거한다. GoldenBall, SplitCapsule, GoldenBumper, 생산 업그레이드 로직은 건드리지 않는다.
- [ ] 삭제 아이콘과 `.meta`를 정확한 경로로 제거한다.
- [ ] ItemCatalog와 Pinball reward 관련 테스트를 실행해 통과하는 것을 확인한다.
- [ ] 관련 hunk와 정확한 삭제 파일만 stage해 커밋한다.

```powershell
git add -p -- "pin-ball/Assets/02. Scripts/00. Core/Enum.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/PinballManager.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/PinballItemModifiers.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Pinball.cs" `
  "pin-ball/Assets/02. Scripts/Item/Editor/ItemCatalogTests.cs" `
  "pin-ball/Assets/Resources/Data/ItemData.json"
git add -u -- "pin-ball/Assets/Resources/ItemIcons"
git diff --cached --check
git commit -m "refactor(pinball): remove obsolete board items"
```

### Task 5: 런처·마그넷 기능과 튜토리얼 의존성 제거

**Files:**
- Delete: `Assets/02. Scripts/Pinball/PinballLauncherController.cs` and `.meta`
- Delete: `Assets/02. Scripts/Pinball/PinballLauncherGlowController.cs` and `.meta`
- Delete: `Assets/02. Scripts/Pinball/PinballLaunchCostDisplay.cs` and `.meta`
- Delete: `Assets/02. Scripts/Pinball/PinballLaunchState.cs` and `.meta`
- Delete: `Assets/02. Scripts/Pinball/PinballMagnetController.cs` and `.meta`
- Delete: `Assets/02. Scripts/03. UI/WaveButtonStateController.cs` and `.meta`
- Delete: `Assets/02. Scripts/Tutorial/TutorialGameRuleController.cs` and `.meta`
- Delete: `Assets/02. Scripts/Pinball/Editor/PinballManualInputTests.cs` and `.meta`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/Pinball/Pinball.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballMotionMath.cs`
- Modify: `Assets/02. Scripts/Visual/PinballArcaneVfx.cs`
- Modify: `Assets/02. Scripts/Visual/ArcaneMaskGlowController.cs`
- Modify: `Assets/02. Scripts/Visual/ArcaneVfxCatalog.cs`
- Modify: `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset`
- Modify: `Assets/02. Scripts/Pinball/Editor/ArcaneVfxCatalogBuilder.cs`
- Modify: `Assets/02. Scripts/Pinball/Editor/ArcaneGlowMathTests.cs`
- Modify: `Assets/02. Scripts/Pinball/Editor/PinballMotionTests.cs`
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Modify: `Assets/02. Scripts/03. UI/Editor/ArcaneGameUiSetup.cs`
- Modify: `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs`
- Modify: `Assets/02. Scripts/Battle/BattleDataTypes.cs`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs`
- Modify: `Assets/Resources/Data/BattleWaveData.json`
- Modify: `Assets/02. Scripts/Tutorial/TutorialManager.cs`
- Create: `Assets/02. Scripts/Pinball/Editor/RemovedPinballFeatureContractTests.cs`

**Interfaces:**
- Removes: manual launch, launch cost, launcher feedback, magnet steering, magnet VFX catalog slots
- Retains: automatic top-center spawn, pooling, clone spawn and re-entry delay
- Tutorial behavior: overlay/focus를 숨긴 뒤 레거시 manager를 비활성화; 새 단계 작성 없음

- [ ] 문자열 기반 contract test를 먼저 작성해 삭제 대상 enum/JSON/source API가 남아 있지 않아야 한다고 고정한다. 삭제될 C# 타입을 직접 참조하지 않아 삭제 후 컴파일을 깨지 않게 한다.

```csharp
[Test]
public void RuntimeSources_DoNotContainRemovedLauncherOrMagnetApis()
{
    string root = Path.GetFullPath(Path.Combine(Application.dataPath, "02. Scripts"));
    string source = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}"))
        .Select(File.ReadAllText));

    Assert.That(source, Does.Not.Contain("CurrentLaunchCost"));
    Assert.That(source, Does.Not.Contain("OnLaunchCostChanged"));
    Assert.That(source, Does.Not.Contain("ApplyTargetMagnet"));
}
```

- [ ] contract test를 실행해 실패하는 것을 확인한다.
- [ ] `PinballManager`에서 launcher serialized fields, launch state, launch cost event/property/setup, 수동 발사 stubs와 유닛 스폰용 `OnGoalReached`를 제거한다. 자동 스폰·풀·생산 업그레이드 메서드는 유지한다.
- [ ] `Pinball`에서 `FixedUpdate`의 magnet 호출과 loaded/launch feedback 메서드를 제거한다. 속도 제한과 충돌 피드백은 유지한다.
- [ ] `PinballMotionMath`의 `AnchoredCompression`과 계산을 제거하고, `PinballMotionTests`는 속도 제한·범퍼 반사 테스트만 남긴다.
- [ ] `PinballArcaneVfx`의 loaded/launch/camera 메서드와 `ArcaneMaskGlowController.CalculateLauncherIntensity`를 제거한다. `ArcaneVfxCatalog`, asset, builder에서 `magnetMask`, `magnetArc`, `magnetSpark`만 제거하고 공·범퍼·골 리소스는 유지한다.
- [ ] `WavePanel`과 `ArcaneGameUiSetup`에서 숨겨진 `Btn_Launch`, launch cost TMP 참조·배치 코드를 제거한다. `GameplayFeedbackSceneTests`의 launcher/magnet 카탈로그 기대도 새 계약에 맞춘다.
- [ ] `BattleRunCommonData`와 JSON에서 `BaseLaunchCost`, `LaunchCostIncrease`를 제거하고 `TitleData` 검증도 해당 두 필드만 제거한다. 이 세 파일의 다른 사용자 변경은 그대로 둔다.
- [ ] `TutorialManager.Start`에서 overlay/focus를 숨긴 후 `enabled = false`로 종료하고, PinballManager 이벤트 구독과 `TutorialGameRuleController` 참조를 제거한다. 기존 튜토리얼 문구·단계는 재설계하지 않는다.
- [ ] 정확히 열거한 파일과 `.meta`만 삭제한다.
- [ ] Unity 컴파일 후 contract, motion, glow, UI tests를 실행해 통과하는 것을 확인한다.
- [ ] 겹치는 파일은 hunk 단위로 stage하고 삭제 파일은 정확한 경로만 stage해 커밋한다.

```powershell
git add -p -- "pin-ball/Assets/02. Scripts/Pinball/PinballManager.cs" `
  "pin-ball/Assets/02. Scripts/Battle/BattleDataTypes.cs" `
  "pin-ball/Assets/02. Scripts/02. Data/TitleData.cs" `
  "pin-ball/Assets/Resources/Data/BattleWaveData.json" `
  "pin-ball/Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs" `
  "pin-ball/Assets/02. Scripts/Visual/PinballArcaneVfx.cs" `
  "pin-ball/Assets/02. Scripts/Visual/ArcaneMaskGlowController.cs" `
  "pin-ball/Assets/02. Scripts/Visual/ArcaneVfxCatalog.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/ArcaneVfxCatalogBuilder.cs" `
  "pin-ball/Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset"
git add -u -- "pin-ball/Assets/02. Scripts/Pinball" `
  "pin-ball/Assets/02. Scripts/03. UI" `
  "pin-ball/Assets/02. Scripts/Tutorial"
git add -- "pin-ball/Assets/02. Scripts/Pinball/Editor/RemovedPinballFeatureContractTests.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/RemovedPinballFeatureContractTests.cs.meta"
git diff --cached --check
git commit -m "refactor(pinball): delete launcher and magnet systems"
```

### Task 6: MoonlitWorkshop 보드 씬을 5골 B 레이아웃으로 교체

**Files:**
- Create: `Assets/02. Scripts/Pinball/Editor/IdlePinballBoardSceneBuilder.cs`
- Create: `Assets/02. Scripts/Pinball/Editor/IdlePinballBoardSceneTests.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Editor menu: `Tools/Pinball/Rebuild Idle Pinball Board`
- Scene source of truth: rebuilt objects and serialized references in `02. Game.unity`; no runtime layout generation

- [ ] 씬 contract test를 먼저 작성한다. `02. Game` 씬을 열고 아래를 검증한다.

  - `PinballGoal` 정확히 5개, x 오름차순 보상 `30, 50, 100, 50, 30`
  - `EPinballObstacle.SmallPin` 13개
  - `EPinballObstacle.BigBumper` 5개, 그중 jackpot 1개, `Spinner_Bumper_` 2개
  - `Launcher`, `Magnet`, `Reflector`, `Btn_Launch` 이름의 레거시 오브젝트 없음
  - 모든 MonoBehaviour에 missing script 없음
  - `AutoBallSpawnPoint`가 보드 가로 중앙 오차 `<= 0.05`
  - `BoardVisual` 상단과 Status HUD 하단 간격 `0.00~0.05` world unit
  - 보드 렌더러는 MoonlitWorkshop 경로, `Pinball` 렌더러는 Arcane 경로

- [ ] 테스트를 실행해 현재 4골/레거시 오브젝트 구성 때문에 실패하는 것을 확인한다.
- [ ] 에디터 전용 builder를 작성한다. 기존 직렬화 템플릿을 clone한 뒤 컨테이너를 비우므로 첫 실행과 재실행 모두 동일한 결과가 되게 한다. 모든 `Undo.DestroyObjectImmediate` 대상은 보드의 `Obstacles`, `Goals`, launcher roots 아래로 제한한다.
- [ ] 정규화 좌표를 정확히 다음 배열로 선언하고 `bounds.min + normalized * bounds.size`로 월드 좌표에 변환한다.

```csharp
private static readonly Vector2 SpecialBumperPoint = new Vector2(0.50f, 0.82f);

private static readonly Vector2[] SmallPinPoints =
{
    new Vector2(0.25f, 0.70f), new Vector2(0.42f, 0.68f),
    new Vector2(0.58f, 0.68f), new Vector2(0.75f, 0.70f),
    new Vector2(0.17f, 0.56f), new Vector2(0.83f, 0.56f),
    new Vector2(0.25f, 0.43f), new Vector2(0.42f, 0.41f),
    new Vector2(0.58f, 0.41f), new Vector2(0.75f, 0.43f),
    new Vector2(0.18f, 0.18f), new Vector2(0.50f, 0.20f),
    new Vector2(0.82f, 0.18f)
};

private static readonly Vector2[] StandardBumperPoints =
{
    new Vector2(0.36f, 0.55f), new Vector2(0.64f, 0.55f)
};

private static readonly Vector2[] SpinnerBumperPoints =
{
    new Vector2(0.31f, 0.30f), new Vector2(0.69f, 0.30f)
};

private static readonly float[] GoalX = { 0.10f, 0.30f, 0.50f, 0.70f, 0.90f };
private static readonly int[] GoalGold = { 30, 50, 100, 50, 30 };
```

- [ ] 보드 가로 폭과 하단을 보존한 상태에서 `BoardVisual` 세로 크기를 약 12% 키우고 루트를 위로 보정해 HUD 간격을 맞춘다. 상단 충돌벽도 새 visual bounds를 따라 이동·확장한다.
- [ ] `AutoBallSpawnPoint`를 `(0.50, 0.95)`에 배치한다. 기존 `PinballManager.autoSpawnPoint` serialized reference를 이 Transform에 유지한다.
- [ ] 특수 범퍼 1개, 작은 핀 13개, 일반 범퍼 2개를 Moonlit sprite로 clone/configure한다. 스피너 2개는 Moonlit spinner sprite를 쓰되 `EPinballObstacle.BigBumper`, `isJackpotBumper=false`로 설정하여 일반 범퍼 Gold·반사 로직만 사용한다.
- [ ] 골 5개를 `Goal_01`~`Goal_05`로 생성하고 하단 x 위치와 `GoalGold`를 직렬화한다. 각 골에는 trigger collider, Moonlit rune visual, persistent focused highlight, 독립 `PinballGoldPopup`을 연결한다.
- [ ] 런처, 레버, launch button/cost text, 좌우 magnet, reflector와 그 전용 VFX children을 씬에서 제거한다.
- [ ] builder를 실행하고 씬을 저장한 뒤 재실행해 오브젝트가 중복되지 않는지 확인한다.
- [ ] 씬 contract test와 `GameplayFeedbackSceneTests`를 실행해 통과하는 것을 확인한다.
- [ ] Game view에서 16:9 기준으로 다음을 수동 확인한다: HUD 틈 최소화, 공 자동 투입, 5골 진입 가능, 선택 골 강조, 전투 중 선택 잠금, Moonlit 보드와 Arcane 공의 시각 분리.
- [ ] builder, tests, scene의 이 기능 변경만 stage해 커밋한다. 씬의 선행 Moonlit 테마 변경은 이번 최종 보드 교체에 포함하되 다른 시스템 변경은 포함하지 않는다.

```powershell
git add -- "pin-ball/Assets/02. Scripts/Pinball/Editor/IdlePinballBoardSceneBuilder.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/IdlePinballBoardSceneBuilder.cs.meta" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/IdlePinballBoardSceneTests.cs" `
  "pin-ball/Assets/02. Scripts/Pinball/Editor/IdlePinballBoardSceneTests.cs.meta" `
  "pin-ball/Assets/01. Scenes/02. Game.unity"
git diff --cached --check
git commit -m "feat(pinball): rebuild moonlit idle pinball board"
```

### Task 7: 전체 회귀 검증과 작업 기록

**Files:**
- Create: `docs/ai-usage/2026-08-26/2026-08-26-idle-pinball-board-redesign.md`
- Verify: `Assets/02. Scripts/Pinball/Editor`
- Verify: `Assets/02. Scripts/Item/Editor`
- Verify: `Assets/02. Scripts/03. UI/Editor`
- Verify: `Assets/01. Scenes/02. Game.unity`

- [ ] `rg`로 제거 대상 런타임 심볼과 데이터가 남지 않았는지 확인한다. 설계 문서와 테스트 이름에 등장하는 문자열은 결과에서 제외한다.

```powershell
rg -n "PinballLauncher|PinballMagnet|CurrentLaunchCost|OnLaunchCostChanged|BaseLaunchCost|LaunchCostIncrease|AutoBallFeeder|TargetMagnet|SwapLever|ChargedPin|OverloadBumper" `
  "pin-ball/Assets" -g "*.cs" -g "*.json" -g "*.unity"
```

- [ ] 전체 EditMode 테스트를 실행하고 결과 XML의 실패·오류가 0인지 확인한다. Unity Editor가 이미 프로젝트를 열고 있다면 batchmode를 동시에 띄우지 말고 열린 Editor Test Runner를 사용한다.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath "H:\unitygame\pin-ball\pin-ball" `
  -runTests -testPlatform EditMode `
  -testResults "H:\unitygame\pin-ball\pin-ball\Temp\idle-board-editmode.xml" `
  -logFile "H:\unitygame\pin-ball\pin-ball\Temp\idle-board-editmode.log" -quit
```

- [ ] PlayMode에서 최소 1회 자동 투입부터 각 골 반환까지 관찰하고, 일반 공과 clone 공이 동일한 금액을 지급하는지 확인한다.
- [ ] FocusedPocket 획득 후 준비 단계에서 1~5번 골을 번갈아 선택해 표시와 `90 / 150 / 300 / 150 / 90` 지급을 확인하고, 전투 중 클릭이 선택을 바꾸지 않는지 확인한다.
- [ ] Profiler/Hierarchy에서 공 재진입 시 오브젝트가 계속 생성되지 않고 기존 pool이 재사용되는지 확인한다.
- [ ] `git diff --check`, `git status --short`, `git diff --cached`로 공백 오류와 사용자 변경 혼입 여부를 점검한다.
- [ ] AGENTS 규칙에 맞춰 AI 사용 기록에 요청, 설계 결정, 실제 수정 범위, 사용자 판단 필요 영역, 테스트 결과와 환경 제한을 기록한다.
- [ ] 기록 파일만 stage해 커밋한다.

```powershell
git add -- "pin-ball/docs/ai-usage/2026-08-26/2026-08-26-idle-pinball-board-redesign.md"
git diff --cached --check
git commit -m "docs: record idle pinball board redesign"
```

## Completion Criteria

- [ ] 씬에 런처·마그넷·reflector·launch UI가 없다.
- [ ] 보드에는 Moonlit 특수 범퍼 1, 작은 핀 13, 일반 범퍼 2, 스피너 외형 범퍼 2, 골 5가 있다.
- [ ] 골 보상이 왼쪽부터 `30 / 50 / 100 / 50 / 30`이고 모든 공에 적용된다.
- [ ] FocusedPocket 선택 골만 3배이며 준비 단계에서만 선택을 바꿀 수 있다.
- [ ] 상단 HUD와 보드 사이의 월드 간격이 `0.05` 이하이고 화면 겹침은 없다.
- [ ] 삭제 아이템과 launch-cost 데이터가 코드·JSON·아이콘에 남지 않는다.
- [ ] 레거시 튜토리얼은 실행되지 않으며 튜토리얼 재제작은 포함되지 않는다.
- [ ] 관련 EditMode 전체 테스트와 Game view 수동 검증이 통과한다.
