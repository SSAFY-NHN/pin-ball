#if UNITY_EDITOR
using System.Reflection;
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

    [TestCase(0.49f, false)]
    [TestCase(0.5f, true)]
    [TestCase(9.5f, true)]
    [TestCase(9.51f, false)]
    public void ContainsAllyPlacement_UsesEntireConfiguredArea(
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
        Vector3 nextRow = InvokeGridPosition(method, 8);

        Assert.That(first, Is.EqualTo(new Vector3(0.5f, 7.5f, 0f)));
        Assert.That(second, Is.EqualTo(new Vector3(1.65f, 7.5f, 0f)));
        Assert.That(nextRow, Is.EqualTo(new Vector3(0.5f, 6.35f, 0f)));
    }

    [TestCase(0f, false)]
    [TestCase(0.0001f, true)]
    [TestCase(0.08f, true)]
    [TestCase(5.3f, true)]
    public void HasCameraMoved_RefreshesAnyScreenToWorldChange(
        float horizontalMovement,
        bool expected)
    {
        MethodInfo method = GetMethod("HasCameraMoved");
        Assert.That(method, Is.Not.Null);

        var result = (bool)method.Invoke(
            null,
            new object[]
            {
                Vector3.zero,
                new Vector3(horizontalMovement, 0f, 0f)
            });

        Assert.That(result, Is.EqualTo(expected));
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
