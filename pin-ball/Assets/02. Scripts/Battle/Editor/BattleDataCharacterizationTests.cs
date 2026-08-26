#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class BattleDataCharacterizationTests
{
    [Test]
    public void RuntimeUnitData_UsesHalfSpeedBalanceValues()
    {
        var expectedAllySpeeds = new Dictionary<string, float>
        {
            ["warrior"] = 1.25f, ["archer"] = 1.4f,
            ["mage"] = 1.25f, ["spearman"] = 1.35f,
            ["knight"] = 1.2f, ["berserker"] = 1.3f,
            ["ranger"] = 1.5f, ["marksman"] = 1.3f,
            ["pyromancer"] = 1.2f, ["frost"] = 1.25f,
            ["lancer"] = 1.55f, ["guard"] = 1.15f
        };
        var expectedEnemySpeeds = new Dictionary<string, float>
        {
            ["goblin"] = 0.6f, ["wolf"] = 1.9f,
            ["goblin_archer"] = 1.1f, ["shield_guard"] = 0.9f,
            ["orc_warrior"] = 1.05f, ["shaman"] = 1f,
            ["assassin"] = 1.6f, ["troll"] = 0.75f,
            ["ogre_elite"] = 0.7f, ["dark_mage_elite"] = 0.9f,
            ["goblin_king"] = 0.75f
        };

        AllyUnitDataCollection allies = JsonUtility.FromJson<AllyUnitDataCollection>(
            Resources.Load<TextAsset>("Data/AllyUnitData").text);
        EnemyUnitDataCollection enemies = JsonUtility.FromJson<EnemyUnitDataCollection>(
            Resources.Load<TextAsset>("Data/EnemyUnitData").text);

        CollectionAssert.AreEquivalent(
            expectedAllySpeeds.Keys, allies.units.Select(unit => unit.id));
        CollectionAssert.AreEquivalent(
            expectedEnemySpeeds.Keys, enemies.units.Select(unit => unit.id));
        foreach (AllyUnitData unit in allies.units)
        {
            Assert.That(unit.moveSpeed,
                Is.EqualTo(expectedAllySpeeds[unit.id]).Within(0.0001f), unit.id);
        }
        foreach (EnemyUnitData unit in enemies.units)
        {
            Assert.That(unit.moveSpeed,
                Is.EqualTo(expectedEnemySpeeds[unit.id]).Within(0.0001f), unit.id);
        }
    }

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
