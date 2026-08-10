#if UNITY_EDITOR
using NUnit.Framework;

public class BattleDataCharacterizationTests
{
    [Test]
    public void AllyCreateStats_AppliesGrowthFromBaseLevel()
    {
        var data = new AllyUnitData
        {
            previousJob = string.Empty,
            health = 180,
            attack = 18,
            defense = 10,
            moveSpeed = 2.5f,
            attackSpeed = 0.85f,
            attackRange = 1.1f,
            mana = 0,
            healthGrowth = 24,
            attackGrowth = 3,
            defenseGrowth = 2,
            attackSpeedGrowth = 0.03f
        };

        BattleUnitStats stats = data.CreateStats(3, 5);

        Assert.That(stats.MaxHp, Is.EqualTo(342f).Within(0.001f));
        Assert.That(stats.AttackDamage, Is.EqualTo(36f).Within(0.001f));
        Assert.That(stats.Defense, Is.EqualTo(14f));
        Assert.That(stats.MoveSpeed, Is.EqualTo(2.5f));
        Assert.That(stats.AttackRate, Is.EqualTo(0.91f).Within(0.0001f));
        Assert.That(stats.AttackRange, Is.EqualTo(1.1f));
        Assert.That(stats.MaxMana, Is.Zero);
    }

    [Test]
    public void EnemyCreateStats_AppliesWaveGrowthAndFlooring()
    {
        var common = new EnemyCommonData
        {
            baseWave = 1,
            healthGrowthPerWave = 0.1f,
            attackGrowthPerWave = 0.2f,
            defenseGrowthInterval = 2,
            defenseGrowthValue = 3,
            moveSpeedGrowthPerWave = 0.05f,
            attackSpeedGrowthPerWave = 0.1f
        };
        var data = new EnemyUnitData
        {
            health = 101,
            attack = 11,
            defense = 4,
            moveSpeed = 2f,
            attackSpeed = 1f,
            attackRange = 1.5f
        };

        BattleUnitStats stats = data.CreateStats(3, common);

        Assert.That(stats.MaxHp, Is.EqualTo(121f));
        Assert.That(stats.AttackDamage, Is.EqualTo(15f));
        Assert.That(stats.Defense, Is.EqualTo(7f));
        Assert.That(stats.MoveSpeed, Is.EqualTo(2.2f).Within(0.0001f));
        Assert.That(stats.AttackRate, Is.EqualTo(1.2f).Within(0.0001f));
        Assert.That(stats.AttackRange, Is.EqualTo(1.5f));
    }
}
#endif
