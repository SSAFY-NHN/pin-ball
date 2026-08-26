# 60초 전투 준비 제한 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 경제와 핀볼 동작을 유지하면서 준비 단계가 60초 후 자동으로 전투를 시작하게 한다.

**Architecture:** 순수 C# 카운트다운 객체가 시간 상태를 소유하고 `BattleManager`가 Unity 프레임과 웨이브 상태를 연결한다. 기존 `WavePanel` 시작 버튼은 남은 시간을 함께 표시한다.

**Tech Stack:** Unity, C#, NUnit EditMode tests, TextMesh Pro

**Spec:** `docs/designs/2026-08-26/2026-08-26-preparation-countdown-design.md`

## Global Constraints

- 준비 제한은 정확히 60초다.
- Gold, 핀볼 생산, 구매·강화 가능 단계와 아군 제거 정책은 변경하지 않는다.
- 기존 시작 버튼을 통한 조기 시작을 유지한다.
- 새 외부 패키지를 추가하지 않는다.

---

### Task 1: 준비 카운트다운 도메인

**Files:**
- Create: `Assets/02. Scripts/Battle/Runtime/PreparationCountdown.cs`
- Test: `Assets/02. Scripts/Battle/Editor/PreparationCountdownTests.cs`

**Interfaces:**
- Produces: `PreparationCountdown(float duration)`, `RemainingTime`, `Reset()`, `Advance(float deltaTime): bool`

- [x] 실패 테스트로 초기값, 감소, 0 고정, 단일 만료 신호를 정의한다.
- [x] 해당 EditMode 테스트 실행을 시도하고 Unity 라이선스 초기화 제한을 기록한다.
- [x] 최소 카운트다운 구현을 추가한다.
- [ ] Unity 라이선스가 정상인 환경에서 해당 EditMode 테스트 통과를 확인한다.

### Task 2: BattleManager 자동 시작 연결

**Files:**
- Modify: `Assets/02. Scripts/Battle/BattleManager.cs`
- Test: `Assets/02. Scripts/Battle/Editor/PreparationCountdownBattleManagerTests.cs`

**Interfaces:**
- Consumes: `PreparationCountdown.Advance(float): bool`
- Produces: `PreparationRemainingTime`, `AdvancePreparationCountdown(float)`

- [x] `Pending`에서만 감소하고 만료 시 기존 시작 경로를 한 번 호출하는 실패 테스트를 작성한다.
- [x] 대상 테스트 실행을 시도하고 Unity 라이선스 초기화 제한을 기록한다.
- [x] 새 런 및 `Pending` 재진입 초기화와 `Update` 연결을 최소 구현한다.
- [ ] Unity 라이선스가 정상인 환경에서 대상 테스트 통과를 확인한다.

### Task 3: 시작 버튼 남은 시간 표시

**Files:**
- Modify: `Assets/02. Scripts/03. UI/WavePanel.cs`
- Test: `Assets/02. Scripts/03. UI/Editor/WavePanelTests.cs`

**Interfaces:**
- Consumes: `BattleManager.PreparationRemainingTime`
- Produces: `WavePanel.FormatStartButtonLabel(float): string`

- [x] 60, 59, 1, 0초 경계 표시 실패 테스트를 작성한다.
- [x] 대상 테스트 실행을 시도하고 Unity 라이선스 초기화 제한을 기록한다.
- [x] 기존 버튼 TMP 문구를 매 초 `전투 시작 (N)`으로 갱신한다.
- [ ] Unity 라이선스가 정상인 환경에서 대상 테스트 통과를 확인한다.

### Task 4: 회귀 검증

**Files:**
- Verify: `Assets/02. Scripts/Battle/Editor`
- Verify: `Assets/02. Scripts/03. UI/Editor`

- [ ] 전체 EditMode 테스트를 실행해 기존 구매, Gold 유지와 웨이브 결과 동작이 보존됐는지 확인한다. Unity LicensingClient 연결 실패로 보류.
- [ ] Unity C# 컴파일 로그에 오류가 없는지 확인한다. Unity LicensingClient 연결 실패로 보류.
- [x] 변경 diff에서 Gold 초기화, 핀볼 상태 변경과 무관한 수정이 없는지 확인한다.
