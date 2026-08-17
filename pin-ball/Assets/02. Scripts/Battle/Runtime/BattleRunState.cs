using System;

public sealed class BattleRunState
{
    public int MaximumPlayerHp { get; }
    public int PlayerHp { get; private set; }

    public BattleRunState(int maximumHp)
    {
        MaximumPlayerHp = Math.Max(1, maximumHp);
        PlayerHp = MaximumPlayerHp;
    }

    public bool ApplyPlayerDamage(int amount)
    {
        int nextHp = Math.Max(0, PlayerHp - Math.Max(0, amount));
        if (nextHp == PlayerHp) return false;
        PlayerHp = nextHp;
        return true;
    }

    public bool RestorePlayerHp(float ratio)
    {
        float safeRatio = Math.Max(0f, Math.Min(1f, ratio));
        int nextHp = Math.Max(
            1,
            Math.Min(
                MaximumPlayerHp,
                (int)Math.Ceiling(MaximumPlayerHp * safeRatio)));
        if (nextHp == PlayerHp) return false;

        PlayerHp = nextHp;
        return true;
    }
}
