#if UNITY_EDITOR
using System.Reflection;

using NUnit.Framework;
using UnityEngine;

public sealed class WaveRosterResetPurchaseStateTests
{
    private GameObject battleObject;
    private GameObject unitObject;

    [TearDown]
    public void TearDown()
    {
        if (battleObject != null) Object.DestroyImmediate(battleObject);
        if (unitObject != null) Object.DestroyImmediate(unitObject);
    }

    [TestCase(0, true)]
    [TestCase(UnitManager.MaxDeployedAllyCount, true)]
    [TestCase(UnitManager.MaxDeployedAllyCount + 1, false)]
    public void CanStartWaveWithAllyCount_AllowsEmptyRosterWithinLimit(
        int allyCount,
        bool expected)
    {
        Assert.That(
            UnitManager.CanStartWaveWithAllyCount(allyCount),
            Is.EqualTo(expected));
    }

    [Test]
    public void CanStartCurrentWave_AllowsInitializedPendingRunWithEmptyRoster()
    {
        BattleManager manager = CreateBattleManager(EWaveState.Pending);

        Assert.That(manager.CanStartCurrentWave, Is.True);
    }

    [TestCase(EWaveState.Pending)]
    [TestCase(EWaveState.Resolving)]
    [TestCase(EWaveState.Victory)]
    [TestCase(EWaveState.Defeat)]
    public void InactiveState_BlocksPaidPurchaseWithoutMutatingState(
        EWaveState state)
    {
        BattleManager manager = CreateBattleManager(state);
        var economy = GetField<BattleEconomy>(manager, "economy");
        var purchases = GetField<UnitPurchaseController>(
            manager,
            "unitPurchaseController");
        int goldBefore = economy.Gold;

        Assert.That(manager.CanPurchaseAlly("warrior"), Is.False);
        Assert.That(manager.TryPurchaseAlly("warrior"), Is.False);
        Assert.That(economy.Gold, Is.EqualTo(goldBefore));
        Assert.That(purchases.GetPurchaseCount("warrior"), Is.Zero);
        Assert.That(purchases.GetNextCost("warrior"), Is.EqualTo(30));
        Assert.That(purchases.GetRemainingCooldown("warrior"), Is.Zero);
        Assert.That(GetUnitManager(manager).DeployedAllyCount, Is.Zero);
    }

    [TestCase(EWaveState.Pending)]
    [TestCase(EWaveState.Resolving)]
    [TestCase(EWaveState.Victory)]
    [TestCase(EWaveState.Defeat)]
    public void InactiveState_BlocksFreePurchaseWithoutConsumingTicket(
        EWaveState state)
    {
        BattleManager manager = CreateBattleManager(state, grantTicket: true);

        Assert.That(manager.CanPurchaseAlly("warrior"), Is.False);
        Assert.That(manager.TryPurchaseAlly("warrior"), Is.False);
        Assert.That(manager.HasTacticalReinforcement, Is.True);
        Assert.That(manager.GetAllyPurchaseCount("warrior"), Is.Zero);
        Assert.That(manager.GetAllyPurchaseCost("warrior"), Is.EqualTo(30));
        Assert.That(manager.GetAllyRemainingCooldown("warrior"), Is.Zero);
        Assert.That(GetUnitManager(manager).DeployedAllyCount, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ActiveState_AllowsPurchaseWhenExistingConditionsPass(
        bool grantTicket)
    {
        BattleManager manager = CreateBattleManager(
            EWaveState.Active,
            grantTicket);

        Assert.That(manager.CanPurchaseAlly("warrior"), Is.True);
    }

    [Test]
    public void ResolveWaveResult_ReturnsEveryOwnedAllyToPool()
    {
        unitObject = new GameObject("Unit Manager");
        var spawner = unitObject.AddComponent<UnitSpawner>();
        var manager = unitObject.AddComponent<UnitManager>();
        var area = unitObject.AddComponent<BattleAreaBounds>();
        var roster = new UnitRoster();
        SetField(manager, "_roster", roster);
        SetField(manager, "_spawner", spawner);
        SetField(
            manager,
            "_preparationController",
            new UnitPreparationController(roster, area));
        var allyObject = new GameObject("ally");
        var ally = allyObject.AddComponent<AllyUnit>();
        ally.Initialize(new BattleUnitStats { MaxHp = 10f }, null);
        roster.AddOwnedAlly(ally);

        manager.ResolveWaveResult();

        Assert.That(manager.DeployedAllyCount, Is.Zero);
        Assert.That(manager.RemainingAllyCount, Is.Zero);
        Assert.That(ally.IsInPool, Is.True);
        Assert.That(ally.gameObject.activeSelf, Is.False);
        Assert.That(ally.transform.parent, Is.EqualTo(spawner.transform));
    }

    [TestCase(EWaveResolutionResult.Cleared, false, 3, EWaveState.Pending, 1)]
    [TestCase(EWaveResolutionResult.Failed, false, 2, EWaveState.Pending, 0)]
    [TestCase(EWaveResolutionResult.Cleared, true, 3, EWaveState.Victory, 0)]
    [TestCase(EWaveResolutionResult.Failed, true, 0, EWaveState.Defeat, 0)]
    public void FinishWaveResolution_ResetsPurchaseStateAndPreservesRunResources(
        EWaveResolutionResult result,
        bool finalWave,
        int remainingChances,
        EWaveState expectedState,
        int expectedWaveIndex)
    {
        BattleManager manager = CreateResolvingBattleManager(
            result,
            finalWave,
            remainingChances);
        int goldBefore = manager.Gold;

        InvokePrivate(manager, "FinishWaveResolution");

        Assert.That(manager.State, Is.EqualTo(expectedState));
        Assert.That(
            GetField<BattleRunState>(manager, "runState").CurrentWaveIndex,
            Is.EqualTo(expectedWaveIndex));
        Assert.That(manager.GetAllyPurchaseCount("warrior"), Is.Zero);
        Assert.That(manager.GetAllyPurchaseCost("warrior"), Is.EqualTo(30));
        Assert.That(manager.GetAllyRemainingCooldown("warrior"), Is.Zero);
        Assert.That(manager.Gold, Is.EqualTo(goldBefore));
        Assert.That(manager.HasTacticalReinforcement, Is.True);
        Assert.That(GetUnitManager(manager).DeployedAllyCount, Is.Zero);
    }

    private BattleManager CreateBattleManager(
        EWaveState state,
        bool grantTicket = false)
    {
        unitObject = new GameObject("Unit Manager");
        var spawner = unitObject.AddComponent<UnitSpawner>();
        var unitManager = unitObject.AddComponent<UnitManager>();
        SetField(unitManager, "_roster", new UnitRoster());
        SetField(unitManager, "_spawner", spawner);

        battleObject = new GameObject("Battle Manager");
        var manager = battleObject.AddComponent<BattleManager>();
        var runState = new BattleRunState(
            new[] { new BattleWaveData() },
            true,
            3);
        runState.ChangeState(state);
        var economy = new BattleEconomy(100);
        var purchases = new UnitPurchaseController(
            economy,
            new UnitPurchaseSettings("warrior", 30, 1.4f, 4f));
        var reinforcement = new TacticalReinforcementController(5);
        if (grantTicket) reinforcement.GrantFromJackpot();

        SetField(manager, "<IsInitialized>k__BackingField", true);
        SetField(manager, "runState", runState);
        SetField(manager, "economy", economy);
        SetField(manager, "unitManager", unitManager);
        SetField(manager, "unitPurchaseController", purchases);
        SetField(manager, "tacticalReinforcementController", reinforcement);
        return manager;
    }

    private BattleManager CreateResolvingBattleManager(
        EWaveResolutionResult result,
        bool finalWave,
        int remainingChances)
    {
        BattleManager manager = CreateBattleManager(
            EWaveState.Pending,
            grantTicket: true);
        var waves = finalWave
            ? new[] { new BattleWaveData() }
            : new[] { new BattleWaveData(), new BattleWaveData() };
        var runState = new BattleRunState(waves, true, 3);
        while (runState.PlayerHp > remainingChances) runState.ConsumeChance();
        runState.ChangeState(EWaveState.Resolving);
        var resolution = new WaveResolutionState();
        resolution.TryBegin(result, 1, 0f, 0f);
        var purchases = GetField<UnitPurchaseController>(
            manager,
            "unitPurchaseController");
        purchases.TryPurchase("warrior", true, _ => true, out _);

        SetField(manager, "runState", runState);
        SetField(manager, "waveResolution", resolution);
        return manager;
    }

    private static UnitManager GetUnitManager(BattleManager manager) =>
        GetField<UnitManager>(manager, "unitManager");

    private static T GetField<T>(object target, string fieldName)
    {
        return (T)target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
            target,
            value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
            target,
            null);
    }
}
#endif
