# Node Skill Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 현재 20개 스킬을 스킬 ID별 코드 분기 없이 실행하고, 향후 데이터 테이블 변경을 V1 Adapter 경계에 격리한다.

**Architecture:** JSON의 `UnitSkillData`는 문자열 기반 V1 그래프 DTO를 가진다. `SkillGraphV1Adapter`가 이를 enum과 명명된 파라미터를 사용하는 런타임 `SkillGraph`로 변환하고 검증한다. `UnitSkillController`는 유닛별 Blackboard를 보유하며 `SkillNodeController`에 조건·대상·효과 실행을 위임한다.

**Tech Stack:** Unity 6000.0.79f1, C#, JsonUtility, UniTask effects from plan 01, NUnit/EditMode tests

## Global Constraints

- 원본 JSON 필드나 effect 배열 인덱스를 전투 실행 코드에서 읽지 않는다.
- 스킬 ID를 기준으로 실행 메서드를 선택하지 않는다.
- 비순환 그래프만 허용하고 비주얼 편집기·reflection 기반 자동 등록은 추가하지 않는다.
- 현재 20개 스킬에 실제로 필요한 트리거·조건·대상·효과만 구현한다.
- 잘못된 스킬은 해당 스킬만 비활성화하고 기본 공격은 유지한다.
- 오류 로그는 유닛 ID, 스킬 ID, 노드 ID를 포함한다.
- plan 01의 `BattleManager.State`와 UniTask 전환이 완료되어 있어야 한다.

---

### Task 1: 공유 V1 스킬 그래프 DTO 정의

**Files:**
- Create: `Assets/02. Scripts/02. Data/SkillGraphData.cs`
- Modify: `Assets/02. Scripts/02. Data/AllyUnitData.cs`
- Modify: `Assets/02. Scripts/02. Data/EnemyUnitData.cs`
- Create: `Assets/02. Scripts/Editor/Tests/SkillGraphAdapterTests.cs`

**Interfaces:**
- Produces: `UnitSkillData`
- Produces: `SkillGraphData`, `SkillEntryData`, `SkillNodeData`
- Changes: `AllyUnitData.skill` to `UnitSkillData`
- Changes: `EnemyUnitData.skills`/`skill` to `UnitSkillData[]`

- [ ] **Step 1: Write a failing JsonUtility DTO test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class SkillGraphAdapterTests
{
    [Test]
    public void JsonUtilityReadsNamedGraphFields()
    {
        const string json = "{\"id\":\"test\",\"graph\":{" +
            "\"entries\":[{\"id\":\"cast\",\"trigger\":\"ManaFull\",\"next\":\"target\"}]," +
            "\"nodes\":[{\"id\":\"target\",\"kind\":\"Target\",\"operation\":\"EventTarget\",\"next\":\"damage\"}," +
            "{\"id\":\"damage\",\"kind\":\"Effect\",\"operation\":\"Damage\",\"ratio\":1.6}]}}";

        var skill = JsonUtility.FromJson<UnitSkillData>(json);

        Assert.That(skill.id, Is.EqualTo("test"));
        Assert.That(skill.graph.entries[0].trigger, Is.EqualTo("ManaFull"));
        Assert.That(skill.graph.nodes[1].ratio, Is.EqualTo(1.6f));
    }
}
```

- [ ] **Step 2: Run the focused test and confirm compile failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter SkillGraphAdapterTests -testResults "Temp/skill-adapter-tests.xml" -logFile "Temp/skill-adapter-tests.log"
```

Expected: `UnitSkillData` and graph DTO types do not exist.

- [ ] **Step 3: Add the V1 DTO with named fields**

```csharp
using System;

[Serializable]
public class UnitSkillData
{
    public string id;
    public string key;
    public string name;
    public string description;
    public SkillGraphData graph;

    public string SkillId => string.IsNullOrEmpty(key) ? id : key;
}

[Serializable]
public class SkillGraphData
{
    public SkillEntryData[] entries;
    public SkillNodeData[] nodes;
}

[Serializable]
public class SkillEntryData
{
    public string id;
    public string trigger;
    public string next;
    public float interval;
}

[Serializable]
public class SkillNodeData
{
    public string id;
    public string kind;
    public string operation;
    public string next;
    public string failure;
    public string stateKey;
    public string stat;
    public string unitId;
    public float amount;
    public float ratio;
    public float duration;
    public float delay;
    public float radius;
    public float distance;
    public float distanceMultiplier;
    public float halfWidth;
    public float threshold;
    public float armorIgnore;
    public float multiplier;
    public int count;
    public int maxCount;
    public int maxTargets;
    public float[] ratios;
    public bool permanent;
}
```

Remove `AllySkillData`, `AllySkillEffectData`, `EnemySkillData`, and `EnemySkillEffectData`. Keep the existing dual `skills`/`skill` normalization property on enemy data but change its type to `UnitSkillData[]`.

- [ ] **Step 4: Run the DTO test and verify pass**

Run the Step 2 command. Expected: `JsonUtilityReadsNamedGraphFields` passes.

- [ ] **Step 5: Commit the shared DTO**

```powershell
git add -- "Assets/02. Scripts/02. Data/SkillGraphData.cs" "Assets/02. Scripts/02. Data/AllyUnitData.cs" "Assets/02. Scripts/02. Data/EnemyUnitData.cs" "Assets/02. Scripts/Editor/Tests/SkillGraphAdapterTests.cs"
git commit -m "refactor: define shared skill graph data"
```

### Task 2: 런타임 그래프와 V1 Adapter 검증 구현

**Files:**
- Create: `Assets/02. Scripts/Battle/SkillGraph.cs`
- Create: `Assets/02. Scripts/Battle/SkillGraphValidator.cs`
- Create: `Assets/02. Scripts/Battle/SkillGraphV1Adapter.cs`
- Create: `Assets/02. Scripts/Editor/Tests/SkillGraphValidatorTests.cs`
- Modify: `Assets/02. Scripts/Editor/Tests/SkillGraphAdapterTests.cs`

**Interfaces:**
- Produces: `ESkillTrigger`, `ESkillNodeKind`, `ESkillCondition`, `ESkillTarget`, `ESkillEffect`, `ESkillStat`
- Produces: `SkillGraph`, `SkillEntry`, `SkillNode`
- Produces: `ISkillGraphAdapter.TryBuild(UnitSkillData, out SkillGraph, out IReadOnlyList<string>)`
- Produces: `SkillGraphValidator.Validate(SkillGraph)`

- [ ] **Step 1: Write failing validation tests**

```csharp
using System.Linq;
using NUnit.Framework;

public class SkillGraphValidatorTests
{
    [Test]
    public void ValidatorRejectsMissingLink()
    {
        var graph = SkillGraphTestFactory.Create(
            new SkillEntry("cast", ESkillTrigger.ManaFull, "missing", 0f),
            System.Array.Empty<SkillNode>());

        var errors = SkillGraphValidator.Validate(graph);
        Assert.That(errors.Any(error => error.Contains("missing")), Is.True);
    }

    [Test]
    public void ValidatorRejectsCycle()
    {
        var nodes = new[]
        {
            SkillNode.Condition("a", ESkillCondition.Once, "b", null),
            SkillNode.Condition("b", ESkillCondition.Once, "a", null)
        };
        var graph = SkillGraphTestFactory.Create(
            new SkillEntry("cast", ESkillTrigger.ManaFull, "a", 0f),
            nodes);

        var errors = SkillGraphValidator.Validate(graph);
        Assert.That(errors.Any(error => error.Contains("cycle")), Is.True);
    }
}
```

`SkillGraphTestFactory` is a test-only helper in the same file that constructs a graph with ID `test-unit/test-skill` and a node dictionary.

```csharp
private static SkillGraph Create(
    SkillEntry entry,
    IReadOnlyList<SkillNode> nodes) =>
    new(
        "test-unit",
        "test-skill",
        new[] { entry },
        nodes.ToDictionary(node => node.Id));
```

- [ ] **Step 2: Run validator tests and confirm compile failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter SkillGraphValidatorTests -testResults "Temp/skill-validator-tests.xml" -logFile "Temp/skill-validator-tests.log"
```

Expected: runtime graph and validator types are missing.

- [ ] **Step 3: Define the table-independent runtime model**

```csharp
public enum ESkillTrigger
{
    BattleStart,
    ManaFull,
    BasicAttackHit,
    BeforeBasicAttackDamage,
    BeforeIncomingDamage,
    BeforeCrowdControl,
    Damaged,
    Periodic
}

public enum ESkillNodeKind { Condition, Target, Effect }
public enum ESkillCondition
{
    EveryNthOccurrence,
    HpAtOrBelow,
    Once,
    IsNotStunned,
    IsOutOfCombat,
    SourceInFront
}
public enum ESkillTarget
{
    Self,
    EventTarget,
    FarthestOpponent,
    HighestHpOpponent,
    OpponentsInRadiusOfSelf,
    OpponentsInRadiusOfEventTarget,
    AlliesInRadiusOfSelf,
    OpponentsInLineToEventTarget,
    AllAllies
}
public enum ESkillEffect
{
    Damage,
    DamageByTargetIndex,
    DamageOverTime,
    Heal,
    HealByTargetCount,
    Shield,
    StatMultiplier,
    Stun,
    Knockback,
    DamageReduction,
    KnockbackImmunity,
    ForceSelectedTargetsToSource,
    ForceSourceToSelectedTarget,
    MoveSource,
    TeleportSourceNearTarget,
    Summon,
    ModifyEventValue,
    ModifyEventValueByTargetStack,
    UpdateTargetStack
}
public enum ESkillStat
{
    AttackDamage,
    AttackRate,
    Defense,
    MoveSpeed,
    AttackRange
}
```

`SkillGraph` stores `UnitId`, `SkillId`, entries, and an ID-keyed read-only node dictionary. `SkillNode` stores parsed enums and the named numerical/string fields from the V1 DTO. Runtime code must not retain a reference to `SkillNodeData`.

Expose these exact construction helpers used by tests and the adapter:

```csharp
public SkillEntry(
    string id,
    ESkillTrigger trigger,
    string nextNodeId,
    float interval);

public static SkillNode Condition(
    string id,
    ESkillCondition operation,
    string nextNodeId,
    string failureNodeId);
```

- [ ] **Step 4: Implement explicit V1 parsing and graph validation**

```csharp
public interface ISkillGraphAdapter
{
    bool TryBuild(
        string unitId,
        UnitSkillData source,
        out SkillGraph graph,
        out IReadOnlyList<string> errors);
}

public sealed class SkillGraphV1Adapter : ISkillGraphAdapter
{
    public bool TryBuild(
        string unitId,
        UnitSkillData source,
        out SkillGraph graph,
        out IReadOnlyList<string> errors)
    {
        var buildErrors = new List<string>();
        var entries = ParseEntries(unitId, source, buildErrors);
        var nodes = ParseNodes(unitId, source, buildErrors);

        graph = new SkillGraph(
            unitId,
            source?.SkillId ?? string.Empty,
            entries,
            nodes);
        buildErrors.AddRange(SkillGraphValidator.Validate(graph));
        errors = buildErrors;
        return buildErrors.Count == 0;
    }
}
```

`ParseEntries` and `ParseNodes` are private methods in the same class. Each uses case-insensitive `Enum.TryParse`, prefixes errors with `unitId/SkillId/nodeId`, and copies every DTO array so later source mutation cannot alter the runtime graph.

```csharp
private static List<SkillEntry> ParseEntries(
    string unitId,
    UnitSkillData source,
    List<string> errors);

private static List<SkillNode> ParseNodes(
    string unitId,
    UnitSkillData source,
    List<string> errors);
```

The implementation must report duplicate IDs, empty entry links, unknown enum names, missing links, invalid negative radius/duration/count values, and cycles. A condition `failure` may be empty to end that branch; every `next` may be empty only to end execution.

- [ ] **Step 5: Add adapter tests for unknown operations and detached data**

```csharp
[Test]
public void AdapterRejectsUnknownOperation()
{
    var data = BuildSkillWithNode("Effect", "NotAnEffect");
    var adapter = new SkillGraphV1Adapter();

    var built = adapter.TryBuild("warrior", data, out _, out var errors);

    Assert.That(built, Is.False);
    Assert.That(errors[0], Does.Contain("warrior"));
    Assert.That(errors[0], Does.Contain("NotAnEffect"));
}

private static UnitSkillData BuildSkillWithNode(
    string kind,
    string operation) =>
    new()
    {
        id = "test-skill",
        graph = new SkillGraphData
        {
            entries = new[]
            {
                new SkillEntryData
                {
                    id = "entry",
                    trigger = "ManaFull",
                    next = "node"
                }
            },
            nodes = new[]
            {
                new SkillNodeData
                {
                    id = "node",
                    kind = kind,
                    operation = operation
                }
            }
        }
    };
```

- [ ] **Step 6: Run adapter and validator tests**

Run both focused test filters. Expected: all graph DTO, adapter, missing-link, and cycle tests pass.

- [ ] **Step 7: Commit the graph model and adapter**

```powershell
git add -- "Assets/02. Scripts/Battle/SkillGraph.cs" "Assets/02. Scripts/Battle/SkillGraphValidator.cs" "Assets/02. Scripts/Battle/SkillGraphV1Adapter.cs" "Assets/02. Scripts/Editor/Tests/SkillGraphValidatorTests.cs" "Assets/02. Scripts/Editor/Tests/SkillGraphAdapterTests.cs"
git commit -m "feat: add validated skill graph adapter"
```

### Task 3: 노드 실행기와 유닛별 Blackboard 구현

**Files:**
- Create: `Assets/02. Scripts/Battle/UnitSkillController.cs`
- Create: `Assets/02. Scripts/Battle/SkillNodeController.cs`
- Create: `Assets/02. Scripts/Editor/Tests/SkillRuntimeTests.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs`

**Interfaces:**
- Produces: `UnitSkillController(UnitBase, UnitManager, IReadOnlyList<SkillGraph>)`
- Produces: `void Publish(ESkillTrigger, UnitBase eventTarget = null)`
- Produces: `float Modify(ESkillTrigger, float value, UnitBase eventTarget = null)`
- Produces: `void Tick(float activeDeltaTime)`
- Produces: `SkillExecutionContext` and per-graph `SkillBlackboard`
- Produces: `UnitManager.FindFarthestAliveOpponent(UnitBase)`
- Produces: `UnitManager.FindHighestHpAliveOpponent(UnitBase)`
- Produces: `UnitManager.GetAliveTeamMembers(EBattleTeam, List<UnitBase>)`

- [ ] **Step 1: Write failing branch, damage, and modifier runtime tests**

```csharp
[Test]
public void DamageNodeAppliesAttackRatioToSelectedTarget()
{
    using var world = SkillRuntimeTestWorld.Create();
    var graph = SkillRuntimeTestGraphs.EventTargetDamage(1.6f);
    var controller = new UnitSkillController(
        world.Ally,
        world.UnitManager,
        new[] { graph });

    var before = world.Enemy.CurrentHp;
    controller.Publish(ESkillTrigger.ManaFull, world.Enemy);

    Assert.That(world.Enemy.CurrentHp, Is.LessThan(before));
}

[Test]
public void ModifierNodeChangesEventValue()
{
    using var world = SkillRuntimeTestWorld.Create();
    var graph = SkillRuntimeTestGraphs.ValueMultiplier(0.75f);
    var controller = new UnitSkillController(
        world.Enemy,
        world.UnitManager,
        new[] { graph });

    var result = controller.Modify(
        ESkillTrigger.BeforeIncomingDamage,
        100f,
        world.Ally);
    Assert.That(result, Is.EqualTo(75f));
}

[Test]
public void FalseConditionFollowsFailureLink()
{
    using var world = SkillRuntimeTestWorld.Create();
    var graph = SkillRuntimeTestGraphs.FalseBranchMultiplier(0.8f);
    var controller = new UnitSkillController(
        world.Enemy,
        world.UnitManager,
        new[] { graph });

    var result = controller.Modify(
        ESkillTrigger.BeforeIncomingDamage,
        100f,
        world.Ally);
    Assert.That(result, Is.EqualTo(80f));
}
```

`SkillRuntimeTestWorld : IDisposable` creates and destroys registered `BattleManager`, `UnitManager`, and lightweight `TestUnit : UnitBase` GameObjects. It exposes `BattleManager`, `UnitManager`, `TestUnit Ally`, and `TestUnit Enemy`, initializes both units with deterministic 100 HP/20 attack/0 defense stats, and registers them through `AddAlly`/`AddEnemy`. `TestUnit.Tick()` is empty and its team is supplied during initialization.

`SkillRuntimeTestGraphs` builds graphs through `SkillGraphV1Adapter` and asserts adapter success before returning. `EventTargetDamage(float ratio)` produces ManaFull → EventTarget → Damage. `ValueMultiplier(float multiplier)` produces BeforeIncomingDamage → ModifyEventValue. `FalseBranchMultiplier(float multiplier)` produces BeforeIncomingDamage → HpAtOrBelow threshold 0, with the false link pointing to ModifyEventValue. All helpers use IDs `test-unit/test-skill` and fully linked nodes.

- [ ] **Step 2: Run SkillRuntimeTests and confirm compile failure**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter SkillRuntimeTests -testResults "Temp/skill-runtime-tests.xml" -logFile "Temp/skill-runtime-tests.log"
```

Expected: controller types are missing.

- [ ] **Step 3: Implement graph dispatch and Blackboard isolation**

```csharp
public sealed class UnitSkillController
{
    public void Publish(ESkillTrigger trigger, UnitBase eventTarget = null)
    {
        Execute(trigger, 0f, eventTarget, false);
    }

    public float Modify(
        ESkillTrigger trigger,
        float value,
        UnitBase eventTarget = null)
    {
        return Execute(trigger, value, eventTarget, true);
    }

    public void Tick(float activeDeltaTime)
    {
        // Accumulate each Periodic entry by entry ID and publish once for
        // every elapsed interval, retaining fractional remainder.
    }

    private float Execute(
        ESkillTrigger trigger,
        float value,
        UnitBase eventTarget,
        bool modifiesValue)
    {
        var context = new SkillExecutionContext(
            owner,
            eventTarget,
            value,
            modifiesValue);
        foreach (var runtime in graphRuntimes)
        {
            ExecuteMatchingEntries(runtime, trigger, context);
        }

        return context.Value;
    }
}
```

Maintain one runtime state object per graph. Store counters, once flags, target references, and stack counts by `stateKey` or node ID. Enforce a step limit of `graph.Nodes.Count + 1` even after static cycle validation.

`SkillExecutionContext` owns the source, event target, mutable selected-target list, and mutable value. `SkillBlackboard` owns dictionaries for counters, flags, stored targets, stacks, and periodic elapsed time. Neither type references a source DTO.

- [ ] **Step 4: Implement only the approved condition and target operations**

`SkillNodeController` evaluates the six condition enums and resolves the nine target enums from Task 2. Radius and line queries delegate to `UnitManager`; team-relative selectors must work for both ally and enemy owners. Remove dead targets before each effect.

Add read-only `UnitBase.AttackRange` and `UnitBase.CurrentTarget` properties for target calculation and front-facing checks. Add the three generic opponent/team query methods listed in Interfaces rather than branching on concrete `AllyUnit`/`EnemyUnit` classes.

```csharp
private bool EvaluateCondition(
    SkillNode node,
    SkillExecutionContext context,
    SkillBlackboard blackboard)
{
    return node.Condition switch
    {
        ESkillCondition.HpAtOrBelow => context.Owner.HpRatio <= node.Threshold,
        ESkillCondition.IsNotStunned => !context.Owner.IsStunned,
        ESkillCondition.IsOutOfCombat =>
            Time.time - context.Owner.LastDamagedTime >= node.Duration,
        _ => EvaluateStatefulCondition(node, context, blackboard)
    };
}
```

- [ ] **Step 5: Implement only the approved effect operations**

Use `UnitBase` public APIs for damage, heal, shield, stat changes, crowd control, and movement. Use `UnitManager.SpawnEnemyReinforcement` for summon. When `SkillNode.Permanent` is true, pass `float.PositiveInfinity`; durations remain non-negative and V1 JSON uses the named `permanent` boolean.

`DamageByTargetIndex` uses `ratios[index]`, capped by both `maxTargets` and array length. `HealByTargetCount` computes `MaxHp * min(ratio * targetCount, amount)`, where `amount` is the cap ratio. `ModifyEventValueByTargetStack` applies stacks only when the current event target equals the stored target.

```csharp
private void ApplyEffect(
    SkillNode node,
    SkillExecutionContext context,
    SkillBlackboard blackboard)
{
    switch (node.Effect)
    {
        case ESkillEffect.Damage:
            ApplyDamage(node, context.Targets, context.Owner);
            break;
        case ESkillEffect.StatMultiplier:
            ApplyStatMultiplier(node, context.Targets);
            break;
        case ESkillEffect.ModifyEventValue:
            context.Value *= node.Multiplier;
            break;
        default:
            ApplySpecializedEffect(node, context, blackboard);
            break;
    }
}
```

`ApplySpecializedEffect` contains explicit cases for every remaining `ESkillEffect` enum from Task 2 and throws `ArgumentOutOfRangeException` for an unregistered enum; it never checks a skill ID.

- [ ] **Step 6: Run SkillRuntimeTests and verify pass**

Run the Step 2 command. Expected: branch selection, target damage, value modification, stack isolation, and periodic remainder tests pass.

- [ ] **Step 7: Commit the node runtime**

```powershell
git add -- "Assets/02. Scripts/Battle/UnitSkillController.cs" "Assets/02. Scripts/Battle/SkillNodeController.cs" "Assets/02. Scripts/Battle/UnitManager.cs" "Assets/02. Scripts/Battle/UnitBase.cs" "Assets/02. Scripts/Editor/Tests/SkillRuntimeTests.cs"
git commit -m "feat: execute unit skills as node graphs"
```

### Task 4: 현재 20개 스킬을 V1 그래프로 이관

**Files:**
- Modify: `Assets/Resources/Data/AllyUnitData.json`
- Modify: `Assets/Resources/Data/EnemyUnitData.json`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs`
- Modify: `Assets/02. Scripts/Editor/Tests/SkillGraphAdapterTests.cs`

**Interfaces:**
- Produces: `bool TitleData.TryGetAllySkillGraphs(string, out IReadOnlyList<SkillGraph>)`
- Produces: `bool TitleData.TryGetEnemySkillGraphs(string, out IReadOnlyList<SkillGraph>)`
- Consumes: `ISkillGraphAdapter`

- [ ] **Step 1: Write a failing all-data migration test**

```csharp
[Test]
public void EveryCurrentSkillBuildsAValidGraph()
{
    var allyText = Resources.Load<TextAsset>("Data/AllyUnitData");
    var enemyText = Resources.Load<TextAsset>("Data/EnemyUnitData");
    var allies = JsonUtility.FromJson<AllyUnitDataCollection>(allyText.text);
    var enemies = JsonUtility.FromJson<EnemyUnitDataCollection>(enemyText.text);
    var adapter = new SkillGraphV1Adapter();
    var built = 0;

    foreach (var unit in allies.units)
    {
        if (unit.skill == null) continue;
        Assert.That(adapter.TryBuild(unit.id, unit.skill, out _, out var errors),
            Is.True, string.Join("\n", errors));
        built++;
    }

    foreach (var unit in enemies.units)
    foreach (var skill in unit.Skills ?? System.Array.Empty<UnitSkillData>())
    {
        Assert.That(adapter.TryBuild(unit.id, skill, out _, out var errors),
            Is.True, string.Join("\n", errors));
        built++;
    }

    Assert.That(built, Is.EqualTo(20));
}
```

- [ ] **Step 2: Run the adapter test and confirm failure on legacy effects**

Run Task 1 Step 2. Expected: current skills have no graph entries and validation fails.

- [ ] **Step 3: Replace all legacy effect arrays with these exact graph behaviors**

| Skill | Entries and graph behavior |
|---|---|
| `shield_judgment` | ManaFull → event target 160% damage → opponents within 2.5 of target force target source for 3s → self shield 25% max HP for 5s |
| `blood_whirlwind` | ManaFull → opponents within 1.8 of self take 180% → self heals 4% max HP per hit target capped at 20% → self attack rate +30% for 5s |
| `arrow_rain` | ManaFull → opponents within 2.5 of event target take 180% → move speed -25% for 5s |
| `piercing_shot` | ManaFull → line toward target, distance `AttackRange * 2`, half-width 0.5, max 4 → damage ratios 260/221/188/160% with 30% armor ignore |
| `explosive_fireball` | ManaFull → opponents within 2 of target take 220% and 120% total DOT over 4s, both with 30% armor ignore |
| `frost_storm` | ManaFull → opponents within 2.5 of target take 130% with 30% armor ignore → stun 1.5s → after 1.5s apply move/attack rate -35% for 4s |
| `piercing_charge` | ManaFull → line toward target, distance 5, half-width 0.6, max 5 → 200% damage → knockback 1.5 → source moves 5 |
| `phalanx_formation` | ManaFull → line distance 2.5, half-width 1.5 → 140% damage and 1s stun → allies within 2.5 of self receive 20% damage reduction and knockback immunity for 5s |
| `wolf_sprint` | BattleStart → self move speed +30% for 3s |
| `focused_fire` | BasicAttackHit updates same-target stack to max 4; BeforeBasicAttackDamage adds 5% damage per stored stack when target matches |
| `shield_block` | BeforeIncomingDamage + SourceInFront → incoming damage -25%; BeforeCrowdControl → duration -30% |
| `orc_rage` | Damaged + HP ≤50% + Once → self attack damage +25% and attack rate +20% permanently |
| `dark_blast` | Every 4th BasicAttackHit → opponents within 1.8 of event target take 140% → attack rate -15% for 3s |
| `shadow_leap` | BattleStart → farthest opponent → teleport source to 0.5 from target → 180% damage → force source target permanently |
| `troll_regeneration` | Periodic every 1s + not stunned → if out of combat for 3s heal 3% max HP, otherwise heal 1.5% |
| `ground_slam` | Every 3rd BasicAttackHit → opponents within 2.2 of self take 170% → stun 1s → knockback 1 |
| `weakening_curse` | Every 4th BasicAttackHit → highest-HP opponent takes 160% → defense -30% for 5s |
| `summon_minions` | Damaged has three independent Once branches at HP ≤75/50/25%; each branch summons 3 goblins and 2 wolves around source |
| `king_slam` | Every 4th BasicAttackHit → event target 220% and 1.5s stun → opponents within 2 of target take 120% |
| `final_order` | Damaged + HP ≤25% + Once → all allies gain move speed +20% and attack rate +20% permanently |

For line distance based on attack range, set `stat = "AttackRange"` and `distanceMultiplier = 2` in V1 data. `SkillNodeController` computes `owner.AttackRange * distanceMultiplier`; other line selectors use the explicit positive `distance`. Do not introduce an expression language.

- [ ] **Step 4: Build and cache validated graphs in TitleData**

After unit JSON dictionaries are loaded, use one `SkillGraphV1Adapter` instance to build graph lists keyed by unit ID. Log each validation error once. Units with invalid graphs receive an empty list while their stat data remains available.

```csharp
public bool TryGetAllySkillGraphs(
    string unitId,
    out IReadOnlyList<SkillGraph> result) =>
    allySkillGraphs.TryGetValue(unitId, out result);
```

- [ ] **Step 5: Run all adapter and validator tests**

Expected: exactly 20 graphs build, no missing links or cycles, and no legacy `effects` property is required.

- [ ] **Step 6: Commit the migrated skill data**

```powershell
git add -- "Assets/Resources/Data/AllyUnitData.json" "Assets/Resources/Data/EnemyUnitData.json" "Assets/02. Scripts/02. Data/TitleData.cs" "Assets/02. Scripts/Editor/Tests/SkillGraphAdapterTests.cs"
git commit -m "data: migrate current skills to node graphs"
```

### Task 5: UnitBase, AllyUnit, EnemyUnit를 그래프 런타임에 연결

**Files:**
- Modify: `Assets/02. Scripts/Battle/UnitBase.cs`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/EnemyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/UnitSpawner.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Modify: `Assets/02. Scripts/Editor/Tests/SkillRuntimeTests.cs`

**Interfaces:**
- Produces: `UnitBase.BindSkills(IReadOnlyList<SkillGraph>)`
- Produces: protected `PublishSkillEvent(...)` and `ModifySkillValue(...)`
- Changes: `AllyUnit.SetData` receives cloned runtime spawn data and common unit data; `EnemyUnit.SetData` keeps enemy identity data
- Consumes: `TitleData.TryGetAllySkillGraphs` and `TryGetEnemySkillGraphs`

- [ ] **Step 1: Add a failing source audit test**

```csharp
[TestCase("Battle/AlllyUnit.cs")]
[TestCase("Battle/EnemyUnit.cs")]
public void UnitsDoNotDispatchSkillsById(string relativePath)
{
    var path = Path.Combine(Application.dataPath, "02. Scripts", relativePath);
    var source = File.ReadAllText(path);

    StringAssert.DoesNotContain("FindSkill(", source);
    StringAssert.DoesNotContain("switch (_skill", source);
    StringAssert.DoesNotContain("case \"shield_judgment\"", source);
    StringAssert.DoesNotContain("UnitId == \"", source);
}
```

- [ ] **Step 2: Run SkillRuntimeTests and confirm source-audit failure**

Expected: existing skill switch, `FindSkill`, and enemy UnitId checks fail the test.

- [ ] **Step 3: Add UnitBase event hooks**

```csharp
public void BindSkills(IReadOnlyList<SkillGraph> graphs)
{
    _skillController = new UnitSkillController(this, _unitManager, graphs);
    _skillController.Publish(ESkillTrigger.BattleStart);
}

protected void PublishSkillEvent(
    ESkillTrigger trigger,
    UnitBase eventTarget = null) =>
    _skillController?.Publish(trigger, eventTarget);

protected float ModifySkillValue(
    ESkillTrigger trigger,
    float value,
    UnitBase eventTarget = null) =>
    _skillController?.Modify(trigger, value, eventTarget) ?? value;
```

Call `Tick(Time.deltaTime)` only while battle state is `Active`. Apply modifier hooks before basic attack damage, incoming raw damage, and crowd-control duration. Publish basic hit after damage and `Damaged` after HP changes but before death.

- [ ] **Step 4: Reduce AllyUnit to mana and generic trigger behavior**

On full mana, reset mana, set attacking state, and publish `ManaFull` with `_currentTarget`. Keep basic-hit and damaged mana gain. Remove all eight skill methods, target scratch list, legacy value helpers, and ID switch.

```csharp
if (_stats.MaxMana > 0f && CurrentMana >= _stats.MaxMana)
{
    CurrentMana = 0f;
    _state = EBattleUnitState.Attacking;
    PublishSkillEvent(ESkillTrigger.ManaFull, _currentTarget);
    return;
}
```

- [ ] **Step 5: Reduce EnemyUnit to identity and basic combat behavior**

Keep `UnitId`, `Rank`, `BreachDamage`, target acquisition, and basic movement/attack. Remove all twelve skill methods, counters, booleans, scratch target list, value helpers, and UnitId branches.

```csharp
protected override void Tick()
{
    if (TryKeepOrAcquireTarget())
    {
        MoveOrAttackTarget();
        return;
    }

    _state = EBattleUnitState.Idle;
    ClearTarget();
}
```

- [ ] **Step 6: Pass TitleData graph lists from UnitSpawner**

`UnitSpawner` assigns identity/common data and returns the unit without binding a graph. `UnitManager` obtains the graph list from `TitleData`, registers the returned unit with `AddAlly`/`AddEnemy`, then calls `BindSkills`. This ordering lets BattleStart selectors see every unit already spawned in the current loop. If each enemy currently binds immediately, move enemy graph binding to a second pass after `SpawnEnemies` has registered the complete wave.

- [ ] **Step 7: Run runtime, adapter, and source-audit tests**

Expected: all 20 graphs validate, representative graph execution tests pass, and source audit finds no skill ID dispatch.

- [ ] **Step 8: Commit unit integration**

```powershell
git add -- "Assets/02. Scripts/Battle/UnitBase.cs" "Assets/02. Scripts/Battle/AlllyUnit.cs" "Assets/02. Scripts/Battle/EnemyUnit.cs" "Assets/02. Scripts/Battle/UnitSpawner.cs" "Assets/02. Scripts/Battle/UnitManager.cs" "Assets/02. Scripts/Editor/Tests/SkillRuntimeTests.cs"
git commit -m "refactor: run unit skills through graph controller"
```

### Task 6: Run complete skill-system verification

**Files:**
- Verify only

**Interfaces:**
- Produces the graph interfaces consumed by plan 03 progression updates.

- [ ] **Step 1: Run all EditMode tests**

```powershell
$unityEditor = "C:\Program Files\Unity\Hub\Editor\6000.0.79f1\Editor\Unity.exe"
& $unityEditor -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults "Temp/skill-system-tests.xml" -logFile "Temp/skill-system-tests.log"
```

Expected: exit code 0.

- [ ] **Step 2: Audit coupling and data counts**

```powershell
rg -n "FindSkill|Value\(.*effectIndex|case \"(shield_judgment|blood_whirlwind|wolf_sprint|king_slam)\"|UnitId == \"" "Assets/02. Scripts/Battle" -g "*.cs"
$ally = Get-Content -Raw -Encoding UTF8 "Assets/Resources/Data/AllyUnitData.json" | ConvertFrom-Json
$enemy = Get-Content -Raw -Encoding UTF8 "Assets/Resources/Data/EnemyUnitData.json" | ConvertFrom-Json
"ally=$(@($ally.units | Where-Object skill).Count), enemy=$(($enemy.units | ForEach-Object { @($_.skills).Count } | Measure-Object -Sum).Sum)"
```

Expected: coupling search returns no matches and counts are `ally=8, enemy=12`.

- [ ] **Step 3: Confirm a clean task boundary**

```powershell
git status --short
```

Expected: no uncommitted files from plan 02.
