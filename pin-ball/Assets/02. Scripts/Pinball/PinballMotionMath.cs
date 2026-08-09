using UnityEngine;

public readonly struct AnchoredCompression
{
    public float ScaleRatio { get; }
    public float CenterOffset { get; }

    public AnchoredCompression(float scaleRatio, float centerOffset)
    {
        ScaleRatio = scaleRatio;
        CenterOffset = centerOffset;
    }
}

public static class PinballMotionMath
{
    public static Vector2 CapVelocity(Vector2 velocity, float maximumSpeed)
    {
        if (maximumSpeed <= 0f || velocity.sqrMagnitude <= maximumSpeed * maximumSpeed)
        {
            return velocity;
        }

        return velocity.normalized * maximumSpeed;
    }

    public static Vector2 CalculateBumperVelocity(
        Vector2 incomingVelocity,
        Vector2 outwardDirection,
        float minimumExitSpeed,
        float speedBonus)
    {
        if (outwardDirection.sqrMagnitude < 0.001f)
        {
            outwardDirection = Vector2.up;
        }

        var exitSpeed = Mathf.Max(minimumExitSpeed, incomingVelocity.magnitude + speedBonus);
        return outwardDirection.normalized * exitSpeed;
    }

    public static AnchoredCompression CalculateAnchoredCompression(
        float originalHeight,
        float compressedAmount)
    {
        if (originalHeight <= 0.001f)
        {
            return new AnchoredCompression(1f, 0f);
        }

        var amount = Mathf.Clamp(compressedAmount, 0f, originalHeight * 0.8f);
        return new AnchoredCompression(
            (originalHeight - amount) / originalHeight,
            -amount * 0.5f);
    }
}
