# Ally Deployment Limit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 배치 아군 수를 `현재 수/5`로 표시하고, 6마리부터 웨이브 시작을, 7마리부터 핀볼 발사를 차단한다.

**Architecture:** `UnitManager`가 배치 수와 제한 규칙을 단일 소스로 제공하고 변경 이벤트를 발행한다. 실제 동작은 `BattleManager`와 `PinballManager`가 방어적으로 검증하며, `WavePanel`과 `StatusPanel`은 같은 상태로 버튼과 텍스트를 갱신한다.

**Tech Stack:** Unity 6, C#, TextMesh Pro, Unity UI, NUnit EditMode tests

## Global Constraints

- 배치 수는 `UnitManager.OwnedAllies.Count` 기준이다.
- 0마리이면 웨이브를 시작할 수 없다.
- 1~5마리는 웨이브와 핀볼을 허용한다.
- 6마리는 웨이브만 차단하고 핀볼은 허용한다.
- 7마리 이상은 웨이브와 핀볼을 모두 차단한다.
- Status UI는 `현재 수/5`로 표시하고 6마리 이상을 빨간색으로 표시한다.
- `[SerializeField]` 필드 이름에 underscore를 사용하지 않는다.
- 런타임 UI 생성, 외부 패키지, 관련 없는 리팩터링을 하지 않는다.

## File Map

- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Create: `Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`
- Modify: `.github/ai-use-log.md`

---

### Task 1: 배치 제한 규칙과 수량 이벤트

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs:10-25,234-241,276-291`
- Create: `Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs`

**Interfaces:**
- Produces: `const int MaxDeployedAllyCount = 5`
- Produces: `int DeployedAllyCount`, `bool CanStartWaveWithCurrentRoster`, `bool CanLaunchPinballWithCurrentRoster`
- Produces: `event Action<int> OnDeployedAllyCountChanged`
- Produces: `static bool CanStartWaveWithAllyCount(int)`, `static bool CanLaunchPinballWithAllyCount(int)`

- [ ] **Step 1: Write the failing boundary tests**

```csharp
#if UNITY_EDITOR
using NUnit.Framework;

public class AllyDeploymentLimitTests
{
    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(5, true)]
    [TestCase(6, false)]
    [TestCase(7, false)]
    public void CanStartWaveWithAllyCount_UsesFiveUnitLimit(
        int count, bool expected)
    {
        Assert.That(
            UnitManager.CanStartWaveWithAllyCount(count),
            Is.EqualTo(expected));
    }

    [TestCase(0, true)]
    [TestCase(5, true)]
    [TestCase(6, true)]
    [TestCase(7, false)]
    public void CanLaunchPinballWithAllyCount_AllowsExactlySix(
        int count, bool expected)
    {
        Assert.That(
            UnitManager.CanLaunchPinballWithAllyCount(count),
            Is.EqualTo(expected));
    }
}
#endif
```

- [ ] **Step 2: Run the focused test and verify failure**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter 'AllyDeploymentLimitTests' -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\ally-limit-results.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\ally-limit.log'
```

Expected: the two rule methods do not exist, so compilation/test discovery fails.

- [ ] **Step 3: Add the minimal rules and public state**

```csharp
public const int MaxDeployedAllyCount = 5;

public event Action<int> OnDeployedAllyCountChanged;

public int DeployedAllyCount => _ownedAllies.Count;
public bool CanStartWaveWithCurrentRoster =>
    CanStartWaveWithAllyCount(DeployedAllyCount);
public bool CanLaunchPinballWithCurrentRoster =>
    CanLaunchPinballWithAllyCount(DeployedAllyCount);

public static bool CanStartWaveWithAllyCount(int count)
{
    return count >= 1 && count <= MaxDeployedAllyCount;
}

public static bool CanLaunchPinballWithAllyCount(int count)
{
    return count <= MaxDeployedAllyCount + 1;
}
```

- [ ] **Step 4: Emit the event only when the owned list changes**

In `AddOwnedAlly`, add and notify only when the ally is not already owned. In `ReleaseUnit`, save the result of `_ownedAllies.Remove(ally)`, preserve existing active-list/reservation cleanup, and notify only when removal succeeded.

```csharp
OnDeployedAllyCountChanged?.Invoke(DeployedAllyCount);
```

- [ ] **Step 5: Run the focused test and verify pass**

Run Step 2 again. Expected: exit code `0`, both boundary tests pass, no compilation errors.

- [ ] **Step 6: Commit rules and tests**

```powershell
git add -- 'Assets/02. Scripts/Battle/UnitManager.cs' 'Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs' 'Assets/02. Scripts/03. UI/Editor/AllyDeploymentLimitTests.cs.meta'
git commit -m "feat: add ally deployment limit rules"
```

---

### Task 2: 실제 웨이브와 핀볼 동작 차단

**Files:**
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs:102-123`
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs:153-173`

**Interfaces:**
- Consumes: Task 1의 두 current-roster 속성
- Preserves: `TryStartWave()`와 `TryLaunchLoadedBall(float)`의 기존 반환 계약

- [ ] **Step 1: Replace the wave roster branch with explicit empty/over-limit checks**

```csharp
if (_unitManager == null || _unitManager.DeployedAllyCount <= 0)
{
    RejectAction("아군 유닛을 한 명 이상 준비해야 합니다.");
    return false;
}

if (!_unitManager.CanStartWaveWithCurrentRoster)
{
    RejectAction("배치 아군은 5마리까지 웨이브에 참가할 수 있습니다.");
    return false;
}
```

- [ ] **Step 2: Add pinball validation before ball and gold checks**

```csharp
if (_unitManager == null ||
    !_unitManager.CanLaunchPinballWithCurrentRoster) return false;
```

Place this after preparation-state validation and before checking the loaded ball or spending gold. A rejected launch must not change ball state, launch count, or gold.

- [ ] **Step 3: Compile and run focused tests**

Run Task 1 Step 2. Expected: exit code `0`; boundary tests still pass.

- [ ] **Step 4: Commit manager enforcement**

```powershell
git add -- 'Assets/02. Scripts/Battle/BattleManager.cs' 'Assets/02. Scripts/Pinball/PinballManager.cs'
git commit -m "feat: enforce ally limits on battle actions"
```

---

### Task 3: 버튼과 Status UI 반영

**Files:**
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs:21-47,92-181`
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs:59-100,216-225`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Consumes: Task 1의 배치 수, 제한 속성, 변경 이벤트
- Produces serialized fields: `allyCountText`, `allyCountDefaultColor`, `allyCountOverLimitColor`

- [ ] **Step 1: Subscribe WavePanel to roster changes**

```csharp
_unitManager.OnDeployedAllyCountChanged += OnDeployedAllyCountChanged;

private void OnDeployedAllyCountChanged(int _)
{
    RefreshButtons();
}
```

Unsubscribe in `OnDestroy` when `_unitManager != null`.

- [ ] **Step 2: Add the shared roster conditions to RefreshButtons**

```csharp
bool hasAlly =
    _unitManager != null &&
    _unitManager.DeployedAllyCount > 0;
bool canStartWithRoster =
    _unitManager != null &&
    _unitManager.CanStartWaveWithCurrentRoster;
bool canLaunchWithRoster =
    _unitManager != null &&
    _unitManager.CanLaunchPinballWithCurrentRoster;
```

Add `canStartWithRoster` to `startButton.interactable` and `canLaunchWithRoster` to `launchButton.interactable`. Preserve preparation state, lock, pinball state, available-ball, ally-presence, and gold checks.

- [ ] **Step 3: Add StatusPanel fields, subscription, and renderer**

```csharp
[SerializeField] private TextMeshProUGUI allyCountText;
[SerializeField] private Color allyCountDefaultColor = Color.white;
[SerializeField] private Color allyCountOverLimitColor = Color.red;

private UnitManager _unitManager;
```

During `Initialize`, get the manager, subscribe, and immediately render `DeployedAllyCount`. Unsubscribe in `OnDestroy`.

```csharp
private void OnDeployedAllyCountChanged(int count)
{
    if (allyCountText == null) return;

    allyCountText.text =
        $"{Mathf.Max(0, count)}/{UnitManager.MaxDeployedAllyCount}";
    allyCountText.color = count > UnitManager.MaxDeployedAllyCount
        ? allyCountOverLimitColor
        : allyCountDefaultColor;
}
```

- [ ] **Step 4: Place and wire the scene TMP text**

In `Assets/01. Scenes/02. Game.unity`, add one scene-placed TextMeshProUGUI under the existing Status UI hierarchy beside HP/gold:

- Name `AllyCountText`; initial text `0/5`
- Match adjacent numeric font, size, alignment, and default color
- Disable Raycast Target
- Assign `StatusPanel.allyCountText`
- Set default color to the existing numeric color and over-limit color to red
- Do not modify unrelated anchors, artwork, or hierarchy

- [ ] **Step 5: Compile and run focused tests**

Run Task 1 Step 2. Expected: exit code `0`, no missing-field or C# errors.

- [ ] **Step 6: Verify the scene in Play Mode**

1. 5 allies: `5/5`, default color, both buttons retain their existing non-roster conditions.
2. 6 allies: `6/5`, red, wave disabled, pinball enabled when ball/gold/state allow.
3. 7 allies: `7/5`, red, wave and pinball disabled.
4. Merge down across a boundary: text and buttons update immediately.
5. Direct manager calls while blocked do not start, launch, or spend gold.

- [ ] **Step 7: Commit UI and scene wiring**

```powershell
git add -- 'Assets/02. Scripts/03. UI/WavePanel.cs' 'Assets/02. Scripts/03. UI/StatusPanel.cs' 'Assets/01. Scenes/02. Game.unity'
git commit -m "feat: show and enforce ally roster status"
```

---

### Task 4: 전체 검증과 AI 활용 기록

**Files:**
- Modify: `.github/ai-use-log.md`

**Interfaces:**
- Consumes: Tasks 1-3 verification results
- Produces: project-required factual AI usage record

- [ ] **Step 1: Run all EditMode tests**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\editmode-results.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\editmode.log'
```

Expected: exit code `0`, all EditMode tests pass, no compilation errors.

- [ ] **Step 2: Verify the existing WebGL build path**

Use the project's configured WebGL Development Build entry point without installing packages or replacing build settings. Record the exact command/profile and result. If no batch entry point exists, use Unity Build Profiles and record the manual limitation.

- [ ] **Step 3: Append the factual AI usage record**

```markdown
## 2026-08-10 아군 배치 수 제한

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 6마리부터 웨이브 시작을, 7마리부터 핀볼 발사를 차단하고 Status UI에 현재 수/5 표시
- AI 제안 내용: UnitManager를 단일 기준점으로 한 Manager 방어 검증과 이벤트 기반 UI 갱신
- AI 실제 수정 영역: 실제 변경한 테스트, C# 컴포넌트, Game 씬, AI 활용 기록 파일
- 사용자 직접 결정/수정 필요 영역: 사용자가 6마리 핀볼 허용과 5/5 형식을 결정; 최종 UI 배치 미세 조정 가능
- 중요한 프롬프트/지시: 기존 구조 보존, Inspector 참조, SerializeField underscore 금지, 최소 변경
- 테스트/검증 결과: 실제 EditMode, Play Mode, WebGL 결과와 제한 사항
```

- [ ] **Step 4: Check scope and whitespace**

```powershell
git diff --check
git status --short
```

Expected: only approved files, generated test `.meta`, AI log, and pre-existing user changes appear. The pre-existing untracked camera plan remains untouched.

- [ ] **Step 5: Commit the AI record**

```powershell
git add -- '.github/ai-use-log.md'
git commit -m "docs: record ally limit AI usage"
```

## Plan Self-Review

- Spec coverage: 5/6/7 boundaries, text/color, buttons, manager enforcement, scene wiring, tests, WebGL, and AI log all have explicit steps.
- Placeholder scan: no TBD, TODO, or undefined implementation action remains.
- Type consistency: every consumer uses the exact Task 1 member names and types.
- Scope check: no unrelated refactor, package, top-level folder, runtime UI creation, or data-format change is included.
