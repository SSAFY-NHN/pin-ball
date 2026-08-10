#if UNITY_EDITOR
using System.Reflection;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class BattleCameraControllerTests
{
    private static readonly Vector3 BattlePosition = new(0f, 0f, -10f);
    private static readonly Vector3 PinballPosition = new(11.66f, -0.03f, -10f);

    [TestCase(EWaveState.Pending, true)]
    [TestCase(EWaveState.Active, false)]
    [TestCase(EWaveState.Victory, false)]
    [TestCase(EWaveState.Defeat, false)]
    public void ResolveTargetPosition_ReturnsPositionForWaveState(
        EWaveState state,
        bool expectsPinball)
    {
        MethodInfo method = typeof(BattleCameraController).GetMethod(
            "ResolveTargetPosition",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        var result = (Vector3)method.Invoke(
            null,
            new object[] { state, BattlePosition, PinballPosition });
        Vector3 expected = expectsPinball ? PinballPosition : BattlePosition;

        Assert.That(
            result,
            Is.EqualTo(expected).Using(
                Vector3ComparerWithEqualsOperator.Instance));
    }

    [TestCase(0f, 0f)]
    [TestCase(0.5f, 0.875f)]
    [TestCase(1f, 1f)]
    public void CalculateEasedProgress_UsesCubicEaseOut(
        float progress,
        float expected)
    {
        MethodInfo method = typeof(BattleCameraController).GetMethod(
            "CalculateEasedProgress",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        var result = (float)method.Invoke(null, new object[] { progress });

        Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
    }
}
#endif
