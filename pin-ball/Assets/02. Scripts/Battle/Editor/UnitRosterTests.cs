#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class UnitRosterTests
{
    private GameObject _allyObject;
    private GameObject _enemyObject;

    [TearDown]
    public void TearDown()
    {
        if (_allyObject != null) Object.DestroyImmediate(_allyObject);
        if (_enemyObject != null) Object.DestroyImmediate(_enemyObject);
    }

    [Test]
    public void AddOwnedAlly_RegistersOwnedAndActiveOnce()
    {
        _allyObject = new GameObject("ally");
        var ally = _allyObject.AddComponent<AllyUnit>();
        var roster = new UnitRoster();

        Assert.That(roster.AddOwnedAlly(ally), Is.True);
        Assert.That(roster.AddOwnedAlly(ally), Is.False);
        Assert.That(roster.OwnedAllyCount, Is.EqualTo(1));
        Assert.That(roster.ActiveAllyCount, Is.EqualTo(1));
    }

    [Test]
    public void RemoveUnit_RemovesOwnedAllyFromBothLists()
    {
        _allyObject = new GameObject("ally");
        var ally = _allyObject.AddComponent<AllyUnit>();
        var roster = new UnitRoster();
        roster.AddOwnedAlly(ally);

        Assert.That(roster.RemoveUnit(ally), Is.True);
        Assert.That(roster.OwnedAllyCount, Is.Zero);
        Assert.That(roster.ActiveAllyCount, Is.Zero);
    }

    [Test]
    public void NotifyUnitDied_AllyIsPermanentlyRemoved()
    {
        _allyObject = new GameObject("ally");
        var ally = _allyObject.AddComponent<AllyUnit>();
        var roster = new UnitRoster();
        roster.AddOwnedAlly(ally);

        Assert.That(roster.NotifyUnitDied(ally), Is.True);
        Assert.That(roster.OwnedAllyCount, Is.Zero);
        Assert.That(roster.ActiveAllyCount, Is.Zero);
    }

    [Test]
    public void AddOwnedAlly_AfterWipe_RegistersReinforcementAsActive()
    {
        _allyObject = new GameObject("defeated ally");
        _enemyObject = new GameObject("reinforcement");
        var defeatedAlly = _allyObject.AddComponent<AllyUnit>();
        var reinforcement = _enemyObject.AddComponent<AllyUnit>();
        var roster = new UnitRoster();
        roster.AddOwnedAlly(defeatedAlly);
        roster.NotifyUnitDied(defeatedAlly);

        Assert.That(roster.AddOwnedAlly(reinforcement), Is.True);
        Assert.That(roster.OwnedAllies, Is.EqualTo(new[] { reinforcement }));
        Assert.That(roster.ActiveAllies, Is.EqualTo(new UnitBase[] { reinforcement }));
    }

    [Test]
    public void NotifyUnitDied_EnemyDoesNotTouchOwnedAllies()
    {
        _allyObject = new GameObject("ally");
        _enemyObject = new GameObject("enemy");
        var ally = _allyObject.AddComponent<AllyUnit>();
        var enemy = _enemyObject.AddComponent<EnemyUnit>();
        var roster = new UnitRoster();
        roster.AddOwnedAlly(ally);
        roster.AddEnemy(enemy);

        Assert.That(roster.NotifyUnitDied(enemy), Is.True);
        Assert.That(roster.OwnedAllyCount, Is.EqualTo(1));
        Assert.That(roster.ActiveEnemyCount, Is.Zero);
    }
}
#endif
