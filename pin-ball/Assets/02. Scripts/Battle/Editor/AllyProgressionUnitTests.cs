#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class AllyProgressionUnitTests
{
    private GameObject unitObject;

    [TearDown]
    public void TearDown()
    {
        if (unitObject != null) Object.DestroyImmediate(unitObject);
    }

    [Test]
    public void ReapplyLevel_PreservesHealthRatioAndUpdatesLevelAndStats()
    {
        unitObject = new GameObject("Ally");
        var ally = unitObject.AddComponent<AllyUnit>();
        ally.Initialize(CreateStats(100f, 10f), null);
        ally.SetData("warrior", 1, null, new AllyCommonData());
        ally.TakeDamage(55f);
        float healthRatio = ally.CurrentHp / ally.MaxHp;

        ally.ReapplyLevel(2, CreateStats(200f, 20f));

        Assert.That(ally.Level, Is.EqualTo(2));
        Assert.That(ally.MaxHp, Is.EqualTo(200f));
        Assert.That(
            ally.CurrentHp,
            Is.EqualTo(ally.MaxHp * healthRatio).Within(0.001f));
        Assert.That(ally.AttackDamage, Is.EqualTo(20f));
    }

    private static BattleUnitStats CreateStats(float hp, float attack)
    {
        return new BattleUnitStats
        {
            MaxHp = hp,
            AttackDamage = attack,
            Defense = 5f,
            AttackRate = 1f,
            AttackRange = 1f,
            MoveSpeed = 1f,
            MaxMana = 0f
        };
    }
}
#endif
