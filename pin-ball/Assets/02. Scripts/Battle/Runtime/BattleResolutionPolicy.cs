public static class BattleResolutionPolicy
{
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

        if (allyCount <= 0)
        {
            result = EWaveResolutionResult.Failed;
            return true;
        }

        result = default;
        return false;
    }

    public static EWaveState ResolveNextState(
        EWaveResolutionResult result,
        bool isFinalWave,
        int playerHp)
    {
        if (result == EWaveResolutionResult.Failed)
        {
            return playerHp <= 0
                ? EWaveState.Defeat
                : EWaveState.Pending;
        }

        return isFinalWave
            ? EWaveState.Victory
            : EWaveState.Pending;
    }
}
