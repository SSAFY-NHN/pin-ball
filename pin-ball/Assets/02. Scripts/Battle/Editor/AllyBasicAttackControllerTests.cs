#if UNITY_EDITOR
using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

public sealed class AllyBasicAttackControllerTestUnit : UnitBase
{
    private EBattleTeam team;
    public override EBattleTeam Team => team;

    public void Configure(EBattleTeam value) => team = value;
    protected override void Tick() { }
}

public sealed class AllyBasicAttackControllerTests
{
    private readonly List<GameObject> objects = new();
    private UnitRoster roster;
    private UnitTargetFinder finder;
    private BattleAreaBounds bounds;
    private AllyBasicAttackController controller;

    [SetUp]
    public void SetUp()
    {
        roster = new UnitRoster();
        finder = new UnitTargetFinder(roster);
        var boundsObject = new GameObject("bounds");
        objects.Add(boundsObject);
        bounds = boundsObject.AddComponent<BattleAreaBounds>();
        controller = new AllyBasicAttackController();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var value in objects)
        {
            if (value != null) Object.DestroyImmediate(value);
        }
        objects.Clear();
    }

    [TestCase("spearman")]
    [TestCase("lancer")]
    [TestCase("guard")]
    public void SpearmanFamily_IgnoresFortyPercentArmorAgainstUnits(string unitId)
    {
        var target = CreateUnit("target", EBattleTeam.Enemy, Vector3.zero, 100f);

        Assert.That(controller.GetArmorIgnoreRatio(unitId, target), Is.EqualTo(0.4f));
        Assert.That(controller.GetArmorIgnoreRatio(unitId, null), Is.Zero);

        target.TakeDamage(100f, controller.GetArmorIgnoreRatio(unitId, target));
        Assert.That(target.CurrentHp, Is.EqualTo(238f));
    }

    [Test]
    public void SpearmanFamily_GainsNothingAgainstZeroDefense()
    {
        var normal = CreateUnit("normal", EBattleTeam.Enemy, Vector3.zero, 0f);
        var pierced = CreateUnit("pierced", EBattleTeam.Enemy, Vector3.zero, 0f);

        normal.TakeDamage(100f);
        pierced.TakeDamage(100f, controller.GetArmorIgnoreRatio("spearman", pierced));

        Assert.That(pierced.CurrentHp, Is.EqualTo(normal.CurrentHp));
    }

    [TestCase("mage")]
    [TestCase("pyromancer")]
    [TestCase("frost")]
    public void MageFamily_HitsOnlyTwoClosestSecondaryTargets(string unitId)
    {
        var source = CreateUnit("source", EBattleTeam.Ally, new Vector3(-2f, 0f), 0f);
        var primary = CreateEnemy("primary", Vector3.zero);
        var first = CreateEnemy("first", new Vector3(0.5f, 0f));
        var second = CreateEnemy("second", new Vector3(1f, 0f));
        var third = CreateEnemy("third", new Vector3(1.25f, 0f));
        var outside = CreateEnemy("outside", new Vector3(1.51f, 0f));
        var effects = new List<UnitBase>();

        int count = controller.ApplySecondaryHits(
            unitId, source, primary, 100f, finder, effects.Add);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(primary.CurrentHp, Is.EqualTo(300f));
        Assert.That(first.CurrentHp, Is.EqualTo(240f));
        Assert.That(second.CurrentHp, Is.EqualTo(240f));
        Assert.That(third.CurrentHp, Is.EqualTo(300f));
        Assert.That(outside.CurrentHp, Is.EqualTo(300f));
        Assert.That(effects, Is.EqualTo(new UnitBase[] { first, second }));
    }

    [TestCase("warrior")]
    [TestCase("archer")]
    [TestCase("spearman")]
    public void NonMageFamilies_DoNotDealSecondaryDamage(string unitId)
    {
        var source = CreateUnit("source", EBattleTeam.Ally, Vector3.left, 0f);
        var primary = CreateEnemy("primary", Vector3.zero);
        var secondary = CreateEnemy("secondary", Vector3.right);

        int count = controller.ApplySecondaryHits(
            unitId, source, primary, 100f, finder, null);

        Assert.That(count, Is.Zero);
        Assert.That(secondary.CurrentHp, Is.EqualTo(300f));
    }

    private AllyBasicAttackControllerTestUnit CreateEnemy(string name, Vector3 position)
    {
        var unit = CreateUnit(name, EBattleTeam.Enemy, position, 0f);
        roster.AddEnemy(unit);
        return unit;
    }

    private AllyBasicAttackControllerTestUnit CreateUnit(
        string name,
        EBattleTeam team,
        Vector3 position,
        float defense)
    {
        var value = new GameObject(name);
        objects.Add(value);
        value.transform.position = position;
        var unit = value.AddComponent<AllyBasicAttackControllerTestUnit>();
        unit.Configure(team);
        unit.Initialize(
            new BattleUnitStats
            {
                MaxHp = 300f,
                AttackDamage = 100f,
                Defense = defense,
                MoveSpeed = 1f,
                AttackRate = 1f,
                AttackRange = 1f
            },
            new UnitCombatContext(finder, bounds, _ => { }));
        return unit;
    }
}
#endif
