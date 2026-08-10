# 전투 도메인 객체 지향 리팩터링 설계

## 배경

현재 전투 시스템은 기능을 빠르게 추가하는 과정에서 `UnitManager`,
`UnitBase`, `AllyUnit`, `EnemyUnit`에 목록 관리, 생성, 배치, 합성, 대상 탐색,
전투, 상태 효과, 스킬 실행 책임이 집중되어 있다. 코드와 Inspector 연결이
동작하고 있으나 한 기능을 변경할 때 여러 책임을 함께 이해해야 하며, 일반
C# 객체만으로 전투 규칙을 검증하기 어렵다.

이번 리팩터링은 기존 플레이 규칙과 데이터 구조를 유지하면서 전투 도메인의
책임을 분리하고, 확인된 미사용 코드를 함께 제거하는 것을 목표로 한다.

## 목표

- 하나의 클래스가 하나의 명확한 변경 이유를 갖도록 책임을 분리한다.
- Unity 생명주기와 Inspector 연결은 얇은 `MonoBehaviour` 계층에 둔다.
- 전투 규칙은 가능한 한 일반 C# 객체로 옮겨 독립 테스트할 수 있게 한다.
- `BattleManager`, `UnitManager`, `UnitBase`를 조정자 또는 Facade 수준으로
  축소한다.
- 큰 조건문과 문자열 분기를 스킬 전략 객체로 교체한다.
- 코드, 직렬화, 데이터 참조가 모두 없는 미사용 코드만 제거한다.
- 리팩터링 전후의 게임 규칙, 수치, 이벤트 시점, 플레이 결과를 유지한다.

## 범위

다음 전투 도메인 전체를 1차 리팩터링 범위로 한다.

- `BattleManager`, `BattleDataTypes`
- `UnitManager`, `UnitSpawner`, `BattleAreaBounds`
- `UnitBase`, `AllyUnit`, `EnemyUnit`
- 전투 유닛 프리팹과 Game 씬의 관련 Inspector 연결
- 전투 매니저 API를 사용하는 핀볼 및 UI 코드의 최소 연결 수정
- 전투 영역에서 확인된 미사용 코드 제거

관련 UI의 디자인이나 핀볼 규칙 자체는 변경하지 않는다. JSON 필드와 저장
형식, 밸런스 수치, 웨이브 구성도 변경하지 않는다.

## 승인된 변경 범위

- 기존 씬과 프리팹의 Inspector 참조 재연결을 허용한다.
- 내부 및 공개 API 변경을 허용한다.
- 새 클래스와 파일은 기존 `Assets/02. Scripts/Battle` 아래에 추가한다.
- 새로운 저장소 최상위 폴더나 외부 패키지는 추가하지 않는다.
- API 또는 Inspector 구조가 달라져도 최종 플레이 동작은 동일해야 한다.

## 선택한 접근법

점진적 객체 합성 방식을 사용한다.

- 씬과 프리팹에 배치되는 루트 `MonoBehaviour`는 유지한다.
- 각 루트는 일반 C# 객체를 생성하고 의존성을 주입하는 Composition Root가
  된다.
- 일반 C# 객체는 `App.Get<T>()`를 호출하지 않는다.
- 교체 가능성이나 독립 테스트 경계가 실제로 필요한 경우에만 인터페이스를
  사용한다.
- 기존 호출부는 호환 메서드를 거쳐 단계적으로 새 API로 이동하고, 모든
  호출부 이전이 끝나면 호환 메서드를 제거한다.

Unity 컴포넌트를 모든 책임마다 추가하는 방식은 Inspector와 초기화 순서를
지나치게 복잡하게 만들 수 있어 채택하지 않는다. Unity에 독립적인 완전한
전투 시뮬레이션 계층을 새로 구축하는 방식도 현재 프로토타입에서 재작성
위험이 크므로 채택하지 않는다.

## 전체 아키텍처

```text
Scene / Prefab
├─ BattleManager        전투 진행 유스케이스 조정과 기존 이벤트 전달
├─ UnitManager          유닛 유스케이스를 외부에 제공하는 Facade
├─ UnitSpawner          Unity 프리팹 풀과 오브젝트 생성·반환
└─ UnitBase             한 유닛의 Unity 표현과 하위 객체 조립
      ↓
Plain C# battle objects
├─ BattleRunState
├─ BattleEconomy
├─ BarrierDamageCalculator
├─ UnitRoster
├─ UnitCreationService
├─ UnitPlacementService
├─ UnitMergeService
├─ UnitTargetFinder
├─ BattleUnitModifiers
├─ UnitHealth
├─ UnitMovement
├─ UnitAttack
├─ UnitStatusEffects
├─ UnitSkillController
└─ UnitSkillRegistry / IUnitSkill implementations
```

## 컴포넌트 책임

### BattleManager

전투 진행 유스케이스를 조정한다. 웨이브 시작 조건을 확인하고, 상태 전환을
지시하며, 기존 UI가 사용하는 이벤트를 전달한다. 골드 계산이나 방벽 피해
공식은 직접 소유하지 않는다.

### BattleRunState

현재 웨이브, 방벽 HP, 전투 상태를 보유한다. 유효한 상태 전환만 허용하고
Unity API에 의존하지 않는다.

### BattleEconomy

현재 골드를 보유하고 소비 및 보상을 처리한다. 음수 소비나 보상은 기존
규칙과 동일하게 무시하거나 보정하며, 잔액 부족은 결과값으로 반환한다.

### BarrierDamageCalculator

남아 있는 적의 돌파 피해 합계, 방벽 피해 감소, 최소 피해를 계산한다. 상태를
변경하지 않는 순수 계산 객체로 유지한다.

### UnitManager

핀볼, UI, 스킬이 사용하는 유닛 관련 API를 제공하는 Facade다. 하위 서비스의
유스케이스를 연결하고 보유 수 변경, 진화 요청, 상세 보기 요청 이벤트를
외부에 전달한다. 목록, 합성 상태, 배치 위치, 대상 탐색 규칙을 직접 구현하지
않는다.

### UnitRoster

보유 아군, 활성 아군, 활성 적 목록을 관리한다. 외부에는 읽기 전용 목록과
개수만 노출한다. 등록, 사망, 풀 반환, 웨이브 종료 시 목록 불변식을 보장한다.

### UnitCreationService

`TitleData`에서 유닛 데이터를 조회하고 레벨, 합성, 장착, 아이템 보정을 적용해
최종 `BattleUnitStats`를 만든다. Unity 오브젝트를 생성하지 않는다. 유효하지
않은 데이터는 명시적인 실패 결과로 반환한다.

### UnitSpawner

아군 및 적 프리팹 풀을 관리한다. 최종 스탯과 초기화 데이터를 받은 뒤 Unity
오브젝트를 활성화하고 반환한다. 전투 규칙, 목록 등록, 위치 선정은 담당하지
않는다.

### UnitPlacementService

준비 영역 내부 좌표 제한, 다른 유닛과의 겹침 검사, 빈 그리드 위치 탐색,
준비 위치 저장과 복원을 담당한다. 실제 `Transform` 이동은 호출자인 Unity
Actor가 수행한다.

### UnitMergeService

합성 가능 조건, 합성 예약, 일반 합성 결과, 진화 후보, 진화 선택 완료를
담당한다. UI를 직접 열지 않고 결과나 이벤트 데이터만 반환한다.

### UnitTargetFinder

`UnitRoster`의 읽기 전용 스냅샷을 이용해 최근접 적, 최근접 아군, 최원거리
아군, 최고 HP 아군, 반경 대상, 직선 대상을 찾는다. 대상의 상태를 변경하지
않는다.

### BattleUnitModifiers

공격력, 공격 속도, 최대 HP, 다양성, 복제 아이템 수치를 보유하고 최종 유닛
스탯에 적용한다. 아이템 이벤트 수신은 상위 Unity 서비스가 담당하며 이
객체에는 값만 전달한다.

### UnitBase

한 유닛의 Unity Actor다. `Transform`, Renderer, 월드 체력 UI처럼 Unity 표현을
소유하고, 하위 일반 C# 객체를 생성·연결하며 Unity 프레임을 전달한다. 체력,
이동, 공격, 상태 효과 공식을 직접 구현하지 않는다.

### UnitHealth

현재 HP, 최대 HP, 보호막을 관리하고 피해, 회복, 사망 결과를 계산한다. 사망
결과는 이벤트 또는 반환값으로 Actor에 알린다.

### UnitMovement

대상 추적 이동, 이동 속도 보정, 넉백 결과를 계산한다. 실제 위치 적용이
필요한 경우 최소한의 Unity 어댑터를 통해 수행한다.

### UnitAttack

공격 가능 시간, 사거리, 기본 공격 피해를 관리한다. 대상 선정과 스킬 실행은
담당하지 않는다.

### UnitStatusEffects

기절, 공격력·공격 속도·이동 속도·방어력 보정, 피해 감소, 넉백 면역, 지속
피해의 지속시간과 현재 값을 관리한다. 현재 코드의 갱신과 중첩 규칙을 그대로
이전한다.

### AllyUnit과 EnemyUnit

`AllyUnit`은 준비 단계 입력, 마나 획득, 아군 스킬 사용 시점만 담당한다.
`EnemyUnit`은 적 전용 전투 시작 효과와 단계별 스킬 사용 시점만 담당한다.
공통 이동, 공격, 체력, 상태 효과는 `UnitBase`가 조립한 객체를 사용한다.

## 스킬 구조

기존 스킬 ID `switch`는 Registry와 전략 객체로 교체한다.

```text
UnitSkillController
  └─ UnitSkillRegistry
       ├─ shield_judgment -> ShieldJudgmentSkill
       ├─ blood_whirlwind -> BloodWhirlwindSkill
       ├─ arrow_rain -> ArrowRainSkill
       ├─ wolf_sprint -> WolfSprintSkill
       ├─ ground_slam -> GroundSlamSkill
       └─ ...
```

각 스킬은 `IUnitSkill`을 구현한다. 실행에 필요한 사용자, 대상 탐색, 스킬
데이터는 `SkillContext`로 전달한다. 스킬 객체는 Manager나 UI를 직접 조회하지
않는다. 알 수 없는 스킬 ID는 현재처럼 경고를 남기고 실행하지 않는다.

공통 피해, 반경 검색, 퍼센트 변환을 위한 작은 보조 객체나 함수는 재사용하되
스킬별 게임 규칙을 하나의 범용 해석기로 과도하게 일반화하지 않는다.

## 주요 데이터 흐름

### 아군 소환

1. 핀볼 골인이 `UnitManager`에 소환을 요청한다.
2. `UnitCreationService`가 JSON 데이터와 현재 보정값으로 최종 스탯을 만든다.
3. `UnitSpawner`가 풀에서 Unity 오브젝트를 확보한다.
4. `UnitPlacementService`가 준비 위치를 결정한다.
5. `UnitRoster`가 보유·활성 아군으로 등록한다.
6. `UnitManager`가 기존 보유 수 변경 이벤트를 전달한다.

### 웨이브 진행

1. `BattleManager`가 준비 상태, 데이터, 아군 수를 확인한다.
2. 상태를 Active로 변경하고 기존 상태 이벤트를 발행한다.
3. `UnitManager`가 현재 웨이브의 적 생성 유스케이스를 실행한다.
4. 각 유닛은 대상 탐색 후 이동, 기본 공격 또는 스킬을 실행한다.
5. 사망한 유닛은 `UnitRoster`의 활성 목록에서 제거된다.
6. `BattleManager`는 기존과 동일하게 프레임 단위로 남은 아군과 적 수를
   검사한다.
7. 승리 또는 패배 결과를 처리하고 유닛을 준비 상태로 복원한다.

### 합성과 진화

1. 준비 단계 드래그 종료 시 `UnitManager`가 합성을 요청한다.
2. `UnitMergeService`가 조건을 검사하고 두 유닛을 예약한다.
3. 일반 합성이면 결과 데이터를 반환해 즉시 새 유닛을 생성한다.
4. 진화 합성이면 두 후보를 이벤트 데이터로 반환한다.
5. UI 선택 결과를 `UnitMergeService`가 검증한 후 결과 유닛을 생성한다.
6. 성공 또는 취소 시 예약 상태를 항상 해제한다.

## 의존성 규칙

- UI와 핀볼은 `BattleManager`, `UnitManager` Facade만 사용한다.
- 일반 C# 객체는 `App.Get<T>()`, `Find*`, `Resources.Load`를 호출하지 않는다.
- 데이터와 Unity 어댑터는 Composition Root에서 명시적으로 전달한다.
- 외부에 노출하는 컬렉션은 읽기 전용이다.
- 하위 객체는 서로의 구체 타입보다 필요한 최소 계약에 의존한다.
- 인터페이스는 스킬 실행, 시간 또는 난수처럼 테스트에서 교체가 필요한
  경계에만 사용한다.

## 오류 처리

- 필수 Inspector 참조가 없으면 초기화 시 객체와 필드 이름을 포함한 오류를
  발생시킨다.
- 유닛 데이터와 스탯이 유효하지 않으면 생성 전에 실패 결과를 반환한다.
- 골드 부족, 배치 제한, 잘못된 준비 단계 같은 정상적인 게임 거부는 `Try*`
  결과와 기존 피드백 이벤트로 처리한다.
- 예상하지 못한 예외는 삼키지 않고 원래 스택 정보를 유지한다.
- 풀에 반환되는 유닛은 대상, 타이머, HP, 마나, 상태 효과가 초기화됐는지
  검증한다.
- 알 수 없는 스킬 ID는 경고 후 해당 스킬만 실행하지 않는다.

## 미사용 코드 제거 정책

삭제 전 다음 참조를 모두 확인한다.

1. C# 직접 호출 및 타입 참조
2. 상속, 인터페이스 구현, 리플렉션 진입점
3. UnityEvent와 씬·프리팹 직렬화
4. JSON의 문자열 기반 ID와 데이터 참조
5. 테스트 코드와 Editor 도구

모든 참조가 없고 현재 동작에 영향이 없는 경우에만 삭제한다. 빈 생명주기
메서드, 사용하지 않는 `using`, 이전 완료 후 남은 호환 메서드는 우선 제거
후보다. 미완성 기능, 낮은 호출 횟수, TODO만으로는 죽은 코드로 판단하지
않는다. 판단이 모호한 항목은 유지하고 결과 보고에 기록한다.

## 마이그레이션 순서

1. 현재 전투 규칙을 특성 테스트로 고정한다.
2. `UnitRoster`와 `UnitTargetFinder`를 추출한다.
3. `UnitCreationService`, `UnitPlacementService`, `UnitMergeService`를 추출한다.
4. `UnitHealth`, `UnitMovement`, `UnitAttack`, `UnitStatusEffects`를 추출한다.
5. 아군 및 적 스킬을 `IUnitSkill` 구현으로 이동한다.
6. `BattleRunState`, `BattleEconomy`, `BarrierDamageCalculator`를 추출한다.
7. UI, 핀볼, 씬과 프리팹을 새 API와 Inspector 구조에 재연결한다.
8. 호환 메서드와 확인된 미사용 코드를 제거한다.
9. 전체 Edit Mode, Play Mode, WebGL 검증을 실행한다.

각 단계는 컴파일과 관련 테스트를 통과한 뒤 다음 단계로 진행한다. 실패 시
해당 단계만 수정하거나 되돌릴 수 있도록 변경 범위를 작게 유지한다.

## 테스트 전략

### 기존 테스트

현재 8개 테스트 파일과 58개 테스트 또는 테스트케이스를 유지하며 모두
통과해야 한다.

### 새 Edit Mode 테스트

- 웨이브 상태 전환과 최종 승패
- 골드 소비, 보상, 잔액 부족
- 돌파 피해, 감소 수치, 최소 피해
- 보유·활성 유닛 목록과 사망 처리
- 최근접, 최원거리, 최고 HP, 반경 및 직선 대상 검색
- 레벨, 합성, 장착, 아이템 보정 후 최종 스탯
- 준비 영역 제한, 겹침, 빈 슬롯, 위치 저장과 복원
- 합성 예약, 일반 합성, 진화 후보와 결과
- 피해, 보호막, 회복, 사망, 시간 기반 상태 효과
- 아군 및 적 스킬의 피해, 버프, 대상 선택
- 풀 반환 후 HP, 대상, 타이머, 마나, 상태 효과 초기화

### Unity 통합 검증

- Developer에서 Title을 거쳐 Game으로 전환
- 핀볼 골인으로 네 종류 아군 소환
- 5, 6, 7마리 배치 및 시작 제한
- 드래그 배치, 합성, 진화 선택
- 웨이브 시작과 적 생성
- 아군 및 적 자동 이동, 공격, 스킬
- 아군 전멸 시 방벽 피해와 재도전
- 10웨이브 보스 처치와 승리
- Missing Script와 누락된 Inspector 참조 검사
- PC WebGL Development Build 성공

## 완료 조건

- 기존 게임 규칙, JSON 구조, 밸런스 수치가 변경되지 않는다.
- 기존 테스트와 새 테스트가 모두 통과한다.
- 컴파일 오류, Missing Script, 필수 Inspector 누락이 없다.
- 주요 Manager가 규칙 구현 대신 유스케이스 조정만 담당한다.
- 분리된 전투 규칙 객체를 Unity 실행 없이 테스트할 수 있다.
- 스킬별 책임이 독립 객체에 위치하며 큰 ID `switch`가 제거된다.
- 제거한 미사용 코드와 삭제 근거를 결과에 기록한다.
- 직접 플레이 검증 항목과 자동 검증하지 못한 제한을 보고한다.

## 확정 결정

- 전투 도메인 전체를 리팩터링한다.
- Inspector 재연결과 API 변경을 허용한다.
- 점진적 객체 합성 방식을 사용한다.
- 플레이 동작은 유지한다.
- 확실히 사용되지 않는 코드만 제거한다.
- 관련 UI는 새 API 연결에 필요한 최소 범위만 수정한다.
