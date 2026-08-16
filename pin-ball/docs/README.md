# Pin-Ball 프로젝트 문서

이 디렉터리는 기술 설계, 구현 계획, AI 활용 기록과 프로젝트 인계 자료를 목적별로 보관한다.

## 문서 구조

- `designs/YYYY-MM-DD/`: 승인된 기술 설계와 구조 결정
- `plans/YYYY-MM-DD/`: 구현 계획과 작업 체크리스트
- `ai-usage/YYYY-MM-DD/`: `AGENTS.md`에서 요구하는 작업별 AI 활용 기록
- `ai-usage/legacy/`: 날짜별 기록으로 분리하기 전의 누적 AI 활용 기록
- `content/`: 영상 대본 등 게임 콘텐츠 문서
- `handbook/`: 프로젝트 작업 방법과 인계 문서

저장소 진입 규칙인 루트 `AGENTS.md`, Copilot이 직접 읽는 `.github/copilot-instructions.md`, 세부 원문 지침인 `.github/project-master-prompt.md`는 기존 위치를 유지한다. 그 밖의 일반 Markdown 문서는 목적에 맞는 `docs` 하위 디렉터리에 둔다.

## 프로젝트 지침

- [프로젝트 지침서](handbook/프로젝트%20지침서.md)
- [아트 리소스 지침서](handbook/아트%20리소스%20지침서.md)
- [새 채팅 추가 인계 지침서](handbook/새_채팅_추가_인계_지침서.md)
- [이전 누적 AI 활용 기록](ai-usage/legacy/ai-use-log.md)

## 최신 리팩터링 문서

- [M2 핀볼 후속 리팩터링](designs/2026-08-16/2026-08-16-pinball-followup-refactoring-design.md)
- [ItemManager 후속 리팩터링](designs/2026-08-16/2026-08-16-item-manager-followup-refactoring-design.md)
- [M4 상점 시스템 리팩터링](designs/2026-08-16/2026-08-16-shop-system-refactoring-design.md)
- [M5 튜토리얼 시스템 리팩터링](designs/2026-08-16/2026-08-16-tutorial-system-refactoring-design.md)
- [M6 SoundManager 리팩터링](designs/2026-08-16/2026-08-16-sound-manager-refactoring-design.md)
- [M7 상태·웨이브 UI 리팩터링](designs/2026-08-16/2026-08-16-status-wave-ui-refactoring-design.md)
- [M8 타이틀 씬과 새 런 초기화](designs/2026-08-16/2026-08-16-title-new-run-initialization-design.md)
- [문서 구조 정리](designs/2026-08-16/2026-08-16-documentation-organization-design.md)

## 새 문서 작성 규칙

- 파일명은 `YYYY-MM-DD-topic-purpose.md` 형식을 기본으로 한다.
- 기술 설계, 구현 계획, AI 활용 기록은 문서 성격과 작성일에 맞는 디렉터리에 저장한다.
- 과거 설계와 계획은 당시 의사결정 기록으로 유지하고, 구현 결과가 달라졌다면 기존 본문을 덮어쓰기보다 상태나 후속 결정을 명시한다.
- 새 문서에서 다른 문서를 가리킬 때는 현재 디렉터리 구조를 기준으로 상대 링크를 사용한다.
- 과거 구현 계획 안의 명령어나 당시 파일 경로는 실행 지침이 아닌 기록으로 취급한다. 현재 작업에는 이 색인과 `AGENTS.md`의 경로를 사용한다.
