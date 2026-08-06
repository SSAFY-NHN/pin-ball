# Copilot Instructions for pin-ball

이 저장소에서 작업하는 Copilot Agent는 다음을 우선 준수한다.

1. `AGENTS.md`의 규칙을 최우선으로 따른다.
2. 구조 변경/파일 이동/대규모 리팩터링은 사용자 승인 없이 수행하지 않는다.
3. 작업 절차는 **분석 → 계획(승인) → 구현 → 검증 → 보고** 순서를 따른다.
4. 전투 시스템 규칙:
   - 패배 조건: 아군 전멸
   - `App.Get<T>()` 호출 허용
   - UI 갱신은 최소 참조
   - SerializeField는 underscore 미사용
   - 큰 기능 총괄은 Manager, 하위 보조는 Controller
5. 런타임 자동 생성보다 씬 배치 + Inspector 참조를 우선한다.
6. 임시 코드는 유지 가능하며 TODO로 관리한다.
7. 사용자 직접 수정이 발생한 영역은 해당 수정본을 최신 기준 스타일로 간주하고, 이후 같은 기능 구현 시 우선 적용한다.
8. 구현 기본 선호: OnGUI 지양, 씬 배치 UI 우선 / SetActive 기반 풀링 우선 / enum+event 기반 단순 상태 연동 우선.

원문 지침 전문은 `.github/project-master-prompt.md`를 참조한다.
