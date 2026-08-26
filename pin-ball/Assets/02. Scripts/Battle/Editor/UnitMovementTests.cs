#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class UnitMovementTests
{
    [Test]
    public void CalculateNextPosition_MovesTowardTargetBySpeedTimesDelta()
    {
        Vector3 result = UnitMovement.CalculateNextPosition(
            Vector3.zero,
            Vector3.right * 10f,
            2f,
            0.5f,
            0f);
        Assert.That(result, Is.EqualTo(Vector3.right));
    }

    [Test]
    public void CalculateNextPosition_KeepsBattleLineYWhenTargetHasDifferentY()
    {
        Vector3 result = UnitMovement.CalculateNextPosition(
            new Vector3(0f, 3f, 0f),
            new Vector3(10f, -8f, 0f),
            2f,
            0.5f,
            3f);

        Assert.That(result, Is.EqualTo(new Vector3(1f, 3f, 0f)));
    }

    [Test]
    public void ApplyKnockback_RespectsImmunityAndNormalizesDirection()
    {
        Assert.That(
            UnitMovement.ApplyKnockback(
                Vector3.zero, Vector3.right * 2f, 3f, false, 0f),
            Is.EqualTo(Vector3.right * 3f));
        Assert.That(
            UnitMovement.ApplyKnockback(
                Vector3.zero, Vector3.right, 3f, true, 0f),
            Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void ApplyKnockback_IgnoresVerticalDirectionAndKeepsBattleLineY()
    {
        Vector3 result = UnitMovement.ApplyKnockback(
            new Vector3(2f, 3f, 0f),
            new Vector3(-2f, 8f, 0f),
            4f,
            false,
            3f);

        Assert.That(result, Is.EqualTo(new Vector3(-2f, 3f, 0f)));
    }
}
#endif
