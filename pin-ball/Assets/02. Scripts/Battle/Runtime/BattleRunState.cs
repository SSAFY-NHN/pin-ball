using System;
using System.Collections.Generic;

public sealed class BattleRunState
{
    private readonly IReadOnlyList<BattleWaveData> _waves;
    private readonly bool _hasValidRun;

    public int CurrentWaveIndex { get; private set; }
    public int CurrentWaveNumber => CurrentWaveIndex + 1;
    public int TotalWaveCount => _waves?.Count ?? 0;
    public int PlayerHp { get; private set; }
    public EWaveState State { get; private set; } = EWaveState.Pending;
    public bool HasValidCurrentWave =>
        _hasValidRun &&
        _waves != null &&
        CurrentWaveIndex >= 0 &&
        CurrentWaveIndex < _waves.Count &&
        _waves[CurrentWaveIndex] != null;
    public BattleWaveData CurrentWave =>
        HasValidCurrentWave ? _waves[CurrentWaveIndex] : null;

    public BattleRunState(
        IReadOnlyList<BattleWaveData> waves,
        bool hasValidRun,
        int maximumHp)
    {
        _waves = waves ?? Array.Empty<BattleWaveData>();
        _hasValidRun = hasValidRun;
        PlayerHp = Math.Max(1, maximumHp);
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
}
