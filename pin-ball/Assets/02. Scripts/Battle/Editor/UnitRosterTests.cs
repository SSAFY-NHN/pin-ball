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
}
#endif
