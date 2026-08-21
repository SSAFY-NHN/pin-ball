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
}
