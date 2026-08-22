public static class BattleResolutionPolicy
{
    public static bool TryResolveDefenseLines(
        int allyDefenseHp,
        int enemyDefenseHp,
        out EWaveResolutionResult result)
    {
        if (allyDefenseHp <= 0)
        {
            result = EWaveResolutionResult.Failed;
            return true;
        }

        result = EWaveResolutionResult.Cleared;
        return enemyDefenseHp <= 0;
    }

    public static EWaveState ResolveNextState(
        EWaveResolutionResult result,
        bool isFinalWave,
        int remainingChances)
    {
        if (result == EWaveResolutionResult.Failed)
        {
            return remainingChances <= 0
                ? EWaveState.Defeat
                : EWaveState.Pending;
        }

        return isFinalWave
            ? EWaveState.Victory
            : EWaveState.Pending;
    }
}
