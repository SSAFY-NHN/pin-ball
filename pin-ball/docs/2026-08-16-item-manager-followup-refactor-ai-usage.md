# ItemManager 후속 리팩터링 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: `ItemManager.TryPurchase()` 구매 조율 분리 검토, 매 프레임 이벤트 큐 `Update()` 제거 설계, 새 런 초기화와 서비스 종료용 `Clear()` 의미 분리. 설계 커밋 후 승인 대기 없이 바로 구현.
- AI 제안 내용: `ItemPurchaseController`에 결제·획득·구매 이벤트·SFX 순서를 분리하고, enqueue 시 코루틴 하나로 다음 프레임 처리를 예약. `ResetRunState()`는 구독자를 보존하고 `Clear()`만 구독자를 제거.
- AI 실제 수정 영역: `ItemManager`, `ItemEventController`, 신규 `ItemPurchaseController`, 설계 문서와 AI 활용 기록.
- 사용자 직접 결정/수정 필요 영역: 새 런을 실제 시작하는 상위 흐름에서 `ResetRunState()` 호출 시점 연결은 별도 요청 범위로 남김.
- 중요한 프롬프트/지시: Dev에서 직접 작업하며 별도 워크트리/브랜치를 만들지 않음. 설계를 먼저 커밋한 뒤 즉시 구현. 테스트나 검사를 위한 검사를 하지 않음.
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석을 수행하지 않음. 공개 API와 호출 순서를 코드 읽기로만 확인함.
