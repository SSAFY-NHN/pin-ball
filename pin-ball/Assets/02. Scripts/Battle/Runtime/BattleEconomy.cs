using System;

public sealed class BattleEconomy
{
    public int Gold { get; private set; }

    public BattleEconomy(int startingGold)
    {
        Gold = Math.Max(0, startingGold);
    }

    public bool TrySpend(int amount)
    {
        int safeAmount = Math.Max(0, amount);
        if (safeAmount == 0) return true;
        if (Gold < safeAmount) return false;
        Gold -= safeAmount;
        return true;
    }

    public bool Add(int amount)
    {
        if (amount <= 0) return false;
        Gold += amount;
        return true;
    }
}
