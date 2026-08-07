# 전투·스킬 시스템 안정화 설계

작성일: 2026-08-07  
대상 브랜치: `feature/pinball`  
기준 커밋: `e976c2b`

## 1. 목표

`dev` 머지 후 추가된 아군·적군 스킬을 데이터 테이블 형식과 분리하고, 전투 상태 전환·유닛 합성·승급·결과 UI 및 프로젝트 정합성 문제를 함께 해결한다.

완료 기준은 다음과 같다.

- 아군 12종, 적 11종의 현재 스킬 23개가 스킬 ID별 C# 분기 없이 동작한다.
- 향후 데이터 테이블 형식이 변경되어도 스킬 실행부는 유지되고 Adapter만 교체할 수 있다.
- 아군 전멸 후 플레이어 HP가 남으면 생존 적을 제거하고 `Pending`으로 전환한다.
- 핀볼·상점·배치·합성은 `Pending`에서만 가능하고, `Active`에서는 자동 전투만 진행한다.
- 같은 직업·같은 레벨 유닛 두 개를 플레이어가 직접 드래그해 합성한다.
- 5레벨 도달 즉시 승급 선택 UI를 표시한다.
- 전투 결과 UI에서 Title 씬으로 이동할 수 있다.
- 프로젝트 코드의 코루틴을 UniTask로 교체한다.
- `ItemManager` 외 Manager/UI 상태 이벤트는 UniRx로 전달한다.
- Unity 6000.0.79f1과 URP 패키지 버전을 정합하게 맞춘다.

## 2. 범위와 제외 범위

### 포함

- 노드 기반 스킬 런타임과 현재 임시 데이터용 V1 Adapter
- 기존 23개 스킬 데이터의 노드 그래프 이관
- 전투 상태별 입력 및 유닛 갱신 제한
- 패배 웨이브 종료 시 생존 적 정리
- 드래그 합성 및 5레벨 승급 선택
- 승리·패배 결과 패널과 Title 씬 이동 버튼
- Manager/UI 상태 스트림의 UniRx 전환
- 프로젝트 코드 코루틴의 UniTask 전환
- UI 초기 상태 누락 및 구독 수명 문제 수정
- 명시 위치 적 소환에 진형 오프셋이 중복되는 문제 수정
- Developer 씬명 오타 수정
- URP 17.0.4 정합성 복구
- 승인된 미사용 프리팹과 메타 파일 삭제
- 자동화 테스트와 Unity 실행 검증

### 제외

- 확정 웨이브 데이터 테이블, 로더 및 신규 웨이브 콘텐츠
- 아이템 아이콘 대체 UI
- Title 씬의 시작 버튼 및 현재 자동 Game 이동 로직
- 비주얼 노드 에디터
- 외부 패키지 신규 설치

현재 `BattleManager.waveList`와 레거시 ID 정규화 코드는 웨이브 테이블 구현 전까지 임시 코드로 유지한다.

## 3. 검토한 접근 방식

### A. 범용 데이터 실행기

고정된 `triggerType`, `effectType`, `targetType` 필드를 실행한다. 구현량은 작지만 확정되지 않은 테이블 필드와 실행기가 다시 결합될 가능성이 있다.

### B. 노드 기반 스킬 런타임 — 선택

원본 데이터를 Adapter가 런타임 `SkillGraph`로 변환한다. 실행기는 원본 JSON을 모르고 이벤트·조건·대상·실행 노드만 처리한다. 데이터 테이블이 변경되면 Adapter만 교체할 수 있다.

### C. 스킬별 전략 클래스

현재 분기문을 별도 클래스로 옮길 수 있지만, 새 스킬마다 C# 구현이 필요하므로 데이터 결합 문제를 해결하지 못한다.

데이터 테이블이 임시라는 사용자 요구를 반영해 B를 선택한다. 프로토타입 범위를 지키기 위해 비순환 그래프와 명시적 노드 등록만 지원하고 비주얼 편집기는 만들지 않는다.

## 4. 스킬 아키텍처

```text
AllyUnitData / EnemyUnitData
        ↓
현재 테이블용 V1 Adapter
        ↓
SkillGraph
        ↓
SkillRuntimeController
        ↓
UnitBase / UnitManager 전투 API
```

### 4.1 노드 종류

- 이벤트: 전투 시작, 마나 충전, 기본 공격 적중, 피격 전·후, 체력 임계치, 일정 주기
- 조건: N번째 공격, 동일 대상, 정면 피격, 전투 이탈 시간, 최초 1회, 중첩 수
- 대상: 자신, 현재 대상, 원형·직선 범위, 최고 체력, 최장 거리, 모든 아군·적군
- 실행: 피해, 회복, 보호막, 능력치 변경, 기절, 넉백, 도발, 순간이동, 소환, 중첩 변경

각 그래프는 이벤트 진입점에서 시작하며 조건과 대상 선택을 거쳐 실행 노드를 순서대로 처리한다. 그래프 연결은 순환을 허용하지 않는다.

### 4.2 런타임 상태

`SkillRuntimeController`는 스킬별 Blackboard를 보유한다. 공격 횟수, 동일 대상 중첩, 재사용 대기시간, 1회 발동 여부와 같은 상태를 이곳에 저장한다. 현재 `AllyUnit`과 `EnemyUnit`에 존재하는 스킬 ID 분기와 스킬 전용 상태 필드는 제거한다.

클래스 책임은 다음과 같다.

- `AllyUnit`, `EnemyUnit`: 진영별 이동·기본 공격·마나 규칙
- `SkillRuntimeController`: 그래프 실행과 Blackboard 관리
- `UnitBase`: 피해·회복·상태 효과의 기본 전투 API
- `UnitManager`: 대상 탐색과 소환
- V1 Adapter: 현재 임시 JSON을 `SkillGraph`로 변환

### 4.3 데이터 검증

Title 데이터 로딩 시 모든 그래프를 검증한다.

- 알 수 없는 노드 종류
- 존재하지 않는 다음 노드
- 필수 값 누락 또는 허용 범위 위반
- 순환 연결
- 존재하지 않는 유닛·소환 대상·승급 후보

오류에는 유닛 ID, 스킬 ID, 노드 ID를 포함한다. 잘못된 그래프는 해당 스킬만 비활성화하고 유닛 기본 공격은 유지한다. 잘못된 값을 조용히 `0`으로 대체하지 않는다.

## 5. 전투 상태 설계

`BattleManager`가 전투 상태의 단일 기준이다.

### Pending

- 핀볼 발사와 목표 선택 가능
- 상점 구매 가능
- 아군 드래그·합성·승급 가능
- 유닛 이동, 공격, 스킬 및 상태 지속시간 정지
- 활성 핀볼이 있거나 승급 선택이 미완료이면 웨이브 시작 불가

### Active

- 유닛 전투와 스킬 그래프만 실행
- 핀볼 발사, 상점 구매, 목표 선택, 드래그 합성 차단
- UI 상태뿐 아니라 각 Manager의 공개 진입점에서도 상태를 검사한다.

### 아군 전멸

```text
생존 적의 총 돌파 피해 계산
→ 생존 적 전부 제거
→ 플레이어 HP 감소
→ HP가 남으면 Pending
→ HP가 0이면 Defeat
```

돌파 피해는 적을 제거하기 전에 한 번만 계산한다. `Pending`으로 전환된 뒤 생존 적이 새 아군을 공격하거나 패시브를 실행할 수 없도록 한다.

### 적 전멸

임시 다음 웨이브가 있으면 보상 지급 후 `Pending`, 마지막 웨이브라면 `Victory`로 전환한다. 웨이브 데이터 구조는 변경하지 않는다.

## 6. 드래그 합성과 승급

합성은 `Pending`에서 플레이어 드래그로만 실행한다.

```text
유닛 드래그
→ 다른 유닛 위에서 놓기
→ 같은 UnitId + 같은 Level 확인
→ 드래그한 유닛 소모
→ 대상 유닛 Level + 1
```

- 유효하지 않은 드롭은 원래 위치로 복귀한다.
- 여러 후보가 겹치면 가장 가까운 유효 대상 하나만 사용한다.
- 결과 유닛은 드롭 대상 위치를 유지한다.
- 성장 단계는 `Level`로 관리하며 `BattleUnitModifier`는 보존한다.
- 합성 전 체력·마나 비율을 보존하고 아이템 능력치는 중복 없이 재계산한다.
- 승급 후에도 같은 직업·같은 레벨끼리 합성하며 `maxLevel` 10을 상한으로 사용한다.

레벨 5 도달 시 해당 유닛을 잠그고 취소할 수 없는 `PromotionPanel`을 표시한다. `previousJob`이 현재 `UnitId`인 후보 중 하나를 선택하면 동일 GameObject의 직업 데이터만 교체하고 레벨, 위치, 체력·마나 비율을 유지한다. 후보가 없으면 재료 소모 전 합성을 차단한다.

## 7. UI 설계

`PromotionPanel`과 `BattleResultPanel`은 `02. Game` 씬의 `UIManager` 아래에 비활성 상태로 미리 배치하고 Inspector로 참조한다.

- `PromotionPanel`: 승급 후보, 직업명과 설명
- `BattleResultPanel`: Victory/Defeat 문구와 `타이틀로` 버튼
- 결과 패널은 ESC로 닫을 수 없는 최상위 모달
- 타이틀 버튼은 프로젝트 `SceneManager.Load(ESceneName.Title)` 사용

Title 씬의 현재 자동 Game 이동 로직은 수정하지 않는다. 사용자가 Title 기능을 구현하기 전에는 결과 버튼으로 이동한 직후 다시 Game으로 넘어갈 수 있음을 제한점으로 기록한다.

## 8. UniRx 상태 모델

`ItemManager`의 전용 구독 시스템은 유지한다. 그 외 Manager/UI 간 지속 상태 알림은 UniRx로 전환한다.

- `BattleManager`: 전투 상태, 웨이브, 플레이어 HP, 골드
- `PinballManager`: Idle/Launched 상태

Manager 내부는 `ReactiveProperty<T>`를 보유하고 외부에는 `IReadOnlyReactiveProperty<T>`만 노출한다. UI와 하위 시스템은 `Subscribe(...).AddTo(this)` 또는 명시적 `CompositeDisposable`로 수명을 관리한다.

`ReactiveProperty`가 구독 시 현재 값을 전달하므로 별도의 초기 이벤트 재발행에 의존하지 않는다. `WavePanel`은 전투 상태와 핀볼 상태를 `CombineLatest`로 조합해 시작 버튼의 `interactable`을 결정한다.

Unity Button 리스너와 `ShopSlot`의 단발성 로컬 콜백은 그대로 유지한다. 프레임마다 발생하는 스킬 내부 호출은 불필요한 스트림 할당 없이 런타임에 직접 전달한다.

## 9. UniTask 전환

프로젝트 소유 코드에서 확인된 코루틴 세 개를 모두 UniTask로 교체한다.

- `ItemManager` 지연 아이템 이벤트
- `UnitBase` 지속 피해
- `UnitBase` 지연 감속

`UniTask.Delay`로 기존 `WaitForSeconds`의 시간 배율 동작을 유지한다. 오브젝트 파괴, Manager 초기화 또는 효과 재적용 시 CancellationToken과 기존 버전 값으로 이전 작업을 종료한다. 취소는 정상 흐름으로 처리하고 예상하지 못한 예외만 기록한다.

전환 후 `Assets/02. Scripts`에는 `IEnumerator`, `StartCoroutine`, `yield return`, `WaitForSeconds`를 남기지 않는다. UniRx 플러그인 내부와 예제는 수정하지 않는다.

## 10. 기타 결함 수정

- 명시 위치로 소환하는 적 증원에는 전역 진형 Y 오프셋을 다시 적용하지 않는다.
- Developer 씬명을 실제 파일명인 `00. Developer`로 수정한다.
- Unity 6000.0.79f1 기준으로 URP manifest와 lock을 17.0.4에 맞춘다.
- Unity 패키지 마이그레이션으로 `UniversalRenderPipelineGlobalSettings` 호환성을 복구한다.
- 다음 미사용 프리팹과 메타 파일을 삭제한다.
  - `Assets/04. Prefabs/UI.prefab`
  - `Assets/04. Prefabs/UI.prefab.meta`
  - `Assets/04. Prefabs/Pinball.prefab`
  - `Assets/04. Prefabs/Pinball.prefab.meta`
- Ball, SmallPin, BigBumper 개별 프리팹은 유지한다.

## 11. 테스트와 검증

- 현재 아군 12종·적 11종의 23개 스킬 그래프 로딩 및 검증
- 액티브, 패시브, 조건부, 범위, 소환 스킬 대표 실행
- `AllyUnit`과 `EnemyUnit`에 스킬 ID별 실행 분기가 남지 않았는지 확인
- `Pending`/`Active` 입력 차단과 활성 핀볼 시작 차단
- 아군 전멸 시 돌파 피해 계산, 적 제거, HP별 상태 전환
- 같은 직업·같은 레벨 합성, 실패 복귀, 5레벨 승급, 10레벨 상한
- UniRx 초기값 전달과 구독 수명
- UniTask 지연 실행, 취소 및 효과 재적용
- 명시 위치 소환과 씬명 매핑
- Unity 스크립트 컴파일과 EditMode 테스트
- Game 씬 실행 스모크 테스트
- 가능한 환경에서 PC WebGL 빌드

테스트는 기존 Unity Test Framework를 사용하고 외부 테스트 패키지는 추가하지 않는다.

## 12. 작업 종료 보고

완료 보고에는 다음을 포함한다.

- 해결한 문제 목록
- 변경·삭제 파일과 변경 이유
- 자동화 테스트, Unity 실행 및 빌드 결과
- 사용자가 직접 확인할 절차
- 제외 범위와 남은 제한점
- 사용한 AI 도구/모델, 사용자 요청, AI 제안·수정 영역, 사용자 결정 영역과 중요 지시
