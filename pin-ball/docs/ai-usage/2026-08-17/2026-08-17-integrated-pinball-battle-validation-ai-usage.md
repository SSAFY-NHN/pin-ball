# 자동 핀볼·연속 전투 통합 검증 AI 활용 기록

## 사용한 AI 도구/모델

- OpenAI Codex
- GPT-5 계열 모델

## 사용자 요청

- 마일스톤 1과 2가 반영된 작업 트리에서 통합 검증 마일스톤만 진행
- 자동 핀볼 생산과 연속 전투가 10단계 보스까지 함께 진행되는지 제한된 Play 검증
- 재현된 교착, 회수 누락, 상태 오류만 최소 수정
- 전체 테스트와 빌드 없이 관련된 소수 테스트만 실행
- 밸런스 변경, 관련 없는 수정, 자동 스테이징과 커밋 금지

## AI 제안 내용

- Game 씬 한 번의 Play 세션에서 공 회수·재생성, 단계 자동 전환, 아군 전멸,
  이동 중 증원, 개별 돌파, 혼합 제거, 같은 단계 재정비, 성장 유지와 10단계 보스를
  순서대로 확인
- 기존 런타임 계측과 공개 동작을 사용하고 임시 검증 파일은 종료 후 제거
- Play에서 재현된 제품 결함만 최소 수정하고 관련 fixture만 재검증

## AI 실제 수정 영역

- 제품 코드와 씬은 수정하지 않음
- 통합 Play 검증용 임시 Editor 파일을 만들었으나 검증 종료 후 파일과 meta를 제거
- Unity 실행 중 자동 변경된 `ProjectSettings/ProjectSettings.asset`을 원래 상태로 복원
- 본 AI 활용 기록만 추가

## 사용자 직접 결정/수정 필요 영역

- 정상 런 진입 순서를 사용해 Editor에서 필수 시나리오 10개를 다시 확인해야 함
- Game 씬 직접 Play에서는 `TitleData`와 `ItemManager`가 등록되지 않아 런이 초기화되지
  않았음. Developer 또는 Title 진입 후 Game 씬으로 전환하는 실제 프로젝트 흐름 확인 필요

## 중요한 프롬프트/지시

- 승인 전 코드·씬 수정과 Unity 실행 금지
- 승인 후 제한된 Play 검증 한 번만 수행
- 같은 실패 명령 반복 금지
- 로그 전체 덤프 금지, 결정적 오류만 보고
- 임시 검증 파일을 남기지 않음
- 재현된 결함만 수정하고 예방 리팩터링 금지

## 테스트/검증 결과

- Game 씬 직접 Play 진입 시 다음 결정적 초기화 오류가 발생함
  - `InvalidOperationException: TitleData is not registered.`
  - `InvalidOperationException: ItemManager is not registered.`
- `BattleManager`와 `PinballManager` 초기화가 중단되어 필수 Play 시나리오 10개는
  성공 또는 실패로 판정할 수 없었음
- 사용자 제한에 따라 Play를 다시 실행하지 않았고 제품 결함 수정도 수행하지 않음
- 관련 EditMode fixture 5개를 한 번의 명령으로 실행: 총 17개 통과, 실패 0개
  - `PinballAutoCycleControllerTests`
  - `PinballBallPoolTests`
  - `DefenseLineBreachTests`
  - `UnitRosterTests`
  - `UnitTargetFinderTests`
- 전체 EditMode, 전체 PlayMode, 전체 빌드와 WebGL 빌드는 실행하지 않음
