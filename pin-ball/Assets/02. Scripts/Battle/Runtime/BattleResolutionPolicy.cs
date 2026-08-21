public static class BattleResolutionPolicy
{
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

    public static bool TryDetectWipe(
        int allyCount,
        int enemyCount,
        out EWaveResolutionResult result)
    {
        if (enemyCount <= 0)
        {
            result = EWaveResolutionResult.Cleared;
            return true;
        }

        result = default;
        return false;
    }
}
