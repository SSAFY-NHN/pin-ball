#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using UnityEngine;

public sealed class SummonMinionsSkillTests
{
    private GameObject casterObject;

    [TearDown]
    public void TearDown()
    {
        if (casterObject != null) Object.DestroyImmediate(casterObject);
    }

    [Test]
    public void OnDamaged_SummonsPatrolManAndRogueAtEachHealthThreshold()
    {
        casterObject = new GameObject("DarkMageBoss");
        var caster = casterObject.AddComponent<EnemyUnit>();
        caster.Initialize(
            new BattleUnitStats
            {
                MaxHp = 100f,
                AttackDamage = 1f,
                AttackRate = 1f,
                AttackRange = 1f,
                MoveSpeed = 1f
            },
            null);
        var actions = new RecordingEnemyBattleActions();
        var context = new UnitSkillContext(
            caster,
            null,
            new UnitTargetFinder(new UnitRoster()),
            actions);
        var data = LoadSummonData();
        var skill = new SummonMinionsSkill();

        for (var phase = 0; phase < 3; phase++)
        {
            caster.TakeDamage(25f);
            skill.OnDamaged(context, data);
        }

        Assert.That(
            actions.Spawns,
            Is.EqualTo(new[]
            {
                new SpawnRequest("PatrolMan", 2),
                new SpawnRequest("Rogue", 1),
                new SpawnRequest("PatrolMan", 2),
                new SpawnRequest("Rogue", 1),
                new SpawnRequest("PatrolMan", 2),
                new SpawnRequest("Rogue", 1)
            }));
    }

    [Test]
    public void OnDamaged_WhenOneHitCrossesAllThresholds_SummonsEveryPhase()
    {
        casterObject = new GameObject("DarkMageBoss");
        var caster = casterObject.AddComponent<EnemyUnit>();
        caster.Initialize(
            new BattleUnitStats
            {
                MaxHp = 100f,
                AttackDamage = 1f,
                AttackRate = 1f,
                AttackRange = 1f,
                MoveSpeed = 1f
            },
            null);

        var actions = new RecordingEnemyBattleActions();
        var context = new UnitSkillContext(
            caster,
            null,
            new UnitTargetFinder(new UnitRoster()),
            actions);

        caster.TakeDamage(75f);
        new SummonMinionsSkill().OnDamaged(context, LoadSummonData());

        Assert.That(actions.Spawns, Has.Count.EqualTo(6));
        Assert.That(
            actions.Spawns.Select(spawn => spawn.EnemyId),
            Is.EqualTo(new[]
            {
                "PatrolMan", "Rogue",
                "PatrolMan", "Rogue",
                "PatrolMan", "Rogue"
            }));
    }

    private static EnemySkillData LoadSummonData()
    {
        var textAsset = Resources.Load<TextAsset>("Data/EnemyUnitData");
        Assert.That(textAsset, Is.Not.Null);

        var collection =
            JsonUtility.FromJson<EnemyUnitDataCollection>(textAsset.text);
        var boss = collection.units.Single(unit => unit.id == "DarkMageBoss");
        return boss.Skills.Single(skill => skill.id == "summon_minions");
    }

    private readonly struct SpawnRequest
    {
        public string EnemyId { get; }
        public int Count { get; }

        public SpawnRequest(string enemyId, int count)
        {
            EnemyId = enemyId;
            Count = count;
        }

        public override bool Equals(object obj)
        {
            return obj is SpawnRequest other &&
                   EnemyId == other.EnemyId &&
                   Count == other.Count;
        }

        public override int GetHashCode()
        {
            return (EnemyId, Count).GetHashCode();
        }
    }

    private sealed class RecordingEnemyBattleActions : IEnemyBattleActions
    {
        public List<SpawnRequest> Spawns { get; } = new();

        public void SpawnEnemyReinforcement(
            string enemyId,
            int count,
            Vector3 center)
        {
            Spawns.Add(new SpawnRequest(enemyId, count));
        }

        public void ApplyEnemySpeedBuff(
            float moveSpeedMultiplier,
            float attackRateMultiplier)
        {
        }
    }
}
#endif
