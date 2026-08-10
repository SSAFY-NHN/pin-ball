#if UNITY_EDITOR
using NUnit.Framework;

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
}
#endif
