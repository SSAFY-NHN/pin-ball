# 능동형 방치 핀볼 자동 순환 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 씬에 배치된 공을 상단에서 자동 생성하고, 하단 종료 지점 또는 Goal에서 회수한 뒤 공별 대기시간 후 재생성하며, 큰 범퍼 기본 골드는 유지하고 Goal의 아군 생성은 제거한다.

**Architecture:** `PinballManager`는 Unity 생명주기와 Inspector 참조만 조율한다. `PinballBallPool`은 씬 사전 배치 공의 available/active 상태를 관리하고, 새 `PinballAutoCycleController`는 회수된 공별 재생성 만료 시각을 관리한다. 공 활성화와 회수는 계속 `SetActive`를 사용한다.

**Tech Stack:** Unity 6.0.0.79f1, C#, Unity Test Framework/NUnit, Rigidbody2D

## Global Constraints

- 구현 1단계만 수행하며 생산 업그레이드 UI, 공 추가 구매, 황금 공, 잭팟, 자동전투·유닛 구매·보스·오프라인 보상은 구현하지 않는다.
- 씬 사전 배치와 Inspector 참조를 사용하고 런타임 공 생성은 사용하지 않는다.
- `[SerializeField]` 이름에는 underscore를 사용하지 않는다.
- 기존 핀볼 물리, 범퍼 피드백, 콤보, 전투와 관련 없는 기능을 변경하지 않는다.
- Goal 진입 시 기존 연출은 유지하지만 아군 생성과 Goal 전투 보상은 실행하지 않는다.
- 외부 패키지, 공개 API 변경, 파일 이동, 대규모 리팩터링을 하지 않는다.

---

### Task 1: 자동 순환 상태를 테스트로 정의

**Files:**
- Create: `Assets/02. Scripts/Pinball/Editor/PinballAutoCycleControllerTests.cs`
- Create: `Assets/02. Scripts/Pinball/PinballAutoCycleController.cs`

**Interfaces:**
- Produces: `Schedule(Pinball ball, float readyAt)`, `TryTakeReady(float currentTime, out Pinball ball)`, `Reset()`

- [ ] 회수된 공이 만료 전에는 나오지 않고 만료 시 정확히 한 번 나오는 실패 테스트를 작성한다.
- [ ] 서로 다른 만료 시각을 가진 두 공이 독립적으로 나오는 실패 테스트를 작성한다.
- [ ] 동일 공 중복 예약과 `Reset()` 후 잔여 예약이 없는 실패 테스트를 작성한다.
- [ ] `PinballAutoCycleControllerTests`를 실행해 타입 부재로 실패하는지 확인한다.
- [ ] Dictionary 기반 최소 구현을 추가한다. 예약은 공별 하나만 유지하고 `TryTakeReady` 성공 시 예약에서 제거한다.
- [ ] 대상 테스트를 다시 실행해 통과시킨다.

### Task 2: Pool이 특정 회수 공을 다시 활성 상태로 전환

**Files:**
- Create: `Assets/02. Scripts/Pinball/Editor/PinballBallPoolTests.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballBallPool.cs`

**Interfaces:**
- Consumes: 씬 배치 `IEnumerable<Pinball>`
- Produces: 기존 `TryAcquireActive(out Pinball ball)`과 새 `TryReactivate(Pinball ball)`

- [ ] 최초 대여된 공이 active가 되고 회수 후 available로 즉시 재대여되지 않는 실패 테스트를 작성한다.
- [ ] 회수된 특정 공만 `TryReactivate`로 active가 되는 실패 테스트를 작성한다.
- [ ] 활성 공·외부 공·이미 재활성화된 공의 중복 전이를 거부하는 실패 테스트를 작성한다.
- [ ] 테스트를 실행해 기대한 상태 전이에서 실패하는지 확인한다.
- [ ] `Release`가 공을 비활성화하되 available queue에 즉시 넣지 않도록 최소 수정하고 `TryReactivate`를 추가한다.
- [ ] Pool 테스트를 실행해 통과시킨다.

### Task 3: PinballManager를 자동 생성과 재생성에 연결

**Files:**
- Modify: `Assets/02. Scripts/Pinball/PinballManager.cs`
- Modify: `Assets/02. Scripts/Pinball/PinballRewardController.cs`

**Interfaces:**
- Inspector: `Transform autoSpawnPoint`, `float respawnDelay`, `Vector2 autoSpawnDirection`
- Consumes: `PinballAutoCycleController`, `PinballBallPool.TryAcquireActive`, `PinballBallPool.TryReactivate`

- [ ] `PinballManager`에 상단 생성 위치, 재생성 대기시간, 아래쪽 생성 방향을 직렬화한다.
- [ ] 초기화/새 런 Reset에서 예약을 비우고 공 한 개를 즉시 상단 활성화한다.
- [ ] `Update()`에서 scaled `Time.time` 기준으로 만료된 공을 재활성화한다.
- [ ] `ReleaseBall`에서 마지막 공 플런저 적재 대신 해당 공의 독립 재생성 시간을 예약한다.
- [ ] 수동 `TryLaunchLoadedBall`은 자동 모드에서 공을 추가 발사하지 못하도록 기존 LoadedBall을 만들지 않는다.
- [ ] `OnGoalBall`에서 `OnGoalReached` 호출과 `ApplyGoalReward` 호출을 제거하고 회수만 수행한다.
- [ ] 더 이상 쓰이지 않는 Goal 아군 보상 의존성을 `PinballRewardController`에서 제거하되 범퍼 보상과 Split Capsule은 유지한다.
- [ ] 컴파일과 대상 테스트를 실행한다.

### Task 4: Game 씬 Inspector 연결

**Files:**
- Modify: `Assets/01. Scenes/02. Game.unity`

**Interfaces:**
- Produces: 씬 배치 `AutoBallSpawnPoint` → `PinballManager.autoSpawnPoint`

- [ ] 핀볼 보드 상단의 안전한 위치에 `AutoBallSpawnPoint` 빈 오브젝트를 배치한다.
- [ ] `PinballManager`에 spawn point, 재생성 대기시간 `3`, 방향 `(0, -1)`을 연결한다.
- [ ] 기존 `pooledBalls` 한 개, OutZone 두 개, Goal과 범퍼 참조가 유지되는지 씬 직렬화를 검사한다.
- [ ] Unity batch compile로 Missing Script와 직렬화 오류가 없는지 확인한다.

### Task 5: 회귀 검증과 기록

**Files:**
- Create: `docs/ai-usage/2026-08-17/2026-08-17-active-idle-pinball-cycle-ai-usage.md`

- [ ] `PinballAutoCycleControllerTests`와 `PinballBallPoolTests`를 실행한다.
- [ ] 전체 EditMode 테스트를 실행한다.
- [ ] Game 씬을 batch로 열어 컴파일·직렬화 오류가 없는지 확인한다.
- [ ] 직접 플레이 기준을 기록한다: 입력 없는 즉시 생성, OutZone/Goal 회수, 3초 뒤 재생성, 큰 범퍼 기본 골드, Goal 아군 미생성, 물리·VFX·SFX·콤보 유지.
- [ ] AI 도구, 요청, 제안, 실제 수정, 사용자 결정, 테스트 결과와 직접 확인 필요 항목을 사실대로 기록한다.
- [ ] `git diff --check`와 `git status --short`로 최종 변경 범위를 확인한다.
