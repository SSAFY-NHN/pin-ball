#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AllyProgressionBattleManagerTests
{
    private GameObject battleObject;
    private GameObject unitObject;
    private GameObject allyObject;

    [TearDown]
    public void TearDown()
    {
        if (battleObject != null) Object.DestroyImmediate(battleObject);
        if (unitObject != null) Object.DestroyImmediate(unitObject);
        if (allyObject != null) Object.DestroyImmediate(allyObject);
    }

    [Test]
    public void TryLevelUpAllyJob_InPreparationSpendsExactCostAndRaisesLevel()
    {
        BattleManager manager = CreateManager(EWaveState.Pending, 1000, true);
        int eventCount = 0;
        manager.OnAllyProgressionChanged += unitId =>
        {
            if (unitId == "warrior") eventCount++;
        };

        Assert.That(manager.TryLevelUpAllyJob("warrior"), Is.True);
        Assert.That(manager.Gold, Is.EqualTo(850));
        Assert.That(manager.GetAllyJobLevel("warrior"), Is.EqualTo(2));
        Assert.That(eventCount, Is.EqualTo(1));
    }

    [TestCase(EWaveState.Active)]
    [TestCase(EWaveState.Resolving)]
    public void TryLevelUpAllyJob_OutsidePreparationPreservesGoldAndLevel(
        EWaveState state)
    {
        BattleManager manager = CreateManager(state, 1000, true);

        Assert.That(manager.TryLevelUpAllyJob("warrior"), Is.False);
        Assert.That(manager.Gold, Is.EqualTo(1000));
        Assert.That(manager.GetAllyJobLevel("warrior"), Is.EqualTo(1));
    }

    [Test]
    public void TryLevelUpAllyJob_BaseJobsDoNotRequirePurchasedUnit()
    {
        BattleManager noOwner = CreateManager(EWaveState.Pending, 1000, false);
        Assert.That(noOwner.TryLevelUpAllyJob("warrior"), Is.True);
        Assert.That(noOwner.Gold, Is.EqualTo(850));
        Assert.That(noOwner.GetAllyJobLevel("warrior"), Is.EqualTo(2));
    }

    [Test]
    public void TryLevelUpAllyJob_StillRequiresEnoughGold()
    {
        BattleManager poor = CreateManager(EWaveState.Pending, 149, false);

        Assert.That(poor.TryLevelUpAllyJob("warrior"), Is.False);
        Assert.That(poor.Gold, Is.EqualTo(149));
    }

    private BattleManager CreateManager(
        EWaveState state,
        int gold,
        bool ownsWarrior)
    {
        unitObject = new GameObject("Unit Manager");
        UnitManager unitManager = unitObject.AddComponent<UnitManager>();
        var roster = new UnitRoster();
        SetField(unitManager, "_roster", roster);

        if (ownsWarrior)
        {
            allyObject = new GameObject("Warrior");
            var ally = allyObject.AddComponent<AllyUnit>();
            ally.Initialize(new BattleUnitStats { MaxHp = 100f }, null);
            ally.SetData("warrior", 1, null, new AllyCommonData());
            roster.AddOwnedAlly(ally);
        }

        battleObject = new GameObject("Battle Manager");
        BattleManager manager = battleObject.AddComponent<BattleManager>();
        var runState = new BattleRunState(
            new[] { new BattleWaveData() }, true, 3);
        runState.ChangeState(state);
        SetField(manager, "<IsInitialized>k__BackingField", true);
        SetField(manager, "runState", runState);
        SetField(manager, "unitManager", unitManager);
        SetField(manager, "economy", new BattleEconomy(gold));
        SetField(manager, "allyProgressionController",
            new AllyProgressionController());
        return manager;
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
