# 웨이브별 아군 초기화·전투 중 구매 구현 계획

> **작업 에이전트 필수 절차:** 이 계획을 실행할 때 `superpowers:test-driven-development`로 각 동작을 실패 테스트부터 구현하고, 완료 주장 전 `superpowers:verification-before-completion`으로 검증한다. 체크박스는 진행 추적에 사용한다.

**목표:** 모든 웨이브를 아군 0명과 기본 구매 상태로 시작하고, 일반·무료 아군 구매를 `EWaveState.Active`에서만 허용한다.

**구조:** `BattleManager`가 웨이브 상태에 따른 구매 허용과 결과 종료 순서를 소유한다. `UnitManager`는 기존 `UnitSpawner.ReturnUnit` 풀 반환 경로로 아군·적 로스터를 비우고, `UnitPurchaseController`는 웨이브 한정 구매 횟수와 쿨다운만 초기화한다. 골드, 전술 증원권, 업그레이드, 아이템, 남은 기회와 웨이브 인덱스는 기존 소유 객체에 그대로 둔다.

**기술:** Unity 6, C#, NUnit EditMode 테스트, Unity YAML 씬 참조 검사

**설계:** `docs/designs/2026-08-26/2026-08-26-wave-roster-reset-active-purchase-design.md` (`e409844`)

## 전역 제약

- 승인 전 런타임 코드·테스트·씬을 수정하지 않는다.
- 새 외부 패키지, 새 UI, 준비 타이머, 경제·비용·증가율·쿨다운 수치 변경을 추가하지 않는다.
- 공개 API, 저장 형식, 데이터 구조와 기존 폴더 구조를 불필요하게 바꾸지 않는다.
- `[SerializeField]` 필드에는 underscore를 사용하지 않는다.
- 풀 회수는 `UnitSpawner.ReturnUnit(UnitBase)`와 `UnitBase.MarkReturnedToPool()`을 재사용한다.
- `Assets/05. Animations/Rabbit/Rabbit1_Mage_Attack.anim`과 `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset`의 사용자 변경을 되돌리거나 스테이징하지 않는다.
- 변경은 아래 명시 파일로 제한한다. 씬 직렬화 변경은 분석상 필요하지 않으며 테스트가 실제 누락 참조를 증명할 때만 별도 승인 대상으로 보고한다.

## 현재 구조 대조

- `UnitManager.InitializeNewRun()`은 직렬화된 `startingAlly`로 전사 1명을 생성한다.
- `BattleManager.CanStartCurrentWave`는 `UnitManager.CanStartWaveWithCurrentRoster`를 통해 최소 아군 1명을 요구한다.
- `BattleManager.CanPurchaseAlly`와 `TryPurchaseAlly`는 `Victory`/`Defeat`만 차단해 `Pending`과 `Resolving`에서도 구매 경로에 진입한다.
- `UnitManager.ResolveWaveResult()`는 적만 풀로 회수하고 생존 아군을 준비 위치로 복원한다.
- `UnitPurchaseController`는 구매 횟수와 남은 쿨다운을 초기화하는 메서드가 없다.
- `AllyPurchasePanelController`는 매 프레임과 `OnStateChanged`에서 `BattleManager.CanPurchaseAlly`, 구매 횟수 기반 비용, 남은 쿨다운을 다시 읽는다. 상태·초기화 API만 고치면 별도 UI 코드와 씬 배선 없이 요구 표시가 갱신된다.

## 수정 파일 지도

- 수정: `Assets/02. Scripts/Battle/Units/UnitRoster.cs` — 아군 로스터를 한 번에 비우고 반환할 스냅샷 제공.
- 수정: `Assets/02. Scripts/Battle/UnitManager.cs` — 기본 아군 생성을 제거하고 새 런/웨이브 결과에서 모든 아군을 기존 풀로 회수.
- 수정: `Assets/02. Scripts/Battle/Runtime/UnitPurchaseController.cs` — 모든 유닛의 구매 횟수와 쿨다운 초기화.
- 수정: `Assets/02. Scripts/Battle/BattleManager.cs` — 0명 시작 허용, `Active` 구매 게이트, 결과 종료 초기화 순서.
- 수정: `Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs` — 아군 drain의 owned/active 동시 제거와 스냅샷 검증.
- 수정: `Assets/02. Scripts/Battle/Editor/UnitPurchaseControllerTests.cs` — 구매 후 전체 초기화와 골드 보존 검증.
- 생성: `Assets/02. Scripts/Battle/Editor/WaveRosterResetPurchaseStateTests.cs` 및 `.meta` — 웨이브 시작·구매 상태 정책과 결과별 공통 초기화 경계 검증.
- 수정: `Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs` — Game 씬의 기본 아군 자동 생성 설정 제거와 기존 네 카드 참조 유지 검증.
- 생성 또는 갱신: `docs/ai-usage/2026-08-26/2026-08-26-wave-roster-reset-active-purchase-implementation-ai-usage.md` — 실제 변경·검증 결과 기록.

---

### 작업 1: 로스터의 전체 아군 회수 경계

**파일:**

- 수정: `Assets/02. Scripts/Battle/Editor/UnitRosterTests.cs`
- 수정: `Assets/02. Scripts/Battle/Units/UnitRoster.cs`

**인터페이스:**

- 생성: `public AllyUnit[] DrainAllies()`
- 보장: 반환 배열은 호출 직전 owned 아군 스냅샷이며, 호출 직후 owned/active 아군 수는 모두 0이다.

- [ ] **1.1 실패 테스트 작성**

  `UnitRosterTests`에 서로 다른 두 아군을 `AddOwnedAlly`로 넣고 `DrainAllies()` 호출 후 다음을 검증한다.

  ```csharp
  Assert.That(drained, Is.EqualTo(new[] { warrior, mage }));
  Assert.That(roster.OwnedAllyCount, Is.Zero);
  Assert.That(roster.ActiveAllyCount, Is.Zero);
  ```

- [ ] **1.2 관련 EditMode 테스트 실행, 컴파일 실패 확인**

  실행 대상: `UnitRosterTests`. 예상 실패: `UnitRoster`에 `DrainAllies` 정의 없음.

- [ ] **1.3 최소 구현**

  `_ownedAllies.ToArray()`를 만든 뒤 `_ownedAllies`와 `_activeAllies`를 함께 비운다. 적 목록은 변경하지 않는다.

- [ ] **1.4 `UnitRosterTests` 재실행, 전체 통과 확인**

---

### 작업 2: 웨이브 구매 상태 초기화

**파일:**

- 수정: `Assets/02. Scripts/Battle/Editor/UnitPurchaseControllerTests.cs`
- 수정: `Assets/02. Scripts/Battle/Runtime/UnitPurchaseController.cs`

**인터페이스:**

- 생성: `public void ResetForWave()`
- 변경하지 않음: `BattleEconomy`, `UnitPurchaseSettings`, 비용 계산식과 쿨다운 수치.

- [ ] **2.1 실패 테스트 작성**

  일반 구매와 무료 구매로 서로 다른 유닛의 구매 횟수·가격·쿨다운을 변경한 뒤 `ResetForWave()`를 호출한다. 네 유닛 모두 `GetPurchaseCount(...) == 0`, `GetNextCost(...) == BaseCost`, `GetRemainingCooldown(...) == 0f`인지 검증한다. 초기 골드와 구매 후 골드를 기록해 reset 호출 전후 골드가 같음도 검증한다.

- [ ] **2.2 관련 EditMode 테스트 실행, 컴파일 실패 확인**

  실행 대상: `UnitPurchaseControllerTests`. 예상 실패: `ResetForWave` 정의 없음.

- [ ] **2.3 최소 구현**

  `settingsByUnitId.Keys`를 순회해 기존 `purchaseCounts[unitId]`와 `remainingCooldowns[unitId]` 값만 0으로 설정한다. 설정, 경제, 외부 객체를 재생성하지 않는다.

- [ ] **2.4 `UnitPurchaseControllerTests` 재실행, 기존 구매 불변 조건 포함 전체 통과 확인**

---

### 작업 3: 새 런 0명 시작과 전 아군 풀 회수

**파일:**

- 수정: `Assets/02. Scripts/Battle/Editor/WaveRosterResetPurchaseStateTests.cs`
- 수정: `Assets/02. Scripts/Battle/UnitManager.cs`

**인터페이스:**

- 제거: `startingAlly` 직렬화 필드와 `InitializeNewRun()`의 자동 `SpawnAlly` 분기.
- 생성: `private void ReturnAllAllies()`
- 변경: `ResolveWaveResult()`는 `ReturnAllEnemies()` 후 `ReturnAllAllies()` 실행.
- 유지: `UnitSpawner.ReturnUnit(UnitBase)`가 HP, 상태 효과, 공격 대상, 전투 상태와 활성 상태를 기존 풀 반환 경로로 초기화.

- [ ] **3.1 실패 테스트 작성**

  다음 사례를 `WaveRosterResetPurchaseStateTests`에 추가한다.

  - `UnitManager.CanStartWaveWithAllyCount(0)`가 참이고 `MaxDeployedAllyCount`까지 참, 초과는 거짓.
  - `UnitManager`의 초기화 결과가 기본 아군을 생성하지 않는다는 경계를 검증한다. Game 씬 기반 검증은 작업 5에서 보강한다.
  - `ResolveWaveResult()`가 보유·활성 아군을 모두 제거하고 반환된 유닛을 `IsInPool == true`, 비활성 상태로 만든다.

- [ ] **3.2 관련 EditMode 테스트 실행, 기존 동작 때문에 실패 확인**

  예상 실패: 0명 시작 거부, 결과 후 owned 아군 잔존.

- [ ] **3.3 자동 기본 아군 생성 제거**

  `startingAlly` 필드와 `_roster.OwnedAllyCount == 0` 자동 생성 블록만 삭제한다. 초기화 서비스·아이템 구독·풀 생성 순서는 유지한다.

- [ ] **3.4 `ReturnAllAllies()` 최소 구현**

  `DrainAllies()` 스냅샷을 순회해 각 아군을 `_preparationController.Remove(ally)` 후 `_spawner.ReturnUnit(ally)`로 반환한다. 이전 owned 수가 0보다 컸다면 `OnDeployedAllyCountChanged(0)`과 `OnBattleRosterChanged`를 각각 한 번만 발행한다. `RefreshAllyItemModifiers()`도 빈 로스터 기준으로 한 번 호출한다.

- [ ] **3.5 시작 가능 범위 갱신**

  `CanStartWaveWithAllyCount(int count)`를 `count >= 0 && count <= MaxDeployedAllyCount`로 바꿔 0명 시작을 허용한다. `CanStartWaveWithCurrentRoster`는 기존 호환을 위해 유지한다.

- [ ] **3.6 관련 EditMode 테스트 재실행, 풀 상태와 이벤트 횟수 통과 확인**

---

### 작업 4: `BattleManager` 상태 게이트와 결과 종료 순서

**파일:**

- 수정: `Assets/02. Scripts/Battle/Editor/WaveRosterResetPurchaseStateTests.cs`
- 수정: `Assets/02. Scripts/Battle/BattleManager.cs`

**인터페이스와 메서드 경계:**

- 변경: `CanStartCurrentWave` — 초기화됨, `Pending`, 준비 잠금 해제, 유효 웨이브, `UnitManager` 존재만 요구. 아군 수는 요구하지 않음.
- 변경: `CanPurchaseAlly(string unitId)` — 기존 null/초기화 검사 전에 `State == EWaveState.Active`를 필수 조건으로 둠.
- 변경: `TryPurchaseAlly(string unitId)` — 동일 `Active` 선검사로 일반·전술 증원권 경로 모두 진입 전 차단.
- 변경: `FinishWaveResolution()` — `unitManager.ResolveWaveResult()`, `unitPurchaseController.ResetForWave()`, `waveResolution.Clear()`, 기존 다음 상태 계산·웨이브 전진·`ChangeState` 순서.
- UI 알림: 초기화 뒤 실행되는 기존 `ChangeState(nextState)`의 `OnStateChanged`를 재사용. `AllyPurchasePanelController`가 이 이벤트에서 비용·버튼·쿨다운을 다시 읽으므로 새 공개 이벤트를 추가하지 않음.

- [ ] **4.1 구매 상태 실패 테스트 작성**

  `Pending`, `Resolving`, `Victory`, `Defeat` 각각에서 `CanPurchaseAlly`와 `TryPurchaseAlly`가 거짓인지 검증한다. 각 호출 전후 다음 스냅샷이 동일해야 한다.

  ```text
  Gold, HasTacticalReinforcement, purchase count,
  next cost, remaining cooldown, owned/active ally count
  ```

  무료 구매 사례는 `TacticalReinforcementController.GrantFromJackpot()`으로 티켓을 보유시킨 뒤 실패 후에도 `HasTicket == true`인지 검증한다.

- [ ] **4.2 `Active` 회귀 테스트 작성**

  기존 조건을 만족하는 일반 구매와 무료 구매가 각 1회 성공하며, 일반 구매만 골드를 차감하고 무료 구매만 티켓을 소비하며, 둘 다 해당 유닛 구매 횟수·가격·쿨다운·로스터를 변경하는지 검증한다.

- [ ] **4.3 0명 웨이브 시작 조건 테스트 작성**

  유효 `BattleRunState`, 잠금 해제, `Pending`, `UnitManager` 존재와 0명 로스터를 구성해 `CanStartCurrentWave == true`를 검증한다. 잠금, null 웨이브, `Active` 상태의 기존 거부 조건도 함께 고정한다.

- [ ] **4.4 결과별 공통 초기화 테스트 작성**

  `Cleared` 일반 웨이브, `Failed` 재도전, 최종 `Victory`, 기회 소진 `Defeat`의 `FinishWaveResolution()` 경계를 각각 구성한다. 모든 사례에서 다음을 검증한다.

  - 결과 표시가 끝나기 전 `Resolving` 동안 구매 불가.
  - 종료 후 owned/active 아군 0명, 구매 횟수 0, 기본 가격, 쿨다운 0.
  - 골드와 전술 증원권 유지.
  - 일반 클리어만 다음 웨이브로 전진, 실패는 같은 인덱스 유지.
  - 최종 결과는 기존 `Victory`/`Defeat` 판정을 유지.

- [ ] **4.5 관련 EditMode 테스트 실행, 기존 게이트·종료 흐름 때문에 실패 확인**

- [ ] **4.6 최소 런타임 수정**

  `BattleManager`의 세 경계만 수정한다. `TrySpawnPurchasedAlly`, `TacticalReinforcementController.TryUse`, `BattleResolutionPolicy`와 경제·업그레이드 로직은 변경하지 않는다.

- [ ] **4.7 관련 EditMode 테스트 재실행, 전 상태·결과 매트릭스 통과 확인**

---

### 작업 5: 구매 UI와 Game 씬 참조 회귀 검증

**파일:**

- 수정: `Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs`
- 검사만: `Assets/01. Scenes/02. Game.unity`
- 검사만: `Assets/02. Scripts/03. UI/AllyPurchasePanelController.cs`

**경계:** UI 코드는 `BattleManager.CanPurchaseAlly`, `GetAllyPurchaseCost`, `GetAllyRemainingCooldown`, `UnitManager.GetOwnedAllyCount`를 그대로 사용한다. 씬에는 새 참조를 추가하지 않는다.

- [ ] **5.1 씬 테스트 보강**

  Game 씬을 연 뒤 다음을 검증한다.

  - `UnitManager`와 `AllyPurchasePanelController`가 각 1개 존재.
  - 네 구매 버튼·표시 텍스트·쿨다운 마스크·쿨다운 텍스트·증원권 문구 참조가 유지됨.
  - `UnitManager`에 더 이상 직렬화된 `startingAlly` 프로퍼티가 없음.
  - 씬에 활성 `AllyUnit` 인스턴스가 없음.
  - 카드 포맷은 owned 0, 기본 가격을 표시하고 쿨다운 0은 빈 문자열로 표시.

- [ ] **5.2 `AllyPurchaseUiSceneTests` 실행**

  런타임 씬 수정 없이 통과해야 한다. 씬의 실제 누락 참조가 발견되면 자동 저장하지 않고 사용자에게 범위 확대 승인을 요청한다.

---

### 작업 6: 전체 검증과 AI 활용 기록

**파일:**

- 생성 또는 갱신: `docs/ai-usage/2026-08-26/2026-08-26-wave-roster-reset-active-purchase-implementation-ai-usage.md`

- [ ] **6.1 관련 EditMode 테스트 실행**

  최소 대상:

  ```text
  UnitRosterTests
  UnitPurchaseControllerTests
  WaveRosterResetPurchaseStateTests
  BattleRunStateTests
  WaveResolutionTests
  UnitPoolResetTests
  AllyInteractionPolicyTests
  TacticalReinforcementControllerTests
  AllyPurchaseUiSceneTests
  ```

- [ ] **6.2 전체 EditMode 테스트 실행**

  Unity 6 batchmode로 전체 EditMode suite를 실행하고 XML·로그의 실패/에러 수를 확인한다.

- [ ] **6.3 C# 컴파일 확인**

  Unity batchmode 종료 코드와 Editor 로그에서 `CS####`, `Compilation failed`, 스크립트 컴파일 오류가 없는지 확인한다.

- [ ] **6.4 씬 참조 검사**

  Game 씬 테스트와 로그에서 Missing Script, null 직렬화 참조, 활성 아군 잔존이 없는지 확인한다. 사용자 소유 두 애셋은 수정 전후 Git 상태가 동일한지 별도로 비교한다.

- [ ] **6.5 가능한 Unity 직접 검증**

  에디터 실행이 허용·가능하면 다음 순서로 확인한다.

  1. 새 런 진입: 아군 0명, 네 구매 버튼 비활성, 기본 가격, 쿨다운 0.
  2. 아군 없이 웨이브 시작: 버튼 동작, 상태 `Active`, 구매 버튼 활성.
  3. 일반 구매: 골드 차감, 로스터 증가, 가격·쿨다운 증가.
  4. 증원권 보유 후 무료 구매: `Active`에서만 소비.
  5. 클리어와 실패 결과 표시 중 구매 불가.
  6. 결과 표시 종료 후 아군 0명, 기본 가격, 쿨다운 0, 골드·티켓 유지.
  7. 다음 웨이브, 같은 웨이브 재도전, 최종 승리, 최종 패배에서 같은 회수 규칙.

- [ ] **6.6 AI 활용 기록 작성**

  사용 모델, 요청, 계획, 실제 수정 파일·메서드, 사용자 결정, 테스트 명령·결과, 직접 확인 항목과 제한점을 사실대로 기록한다. 실행하지 못한 검증은 통과로 쓰지 않는다.

- [ ] **6.7 최종 diff·Git 상태 검토**

  계획 밖 파일 변경, 자동 포맷, 씬 재직렬화가 없는지 확인한다. `Rabbit1_Mage_Attack.anim`, `ArcaneVfxCatalog.asset`은 기존 사용자 변경 그대로 남기고 diff·스테이징 대상에서 제외한다.

## 완료 조건

- 확정 요구사항의 상태·결과 매트릭스가 EditMode 테스트로 통과한다.
- 새 런, 다음 웨이브, 재도전, 최종 승리·패배 모두 결과 표시 종료 뒤 아군 0명이다.
- 비활성 구매 실패는 골드, 티켓, 구매 횟수, 가격, 쿨다운, 로스터를 바꾸지 않는다.
- 웨이브 종료 reset은 골드·티켓·업그레이드·아이템·기회·웨이브 정책을 바꾸지 않는다.
- 관련 EditMode, 전체 EditMode, C# 컴파일과 씬 참조 검증 결과가 기록된다.
- 사용자 소유 애셋 변경은 보존되고 작업 diff에 포함되지 않는다.
