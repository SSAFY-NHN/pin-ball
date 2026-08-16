# 상점 시스템 리팩터링 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: 상품 후보 생성·셔플·포션 보장, 리롤 결제와 갱신 조율, Scene 슬롯 참조, 튜토리얼 구매 제한 경계를 `ShopPanel`에서 분리하고 기존 `ItemManager.TryPurchase()` 사용 유지.
- AI 제안 내용: `ShopOfferController`, `ShopRerollController`, `ShopPurchasePolicyController`로 규칙을 나누고 `ShopPanel`은 슬롯 표시와 UI 이벤트에 집중.
- AI 실제 수정 영역: `ShopPanel`, 신규 상점 Controller 세 개, Game 씬 ShopSlot Inspector 배열, 설계 문서와 AI 활용 기록.
- 사용자 직접 결정/수정 필요 영역: 없음. 기존 슬롯 세 개의 Scene 배치 순서를 상품 표시 순서로 유지.
- 중요한 프롬프트/지시: 설계를 먼저 커밋한 뒤 승인 대기 없이 구현. 기존 구매 API와 상점 동작 보존.
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석을 수행하지 않음. 코드 호출 관계와 Scene 직렬화 참조만 읽어 확인함.
