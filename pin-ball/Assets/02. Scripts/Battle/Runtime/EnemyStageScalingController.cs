using UnityEngine;

public sealed class EnemyStageScalingController
{
    private readonly int baseEnemyCount;
    private readonly int enemyCountGrowthInterval;
    private readonly int enemyCountGrowthAmount;
    private readonly int maximumEnemyCount;

    public EnemyStageScalingController(
        int baseEnemyCount,
        int enemyCountGrowthInterval,
        int enemyCountGrowthAmount,
        int maximumEnemyCount)
    {
        this.baseEnemyCount = Mathf.Max(1, baseEnemyCount);
        this.enemyCountGrowthInterval = Mathf.Max(1, enemyCountGrowthInterval);
        this.enemyCountGrowthAmount = Mathf.Max(0, enemyCountGrowthAmount);
        this.maximumEnemyCount = Mathf.Max(this.baseEnemyCount, maximumEnemyCount);
    }

    public int CalculateEnemyCount(int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        int intervalBonus =
            (safeStage - 1) / enemyCountGrowthInterval *
            enemyCountGrowthAmount;
        return Mathf.Min(maximumEnemyCount, baseEnemyCount + intervalBonus);
    }
}
