#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;
using UnityEngine;

public sealed class UnitTargetTestUnit : UnitBase
{
    private EBattleTeam _team;

    public override EBattleTeam Team => _team;

    public void ConfigureTeam(EBattleTeam team)
    {
        _team = team;
    }

    public static void SetCurrentHp(UnitBase unit, float currentHp)
    {
        var healthField = typeof(UnitBase).GetField(
            "_health",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var health = healthField?.GetValue(unit);
        var currentHpField = typeof(UnitHealth).GetField(
            "<CurrentHp>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (health == null || currentHpField == null)
        {
            throw new MissingFieldException(
                typeof(UnitHealth).FullName,
                "<CurrentHp>k__BackingField");
        }

        currentHpField.SetValue(health, currentHp);
    }

    protected override void Tick()
    {
    }
}

public class UnitTargetFinderTests
{
    private readonly List<GameObject> _targetObjects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var targetObject in _targetObjects)
        {
            if (targetObject != null)
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        _targetObjects.Clear();
    }

    [Test]
    public void TargetQueries_SelectClosestFarthestHighestHpAndSortedLineTargets()
    {
        var roster = new UnitRoster();
        var finder = new UnitTargetFinder(roster);
        var near = CreateTestUnit(
            "near",
            EBattleTeam.Enemy,
            new Vector3(1f, 0f));
        var farInLine = CreateAlly("farInLine", new Vector3(2f, 0f));
        var far = CreateAlly("far", new Vector3(2f, 2f));
        var highestHp = farInLine;

        UnitTargetTestUnit.SetCurrentHp(farInLine, 20f);
        UnitTargetTestUnit.SetCurrentHp(far, 10f);
        roster.AddEnemy(near);
        roster.AddEnemy(farInLine);
        roster.AddOwnedAlly(farInLine);
        roster.AddOwnedAlly(far);

        var lineTargets = new List<UnitBase>();
        finder.GetEnemiesInLine(
            Vector3.zero,
            Vector3.right,
            10f,
            0.5f,
            lineTargets);

        Assert.That(
            finder.FindClosestAliveEnemy(Vector3.zero, 10f),
            Is.SameAs(near));
        Assert.That(
            finder.FindFarthestAliveAlly(Vector3.zero),
            Is.SameAs(far));
        Assert.That(
            finder.FindHighestHpAliveAlly(),
            Is.SameAs(highestHp));
        Assert.That(
            lineTargets,
            Is.EqualTo(new UnitBase[] { near, farInLine }));
    }

    private UnitTargetTestUnit CreateTestUnit(
        string name,
        EBattleTeam team,
        Vector3 position)
    {
        var targetObject = CreateTargetObject(name, position);
        var unit = targetObject.AddComponent<UnitTargetTestUnit>();
        unit.ConfigureTeam(team);
        return unit;
    }

    private AllyUnit CreateAlly(string name, Vector3 position)
    {
        return CreateTargetObject(name, position).AddComponent<AllyUnit>();
    }

    private GameObject CreateTargetObject(string name, Vector3 position)
    {
        var targetObject = new GameObject(name);
        targetObject.transform.position = position;
        _targetObjects.Add(targetObject);
        return targetObject;
    }
}
#endif
