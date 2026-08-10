using UnityEngine;

public static class UnitMovement
{
    public static Vector3 CalculateNextPosition(
        Vector3 currentPosition,
        Vector3 targetPosition,
        float speed,
        float deltaTime)
    {
        return Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
    }

    public static Vector3 ApplyKnockback(
        Vector3 position,
        Vector3 direction,
        float distance,
        bool isImmune)
    {
        if (isImmune || direction.sqrMagnitude <= 0.001f) return position;
        return position + direction.normalized * distance;
    }
}
