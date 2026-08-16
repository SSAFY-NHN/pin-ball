# Battle Follow-up Object-Oriented Refactoring Design

## 배경

1차 전투 도메인 리팩터링으로 전투 상태, 경제, 유닛 생성 규칙, 배치 규칙,
합성 규칙, 대상 검색, 스탯과 스킬 책임은 일반 C# 객체로 분리됐다. 그러나
`UnitManager`는 여전히 유닛 생성, roster 변경, 웨이브 유닛 수명주기, 준비
배치, 합성·진화, 아이템 보정과 자동 포션을 함께 조율한다. `UnitManager`와
`BattleManager`에는 전투 중 매 프레임 실행되는 조건 확인도 남아 있다.

이번 M1 작업은 기존 전투 구조와 공개 API를 유지하면서 `UnitManager` 아래의
보조 책임을 세 Controller로 정리한다. 사용자가 확정한 범위에 따라 사용되지
않던 진화 선택 UI를 복구하고, 전멸 판정과 자동 포션 확인을 이벤트 기반으로
전환한다.

## 고정 제약

- `.github/project-master-prompt.md`의 `3. 기존 프로젝트 구조`를 변경하지 않는다.
- `App`, `AppService`, `Singleton<T>` 구조를 유지한다.
- `BattleManager`, `UnitManager`, `UnitBase`의 이름과 상속을 유지한다.
- 기존 공개 메서드와 이벤트를 삭제하거나 이름을 바꾸지 않는다.
- Scene, Prefab, serialized field와 JSON 데이터 형식을 변경하지 않는다.
- 새 MonoBehaviour, Manager, Service, interface와 외부 패키지를 추가하지 않는다.
- 기존 전투 수치, 보상, 방벽 피해, 유닛 스탯과 풀 부족 시 생성 정책을 유지한다.
- 테스트 코드나 검증 전용 추상화를 추가하지 않는다.
- 테스트, 빌드, Unity Editor, 정적 분석과 전체 validation을 실행하지 않는다.

## 사용자 확정 사항

- 집중형 세 Controller 분리 방식을 사용한다.
- 진화 시 기존 Scene 배치 `EvolutionPanel`에서 두 후보 중 하나를 반드시 선택한다.
- 진화 선택 대기 중 모든 준비 행동을 잠근다.
- 선택 UI에는 정상 취소나 닫기 동작을 추가하지 않는다.
- 유닛 풀은 비어 있을 때 생성하고 반환 후 재사용하는 현재 정책을 유지한다.
- 유닛 사망 또는 전투 목록 변경 직후 이벤트 기반으로 전멸 여부를 판단한다.
- 2초 결과 대기는 코루틴으로 처리하고 `BattleManager.Update()`를 제거한다.
- 아군 피해가 발생했을 때만 다음 프레임 자동 포션 확인을 예약하고
  `UnitManager.Update()`를 제거한다.

## 목표

- `UnitManager`를 roster 소유자, 기존 공개 Facade와 시스템 조정자 수준으로 줄인다.
- 유닛 생성, 준비 단계, 아이템 적용 책임의 소유자를 각각 하나로 만든다.
- 기본 스킬 Registry를 유닛마다 생성하지 않고 Manager 수명 동안 공유한다.
- 사용되지 않던 진화 선택 이벤트와 Scene UI 흐름을 복구한다.
- 전멸 판정과 자동 포션의 상시 프레임 폴링을 제거한다.
- 기존 풀, 전투 규칙과 외부 호출 관계를 유지한다.

## 비목표

- 전투 밸런스, 합성 조건, 진화 후보 데이터 또는 스킬 동작 변경
- UnitSpawner 풀 사전 생성, Scene 사전 배치 또는 풀 상한 도입
- EvolutionPanel 시각 디자인, 카드 레이아웃 또는 닫기 버튼 추가
- Pinball, Item, Tutorial, SoundManager와 일반 UI 시스템 리팩터링
- BattleManager의 AppService 구조 또는 App 기반 서비스 조회 제거
- 새 런 초기화와 난이도 선택 구현
- 테스트 추가, 테스트 수정 또는 별도 검증 체계 도입

## 선택한 접근

`UnitManager` 아래에 일반 C# Controller 세 개를 합성한다. 기존 1차
리팩터링에서 도입한 Service와 도메인 객체는 재사용하고 새 위임 계층을
중복해서 만들지 않는다.

```text
UnitManager : AppService
├─ UnitRoster
├─ UnitTargetFinder
├─ UnitSpawnController
│  ├─ UnitCreationService
│  ├─ UnitSpawner
│  └─ UnitSkillRegistry
├─ UnitPreparationController
│  ├─ UnitPlacementService
│  └─ UnitMergeService
├─ UnitItemController
│  ├─ BattleUnitModifiers
│  └─ reusable HashSet<string>
└─ UnitCombatContext

BattleManager : AppService
├─ BattleRunState
├─ BattleEconomy
├─ WaveResolutionState
└─ wave resolution Coroutine
```

더 깊게 `UnitRosterController`와 `WaveUnitController`까지 추가하는 방식은 기존
`UnitRoster`와 `UnitSpawner` 위에 의미가 겹치는 위임 계층을 만들기 때문에
채택하지 않는다. 새 Controller 없이 메서드만 정리하는 방식은 책임 집중과
상시 폴링을 충분히 해소하지 못해 채택하지 않는다.

## 컴포넌트 책임

### UnitManager

다음 책임을 유지한다.

- `UnitRoster`, `UnitTargetFinder`와 세 Controller의 생성 및 소유
- 기존 공개 유닛 API의 Facade
- roster 등록·제거와 외부 공개 이벤트 발행
- `BattleManager`, `ItemManager` 이벤트 구독과 해제
- 진화 선택 UI 이벤트, 준비 잠금, VFX와 SFX 조율
- 전투 목록 변경 내부 이벤트 발행
- 자동 포션 확인 코루틴 예약
- `IItemEventListener`, `IEnemyBattleActions` 구현

생성 세부 규칙, 준비 배치·합성 상태와 아이템 계산은 직접 소유하지 않는다.

### UnitSpawnController

유닛 데이터 생성과 `UnitSpawner` 호출만 담당한다.

- `IUnitDataSource`, `UnitSpawner`, `UnitCombatContext`를 명시적으로 전달받는다.
- 내부에서 `UnitCreationService`와 기본 `UnitSkillRegistry`를 한 번 생성한다.
- 아군 생성 시 `AllyCommonData`, 최종 스탯, combat context와 공유 Registry를 전달한다.
- 적 생성 시 현재 웨이브, 스폰 인덱스, 선택 위치와 공유 Registry를 전달한다.
- 적 스폰 인덱스를 소유하고 웨이브 적 생성 시작 시 초기화한다.
- 생성 결과만 반환하고 roster 등록, 배치, 이벤트, SFX와 보상은 처리하지 않는다.

풀에 사용 가능한 유닛이 없을 때 `UnitSpawner`가 기존 프리팹을 생성하는 동작은
그대로 둔다.

### UnitPreparationController

준비 단계의 유닛 상호작용과 pending merge 상태를 담당한다.

- `UnitRoster`, `IUnitDataSource`, `BattleAreaBounds`를 명시적으로 전달받는다.
- 기존 `UnitPlacementService`와 `UnitMergeService`를 생성하고 소유한다.
- 준비 행동 가능 여부를 인자로 받아 드래그 가능 조건을 판단한다.
- 배치 유효성, 위치 저장, 빈 격자 배치와 저장 위치 복원을 제공한다.
- 같은 계열 유닛 강조와 강조 해제를 처리한다.
- 합성 시작 결과를 `UnitMergeDecision`으로 반환한다.
- 진화 대기 시 source의 드래그 전 위치를 보유한다.
- 유효한 선택 ID를 `UnitMergeDecision`으로 반환한다.
- 오류 취소 시 source 위치 복원, 예약 해제와 pending 상태 정리를 처리한다.

유닛 생성·반환, UI 이벤트, BattleManager 잠금과 SFX는 처리하지 않는다.

### UnitItemController

전투 유닛에 적용되는 아이템 상태와 자동 포션 규칙을 담당한다.

- `ItemManager`를 명시적으로 전달받는다.
- 기존 `BattleUnitModifiers`를 생성하고 소유한다.
- `OnItemEvent`로 받은 수치를 저장하고 활성 아군에 최종 보정을 적용한다.
- 다양성 계산에 사용하는 `HashSet<string>`을 필드로 보유하고 호출마다 재사용한다.
- 활성 아군 중 첫 HP 50% 미만 생존 아군을 찾는다.
- 파티 포션을 먼저 소비하고, 없으면 개인 포션을 소비한다.
- 파티 포션 성공 시 모든 활성 생존 아군을 25% 회복한다.
- 개인 포션 성공 시 선택된 아군을 50% 회복한다.

Coroutine과 Unity 생명주기는 소유하지 않는다. 다음 프레임 실행 예약은
`UnitManager`가 담당한다.

### BattleManager

기존 run, 경제, 결과 처리 책임을 유지한다. `UnitManager`의 내부 전투 목록 변경
이벤트를 구독하고 Active 상태에서 `BattleResolutionPolicy`를 즉시 실행한다.
전멸이 확인되면 기존 보상과 피해 순서로 `BeginWaveResolution`을 실행하고 결과
대기 Coroutine을 시작한다.

기존 빈 `Awake()`와 상시 `Update()`는 제거한다.

### UnitCombatContext와 UnitBase

`UnitCombatContext`에 선택적인 `Action<UnitBase> NotifyUnitDamaged`를 추가한다.
기존 세 인자 생성 코드는 그대로 컴파일되도록 네 번째 인자는 선택 인자로 둔다.

`UnitBase.TakeDamage`는 실제 HP 피해가 적용되고 유닛이 생존한 경우에만 피해
callback을 호출한다. 사망한 유닛은 기존 `NotifyUnitDied` 흐름만 사용한다.

### EvolutionPanel

기존 두 `EvolutionChoiceView`를 그대로 사용한다. 후보 바인딩에 성공하면 패널을
표시하고, 유효한 선택이 완료된 경우에만 숨긴다. 후보 바인딩 실패 시
`UnitManager`의 내부 오류 취소 API를 호출해 예약 유닛과 준비 잠금을 복원한다.
사용자가 호출할 수 있는 정상 취소 버튼은 추가하지 않는다.

## 초기화와 소유권

- Scene은 기존 `BattleManager`, `UnitManager`, `UnitSpawner`, UI와 유닛 Prefab을 소유한다.
- `UnitManager.Awake`는 기존처럼 `UnitRoster`와 `UnitTargetFinder`를 생성한다.
- `UnitManager.Start`는 App으로 BattleManager, TitleData와 ItemManager를 조회한다.
- 같은 Start에서 `UnitCombatContext`와 세 Controller를 생성한다.
- `UnitSpawnController`가 생성한 기본 Registry는 해당 Controller 수명 동안 공유한다.
- 각 Controller는 App 또는 Find 계열 API를 호출하지 않는다.
- `UnitManager.OnDestroy`가 pending 진화와 Coroutine을 정리하고 이벤트 구독을 해제한다.

## 주요 데이터 흐름

### 아군 생성

1. 기존 외부 호출자가 `UnitManager.SpawnAlly`를 호출한다.
2. UnitManager가 `UnitSpawnController`에 생성 데이터를 전달한다.
3. Controller가 최종 데이터를 계산하고 UnitSpawner에서 유닛을 얻는다.
4. UnitManager가 `UnitPreparationController`로 빈 준비 격자에 배치한다.
5. 배치에 실패하면 기존처럼 UnitSpawner에 반환한다.
6. 성공하면 UnitManager가 roster에 등록하고 아이템 보정을 갱신한다.
7. 배치 수 이벤트와 생성 SFX를 기존 시점에 발생시킨다.

### 적 생성과 강화 소환

1. Active 상태 진입 시 UnitManager가 기존 적을 반환하고 roster를 정리한다.
2. UnitSpawnController가 스폰 인덱스를 초기화하고 현재 웨이브 적을 생성한다.
3. UnitManager가 각 적을 roster에 등록한다.
4. 웨이브 적 생성 배치가 끝난 뒤 전투 목록 변경 이벤트를 한 번 발생시킨다.
5. 적 스킬 강화 소환은 기존 `IEnemyBattleActions` API를 통해 같은 생성 경로를 사용한다.

### 유닛 사망과 전멸 판정

1. UnitBase가 사망하면 기존 UnitCombatContext callback으로 UnitManager에 알린다.
2. UnitManager가 roster에서 제거하고 UnitSpawner에 반환한다.
3. 아군이면 보유 목록과 준비 위치도 정리하고 배치 수 이벤트를 발생시킨다.
4. roster 변경이 완료된 직후 내부 전투 목록 변경 이벤트를 발생시킨다.
5. BattleManager는 Active 상태에서 현재 아군·적 수를 즉시 판정한다.
6. 전멸이면 Resolving 상태로 전환하고 보상 또는 방벽 피해를 적용한다.
7. 기존 결과 SFX와 `OnWaveResolutionStarted`를 발생시킨다.
8. 결과 대기 Coroutine을 시작한다.

즉시 이벤트 판정은 기존 Update 폴링보다 같은 프레임 안에서 먼저 결과를 확정할
수 있다. 사용자가 이 방식을 명시적으로 선택했다. 같은 프레임에 여러 사망이
순차 처리되는 경우 첫 번째로 성립한 결과가 Resolving 상태를 선점하며 이후
이벤트는 무시된다. 승패 결정 자체는 기존 `BattleResolutionPolicy`를 사용한다.

### 결과 대기

1. Coroutine이 `WaitForSeconds(waveResolutionDuration)`로 대기한다.
2. 대기 후 현재 상태와 pending 결과를 다시 확인한다.
3. UnitManager에 웨이브 결과 유닛 정리를 요청한다.
4. 기존 정책에 따라 웨이브 증가 또는 Pending, Victory, Defeat로 전환한다.
5. Coroutine 참조를 정리한다.

duration이 0이어도 기존 Update 흐름처럼 다음 Coroutine 재개 시점에 마무리한다.

### 자동 포션

1. 생존한 아군이 실제 HP 피해를 받으면 UnitCombatContext가 UnitManager에 알린다.
2. UnitManager는 자동 포션 Coroutine이 없을 때만 새 Coroutine을 시작한다.
3. 같은 프레임의 추가 피해 요청은 기존 예약으로 합친다.
4. Coroutine이 한 프레임 대기한 뒤 참조를 먼저 비운다.
5. UnitItemController가 현재 활성 아군을 순서대로 확인한다.
6. 첫 HP 50% 미만 생존 아군에서 파티 포션, 개인 포션 순서로 소비를 시도한다.
7. 두 포션이 없어도 해당 첫 대상 이후의 아군은 확인하지 않는다.

이는 기존 UnitManager.Update의 대상 순서와 포션 우선순위를 유지한다.

### 합성과 진화 선택

1. 기존 `TryMergeAllies`가 UnitPreparationController에 합성 시작을 요청한다.
2. 거부 결과면 필요할 때 source 위치를 복원하고 false를 반환한다.
3. 일반 합성이면 UnitManager가 두 입력을 반환하고 결과 유닛을 생성한다.
4. 진화 대기 결과면 Controller가 source 원래 위치와 pending 결정을 보유한다.
5. UnitManager가 준비 행동을 잠그고 `OnEvolutionRequested`를 발생시킨다.
6. `TryMergeAllies`는 합성 예약이 성립했으므로 true를 반환한다.
7. UI 선택 시 `ChooseEvolution`이 Controller에 ID 검증을 요청한다.
8. 유효하면 두 입력을 반환하고 선택 유닛을 생성한다.
9. Controller가 예약과 pending 상태를 완료 처리한다.
10. UnitManager가 준비 잠금을 해제하고 VFX, SFX와 `OnAlliesMerged`를 발생시킨다.

`OnAlliesMerged`는 진화 후보를 표시할 때가 아니라 실제 선택 완료 후 발생한다.

## 오류 처리

- 생성 데이터가 null이거나 스탯이 유효하지 않으면 기존 경고와 null 반환을 유지한다.
- 풀과 Prefab이 모두 유효하지 않으면 기존 UnitSpawner 오류를 유지한다.
- 진화 후보가 정확히 두 개가 아니면 합성을 거부하고 예약을 해제한다.
- `OnEvolutionRequested` 구독자가 없으면 source 위치를 복원하고 pending 예약과 잠금을 해제한다.
- EvolutionPanel 후보 바인딩 실패도 같은 내부 오류 취소 경로를 사용한다.
- 잘못된 진화 ID 선택은 false를 반환하고 pending 상태와 패널을 유지한다.
- 진화 유닛 생성 실패 시 기존 동작처럼 입력 유닛을 이미 소비한 결과를 유지한다.
- 전투 목록 이벤트가 중복 발생해도 State가 Active가 아니면 결과 처리를 시작하지 않는다.
- 자동 포션 실행 시 대상이 사망했거나 풀에 반환됐으면 건너뛴다.
- 예외를 삼키거나 누락된 참조를 Find 계열 API로 자동 대체하지 않는다.

## 공개 API와 호환성

- UnitManager의 기존 public event와 public method를 유지한다.
- BattleManager의 기존 public event와 public method를 유지한다.
- UnitBase의 기존 공개 전투 API를 유지한다.
- UnitCombatContext의 기존 세 인자 생성 코드는 선택 인자 기본값으로 보존한다.
- `UnitMergeService.TryChooseAutomaticEvolution`은 기존 코드와 Editor 테스트의 소스
  호환을 위해 유지하지만 게임 런타임 합성 흐름에서는 호출하지 않는다.
- 해당 메서드의 TODO는 실제 UI 선택이 복구됐으므로 호환 메서드임을 설명하는
  주석으로 교체한다.
- PinballManager의 `SpawnAlly` 호출과 EvolutionPanel의 `ChooseEvolution` 호출은 유지한다.

## Unity 직렬화와 수명주기

- 기존 MonoBehaviour, namespace, 상속과 SerializeField 이름을 바꾸지 않는다.
- 새 Controller는 일반 C# 객체이며 Scene 또는 Prefab Component를 추가하지 않는다.
- UnitSpawner의 프리팹, 스폰 지점과 풀 부모 Inspector 참조를 유지한다.
- 기존 SetActive 기반 UnitBase 풀 반환과 초기화 흐름을 유지한다.
- 새 반복 Instantiate 또는 Destroy를 추가하지 않는다.
- Scene과 Prefab YAML 수정은 필요하지 않다.

## 성능

- UnitManager와 BattleManager의 상시 Update를 제거한다.
- 기본 UnitSkillRegistry와 factory Dictionary를 유닛마다 재생성하지 않는다.
- 다양성 아이템의 유닛 종류 HashSet을 호출마다 생성하지 않고 재사용한다.
- 자동 포션 Coroutine은 피해 요청이 있을 때 하나만 존재한다.
- 결과 대기 Coroutine은 Resolving 상태당 하나만 존재한다.
- 적 생성과 유닛 반환의 기존 배열 snapshot과 풀 확장 정책은 변경하지 않는다.

## 파일 범위

### 신규 파일

- `Assets/02. Scripts/Battle/Units/UnitSpawnController.cs`
- `Assets/02. Scripts/Battle/Units/UnitPreparationController.cs`
- `Assets/02. Scripts/Battle/Units/UnitItemController.cs`
- 각 파일의 Unity `.meta`

### 수정 파일

- `Assets/02. Scripts/Battle/UnitManager.cs`
- `Assets/02. Scripts/Battle/BattleManager.cs`
- `Assets/02. Scripts/Battle/UnitBase.cs`
- `Assets/02. Scripts/Battle/Units/UnitCombatContext.cs`
- `Assets/02. Scripts/Battle/Units/UnitMergeService.cs`
- `Assets/02. Scripts/03. UI/EvolutionPanel.cs`
- `.github/ai-use-log.md`

### 의도적으로 수정하지 않는 파일

- Scene과 Prefab Asset
- UnitSpawner와 데이터 파일
- Pinball, Item, Tutorial, SoundManager 내부 구현
- EvolutionChoiceView와 다른 UI Panel
- Editor 및 기존 테스트 코드
- Core의 App, AppService와 Singleton<T>

## 검사 및 검증 제외

사용자의 명시적 지시에 따라 다음을 수행하지 않는다.

- 테스트 코드 또는 fixture 작성과 수정
- 기존 테스트, EditMode, PlayMode와 Unity Test Runner 실행
- dotnet test, dotnet build와 Unity Build 실행
- Unity Editor 또는 Test Scene 실행
- 정적 분석, 전체 validation, coverage와 검사용 검사 실행
- 검사를 위한 interface, wrapper, DI 또는 mock 경계 추가

구현 시에는 현재 타입, signature, 호출 관계와 이벤트 순서를 직접 읽어 논리적
일관성을 유지한다. 이는 별도 테스트나 검증 명령 실행을 의미하지 않는다.

## 완료 기준

- UnitManager와 BattleManager의 이름, 상속, 기존 공개 API가 유지된다.
- UnitSpawnController, UnitPreparationController, UnitItemController의 책임이 겹치지 않는다.
- UnitManager.Update와 BattleManager.Update가 제거된다.
- 기본 UnitSkillRegistry가 UnitSpawnController 수명 동안 하나만 생성된다.
- 기존 UnitSpawner의 on-demand 생성과 SetActive 풀링이 유지된다.
- 진화 합성 시 준비 행동이 잠기고 기존 EvolutionPanel에 두 후보가 표시된다.
- 유효한 선택 후 진화 유닛, VFX, SFX, OnAlliesMerged와 잠금 해제가 처리된다.
- UI 표시 실패 시 입력 유닛 위치, 합성 예약과 준비 잠금이 복원된다.
- 전투 목록 변경 직후 기존 BattleResolutionPolicy로 전멸을 판정한다.
- 기존 보상·방벽 피해 적용과 2초 Resolving 상태 순서가 유지된다.
- 아군 피해가 있을 때만 다음 프레임 자동 포션 확인을 한 번 예약한다.
- 자동 포션의 대상 순서, 파티 우선과 회복 수치가 유지된다.
- Scene, Prefab, 데이터, 테스트 코드가 변경되지 않는다.
- 테스트, 빌드, Unity 실행과 별도 validation을 수행하지 않는다.
