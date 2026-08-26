# 일직선 전투 및 직업 성장 시스템 구현 계획

> **작업 에이전트 필수 절차:** `superpowers:subagent-driven-development` 또는 `superpowers:executing-plans`로 작업별 실행한다. 각 동작은 `superpowers:test-driven-development`, 완료 주장은 `superpowers:verification-before-completion`을 적용한다. 단계는 체크박스로 추적한다.

**목표:** 전투를 하나의 수평선으로 제한하고 방어선 공방을 늘리며, 준비 단계 맵 위에서 직업별 공통 레벨과 상위 직업 구매 해금을 관리한다.

**구조:** 순수 C# `AllyProgressionController`가 기본 직업 레벨·비용·해금을 소유한다. `BattleManager`가 Gold와 준비 단계 게이트를 중재하고, `UnitManager`가 공통 레벨을 스폰 및 기존 유닛 능력치에 적용한다. 이동·스폰·핀볼 종료는 각 기존 책임 객체의 가장 좁은 API에서 수정하고 UI는 씬에 사전 배치한다.

**기술:** Unity 6.0.0.79f1, C#, NUnit EditMode, Unity UI, TextMeshPro, DOTween, Unity YAML 씬 검사, PC WebGL

**설계:** `docs/designs/2026-08-26/2026-08-26-linear-battle-unit-progression-design.md`

## 전역 제약

- 모든 유닛 이동과 넉백 결과는 전투 기준 Y를 유지한다.
- 양쪽 방어선 기본 HP는 정확히 300이다.
- 모든 JSON `moveSpeed`는 현재 값의 정확히 50%다.
- 아이템 구매는 준비 단계 전용으로 유지한다.
- 기본 직업 레벨은 Lv.1~10, 비용은 `ceil(150 × 1.35^(현재 레벨 - 1))` Gold다.
- 상위 직업 해금은 Lv.5 첫 번째, Lv.10 두 번째다.
- `UnitUpgradePanel`은 준비 단계 맵 위 자동 표시 패널이며 열기·닫기 UI가 없다.
- 최종 `ResultPanel`은 핀볼만 정지하며 `Time.timeScale`을 변경하지 않는다.
- `AllyDefenseLine` 파란 표시를 유지한다.
- `[SerializeField]` 필드에는 underscore를 사용하지 않는다.
- 런타임 UI 자동 생성, 외부 패키지, 파일 이동, 대규모 리팩터링을 하지 않는다.
- 사용자 소유 변경 파일은 수정·복원·스테이징하지 않는다: `Rabbit1_Mage_Attack.anim`, `ArcaneVfxCatalog.asset`, `EditorBuildSettings.asset`, `ProjectSettings.asset`, `ShaderGraphSettings.asset`.

## 파일 지도

- 수정: `Assets/02. Scripts/Battle/Units/UnitMovement.cs`, `UnitBase.cs`, `UnitSpawner.cs`, `UnitManager.cs` — 고정 Y 이동·스폰·현재 유닛 갱신.
- 수정: `Assets/Resources/Data/AllyUnitData.json`, `EnemyUnitData.json`, `Assets/01. Scenes/02. Game.unity` — 이동 속도·방어선 HP·UI 참조.
- 생성: `Assets/02. Scripts/Battle/Runtime/AllyProgressionController.cs` — 직업 레벨·비용·해금.
- 수정: `Assets/02. Scripts/Battle/BattleManager.cs`, `Runtime/UnitPurchaseController.cs`, `Units/UnitCreationService.cs`, `Assets/02. Scripts/03. UI/AllyPurchasePanelController.cs` — 경제·상태·스폰 레벨·잠금 게이트와 상위 직업 구매 카드.
- 생성: `Assets/02. Scripts/03. UI/UnitUpgradePanel.cs`, `UnitUpgradeCard.cs` — 준비 단계 강화 UI.
- 수정: `Assets/02. Scripts/03. UI/PinballComboDisplay.cs`, `Assets/02. Scripts/Pinball/Pinball.cs`, `PinballManager.cs`, `Assets/02. Scripts/03. UI/ResultPanel.cs` — 콤보와 종료 정지.
- 생성/수정: 대응 `Editor/*Tests.cs` — 순수 동작과 씬 배선 회귀.
- 생성: `docs/ai-usage/2026-08-26/2026-08-26-linear-battle-unit-progression-implementation-ai-usage.md`.

---

### 작업 1: 수평선 이동과 고정 스폰

**파일:**

- 수정: `Assets/02. Scripts/Battle/Units/UnitMovement.cs`
- 수정: `Assets/02. Scripts/Battle/Editor/UnitMovementTests.cs`
- 수정: `Assets/02. Scripts/Battle/UnitSpawner.cs`
- 수정: `Assets/02. Scripts/Battle/UnitBase.cs`
- 수정: `Assets/02. Scripts/Battle/UnitManager.cs`
- 생성: `Assets/02. Scripts/Battle/Editor/LinearBattleSpawnTests.cs`

**인터페이스:**

```csharp
public static Vector3 CalculateNextPosition(
    Vector3 currentPosition, Vector3 targetPosition,
    float speed, float deltaTime, float battleLineY);
public static Vector3 ApplyKnockback(
    Vector3 position, Vector3 direction, float distance,
    bool isImmune, float battleLineY);
```

`UnitSpawner`는 `allySpawnPoint.position.y`를 전투선 Y로 사용하고 양 팀 스폰에 같은 Y를 강제한다. X 랜덤 오프셋과 적 편대 Y 오프셋도 제거해 방어선 스폰 지점 자체가 정확한 생성점이 된다.

- [ ] **1.1 이동 RED 테스트 작성:** `(0, 3)`에서 `(10, -8)`로 이동해도 결과 Y가 3인지, 넉백 방향에 Y가 있어도 결과 Y가 3인지 검증한다.
- [ ] **1.2 스폰 RED 테스트 작성:** 아군·적군을 여러 번 생성해 각각 지정 스폰 X와 공통 Y가 항상 같고 `Random` 상태에 무관한지 검증한다.
- [ ] **1.3 RED 실행:** `-testFilter 'UnitMovementTests|LinearBattleSpawnTests'`; 예상은 새 시그니처/고정선 동작 부재 실패다.
- [ ] **1.4 최소 구현:** `targetPosition.y = battleLineY`, 넉백 방향은 `new Vector3(direction.x, 0f, 0f)`로 정규화한다. `UnitBase`가 스폰 시 기록한 `battleLineY`를 이동·넉백에 전달한다.
- [ ] **1.5 구매 배치 제거:** `UnitManager.SpawnAlly`에서 `TryPlaceInFreeGridSlot` 호출과 실패 반환을 제거한다. 기존 로스터 등록·사운드·즉시 전투 참여는 유지한다.
- [ ] **1.6 GREEN 실행:** 위 필터와 `WaveRosterResetPurchaseStateTests|DefenseLineBreachTests`를 실행한다.
- [ ] **1.7 커밋:** `fix(battle): align units on one battle line`.

### 작업 2: 이동 속도와 방어선 내구도 밸런스

**파일:**

- 수정: `Assets/Resources/Data/AllyUnitData.json`
- 수정: `Assets/Resources/Data/EnemyUnitData.json`
- 수정: `Assets/01. Scenes/02. Game.unity`
- 수정: `Assets/02. Scripts/Battle/Editor/BattleDataCharacterizationTests.cs`
- 수정: `Assets/02. Scripts/Battle/Editor/BattleDefenseLineControllerTests.cs`
- 수정: `Assets/02. Scripts/Battle/Editor/DefenseLineSceneTests.cs`

- [ ] **2.1 RED 데이터 테스트:** 모든 기대 `moveSpeed`를 기존 값의 절반으로 명시하고, Game 씬 `allyDefenseLineMaxHp`와 `enemyDefenseLineMaxHp`가 300인지 검사한다.
- [ ] **2.2 RED 실행:** `-testFilter 'BattleDataCharacterizationTests|BattleDefenseLineControllerTests|DefenseLineSceneTests'`; 현재 속도와 HP 20 때문에 실패해야 한다.
- [ ] **2.3 데이터 최소 수정:** Ally/Enemy JSON의 각 `moveSpeed`만 50%로 변경하고 씬의 두 HP만 300으로 바꾼다.
- [ ] **2.4 GREEN 실행:** 같은 필터에서 데이터 역직렬화, HP reset, 누적 피해 `299 + 1` 경계를 확인한다.
- [ ] **2.5 커밋:** `balance(battle): slow units and strengthen lines`.

### 작업 3: 직업별 공통 성장 도메인

**파일:**

- 생성: `Assets/02. Scripts/Battle/Runtime/AllyProgressionController.cs`
- 생성: `Assets/02. Scripts/Battle/Editor/AllyProgressionControllerTests.cs`

**인터페이스:**

```csharp
public readonly struct AllyProgressionResult
{
    public string UnitId { get; }
    public int Level { get; }
    public string UnlockedUnitId { get; }
}

public sealed class AllyProgressionController
{
    public const int MaximumLevel = 10;
    public int GetLevel(string rootUnitId);
    public int GetNextCost(string rootUnitId);
    public bool IsUnlocked(string unitId);
    public bool CanLevelUp(string rootUnitId, bool isOwned, int gold);
    public bool TryLevelUp(string rootUnitId, bool isOwned, int gold,
        out AllyProgressionResult result);
    public void Reset();
}
```

잠금표는 `warrior: knight/berserker`, `archer: ranger/marksman`, `mage: pyromancer/frost`, `spearman: lancer/guard` 순서다. 기본 직업은 항상 구매 가능하다.

- [ ] **3.1 RED 초기값·비용 테스트:** Lv.1, 다음 비용 150, Lv.2 비용 203, Lv.10 최대 상태를 검증한다.
- [ ] **3.2 RED 게이트 테스트:** 미보유, Gold 부족, 알 수 없는 ID, 최대 레벨은 false와 상태 불변을 검증한다.
- [ ] **3.3 RED 해금 테스트:** Lv.5에서 첫 ID만, Lv.10에서 둘째 ID도 해금되고 중간 레벨은 새 해금 ID가 비어 있는지 검증한다.
- [ ] **3.4 RED 실행:** `-testFilter 'AllyProgressionControllerTests'`; 타입 부재로 실패해야 한다.
- [ ] **3.5 최소 구현:** `StringComparer.Ordinal` 사전 4개와 `Math.Pow(1.35d, level - 1)`/`Math.Ceiling`을 사용한다. Gold 차감은 하지 않고 결과만 반환한다.
- [ ] **3.6 GREEN 실행:** 전체 성장 테스트 통과.
- [ ] **3.7 커밋:** `feat(battle): add shared ally progression`.

### 작업 4: 경제·구매·현재 유닛 능력치 연동

**파일:**

- 수정: `Assets/02. Scripts/Battle/BattleManager.cs`
- 수정: `Assets/02. Scripts/Battle/UnitManager.cs`
- 수정: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- 수정: `Assets/02. Scripts/Battle/UnitBase.cs`
- 수정: `Assets/02. Scripts/Battle/Units/UnitCreationService.cs`
- 수정: `Assets/02. Scripts/Battle/Runtime/UnitPurchaseController.cs`
- 수정: `Assets/02. Scripts/03. UI/AllyPurchasePanelController.cs`
- 수정: `Assets/01. Scenes/02. Game.unity`
- 생성: `Assets/02. Scripts/Battle/Editor/AllyProgressionBattleManagerTests.cs`
- 수정: `Assets/02. Scripts/Battle/Editor/UnitPurchaseControllerTests.cs`
- 수정: `Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs`

**BattleManager API:**

```csharp
public event Action<string> OnAllyProgressionChanged;
public int GetAllyJobLevel(string rootUnitId);
public int GetAllyJobLevelUpCost(string rootUnitId);
public bool IsAllyJobUnlocked(string unitId);
public bool CanLevelUpAllyJob(string rootUnitId);
public bool TryLevelUpAllyJob(string rootUnitId);
```

**UnitBase API:**

```csharp
public void ReapplyBaseStats(BattleUnitStats stats);
```

이 메서드는 현재 HP 비율을 보존하고 아이템·공유 공격 배율을 다시 적용한다.

- [ ] **4.1 RED 상태·경제 테스트:** Pending+보유+충분한 Gold만 성공하고 정확한 비용 1회 차감, Active/Resolving/미보유/부족/최대는 Gold와 레벨 불변인지 검증한다.
- [ ] **4.2 RED 현재·신규 유닛 테스트:** 전사 Lv.2 강화 직후 보유 전사 전부 Lv.2 스탯, 이후 구매 전사도 Lv.2 스탯인지 검증한다.
- [ ] **4.3 RED 잠금 구매 테스트:** `knight`는 전사 Lv.4에서 거절, Lv.5에서 허용하며 `berserker`는 Lv.10까지 거절한다.
- [ ] **4.4 RED 실행:** `-testFilter 'AllyProgressionBattleManagerTests|UnitPurchaseControllerTests|UnitCreationServiceTests'`.
- [ ] **4.5 최소 구현:** `BattleManager`가 controller 결과 후에만 `BattleEconomy.TrySpend`, `UnitManager.RefreshOwnedAlliesForRootJob`, 이벤트 호출을 수행한다. 구매 spawn data Level은 기본 직업 공통 레벨, 상위 직업은 데이터 최소 전직 레벨을 사용한다.
- [ ] **4.6 구매 후보 확장:** 기존 4개 `UnitPurchaseSettings`에 상위 8개 설정을 추가하되 `IsAllyJobUnlocked`를 통과해야 구매하도록 한다. 상위 직업 비용은 해당 기본 직업 기본 비용의 2배, 비용 배율과 쿨다운은 원본 설정과 동일하게 둔다.
- [ ] **4.7 구매 UI RED 테스트:** 기존 4개 기본 카드와 8개 상위 카드가 씬에 정확히 하나씩 존재하고, 상위 카드는 잠금 중 비활성/해금 뒤 활성인지 검증한다.
- [ ] **4.8 구매 UI 최소 수정:** `AllyPurchasePanelController`에 8개 상위 직업 카드 참조를 추가한다. 카드 오브젝트는 씬에 사전 배치하고 잠금 중에도 초상화·직업명·`Lv.5 해금` 또는 `Lv.10 해금` 문구를 보이되 Button만 비활성화한다. 해금 후 기존 비용·보유 수·쿨다운 형식으로 전환한다.
- [ ] **4.9 GREEN 실행:** 위 필터와 `WaveRosterResetPurchaseStateTests|AllyPurchaseUiSceneTests` 통과.
- [ ] **4.10 커밋:** `feat(battle): wire ally levels and unlocks`.

### 작업 5: 준비 단계 맵 상시 강화 패널

**파일:**

- 생성: `Assets/02. Scripts/03. UI/UnitUpgradePanel.cs`
- 생성: `Assets/02. Scripts/03. UI/UnitUpgradeCard.cs`
- 생성: `Assets/02. Scripts/03. UI/Editor/UnitUpgradePanelTests.cs`
- 생성: `Assets/02. Scripts/03. UI/Editor/UnitUpgradeSceneTests.cs`
- 수정: `Assets/01. Scenes/02. Game.unity`

**인터페이스:**

```csharp
public sealed class UnitUpgradeCard : MonoBehaviour
{
    public string RootUnitId { get; }
    public void Refresh(BattleManager battleManager, UnitManager unitManager,
        TitleData titleData);
}

public sealed class UnitUpgradePanel : UIBase
{
    public override bool IsDefaultPanel => true;
    public override bool IsManagedByStack => false;
}
```

- [ ] **5.1 RED 표시 규칙 테스트:** Pending이며 런 미종료일 때만 패널 활성, Active/Resolving/Victory/Defeat에서 비활성인지 검증한다.
- [ ] **5.2 RED 카드 테스트:** 미보유 문구와 버튼 false, 보유 시 레벨·비용·다음 HP/공격/방어/공속, Lv.5/Lv.10 해금 문구를 검증한다.
- [ ] **5.3 RED 실행:** `-testFilter 'UnitUpgradePanelTests|UnitUpgradeSceneTests'`; 타입/씬 객체 부재 실패.
- [ ] **5.4 최소 UI 구현:** 상태·Gold·로스터·성장 이벤트를 구독하고 값 변경 시에만 TMP와 버튼을 갱신한다. 버튼은 자신의 `rootUnitId`로 `TryLevelUpAllyJob`만 호출한다.
- [ ] **5.5 씬 배치:** `Panel_BattleArea` 위 Canvas 계층에 `UnitUpgradePanel`과 4개 `UnitUpgradeCard`를 사전 배치한다. 각 카드에 기존 직업 초상화, 직업명/레벨/능력치/비용/해금 TMP와 Button을 연결한다. 열기·닫기 Button은 만들지 않는다. `WaveResultPanel`과 `ResultPanel`이 더 높은 sibling이 되게 한다.
- [ ] **5.6 GREEN 실행:** 두 신규 테스트와 `AllyPurchaseUiSceneTests|WaveResultPanelTests` 통과, Missing Script/참조 0개.
- [ ] **5.7 커밋:** `feat(ui): show ally upgrades during preparation`.

### 작업 6: 콤보 텍스트 렌더링 수정

**파일:**

- 수정: `Assets/02. Scripts/03. UI/PinballComboDisplay.cs`
- 생성: `Assets/02. Scripts/03. UI/Editor/PinballComboDisplayTests.cs`
- 수정: `Assets/01. Scenes/02. Game.unity`

**Inspector:** `TextMeshProUGUI comboText`, `Image timeFillImage`, `RectTransform textGroup`만 사용한다.

- [ ] **6.1 RED 테스트:** `3 COMBO x2` 문구가 단일 TMP에 기록되고 진행도 0/0.5/1이 `fillAmount` 0/0.5/1인지 검증한다.
- [ ] **6.2 RED 씬 검사:** `ComboDisplay` 아래 TMP가 정확히 1개, Filled Image가 1개이며 필수 참조가 연결됐는지 검증한다.
- [ ] **6.3 RED 실행:** `-testFilter 'PinballComboDisplayTests'`; 기존 이중 TMP 구조 때문에 실패.
- [ ] **6.4 최소 구현·씬 수정:** `backgroundText`, `foregroundText`, `fillMask`를 제거하고 단일 텍스트/게이지로 교체한다. 기존 `DOPunchScale(...).SetUpdate(true)`는 유지한다.
- [ ] **6.5 GREEN 실행:** 신규 테스트와 `PinballRewardControllerTests` 통과.
- [ ] **6.6 커밋:** `fix(ui): simplify combo display rendering`.

### 작업 7: 최종 결과의 핀볼 정지

**파일:**

- 수정: `Assets/02. Scripts/Pinball/Pinball.cs`
- 수정: `Assets/02. Scripts/Pinball/PinballManager.cs`
- 수정: `Assets/02. Scripts/03. UI/ResultPanel.cs`
- 생성: `Assets/02. Scripts/Pinball/Editor/PinballResultPauseTests.cs`

**인터페이스:**

```csharp
public void PauseSimulation(); // velocity/angularVelocity 0, simulated false
public bool IsResultPaused { get; }
public void PauseForResult();
```

- [ ] **7.1 RED 핀볼 테스트:** 활성 영구/복제 공에 속도와 회전을 주고 `PauseForResult` 후 0, `simulated == false`, `IsResultPaused == true`인지 검증한다.
- [ ] **7.2 RED 루프 테스트:** 정지 뒤 `Update` 경계를 호출해 콤보 만료 이벤트와 자동 재생성이 진행되지 않는지 검증한다.
- [ ] **7.3 RED ResultPanel 테스트:** Victory/Defeat에서 정확히 1회 `PauseForResult`, 다른 상태에서는 호출하지 않으며 `Time.timeScale`이 유지되는지 검증한다.
- [ ] **7.4 RED 실행:** `-testFilter 'PinballResultPauseTests|PinballBallPoolTests'`.
- [ ] **7.5 최소 구현:** `PinballManager.PauseForResult`는 idempotent하게 모든 `permanentBalls`, `cloneBalls`, `ActiveBalls`에 `PauseSimulation`을 호출하고 이후 `Update`를 조기 반환한다. `ResultPanel`은 표시 직전에 호출한다.
- [ ] **7.6 GREEN 실행:** 위 필터와 `PinballAutoCycleControllerTests` 통과.
- [ ] **7.7 커밋:** `fix(pinball): pause simulation on final result`.

### 작업 8: 통합 검증과 기록

**파일:**

- 생성: `docs/ai-usage/2026-08-26/2026-08-26-linear-battle-unit-progression-implementation-ai-usage.md`
- 검사: 설계·계획의 모든 관련 파일

- [ ] **8.1 관련 EditMode 실행:**

  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball' -runTests -testPlatform EditMode -testFilter 'UnitMovementTests|LinearBattleSpawnTests|BattleDataCharacterizationTests|BattleDefenseLineControllerTests|DefenseLineSceneTests|AllyProgressionControllerTests|AllyProgressionBattleManagerTests|UnitPurchaseControllerTests|UnitCreationServiceTests|UnitUpgradePanelTests|UnitUpgradeSceneTests|PinballComboDisplayTests|PinballResultPauseTests|PinballBallPoolTests|PinballAutoCycleControllerTests|WaveRosterResetPurchaseStateTests' -testResults 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\linear-progression-related.xml' -logFile 'C:\Users\SSAFY\Documents\GitHub\pin-ball\pin-ball\Temp\linear-progression-related.log'
  ```

- [ ] **8.2 전체 EditMode 실행:** 같은 Unity 명령에서 `-testFilter`를 제거하고 별도 XML/log에 기록한다. 기존 실패와 새 실패를 분리한다.
- [ ] **8.3 WebGL 빌드:** 기존 `WebGlPerformanceBuild.BuildDevelopment` 진입점으로 `.utmp/WebGLPerformanceDevelopment`에 빌드하고 저장소 `docs/` 배포본은 덮어쓰지 않는다.
- [ ] **8.4 정적 검사:** `git diff --check`; 로그에서 `error CS`, `Compilation failed`, `MissingReferenceException`, `Missing Script`, 신규 UI missing reference를 검색한다.
- [ ] **8.5 수동 검증:** 한 줄 이동, 양쪽 방어선 정확 스폰, 1유닛 약 10~15초 방어선 공격, 증원 우선 타깃, 준비 단계 강화 패널, Lv.5/Lv.10 구매 해금, 준비 단계 아이템 구매, 단일 콤보 텍스트, ResultPanel 핀볼 정지를 확인한다.
- [ ] **8.6 파란 선 확인:** `AllyDefenseLine`이 보이고 HP/피격 연출이 유지되며 비활성 `TutorialFocusIndicator/FocusFrame`이 평상시 보이지 않는지 확인한다.
- [ ] **8.7 AI 기록:** 실제 변경, RED/GREEN 결과, 전체 테스트·빌드 결과, 수동 확인, 제한점을 사실대로 기록한다.
- [ ] **8.8 최종 diff:** 사용자 소유 파일 5개의 시작 상태를 보존하고 관련 파일만 명시적으로 스테이징한다.
- [ ] **8.9 최종 커밋:** `feat(gameplay): add linear battle progression loop`.

## 완료 조건

- 모든 유닛이 같은 Y에서 X축으로만 이동·넉백되고 양 팀이 각 방어선에서 정확히 스폰된다.
- JSON 이동 속도는 모두 기존의 50%, 양쪽 방어선 기본 HP는 300이다.
- 상대 유닛이 생기면 방어선 공격을 중단하고 상대를 우선 공격한다.
- 기본 직업 공통 레벨·비용·Lv.5/Lv.10 해금이 Gold와 준비 단계 규칙을 지킨다.
- 현재 및 신규 같은 직업 유닛이 동일 레벨 능력치를 받고 잠긴 상위 직업은 구매되지 않는다.
- 준비 단계 맵 위 강화 패널이 자동 표시되고 전투·결과 중 숨는다.
- 아이템은 준비 단계에서 구매 가능하고 전투 중 비활성이다.
- 콤보 문구는 단일 TMP로 정상 정렬되고 별도 게이지가 시간을 표시한다.
- 최종 ResultPanel에서 모든 핀볼이 멈추고 결과 UI 버튼은 계속 동작한다.
- 파란 방어선 표시와 기존 전체 아군/방어선 업그레이드가 유지된다.
- 관련·전체 EditMode, WebGL 빌드, 씬 참조, diff 검증 결과가 기록된다.
