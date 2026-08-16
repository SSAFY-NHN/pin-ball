# SoundManager 리팩터링 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: 고정 `SoundManager`와 공개 API를 유지하면서 BGM, SFX 풀, Mixer, Scene Button 클릭음 책임 분리와 직렬화 필드 이름 정리. Update와 `SoundName.GetAttack()` 구조 검토.
- AI 제안 내용: 네 하위 Controller로 책임을 분리하고 활성 SFX Update 및 자동 Button 검색은 비용·동작 보존을 위해 유지하되 Manager 밖으로 이동. 유닛 사운드 데이터 스키마 이전은 별도 마일스톤으로 유지.
- AI 실제 수정 영역: `SoundManager`, 신규 Sound Controller 네 개, 이름이 변경된 직렬화 필드를 참조하는 기존 Editor 테스트 문자열, 설계 문서와 AI 활용 기록.
- 사용자 직접 결정/수정 필요 영역: 향후 Scene UI Controller 기반 Button 명시 등록 전환과 유닛 데이터 사운드 필드 추가 여부.
- 중요한 프롬프트/지시: 설계를 먼저 커밋한 뒤 승인 대기 없이 구현. `SoundManager` 공개 API와 기존 사운드 동작 유지.
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석을 수행하지 않음. 공개 위임 관계와 직렬화 호환 속성을 코드 읽기로만 확인함.
