# 기지 전선 밀어내기 스킬 구현 계획

> **작업 에이전트 필수 절차:** 이 계획을 실행할 때 `superpowers:executing-plans`, 각 동작 구현 전 `superpowers:test-driven-development`, 완료 주장 전 `superpowers:verification-before-completion`을 사용한다. 단계는 체크박스로 추적한다.

**목표:** 각 웨이브의 `EWaveState.Active` 게임 시간 30초 후 한 번 사용할 수 있고, 유효한 모든 적을 적 방어선 방향으로 정확히 3 Unity 유닛 밀어내는 씬 배치 기지 스킬 `전선 밀어내기`를 구현한다.

**구조:** 순수 C# `BaseKnockbackSkillController`가 `Locked`, `Ready`, `Used`와 게임 시간만 소유한다. `BattleManager`가 웨이브 상태, 컨트롤러, `UnitManager`, UI 요청을 중재하고 실제 적용 수가 1 이상일 때만 사용을 확정한다. `UnitManager`가 활성 적 스냅샷과 두 방어선 기반 고정 방향을 소유하며, `UnitBase`의 좁은 성공 반환 API가 기지 스킬 경로에서만 방어선·타겟 상태를 해제한다. `BaseSkillPanel`은 Game 씬에 사전 배치한 Button/TMP 참조만 사용한다.

**기술:** Unity 6.0.0.79f1, C#, NUnit EditMode, Unity UI, TextMeshPro, DOTween, Unity YAML 씬 참조 검사

**설계:** `docs/designs/2026-08-26/2026-08-26-base-knockback-skill-design.md` (`d85bb4c`)

## 전역 제약

- 스킬 이름은 `전선 밀어내기`, 넉백 거리는 정확히 3 Unity 유닛, 해금 시간은 `EWaveState.Active` 게임 시간 30초다.
- `Pending`, `Resolving`, `Victory`, `Defeat`에서는 시간 진행과 사용을 모두 거부한다.
- 실제 넉백 성공 수가 1 이상일 때만 웨이브 사용권을 소비한다.
- 피해, 기절, 감속, 신규 이미지, 사운드, 전장 VFX, 외부 패키지, 전장 경계 강제 보정을 추가하지 않는다.
- 런타임 UI 자동 생성 금지. Game 씬 기존 Canvas에 사전 배치하고 Inspector 참조만 사용한다.
- 기존 `ApplyKnockback(Vector3, float)` 공개 API와 일반 유닛 스킬 의미를 유지한다.
- `[SerializeField]` 필드에는 underscore를 사용하지 않는다.
- 요청 없는 리팩터링, 기존 폴더·파일 이동, 대규모 포맷 변경을 하지 않는다.
- `UnitRoster`, `UnitMovement`, `UnitStatusEffects`, `BattleAssaultController`는 테스트가 설계 충돌을 증명하지 않는 한 수정하지 않는다.
- 사용자 소유 변경 4개를 수정·복원·스테이징·커밋하지 않는다.
  - `Assets/05. Animations/Rabbit/Rabbit1_Mage_Attack.anim`
  - `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset`
  - `ProjectSettings/EditorBuildSettings.asset`
  - `ProjectSettings/ShaderGraphSettings.asset`
- Unity 자동 변경 파일과 씬 전체 재직렬화는 검토 없이 포함하지 않는다.

## 수정 파일 지도

- 생성: `Assets/02. Scripts/Battle/Runtime/BaseKnockbackSkillController.cs` 및 `.meta` — 순수 웨이브 상태·시간.
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackSkillControllerTests.cs` 및 `.meta` — 상태 전이·시간·사용권.
- 수정: `Assets/02. Scripts/Battle/UnitBase.cs` — 성공 반환형 기지 스킬 넉백 API와 성공 시 전투 상태 해제.
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackUnitTests.cs` 및 `.meta` — 면역, 거리, 방어선·타겟, 이동 재개.
- 수정: `Assets/02. Scripts/Battle/UnitManager.cs` — 활성 적 스냅샷, 고정 방향, 적용 수 반환.
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackUnitManagerTests.cs` 및 `.meta` — 대상 필터·다중 적용·방향·경계 밖 이동.
- 수정: `Assets/02. Scripts/Battle/BattleManager.cs` — Active 시간 전달, 웨이브 reset, 사용 요청 중재, UI 상태 이벤트.
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackBattleManagerTests.cs` 및 `.meta` — 상태 게이트·성공 소비·실패 불변.
- 생성: `Assets/02. Scripts/03. UI/BaseSkillPanel.cs` 및 `.meta` — Button/TMP 표시와 해금 강조.
- 생성: `Assets/02. Scripts/03. UI/Editor/BaseSkillPanelTests.cs` 및 `.meta` — 표시 계산과 변경 감지.
- 생성: `Assets/02. Scripts/03. UI/Editor/BaseSkillSceneTests.cs` 및 `.meta` — Game 씬 배치·Inspector·Missing Script 검사.
- 수정: `Assets/01. Scenes/02. Game.unity` — 기존 Canvas에 `BaseSkillPanel`, 버튼, 이름·상태 TMP 사전 배치.
- 생성: `docs/ai-usage/2026-08-26/2026-08-26-base-knockback-skill-implementation-ai-usage.md` — 실제 수정·검증 기록.

---

### 작업 1: 순수 웨이브 스킬 상태와 게임 시간

**파일:**

- 생성: `Assets/02. Scripts/Battle/Runtime/BaseKnockbackSkillController.cs`
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackSkillControllerTests.cs`

**인터페이스:**

```csharp
public enum EBaseKnockbackSkillState { Locked, Ready, Used }

public sealed class BaseKnockbackSkillController
{
    public const float UnlockSeconds = 30f;
    public EBaseKnockbackSkillState State { get; }
    public float ElapsedTime { get; }
    public float RemainingTime { get; }
    public bool CanUse { get; }
    public void StartWave();
    public bool Advance(float deltaTime, bool isActive);
    public bool TryConfirmUse(bool appliedToAnyEnemy);
}
```

`Advance` 반환값은 상태 또는 올림 표시 초가 달라졌는지 뜻한다. 음수 시간은 0으로 처리한다. `TryConfirmUse(false)`는 `Ready`를 유지한다.

- [ ] **1.1 상태 초기값·웨이브 reset 실패 테스트 작성**

  ```csharp
  var controller = new BaseKnockbackSkillController();
  controller.StartWave();
  Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Locked));
  Assert.That(controller.ElapsedTime, Is.Zero);
  Assert.That(controller.RemainingTime, Is.EqualTo(30f));
  Assert.That(controller.CanUse, Is.False);
  ```

  30초 진행과 성공 사용 뒤 `StartWave()`를 다시 호출해 같은 초기값이 복원되는 사례도 포함한다.

- [ ] **1.2 29.99초·30초·배속·비활성 상태 실패 테스트 작성**

  ```csharp
  controller.Advance(29.99f, true);
  Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Locked));
  controller.Advance(0.01f, true);
  Assert.That(controller.State, Is.EqualTo(EBaseKnockbackSkillState.Ready));
  ```

  `Advance(2f, true)` 15회가 30초가 되는 사례, `Advance(30f, false)`와 음수 입력이 시간·상태를 바꾸지 않는 사례를 추가한다.

- [ ] **1.3 사용 확정과 표시 변경 실패 테스트 작성**

  잠금 중 성공 보고 거부, Ready에서 `false` 보고 시 Ready 유지, `true` 보고 시 Used, Used에서 두 번째 사용 거부를 검증한다. 29.1초에서 표시 1초, 29.2초에서는 변경 없음, 30초에서는 Ready 전이 변경을 검증한다.

- [ ] **1.4 관련 EditMode RED 실행**

  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter 'BaseKnockbackSkillControllerTests' -testResults 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\base-knockback-task1-red.xml' -logFile 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\base-knockback-task1-red.log'
  ```

  예상: 새 타입 부재로 컴파일 실패.

- [ ] **1.5 최소 구현**

  `StartWave`는 0초/Locked, `Advance`는 `isActive && State != Used`일 때만 `Mathf.Max(0f, deltaTime)` 누적, 30초 도달 시 Ready로 전이한다. `RemainingTime`은 `Mathf.Max(0f, UnlockSeconds - ElapsedTime)`다.

- [ ] **1.6 관련 EditMode GREEN 실행**

  작업 1 명령을 `task1-green.xml/.log`로 실행해 전부 통과한다.

---

### 작업 2: `UnitBase`의 성공 반환형 넉백과 상태 해제

**파일:**

- 수정: `Assets/02. Scripts/Battle/UnitBase.cs`
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackUnitTests.cs`
- 회귀 검사: `Assets/02. Scripts/Battle/Editor/UnitMovementTests.cs`
- 회귀 검사: `Assets/02. Scripts/Battle/Editor/DefenseLineBreachTests.cs`

**인터페이스:**

```csharp
public bool TryApplyBaseKnockback(Vector3 direction, float distance);
```

기존 `public void ApplyKnockback(Vector3 direction, float distance)`는 그대로 둔다. 새 메서드는 살아 있고 풀 밖이며, 양수 거리·유효 방향·비면역일 때만 위치를 변경하고 `true`를 반환한다. 성공한 경우에만 `HasReachedDefenseLine = false`, `_currentTarget = null`, `_forcedTarget = null`, `_forcedTargetUntil = 0f`, 상태를 Idle로 바꾼다. HP, 효과, 공격 쿨다운, 전투 활성 여부는 건드리지 않는다.

- [ ] **2.1 유효성·정확한 거리 실패 테스트 작성**

  일반 적에게 `Vector3.right * 4f`, 거리 3을 넣어 정확히 `Vector3.right * 3f` 이동하고 성공하는지 검증한다. 0 방향, 0/음수 거리, 죽은 적, 풀의 적은 실패하며 위치가 유지돼야 한다.

- [ ] **2.2 면역과 방어선 상태 불변 실패 테스트 작성**

  적을 방어선 도달 상태로 만들고 넉백 면역을 적용한 뒤 호출한다.

  ```csharp
  Vector3 before = enemy.transform.position;
  bool applied = enemy.TryApplyBaseKnockback(Vector3.right, 3f);
  Assert.That(applied, Is.False);
  Assert.That(enemy.transform.position, Is.EqualTo(before));
  Assert.That(enemy.HasReachedDefenseLine, Is.True);
  ```

- [ ] **2.3 성공 시 방어선·타겟 해제와 재탐색 실패 테스트 작성**

  방어선 도달 및 아군 타겟을 가진 적에게 성공 넉백을 적용한다. 즉시 `HasReachedDefenseLine == false`, `CurrentTarget == null`, `IsBattleActive == true`를 검증한다. 다음 `Tick()`에서 살아 있는 아군을 다시 찾고 Moving 또는 Attacking 상태가 되는지 검증한다.

- [ ] **2.4 관련 EditMode RED 실행**

  필터: `BaseKnockbackUnitTests|UnitMovementTests|DefenseLineBreachTests`. 예상: 새 API 부재.

- [ ] **2.5 최소 구현**

  면역을 먼저 확인해 모든 상태 변경을 막는다. 성공 위치는 기존 `UnitMovement.ApplyKnockback`을 재사용한다. 일반 `ApplyKnockback` 호출부와 의미는 변경하지 않는다.

- [ ] **2.6 관련 EditMode GREEN 실행**

  새 테스트 통과와 기존 `UnitMovementTests` 회귀 없음 확인. 알려진 기존 `DefenseLineBreachTests.Tick_AfterReinforcementAppears_LeavesDefenseLineAndTargetsAlly`의 null `battleArea` 실패는 별도 기록한다.

---

### 작업 3: `UnitManager` 활성 적 스냅샷과 고정 방향 적용

**파일:**

- 수정: `Assets/02. Scripts/Battle/UnitManager.cs`
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackUnitManagerTests.cs`

**인터페이스:**

```csharp
public bool HasAliveActiveEnemy { get; }
public int TryApplyBaseKnockback(float distance);
```

방향은 `(enemyDefenseLine.position - allyDefenseLine.position).normalized`로 한 번 계산한다. 대상마다 현재 위치에서 방어선을 바라보는 방향을 다시 계산하지 않는다.

- [ ] **3.1 대상 필터·여러 적 실패 테스트 작성**

  로스터에 `null`, 죽은 적, 풀 반환 적, 일반 적 2명을 구성한다. 적용 결과가 2이고 두 일반 적만 같은 고정 방향으로 정확히 3 이동하는지 검증한다. 순회 중 상태 변경에 안전하도록 `ActiveEnemies.ToArray()` 스냅샷을 요구한다.

- [ ] **3.2 면역 혼합·전원 면역·적 없음 실패 테스트 작성**

  일반 1명+면역 1명은 결과 1, 전원 면역과 빈 로스터는 결과 0이어야 한다. 면역 적의 위치와 방어선 도달 상태는 유지한다. `HasAliveActiveEnemy`는 null·죽음·풀 반환만 제외한다. 따라서 전원 면역이어도 버튼 요청은 가능하지만 실제 적용 0으로 실패하고 사용권을 보존한다.

- [ ] **3.3 방어선 방향 실패 테스트 작성**

  아군선 `(5, 0)`, 적선 `(-5, 0)`이면 모든 적이 왼쪽으로 이동해야 한다. 적이 이미 적 방어선을 넘어가 있어도 방향이 반전되지 않아야 한다. 한 방어선 null 또는 두 선의 위치가 같으면 적용 0이다.

- [ ] **3.4 경계 밖 위치와 복귀 경로 실패 테스트 작성**

  경계 가까운 적을 3 유닛 밀어 결과 위치가 `BattleAreaBounds` 밖이어도 즉시 Clamp되지 않음을 검증한다. 다음 적 Tick에서는 기존 타겟/방어선 이동 경로가 실행되고 전투 활성·로스터 등록을 유지하는지 검증한다. 기존 `MoveTowardsPosition`의 Clamp 동작 자체는 바꾸지 않는다.

- [ ] **3.5 관련 EditMode RED 실행**

  필터: `BaseKnockbackUnitManagerTests|UnitRosterTests|UnitMovementTests`. 예상: `UnitManager` API 부재.

- [ ] **3.6 최소 구현**

  스냅샷을 만든 뒤 `enemy != null && enemy.IsAlive && !enemy.IsInPool`을 검사하고 `TryApplyBaseKnockback(direction, distance)` 성공 수만 센다. 거리 3은 `BattleManager`가 상수로 전달하고 UnitManager는 받은 양수 거리만 처리한다.

- [ ] **3.7 관련 EditMode GREEN 실행**

  모든 대상·방향·경계 사례와 기존 로스터/이동 테스트를 확인한다.

---

### 작업 4: `BattleManager` 시간·상태·사용 요청 중재

**파일:**

- 수정: `Assets/02. Scripts/Battle/BattleManager.cs`
- 생성: `Assets/02. Scripts/Battle/Editor/BaseKnockbackBattleManagerTests.cs`
- 회귀 검사: `Assets/02. Scripts/Battle/Editor/BattleAssaultControllerTests.cs`
- 회귀 검사: `Assets/02. Scripts/Battle/Editor/WaveRosterResetPurchaseStateTests.cs`

**인터페이스:**

```csharp
public const float BaseKnockbackDistance = 3f;
public EBaseKnockbackSkillState BaseKnockbackSkillState { get; }
public float BaseKnockbackRemainingTime { get; }
public bool CanUseBaseKnockbackSkill { get; }
public event Action OnBaseKnockbackSkillDisplayChanged;
public bool TryUseBaseKnockbackSkill();
internal void AdvanceBaseKnockbackSkill(float deltaTime);
```

`CanUseBaseKnockbackSkill`은 초기화됨, `State == Active`, 컨트롤러 Ready, `UnitManager.HasAliveActiveEnemy`를 모두 요구한다. UI 문구는 Ready이면 적이 없어도 `사용 가능`이므로 상태/남은 시간 프로퍼티와 버튼 가능 여부를 분리한다. `AdvanceBaseKnockbackSkill`은 테스트 가능한 시간 경계이며 `Update`가 `Time.deltaTime`을 전달한다.

- [ ] **4.1 웨이브 시작 reset과 시간 전달 실패 테스트 작성**

  `TryStartWave` 성공 직후 Locked/0초, 다음 웨이브 시작과 실패 재도전 시작도 Locked/0초인지 검증한다. Active에서 29.99/30초 입력 전이를 검증하고, `Pending`, `Resolving`, `Victory`, `Defeat`에서는 같은 입력에도 불변인지 검증한다.

- [ ] **4.2 배속 공통 시간 흐름 실패 테스트 작성**

  같은 `deltaTime`을 `BattleAssaultController.Advance`와 스킬 컨트롤러에 전달해 둘의 경과 시간이 같음을 검증한다. 2초 입력 15회가 30초를 해금함을 고정한다. `Time.unscaledDeltaTime` 사용은 금지한다.

- [ ] **4.3 실패 시 사용권·전투·경제 불변 테스트 작성**

  적 없음, 전원 면역, 비Active, 잠금, Used 두 번째 요청을 각각 호출한다. 호출 전후 다음 스냅샷이 같아야 한다.

  ```text
  Gold, PlayerHp, wave number/state, assault elapsed/phase,
  tactical reinforcement, ally purchase count/cost/cooldown,
  ally/enemy defense HP, enemy HP
  ```

  적 없음과 전원 면역 실패 뒤 스킬 상태는 Ready다.

- [ ] **4.4 성공 소비·중복 거부 실패 테스트 작성**

  Ready 상태에서 일반 적 1명 이상이면 UnitManager 반환 수가 1 이상이고 상태가 Used가 된다. 같은 웨이브 두 번째 요청은 false이며 추가 이동이 없어야 한다.

- [ ] **4.5 UI 갱신 이벤트 실패 테스트 작성**

  웨이브 reset, 표시 정수 초 변경, Ready 전이, 성공 Used, 유효 적 유무 변경 때 이벤트가 발생하는지 검증한다. 같은 표시 초 안의 프레임 진행에는 이벤트가 없어야 한다. `UnitManager.OnBattleRosterChanged` 구독·해제를 `InitializeNewRun`/`OnDestroy`에 대칭 배치한다.

- [ ] **4.6 관련 EditMode RED 실행**

  필터: `BaseKnockbackBattleManagerTests|BattleAssaultControllerTests|WaveRosterResetPurchaseStateTests`. 예상: 새 BattleManager API 부재.

- [ ] **4.7 최소 구현**

  `InitializeNewRun`에서 컨트롤러 생성과 로스터 이벤트 구독, 성공적인 `TryStartWave`에서 `StartWave`, Active `Update`에서 `AdvanceBaseKnockbackSkill(Time.deltaTime)` 호출, 요청 시 조건 재검증 후 UnitManager 호출, 적용 수가 1 이상일 때만 `TryConfirmUse(true)`를 호출한다.

- [ ] **4.8 관련 EditMode GREEN 실행**

  상태·경제 불변과 기존 공세·웨이브 reset 테스트를 확인한다. 기존 기준선 실패는 새 실패로 계산하지 않는다.

---

### 작업 5: 씬 배치 `BaseSkillPanel`과 최소 UI 갱신

**파일:**

- 생성: `Assets/02. Scripts/03. UI/BaseSkillPanel.cs`
- 생성: `Assets/02. Scripts/03. UI/Editor/BaseSkillPanelTests.cs`
- 수정: `Assets/01. Scenes/02. Game.unity`

**인터페이스와 Inspector 참조:**

```csharp
[SerializeField] private Button useButton;
[SerializeField] private TextMeshProUGUI skillNameText;
[SerializeField] private TextMeshProUGUI statusText;
[SerializeField, Min(0f)] private float readyFeedbackDuration = 0.35f;
```

```csharp
public static string FormatStatus(
    EWaveState waveState,
    EBaseKnockbackSkillState skillState,
    float remainingTime);
```

표시 규칙: Pending은 `대기`; Active+Locked는 `Mathf.CeilToInt(remainingTime)`; Ready는 `사용 가능`; Used는 `사용 완료`; Resolving/Victory/Defeat는 사용 불가이며 현재 스킬 상태에 맞춰 `대기` 또는 `사용 완료`를 유지한다.

- [ ] **5.1 상태 문구·버튼 실패 테스트 작성**

  30, 29.99, 1.01, 1, 0초의 올림 표시와 `대기`, `사용 가능`, `사용 완료`를 검증한다. Ready+적 없음은 문구 `사용 가능`, 버튼 false인지 검증한다.

- [ ] **5.2 변경 감지·강조 실패 테스트 작성**

  동일 상태·표시 초 반복 갱신은 TMP text 할당과 강조를 반복하지 않아야 한다. Locked에서 Ready로 처음 전이할 때만 버튼 RectTransform에 `DOPunchScale`을 0.35초 실행한다. 사운드/VFX 호출은 없어야 한다.

- [ ] **5.3 관련 EditMode RED 실행**

  필터: `BaseSkillPanelTests`. 예상: 새 UI 타입 부재.

- [ ] **5.4 최소 UI 구현**

  `Start`에서 `App.Get<BattleManager>()`, 버튼 리스너, `OnBaseKnockbackSkillDisplayChanged`와 `OnStateChanged`를 연결한다. 버튼 클릭은 `TryUseBaseKnockbackSkill()`만 호출한다. `OnDestroy`에서 리스너·이벤트·Tween을 해제한다. 참조 누락은 명시적 오류 후 비활성 처리한다.

- [ ] **5.5 Game 씬 수동 최소 수정**

  기존 Canvas의 전투 HUD, `Panel_Status` 인접 영역에 다음 계층을 1개씩 배치한다.

  ```text
  BaseSkillPanel (BaseSkillPanel)
    SkillNameText  = 전선 밀어내기
    UseButton (Button)
      StatusText   = 대기
  ```

  기존 패널의 배경·폰트·버튼 Sprite·색상·전환 스타일을 재사용한다. 새 이미지·오디오 애셋을 만들지 않는다. 스크립트의 세 객체 참조를 Inspector에 연결한다. Button 영구 OnClick은 비워 두고 스크립트가 리스너를 연결하게 한다.

- [ ] **5.6 관련 EditMode GREEN 실행**

  `BaseSkillPanelTests` 통과와 Console의 Missing Reference 없음 확인.

---

### 작업 6: Game 씬 구조·참조·Missing Script 회귀

**파일:**

- 생성: `Assets/02. Scripts/03. UI/Editor/BaseSkillSceneTests.cs`
- 검사: `Assets/01. Scenes/02. Game.unity`
- 회귀 검사: `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs`
- 회귀 검사: `Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs`
- 회귀 검사: `Assets/02. Scripts/Battle/Editor/DefenseLineSceneTests.cs`

- [ ] **6.1 씬 배치·Inspector 실패 테스트 작성**

  Game 씬을 열고 `BaseSkillPanel` 컴포넌트가 정확히 1개이며 활성 계층에 있는지 검사한다. `useButton`, `skillNameText`, `statusText` 직렬화 참조가 null이 아니고 이름 문구가 `전선 밀어내기`인지 검증한다.

- [ ] **6.2 Missing Script·런타임 생성 금지 검사 작성**

  씬의 모든 `GameObject` 컴포넌트를 순회해 null 컴포넌트가 없음을 검증한다. `BaseSkillPanel` 코드에 `new GameObject`, `Instantiate`, `AddComponent<Button>`, `AddComponent<TextMeshProUGUI>`가 없음을 소스 검사로 고정한다.

- [ ] **6.3 기존 HUD·방어선 참조 회귀 검사 실행**

  필터: `BaseSkillSceneTests|GameplayFeedbackSceneTests|AllyPurchaseUiSceneTests|DefenseLineSceneTests`. 새 씬 회귀 외 기존 `GameplayFeedbackSceneTests`의 `LaunchCost` 기대 불일치는 기준선 실패로 분리한다.

- [ ] **6.4 씬 YAML diff 검토**

  추가한 `BaseSkillPanel` 계층, 새 스크립트 참조, 기존 부모 Transform 자식 목록 변화만 남긴다. unrelated 객체 순서·직렬화·조명·카메라·프로젝트 설정 변화는 포함하지 않는다.

---

### 작업 7: 전체 검증, AI 기록, 관련 파일만 커밋

**파일:**

- 생성: `docs/ai-usage/2026-08-26/2026-08-26-base-knockback-skill-implementation-ai-usage.md`
- 검사: 계획에 명시된 모든 수정 파일

- [ ] **7.1 관련 EditMode 통합 실행**

  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter 'BaseKnockbackSkillControllerTests|BaseKnockbackUnitTests|BaseKnockbackUnitManagerTests|BaseKnockbackBattleManagerTests|BaseSkillPanelTests|BaseSkillSceneTests|UnitMovementTests|UnitRosterTests|UnitStatusEffectsTests|BattleAssaultControllerTests|DefenseLineBreachTests|DefenseLineSceneTests|WaveRosterResetPurchaseStateTests|GameplayFeedbackSceneTests|AllyPurchaseUiSceneTests' -testResults 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\base-knockback-related.xml' -logFile 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\base-knockback-related.log'
  ```

- [ ] **7.2 전체 EditMode 실행**

  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball' -runTests -testPlatform EditMode -testResults 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\base-knockback-full-editmode.xml' -logFile 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\base-knockback-full-editmode.log'
  ```

  기준선 5개와 새 실패를 분리한다.

  ```text
  BattleDataCharacterizationTests.EnemyCreateStats_AppliesWaveGrowthAndFlooring
  UnitCreationServiceTests.TryCreateEnemy_CreatesWaveScaledStats
  DefenseLineBreachTests.Tick_AfterReinforcementAppears_LeavesDefenseLineAndTargetsAlly
  GameplayFeedbackSceneTests.GameScene_WiresResultCostAndInteractionGlow
  SoundManagerTests.DeveloperScene_RegistersStartupBgmAndEverySfxClip
  ```

- [ ] **7.3 C# 컴파일·로그 검사**

  Unity 종료 코드, XML 합계, 로그에서 `error CS`, `Compilation failed`, `MissingReferenceException`, `Missing Script`, `[BaseSkillPanel] Missing reference`를 검색한다. 실행하지 못한 검사는 성공으로 기록하지 않는다.

- [ ] **7.4 가능한 Unity 직접 검증**

  1. Pending에서 `대기`, 버튼 비활성.
  2. 웨이브 시작 0초에서 `30`, 1배속 약 30초와 2배속 약 15초 후 `사용 가능`.
  3. 일시정지와 비Active에서 시간이 멈춤.
  4. 적 없음/전원 면역에서 버튼 또는 요청 실패, Ready 유지.
  5. 일반 적 여러 명이 적 방어선 방향으로 각각 3 이동, 면역 적 불변.
  6. 방어선 공격 중 적이 상태를 해제하고 이동·타겟 탐색 재개.
  7. 경계 밖 적이 전투 활성·로스터를 유지하고 기존 이동 경로로 복귀.
  8. 성공 후 `사용 완료`, 같은 웨이브 재사용 불가.
  9. 다음 웨이브와 실패 재도전 시작에서 다시 `30`.
  10. 골드·구매·공세·웨이브 상태가 실패 요청 전후 동일.

- [ ] **7.5 AI 활용 기록 작성**

  모델·도구, 사용자 요청, 승인된 계획, 실제 수정 파일/API, TDD RED/GREEN, 관련/전체 테스트 수, 기준선 실패 5개, 직접 검증 결과, 제한점을 사실대로 기록한다.

- [ ] **7.6 최종 diff와 사용자 변경 보존 검사**

  `git status --short`, `git diff --check`, 파일별 diff를 확인한다. 계획 밖 변경과 씬 자동 재직렬화가 없어야 한다. 사용자 소유 4개 파일의 상태는 시작 시와 같아야 한다.

- [ ] **7.7 관련 파일만 스테이징·커밋**

  사용자 소유 4개 파일을 경로 목록에 넣지 않는다. 계획·런타임·테스트·씬·AI 기록의 실제 관련 파일만 명시적으로 `git add -- <paths>` 후 커밋한다.

  ```text
  feat(battle): add base knockback skill
  ```

## 완료 조건

- 시작 직후 Locked/0초, 29.99초 Locked, 30초 Ready가 증명된다.
- Active 게임 시간만 누적되고 2배속 입력 규칙이 공세 시간과 같다.
- null·죽음·풀 반환 적은 제외되고 유효한 모든 비면역 적만 고정 방향으로 정확히 3 이동한다.
- 면역 적은 위치와 방어선 상태를 유지하고, 성공 적은 방어선·타겟 상태를 해제해 전투를 재개한다.
- 적 없음/전원 면역/비Active/중복 요청은 사용권과 경제·구매·공세·웨이브 상태를 보존한다.
- 성공 적용 수 1 이상일 때만 Used가 되며 다음 웨이브·재도전에서 Locked/0초로 초기화된다.
- Game 씬에 사전 배치 UI가 정확히 1개 있고 모든 Inspector 참조가 유효하며 런타임 UI 생성이 없다.
- 경계 밖 이동 직후 위치를 Clamp하지 않고 기존 이동·타겟 탐색 경로로 복귀함이 검증된다.
- 관련 EditMode, 전체 EditMode, C# 컴파일, 씬 참조와 가능한 직접 검증 결과가 기록된다.
- 기존 전체 EditMode 실패 5개와 새 회귀가 구분된다.
- 사용자 소유 변경 4개와 계획 밖 Unity 자동 변경이 작업 커밋에 포함되지 않는다.
