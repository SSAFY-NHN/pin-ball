using System;
using System.Collections.Generic;

public sealed class BattleRunState
{
    private readonly IReadOnlyList<BattleWaveData> waves;
    private readonly bool hasValidRun;

    public int CurrentWaveIndex { get; private set; }
    public int CurrentWaveNumber => CurrentWaveIndex + 1;
    public int TotalWaveCount => waves?.Count ?? 0;
    public int MaximumPlayerHp { get; private set; }
    public int PlayerHp { get; private set; }
    public EWaveState State { get; private set; } = EWaveState.Pending;
    public bool HasValidCurrentWave =>
        hasValidRun && CurrentWaveIndex >= 0 &&
        CurrentWaveIndex < TotalWaveCount && waves[CurrentWaveIndex] != null;
    public BattleWaveData CurrentWave =>
        HasValidCurrentWave ? waves[CurrentWaveIndex] : null;

    public BattleRunState(
        IReadOnlyList<BattleWaveData> waves,
        bool hasValidRun,
        int maximumChances)
    {
        this.waves = waves ?? Array.Empty<BattleWaveData>();
        this.hasValidRun = hasValidRun;
        MaximumPlayerHp = Math.Max(1, maximumChances);
        PlayerHp = MaximumPlayerHp;
    }

    public BattleRunState(int maximumHp)
        : this(Array.Empty<BattleWaveData>(), false, maximumHp)
    {
    }

    public bool ConsumeChance()
    {
        if (PlayerHp <= 0) return false;
        PlayerHp--;
        return true;
    }

    public bool ChangeState(EWaveState nextState)
    {
        if (State == nextState) return false;
        State = nextState;
        return true;
    }

    public bool AdvanceWave()
    {
        if (CurrentWaveIndex + 1 >= TotalWaveCount) return false;
        CurrentWaveIndex++;
        return true;
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

    public bool IncreaseMaximumPlayerHp(int amount)
    {
        int safeAmount = Math.Max(0, amount);
        if (safeAmount == 0) return false;

        MaximumPlayerHp += safeAmount;
        PlayerHp = Math.Min(MaximumPlayerHp, PlayerHp + safeAmount);
        return true;
    }
}
