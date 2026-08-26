# Two-Stage Ally Evolution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build four five-job ally trees with level-5 branching, level-20 automatic evolution, 16 role-correct skills, and complete battle animation profiles.

**Architecture:** Keep JSON as source of truth and identify jobs with asset-aligned IDs (`bear1` through `rabbit5`). Extend the existing merge service to use each job's `requiredLevel` and child count, then keep skill behavior in focused `AllySkillBase` implementations while values remain in JSON. Extend `BattleUnitVisual` with a skill state and configure all profiles on the existing pooled ally prefab.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity serialized prefabs, JSON resources

**Spec:** `docs/superpowers/specs/2026-08-26-two-stage-ally-evolution-design.md`

## Global Constraints

- Maximum ally level is 30.
- First evolution occurs at level 5 and presents exactly two choices.
- Second evolution occurs at level 20 and follows the selected branch automatically.
- Use IDs `bear1`-`bear5`, `dog1`-`dog5`, `cat1`-`cat5`, and `rabbit1`-`rabbit5`.
- Basic jobs have no active skill; all 16 evolved jobs have a unique active skill.
- Preserve JSON data ownership, pooling, Inspector references, existing folder structure, enemy behavior, and WebGL compatibility.
- Do not install packages, rename files outside this feature, or modify unrelated assets.

---

### Task 1: Represent two evolution tiers in ally data

**Files:**
- Modify: `Assets/02. Scripts/02. Data/AllyUnitData.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitCreationService.cs`
- Test: `Assets/02. Scripts/Battle/Editor/UnitCreationServiceTests.cs`

**Interfaces:**
- Produces: `AllyCommonData.firstClassLevel`, `AllyCommonData.secondClassLevel`, `AllyUnitData.requiredLevel`
- Produces: `AllyUnitData.CreateStats(int level)` using its own required level

- [ ] **Step 1: Write failing data and stat-origin tests**

Add tests proving a base job grows from level 1, a first job from level 5, and a second job from level 20:

```csharp
[TestCase(1, 1, 100f)]
[TestCase(5, 5, 100f)]
[TestCase(20, 20, 100f)]
public void CreateStats_UsesRequiredLevelAsGrowthOrigin(
    int level,
    int requiredLevel,
    float expectedHp)
{
    var data = new AllyUnitData
    {
        health = 100f,
        healthGrowth = 10f,
        attack = 10f,
        attackGrowth = 1f,
        defense = 2f,
        defenseGrowth = 1f,
        attackSpeed = 1f,
        requiredLevel = requiredLevel
    };

    Assert.That(data.CreateStats(level).MaxHp, Is.EqualTo(expectedHp));
}
```

- [ ] **Step 2: Run focused tests and verify failure**

Run Unity EditMode tests for `UnitCreationServiceTests`. Expected: compile failure because `requiredLevel` and the one-argument `CreateStats` overload do not exist.

- [ ] **Step 3: Add explicit tier fields and required-level growth**

Implement:

```csharp
public class AllyCommonData
{
    public int maxLevel;
    public int firstClassLevel;
    public int secondClassLevel;
    public float startMana;
    public float basicAttackManaGain;
    public float hitManaGain;
    public float hitManaGainCooldown;
}

public int requiredLevel = 1;

public BattleUnitStats CreateStats(int level)
{
    int growthLevels = Mathf.Max(0, level - Mathf.Max(1, requiredLevel));
    // Apply existing growth formulas with growthLevels.
}
```

Update `UnitCreationService.TryCreateAlly` to clamp level between `requiredLevel` and `maxLevel`, then call `CreateStats(spawnData.Level)`.

- [ ] **Step 4: Run focused tests and verify pass**

Run `UnitCreationServiceTests`. Expected: all pass.

- [ ] **Step 5: Commit**

```text
feat(battle): support two ally evolution tiers
```

### Task 2: Make merge decisions follow the five-job tree

**Files:**
- Modify: `Assets/02. Scripts/Battle/Units/UnitMergeService.cs`
- Modify: `Assets/02. Scripts/Battle/Units/UnitMergeDecision.cs`
- Modify: `Assets/02. Scripts/Battle/UnitManager.cs`
- Test: `Assets/02. Scripts/Battle/Editor/UnitMergeServiceTests.cs`

**Interfaces:**
- Consumes: `requiredLevel`, `firstClassLevel`, `secondClassLevel`
- Produces: level-5 `EvolutionRequired` with two choices
- Produces: level-20 `Immediate` decision with the sole child job ID

- [ ] **Step 1: Replace single-tier tests with full-tree cases**

Build a fake tree `bear1 -> bear2/bear3`, `bear2 -> bear4`, `bear3 -> bear5`. Add tests:

```csharp
[Test]
public void TryBegin_LevelFive_ReturnsTwoFirstEvolutionChoices()
{
    AllyUnit left = CreateUnit("bear1", 4);
    AllyUnit right = CreateUnit("bear1", 4);
    UnitMergeDecision result = _service.TryBegin(left, right);

    Assert.That(result.Type, Is.EqualTo(UnitMergeDecisionType.EvolutionRequired));
    Assert.That(
        new[] { result.FirstChoice.id, result.SecondChoice.id },
        Is.EquivalentTo(new[] { "bear2", "bear3" }));
}

[Test]
public void TryBegin_LevelTwenty_AutomaticallyUsesOnlyChild()
{
    AllyUnit left = CreateUnit("bear2", 19);
    AllyUnit right = CreateUnit("bear2", 19);
    UnitMergeDecision result = _service.TryBegin(left, right);

    Assert.That(result.Type, Is.EqualTo(UnitMergeDecisionType.Immediate));
    Assert.That(result.ResultUnitId, Is.EqualTo("bear4"));
}
```

Also test cross-branch rejection, preservation of reservations on failed choice, and level-30 rejection.

- [ ] **Step 2: Run focused tests and verify current single-tier behavior fails**

Run `UnitMergeServiceTests`. Expected: level-20 child remains `bear2` instead of `bear4`.

- [ ] **Step 3: Implement child-count evolution rules**

After calculating `resultLevel`, query children of the current job:

```csharp
_dataSource.GetNextAllyJobs(resultJob.id, _evolutionCandidates);

if (resultLevel == common.firstClassLevel)
{
    return RequireChoiceWhenExactlyTwoChildren(...);
}

if (resultLevel == common.secondClassLevel)
{
    return EvolveImmediatelyWhenExactlyOneChild(...);
}
```

Require matching current job IDs after the first evolution. Keep root matching only for levels below 5. Return a rejected decision and release reservations when child counts violate the tree contract.

- [ ] **Step 4: Preserve HP ratio through evolved replacement**

Add current HP ratio to `UnitMergeDecision`, calculate a deterministic ratio from the consumed units, and apply it in `UnitManager.SpawnMergedAlly` after initialization.

- [ ] **Step 5: Run merge and creation tests**

Run `UnitMergeServiceTests` and `UnitCreationServiceTests`. Expected: all pass.

- [ ] **Step 6: Commit**

```text
feat(battle): add automatic second evolution
```

### Task 3: Replace ally data with 20 asset-aligned jobs

**Files:**
- Modify: `Assets/Resources/Data/AllyUnitData.json`
- Modify: `Assets/02. Scripts/02. Data/TitleData.cs`
- Test: `Assets/02. Scripts/Battle/Editor/BattleDataCharacterizationTests.cs`

**Interfaces:**
- Produces: four roots, eight first jobs, eight second jobs
- Produces: skill data IDs consumed by Tasks 4 and 5

- [ ] **Step 1: Add structural validation tests**

Assert `maxLevel == 30`, evolution levels `5` and `20`, 20 unique IDs, four roots, correct child counts, no cycles, and exact lineage edges from the spec.

```csharp
[Test]
public void AllyData_DefinesCompleteTwoStageTrees()
{
    Assert.That(_titleData.AllyCommon.maxLevel, Is.EqualTo(30));
    Assert.That(_titleData.AllyCommon.firstClassLevel, Is.EqualTo(5));
    Assert.That(_titleData.AllyCommon.secondClassLevel, Is.EqualTo(20));
    Assert.That(AllIds(), Is.EquivalentTo(ExpectedTwentyIds));
    AssertEdge("bear1", "bear2", "bear3");
    AssertEdge("bear2", "bear4");
    AssertEdge("bear3", "bear5");
}
```

Repeat edge assertions for dog, cat, and rabbit.

- [ ] **Step 2: Run the characterization test and verify failure**

Expected: old generic IDs and 12-job count fail.

- [ ] **Step 3: Rewrite JSON IDs, ancestry, stats, and descriptions**

Enter all 20 rows exactly as approved in the spec. Set `requiredLevel` to 1, 5, or 20. Give basic jobs `mana: 0` and omit `skill`. Give every evolved job one of the 16 IDs from Tasks 4 and 5. Preserve common mana-gain values unless a focused balance test establishes a defect.

- [ ] **Step 4: Make lineage traversal support arbitrary depth**

Keep `TitleData.TryGetRootAllyJob` iterative, add a visited-ID set, and return false on missing parents or cycles. Ensure `GetNextAllyJobs` returns direct children only.

- [ ] **Step 5: Run data, creation, and merge tests**

Expected: all pass with real JSON.

- [ ] **Step 6: Commit**

```text
feat(data): define twenty ally jobs
```

### Task 4: Implement bear and dog skill families

**Files:**
- Create: `Assets/02. Scripts/Battle/Skills/Ally/IronFistSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/FeralRampageSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/ShadowSlashSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/CloneFlurrySkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/BattleCrySkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/IronFormationSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/PiercingChargeSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/TranscendentChargeSkill.cs`
- Modify: `Assets/02. Scripts/Battle/Skills/UnitSkillRegistry.cs`
- Modify: `Assets/Resources/Data/AllyUnitData.json`
- Test: `Assets/02. Scripts/Battle/Editor/AllyMeleeSkillTests.cs`

**Interfaces:**
- Produces skill IDs: `iron_fist`, `feral_rampage`, `shadow_slash`, `clone_flurry`, `battle_cry`, `iron_formation`, `piercing_charge`, `transcendent_charge`

- [ ] **Step 1: Write one focused test fixture per targeting pattern**

Test exact target selection and observable outcomes: HP loss, healing, shield, forced position, stun, knockback, attack-rate multiplier, and damage reduction. Use deterministic `UnitSkillContext` fakes and JSON-shaped `AllySkillData` values.

```csharp
[Test]
public void IronFist_DamagesTargetAndShieldsCaster()
{
    Execute("iron_fist", caster, target, Effects(180f, 5f, 20f));
    Assert.That(target.Hp, Is.LessThan(target.MaxHp));
    Assert.That(caster.Shield, Is.EqualTo(caster.MaxHp * 0.20f));
}
```

- [ ] **Step 2: Run tests and verify missing skill failures**

Expected: registry cannot create the new IDs.

- [ ] **Step 3: Implement eight focused skill classes**

Each class exposes one exact ID and reads every tunable number through `UnitSkillValueReader`. Reuse existing target-finder APIs. Add only the narrow UnitBase API needed by an approved effect, such as a read-only shield value for tests; do not introduce a generic effect engine.

- [ ] **Step 4: Register the eight classes and set JSON coefficients**

Use conservative initial values: first jobs 140%-190% attack scaling and 3-5 second buffs; second jobs 200%-320% scaling and stronger utility. Healing and shields remain percentage-of-max-HP values.

- [ ] **Step 5: Run melee skill tests and existing battle tests**

Expected: new tests pass; enemy skills remain registered and passing.

- [ ] **Step 6: Commit**

```text
feat(battle): rebuild bear and dog skills
```

### Task 5: Implement cat and rabbit skill families

**Files:**
- Create: `Assets/02. Scripts/Battle/Skills/Ally/RapidFireSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/ExplosiveBarrageSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/PiercingShotSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/FinishingPierceSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/HealingLightSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/PrayerOfLifeSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/ExplosiveFireballSkill.cs`
- Create: `Assets/02. Scripts/Battle/Skills/Ally/ManaStormSkill.cs`
- Modify: `Assets/02. Scripts/Battle/Skills/UnitSkillRegistry.cs`
- Modify: `Assets/Resources/Data/AllyUnitData.json`
- Test: `Assets/02. Scripts/Battle/Editor/AllyRangedSkillTests.cs`

**Interfaces:**
- Produces skill IDs: `rapid_fire`, `explosive_barrage`, `piercing_shot`, `finishing_pierce`, `healing_light`, `prayer_of_life`, `explosive_fireball`, `mana_storm`

- [ ] **Step 1: Write ranged and support skill tests**

Cover multi-hit totals, radius exclusion, line ordering, armor ignore, lowest-HP selection, area healing, shielding, burn ticks, stun, and delayed slow.

```csharp
[Test]
public void HealingLight_HealsLowestHpAllyOnly()
{
    Execute("healing_light", healer, primaryEnemy, Effects(35f));
    Assert.That(lowestHpAlly.Hp, Is.EqualTo(expectedHealedHp));
    Assert.That(otherAlly.Hp, Is.EqualTo(originalOtherHp));
}
```

- [ ] **Step 2: Run tests and verify missing skill failures**

Expected: registry cannot create new IDs and no ally-healing active skill exists.

- [ ] **Step 3: Add ally-targeting context needed by healing skills**

Expose the existing `FindHighestHpAliveAlly`/roster queries through a narrow lowest-HP lookup on `UnitTargetFinder` and `UnitSkillContext`. Test empty-roster and dead-unit exclusion.

- [ ] **Step 4: Implement and register eight skills**

Keep damage application in each class explicit. Use existing damage-over-time, stun, slow, shield, and line/radius APIs. Do not alter enemy skill classes.

- [ ] **Step 5: Set JSON coefficients and run all skill tests**

Use first-job scaling of 130%-220%, second-job scaling of 220%-360%, healer recovery of 25%-40% max HP, and second-healer shields of 15%-25% max HP. Expected: all ally and enemy skill tests pass.

- [ ] **Step 6: Commit**

```text
feat(battle): rebuild cat and rabbit skills
```

### Task 6: Add skill-cast animation state and timing

**Files:**
- Modify: `Assets/02. Scripts/Battle/EBattleUnitState.cs`
- Modify: `Assets/02. Scripts/Battle/BattleUnitVisual.cs`
- Modify: `Assets/02. Scripts/Battle/AlllyUnit.cs`
- Modify: `Assets/02. Scripts/Battle/Skills/Ally/AllySkillController.cs`
- Test: `Assets/02. Scripts/Battle/Editor/BattleUnitVisualTests.cs`
- Test: `Assets/02. Scripts/Battle/Editor/AllySkillAnimationTests.cs`

**Interfaces:**
- Produces: `EBattleUnitState.CastingSkill`
- Produces: `BattleUnitVisual.PlaySkill(Action impact, Action complete)`
- Produces: profile fields `skillFrames`, `skillFramesPerSecond`, `skillImpactNormalizedTime`

- [ ] **Step 1: Write tests for cast-state lifecycle**

Verify entering cast state, invoking impact once at the configured normalized time, rendering every skill frame, returning to idle/targeting state, and cancelling callbacks on pool return.

- [ ] **Step 2: Run tests and verify failure**

Expected: cast state and skill-frame profile do not exist.

- [ ] **Step 3: Extend animation profiles and one-shot playback**

Add skill frames and timing fields without changing idle/move/attack looping. During `CastingSkill`, clamp frame index instead of wrapping it. Fire impact exactly once, then completion exactly once.

- [ ] **Step 4: Defer skill effects until visual impact callback**

Change `AllySkillController` so mana is validated first, the cast starts, and `active.Execute` runs at impact. Missing registry entries must warn without consuming mana. Pool reset must clear pending callbacks.

- [ ] **Step 5: Run visual, skill, pool, and attack tests**

Expected: all pass and basic attacks still use existing attack timing.

- [ ] **Step 6: Commit**

```text
feat(battle): animate ally skill casts
```

### Task 7: Configure all 20 visual profiles on the ally prefab

**Files:**
- Modify: `Assets/04. Prefabs/AllyUnit.prefab`
- Modify: sprite importer `.meta` files under `Assets/03. Images/Animals/`
- Test: `Assets/02. Scripts/Battle/Editor/BattleUnitVisualTests.cs`

**Interfaces:**
- Consumes: 20 asset-aligned job IDs and `ConfigureUnitAnimation` extended with skill frames
- Produces: complete serialized profile set on `AllyUnit.prefab`

- [ ] **Step 1: Add importer and profile completeness tests**

For every ID, assert nonempty idle, move, attack, and skill arrays. Assert second jobs use their `*_skill` sprite sheet. Assert first jobs without a skill sheet use their attack array as skill frames.

- [ ] **Step 2: Run tests and verify eight missing or mismatched profiles**

Expected: old generic IDs and missing new profiles fail.

- [ ] **Step 3: Normalize sprite import settings**

Ensure each runtime PNG is Multiple when it is a sheet, uses Point filtering, no compression, correct pixels-per-unit, transparent alpha, stable frame names, and frame order matching the sheet. Leave `.pxo` files unreferenced.

- [ ] **Step 4: Serialize 20 prefab profiles**

Map base and evolution sprites exactly by ID. Preserve `sourceFacesRight` behavior and sorting order. Use static Tiny32 sprite for idle when no idle sheet exists.

- [ ] **Step 5: Run profile and importer tests**

Expected: all 20 profiles complete and all frame references resolve.

- [ ] **Step 6: Commit**

```text
feat(art): connect all ally evolution sprites
```

### Task 8: Update UI IDs, portraits, and evolution presentation

**Files:**
- Modify: `Assets/02. Scripts/03. UI/AllyPortraitProvider.cs`
- Modify: `Assets/02. Scripts/03. UI/EvolutionPanel.cs`
- Modify: `Assets/02. Scripts/03. UI/EvolutionChoiceView.cs`
- Modify: UI tests under `Assets/02. Scripts/03. UI/Editor/`

**Interfaces:**
- Consumes: new 20-job data and direct-child evolution choices
- Produces: correct portrait, name, role, skill, and level information

- [ ] **Step 1: Write UI mapping and presentation tests**

Assert all 20 IDs return a non-null portrait. Assert level-5 choices display two direct children and level-20 automatic evolution never opens the panel.

- [ ] **Step 2: Run tests and verify old ID mappings fail**

Expected: portrait dictionary lacks asset-aligned IDs.

- [ ] **Step 3: Replace portrait mapping**

Keep `AllyPortraitProvider` resource-based and reuse the closest existing portrait for intermediate/final members of each branch. Map bear fighter/ninja branches to the existing bear tank/damage portraits, cat gun/bow branches to cat ranger/crossbow portraits, rabbit heal/mage branches to rabbit ice/fire portraits, and dog warrior/lancer branches to dog guardian/lancer portraits. Assert every ID resolves; do not search `Assets` at runtime.

- [ ] **Step 4: Update evolution labels and panel conditions**

Display job name, role, and skill description from JSON. Keep panel limited to two-choice decisions; automatic second evolution uses only existing glow and sound feedback.

- [ ] **Step 5: Run UI and merge tests**

Expected: all pass.

- [ ] **Step 6: Commit**

```text
feat(ui): present two-stage ally jobs
```

### Task 9: Run integration validation and record AI usage

**Files:**
- Create: `docs/2026-08-26-two-stage-ally-evolution-ai-usage.md`
- Modify only if validation exposes an in-scope defect: files from Tasks 1-8

**Interfaces:**
- Consumes: completed feature
- Produces: verified Unity project and factual work record

- [ ] **Step 1: Run focused EditMode groups**

Run data, creation, merge, melee skill, ranged skill, skill-animation, visual-profile, pooling, and UI tests. Expected: zero failures.

- [ ] **Step 2: Run complete EditMode suite**

Run Unity 6 batch EditMode tests. Expected: process exit code 0 and zero failed tests in XML.

- [ ] **Step 3: Verify Game scene manually**

Check each family through levels 5 and 20, both first branches, automatic second evolution, HP-ratio preservation, skill target/effect, animation completion, facing, death, and pooled reuse.

- [ ] **Step 4: Verify WebGL compile**

Run the project's existing WebGL build validation path. Expected: no compile errors and no missing serialized references.

- [ ] **Step 5: Write factual AI usage record**

Record model/tool, user request, approved design, exact modified area, user decisions, key instructions, automated tests, manual checks, and remaining balance limitations.

- [ ] **Step 6: Inspect final diff and commit**

Confirm no unrelated files are staged, especially the pre-existing `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset` modification.

```text
docs: record ally evolution implementation
```
