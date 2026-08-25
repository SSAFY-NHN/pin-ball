# 유닛 역할 상성 강화 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: 승인 후 `superpowers:test-driven-development`로 각 작업을 Red-Green-Refactor 순서로 수행하고, 완료 주장 전 `superpowers:verification-before-completion`을 사용한다. 이 문서의 체크박스를 진행 기록으로 사용한다.

**Goal:** 기존 타겟 선택·방어선·진화 스킬을 보존하면서 마법사 계열의 제한된 범위 기본 공격, 창병 계열의 유닛 대상 방어 관통, 역할 문구와 10웨이브 역할 학습 조합을 추가한다.

**Architecture:** `UnitBase`의 기존 기본 공격 경로에 방어 관통 비율을 조회하는 protected virtual 훅만 추가하고, `AllyUnit`이 순수 C# `AllyBasicAttackController`에 계열별 정책을 위임한다. 컨트롤러는 현재 `UnitId`와 `UnitTargetFinder`만 사용해 마법사 보조 대상을 고르고 모든 피해를 기존 `UnitBase.TakeDamage()`로 전달한다. 웨이브 데이터 계약과 공세 컨트롤러는 바꾸지 않고 `BattleWaveData.json`의 조합만 역할 학습 순서에 맞춘다.

**Tech Stack:** Unity 6, C#, Unity Test Framework EditMode, NUnit, `JsonUtility`, TextMeshPro, 기존 `SetActive` 기반 VFX·유닛 풀링

**Spec:** `docs/designs/2026-08-25/2026-08-25-unit-role-counterplay-design.md` (commit `57481a9`)

## Global Constraints

- 사용자 승인 전 이 계획 문서 외 코드·데이터·프리팹·씬을 수정하지 않는다.
- 별도 적·아군 속성 태그와 상성 추가 피해 표를 만들지 않는다.
- 기존 가장 가까운 적 우선 타겟 규칙을 유지한다.
- 전사와 궁수의 기본 공격 및 능력치를 변경하지 않는다.
- 마법사·화염술사·빙결술사는 주 대상 100%, 주 대상 반경 1.5 안의 가장 가까운 다른 적 최대 2명에게 각각 60% 기본 공격 피해를 준다.
- 주 대상 중복, 세 번째 보조 대상, 범위 밖 적과 방어선 보조 피해를 허용하지 않는다.
- 창병·돌격창병·방진수호병은 유닛 대상 기본 공격에서만 방어력 40%를 무시한다. 방어력 0 대상과 방어선에는 추가 이득을 만들지 않는다.
- 기존 진화 스킬의 피해·방어 무시·범위·타겟 규칙을 변경하지 않는다.
- 모든 보조 피해는 기존 `TakeDamage` 피해·사망·피격 피드백 경로를 사용한다.
- 60초 강화 증원, 90초 최후 공세, 동시 생존 적 상한 8명과 기존 반복 간격 구조를 유지한다.
- 새 적, 새 보스, 새 외부 패키지, 런타임 UI 생성과 관련 없는 리팩터링을 추가하지 않는다.
- 양측 방어선 승패, 출격 쿨다운, 창병 구매, 전술 증원권, 핀볼 경제, 유닛·VFX 풀링을 보존한다.
- `[SerializeField]` 필드 이름에 underscore를 사용하지 않는다.
- 사용자 소유 변경인 `Assets/05. Animations/Rabbit/Rabbit1_Mage_Attack.anim`과 `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset`은 수정·되돌리기·스테이징·커밋하지 않는다.

---

## 현재 코드·데이터·씬 대조 결과

- Git HEAD는 설계 커밋 `57481a9`다. 선행 커밋 `fa64eeb`, `097d5be`, `6ea0b6c`, `39dfa1f`가 바로 아래에 있으며, 작업 트리에는 사용자 소유 애니메이션·VFX 변경 2개만 있다.
- `UnitBase.TryAttack()`은 가장 가까운 현재 대상을 유지한 뒤 `TakeDamage(GetBasicAttackDamage(target), 0f, this)`를 호출한다. `TakeDamage`는 방어 계산, 피해 숫자, 피격 상태, 사망, 아군 피격 마나 알림을 한 경로에서 처리한다.
- `UnitHealth.TakeDamage()`는 `effectiveDefense = defense * (1 - Clamp01(armorIgnoreRatio))` 공식을 이미 사용한다. 새 피해 공식이나 방어력 태그가 필요 없다.
- 방어선 공격은 `TryMoveOrAttackDefenseLine()`에서 `UnitManager.RequestDefenseLineAttack()`으로 분기된다. 유닛 `TakeDamage` 경로와 분리돼 있어 관통·보조타 훅을 유닛 공격에만 두면 방어선 제외가 보장된다.
- `UnitTargetFinder.GetAliveEnemiesInRadius()`는 기존 스킬들이 사용하는 생존 적 반경 조회다. 현재 결과는 roster 순서이므로 컨트롤러가 주 대상 제외 후 주 대상과의 거리로 정렬하고 앞의 2명만 사용해야 한다.
- `AllyUnit.OnBasicAttackHit()`은 현재 공격 VFX 1회와 기본 공격 마나 획득 1회를 처리한다. 마법사 보조타는 이 지점에서 수행하면 스킬 시전과 분리되고 마나 획득 횟수도 기존 1회로 유지된다.
- `TitleData.TryGetRootAllyJob()`과 `previousJob` 계보가 있지만 매 기본 공격마다 데이터 서비스를 조회할 이유가 없다. 승인된 12개 ID를 컨트롤러의 고정 계열 집합으로 판정하면 새 데이터 필드 없이 진화 계열을 포함할 수 있다.
- `UnitAttackEffectPlayer`는 프리팹별 인스턴스 하나만 보유해 같은 프레임에 3개 대상 재생 시 앞선 코루틴을 중단한다. 최대 3개 동시 투사체가 보이도록 기존 프리팹의 작은 `SetActive` 풀로 확장해야 한다.
- `AllyUnit.prefab`의 화염 투사체 매핑은 현재 `mage`, `pyromancer`만 포함한다. 새 VFX를 만들지 않고 `frost`도 같은 기존 마법사 투사체 매핑에 포함한다.
- 구매 카드 역할 문구는 씬 YAML이 아니라 `AllyPurchasePanelController.Refresh()`의 문자열로 생성된다. 씬 오브젝트나 Inspector 참조 변경은 필요 없다.
- `BattleWaveData.json`은 10웨이브와 초기·기본·강화·최후 4단계를 이미 가진다. `BattleAssaultController`가 8명 상한과 60/90초 전환을 소유하므로 데이터 조합과 검증 테스트만 변경한다.

## 승인 대상 웨이브 변경표

표의 초기 공세는 기존 첫 생성 시각·개별 간격 형식을 유지한다. 반복 간격은 현재 단계별 간격을 유지해 이번 변경에서 공격 특성과 공세 빈도를 동시에 조정하지 않는다. 한 번의 조합 수량은 모두 2~5명이며 런타임 상한 8명 아래다.

| 웨이브·학습 역할 | 초기 공세 | 기본 증원 | 강화 증원 | 최후 공세 |
|---|---|---|---|---|
| 1 고블린 물량 · 마법사 | `goblin x4` (`0초/1.2초`) | `goblin x3 / 12초` | `goblin x4 / 10초` | `goblin x5 / 8초` |
| 2 늑대 돌진 · 전사 | `wolf x3` (`0초/1.5초`) | `wolf x2 / 12초` | `wolf x3 / 10초` | `wolf x4 / 8초` |
| 3 근접+적 궁수 · 전사·궁수 | `goblin x3` (`0초/1.5초`) + `goblin_archer x1` (`3초`) | `goblin x2 + goblin_archer x1 / 12초` | `goblin x2 + goblin_archer x2 / 10초` | `goblin x3 + goblin_archer x2 / 8초` |
| 4 물량+원거리 · 마법사 보호 | `goblin x4` (`0초/1.2초`) + `goblin_archer x1` (`2초`) | `goblin x3 + goblin_archer x1 / 12초` | `goblin x4 + goblin_archer x1 / 10초` | `goblin x4 + goblin_archer x2 / 8초` |
| 5 방패병 · 창병 | `shield_guard x3` (`0초/1.8초`) | `shield_guard x2 / 11초` | `shield_guard x3 / 9초` | `shield_guard x4 / 8초` |
| 6 방패 진형 · 창병·마법사 | `shield_guard x2` (`0초/2초`) + `goblin x3` (`1초/1.2초`) | `shield_guard x2 + goblin x2 / 11초` | `shield_guard x2 + goblin x3 / 9초` | `shield_guard x3 + goblin x3 / 8초` |
| 7 오크·트롤 · 창병·궁수 | `orc_warrior x2` (`0초/2초`) + `troll x1` (`4초`) | `orc_warrior x2 + troll x1 / 10초` | `orc_warrior x2 + troll x2 / 9초` | `orc_warrior x3 + troll x2 / 8초` |
| 8 주술사·암살자 · 전사 후열 보호 | `shaman x2` (`0초/2초`) + `assassin x2` (`1초/2초`) | `shaman x1 + assassin x2 / 10초` | `shaman x2 + assassin x2 / 9초` | `shaman x2 + assassin x3 / 8초` |
| 9 정예 혼성 · 네 역할 종합 | `ogre_elite x1` (`0초`) + `dark_mage_elite x1` (`2초`) + `shield_guard x1` (`3초`) + `assassin x1` (`4초`) | `orc_warrior x1 + shield_guard x1 + shaman x1 + assassin x1 / 10초` | `ogre_elite x1 + shield_guard x1 + shaman x1 + assassin x1 / 9초` | `ogre_elite x1 + dark_mage_elite x1 + troll x1 + assassin x1 / 8초` |
| 10 보스+증원 · 궁수·마법사 | `goblin_king x1` (`0초`) + `goblin x3` (`2초/1.2초`) | `goblin x3 + wolf x1 / 10초` | `goblin x4 + shield_guard x1 / 9초` | `goblin x4 + troll x1 / 7초` |

웨이브 이름도 역할을 드러내도록 `고블린 물량`, `늑대 돌진`, `근접과 궁수`, `물량과 원거리`, `방패병`, `방패 진형`, `오크와 트롤`, `주술사와 암살자`, `정예 혼성`, `보스와 증원`으로 바꾼다. `IsElite`는 5·9웨이브, `IsBoss`는 10웨이브라는 현재 표식을 유지한다.

---

### Task 1: 기본 공격 계열 정책과 창병 관통

**Files:**
- Create: `Assets/02. Scripts/Battle/Runtime/AllyBasicAttackController.cs`
- Create: `Assets/02. Scripts/Battle/Runtime/AllyBasicAttackController.cs.meta`
- Create: `Assets/02. Scripts/Battle/Editor/AllyBasicAttackControllerTests.cs`
- Create: `Assets/02. Scripts/Battle/Editor/AllyBasicAttackControllerTests.cs.meta`
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`

**Interfaces:**
- `AllyBasicAttackController.GetArmorIgnoreRatio(string unitId, UnitBase target) : float`
- `AllyBasicAttackController.ApplySecondaryHits(string unitId, UnitBase source, UnitBase primaryTarget, float basicAttackDamage, UnitTargetFinder targetFinder, Action<UnitBase> playEffect) : int`
- 계열 상수: 마법사 반경 `1.5f`, 보조 대상 `2`, 피해 배율 `0.6f`; 창병 관통 `0.4f`.
- `UnitBase.GetBasicAttackArmorIgnoreRatio(UnitBase target) : float` protected virtual, 기본값 `0f`.
- `AllyUnit` override는 컨트롤러에 `UnitId`와 실제 유닛 대상을 전달한다.

- [ ] `GetArmorIgnoreRatio`가 `spearman`, `lancer`, `guard`에 유닛 대상 `0.4f`, `warrior`, `archer`, `mage`, null 방어선 대상에 `0f`를 반환하는 failing test를 작성한다.
- [ ] 방어력 100 대상에서 일반 100 피해가 50, 창병 계열이 방어력 60으로 계산돼 62가 되고, 방어력 0 대상에서는 둘 다 100이 되는 `UnitHealth` 연계 failing test를 작성한다.
- [ ] `UnitBase.TryAttack()`의 하드코딩된 `0f`를 `GetBasicAttackArmorIgnoreRatio(_currentTarget)`로만 교체한다. 방어선 `RequestDefenseLineAttack()`은 수정하지 않는다.
- [ ] 전사·궁수·마법사·적 유닛의 기본 훅이 `0f`여서 기존 피해가 같은 characterization test를 통과시킨다.
- [ ] `AllyUnit`에서만 창병 계열 훅을 override하고 관련 test를 통과시킨다.

### Task 2: 마법사 보조 대상·피해·피드백

**Files:**
- Modify: `Assets/02. Scripts/Battle/Runtime/AllyBasicAttackController.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/AllyBasicAttackControllerTests.cs`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitTargetFinderTests.cs`

**Interfaces:**
- `ApplySecondaryHits(...)`는 기존 `GetAliveEnemiesInRadius(primaryTarget.transform.position, 1.5f, buffer)` 결과에서 주 대상을 제거하고, 주 대상과의 거리 오름차순으로 정렬하며, 최대 2명에게 `TakeDamage(basicAttackDamage * 0.6f, 0f, source)`를 호출한다.
- 반환값은 실제 보조 피해를 시도한 대상 수다. `playEffect`는 각 보조 대상의 `TakeDamage` 뒤 한 번 호출한다.
- `AllyUnit.OnBasicAttackHit()` 순서: 주 대상 기존 VFX, 마법사 보조타·보조 VFX, 기존 기본 공격 마나 1회 획득.

- [ ] `mage`, `pyromancer`, `frost`가 반경 1.5 안의 가장 가까운 다른 적 2명에게 60% 피해를 주는 failing test를 작성한다.
- [ ] 주 대상이 결과에 포함돼도 중복 피해가 없고, 세 번째 근거리 적과 1.5 밖 적은 피해를 받지 않는 failing test를 작성한다.
- [ ] 같은 거리에서는 기존 roster 순서를 tie-break로 유지해 결과가 결정적인 test를 작성한다.
- [ ] `warrior`, `knight`, `berserker`, `archer`, `ranger`, `marksman`, 창병 계열은 보조 피해 0건인 failing test를 작성한다.
- [ ] 보조 대상에 실 `TakeDamage`를 사용해 방어 감소, 피격 상태, 피해 피드백, 사망·roster 알림이 발생하는 integration test를 작성한다.
- [ ] 보조타마다 마나를 추가 획득하지 않고 주 기본 공격 1회분만 얻는 `AllyUnit` regression test를 작성한다.
- [ ] 컨트롤러에 고정 ID 계열 판정과 재사용 `List<UnitBase>` 하나만 구현하고 test를 통과시킨다. 새 태그·데이터 파일·LINQ 할당은 추가하지 않는다.

### Task 3: 동시 마법사 공격 이펙트 풀

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitAttackEffectPlayer.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/UnitAttackEffectPlayerTests.cs`
- Modify: `Assets/04. Prefabs/AllyUnit.prefab`

**Interfaces:**
- 기존 `Play(string unitId, UnitBase target)` 공개 진입점은 유지한다.
- 프리팹별 활성·비활성 인스턴스 목록을 보유하고, 재생 시 비활성 인스턴스를 재사용하며 최대 동시 3개까지만 생성한다.
- 각 코루틴 종료와 `OnDisable()`은 인스턴스를 `SetActive(false)`로 반환한다. `Destroy`를 사용하지 않는다.
- `fireUnitIds`는 `mage`, `pyromancer`, `frost`다. 새 프리팹이나 외부 VFX asset을 추가하지 않는다.

- [ ] 같은 프레임에 같은 마법사 VFX를 서로 다른 3개 대상에 호출해 활성 인스턴스 3개가 공존하는 failing test를 작성한다.
- [ ] 네 번째 동시 호출이 새 인스턴스를 만들지 않고, 완료 뒤 비활성 인스턴스를 재사용하는 failing test를 작성한다.
- [ ] `OnDisable()` 뒤 모든 풀 인스턴스가 비활성이고 실행 코루틴이 남지 않는 regression test를 작성한다.
- [ ] 단일 dictionary를 최대 3개 `SetActive` 풀로 최소 변경하고 기존 궁수·레인저·적 궁수 이펙트 test를 통과시킨다.
- [ ] `AllyUnit.prefab`의 기존 화염 이펙트 참조를 유지하고 `frost` ID만 매핑에 추가한다. 사용자 소유 `ArcaneVfxCatalog.asset`은 건드리지 않는다.

### Task 4: 구매 카드 역할 문구

**Files:**
- Modify: `Assets/02. Scripts/03. UI/AllyPurchasePanelController.cs`
- Modify: `Assets/02. Scripts/03. UI/Editor/AllyPurchaseUiSceneTests.cs`

**Interfaces:**
- 전사: `돌진 저지 · 전열 방어`
- 궁수: `장거리 · 단일 지속 피해`
- 마법사: `원거리 · 범위 피해`
- 창병: `중거리 · 방어 관통`
- `FormatCard()`의 이름·역할·보유 수·비용/무료 줄 구조는 유지한다.

- [ ] 네 역할 문구의 전체 카드 문자열을 고정하는 failing parameterized test를 작성한다.
- [ ] `Refresh()`의 네 문자열만 교체하고 씬 오브젝트·참조·레이아웃을 수정하지 않는다.
- [ ] 쿨다운 표시, 버튼 활성 조건, 전술 증원권 무료 문구 회귀 test를 통과시킨다.

### Task 5: 10웨이브 역할 학습 데이터

**Files:**
- Modify: `Assets/Resources/Data/BattleWaveData.json`
- Modify: `Assets/02. Scripts/Battle/Editor/BattleWaveScheduleDataTests.cs`
- Modify: `Assets/02. Scripts/Battle/Editor/EnemyWaveVisualTests.cs`

**Interfaces:**
- 기존 `BattleWaveData.InitialAssault`, `BasicReinforcement`, `EmpoweredReinforcement`, `FinalAssault` 계약만 사용한다.
- 이 계획의 웨이브 변경표가 이름, 초기 시각·간격, 단계별 ID·수량·반복 간격의 단일 기대값이다.
- `goblin_king`은 10웨이브 초기 공세에 1회만 존재한다.

- [ ] 변경표 전체를 구조적으로 비교하는 failing test를 작성한다. 문자열 포함 검사가 아니라 각 단계의 ID·수량과 간격을 비교한다.
- [ ] 모든 조합의 합계가 8 이하이고, 초기 마지막 생성 시각이 60초 미만이며, 모든 ID가 `EnemyUnitData.json`에 존재하는 failing test를 작성한다.
- [ ] 웨이브 1~10의 역할 주제별 필수 적 조합을 별도 parameterized test로 고정한다.
- [ ] JSON만 변경표대로 수정한다. 60/90초 경계와 `BattleAssaultController` 코드는 수정하지 않는다.
- [ ] 기존 보스 1회, 10웨이브 수, 공세 데이터 유효성, 적 시각 asset 매핑 test를 통과시킨다.

### Task 6: 회귀 검증과 AI 활용 기록

**Files:**
- Create: `docs/ai-usage/2026-08-25/2026-08-25-unit-role-counterplay-implementation-ai-usage.md`

**Verification:**
- 관련 EditMode: 새 기본 공격 controller, `UnitHealth`, `UnitTargetFinder`, `UnitAttackEffectPlayer`, 역할 카드, 웨이브 schedule/visual.
- 회귀 EditMode: 진화 스킬 전체, `UnitAttack`, 방어선 공격, 구매·쿨다운, 전술 증원권, 공세 controller, 웨이브 resolution, 유닛 pool reset.
- C# compile: Unity batchmode EditMode 로그에서 compiler error 0건 확인.
- Scene/data: `02. Game.unity` missing script/reference 0건, `AllyUnit.prefab` VFX 참조, JSON 10웨이브·보스 1회·상한 8 검사.
- PlayMode: 주 대상 100%, 보조 2명 60%, 세 번째·범위 밖 제외, 창병 고방어 대상 우위, 방어선 동일 피해, 60/90초 공세와 8명 상한, 네 카드 문구 확인.
- WebGL: 아군 5명·적 8명에서 동시 마법사 투사체, 풀 재사용, 핀볼 경제·출격 쿨다운·전술 증원권 유지 확인.

- [ ] 변경 전 관련 characterization test를 실행해 baseline을 기록한다.
- [ ] Task별 Red 단계에서 새 test가 의도한 이유로 실패하는지 확인하고, Green 단계에서 해당 test와 인접 회귀 test를 실행한다.
- [ ] 전체 관련 EditMode suite를 Unity batchmode로 실행하고 XML의 total/passed/failed/skipped와 로그의 compile error를 기록한다.
- [ ] 씬·프리팹·JSON 정적 검사를 실행하고 가능한 PlayMode/WebGL 수동 검증을 수행한다. 실행하지 못한 항목은 통과로 쓰지 않는다.
- [ ] `git diff --check`, `git status --short`, 변경 파일 목록을 확인한다.
- [ ] 사용자 소유 애니메이션·VFX asset 2개의 diff가 작업 전과 같고 스테이징·커밋 대상에서 제외됐는지 확인한다.
- [ ] AI 도구/모델, 요청, 제안, 실제 수정, 사용자 결정, 중요 지시, 검증 결과와 제한점을 기록한다.
- [ ] 최종 보고에 변경 파일, 이유, test 결과, 직접 확인 절차와 제한점을 포함한다.

## 자체 검토 결과

- 설계의 계열 12개, 마법사 100%/60%/1.5/2명, 중복·세 번째·범위 밖·방어선 제외, 창병 40%, 방어력 0, 스킬 보존, 기존 피해 경로, 역할 문구와 10웨이브 주제를 Task 1~5에 각각 연결했다.
- 공개 변경은 `UnitBase`의 protected virtual 훅 하나뿐이다. 기존 public 공격·타겟·VFX API는 유지한다.
- 새 런타임 타입은 계열 기본 공격 정책 한 책임만 가진다. 새 데이터 필드, 속성표, 상성 배율표와 별도 튜토리얼은 없다.
- 씬 변경은 필요 없다. 프리팹 변경은 기존 마법사 VFX에 `frost` ID를 연결하는 한 줄로 제한한다.
- 웨이브 조합은 한 번의 생성 시도 기준 최대 6명이며 동시 생존 상한 8 로직을 변경하지 않는다.
- 계획에서 placeholder, 정의되지 않은 타입·메서드, 요청 밖 리팩터링을 제거했다.
- 구현 중 현재 코드와 계획의 경계가 충돌하면 범위를 임의 확장하지 않고 차이를 보고해 재승인을 받는다.
