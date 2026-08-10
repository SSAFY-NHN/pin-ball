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
            0.5f);
        Assert.That(result, Is.EqualTo(Vector3.right));
    }

    [Test]
    public void ApplyKnockback_RespectsImmunityAndNormalizesDirection()
    {
        Assert.That(
            UnitMovement.ApplyKnockback(Vector3.zero, Vector3.right * 2f, 3f, false),
            Is.EqualTo(Vector3.right * 3f));
        Assert.That(
            UnitMovement.ApplyKnockback(Vector3.zero, Vector3.right, 3f, true),
            Is.EqualTo(Vector3.zero));
    }
}
#endif
