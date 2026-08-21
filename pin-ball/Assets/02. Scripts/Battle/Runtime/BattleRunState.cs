using System;
using System.Collections.Generic;

public sealed class BattleRunState
{
    private readonly IReadOnlyList<BattleWaveData> waves;
    private readonly bool hasValidRun;

    public int CurrentWaveIndex { get; private set; }
    public int CurrentWaveNumber => CurrentWaveIndex + 1;
    public int TotalWaveCount => waves?.Count ?? 0;
    public int MaximumPlayerHp { get; }
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
}
