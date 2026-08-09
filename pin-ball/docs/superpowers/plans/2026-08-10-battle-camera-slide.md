# Battle Camera Slide Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 준비 상태에는 핀볼 보드를 보여주고 전투 중에는 기존 전투 화면을 보여주도록 Main Camera를 부드럽게 이동한다.

**Architecture:** Main Camera에 씬 배치되는 `BattleCameraController`가 `BattleManager.OnStateChanged`를 구독한다. 상태별 목표 위치는 Inspector 값에서 결정하고, 하나의 코루틴이 현재 위치부터 목표 위치까지 unscaled time 기반 ease-out 보간을 수행한다.

**Tech Stack:** Unity 6.0.79f1, C#, Unity Test Framework/NUnit, Unity Scene YAML

## Global Constraints

- 대상 플랫폼은 PC WebGL이다.
- 기존 프로젝트 구조와 공개 API를 변경하지 않는다.
- 새 외부 패키지를 설치하지 않는다.
- 핵심 컴포넌트는 씬 배치와 Inspector 참조를 사용하며 런타임 자동 생성하지 않는다.
- `[SerializeField]` 필드 이름에는 underscore를 사용하지 않는다.
- 전투 위치는 `(0, 0, -10)`, 핀볼 위치는 `(11.66, -0.03, -10)`, 이동 시간은 `0.5초`로 시작한다.
- `FixedAspectCamera`, UI 컨트롤러, 전투 규칙은 수정하지 않는다.

---

### Task 1: 상태 기반 카메라 슬라이드 컴포넌트

**Files:**
- Create: `Assets/02. Scripts/Battle/BattleCameraController.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleCameraControllerTests.cs`

**Interfaces:**
- Consumes: `BattleManager.State`, `BattleManager.OnStateChanged`, `EWaveState`
- Produces: 씬에 배치 가능한 `BattleCameraController : MonoBehaviour`

- [ ] **Step 1: 상태별 목표 위치와 easing을 검증하는 실패 테스트 작성**

`BattleCameraControllerTests.cs`에 private static 메서드를 reflection으로 호출하는 EditMode 테스트를 작성한다. 새 public API를 만들지 않으면서 아래 동작을 고정한다.

```csharp
#if UNITY_EDITOR
using System.Reflection;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class BattleCameraControllerTests
{
    private static readonly Vector3 BattlePosition = new(0f, 0f, -10f);
    private static readonly Vector3 PinballPosition = new(11.66f, -0.03f, -10f);

    [TestCase(EWaveState.Pending, true)]
    [TestCase(EWaveState.Active, false)]
    [TestCase(EWaveState.Victory, false)]
    [TestCase(EWaveState.Defeat, false)]
    public void ResolveTargetPosition_ReturnsPositionForWaveState(
        EWaveState state,
        bool expectsPinball)
    {
        MethodInfo method = typeof(BattleCameraController).GetMethod(
            "ResolveTargetPosition",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        var result = (Vector3)method.Invoke(
            null,
            new object[] { state, BattlePosition, PinballPosition });
        Vector3 expected = expectsPinball ? PinballPosition : BattlePosition;

        Assert.That(
            result,
            Is.EqualTo(expected).Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [TestCase(0f, 0f)]
    [TestCase(0.5f, 0.875f)]
    [TestCase(1f, 1f)]
    public void CalculateEasedProgress_UsesCubicEaseOut(
        float progress,
        float expected)
    {
        MethodInfo method = typeof(BattleCameraController).GetMethod(
            "CalculateEasedProgress",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        var result = (float)method.Invoke(null, new object[] { progress });

        Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
    }
}
#endif
```

- [ ] **Step 2: 테스트를 실행해 실패 확인**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter BattleCameraControllerTests -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\battle-camera-tests.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\battle-camera-tests.log'
```

Expected: `BattleCameraController`가 아직 없어서 컴파일 실패하거나 해당 테스트가 실패한다.

- [ ] **Step 3: 최소 카메라 컨트롤러 구현**

`BattleCameraController.cs`를 아래 책임으로 구현한다.

```csharp
using System.Collections;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleCameraController : MonoBehaviour
{
    [SerializeField] private Vector3 battlePosition = new(0f, 0f, -10f);
    [SerializeField] private Vector3 pinballPosition = new(11.66f, -0.03f, -10f);
    [SerializeField, Min(0f)] private float slideDuration = 0.5f;

    private BattleManager battleManager;
    private Coroutine slideCoroutine;

    private void Start()
    {
        if (!App.TryGet(out battleManager))
        {
            Debug.LogError("[BattleCameraController] Missing service: BattleManager");
            enabled = false;
            return;
        }

        battleManager.OnStateChanged += OnBattleStateChanged;
        ApplyPosition(battleManager.State, true);
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        ApplyPosition(state, false);
    }

    private void ApplyPosition(EWaveState state, bool immediate)
    {
        Vector3 targetPosition = ResolveTargetPosition(
            state,
            battlePosition,
            pinballPosition);

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }

        if (immediate || slideDuration <= 0f)
        {
            transform.position = targetPosition;
            return;
        }

        slideCoroutine = StartCoroutine(SlideTo(targetPosition));
    }

    private IEnumerator SlideTo(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / slideDuration);
            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                CalculateEasedProgress(progress));
            yield return null;
        }

        transform.position = targetPosition;
        slideCoroutine = null;
    }

    private static Vector3 ResolveTargetPosition(
        EWaveState state,
        Vector3 battlePosition,
        Vector3 pinballPosition)
    {
        return state == EWaveState.Pending
            ? pinballPosition
            : battlePosition;
    }

    private static float CalculateEasedProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        return 1f - Mathf.Pow(1f - clampedProgress, 3f);
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnStateChanged -= OnBattleStateChanged;
        }
    }
}
```

- [ ] **Step 4: EditMode 테스트 통과 확인**

Step 2의 Unity 명령을 다시 실행한다.

Expected: 프로세스 exit code `0`, 상태 매핑 4개와 easing 3개 테스트가 모두 통과한다.

- [ ] **Step 5: 컴포넌트 변경 커밋**

```powershell
git add -- 'Assets/02. Scripts/Battle/BattleCameraController.cs' 'Assets/02. Scripts/Battle/BattleCameraController.cs.meta' 'Assets/02. Scripts/Battle/Editor/BattleCameraControllerTests.cs' 'Assets/02. Scripts/Battle/Editor/BattleCameraControllerTests.cs.meta'
git commit -m "feat: add battle camera slide controller"
```

---

### Task 2: Game 씬 Main Camera 연결

**Files:**
- Modify: `Assets/01. Scenes/02. Game.unity` Main Camera object around its existing component list

**Interfaces:**
- Consumes: Task 1의 `BattleCameraController`
- Produces: 준비/전투 상태에 따라 자동 이동하는 Game 씬 Main Camera

- [ ] **Step 1: Unity Editor에서 Main Camera에 컴포넌트 연결**

`Assets/01. Scenes/02. Game.unity`의 `Main Camera`에 `BattleCameraController`를 추가하고 값을 아래처럼 설정한다.

```text
Battle Position:  (0, 0, -10)
Pinball Position: (11.66, -0.03, -10)
Slide Duration:   0.5
```

기존 `Camera`, `AudioListener`, URP Camera Data, `FixedAspectCamera` 설정은 변경하지 않는다.

- [ ] **Step 2: 씬 직렬화 변경 범위 확인**

Run:

```powershell
git diff -- 'Assets/01. Scenes/02. Game.unity'
```

Expected: Main Camera의 component 참조 1개와 `BattleCameraController` 직렬화 블록만 추가된다. 다른 오브젝트나 GUID가 변경되면 저장 결과를 채택하지 않는다.

- [ ] **Step 3: Play Mode 수동 검증**

```text
1. 최초 Pending: 카메라가 즉시 (11.66, -0.03, -10)에 있고 핀볼 보드가 보인다.
2. 전투 시작: 약 0.5초 동안 (0, 0, -10)으로 ease-out 이동한다.
3. 다음 Pending: 약 0.5초 동안 핀볼 위치로 돌아온다.
4. 이동 중 상태 변경: 현재 위치에서 새 목표로 방향을 바꾸며 튀지 않는다.
5. Victory/Defeat: 전투 위치를 유지한다.
6. Game 뷰 16:9 및 WebGL 해상도에서 UI 클릭과 유닛 드래그가 정상이다.
```

- [ ] **Step 4: 씬 연결 커밋**

```powershell
git add -- 'Assets/01. Scenes/02. Game.unity'
git commit -m "feat: wire camera slide in game scene"
```

---

### Task 3: 최종 검증과 AI 활용 기록

**Files:**
- Modify: `.github/ai-use-log.md`

**Interfaces:**
- Consumes: Task 1과 Task 2의 완료 결과
- Produces: 검증 결과와 사실 기반 AI 작업 기록

- [ ] **Step 1: 전체 EditMode 테스트 실행**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UNITY\pin-ball\pin-ball' -runTests -testPlatform EditMode -testResults 'D:\UNITY\pin-ball\pin-ball\Temp\editmode-results.xml' -logFile 'D:\UNITY\pin-ball\pin-ball\Temp\editmode.log'
```

Expected: exit code `0`, 모든 EditMode 테스트 통과, C# 컴파일 오류 없음.

- [ ] **Step 2: WebGL Development 빌드 검증**

기존 WebGL 빌드 진입점이 있으면 사용한다. 없으면 Unity Editor Build Profiles에서 WebGL Development Build를 기존 산출물을 덮어쓰지 않는 임시 경로에 생성한다.

Expected: C# 컴파일 오류와 씬 직렬화 오류 없이 빌드 성공. 환경 제한으로 실행하지 못하면 정확한 제한과 미실행 항목을 기록한다.

- [ ] **Step 3: AI 활용 기록 추가**

`.github/ai-use-log.md`에 실제 결과를 반영해 아래 항목을 추가한다.

```markdown
## 2026-08-10 전투 상태 기반 카메라 슬라이드

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 준비 상태에는 맵 옆 핀볼이 보이도록 카메라를 이동하고, 전투 시작 시 기존 전투 화면으로 부드럽게 복귀
- AI 제안 내용: BattleManager 상태 이벤트를 구독하는 씬 배치형 BattleCameraController와 0.5초 ease-out 슬라이드
- AI 실제 수정 영역: 실제 변경한 C# 테스트/컴포넌트, Game 씬, AI 활용 기록 파일 목록
- 사용자 직접 결정/수정 필요 영역: 사용자가 전용 컴포넌트 방식과 부드러운 슬라이드를 결정; 최종 핀볼 구도와 이동 시간은 Inspector에서 조정 가능
- 중요한 프롬프트/지시: 기존 구조 보존, Inspector 참조 우선, SerializeField underscore 금지, 최소 변경, 외부 패키지 금지
- 테스트/검증 결과: 실제 EditMode/Play Mode/WebGL 검증 결과와 제한 사항
```

- [ ] **Step 4: 작업 트리와 변경 범위 최종 확인**

```powershell
git diff --check
git status --short
```

Expected: 공백 오류 없음. 요청 범위 파일 외에 새 변경이 없고 기존 사용자 변경은 건드리지 않는다.

- [ ] **Step 5: AI 활용 기록 커밋**

```powershell
git add -- '.github/ai-use-log.md'
git commit -m "docs: record battle camera AI usage"
```

