# temp 브랜치 기능별 제거 목록

## 목적과 기준

- 비교 기준: `git diff --find-renames Dev...temp`
- 작성 시점: `temp`의 `7b5c410`까지
- 원칙: 아래 순서를 기능 단위로 따르고, 각 단계마다 지정 테스트와 검색을 실행한다.
- 공통 제거 순서: 테스트 → UI/씬 참조 → 런타임 연결 → 도메인 파일 → 데이터·문서
- 공통 주의: 여러 기능이 함께 수정한 `Game.unity`, `BattleManager`, `UnitManager`, `PinballManager`는 파일 전체를 Dev로 되돌리지 않는다.

## 1. 자동 핀볼 순환과 영구 공·복제 공 풀 분리

- 상태: 유지
- 관련 커밋: `0d10212`, `2600e3f`, `a237587`, `dba5cc7`
- 추가 파일: `Assets/02. Scripts/Pinball/PinballAutoCycleController.cs`, `Assets/02. Scripts/Pinball/Editor/PinballAutoCycleControllerTests.cs`, `Assets/02. Scripts/Pinball/Editor/PinballBallPoolTests.cs`
- 수정 파일과 심볼: `PinballManager.cs` — 자동 공급/회수 흐름, `PinballBallPool.cs` — 영구 공과 복제 공 풀, `Pinball.cs` — 활성화 수명, `PinballObstacle.cs` — 충돌 집계
- 씬·프리팹·데이터: `Game.unity`의 자동 공급 설정과 사전 배치 공, `Ball.prefab`
- 의존 기능: 생산 업그레이드의 공 추가·공급 속도, 지표의 공 활성화/회수/복제 수
- 제거 순서: 자동 순환 테스트 → 생산·지표 구독 제거 → 씬 자동 공급 참조 제거 → `PinballManager` 연결 제거 → 컨트롤러·풀 분리 코드 제거
- 검증: `PinballAutoCycleControllerTests`, `PinballBallPoolTests`; `rg -n 'PinballAutoCycleController|ActiveCloneCount|OnPermanentBall' Assets`

## 2. 핀볼 생산 업그레이드와 생산 UI

- 상태: 유지
- 관련 커밋: `48c58a4`, `43ecd6b`, `a237587`
- 추가 파일: `Assets/02. Scripts/Pinball/PinballProductionUpgradeController.cs`, `Assets/02. Scripts/03. UI/PinballProductionUpgradeDisplayController.cs`, 대응 Editor 테스트와 `.meta`
- 수정 파일과 심볼: `PinballManager.cs` — 구매 API/이벤트, `PinballBallPool.cs` — 영구 공 수량 확장
- 씬·프리팹·데이터: `Game.unity` 생산 업그레이드 버튼·텍스트·비용 설정
- 의존 기능: 자동 핀볼 순환, 전투 골드 경제, 프로토타입 지표
- 제거 순서: 생산 테스트 → 지표 구독 → 씬 UI → `PinballManager` API → 컨트롤러 파일
- 검증: `PinballProductionUpgradeControllerTests`; `rg -n 'PinballProductionUpgrade|OnProductionUpgradePurchased' Assets`

## 3. 콤보 배율·황금 공·잭팟과 피드백

- 상태: 유지
- 관련 커밋: `e60cafb`, `fac4fd9`, `776f1f2`
- 추가 파일: `Assets/02. Scripts/Pinball/Editor/PinballRewardControllerTests.cs`
- 수정 파일과 심볼: `PinballComboController.cs` — 콤보 배율, `PinballRewardController.cs` — 황금/잭팟 보상, `PinballManager.cs` — 잭팟 이벤트, `PinballObstacle.cs` — 범퍼 결과, `PinballComboDisplay.cs`, `PinballArcaneVfx.cs`, `PinballGoldPopup.cs`
- 씬·프리팹·데이터: `Game.unity` 잭팟 범퍼·피드백, `Ball.prefab`
- 의존 기능: 전술 증원의 콤보 5/잭팟 티켓, 지표의 잭팟·골드 항목
- 제거 순서: 보상 테스트 → 전술 증원 트리거 분리 → 지표 구독 → 씬/VFX → 보상·콤보 런타임
- 검증: `PinballRewardControllerTests`, `TacticalReinforcementControllerTests`; `rg -n 'Jackpot|GoldenBall|OnComboChanged' Assets`

## 4. 전투 업그레이드

- 상태: 유지
- 관련 커밋: `a19baae`, `6f933ae`, `1166565`
- 추가 파일: `Assets/02. Scripts/Battle/Runtime/BattleUpgradeController.cs`, `Assets/02. Scripts/03. UI/BattleUpgradeDisplayController.cs`, 대응 `.meta`
- 수정 파일과 심볼: `BattleManager.cs` — `TryPurchaseBattleUpgrade`, `ApplyBattleUpgrade`; `UnitManager.cs` — `SetSharedAttackMultiplier`; `UnitItemController.cs` — 공격 배율 합성
- 씬·프리팹·데이터: `Game.unity` 공격력/아군 방어선 HP 업그레이드 UI와 설정
- 의존 기능: 전투 골드, 양측 방어선 중 아군 최대 HP 증가(+10/레벨)
- 제거 순서: 관련 도메인 테스트 → 씬 UI → BattleManager 구매 API → UnitManager 배율 연결 → 컨트롤러
- 검증: 전투 업그레이드 관련 EditMode 테스트; `rg -n 'BattleUpgradeController|DefenseLineHp|AllyAttack' Assets`

## 5. 고정 10웨이브와 양측 방어선 판정

- 상태: 이번 작업에서 무한 연속 스테이지를 대체
- 관련 커밋: `f91fd57`, `33a1f0f`, `e4b80a8`, `bbed57b`, `76335d0`, `21d6736`, `98eeadf`, `2313f5c`, `7b5c410`
- 추가 파일: `BattleDefenseLineController.cs`, `DefenseLineTrigger.cs`, `DefenseLineBreachTests.cs`, `DefenseLineSceneTests.cs`와 대응 `.meta`
- 수정 파일과 심볼: `BattleRunState` — 웨이브/기회, `BattleResolutionPolicy.ResolveNextState`, `WaveResolutionState`, `BattleManager.TryStartWave/TryApplyDefenseLineAttack`, `UnitBase.TryMoveOrAttackDefenseLine`, `UnitManager.BeginWave/ResolveWaveResult`
- 씬·프리팹·데이터: `Game.unity`의 `AllyDefenseLine`/`EnemyDefenseLine`, HP 20/20, 기회 3, 결과 지연 2초; `BattleWaveData.json` 10개 웨이브
- 의존 기능: 아군 구매/전술 증원, 전투 업그레이드, HUD/결과창, 지표
- 제거 순서: 방어선/웨이브 테스트 → HUD/씬 라인 → 지표 이벤트 → BattleManager 흐름 → UnitBase/UnitManager 라인 공격 → 런타임 상태 → JSON
- 검증: `BattleRunStateTests`, `WaveResolutionTests`, `BattleDefenseLineControllerTests`, `DefenseLineBreachTests`, `DefenseLineSceneTests`; `rg -n 'TryStartWave|DefenseLine|BattleWaveStartedData' Assets`

## 6. 프로토타입 지표

- 상태: 유지
- 관련 커밋: `a9a0748`, `f9e6163`, `63e6e0b`, `21d6736`
- 추가 파일: `Assets/02. Scripts/PrototypeMetricsController.cs`, `Assets/02. Scripts/03. UI/PrototypeMetricsDisplayController.cs`, 대응 `.meta`
- 수정 파일과 심볼: `BattleManager` — 웨이브/구매/보스 이벤트, `PinballManager` — 공/범퍼/생산 이벤트
- 씬·프리팹·데이터: `Game.unity`의 `PrototypeMetricsPanel`
- 의존 기능: 자동 핀볼, 생산, 잭팟, 구매, 전투 업그레이드, 웨이브
- 제거 순서: 씬 패널 → 두 매니저 이벤트 구독/발행 중 지표 전용분 → 표시 컨트롤러 → 지표 컨트롤러
- 검증: `GameplayFeedbackSceneTests`; `rg -n 'PrototypeMetrics|OnWaveStarted|OnPermanentBallActivated' Assets`

## 7. 선택형 아군 구매와 유닛별 소유권

- 상태: 유지
- 관련 커밋: `6d46fd4`, `ea69b4b`, `7ddb763`, `d7f743f`, `8f13548`, `a8d751c`, `ad37a6b`
- 추가 파일: `UnitPurchaseController.cs`, `UnitPurchaseControllerTests.cs`, `AllyPurchasePanelController.cs`, `AllyPurchaseUiSceneTests.cs`와 대응 `.meta`
- 수정 파일과 심볼: `BattleManager.TryPurchaseAlly`, `UnitManager.TryPurchaseAlly`, `UnitRoster.OwnedAllies/GetOwnedAllyCount`, `AlllyUnit` 전투 중 참여
- 씬·프리팹·데이터: `Game.unity` 전사/궁수/마법사 구매 카드
- 의존 기능: 전투 골드, 전술 증원 무료 구매권, 웨이브 중 즉시 증원
- 제거 순서: 구매 테스트 → 전술 증원 무료 분기 → 씬 카드 → BattleManager API → UnitManager/UnitRoster 소유권 확장 → 컨트롤러
- 검증: `UnitPurchaseControllerTests`, `UnitRosterTests`, `AllyPurchaseUiSceneTests`; `rg -n 'TryPurchaseAlly|OwnedAllies|AllyPurchasePanelController' Assets`

## 8. 전술 증원

- 상태: 유지 — 사용자 명시 결정
- 관련 커밋: `d2d3167`, `3c84365`, `b5e058d`, `8f13548`
- 추가 파일: `TacticalReinforcementController.cs`, `TacticalReinforcementControllerTests.cs`와 대응 `.meta`
- 수정 파일과 심볼: `BattleManager.OnPinballComboChanged/OnJackpotTriggered/TryPurchaseAlly`, `AllyPurchasePanelController.OnTacticalReinforcementChanged`
- 씬·프리팹·데이터: `Game.unity` 구매 패널의 `reinforcementNotice`; 중첩되지 않는 티켓 1개
- 의존 기능: 콤보/잭팟 이벤트, 선택형 아군 구매
- 제거 순서: 테스트 → 구매 패널 안내 → BattleManager 구독·무료 분기·이벤트 → 컨트롤러
- 검증: `TacticalReinforcementControllerTests`, `UnitPurchaseControllerTests`, `AllyPurchaseUiSceneTests`; `rg -n 'TacticalReinforcement|HasTacticalReinforcement' Assets`

## 9. 경제와 적 능력치 밸런스

- 상태: 유지
- 관련 커밋: `954ae9a`, `9a1da55`, `227f2b6`
- 추가 파일: 없음
- 수정 파일과 심볼: `EnemyUnitData.cs` 기본 이동/전투 값, `BattleManager` 구매 비용 설정, `UnitPurchaseController` 비용 증가, `BattleEconomyTests`
- 씬·프리팹·데이터: `EnemyUnitData.json`, `BattleWaveData.json`, `EnemyUnit.prefab`, `Game.unity` 비용 설정
- 의존 기능: 아군 구매, 전투 업그레이드, 고정 웨이브 난이도
- 제거 순서: 밸런스 테스트 → 씬 수치 → JSON → 코드 기본값
- 검증: `BattleEconomyTests`, `UnitPurchaseControllerTests`; `git diff Dev...HEAD -- Assets/Resources/Data Assets/04. Prefabs/EnemyUnit.prefab`

## 10. 결과·상태·구매 UI 변경

- 상태: 유지
- 관련 커밋: `8adbf63`, `ea69b4b`, `98eeadf`, `2313f5c`
- 추가 파일: 구매/생산/전투 업그레이드/지표 표시 컨트롤러와 씬 테스트
- 수정 파일과 심볼: `WavePanel.Refresh`, `StatusPanel.FormatChances/FormatDefenseLines`, `StatusWaveHudController.Display`, `WaveResultPanel`, `ResultPanel.OnBattleStateChanged`, `StatusFeedbackController`
- 씬·프리팹·데이터: `Game.unity` 시작 버튼, 10노드/9커넥터, 양측 HP 텍스트, 승패 이미지, 재시작/타이틀 버튼
- 의존 기능: 고정 웨이브/방어선, 구매, 생산, 전술 증원
- 제거 순서: UI 테스트 → 씬 참조 → 각 표시 컨트롤러 → 이벤트 포맷터
- 검증: `WaveHudStateTests`, `WaveResultPanelTests`, `GameplayFeedbackSceneTests`, `AllyPurchaseUiSceneTests`; `rg -n 'FormatChances|StatusWaveHudController|ResultPanel' Assets`

## 11. Dev 대비 계속 삭제 상태인 기존 동작

- 상태: Dev 대비 계속 삭제 상태
- 관련 커밋: `2600e3f`, `e909f45`, 이후 현재 작업
- 삭제 상태 기능: 수동 핀볼 발사, 골 포켓 아군 생성, 준비/전투 카메라 및 패널 슬라이드, 기존 튜토리얼 호환 일부
- 수정/삭제 파일과 심볼: `WavePanel`의 launch 제어는 숨김 유지, `PinballManager` 수동 launch 흐름 미사용, `GameLayoutController`/`BattleCameraController` 상태 슬라이드 제거, `TutorialManager` 호환 흐름 축소, `AllyDeploymentLimitTests`와 `BattleCameraControllerTests` 삭제 상태
- 씬·프리팹·데이터: `Game.unity`에서 launch UI는 참조만 남고 비활성 처리; 자동 핀볼 레이아웃 유지
- 의존 기능: 자동 핀볼과 현재 구매 방식이 대체 기능
- 복귀 순서: 요구사항 재승인 → 삭제된 테스트 복구 → 씬/UI → 런타임 이벤트 → 튜토리얼; 현재 자동 흐름과 충돌 해결 필요
- 검증: `rg -n 'OnGoalReached|launchButton|BattleCameraController|GameLayoutController' Assets`; 자동 순환/구매 테스트 전체

## 현재 제거 완료 항목

- 무한 연속 스테이지: `BattleStageController`, `EnemyStageScalingController`, 관련 테스트와 직렬화 키 제거 완료
- 웨이브 클리어/재시도/최종 클리어 골드 보상: 세 필드와 JSON 값 제거 완료
- 전술 증원: 제거하지 않고 유지
