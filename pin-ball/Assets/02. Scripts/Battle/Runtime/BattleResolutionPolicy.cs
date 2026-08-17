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

        result = default;
        return false;
    }
}
