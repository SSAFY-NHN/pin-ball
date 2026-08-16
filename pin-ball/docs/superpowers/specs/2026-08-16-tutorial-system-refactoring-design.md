# 튜토리얼 시스템 리팩터링 설계

## 목표

`TutorialManager`는 튜토리얼 수명과 게임 이벤트 연결을 조율하고, 진행 규칙·UI 표시·입력 제한·튜토리얼 전용 게임 규칙을 각각 분리한다. 기존 단계 순서와 사용자 경험은 유지한다.

## 책임 분리

### TutorialProgress

현재 클래스가 단계와 전이 규칙만 계속 소유한다. Unity UI나 게임 Manager를 참조하지 않는다.

### TutorialUIController

Overlay, 안내 문구, Continue 버튼의 표시 상태를 담당한다. 메시지 단계와 행동 단계의 차이는 Continue 버튼 활성 여부로 표현한다. 완료 시 Overlay를 숨긴다.

### TutorialInteractionController

Shop, Items, Wave Start 버튼 중 현재 허용된 버튼만 interactable로 설정한다. `TutorialFocusIndicator`의 포커스 대상, 입력 차단, 숨김을 함께 조율한다. 완료 시 입력 차단과 포커스를 해제한다.

### TutorialGameRuleController

튜토리얼 시작 골드를 `현재 발사 비용 × 3`으로 지급한다. 첫 발사 결과의 UnitId를 기억하고 두 번째 발사 결과에 같은 UnitId를 적용한다. 이 규칙은 일반 핀볼/전투 코드로 이동하지 않는다.

### TutorialManager

Scene 참조와 App 서비스를 Controller에 전달하고, 버튼 및 게임 이벤트를 구독하며, 이벤트에 따라 `TutorialProgress`를 전이한 뒤 현재 단계를 표시한다. 단계별 메시지와 대상 선택은 현 Manager에 남겨 전체 튜토리얼 흐름을 한 곳에서 읽을 수 있게 한다.

## Scene 참조

`ShopPanel`과 `WavePanel`을 `[SerializeField]`로 추가하고 Game 씬의 기존 컴포넌트를 직접 연결한다. `FindFirstObjectByType` 호출은 제거한다. 기존 BottomTabPanel과 버튼 참조 방식은 유지한다.

## 최대 시간

`Update()` 폴링을 제거하고 튜토리얼 시작 시 `WaitForSecondsRealtime(maximumDuration)` 코루틴 하나를 시작한다. 시간이 지나기 전에 완료되면 Manager를 비활성화하면서 코루틴을 중단한다.

## 완료 상태와 새 런

Editor 실행마다 `Tutorial.Completed`를 삭제하는 전처리 코드를 제거한다. 완료 상태는 PlayerPrefs에 저장되어 Editor와 빌드에서 동일하게 유지된다.

- 완료된 튜토리얼은 새 런에서도 다시 시작하지 않는다.
- 미완료 상태에서 Game 씬을 다시 로드하면 진행은 첫 단계부터 시작한다.
- ItemManager 등 새 런 상태 초기화는 튜토리얼 완료 PlayerPrefs를 지우지 않는다.
- 튜토리얼을 다시 확인하려면 개발자가 PlayerPrefs 키를 명시적으로 삭제한다.

## 보존 사항

- 단계 순서와 전이 조건
- 안내 문구와 포커스 대상
- Personal Healing Potion 구매 제한
- 시작 골드 수치
- 첫 두 소환의 동일 UnitId 규칙
- 완료 시 Shop/Wave UI 갱신과 PlayerPrefs 저장

## 확인 범위

사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석은 수행하지 않는다. 이벤트 구독/해제, 단계별 Controller 호출, Scene 직렬화 참조를 코드 읽기로만 확인한다.
