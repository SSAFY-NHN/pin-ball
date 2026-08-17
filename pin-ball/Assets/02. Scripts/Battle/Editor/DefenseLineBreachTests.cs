#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class DefenseLineBreachTests
{
    [Test]
    public void TryDetectWipe_AlliesGoneWithEnemiesRemaining_DoesNotResolveStage()
    {
        bool resolved = BattleResolutionPolicy.TryDetectWipe(
            0,
            1,
            out EWaveResolutionResult result);

        Assert.That(resolved, Is.False);
        Assert.That(result, Is.EqualTo(default(EWaveResolutionResult)));
    }

    [Test]
    public void TryDetectWipe_NoEnemies_ClearsStageWithoutAllies()
    {
        bool resolved = BattleResolutionPolicy.TryDetectWipe(
            0,
            0,
            out EWaveResolutionResult result);

        Assert.That(resolved, Is.True);
        Assert.That(result, Is.EqualTo(EWaveResolutionResult.Cleared));
    }

    [Test]
    public void TryConsumeBreach_OnlySucceedsOncePerActivation()
    {
        var enemyObject = new GameObject("enemy");
        try
        {
            var enemy = enemyObject.AddComponent<EnemyUnit>();
            enemy.SetData(new EnemyUnitData
            {
                id = "goblin",
                breachDamage = 3
            });

            Assert.That(enemy.TryConsumeBreach(), Is.True);
            Assert.That(enemy.TryConsumeBreach(), Is.False);

            enemy.SetData(new EnemyUnitData
            {
                id = "goblin",
                breachDamage = 3
            });

            Assert.That(enemy.TryConsumeBreach(), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
        }
    }
}
#endif
