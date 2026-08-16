# 타이틀 씬과 새 런 초기화 설계

> 구현 상태: 완료  
> 설계 커밋: `c9a5326`  
> 구현 커밋: `ec6d97b`

## 목표

타이틀에서 Game 씬을 시작하거나 결과 화면에서 Title을 거쳐 다시 시작할 때 이전 런 상태가 남지 않도록 기존 서비스 초기화 API를 하나의 명시적인 흐름으로 연결한다. 새 전역 Manager는 추가하지 않는다.

## 현재 서비스 수명

- 지속 서비스: Developer 씬의 `SceneManager`, `ItemManager`, `TitleData`, `SoundManager`
- Game 씬 서비스: `BattleManager`, `UnitManager`, `PinballManager`

Game 씬 서비스는 Title 이동 시 파괴되고 Game 재진입 시 새 인스턴스가 생성된다. 현재도 다수 상태가 재생성으로 초기화되지만 각 `Start()`에 흩어져 있어 새 런 경계가 명시적이지 않다.

## 조정자 위치

`SceneManager`가 일반 C# 객체인 `GameRunController`를 소유한다. AppService나 Singleton을 추가하지 않는다. SceneManager는 Scene 전환 시점만 판단하고 실제 서비스 초기화 순서는 Controller에 위임한다.

## 초기화 순서

### Game 씬 로드 직전

`GameRunController.PrepareForSceneLoad()`가 지속 서비스인 `ItemManager.ResetRunState()`를 호출한다.

- 카탈로그 유지
- 시스템 구독자 유지
- 보유 수량과 활성 아이템 제거
- 대기 이벤트와 지연 코루틴 제거

### Game 씬 로드 직후

`LoadScene()`이 반환되면 새 Game 씬의 모든 `Awake()`와 App 등록이 완료된 상태다. `GameRunController.InitializeLoadedScene()`은 다음 순서로 호출한다.

1. `UnitManager.InitializeNewRun()`
2. `BattleManager.InitializeNewRun()`
3. `PinballManager.InitializeNewRun()`

Unit이 먼저 roster와 하위 Controller를 준비하고, Battle이 Unit roster 이벤트를 구독하며 새 전투 런을 만든다. Pinball은 준비된 Battle/Unit/Item 서비스를 참조해 공 풀과 런 상태를 만든다.

각 메서드는 중복 호출을 무시하는 idempotent 초기화다. Game 씬을 Editor에서 직접 실행해 SceneManager 흐름을 거치지 않는 경우 각 Manager의 `Start()`가 자신의 `InitializeNewRun()`을 fallback으로 호출한다.

## 서비스별 새 런 상태

### BattleManager

- 새 `BattleRunState`: 최대 HP, 첫 웨이브, Pending 상태
- 새 `BattleEconomy`: TitleData 시작 골드
- 방어 아이템 보정, 준비 잠금, 결과 대기 상태 초기화
- 이전 결과 코루틴 중단
- 초기 상태 이벤트 발행

### UnitManager

- 새 `UnitRoster`, TargetFinder, 전투 Context
- 소환·배치·합성·아이템 Controller 재생성
- 자동 포션 코루틴과 합성 예약 제거
- 보유/활성 유닛 목록이 비어 있는 상태로 시작
- 기존 Item 구독은 중복하지 않음

Game 씬 인스턴스가 새로 만들어지므로 이전 유닛 GameObject와 풀도 Scene 파괴로 제거된다.

### PinballManager

- 새 공 풀 구성 및 모든 공 비활성/재대기
- 발사 비용 증가 횟수와 보정 초기화
- 골 선택·교환 상태 및 원본 데이터 복원
- 아이템 보정 초기화 후 현재 Item 구독 재적용
- 첫 공 장전

## Tutorial 완료 상태

`Tutorial.Completed` PlayerPrefs는 계정/설치 단위 진행 상태로 취급해 새 런 초기화 대상에서 제외한다. 완료된 튜토리얼은 다음 런에서도 유지되고, 미완료 상태는 Game 씬의 새 TutorialManager가 첫 단계부터 시작한다.

## 기존 API 의미

- `ItemManager.ResetRunState()`: 새 런용, 카탈로그·구독자 유지
- `ItemManager.Clear()`: 서비스 종료용, 구독자까지 제거
- `BattleManager.InitializeNewRun()`, `UnitManager.InitializeNewRun()`, `PinballManager.InitializeNewRun()`: Game 씬 새 인스턴스 구성용 내부 API

## 보존 사항

- 기존 App/AppService 등록 구조
- Title과 Result 화면의 기존 `SceneManager.Load()` 호출
- Game 씬 직접 실행 지원
- 기존 시작 HP·골드·웨이브·핀볼 비용 데이터
- 튜토리얼 완료 상태 정책
- 새 전역 Manager 없음

## 확인 범위

사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석은 수행하지 않는다. Scene 로드 전후 호출 순서, idempotent guard, 서비스 의존 관계와 구독 해제를 코드 읽기로만 확인한다.
