#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class DefenseLineBreachTests
{
    private sealed class TestEnemyUnit : EnemyUnit
    {
        public void InvokeTick() => Tick();
    }

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
    public void ReachDefenseLine_KeepsEnemyAliveAndInRoster()
    {
        var enemyObject = new GameObject("enemy");
        try
        {
            var roster = new UnitRoster();
            var enemy = enemyObject.AddComponent<EnemyUnit>();
            enemy.SetData(new EnemyUnitData
            {
                id = "goblin",
                breachDamage = 3
            });
            roster.AddEnemy(enemy);

            enemy.ReachDefenseLine();

            Assert.That(enemy.HasReachedDefenseLine, Is.True);
            Assert.That(enemy.IsAlive, Is.True);
            Assert.That(roster.ActiveEnemyCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void Tick_AfterReinforcementAppears_LeavesDefenseLineAndTargetsAlly()
    {
        var enemyObject = new GameObject("enemy");
        var allyObject = new GameObject("ally");
        try
        {
            var roster = new UnitRoster();
            var finder = new UnitTargetFinder(roster);
            var context = new UnitCombatContext(finder, null, null, null);
            var enemy = enemyObject.AddComponent<TestEnemyUnit>();
            var ally = allyObject.AddComponent<AllyUnit>();
            var stats = new BattleUnitStats
            {
                MaxHp = 10f,
                AttackDamage = 1f,
                AttackRate = 1f,
                AttackRange = 1f,
                MoveSpeed = 1f
            };
            enemy.Initialize(stats, context);
            ally.Initialize(stats, context);
            enemy.SetData(new EnemyUnitData { id = "goblin" });
            roster.AddEnemy(enemy);
            enemy.ReachDefenseLine();
            roster.AddOwnedAlly(ally);

            enemy.InvokeTick();

            Assert.That(enemy.HasReachedDefenseLine, Is.False);
            Assert.That(enemy.CurrentTarget, Is.SameAs(ally));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(allyObject);
        }
    }
}
#endif
