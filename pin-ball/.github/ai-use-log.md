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

## 2026-08-10 아군 배치 수 제한

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군 6마리부터 웨이브 시작을 차단하고 7마리부터 핀볼 발사를 차단하며, Status UI에 현재 수/5를 표시하고 6마리 이상을 빨간색으로 표시
- AI 제안 내용: `UnitManager`를 배치 수와 제한 규칙의 단일 기준점으로 사용하고, Manager 방어 검증과 이벤트 기반 UI 갱신을 적용
- AI 실제 수정 영역: `UnitManager`, `BattleManager`, `PinballManager`, `WavePanel`, `StatusPanel`, EditMode 경계값 테스트, Game 씬의 `AllyCountText` 및 Inspector 참조
- 사용자 직접 결정/수정 필요 영역: 사용자가 정확히 6마리일 때 핀볼 발사를 허용하고 표시 형식을 `5/5`로 결정했으며, 실제 Game View에서 최종 텍스트 위치 미세 조정 가능
- 중요한 프롬프트/지시: 기존 구조 보존, 씬 배치와 Inspector 참조 우선, `[SerializeField]` underscore 금지, 최소 변경, 외부 패키지 금지
- 테스트/검증 결과: 구현 전 두 규칙 메서드 부재로 `CS0117` RED 실패를 확인했다. 구현 후 5·6·7 경계를 포함한 focused EditMode 테스트와 전체 EditMode 테스트가 Unity 로그 기준 code 0으로 완료됐고 C# 및 씬 역직렬화 오류가 없었다. 프로젝트에 WebGL 배치 빌드 진입점이 없어 WebGL 빌드는 수행하지 않았으며, 5·6·7마리 실제 배치에 대한 Play Mode 시각 확인은 사용자 직접 확인 항목으로 남았다.

## 2026-08-10 아군 준비 배치 제한 및 복원

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군을 맵 오른쪽 절반에만 배치하고 오른쪽 끝까지 사용할 수 있게 하며, 소환 시 가로 우선 격자로 배치하고 웨이브 종료 후 기존 배치를 복원
- AI 제안 내용: 전체 전투 경계와 아군 준비 배치 경계를 분리하고 `UnitManager`가 캐릭터별 준비 위치를 런타임 동안 보존
- AI 실제 수정 영역: `BattleAreaBounds`, `UnitSpawner`, `UnitManager`, `AlllyUnit`, EditMode 배치 테스트, Game 씬의 `Panel_BattleArea`, AI 사용 기록
- 사용자 직접 결정/수정 필요 영역: 사용자가 오른쪽 절반, 가로 우선 격자, 캐릭터별 위치 저장 방식을 결정했으며 최종 배치 간격과 체감은 Game View에서 확인 가능
- 중요한 프롬프트/지시: 기존 구조와 적/전투 이동 보존, 최소 수정, Inspector 참조 유지, SetActive 풀링 유지, `[SerializeField]` underscore 금지
- 테스트/검증 결과: 새 테스트는 구현 전 7개와 3개가 각각 의도대로 실패했고 구현 후 배치 테스트 10/10, 전체 EditMode 45/45가 통과했다. `dotnet build Assembly-CSharp-Editor.csproj --no-restore`는 오류 0개로 완료됐으며 기존 패키지 참조 경고 9개가 남았다. 프로젝트에 WebGL 배치 빌드 진입점이 없어 실제 WebGL 빌드와 Game View 체감 확인은 수행하지 못했다.

## 2026-08-10 게임플레이 피드백 마일스톤

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군 보유 제한 해제와 5명 웨이브 참가 제한, 아군 영구 사망, 2초 결과 대기 상태, 강화된 HP/골드 피드백, 발사 비용 유지, 보드·손잡이 발광
- AI 제안 내용: `UnitManager` 영구 사망 정리, 명시적 `Resolving` 상태와 결과 이벤트, UI 비귀속 결과 배너, 기존 mask/additive/Bloom을 재사용하는 발광 상태 컴포넌트
- AI 실제 수정 영역: 유닛 roster/발사 조건, `BattleManager`와 resolution 도메인, `WaveResultPanel`·`StatusPanel`, 보드/손잡이 발광 코드와 Game 씬 배선, EditMode 테스트
- 사용자 직접 결정/수정 필요 영역: 사용자가 전용 종료 상태와 2초 대기, 안내 문구 없는 손잡이 발광을 선택했으며 튜토리얼·금색 유닛 조명·접지 그림자는 이번 작업에서 제외했다. 최종 발광 강도와 배너 위치는 Game View에서 미세 조정 가능하다.
- 중요한 프롬프트/지시: 기존 구조·물리·콜라이더 보존, Inspector 참조 우선, 외부 패키지 금지, `발사 {비용}G` UI 유지, 범위 밖 기능 제외
- 테스트/검증 결과: Task 1 집중 테스트 18/18 통과. 최종 핵심 EditMode 묶음은 최초 36/37 통과 후 BoardGlow 스프라이트 fileID를 수정했고, 해당 씬 회귀 테스트 1/1이 통과했다. 구현 전 전체 기준선의 기존 `EnemyWaveVisualTests` 3개 실패는 사용자 승인 아래 범위 밖으로 유지했다. Unity 컴파일은 EditMode 실행에서 완료됐고, 별도 `dotnet build`는 설치된 .NET SDK가 없어 실행할 수 없었다. 사용자 요청에 따라 반복 전체 테스트, Play Mode 및 WebGL 빌드는 생략했다.
