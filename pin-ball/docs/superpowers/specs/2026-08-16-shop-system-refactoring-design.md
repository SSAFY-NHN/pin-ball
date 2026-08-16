# 상점 시스템 리팩터링 설계

## 목표

`ShopPanel`에서 상품 규칙과 리롤 결제를 분리하고, Panel은 Scene에 배치된 슬롯 표시와 UI 이벤트 처리에 집중한다. 기존 구매 API와 플레이 동작은 유지한다.

## 구성 요소

### ShopOfferController

`ItemManager.GetItems()`로 전체 카탈로그를 받아 후보 목록을 만든다. null과 구매 한도에 도달한 일반 아이템을 제외하되, 기존 동작대로 두 회복 포션은 후보에 유지한다. 표시 슬롯 수만큼 Fisher-Yates 방식의 기존 부분 셔플을 수행한다.

최초 상점 표시와 웨이브 Pending 갱신에서는 Party Healing Potion을 0번, Personal Healing Potion을 1번 슬롯에 고정한다. 수동 리롤에서는 포션을 보장하지 않는다.

### ShopRerollController

`BattleManager.CanUsePreparationActions`를 확인하고, 유료 리롤이면 `TrySpendPreparationGold()`로 비용을 결제한다. 성공한 경우에만 `ShopOfferController`에 새 상품 생성을 요청한다. 초기/웨이브 상품 갱신은 결제 없이 별도 메서드로 수행한다.

### ShopPurchasePolicyController

일반 구매 가능 여부는 `ItemManager.CanPurchase()`에 위임한다. 튜토리얼이 특정 상품만 허용하는 동안에는 일반 규칙 위에 일시 제한을 추가한다. `ShopPanel.SetTutorialPurchaseRestriction()` 공개 API는 유지하되 정책 Controller에 값을 전달한다. 제한 해제 후에는 일반 상점 규칙만 적용한다.

### ShopPanel

`[SerializeField] private ShopSlot[] itemSlots`로 Scene 배치 슬롯을 명시적으로 참조한다. `GetComponentsInChildren<ShopSlot>()` 탐색은 제거한다. Panel은 다음만 담당한다.

- 버튼 및 BattleManager 이벤트 연결
- Controller 호출
- 생성된 상품을 슬롯에 표시
- 골드, 준비 단계, 구매 정책에 따른 슬롯 interactable 표시
- 구매 버튼에서 기존 `ItemManager.TryPurchase()` 호출

## Scene 참조

Game 씬의 기존 ShopSlot 세 개를 배열 순서대로 Inspector 참조에 저장한다. 배열 순서가 상품 표시 순서이며, 포션 보장 슬롯 인덱스도 이 순서를 따른다. 슬롯 누락은 기존 경고 흐름을 유지하고 null 슬롯은 건너뛴다.

## 보존 사항

- `ItemManager.TryPurchase()` 구매 API
- 현재 리롤 비용과 준비 단계 제한
- 최초/웨이브 갱신의 포션 보장과 수동 리롤의 무보장
- 구매 후 슬롯 상태 갱신
- 튜토리얼의 Personal Healing Potion 단일 구매 제한
- 기존 ShopSlot 표시 및 Tooltip 동작

## 확인 범위

사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석은 수행하지 않는다. Controller 호출 순서, 기존 조건식, Game 씬 슬롯 참조를 코드와 직렬화 원문으로만 확인한다.
