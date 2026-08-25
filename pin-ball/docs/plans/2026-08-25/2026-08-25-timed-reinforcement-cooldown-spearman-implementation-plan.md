# 시간표 증원·출격 쿨다운·창병 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: 승인 후 `superpowers:test-driven-development`로 각 작업을 Red-Green-Refactor 순서로 수행하고, 완료 주장 전 `superpowers:verification-before-completion`을 사용한다. 이 문서의 체크박스를 진행 기록으로 사용한다.

**Goal:** 10개 웨이브를 시간표 기반 지속 증원 구조로 바꾸고, 4개 기본 아군의 독립 출격 쿨다운·창병 구매 카드·공세 카운트다운 HUD를 기존 방어선 및 핀볼 흐름을 보존하며 추가한다.

**Architecture:** `BattleAssaultController`가 순수 C# 상태 객체로 웨이브 경과 시간, 초기 예약, 공세 단계와 반복 시점을 소유한다. `BattleManager`는 Unity `Time.deltaTime`을 전달하고 상태·구매·UI 이벤트를 중계하며, 실제 적과 아군 생성은 기존 `UnitManager` 풀링 경로만 사용한다. `UnitPurchaseController`가 유닛별 남은 쿨다운을 소유하고 UI는 `BattleManager` 조회값만 표시한다.

**Tech Stack:** Unity 6, C#, Unity Test Framework EditMode, NUnit, TextMeshPro, uGUI, Unity YAML scene serialization, `JsonUtility`

**Spec:** `docs/designs/2026-08-25/2026-08-25-timed-reinforcement-cooldown-spearman-design.md` (commit `654ad6c`)

## Global Constraints

- 사용자 승인 전 이 계획 문서 외 코드·데이터·씬을 수정하지 않는다.
- 초기 시간표 이후에도 적 방어선이 살아 있는 동안 증원을 계속한다.
- 강화 증원은 60초, 최후 공세는 90초부터 반복한다.
- 동시 생존 적 상한은 8명이며, 상한 때문에 누락된 수량은 대기열에 누적하지 않는다.
- 적 전멸은 웨이브 종료 조건이 아니다. 양측 방어선 파괴 기반 승리·실패만 유지한다.
- 핀볼 골드, 콤보, 잭팟, 전술 증원권 최대 1개와 성공 시 소비 규칙을 유지한다.
- 전사 4초, 궁수 5초, 마법사 7초, 창병 5초 쿨다운은 Spawn 성공 뒤에만 시작한다.
- 실패한 구매는 골드, 구매 횟수, 증원권과 쿨다운을 바꾸지 않는다.
- 창병 비용은 35골드, 증가 배율은 1.4이며 생존 아군 상한은 5명으로 유지한다.
- 전선 위험 UI, 런타임 UI 자동 생성, 새 외부 패키지, 관련 없는 리팩터링은 제외한다.
- `[SerializeField]` 이름에 underscore를 쓰지 않는다. 씬 배치·Inspector 참조와 `SetActive` 풀링을 우선한다.
- `Rabbit1_Mage_Attack.anim`, `ArcaneVfxCatalog.asset`의 기존 사용자 변경은 수정·스테이징·커밋하지 않는다.

---

## 현재 구조 대조 결과

- `BattleManager.TryStartWave()`는 `UnitManager.BeginWave()`를 호출하며 현재 웨이브 적 전체를 즉시 생성한다. 방어선 파괴만 `BeginWaveResolution()`을 호출하므로 적 전멸 기반 종료는 이미 제거돼 있다.
- `UnitManager`는 아군 상한 5명과 `SetActive` 기반 `UnitSpawner` 풀을 사용한다. 적 생성은 private `SpawnEnemy()`를 거치며 생존 적 수는 `RemainingEnemyCount`로 조회 가능하다.
- `UnitPurchaseController`는 전사·궁수·마법사의 비용과 구매 횟수를 독립 관리한다. Spawn 성공 후 결제·횟수 증가가 이뤄지고 무료 출격 실패 시 증원권은 유지된다. 쿨다운 상태는 아직 없다.
- `AllyPurchasePanelController`와 `02. Game.unity`에는 텍스트형 구매 카드 3개만 연결돼 있다. `spearman` 데이터, `AllyUnit.prefab` 시각 분기, 초상화 매핑과 진화 계보는 이미 존재한다.
- `StatusPanel`에는 기회·골드·양측 방어선·10웨이브 HUD가 있다. 공세 텍스트 참조는 없다.
- `GameSpeedController`는 Active 상태의 2배속만 `Time.timeScale = 2`로 적용한다. 공세와 쿨다운에 `Time.deltaTime`을 전달하면 일시정지·2배속 규칙을 별도 시계 없이 따른다.
- `BattleWaveData.json`은 10개 웨이브의 즉시 생성 조합만 가진다. `TitleData.ValidateBattleWaves()`는 적 ID·수량과 10웨이브·최종 보스만 검증한다.

## 밸런스 초깃값

### 적용 원칙

- 기존 웨이브 이름, 엘리트/보스 표식, 기존 적 종류와 수량은 초기 공세에서 그대로 유지한다.
- 초기 공세 마지막 생성은 10초 이내로 끝낸다. 첫 기본 증원은 초기 공세 종료 후 해당 기본 간격만큼 뒤에 시도한다.
- 반복 간격은 공세 시작 시각 기준이다. 시도 시 빈 슬롯만 채우고 나머지는 폐기한다.
- 웨이브 1~4는 기존 고블린 계열 학습을 유지한다. 웨이브 5부터 현재 데이터에 이미 존재하지만 기존 시간표에서 쓰지 않던 `wolf`, `shaman`, `assassin`, `troll`, `ogre_elite`, `dark_mage_elite`를 단계적으로 증원에만 도입한다.
- 웨이브 10의 `goblin_king`은 초기 공세에서 한 번만 생성한다. 반복 증원은 부하만 사용해 보스 중복을 막는다.

### 초기 공세 시간표

| 웨이브 | 초기 공세 항목 `(첫 시각 / 개별 간격)` |
|---|---|
| 1 로그 정찰대 | `goblin x3 (0초 / 1.5초)` |
| 2 로그 습격대 | `goblin x4 (0초 / 1.5초)` |
| 3 궁수 합류 | `goblin x3 (0초 / 1.5초)`, `goblin_archer x1 (3초 / 0초)` |
| 4 원거리 지원 | `goblin x3 (0초 / 1.5초)`, `goblin_archer x2 (2초 / 2초)` |
| 5 메이스 전사 | `goblin x2 (0초 / 1.5초)`, `goblin_archer x2 (2초 / 2초)`, `shield_guard x1 (4초 / 0초)` |
| 6 방패 진형 | `goblin x2 (0초 / 1.5초)`, `goblin_archer x1 (2초 / 0초)`, `shield_guard x2 (3초 / 2초)` |
| 7 검방 전사 등장 | `goblin x2 (0초 / 1.5초)`, `goblin_archer x1 (2초 / 0초)`, `shield_guard x1 (3초 / 0초)`, `orc_warrior x1 (5초 / 0초)` |
| 8 혼성 부대 | `goblin x1 (0초 / 0초)`, `goblin_archer x2 (1초 / 2초)`, `shield_guard x1 (3초 / 0초)`, `orc_warrior x1 (5초 / 0초)` |
| 9 전사 연합 | `goblin x1 (0초 / 0초)`, `goblin_archer x1 (1초 / 0초)`, `shield_guard x1 (2초 / 0초)`, `orc_warrior x2 (4초 / 2초)` |
| 10 기마 마도사 | `goblin x1 (0초 / 0초)`, `goblin_archer x1 (1초 / 0초)`, `shield_guard x1 (2초 / 0초)`, `goblin_king x1 (5초 / 0초)` |

### 반복 증원 조합과 간격

| 웨이브 | 기본 증원 `<60초` | 강화 증원 `60~90초` | 최후 공세 `>=90초` |
|---|---|---|---|
| 1 | `goblin x2 / 12초` | `goblin x2 + wolf x1 / 10초` | `goblin x2 + wolf x2 / 8초` |
| 2 | `goblin x3 / 12초` | `goblin x2 + wolf x2 / 10초` | `goblin x2 + goblin_archer x1 + wolf x2 / 8초` |
| 3 | `goblin x2 + goblin_archer x1 / 12초` | `goblin x2 + goblin_archer x1 + wolf x1 / 10초` | `goblin x2 + goblin_archer x2 + wolf x1 / 8초` |
| 4 | `goblin x2 + goblin_archer x2 / 12초` | `shield_guard x1 + goblin x2 + goblin_archer x1 / 10초` | `shield_guard x1 + goblin x2 + goblin_archer x2 + wolf x1 / 8초` |
| 5 | `goblin x1 + goblin_archer x1 + shield_guard x1 / 11초` | `orc_warrior x1 + shield_guard x1 + goblin_archer x1 + goblin x1 / 9초` | `orc_warrior x1 + shield_guard x2 + goblin_archer x1 + wolf x1 / 8초` |
| 6 | `goblin x1 + goblin_archer x1 + shield_guard x2 / 11초` | `orc_warrior x1 + shield_guard x2 + goblin_archer x1 / 9초` | `troll x1 + shield_guard x1 + orc_warrior x1 + goblin_archer x1 / 8초` |
| 7 | `orc_warrior x1 + shield_guard x1 + goblin_archer x1 / 10초` | `orc_warrior x1 + shield_guard x1 + shaman x1 + wolf x1 / 9초` | `troll x1 + orc_warrior x1 + shaman x1 + assassin x1 / 8초` |
| 8 | `orc_warrior x1 + shield_guard x1 + goblin_archer x2 / 10초` | `troll x1 + orc_warrior x1 + shaman x1 + assassin x1 / 9초` | `troll x1 + orc_warrior x1 + shaman x1 + assassin x2 / 8초` |
| 9 | `orc_warrior x2 + shield_guard x1 + shaman x1 / 10초` | `ogre_elite x1 + orc_warrior x1 + shaman x1 + assassin x1 / 9초` | `ogre_elite x1 + dark_mage_elite x1 + orc_warrior x1 + assassin x1 / 8초` |
| 10 | `shield_guard x1 + orc_warrior x1 + goblin_archer x1 / 10초` | `troll x1 + shield_guard x1 + shaman x1 + assassin x1 / 9초` | `ogre_elite x1 + dark_mage_elite x1 + orc_warrior x1 + assassin x1 / 7초` |

이 값은 기능 검증용 초깃값이다. 특히 9~10웨이브 최후 공세는 적 웨이브 배율과 정예 스킬이 함께 적용되므로 WebGL 실기 플레이 후 간격만 우선 조정한다. 적 능력치는 변경하지 않는다.

---

### Task 1: 웨이브 데이터 계약과 검증

**Files:**
- Modify: `Assets/02. Scripts/Battle/BattleDataTypes.cs`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleWaveScheduleDataTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleWaveScheduleDataTests.cs.meta`

**Interfaces:**
- Produces: `BattleTimedSpawnData`, `BattleReinforcementGroupData`, `BattleWaveData.InitialAssault`, `BasicReinforcement`, `EmpoweredReinforcement`, `FinalAssault`
- Validation: 모든 ID가 `EnemyUnit`에 존재, 수량 `>=1`, 첫 시각·간격·반복 간격 `>=0`, 마지막 초기 생성 시각 `<60f`, 각 반복 조합이 비어 있지 않음

- [ ] 기존 JSON을 읽는 characterization test를 먼저 작성해 10웨이브, 보스, 기존 조합 보존을 고정한다.
- [ ] 새 필드가 없는 임시 JSON을 역직렬화했을 때 검증이 실패하는 test를 작성한다.
- [ ] 잘못된 적 ID, 음수 시각, 60초 이상 마지막 초기 생성, 빈 반복 조합을 각각 거부하는 test를 작성한다.
- [ ] test 실패를 확인한 뒤 직렬화 타입과 `TitleData.ValidateBattleWaves()` 검증을 최소 추가한다.
- [ ] 관련 test를 다시 실행해 통과를 확인한다.

### Task 2: 순수 공세 스케줄러

**Files:**
- Create: `Assets/02. Scripts/Battle/Runtime/BattleAssaultController.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/BattleAssaultController.cs.meta`
- Create: `Assets/02. Scripts/Battle/Editor/BattleAssaultControllerTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleAssaultControllerTests.cs.meta`

**Interfaces:**
- Produces: `EBattleAssaultPhase { Initial, Basic, Empowered, Final }`
- Produces: `Start(BattleWaveData wave)`, `Advance(float deltaTime, int aliveEnemyCount, Func<string, bool> trySpawn)`, `Stop()`, `ElapsedTime`, `Phase`, `IsRunning`
- Event: `PhaseChanged(EBattleAssaultPhase phase)`는 60초와 90초 경계를 각각 한 번만 알린다.

- [ ] 0초 항목과 항목별 첫 시각·개별 간격이 정확히 한 번씩 요청되는 failing test를 작성한다.
- [ ] 초기 종료 후 기본 조합 반복, 60초 강화 전환, 90초 최후 전환 failing test를 작성한다.
- [ ] 생존 적 8명일 때 요청 없음, 빈자리 발생 후 다음 예약 시점에만 재개, 누락 수량 미누적 failing test를 작성한다.
- [ ] Spawn callback 실패를 같은 frame에 재시도하지 않는 test와 `Stop()` 뒤 요청 없음 test를 작성한다.
- [ ] 큰 `deltaTime`으로 경계를 건너도 단계 이벤트가 중복되지 않고 과거 반복을 한 frame에 몰아 생성하지 않는 test를 작성한다.
- [ ] 최소 상태 머신과 예약 인덱스만 구현하고 모든 test를 통과시킨다.

### Task 3: 기존 풀링 생성 경로와 BattleManager 연결

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/WaveResolutionTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitPoolResetTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleAssaultIntegrationTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/BattleAssaultIntegrationTests.cs.meta`

**Interfaces:**
- `UnitManager.PrepareWave()`는 이전 적을 풀로 반환하고 spawn index를 초기화한다.
- `UnitManager.TrySpawnScheduledEnemy(string enemyId, int waveNumber)`는 기존 `SpawnEnemy()`를 사용하고 성공 여부를 반환한다.
- `UnitManager.StartPreparedWave()`는 준비된 아군·적에게 `StartBattle()`을 호출한다.
- `BattleManager` exposes read-only `AssaultElapsedTime`, `AssaultPhase`, `GetAssaultCountdownText()` and event `OnAssaultPhaseChanged`.

- [ ] 웨이브 시작 시 초기 0초 적만 생성되고 이후 `Advance`에서 예약 적이 생기는 integration failing test를 작성한다.
- [ ] 적이 0명이 되어도 Active 유지, 양측 방어선 파괴만 기존 결과 처리로 진입하는 regression test를 보강한다.
- [ ] `BeginWaveResolution()` 직전에 공세를 중지해 resolving 이후 적이 생성되지 않는 test를 작성한다.
- [ ] 재도전·다음 웨이브에서 `ElapsedTime=0`, 초기 예약 재생성 test를 작성한다.
- [ ] `BattleManager.Update()`에서 Active일 때만 `Time.deltaTime`을 공세 컨트롤러에 전달하고 풀링 생성 callback을 연결한다.
- [ ] 기존 방어선·웨이브·풀 reset test와 새 integration test를 모두 통과시킨다.

### Task 4: 유닛별 독립 출격 쿨다운과 창병 구매 도메인

**Files:**
- Modify: `Assets/02. Scripts/Battle/Runtime/UnitPurchaseController.cs`
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitPurchaseControllerTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/TacticalReinforcementControllerTests.cs`

**Interfaces:**
- `UnitPurchaseSettings(string unitId, int baseCost, float costMultiplier, float cooldownSeconds)`
- `UnitPurchaseController.Advance(float deltaTime)`, `GetRemainingCooldown(string unitId)`, `IsCoolingDown(string unitId)`
- `CanPurchase`와 `CanPurchaseFree`는 쿨다운을 최우선 차단한다.
- `BattleManager.GetAllyRemainingCooldown(string unitId)`와 `IsAllyCoolingDown(string unitId)`가 UI 조회점을 제공한다.

- [ ] 전사 4초, 궁수 5초, 마법사 7초, 창병 5초가 성공 직후 시작되고 독립 감소하는 failing test를 작성한다.
- [ ] 쿨다운 중 일반·무료 출격 차단 test를 작성한다.
- [ ] 골드 부족, 상한, 잘못된 ID, Spawn 실패에서 골드·횟수·쿨다운이 불변인 test를 작성한다.
- [ ] 무료 창병 Spawn 실패 시 증원권 유지, 성공 시 증원권 소비·구매 횟수 증가·5초 시작 test를 작성한다.
- [ ] `BattleManager.Update()`가 초기화 후 모든 웨이브 상태에서 `Time.deltaTime`으로 쿨다운을 감소시키도록 최소 연결한다. 새 런에서 controller 생성으로만 전체 초기화한다.
- [ ] `spearmanPurchaseSettings = new("spearman", 35, 1.4f, 5f)`와 기존 3종 쿨다운 값을 Inspector 기본값 및 controller 생성에 연결한다.
- [ ] 구매·전술 증원 test 전체를 통과시킨다.

### Task 5: 구매 UI 4카드와 쿨다운 표시

**Files:**
- Modify: `Assets/02. Scripts/03. UI/AllyPurchasePanelController.cs`
- Modify: `Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- 각 카드 Inspector 참조: `Button`, 본문 `TextMeshProUGUI`, portrait `Image`, cooldown mask `Image`, cooldown `TextMeshProUGUI`.
- `FormatCard(...)`는 역할·보유 수·유료/무료 상태를 유지한다.
- `FormatCooldown(float)`는 남은 시간을 올림해 `4`, `3`, ..., `1`로 표시하고 0에서 빈 문자열을 반환한다.
- 차단 우선순위: cooldown, 생존 아군 5명, 골드 부족. 쿨다운이면 mask 활성화와 버튼 비활성화가 무료권보다 우선한다.

- [ ] 4개 카드의 모든 참조와 `spearman` 초상화를 검사하는 scene failing test를 작성한다.
- [ ] 쿨다운 올림 표시와 무료권/쿨다운 우선순위 failing test를 작성한다.
- [ ] controller에 창병 클릭, 4개 카드 갱신, 쿨다운 mask 표시를 최소 추가한다.
- [ ] 씬의 기존 패널 폭 안에서 4개 카드를 84px씩 배치하고 기존 3개 카드에도 portrait·mask·cooldown text를 씬 오브젝트로 추가한다. 네 번째 카드에는 기존 `ui_character_portrait_04_dog_lancer`를 연결한다.
- [ ] 4개 카드가 WebGL 기준 해상도에서 겹치지 않고 Inspector 참조가 모두 존재하는지 scene test로 확인한다.

### Task 6: 공세 카운트다운 HUD와 단계 피드백

**Files:**
- Create: `Assets/02. Scripts/03. UI/AssaultCountdownFormatter.cs`
- Create: `Assets/02. Scripts/03. UI/AssaultCountdownFormatter.cs.meta`
- Create: `Assets/02. Scripts/03. UI/Editor/AssaultCountdownFormatterTests.cs`
- Create: `Assets/02. Scripts/03. UI/Editor/AssaultCountdownFormatterTests.cs.meta`
- Modify: `Assets/02. Scripts/03. UI/StatusPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs`
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- `AssaultCountdownFormatter.Format(float elapsedTime)` returns `강화 증원까지 00:SS`, `최후 공세까지 00:SS`, or `최후 공세 진행 중`.
- `StatusPanel` receives one scene-placed `TextMeshProUGUI assaultCountdownText`; visibility follows `EWaveState.Active` only.

- [ ] 0, 0.01, 59, 59.99, 60, 89.99, 90초 경계와 올림을 고정하는 failing test를 작성한다.
- [ ] 준비·Resolving·승리·패배에서 숨고 Active에서만 보이는 state test를 작성한다.
- [ ] `StatusPanel`이 `BattleManager.AssaultElapsedTime`만 읽어 text를 갱신하게 구현한다.
- [ ] 60초와 90초 `OnAssaultPhaseChanged`에서 기존 UI 색상 피드백 방식으로 짧게 강조하고, 90초에 기존 `SoundName.BossWind`를 한 번만 재생한다.
- [ ] 상태 HUD에 공세 Text를 씬 배치하고 Inspector 참조 및 비활성 초기 상태를 scene test로 확인한다.

### Task 7: 10웨이브 JSON 적용

**Files:**
- Modify: `Assets/Resources/Data/BattleWaveData.json`
- Modify: `Assets/02. Scripts/Battle/Editor/BattleWaveScheduleDataTests.cs`

**Interfaces:**
- Task 1 데이터 계약을 사용한다.
- 이 문서의 초기 공세와 세 반복 조합을 정확히 직렬화한다.

- [ ] 위 밸런스 표를 기대값으로 검사하는 failing test를 작성한다.
- [ ] 기존 `Enemies` 필드를 새 `InitialAssault`로 옮기되 이름·엘리트·보스·초기 총수량을 보존한다.
- [ ] 10개 웨이브의 기본·강화·최후 조합과 간격을 입력한다.
- [ ] 모든 적 ID, 마지막 초기 시각 `<60초`, 보스 1회, 조합 비어 있지 않음을 test로 확인한다.

### Task 8: 회귀 검증과 기록

**Files:**
- Create: `docs/ai-usage/2026-08-25/2026-08-25-timed-reinforcement-cooldown-spearman-implementation-ai-usage.md`

**Verification:**
- 관련 EditMode: 공세 controller/integration, 구매, 전술 증원, 데이터, UI formatter, scene reference, 방어선, 웨이브 resolution, 풀 reset.
- C# compile: Unity batchmode EditMode 실행 로그에서 compiler error 0건 확인.
- Scene: `02. Game.unity`를 열어 missing script/reference 0건, 4개 카드, cooldown mask, 공세 HUD 확인.
- PlayMode 수동: 1배속에서 60/90초 경계, 2배속에서 실제 절반 wall-clock, pause 시 정지; 적 상한 8; 적 전멸 후 Active 유지; 양측 방어선 결과; 실패 구매 불변; 무료 창병 성공; 핀볼 골드·콤보·잭팟 유지.
- WebGL: 아군 5명+적 8명에서 UI 가독성과 프레임 체감 확인.

- [ ] 관련 EditMode test를 Unity batchmode로 실행하고 XML·로그 결과를 기록한다.
- [ ] Unity C# compile error와 scene missing reference를 검사한다.
- [ ] 가능한 수동 PlayMode/WebGL 검증을 실행하고 실행하지 못한 항목은 제한점으로 명시한다.
- [ ] `git diff --check`, `git status --short`, 변경 파일 목록을 확인한다.
- [ ] 사용자 소유 asset 2개가 변경 전 상태 그대로이며 작업 diff에 섞이지 않았는지 확인한다.
- [ ] AI 도구/모델, 요청, 제안, 실제 수정, 사용자 결정, 중요 지시, 검증 결과와 제한점을 AI 활용 기록에 사실대로 작성한다.
- [ ] 최종 보고에 변경 파일, 변경 이유, 검증 결과, 직접 확인 절차와 제한점을 포함한다.

## 자체 검토 결과

- 설계의 초기·기본·강화·최후 공세, 8명 상한, 비누적 누락, 결과 중지, 시간 배율, 4종 쿨다운, 실패 원자성, 무료권, 창병, UI, scene 참조와 회귀 검증을 각각 Task 1~8에 연결했다.
- 공개 API 변경은 `BattleManager`의 읽기 전용 조회와 이벤트, `UnitManager`의 좁은 생성 진입점에 한정한다.
- 새 외부 패키지, 새 최상위 폴더, 런타임 UI 생성, 적 능력치 변경과 관련 없는 리팩터링은 없다.
- 구현 중 실제 코드 구조가 이 계획과 충돌하면 임의 확장하지 않고 사용자에게 차이를 보고한 뒤 승인 범위를 재확인한다.
