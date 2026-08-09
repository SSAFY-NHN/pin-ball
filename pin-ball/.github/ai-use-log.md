# AI 활용 기록

## 2026-08-10 전투 상태 기반 카메라 슬라이드

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 준비 상태에는 맵 옆 핀볼이 보이도록 카메라를 이동하고, 전투 시작 시 기존 전투 화면으로 부드럽게 복귀
- AI 제안 내용: `BattleManager` 상태 이벤트를 구독하는 씬 배치형 `BattleCameraController`와 0.5초 cubic ease-out 슬라이드
- AI 실제 수정 영역: `BattleCameraController`, EditMode 테스트, Game 씬 Main Camera 연결, 설계·구현 계획 및 AI 활용 기록
- 사용자 직접 결정/수정 필요 영역: 사용자가 전용 컴포넌트 방식과 부드러운 슬라이드를 결정했으며, 최종 핀볼 구도와 이동 시간은 Inspector에서 조정 가능
- 중요한 프롬프트/지시: 기존 구조 보존, Inspector 참조 우선, `[SerializeField]` underscore 금지, 최소 변경, 외부 패키지 금지
- 테스트/검증 결과: 구현 전 `BattleCameraController` 타입 부재로 `CS0246` 실패를 확인하고, 구현 후 `dotnet build Assembly-CSharp-Editor.csproj --no-restore`가 오류 0개로 성공했다. Unity 배치 EditMode 테스트는 라이선스 클라이언트 연결 실패로 결과가 생성되지 않았으며 Play Mode와 WebGL 실행 검증은 수행하지 못했다.

## 2026-08-10 핀볼 배치 복구

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: `feat: refine evolution ui and mask glow` 커밋에서 사라진 기존 핀볼 배치 복구
- AI 제안 내용: 커밋 전체를 되돌리지 않고 `3cd17e2^`의 핀볼 Transform 배치만 현재 Game 씬에 병합
- AI 실제 수정 영역: `Assets/01. Scenes/02. Game.unity`의 핀볼 오브젝트 위치·회전·스케일
- 사용자 직접 결정/수정 필요 영역: 사용자가 기존 Evolution UI와 카메라 변경을 유지하는 선택적 복구를 승인
- 중요한 프롬프트/지시: 기존 변경 보존, 관련 없는 UI 변경 유지, 최소 범위 복구
- 테스트/검증 결과: 공통으로 대응되는 핀볼 오브젝트의 Transform을 커밋 직전 값과 대조해 복원했다. 완전히 삭제된 중복 범퍼 1개와 Goal Guide 4개도 컴포넌트 및 부모 계층 참조와 함께 복원했다. 씬 전체 검사 결과 중복 fileID 0개, 미해결 로컬 참조 0개이며 `git diff --check`를 통과했다. Unity 실행 검증은 라이선스 클라이언트 제한으로 수행하지 못했다.
