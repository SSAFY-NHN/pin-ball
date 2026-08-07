# Battle and Skill Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 데이터 테이블과 분리된 노드 스킬 런타임, 안전한 전투 상태, 수동 합성·승급·결과 UI를 구현하고 Unity 프로젝트 정합성을 복구한다.

**Architecture:** 현재 JSON은 V1 Adapter를 통해 런타임 `SkillGraph`로 변환하고, `UnitSkillController`가 이벤트·조건·대상·효과 노드를 실행한다. Manager/UI 상태는 UniRx 읽기 전용 스트림으로 전달하고 지연 작업은 UniTask와 CancellationToken으로 관리한다. 구현은 아래 네 계획을 순서대로 실행하며 각 계획 종료 시 컴파일·테스트·커밋한다.

**Tech Stack:** Unity 6000.0.79f1, C#, UniRx, UniTask, Unity Test Framework 1.6.0, URP 17.0.4, PC WebGL

## Global Constraints

- 기존 폴더·파일명과 사용자 수정 스타일을 유지한다. `AlllyUnit.cs`의 파일명은 이번 범위에서 변경하지 않는다.
- 새로운 최상위 폴더, 외부 패키지, 비주얼 노드 에디터를 추가하지 않는다.
- `[SerializeField]` 필드 이름에 underscore를 사용하지 않는다.
- 핵심 UI와 컴포넌트는 `02. Game` 씬 및 프리팹에 미리 배치하고 Inspector 참조를 연결한다.
- `ItemManager`의 전용 아이템 구독 구조는 유지한다.
- 웨이브 테이블·아이템 아이콘·Title 씬 기능은 수정하지 않는다.
- 아군 전멸 후 HP가 남으면 생존 적을 제거한 뒤 `Pending`으로 간다.
- 핀볼·상점·배치·합성은 `Pending`에서만 허용한다.
- 합성은 같은 직업·같은 레벨 두 유닛을 직접 드래그할 때만 수행한다.
- 5레벨 도달 즉시 취소 불가능한 승급 선택 UI를 표시한다.
- 각 작업은 실패 테스트 → 최소 구현 → 통과 확인 → 커밋 순서를 지킨다.

---

## Execution Order

1. [Reactive and Async Foundation](2026-08-07-01-reactive-async-foundation.md)
2. [Node Skill Runtime](2026-08-07-02-skill-graph-runtime.md)
3. [Battle Preparation, Merge, and Results](2026-08-07-03-battle-preparation-merge-results.md)
4. [Unity Project Stabilization](2026-08-07-04-unity-project-stabilization.md)

각 계획은 앞 계획의 공개 인터페이스를 전제로 한다. 순서를 바꾸거나 여러 계획을 동시에 실행하지 않는다.

## Spec Coverage

| Approved requirement | Owning plan/task |
|---|---|
| Battle/Pinball/HP/Gold/Wave UniRx state | Plan 01 Tasks 1–2 |
| Project coroutine removal with UniTask | Plan 01 Task 3 |
| Adapter-isolated node skill runtime | Plan 02 Tasks 1–3 |
| Current 20 skill migration and ID-branch removal | Plan 02 Tasks 4–5 |
| Ally wipe priority, breach damage, enemy cleanup, Pending/Defeat | Plan 03 Task 1 |
| Pending-only preparation and frozen combat time | Plan 03 Task 2 |
| Manual same-ID/same-level merge and level-5 promotion | Plan 03 Tasks 3–4 |
| Preplaced promotion/result UI and Title button | Plan 03 Tasks 5–6 |
| Reinforcement position and Developer scene typo | Plan 04 Task 1 |
| Unity 6000.0.79f1 + URP 17.0.4 compatibility | Plan 04 Task 2 |
| Exact obsolete prefab deletion | Plan 04 Task 3 |
| Automated tests, scene import, WebGL build, AI record | Plan 04 Tasks 4–6 |

## Locked File Structure

### 새 런타임 파일

- `Assets/02. Scripts/02. Data/SkillGraphData.cs`: 현재 JSON용 공유 스킬·노드 DTO
- `Assets/02. Scripts/Battle/SkillGraph.cs`: 테이블과 무관한 런타임 그래프 모델
- `Assets/02. Scripts/Battle/SkillGraphValidator.cs`: 링크·필수 값·순환 검증
- `Assets/02. Scripts/Battle/SkillGraphV1Adapter.cs`: 현재 DTO를 런타임 그래프로 변환
- `Assets/02. Scripts/Battle/UnitSkillController.cs`: 그래프 상태와 실행 순서 관리
- `Assets/02. Scripts/Battle/SkillNodeController.cs`: 조건·대상·효과 노드 실행
- `Assets/02. Scripts/Battle/BattlePhaseRules.cs`: 상태별 입력과 패배 계산 규칙
- `Assets/02. Scripts/Battle/AllyMergeRules.cs`: 합성 가능 여부의 순수 규칙
- `Assets/02. Scripts/Battle/AllyDragController.cs`: 월드 유닛 드래그 입력
- `Assets/02. Scripts/03. UI/PromotionPanel.cs`: 승급 선택 모달
- `Assets/02. Scripts/03. UI/BattleResultPanel.cs`: 승리·패배 결과 모달

### 새 테스트 파일

- `Assets/02. Scripts/Editor/Tests/ReactiveManagerTests.cs`
- `Assets/02. Scripts/Editor/Tests/AsyncPolicyTests.cs`
- `Assets/02. Scripts/Editor/Tests/SkillGraphValidatorTests.cs`
- `Assets/02. Scripts/Editor/Tests/SkillGraphAdapterTests.cs`
- `Assets/02. Scripts/Editor/Tests/SkillRuntimeTests.cs`
- `Assets/02. Scripts/Editor/Tests/BattlePhaseRulesTests.cs`
- `Assets/02. Scripts/Editor/Tests/AllyMergeRulesTests.cs`
- `Assets/02. Scripts/Editor/Tests/ProjectConfigurationTests.cs`

### 주요 수정 파일

- 데이터: `AllyUnitData.cs`, `EnemyUnitData.cs`, `TitleData.cs`, 아군·적군 JSON
- 전투: `BattleManager.cs`, `UnitManager.cs`, `UnitSpawner.cs`, `UnitBase.cs`, `AlllyUnit.cs`, `EnemyUnit.cs`, `BattleDataTypes.cs`
- 상태/UI: `PinballManager.cs`, `PinballGoal.cs`, `UIManager.cs`, `UIBase.cs`, `WavePanel.cs`, `StatusPanel.cs`, `ShopPanel.cs`
- 비동기: `ItemManager.cs`
- 씬/프리팹: `02. Game.unity`, `AllyUnit.prefab`
- 프로젝트: `SceneManager.cs`, `Packages/manifest.json`, `Packages/packages-lock.json`, `UniversalRenderPipelineGlobalSettings.asset`

## Final Acceptance

- EditMode 테스트 전체 통과
- Game 씬 진입 및 대표 전투 스모크 테스트 통과
- WebGL 빌드 성공 또는 환경상 불가 사유와 마지막 성공 단계 기록
- `Assets/02. Scripts`에 프로젝트 코루틴 API가 남지 않음
- `AlllyUnit.cs`, `EnemyUnit.cs`에 스킬 ID 실행 분기가 남지 않음
- 현재 스킬 20개가 모두 V1 Adapter 검증을 통과
- 승인된 네 프리팹 파일만 삭제되고 Ball·Pin·Bumper 프리팹은 유지
- 작업 종료 AI 활용 기록과 해결 문제 목록 작성
