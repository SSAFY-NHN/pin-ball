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
    public void Initialize_WaitsForExplicitBattleStart()
    {
        var allyObject = new GameObject("ally");
        try
        {
            var ally = allyObject.AddComponent<AllyUnit>();
            ally.Initialize(new BattleUnitStats { MaxHp = 10f }, null);

            Assert.That(ally.IsBattleActive, Is.False);

            ally.StartBattle();

            Assert.That(ally.IsBattleActive, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(allyObject);
        }
    }

    [Test]
    public void ReachDefenseLine_IgnoresOwnLineAndAcceptsOpposingLine()
    {
        var allyObject = new GameObject("ally");
        try
        {
            var ally = allyObject.AddComponent<AllyUnit>();

            ally.ReachDefenseLine(EBattleTeam.Ally);
            Assert.That(ally.HasReachedDefenseLine, Is.False);

            ally.ReachDefenseLine(EBattleTeam.Enemy);
            Assert.That(ally.HasReachedDefenseLine, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(allyObject);
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
            enemy.ReachDefenseLine(EBattleTeam.Ally);
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
