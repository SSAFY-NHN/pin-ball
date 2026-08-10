#if UNITY_EDITOR
using System.Reflection;
using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

public class AllyPreparationPlacementTests
{
    private static MethodInfo GetMethod(string name)
    {
        return typeof(BattleAreaBounds).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
    }

    [TestCase(5.49f, false)]
    [TestCase(5.5f, true)]
    [TestCase(9.5f, true)]
    [TestCase(9.51f, false)]
    public void ContainsAllyPlacement_UsesPaddedRightHalf(
        float x,
        bool expected)
    {
        MethodInfo method = GetMethod("ContainsAllyPlacement");
        Assert.That(method, Is.Not.Null);

        var result = (bool)method.Invoke(
            null,
            new object[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 8f),
                new Vector3(x, 4f, 0f),
                0.5f
            });

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ClampAllyPlacement_ReachesPaddedRightEdge()
    {
        MethodInfo method = GetMethod("ClampAllyPlacement");
        Assert.That(method, Is.Not.Null);

        var result = (Vector3)method.Invoke(
            null,
            new object[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 8f),
                new Vector3(20f, 4f, 3f),
                0.5f
            });

        Assert.That(result, Is.EqualTo(new Vector3(9.5f, 4f, 3f)));
    }

    [Test]
    public void TryGetAllyGridPosition_AdvancesHorizontallyBeforeNextRow()
    {
        MethodInfo method = GetMethod("TryGetAllyGridPosition");
        Assert.That(method, Is.Not.Null);

        Vector3 first = InvokeGridPosition(method, 0);
        Vector3 second = InvokeGridPosition(method, 1);
        Vector3 nextRow = InvokeGridPosition(method, 4);

        Assert.That(first, Is.EqualTo(new Vector3(5.5f, 7.5f, 0f)));
        Assert.That(second, Is.EqualTo(new Vector3(6.65f, 7.5f, 0f)));
        Assert.That(nextRow, Is.EqualTo(new Vector3(5.5f, 6.35f, 0f)));
    }

    [Test]
    public void TryGetAllyGridPosition_RejectsRowsBelowPaddedBottom()
    {
        MethodInfo method = GetMethod("TryGetAllyGridPosition");
        Assert.That(method, Is.Not.Null);

        object[] arguments =
        {
            new Vector2(0f, 0f),
            new Vector2(10f, 2f),
            8,
            0.5f,
            Vector3.zero
        };

        var result = (bool)method.Invoke(null, arguments);

        Assert.That(result, Is.False);
    }

    [TestCase(5f, 4f, true)]
    [TestCase(5.4f, 4f, true)]
    [TestCase(6f, 4f, false)]
    public void IsGridPositionOccupied_UsesMinimumDistance(
        float x,
        float y,
        bool expected)
    {
        MethodInfo method = typeof(UnitManager).GetMethod(
            "IsGridPositionOccupied",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var result = (bool)method.Invoke(
            null,
            new object[]
            {
                new Vector3(x, y, 0f),
                new List<Vector3> { new(5f, 4f, 0f) },
                0.5f
            });

        Assert.That(result, Is.EqualTo(expected));
    }

    private static Vector3 InvokeGridPosition(MethodInfo method, int index)
    {
        object[] arguments =
        {
            new Vector2(0f, 0f),
            new Vector2(10f, 8f),
            index,
            0.5f,
            Vector3.zero
        };

        var result = (bool)method.Invoke(null, arguments);
        Assert.That(result, Is.True);
        return (Vector3)arguments[4];
    }
}
#endif
