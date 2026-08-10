using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class PinballMotionTests
{
    [TestCase(false, 0f, false)]
    [TestCase(false, 0.01f, true)]
    [TestCase(true, 1f, false)]
    public void ShouldPlayPullSound_PlaysOnceAfterLeverActuallyMoves(
        bool hasPlayedPullSound,
        float pullDistance,
        bool expected)
    {
        var method = typeof(PinballLauncherController).GetMethod(
            "ShouldPlayPullSound",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        Assert.That(
            method?.Invoke(
                null,
                new object[] { hasPlayedPullSound, pullDistance }),
            Is.EqualTo(expected));
    }

    [Test]
    public void Magnet_IsActiveOnlyWhileMouseIsHeld()
    {
        var gameObject = new GameObject("Magnet Test");
        gameObject.AddComponent<BoxCollider2D>();
        var magnet = gameObject.AddComponent<PinballMagnetController>();
        var activeField = typeof(PinballMagnetController).GetField(
            "_isActive",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var mouseDownMethod = typeof(PinballMagnetController).GetMethod(
            "OnMouseDown",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var mouseUpMethod = typeof(PinballMagnetController).GetMethod(
            "OnMouseUp",
            BindingFlags.Instance | BindingFlags.NonPublic);

        try
        {
            mouseDownMethod?.Invoke(magnet, null);
            Assert.That(activeField?.GetValue(magnet), Is.EqualTo(true));

            mouseUpMethod?.Invoke(magnet, null);
            Assert.That(activeField?.GetValue(magnet), Is.EqualTo(false));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void CapVelocity_ReducesOnlyVelocityAboveMaximum()
    {
        Assert.That(
            PinballMotionMath.CapVelocity(new Vector2(12f, 0f), 8f),
            Is.EqualTo(new Vector2(8f, 0f)).Using(Vector2ComparerWithEqualsOperator.Instance));
        Assert.That(
            PinballMotionMath.CapVelocity(new Vector2(3f, 4f), 8f),
            Is.EqualTo(new Vector2(3f, 4f)).Using(Vector2ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void CalculateBumperVelocity_UsesOutwardDirectionAndMinimumSpeed()
    {
        var result = PinballMotionMath.CalculateBumperVelocity(
            new Vector2(2f, -1f), Vector2.up, 6f, 1f);

        Assert.That(result, Is.EqualTo(new Vector2(0f, 6f))
            .Using(Vector2ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void CalculateAnchoredCompression_KeepsBottomEdgeStationary()
    {
        var result = PinballMotionMath.CalculateAnchoredCompression(2f, 0.6f);

        Assert.That(result.ScaleRatio, Is.EqualTo(0.7f).Within(0.0001f));
        Assert.That(result.CenterOffset, Is.EqualTo(-0.3f).Within(0.0001f));
    }
}
