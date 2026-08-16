# 핀볼 시스템 객체지향 리팩터링 설계

## 배경

현재 핀볼 시스템은 플레이 동작과 풀링 자체는 유지되고 있지만, PinballManager가
공 풀 상태, 발사 비용, 골 선택과 교환, 아이템 효과 수치, 충돌 보상, 분열 공
활성화, 유닛 소환까지 함께 담당한다. 이번 작업은 기능 추가가 아니라 기존
핀볼 동작을 유지한 채 내부 책임과 소유권을 분리하는 리팩터링이다.

테스트 용이성을 위한 추상화가 아니라 실제 책임 집중, 상태 소유권과 런타임
검색 문제만 대상으로 한다.

## 고정 제약

.github/project-master-prompt.md의 3. 기존 프로젝트 구조를 변경하지 않는다.

- App, AppService, Singleton<T> 구조를 유지한다.
- PinballManager의 클래스명과 AppService 상속을 유지한다.
- 기존 공개 이벤트와 공개 API를 유지한다.
- Scene과 Prefab의 기존 계층, 직렬화 필드명과 데이터 형식을 유지한다.
- 외부 패키지, DI 프레임워크, 새 전역 서비스나 최상위 폴더를 추가하지 않는다.
- 게임 규칙, 수치, 이벤트 순서, 풀링 방식과 플레이 결과를 변경하지 않는다.
- 테스트 코드, 검사용 추상화와 검증 전용 코드를 추가하지 않는다.
- 테스트, 빌드, Unity Editor, 정적 분석과 전체 validation을 실행하지 않는다.

## 목표

- PinballManager를 핀볼 유스케이스 조정자와 기존 공개 Facade 역할로 축소한다.
- 사용 가능·장전·활성 공 상태의 소유자를 하나로 만든다.
- 발사 비용과 성공 횟수 상태의 소유자를 하나로 만든다.
- 핀볼 아이템 효과 수치와 계산을 Manager에서 분리한다.
- 골 등록·정렬·선택·교환·폭 조절을 하나의 Controller로 모은다.
- FindFirstObjectByType<PinballManager>() 런타임 검색을 제거한다.
- 기존 SetActive 기반 공 풀과 현재 이벤트 시점을 그대로 유지한다.

## 비목표

- 전투, 아이템, 튜토리얼 또는 UI 시스템 리팩터링
- App.Get<T>() 기반 프로젝트 서비스 구조 제거
- 핀볼 물리 수치, 골 보상, 아이템 밸런스 또는 유닛 소환 규칙 변경
- 기존 VFX 구조 재설계 또는 모든 초기화 객체의 풀링
- 현재 사용 근거가 없는 구형 Assets/04. Prefabs/Pinball.prefab 정리
- 테스트 추가, 빌드 수정 또는 별도 검증 체계 도입

## 선택한 접근

기존 PinballManager 아래에 일반 C# 객체를 합성하는 집중형 구조를 사용한다.
새 책임 객체는 PinballManager가 생성하고 소유하며, Unity Component와 Scene
객체의 생명주기는 기존 MonoBehaviour가 계속 담당한다.

구조는 다음과 같다.

    App / AppService
    └─ PinballManager : AppService
       ├─ PinballBallPool
       ├─ PinballLaunchState
       ├─ PinballItemModifiers
       └─ PinballGoalController

    Scene / Prefab MonoBehaviours
    ├─ Pinball
    ├─ PinballGoal
    ├─ PinballLauncherController
    ├─ PinballOutZone
    ├─ PinballReflectorController
    ├─ PinballMagnetController
    └─ PinballLaunchCostDisplay

새 interface는 만들지 않는다. 네 객체 모두 실제 구현이 하나이고 런타임 교체
요구가 없기 때문이다. 새로운 MonoBehaviour도 추가하지 않는다.

## 컴포넌트 책임

### PinballManager

핀볼 유스케이스의 Unity 진입점이자 기존 호출자를 위한 Facade다. 전투, 유닛,
아이템과 데이터 서비스를 기존 App.Get<T>()로 한 번 조회하고 하위 객체를
조립한다. 발사, 충돌, 골인과 실패 이벤트를 조정하고 기존 이벤트를 전달한다.
공 컬렉션, 비용 계산, 아이템 수치와 골 선택 규칙을 직접 구현하지 않는다.

### PinballBallPool

Inspector의 기존 pooledBalls를 받아 사용 가능 공, 장전 공과 활성 공 상태를
관리한다. 각 공은 동시에 하나의 상태에만 속하게 한다. 공을 생성하거나
파괴하지 않고 기존 LoadAt, Activate, Deactivate와 SetActive 수명주기를
사용한다. 외부에는 활성 공 읽기 전용 컬렉션과 상태 질의만 제공한다.

### PinballLaunchState

기본 비용, 성공당 증가 비용, 할인, 최소 비용과 성공 발사 횟수를 보유한다.
현재 발사 비용 계산, 성공 횟수 증가와 준비 단계 복귀 시 횟수 초기화를
담당한다. Unity 객체, Manager 또는 UI에 의존하지 않는다.

### PinballItemModifiers

Golden Ball, Auto Ball Feeder, Target Magnet, Split Capsule, Golden Bumper,
Focused Pocket, Swap Lever, Charged Pin과 Overload Bumper의 현재 효과 수치를
보유한다. Item 값을 현재 수치에 반영하고 보상, 조건과 배율을 계산한다.
골드 추가, 공 활성화와 유닛 소환처럼 외부 상태를 직접 변경하지 않는다.

### PinballGoalController

Scene의 Goal 등록과 해제, x좌표 정렬, 현재 선택 골, 교환 대기 골, 남은 교환
횟수와 Focused Pocket 폭 갱신을 담당한다. Goal의 충돌, 입력, VFX와 직렬화된
BattleUnitSpawnData는 소유하지 않는다. Scene이 Goal Component를 소유하고
Controller는 등록된 참조와 선택 상태만 소유한다.

### Pinball

Rigidbody2D 물리, 공별 충돌 카운터와 아이템 사용 카운터, 공 VFX 연결을
유지한다. Manager를 명시적으로 전달받지 않고 활성화 시점에
App.Get<PinballManager>()로 조회해 캐시한다. Manager 조립 순서와 무관한
검증용 계층이나 interface는 추가하지 않는다.

### PinballGoal과 장치 Component

PinballGoal은 기존처럼 Start에서 App.Get<PinballManager>()를 사용해 등록한다.
입력과 Trigger를 Manager에 전달하고 VFX를 재생한다. PinballOutZone과
PinballLaunchCostDisplay의 기존 App.Get 사용도 유지한다.
PinballReflectorController의 전역 객체 검색만 App.Get<PinballManager>()로
교체한다. Launcher와 Magnet의 기존 Inspector 참조는 그대로 유지한다.

## 주요 데이터 흐름

### 초기화

1. PinballManager가 기존 App 서비스를 조회한다.
2. BattleRunCommon의 기존 비용 수치로 PinballLaunchState를 만든다.
3. 기존 pooledBalls로 PinballBallPool을 만든다.
4. PinballItemModifiers와 PinballGoalController를 만든다.
5. 기존 아이템 이벤트와 전투 상태 이벤트를 구독한다.
6. 공 풀을 준비하고 첫 공을 장전한다.
7. 기존 시점에 발사 비용 변경 이벤트를 발행한다.

공과 Goal은 Manager를 전달받지 않는다. 각 MonoBehaviour가 활성화 또는
Start 시점에 App.Get<PinballManager>()로 조회해 캐시한다.

### 공 발사

1. Launcher가 기존 TryLaunchLoadedBall을 호출한다.
2. 준비 단계, 장전 공과 골드 소비 가능 여부를 기존 순서로 확인한다.
3. PinballBallPool이 장전 공을 활성 공 상태로 옮긴다.
4. 기존 방향과 속도 계산, 발사 VFX와 SFX를 실행한다.
5. PinballLaunchState가 성공 횟수를 증가시킨다.
6. 비용 변경 이벤트와 EPinballState.Launched를 기존 순서로 발행한다.

### 충돌과 아이템 효과

작은 핀은 기존처럼 SFX와 작은 핀 충돌 횟수만 변경한다. 큰 범퍼는 기존 기본
Golden Ball 보상을 먼저 지급하고 Golden Bumper 추가 보상을 적용한다.
Split Capsule은 새 공을 만들지 않고 PinballBallPool에서 사용 가능 공을
가져와 활성화한다. Target Magnet, Charged Pin과 Overload Bumper의 조건 및
적용 횟수는 현재 공별 카운터를 그대로 사용한다.

### 골인

다음 순서를 변경하지 않는다.

1. Goal 데이터로 새 BattleUnitSpawnData를 만든다.
2. OnGoalReached를 먼저 발행한다.
3. Tutorial이 같은 데이터의 UnitId를 변경할 수 있는 현재 동작을 보존한다.
4. Charged Pin 공격 보너스를 계산한다.
5. 기본 아군을 소환한다.
6. Overload 조건을 충족하면 같은 데이터로 추가 소환한다.
7. 공을 풀에 반환한다.

### 공 반환과 웨이브 초기화

성공과 실패 모두 하나의 반환 경로를 사용한다. 마지막 활성 공이 반환될 때만
EPinballState.Idle을 발행하고 다음 공을 장전한다. 전투 상태가 Pending으로
돌아오면 성공 발사 횟수, 교환 횟수와 교환 대기 골을 초기화하고 골 폭과 비용
이벤트를 기존 시점에 갱신한다.

## 의존성 및 전역 접근

App.Get<T>()는 프로젝트의 고정 서비스 조회 방식이며 이번 설계에서 유지한다.
일반 C# 하위 객체는 App.Get, Find 계열 API나 Resources.Load를 호출하지 않는다.
MonoBehaviour는 사용자 결정에 따라 필요한 PinballManager를 App.Get으로
조회한다. PinballReflectorController의 FindFirstObjectByType는 제거한다.
SoundManager.PlaySFXIfAvailable과 VFX 리소스 로딩은 고정 프로젝트 구조와
기존 동작이므로 유지한다. 새로운 mutable static state는 추가하지 않는다.

## 객체 소유권과 수명주기

- Scene은 PinballManager, 공, Goal과 모든 장치 MonoBehaviour를 소유한다.
- PinballManager는 네 일반 C# 객체를 생성하고 자신의 수명 동안 소유한다.
- PinballBallPool은 공을 생성하거나 파괴하지 않고 상태 컬렉션만 소유한다.
- PinballGoalController는 Goal을 생성하거나 파괴하지 않고 등록 참조와 선택 상태만 소유한다.
- PinballManager가 아이템과 전투 상태 이벤트를 구독하고 OnDestroy에서 해제한다.
- 공은 기존 SetActive 기반으로 장전, 활성화와 반환된다.
- 플레이 중 반복적인 Instantiate 또는 Destroy는 새로 추가하지 않는다.

초기화 시 한 번 생성되는 Goal, OutZone, Magnet과 Launcher VFX는 반복 생성이
아니므로 이번 책임 분리 범위에서 유지한다. 비용 Text와 VFX 구조를 Scene에
새로 옮기는 작업은 플레이 동작과 직렬화 위험 대비 이득이 작아 수행하지 않는다.

## Unity 직렬화 보호

기존 MonoBehaviour 이름, namespace, 상속과 SerializeField 이름을 변경하지
않는다. PinballManager의 launchPosition, minimumLaunchSpeed,
maximumLaunchSpeed, launcherController와 pooledBalls를 유지한다.
새 하위 객체는 일반 C# 객체이므로 Scene이나 Prefab에 Component를 추가하지 않는다.
따라서 이번 설계에서는 Scene 또는 Prefab YAML 수정이 필요하지 않다.

현재 Scene 또는 다른 Asset에서 참조되지 않는 구형 Pinball.prefab에는 오래된
PinballManager 직렬화 필드가 남아 있지만 사용 근거가 없고 제거 영향도
검증하지 않으므로 이번 범위에서는 수정하거나 삭제하지 않는다.

## 오류 처리

기존 null 방어와 정상적인 실패 반환을 유지한다. 예외를 삼키거나 누락된 참조를
자동 검색으로 대체하지 않는다. 별도 validation 객체, OnValidate, assertion,
검사용 wrapper 또는 테스트 전용 분기를 추가하지 않는다.

## 파일 범위

### 새 파일

- Assets/02. Scripts/Pinball/PinballBallPool.cs
- Assets/02. Scripts/Pinball/PinballLaunchState.cs
- Assets/02. Scripts/Pinball/PinballItemModifiers.cs
- Assets/02. Scripts/Pinball/PinballGoalController.cs
- 각 C# 파일의 Unity meta 파일

### 수정 파일

- Assets/02. Scripts/Pinball/PinballManager.cs
- Assets/02. Scripts/Pinball/Pinball.cs
- Assets/02. Scripts/Pinball/PinballReflectorController.cs

### 의도적으로 수정하지 않는 파일

- Battle, Item, Tutorial과 UI 호출부
- Scene과 Prefab Asset
- Editor 및 기존 테스트 코드
- Core의 App, AppService와 Singleton<T>
- SoundManager와 Visual 시스템

## 검사 및 검증 제외

사용자의 명시적 지시에 따라 다음을 수행하지 않는다.

- 테스트 코드 또는 fixture 작성
- 기존 테스트, EditMode, PlayMode와 Unity Test Runner 실행
- dotnet test, dotnet build와 Unity Build 실행
- Unity Editor 또는 Test Scene 실행
- 정적 분석, 전체 validation과 coverage 측정
- 검사를 쉽게 만들기 위한 interface, wrapper, DI 또는 mock 경계 추가

구현 시에는 현재 소스의 타입, 메서드 signature와 호출 순서를 직접 읽어 기존
흐름과 논리적으로 일치하도록 수정한다. 이는 별도 검사 명령 실행을 의미하지 않는다.

## 완료 기준

- PinballManager의 이름, 상속, 공개 이벤트와 공개 API가 유지된다.
- 기존 비용, 보상, 골 선택과 교환, 분열, 자석, Charged Pin과 Overload 동작이 유지된다.
- 공 상태, 비용 상태, 아이템 수치와 골 선택 상태의 소유자가 각각 명확해진다.
- 일반 C# 하위 객체가 Manager, App, Scene 검색이나 Unity 생명주기에 의존하지 않는다.
- PinballReflectorController의 FindFirstObjectByType가 제거된다.
- 기존 SetActive 공 풀과 이벤트 순서가 유지된다.
- 새 interface, Manager, mutable static state와 반복 런타임 생성이 추가되지 않는다.
- .github/project-master-prompt.md의 3. 기존 프로젝트 구조가 변경되지 않는다.
- 테스트, 빌드, Unity 실행과 별도 validation을 수행하지 않는다.
