# 웨이브별 아군 초기화·전투 중 구매 구현 AI 활용 기록

## 사용 도구/모델

- Codex (GPT-5 계열)
- 로컬 Git, Unity 6.0.0.79f1 batchmode, NUnit EditMode Test Runner

## 사용자 요청

- 새 런과 모든 다음 웨이브·재도전을 아군 0명으로 시작
- 아군 없이 웨이브 시작 허용
- 일반 구매와 전술 증원권 무료 구매를 `EWaveState.Active`에서만 허용
- 결과 표시 종료 시 모든 아군 풀 회수
- 웨이브 종료 시 구매 횟수·가격·출격 쿨다운 초기화
- 골드, 증원권, 업그레이드, 아이템, 남은 기회와 웨이브 인덱스 정책 유지
- 구현 계획 승인 후 TDD 구현과 Unity 검증

## AI 제안 내용

- `BattleManager`에서 웨이브 상태 구매 게이트와 결과 종료 순서를 소유
- `UnitManager`에서 기존 `UnitSpawner.ReturnUnit` 경로로 모든 아군 회수
- `UnitPurchaseController.ResetForWave`로 구매 횟수와 쿨다운만 초기화
- 구매 횟수 기반 기존 비용 계산을 유지해 기본 가격 복원
- `AllyPurchasePanelController`의 기존 상태 이벤트와 조회 API를 재사용해 새 UI 이벤트와 씬 배선 추가 방지

## AI 실제 수정 영역

- `BattleManager`
  - 아군 수와 무관한 `CanStartCurrentWave` 조건 적용
  - `CanPurchaseAlly`, `TryPurchaseAlly`에 `Active` 선검사 적용
  - `FinishWaveResolution`에서 아군 회수 뒤 구매 상태 초기화
- `UnitManager`
  - `startingAlly`와 새 런 기본 전사 생성 제거
  - 0명 웨이브 시작 허용
  - 결과 종료 시 모든 owned/active 아군을 풀로 반환
- `UnitRoster`
  - owned/active 아군을 함께 비우는 `DrainAllies` 추가
- `UnitPurchaseController`
  - 모든 구매 횟수와 남은 쿨다운을 0으로 만드는 `ResetForWave` 추가
- EditMode 테스트
  - inactive 상태 일반·무료 구매 불변 조건
  - active 상태 구매 가능 판정
  - 0명 웨이브 시작
  - 모든 아군 풀 회수
  - 클리어, 실패, 최종 승리, 최종 패배의 공통 구매 상태 초기화
  - 골드·전술 증원권 유지와 웨이브 인덱스 정책
  - Game 씬 기본 아군 제거와 기존 구매 UI 참조
- 구현 계획 문서 작성

## 사용자 직접 결정/수정 필요 영역

- 사용자 확정 설계와 제외 범위를 그대로 적용했다.
- 준비 단계 타이머, 경제 정책, 유닛 레벨, 덱 편성, 수치 변경과 새 UI는 수정하지 않았다.
- 실제 플레이 감각과 결과 패널 표시 시간 동안의 조작 경험은 사용자가 에디터에서 직접 확인할 수 있다.

## 중요 프롬프트/지시

- 설계 커밋 `e409844`와 선행 전투·UI 구현을 기준으로 분석
- 승인 전 코드 수정 금지
- TDD로 최소 범위 구현
- 사용자 소유 `Rabbit1_Mage_Attack.anim`, `ArcaneVfxCatalog.asset` 변경 보존 및 스테이징 제외
- 요청 없는 리팩터링, 외부 패키지와 씬 재직렬화 금지

## 테스트/검증 결과

### TDD RED

- 초기 상태 테스트: 13개 중 7개가 기대대로 실패했다.
  - 0명 시작 2개
  - `Pending`/`Resolving` 일반·무료 구매 차단 4개
  - 결과 후 모든 아군 회수 1개
- 결과별 reset 호출을 제거한 검증: 17개 중 4개가 기대대로 실패했다.
  - 일반 클리어
  - 실패 후 재도전
  - 최종 승리
  - 최종 패배

### 관련 EditMode 최종 검증

- 결과: 83/83 통과, 실패 0, skipped 0
- 포함 대상:
  - `WaveRosterResetPurchaseStateTests`
  - `UnitRosterTests`
  - `UnitPurchaseControllerTests`
  - `BattleRunStateTests`
  - `WaveResolutionTests`
  - `UnitPoolResetTests`
  - `AllyInteractionPolicyTests`
  - `TacticalReinforcementControllerTests`
  - `AllyPurchaseUiSceneTests`
- Unity 로그에서 C# 컴파일 오류, `Compilation failed`, `MissingReferenceException`, `Missing Script` 검색 결과 0개

### 전체 EditMode 검증

- 결과: 253개 중 248개 통과, 5개 실패
- 이번 변경과 무관한 기존 실패:
  - `BattleDataCharacterizationTests.EnemyCreateStats_AppliesWaveGrowthAndFlooring`: 기대 121, 실제 122
  - `UnitCreationServiceTests.TryCreateEnemy_CreatesWaveScaledStats`: 기대 120, 실제 121
  - `DefenseLineBreachTests.Tick_AfterReinforcementAppears_LeavesDefenseLineAndTargetsAlly`: 기존 테스트의 null `battleArea`
  - `GameplayFeedbackSceneTests.GameScene_WiresResultCostAndInteractionGlow`: `LaunchCost` 씬 기대 불일치
  - `SoundManagerTests.DeveloperScene_RegistersStartupBgmAndEverySfxClip`: 기대 17, 실제 19
- 전체 실행 로그에서도 C# 컴파일 오류와 Missing Script/Reference 검색 결과 0개

### Git·씬 검사

- Game 씬의 구매 패널 1개와 네 카드 참조, 증원권 문구 참조를 테스트로 확인했다.
- 활성 `AllyUnit` 씬 인스턴스가 없고 제거된 `startingAlly` 직렬화 프로퍼티가 노출되지 않음을 확인했다.
- Unity가 자동 변경한 `ProjectSettings.asset`의 WebGL define은 작업 범위 밖 변경으로 확인 후 원복했다.
- 사용자 소유 애니메이션과 VFX 애셋은 수정·원복·스테이징하지 않았다.

## 제한점/직접 확인 필요

- 전체 EditMode의 기존 실패 5개는 이번 요청 범위 밖이므로 수정하지 않았다.
- GUI 플레이 모드의 사람 조작 검증은 실행하지 않았다. Game 씬 참조와 상태 흐름은 EditMode 테스트로 검증했다.
- 사용자는 에디터에서 새 런, 무유닛 웨이브 시작, 전투 중 일반·무료 구매, 결과 표시 종료 후 0명·기본 가격·쿨다운 0, 다음 웨이브·재도전·최종 결과를 직접 확인할 수 있다.
