#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BaseKnockbackUnitManagerTests
{
    [Test]
    public void TryApplyBaseKnockback_MovesEveryEligibleEnemyInDefenseLineDirection()
    {
        GameObject managerObject = new("manager");
        GameObject allyLineObject = new("ally line");
        GameObject enemyLineObject = new("enemy line");
        GameObject firstObject = new("first");
        GameObject secondObject = new("second");
        try
        {
            UnitManager manager = managerObject.AddComponent<UnitManager>();
            UnitRoster roster = PrepareRoster(manager);
            SetDefenseLines(manager, allyLineObject, enemyLineObject);
            allyLineObject.transform.position = new Vector3(5f, 0f);
            enemyLineObject.transform.position = new Vector3(-5f, 0f);
            var first = CreateEnemy(firstObject);
            var second = CreateEnemy(secondObject);
            second.transform.position = new Vector3(-8f, 2f);
            roster.AddEnemy(first);
            roster.AddEnemy(second);

            int applied = manager.TryApplyBaseKnockback(3f);

            Assert.That(applied, Is.EqualTo(2));
            Assert.That(first.transform.position, Is.EqualTo(Vector3.left * 3f));
            Assert.That(second.transform.position, Is.EqualTo(new Vector3(-11f, 0f)));
            Assert.That(manager.HasAliveActiveEnemy, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(allyLineObject);
            Object.DestroyImmediate(enemyLineObject);
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
        }
    }

    [Test]
    public void TryApplyBaseKnockback_ExcludesNullDeadPooledAndImmuneEnemies()
    {
        GameObject managerObject = new("manager");
        GameObject allyLineObject = new("ally line");
        GameObject enemyLineObject = new("enemy line");
        GameObject validObject = new("valid");
        GameObject immuneObject = new("immune");
        GameObject deadObject = new("dead");
        GameObject pooledObject = new("pooled");
        try
        {
            UnitManager manager = managerObject.AddComponent<UnitManager>();
            UnitRoster roster = PrepareRoster(manager);
            SetDefenseLines(manager, allyLineObject, enemyLineObject);
            allyLineObject.transform.position = Vector3.zero;
            enemyLineObject.transform.position = Vector3.right * 5f;
            EnemyUnit valid = CreateEnemy(validObject);
            EnemyUnit immune = CreateEnemy(immuneObject);
            EnemyUnit dead = CreateEnemy(deadObject);
            EnemyUnit pooled = CreateEnemy(pooledObject);
            immune.ReachDefenseLine(EBattleTeam.Ally);
            immune.ApplyKnockbackImmunity(10f);
            dead.TakeDamage(100f);
            pooled.MarkReturnedToPool();
            roster.AddEnemy(valid);
            roster.AddEnemy(immune);
            roster.AddEnemy(dead);
            roster.AddEnemy(pooled);
            GetActiveEnemies(roster).Add(null);
            Vector3 immuneBefore = immune.transform.position;

            int applied = manager.TryApplyBaseKnockback(3f);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(valid.transform.position, Is.EqualTo(Vector3.right * 3f));
            Assert.That(immune.transform.position, Is.EqualTo(immuneBefore));
            Assert.That(immune.HasReachedDefenseLine, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(allyLineObject);
            Object.DestroyImmediate(enemyLineObject);
            Object.DestroyImmediate(validObject);
            Object.DestroyImmediate(immuneObject);
            Object.DestroyImmediate(deadObject);
            Object.DestroyImmediate(pooledObject);
        }
    }

    [Test]
    public void TryApplyBaseKnockback_ReturnsZeroForMissingOrCoincidentDefenseLines()
    {
        GameObject managerObject = new("manager");
        GameObject allyLineObject = new("ally line");
        GameObject enemyLineObject = new("enemy line");
        GameObject enemyObject = new("enemy");
        try
        {
            UnitManager manager = managerObject.AddComponent<UnitManager>();
            UnitRoster roster = PrepareRoster(manager);
            roster.AddEnemy(CreateEnemy(enemyObject));

            Assert.That(manager.TryApplyBaseKnockback(3f), Is.Zero);

            SetDefenseLines(manager, allyLineObject, enemyLineObject);
            allyLineObject.transform.position = Vector3.one;
            enemyLineObject.transform.position = Vector3.one;
            Assert.That(manager.TryApplyBaseKnockback(3f), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(allyLineObject);
            Object.DestroyImmediate(enemyLineObject);
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void TryApplyBaseKnockback_AllImmuneKeepsUseCandidateButAppliesNone()
    {
        GameObject managerObject = new("manager");
        GameObject allyLineObject = new("ally line");
        GameObject enemyLineObject = new("enemy line");
        GameObject enemyObject = new("enemy");
        try
        {
            UnitManager manager = managerObject.AddComponent<UnitManager>();
            UnitRoster roster = PrepareRoster(manager);
            SetDefenseLines(manager, allyLineObject, enemyLineObject);
            enemyLineObject.transform.position = Vector3.right;
            EnemyUnit enemy = CreateEnemy(enemyObject);
            enemy.ApplyKnockbackImmunity(10f);
            roster.AddEnemy(enemy);

            Assert.That(manager.HasAliveActiveEnemy, Is.True);
            Assert.That(manager.TryApplyBaseKnockback(3f), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(allyLineObject);
            Object.DestroyImmediate(enemyLineObject);
            Object.DestroyImmediate(enemyObject);
        }
    }

    private static EnemyUnit CreateEnemy(GameObject target)
    {
        var enemy = target.AddComponent<EnemyUnit>();
        enemy.Initialize(new BattleUnitStats { MaxHp = 10f }, null);
        return enemy;
    }

    private static UnitRoster PrepareRoster(UnitManager manager)
    {
        var roster = new UnitRoster();
        GetField(typeof(UnitManager), "_roster").SetValue(manager, roster);
        return roster;
    }

    private static List<UnitBase> GetActiveEnemies(UnitRoster roster)
    {
        return (List<UnitBase>)GetField(typeof(UnitRoster), "_activeEnemies")
            .GetValue(roster);
    }

    private static void SetDefenseLines(
        UnitManager manager,
        GameObject allyLineObject,
        GameObject enemyLineObject)
    {
        GetField(typeof(UnitManager), "allyDefenseLine").SetValue(
            manager,
            allyLineObject.AddComponent<DefenseLineTrigger>());
        GetField(typeof(UnitManager), "enemyDefenseLine").SetValue(
            manager,
            enemyLineObject.AddComponent<DefenseLineTrigger>());
    }

    private static FieldInfo GetField(System.Type type, string name)
    {
        return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
#endif
