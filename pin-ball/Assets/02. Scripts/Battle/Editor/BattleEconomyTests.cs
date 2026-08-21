#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class BattleEconomyTests
{
    [Test]
    public void TrySpend_ClampsNegativeAmountToSuccessfulZeroSpend()
    {
        var economy = new BattleEconomy(100);
        Assert.That(economy.TrySpend(-10), Is.True);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }

    [Test]
    public void TrySpend_RejectsInsufficientBalanceWithoutMutation()
    {
        var economy = new BattleEconomy(100);
        Assert.That(economy.TrySpend(101), Is.False);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }

    [Test]
    public void Add_IgnoresNonPositiveReward()
    {
        var economy = new BattleEconomy(100);
        economy.Add(0);
        economy.Add(-20);
        Assert.That(economy.Gold, Is.EqualTo(100));
    }

    [Test]
    public void WaveResolutionPolicy_DoesNotMutateEconomy()
    {
        var economy = new BattleEconomy(25);

        BattleResolutionPolicy.ResolveNextState(
            EWaveResolutionResult.Cleared,
            false,
            3);
        BattleResolutionPolicy.ResolveNextState(
            EWaveResolutionResult.Failed,
            false,
            2);

        Assert.That(economy.Gold, Is.EqualTo(25));
    }
}

public class BattleManagerPreStartEconomyTests
{
    private GameObject _gameObject;
    private BattleManager _manager;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("BattleManager Pre-Start Test");
        _manager = _gameObject.AddComponent<BattleManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void TrySpendGold_BeforeStart_NonPositiveAmountsSucceedWithoutEvent()
    {
        int eventCount = 0;
        _manager.OnGoldChanged += _ => eventCount++;

        Assert.That(_manager.TrySpendGold(0), Is.True);
        Assert.That(_manager.TrySpendGold(-10), Is.True);
        Assert.That(_manager.Gold, Is.Zero);
        Assert.That(eventCount, Is.Zero);
    }

    [Test]
    public void TrySpendGold_BeforeStart_PositiveAmountFailsAgainstZeroWithoutEvent()
    {
        int eventCount = 0;
        _manager.OnGoldChanged += _ => eventCount++;

        Assert.That(_manager.TrySpendGold(1), Is.False);
        Assert.That(_manager.Gold, Is.Zero);
        Assert.That(eventCount, Is.Zero);
    }

    [Test]
    public void AddGold_BeforeStart_PositiveAmountMutatesGoldAndEmitsEvent()
    {
        int eventCount = 0;
        int observedGold = -1;
        _manager.OnGoldChanged += gold =>
        {
            eventCount++;
            observedGold = gold;
        };

        _manager.AddGold(25);

        Assert.That(_manager.Gold, Is.EqualTo(25));
        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(observedGold, Is.EqualTo(25));
    }
}
#endif
