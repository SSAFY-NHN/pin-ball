using UnityEngine;

public static class UnitMovement
{
    public static Vector3 CalculateNextPosition(
        Vector3 currentPosition,
        Vector3 targetPosition,
        float speed,
        float deltaTime,
        float battleLineY)
    {
        currentPosition.y = battleLineY;
        targetPosition.y = battleLineY;
        Vector3 nextPosition = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
        nextPosition.y = battleLineY;
        return nextPosition;
    }

    public static Vector3 ApplyKnockback(
        Vector3 position,
        Vector3 direction,
        float distance,
        bool isImmune,
        float battleLineY)
    {
        position.y = battleLineY;
        direction.y = 0f;
        if (isImmune || direction.sqrMagnitude <= 0.001f) return position;
        Vector3 result = position + direction.normalized * distance;
        result.y = battleLineY;
        return result;
    }
}
