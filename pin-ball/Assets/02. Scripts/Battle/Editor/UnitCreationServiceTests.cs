#if UNITY_EDITOR
using System.Collections.Generic;

using NUnit.Framework;

public class UnitCreationServiceTests
{
    private FakeUnitDataSource _dataSource;
    private UnitCreationService _service;

    [SetUp]
    public void SetUp()
    {
        _dataSource = new FakeUnitDataSource
        {
            AllyCommonValue = new AllyCommonData
            {
                maxLevel = 10,
                classLevel = 5
            },
            EnemyCommonValue = new EnemyCommonData
            {
                baseWave = 1,
                healthGrowthPerWave = 0.1f,
                attackGrowthPerWave = 0.2f,
                defenseGrowthInterval = 2,
                defenseGrowthValue = 3,
                moveSpeedGrowthPerWave = 0.05f,
                attackSpeedGrowthPerWave = 0.1f
            }
        };
        _dataSource.Allies.Add("warrior", CreateAlly("warrior", null));
        _dataSource.Allies.Add("knight", CreateAlly("knight", "warrior"));
        _dataSource.Enemies.Add("goblin", new EnemyUnitData
        {
            id = "goblin",
            health = 100,
            attack = 20,
            defense = 5,
            moveSpeed = 2f,
            attackSpeed = 1f,
            attackRange = 1.5f
        });
        _service = new UnitCreationService(_dataSource);
    }

    [Test]
    public void TryCreateAlly_CreatesBaseStatsAtRequestedLevel()
    {
        var spawnData = new BattleUnitSpawnData
        {
            UnitId = "warrior",
            Level = 3
        };

        bool created = _service.TryCreateAlly(
            spawnData,
            0f,
            out AllyUnitData allyData,
            out BattleUnitStats stats);

        Assert.That(created, Is.True);
        Assert.That(allyData.id, Is.EqualTo("warrior"));
        Assert.That(spawnData.Level, Is.EqualTo(3));
        Assert.That(stats.MaxHp, Is.EqualTo(138f).Within(0.001f));
        Assert.That(stats.AttackDamage, Is.EqualTo(27.6f).Within(0.001f));
        Assert.That(stats.Defense, Is.EqualTo(7f));
        Assert.That(stats.AttackRate, Is.EqualTo(1.2f));
    }

    [Test]
    public void TryCreateAlly_ClampsAdvancedClassToClassLevel()
    {
        var spawnData = new BattleUnitSpawnData
        {
            UnitId = "knight",
            Level = 1
        };

        bool created = _service.TryCreateAlly(
            spawnData,
            0f,
            out _,
            out BattleUnitStats stats);

        Assert.That(created, Is.True);
        Assert.That(spawnData.Level, Is.EqualTo(5));
        Assert.That(stats.MaxHp, Is.EqualTo(115f).Within(0.001f));
        Assert.That(stats.AttackDamage, Is.EqualTo(23f).Within(0.001f));
    }

    [Test]
    public void TryCreateAlly_AppliesMergeEquipmentThenTemporaryAttackBonus()
    {
        var spawnData = new BattleUnitSpawnData
        {
            UnitId = "warrior",
            Level = 1,
            Modifier = new BattleUnitModifier
            {
                MergeTier = 2,
                MergeAttackBonusPerTier = 0.1f,
                MergeHpBonusPerTier = 0.2f,
                EquipmentAttackBonus = 3f,
                EquipmentHpBonus = 5f
            }
        };

        bool created = _service.TryCreateAlly(
            spawnData,
            0.1f,
            out _,
            out BattleUnitStats stats);

        Assert.That(created, Is.True);
        Assert.That(stats.AttackDamage, Is.EqualTo(33.66f).Within(0.001f));
        Assert.That(stats.MaxHp, Is.EqualTo(166f).Within(0.001f));
    }

    [Test]
    public void TryCreateAlly_ReturnsFalseForMissingUnitId()
    {
        bool created = _service.TryCreateAlly(
            new BattleUnitSpawnData { UnitId = "missing", Level = 1 },
            0f,
            out AllyUnitData allyData,
            out BattleUnitStats stats);

        Assert.That(created, Is.False);
        Assert.That(allyData, Is.Null);
        Assert.That(stats.MaxHp, Is.Zero);
    }

    [Test]
    public void TryCreateEnemy_CreatesWaveScaledStats()
    {
        bool created = _service.TryCreateEnemy(
            "goblin",
            3,
            out EnemyUnitData enemyData,
            out BattleUnitStats stats);

        Assert.That(created, Is.True);
        Assert.That(enemyData.id, Is.EqualTo("goblin"));
        Assert.That(stats.MaxHp, Is.EqualTo(120f));
        Assert.That(stats.AttackDamage, Is.EqualTo(28f));
        Assert.That(stats.Defense, Is.EqualTo(8f));
        Assert.That(stats.MoveSpeed, Is.EqualTo(2.2f).Within(0.001f));
        Assert.That(stats.AttackRate, Is.EqualTo(1.2f).Within(0.001f));
    }

    private static AllyUnitData CreateAlly(string id, string previousJob)
    {
        return new AllyUnitData
        {
            id = id,
            previousJob = previousJob,
            health = 100,
            attack = 20,
            defense = 5,
            moveSpeed = 2f,
            attackSpeed = 1f,
            attackRange = 1.5f,
            healthGrowth = 10,
            attackGrowth = 2,
            defenseGrowth = 1,
            attackSpeedGrowth = 0.1f
        };
    }

    private sealed class FakeUnitDataSource : IUnitDataSource
    {
        public readonly Dictionary<string, AllyUnitData> Allies = new();
        public readonly Dictionary<string, EnemyUnitData> Enemies = new();

        public AllyCommonData AllyCommonValue;
        public EnemyCommonData EnemyCommonValue;

        public AllyCommonData AllyCommon => AllyCommonValue;
        public EnemyCommonData EnemyCommon => EnemyCommonValue;

        public bool TryGetAllyUnit(string id, out AllyUnitData result)
        {
            return Allies.TryGetValue(id, out result);
        }

        public bool TryGetEnemyUnit(string id, out EnemyUnitData result)
        {
            return Enemies.TryGetValue(id, out result);
        }

        public bool TryGetRootAllyJob(string unitId, out AllyUnitData rootJob)
        {
            rootJob = null;
            if (!Allies.TryGetValue(unitId, out AllyUnitData current)) return false;

            while (!string.IsNullOrEmpty(current.previousJob))
            {
                if (!Allies.TryGetValue(current.previousJob, out current)) return false;
            }

            rootJob = current;
            return true;
        }

        public void GetNextAllyJobs(
            string previousJobId,
            List<AllyUnitData> result)
        {
            result.Clear();
            foreach (AllyUnitData ally in Allies.Values)
            {
                if (ally.previousJob == previousJobId) result.Add(ally);
            }
        }
    }
}
#endif
