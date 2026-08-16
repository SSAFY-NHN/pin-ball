# 핀볼 후속 리팩터링 M2 설계

> 구현 상태: 완료  
> 설계 커밋: `a8bc540`  
> 구현 커밋: `95ba6fb`

## 배경

1차 핀볼 리팩터링으로 `PinballBallPool`, `PinballLaunchState`,
`PinballGoalController`, `PinballItemModifiers`가 분리되었다. M2는 기존 게임 규칙과
공개 API를 유지하면서 `PinballManager`에 남은 보상 조율을 분리하고, 핀볼 UI와 VFX의
런타임 오브젝트 생성을 Scene 사전 배치와 Inspector 참조로 전환한다.

## 고정 제약

- `.github/project-master-prompt.md`의 `3. 기존 프로젝트 구조`를 변경하지 않는다.
- `PinballManager : AppService`, `App.Get<PinballManager>()` 사용을 유지한다.
- 공은 Scene 사전 배치 목록과 SetActive 기반 `PinballBallPool`로 관리한다.
- 기존 공개 메서드와 이벤트, 아이템 수치, 보상 수치, 효과 재생 수치를 유지한다.
- 새로운 최상위 폴더, 외부 패키지, interface, Service를 추가하지 않는다.
- 테스트 코드 작성, 테스트·빌드·Unity Editor·정적 분석·별도 validation을 수행하지 않는다.

## 목표

- 충돌 보상, 분열, Goal 유닛 소환 적용을 하나의 일반 C# Controller로 분리한다.
- `PinballManager`는 생명주기, 시스템 연결, 공 흐름, 이벤트 조율만 담당한다.
- 비용 TMP와 Goal·OutZone·Magnet·Launcher VFX를 Scene이 소유하게 한다.
- 런타임 `new GameObject`, `AddComponent`, `new Material`, Material `Destroy`를 제거한다.
- 새 런의 핀볼 상태 초기화 순서를 명시적인 내부 API에 모은다.
- 사용되지 않는 구형 `Pinball.prefab`을 제거한다.

## 비목표

- `Pinball`의 물리, 충돌 판정, 발사 감각 변경
- 아이템 데이터 또는 보상 밸런스 변경
- Scene 재로드 없는 재시작 기능 추가
- 핀볼 외 전투·아이템·튜토리얼 구조 변경
- VFX 디자인, Sprite, 재생 시간, 색상 변경
- 새 공 생성 방식 또는 Instantiate 기반 풀 확장

## 책임 구조

### PinballManager

다음 책임을 유지한다.

- Unity 생명주기와 App 서비스 연결
- 아이템 이벤트 구독과 해제
- 공 발사, 회수, 다음 공 로드
- Goal 등록·선택 공개 façade
- `OnStateChanged`, `OnLaunchCostChanged`, `OnGoalReached` 발행
- 새 런과 웨이브 준비 초기화 순서 조율
- SFX와 공 피드백 호출

보상 수치 계산, 분열 공 활성화, 유닛 소환 반복은 직접 처리하지 않는다.

### PinballRewardController

새 일반 C# 객체이며 다음만 담당한다.

- Big Bumper 기본 골드 지급
- Golden Bumper 누적 보상 계산과 지급
- Split Capsule 조건 확인과 복제 공 활성화
- Charged Pin 임시 공격력 보정 계산
- Goal 기본 유닛과 Overload 추가 유닛 소환

생성자에서 `BattleManager`, `UnitManager`, `PinballBallPool`,
`PinballItemModifiers`를 명시적으로 전달받는다. App 조회, SFX, 카메라 피드백,
Manager 이벤트, 공 회수는 수행하지 않는다.

충돌 처리 결과로 실제 총 골드 보상을 반환해 Manager가 기존 공 피드백을 재생할 수 있게
한다. Goal 처리는 기존과 같은 순서로 기본 유닛을 먼저 소환하고 Overload 조건을 확인해
추가 소환한다.

### 기존 상태 객체

- `PinballManager`가 `PinballBallPool`, `PinballLaunchState`,
  `PinballGoalController`, `PinballItemModifiers`, `PinballRewardController`를 생성하고 소유한다.
- Scene이 Pinball, Goal, OutZone, Magnet, Launcher, 비용 TMP, VFX GameObject를 소유한다.
- `PinballRewardController`는 전달받은 객체를 사용하지만 수명은 소유하지 않는다.

## 보상 데이터 흐름

### Big Bumper 충돌

1. `Pinball`이 기존 경로로 `PinballManager.OnBallHit`을 호출한다.
2. Small Pin은 기존처럼 Manager가 SFX와 충돌 횟수만 처리한다.
3. Big Bumper는 Manager가 SFX와 충돌 횟수를 처리한다.
4. Manager가 `PinballRewardController.ApplyBumperReward`를 호출한다.
5. Controller가 기본 골드와 Golden Bumper 추가 골드를 지급하고 총액을 반환한다.
6. Controller가 Split Capsule 조건에 맞으면 풀에서 복제 공을 획득해 활성화한다.
7. Manager가 반환된 총액으로 기존 골드 피드백을 재생한다.

### Goal 도달

1. Manager가 Goal의 `BattleUnitSpawnData` 복사본을 만들고 `OnGoalReached`를 발행한다.
2. Manager가 Controller에 공과 유닛 데이터를 전달한다.
3. Controller가 Charged Pin 보정을 계산하고 기본 유닛을 소환한다.
4. Overload 조건을 만족하면 기존 횟수만큼 추가 유닛을 소환한다.
5. Controller가 공의 Overload 사용 횟수를 갱신한다.
6. Manager가 기존 경로로 공을 풀에 반환한다.

## Scene 사전 배치

### 발사 비용 TMP

`PinballLaunchCostDisplay`는 `[SerializeField] private TextMeshPro costText`를 사용한다.
Game Scene Launcher 하위에 기존 런타임 값과 같은 위치·회전·크기·텍스트 스타일을 가진
`LaunchCost`를 배치한다. `Transform.Find`, `new GameObject`, `AddComponent`와 스타일
구성 코드를 제거하고 텍스트와 가용 색상 갱신만 남긴다.

필수 참조가 없으면 오류를 남기고 표시 컴포넌트만 비활성화한다.

### Goal VFX

각 Goal 하위에 다음 `ArcaneSpriteEffect` 자식을 미리 배치하고 Inspector로 연결한다.

- Goal Rune Burst
- Goal Absorption Ring
- Goal Burst Ring
- Goal Arc Top Left
- Goal Arc Top Right
- Goal Arc Bottom Left
- Goal Arc Bottom Right
- Goal Spark

기존 `ArcaneVfxCatalog` Sprite 배열과 sorting order로 기존 `Initialize`를 호출하되,
GameObject, SpriteRenderer, Material을 생성하지 않는다.

### OutZone VFX

각 OutZone 하위에 `Out Zone Ring`, `Out Zone Impact`를 미리 배치하고 연결한다.
기존 재생 위치, 크기, 색상, 시간과 sorting order를 유지한다.

### Magnet VFX

각 Magnet 하위에 `Magnet Arc`, `Magnet Spark`를 미리 배치하고 연결한다.
자석 물리와 `FixedUpdate` 동작은 변경하지 않는다.

### Launcher Glow

Launcher 스프링 하위에 `Plunger Spring Glow`와 SpriteRenderer를 미리 배치한다.
`InitializeSpring(SpriteRenderer)` 형태는 유지하고 원본 Sprite와 sorting 정보만 동기화한다.
런타임 GameObject, SpriteRenderer, Material 생성은 제거한다.

### Material

기존 `Assets/09. Materials/Pinball` 아래에 Goal, OutZone, Magnet, Launcher용 Material
asset을 각각 둔다. 기존 런타임 Material의 shader, intensity, glow spread를 보존한다.
Scene의 VFX SpriteRenderer가 해당 Material을 참조하며 스크립트는 serialized Material을
초기화에 전달한다. 런타임 `Resources.Load<Shader>`, `new Material`, `Destroy`를 제거한다.

VFX 참조나 Catalog가 누락되면 gameplay는 유지하고 해당 효과만 생략한다. 누락을 대신하는
런타임 생성 fallback은 두지 않는다.

## 새 런 초기화

`PinballManager`에 `internal void ResetForNewRun()`을 추가한다. 외부 public API로 열지 않고
최초 `Start()`의 상태 구성에도 같은 초기화 경로를 사용한다. Scene 재로드 없는 재시작
기능이 생길 때만 호출 범위 확장을 다시 검토한다.

초기화 범위는 다음과 같다.

- 모든 공을 `Deactivate()`하고 available, active, loaded 풀 상태 재구성
- 성공한 발사 횟수와 발사 비용 아이템 보정 초기화
- 선택 Goal을 첫 Goal로 복원
- 교환 대기와 남은 교환 횟수 초기화
- 교환으로 변경된 Goal의 최초 `BattleUnitSpawnData` 복원
- Focused Pocket 폭 보정 초기화
- 모든 `PinballItemModifiers` 값을 기본값으로 복원
- 다음 공 로드와 비용 변경 이벤트 발행

`PinballBallPool`은 최초 전달된 Scene 공 목록을 보관하고 `ResetForNewRun()`에서 모든 공을
한 번씩 비활성화한 뒤 available queue를 다시 만든다.

`PinballGoalController.Register()`는 각 Goal의 최초 유닛 데이터를 복사해 보관한다.
Manager와 Goal의 `Start()` 실행 순서에 의존하지 않으며, 미래의 새 런 초기화에서는 이
복사본을 사용해 교환 전 데이터를 복원한다.

기존 웨이브 Pending 진입 시 `ResetForPreparation()`은 현재처럼 성공 발사 횟수와 교환
횟수를 초기화하고 다음 공을 준비한다. 아이템 보정과 현재 런의 Goal 데이터는 웨이브 사이에
유지한다.

## 구형 Prefab 정리

`Assets/04. Prefabs/Pinball.prefab`은 GUID 참조가 프로젝트 내에 없고 현재 Game Scene보다
오래된 Manager, Goal, OutZone의 독립 복사본이다. prefab과 `.meta`를 삭제하며 Game Scene을
새 prefab으로 만들지 않는다.

## 오류 처리

- null 공, Goal 또는 잘못된 풀 반환은 기존처럼 작업을 생략한다.
- 필수 비용 TMP가 없으면 명시적 오류 후 해당 표시만 중단한다.
- 선택 VFX 참조가 없으면 gameplay를 중단하지 않고 해당 효과만 생략한다.
- Scene 연결 오류를 숨기는 Find, AddComponent, 런타임 생성 fallback을 추가하지 않는다.
- 예외를 무시하거나 관련 없는 시스템에서 대체 참조를 찾지 않는다.

## 파일 범위

### 추가

- `Assets/02. Scripts/Pinball/PinballRewardController.cs`
- 대응 Unity `.meta`
- `Assets/09. Materials/Pinball` 아래 VFX Material 4개와 대응 `.meta`

### 수정

- `Assets/02. Scripts/Pinball/PinballManager.cs`
- `Assets/02. Scripts/Pinball/PinballLaunchCostDisplay.cs`
- `Assets/02. Scripts/Pinball/PinballGoal.cs`
- `Assets/02. Scripts/Pinball/PinballOutZone.cs`
- `Assets/02. Scripts/Pinball/PinballMagnetController.cs`
- `Assets/02. Scripts/Pinball/PinballLauncherGlowController.cs`
- `Assets/02. Scripts/Pinball/PinballBallPool.cs`
- `Assets/02. Scripts/Pinball/PinballLaunchState.cs`
- `Assets/02. Scripts/Pinball/PinballGoalController.cs`
- `Assets/02. Scripts/Pinball/PinballItemModifiers.cs`
- `Assets/01. Scenes/02. Game.unity`
- `docs/ai-usage/2026-08-16/2026-08-16-pinball-followup-refactor-ai-usage.md`

### 삭제

- `Assets/04. Prefabs/Pinball.prefab`
- `Assets/04. Prefabs/Pinball.prefab.meta`

### 변경하지 않음

- `App`, `AppService`, `Singleton<T>`
- Pinball 물리와 공 입력
- 아이템 데이터와 전투 데이터
- 전투·아이템·튜토리얼 시스템
- 테스트와 Editor 코드

## 완료 기준

- `PinballManager`가 충돌 보상 계산, 분열 반복, Overload 소환 반복을 직접 수행하지 않는다.
- `PinballRewardController`의 책임이 핀볼 결과 보상 적용 하나로 제한된다.
- 비용 TMP와 지정한 모든 VFX GameObject가 Game Scene에 사전 배치되어 있다.
- 대상 핀볼 스크립트에서 VFX/UI 목적의 `new GameObject`, `AddComponent`, `new Material`,
  Material `Destroy`가 제거된다.
- 새 런 내부 초기화 API가 공 풀, 발사, Goal, 교환, 아이템 보정을 모두 초기화한다.
- 웨이브별 기존 초기화 동작과 핀볼 공개 API가 유지된다.
- 구형 `Pinball.prefab`이 제거된다.
- 테스트·빌드·Unity Editor·정적 분석·별도 validation을 실행하지 않는다.
