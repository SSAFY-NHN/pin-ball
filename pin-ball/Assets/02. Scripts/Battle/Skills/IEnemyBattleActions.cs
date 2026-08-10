using UnityEngine;

public interface IEnemyBattleActions
{
    void SpawnEnemyReinforcement(string enemyId, int count, Vector3 center);
    void ApplyEnemySpeedBuff(float moveSpeedMultiplier, float attackRateMultiplier);
}
