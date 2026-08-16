# ItemManager 후속 리팩터링 설계

## 목표

`ItemManager`의 공개 API와 기존 아이템 동작을 유지하면서 구매 조율, 이벤트 큐 예약, 새 런 초기화의 책임을 명확히 분리한다.

## 구매 책임

`ItemPurchaseController`를 `ItemManager` 하위 보조 객체로 추가한다. Controller는 다음 순서를 한 메서드에서 조율한다.

1. null 및 인벤토리 구매 제한 확인
2. `BattleManager.TrySpendPreparationGold()` 결제
3. `ItemManager.Raise()` 경로를 통한 인벤토리 획득과 아이템 이벤트 예약
4. `OnItemPurchased` 통지
5. 구매 SFX 재생

`ItemManager.TryPurchase()`는 기존 공개 API를 유지하고 Controller에 필요한 획득·구매 통지 동작을 전달하는 façade가 된다. 결제 실패 시 획득, 이벤트, SFX는 발생하지 않는다.

## 이벤트 큐 예약

매 프레임 호출되는 `ItemManager.Update()`를 제거한다. `Raise()`가 이벤트를 enqueue한 직후, 아직 예약된 처리가 없을 때만 코루틴 하나를 시작한다.

코루틴은 `yield return null`로 다음 프레임까지 기다린 뒤 대기 큐 전체를 처리한다. 같은 프레임에 여러 이벤트가 들어와도 코루틴은 하나만 존재한다. 기존 `RaiseImmediate()`는 즉시 전달하며 큐 예약을 사용하지 않는다. 공개 `DispatchQueuedEvents()`를 직접 호출하면 예약 코루틴을 취소하고 즉시 큐를 처리한다.

## 초기화와 종료

- `ResetRunState()`: 인벤토리와 대기 이벤트를 비우고 지연/예약 코루틴을 중단한다. 시스템 구독자는 유지한다.
- `Clear()`: 서비스 종료용으로 `ResetRunState()`를 수행한 뒤 구독자까지 제거한다.

이를 위해 `ItemEventController`의 현재 `Clear()`를 `ClearQueuedEvents()`와 `ClearSubscribers()`로 분리한다. 기존 `ItemManager.Clear()` 공개 API는 유지한다.

새 런의 명확한 경계는 `SceneManager`가 `ESceneName.Game`을 실제로 로드하기 직전이다. 타이틀 시작과 게임 재시작이 모두 이 경로를 사용하므로 `SceneManager.OnScreenCovered()`에서 `ItemManager.ResetRunState()`를 한 번 호출한다. 이때 `ItemCatalogController`는 초기화하지 않거나 비우지 않으며, 보유 수량과 활성 아이템만 제거한다. ItemManager가 존재하지 않는 진입 경로도 허용하기 위해 `App.TryGet<ItemManager>()`를 사용한다.

## 보존 사항

- `ItemManager`의 기존 공개 구매·조회·구독 API
- 구매 처리 순서와 실패 조건
- 획득 시 `OnItemAcquired`, 구매 시 `OnItemPurchased`
- 다음 프레임 아이템 이벤트 전달 의미
- `App.Get<BattleManager>()`와 기존 SoundManager 사용
- 하드코딩된 포션 구매 제한과 아이템 데이터는 이번 변경 범위에서 유지

## 오류 및 수명 처리

기존 null/구매 제한/결제 실패는 `false`로 반환한다. `OnDestroy()`에서는 예약 코루틴 참조만 정리하며 Unity가 컴포넌트 코루틴을 종료하는 기존 수명 규칙을 따른다. 구독 해제 책임은 기존 소비자와 서비스 종료용 `Clear()`에 남긴다.

## 확인 범위

사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석은 수행하지 않는다. 변경 코드의 호출 순서, 공개 API 보존, 구독자 유지 여부를 코드 읽기로만 확인한다.
