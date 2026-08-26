#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BaseKnockbackUnitTests
{
    private sealed class TestEnemyUnit : EnemyUnit
    {
        public void InvokeTick() => Tick();
    }

    [Test]
    public void TryApplyBaseKnockback_MovesExactlyNormalizedDistance()
    {
        GameObject enemyObject = new("enemy");
        try
        {
            var enemy = enemyObject.AddComponent<TestEnemyUnit>();
            enemy.Initialize(CreateStats(), null);

            bool applied = enemy.TryApplyBaseKnockback(Vector3.right * 4f, 3f);

            Assert.That(applied, Is.True);
            Assert.That(enemy.transform.position, Is.EqualTo(Vector3.right * 3f));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
        }
    }

    [TestCase(0f, 0f)]
    [TestCase(1f, 0f)]
    [TestCase(1f, -1f)]
    public void TryApplyBaseKnockback_RejectsInvalidDirectionOrDistance(
        float directionX,
        float distance)
    {
        GameObject enemyObject = new("enemy");
        try
        {
            var enemy = enemyObject.AddComponent<TestEnemyUnit>();
            enemy.Initialize(CreateStats(), null);
            Vector3 before = enemy.transform.position;

            Assert.That(
                enemy.TryApplyBaseKnockback(Vector3.right * directionX, distance),
                Is.False);
            Assert.That(enemy.transform.position, Is.EqualTo(before));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void TryApplyBaseKnockback_RejectsDeadAndPooledUnits()
    {
        GameObject deadObject = new("dead");
        GameObject pooledObject = new("pooled");
        try
        {
            var dead = deadObject.AddComponent<TestEnemyUnit>();
            dead.Initialize(CreateStats(), null);
            dead.TakeDamage(100f);
            var pooled = pooledObject.AddComponent<TestEnemyUnit>();
            pooled.Initialize(CreateStats(), null);
            pooled.MarkReturnedToPool();

            Assert.That(dead.TryApplyBaseKnockback(Vector3.right, 3f), Is.False);
            Assert.That(pooled.TryApplyBaseKnockback(Vector3.right, 3f), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(deadObject);
            Object.DestroyImmediate(pooledObject);
        }
    }

    [Test]
    public void TryApplyBaseKnockback_ImmuneUnitKeepsPositionAndDefenseLineState()
    {
        GameObject enemyObject = new("enemy");
        try
        {
            var enemy = enemyObject.AddComponent<TestEnemyUnit>();
            enemy.Initialize(CreateStats(), null);
            enemy.ReachDefenseLine(EBattleTeam.Ally);
            enemy.ApplyKnockbackImmunity(10f);
            Vector3 before = enemy.transform.position;

            Assert.That(enemy.TryApplyBaseKnockback(Vector3.right, 3f), Is.False);
            Assert.That(enemy.transform.position, Is.EqualTo(before));
            Assert.That(enemy.HasReachedDefenseLine, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void TryApplyBaseKnockback_ClearsDefenseLineAndResumesTargeting()
    {
        GameObject enemyObject = new("enemy");
        GameObject allyObject = new("ally");
        GameObject battleAreaObject = new("battle area");
        try
        {
            var roster = new UnitRoster();
            var finder = new UnitTargetFinder(roster);
            var battleArea = battleAreaObject.AddComponent<BattleAreaBounds>();
            var context = new UnitCombatContext(finder, battleArea, _ => { }, null);
            var enemy = enemyObject.AddComponent<TestEnemyUnit>();
            var ally = allyObject.AddComponent<AllyUnit>();
            enemy.Initialize(CreateStats(), context);
            ally.Initialize(CreateStats(), context);
            roster.AddEnemy(enemy);
            roster.AddOwnedAlly(ally);
            enemy.StartBattle();
            enemy.ReachDefenseLine(EBattleTeam.Ally);
            enemy.ForceTarget(ally, 10f);

            Assert.That(enemy.TryApplyBaseKnockback(Vector3.right, 3f), Is.True);
            Assert.That(enemy.HasReachedDefenseLine, Is.False);
            Assert.That(enemy.CurrentTarget, Is.Null);
            Assert.That(enemy.IsBattleActive, Is.True);

            enemy.InvokeTick();

            Assert.That(enemy.CurrentTarget, Is.SameAs(ally));
            Assert.That(
                enemy.State,
                Is.EqualTo(EBattleUnitState.Moving)
                    .Or.EqualTo(EBattleUnitState.Attacking));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(allyObject);
            Object.DestroyImmediate(battleAreaObject);
        }
    }

    private static BattleUnitStats CreateStats()
    {
        return new BattleUnitStats
        {
            MaxHp = 10f,
            AttackDamage = 1f,
            AttackRate = 1f,
            AttackRange = 1f,
            MoveSpeed = 1f
        };
    }
}
#endif
