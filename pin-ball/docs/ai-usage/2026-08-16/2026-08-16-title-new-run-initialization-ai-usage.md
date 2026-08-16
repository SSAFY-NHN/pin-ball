# 타이틀·새 런 초기화 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: 타이틀 시작과 결과 후 재시작에서 Battle, Unit, Item, Pinball 런 상태를 하나의 흐름으로 초기화하고 새 전역 Manager 없이 조정자 위치 결정.
- AI 제안 내용: 지속 `SceneManager`가 일반 `GameRunController`를 소유하고 Game 로드 전 Item 상태, 로드 후 Unit → Battle → Pinball 순서로 명시 초기화. 각 Game 서비스 Start는 직접 Scene 진입 fallback으로 동일 API 호출.
- AI 실제 수정 영역: `SceneManager`, 신규 `GameRunController`, `BattleManager`, `UnitManager`, `PinballManager`, 설계 문서와 AI 활용 기록.
- 사용자 직접 결정/수정 필요 영역: 없음. 튜토리얼 완료 PlayerPrefs는 새 런과 독립된 설치 단위 진행 상태로 유지.
- 중요한 프롬프트/지시: 설계를 먼저 커밋한 뒤 승인 대기 없이 구현. 새로운 전역 Manager 금지와 기존 App/AppService 구조 유지.
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석을 수행하지 않음. Scene 로드 전후 순서, idempotent guard, 서비스 구독 관계를 코드 읽기로만 확인함.
