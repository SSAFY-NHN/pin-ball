# 상태·웨이브 UI 리팩터링 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: StatusPanel 자원 피드백과 웨이브 HUD 표시 책임 분리, WavePanel 상태 조합 계산 분리, Scene 참조 검증 경계 정리, 10웨이브 정책 확인. 기존 WaveHudState 재추상화 금지.
- AI 제안 내용: `StatusFeedbackController`, `StatusWaveHudController`, `WaveButtonStateController`를 추가하고 Panel은 이벤트 전달과 실제 UI 적용에 집중. TitleData 계약과 HUD 에셋에 맞춰 10웨이브 정책 유지.
- AI 실제 수정 영역: `StatusPanel`, `WavePanel`, 신규 UI Controller 세 개, 설계 문서와 AI 활용 기록.
- 사용자 직접 결정/수정 필요 영역: 향후 가변 웨이브 난이도를 도입할 때 TitleData 계약과 특수 웨이브 메타데이터를 함께 재설계.
- 중요한 프롬프트/지시: 설계를 먼저 커밋한 뒤 승인 대기 없이 구현. `WaveHudState` 유지.
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석을 수행하지 않음. 기존 조건식, 공개 API 위임, 직렬화 필드 보존을 코드 읽기로만 확인함.
