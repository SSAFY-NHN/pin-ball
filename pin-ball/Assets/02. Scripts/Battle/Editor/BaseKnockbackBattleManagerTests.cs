#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BaseKnockbackBattleManagerTests
{
    private GameObject battleObject;
    private GameObject unitObject;
    private GameObject allyLineObject;
    private GameObject enemyLineObject;
    private GameObject enemyObject;

    [TearDown]
    public void TearDown()
    {
        if (battleObject != null) Object.DestroyImmediate(battleObject);
        if (unitObject != null) Object.DestroyImmediate(unitObject);
        if (allyLineObject != null) Object.DestroyImmediate(allyLineObject);
        if (enemyLineObject != null) Object.DestroyImmediate(enemyLineObject);
        if (enemyObject != null) Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void AdvanceBaseKnockbackSkill_ProgressesOnlyDuringActiveGameTime()
    {
        BattleManager manager = CreateManager(EWaveState.Pending, addEnemy: false);

        Advance(manager, 30f);
        Assert.That(manager.BaseKnockbackRemainingTime, Is.EqualTo(30f));
        Assert.That(
            manager.BaseKnockbackSkillState,
            Is.EqualTo(EBaseKnockbackSkillState.Locked));

        GetField<BattleRunState>(manager, "runState")
            .ChangeState(EWaveState.Active);
        Advance(manager, 29.99f);
        Assert.That(
            manager.BaseKnockbackSkillState,
            Is.EqualTo(EBaseKnockbackSkillState.Locked));
        Advance(manager, 0.01f);
        Assert.That(
            manager.BaseKnockbackSkillState,
            Is.EqualTo(EBaseKnockbackSkillState.Ready));
    }

    [TestCase(EWaveState.Pending)]
    [TestCase(EWaveState.Resolving)]
    [TestCase(EWaveState.Victory)]
    [TestCase(EWaveState.Defeat)]
    public void TryUseBaseKnockbackSkill_RejectsInactiveStatesWithoutMutation(
        EWaveState state)
    {
        BattleManager manager = CreateManager(state, addEnemy: true);
        BaseKnockbackSkillController controller =
            GetField<BaseKnockbackSkillController>(
                manager,
                "baseKnockbackSkillController");
        controller.Advance(30f, true);
        int goldBefore = manager.Gold;
        Vector3 positionBefore = enemyObject.transform.position;

        Assert.That(manager.TryUseBaseKnockbackSkill(), Is.False);
        Assert.That(manager.BaseKnockbackSkillState,
            Is.EqualTo(EBaseKnockbackSkillState.Ready));
        Assert.That(manager.Gold, Is.EqualTo(goldBefore));
        Assert.That(enemyObject.transform.position, Is.EqualTo(positionBefore));
        Assert.That(manager.State, Is.EqualTo(state));
    }

    [Test]
    public void TryUseBaseKnockbackSkill_EmptyRosterKeepsReadyUse()
    {
        BattleManager manager = CreateManager(EWaveState.Active, addEnemy: false);
        Advance(manager, 30f);

        Assert.That(manager.CanUseBaseKnockbackSkill, Is.False);
        Assert.That(manager.TryUseBaseKnockbackSkill(), Is.False);
        Assert.That(manager.BaseKnockbackSkillState,
            Is.EqualTo(EBaseKnockbackSkillState.Ready));
    }

    [Test]
    public void TryUseBaseKnockbackSkill_AllImmuneKeepsReadyUse()
    {
        BattleManager manager = CreateManager(EWaveState.Active, addEnemy: true);
        enemyObject.GetComponent<EnemyUnit>().ApplyKnockbackImmunity(10f);
        Advance(manager, 30f);

        Assert.That(manager.CanUseBaseKnockbackSkill, Is.True);
        Assert.That(manager.TryUseBaseKnockbackSkill(), Is.False);
        Assert.That(manager.BaseKnockbackSkillState,
            Is.EqualTo(EBaseKnockbackSkillState.Ready));
    }

    [Test]
    public void TryUseBaseKnockbackSkill_SuccessConsumesUseAndRejectsSecondRequest()
    {
        BattleManager manager = CreateManager(EWaveState.Active, addEnemy: true);
        Advance(manager, 30f);

        Assert.That(manager.TryUseBaseKnockbackSkill(), Is.True);
        Assert.That(manager.BaseKnockbackSkillState,
            Is.EqualTo(EBaseKnockbackSkillState.Used));
        Assert.That(enemyObject.transform.position, Is.EqualTo(Vector3.right * 3f));

        Assert.That(manager.TryUseBaseKnockbackSkill(), Is.False);
        Assert.That(enemyObject.transform.position, Is.EqualTo(Vector3.right * 3f));
    }

    [Test]
    public void BaseKnockbackDisplayEvent_FiresOnlyForDisplayOrUseChanges()
    {
        BattleManager manager = CreateManager(EWaveState.Active, addEnemy: true);
        var eventCount = 0;
        manager.OnBaseKnockbackSkillDisplayChanged += () => eventCount++;

        Advance(manager, 0.1f);
        Assert.That(eventCount, Is.Zero);
        Advance(manager, 1f);
        Assert.That(eventCount, Is.EqualTo(1));
        Advance(manager, 28.9f);
        Assert.That(eventCount, Is.EqualTo(2));
        manager.TryUseBaseKnockbackSkill();
        Assert.That(eventCount, Is.EqualTo(3));
    }

    private BattleManager CreateManager(EWaveState state, bool addEnemy)
    {
        unitObject = new GameObject("Unit Manager");
        UnitManager unitManager = unitObject.AddComponent<UnitManager>();
        var roster = new UnitRoster();
        SetField(unitManager, "_roster", roster);
        allyLineObject = new GameObject("Ally Defense Line");
        enemyLineObject = new GameObject("Enemy Defense Line");
        enemyLineObject.transform.position = Vector3.right * 5f;
        SetField(
            unitManager,
            "allyDefenseLine",
            allyLineObject.AddComponent<DefenseLineTrigger>());
        SetField(
            unitManager,
            "enemyDefenseLine",
            enemyLineObject.AddComponent<DefenseLineTrigger>());

        if (addEnemy)
        {
            enemyObject = new GameObject("enemy");
            var enemy = enemyObject.AddComponent<EnemyUnit>();
            enemy.Initialize(new BattleUnitStats { MaxHp = 10f }, null);
            roster.AddEnemy(enemy);
        }

        battleObject = new GameObject("Battle Manager");
        BattleManager manager = battleObject.AddComponent<BattleManager>();
        var runState = new BattleRunState(
            new[] { new BattleWaveData() },
            true,
            3);
        runState.ChangeState(state);
        SetField(manager, "<IsInitialized>k__BackingField", true);
        SetField(manager, "runState", runState);
        SetField(manager, "unitManager", unitManager);
        SetField(manager, "economy", new BattleEconomy(100));
        SetField(
            manager,
            "baseKnockbackSkillController",
            new BaseKnockbackSkillController());
        return manager;
    }

    private static T GetField<T>(object target, string name)
    {
        return (T)target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    }

    private static void Advance(BattleManager manager, float deltaTime)
    {
        manager.GetType().GetMethod(
            "AdvanceBaseKnockbackSkill",
            BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
            manager,
            new object[] { deltaTime });
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
            target,
            value);
    }
}
#endif
