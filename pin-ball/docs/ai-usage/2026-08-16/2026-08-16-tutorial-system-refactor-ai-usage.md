# 튜토리얼 시스템 리팩터링 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: 튜토리얼 진행/UI/입력·포커스/게임 규칙 책임 분리, ShopPanel·WavePanel Inspector 연결, Update 타이머 제거, Editor 완료 키 처리와 새 런 관계 결정.
- AI 제안 내용: 기존 `TutorialProgress`를 유지하고 `TutorialUIController`, `TutorialInteractionController`, `TutorialGameRuleController`를 추가. 최대 시간은 실시간 코루틴으로 처리하고 완료 PlayerPrefs는 새 런과 독립적으로 유지.
- AI 실제 수정 영역: `TutorialManager`, 신규 Tutorial Controller 세 개, Game 씬 Inspector 참조, 설계 문서와 AI 활용 기록.
- 사용자 직접 결정/수정 필요 영역: 튜토리얼 재실행이 필요할 때 `Tutorial.Completed` PlayerPrefs 키를 개발자가 명시적으로 삭제.
- 중요한 프롬프트/지시: 설계를 먼저 커밋한 뒤 승인 대기 없이 구현. 기존 단계 순서와 게임 동작 보존.
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석을 수행하지 않음. 이벤트 흐름, Controller 호출, Scene 직렬화 참조를 코드 읽기로만 확인함.
