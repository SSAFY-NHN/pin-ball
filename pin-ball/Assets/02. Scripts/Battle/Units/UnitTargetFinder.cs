using System.Collections.Generic;

using UnityEngine;

public sealed class UnitTargetFinder
{
    private readonly UnitRoster _roster;

    public UnitTargetFinder(UnitRoster roster)
    {
        _roster = roster;
    }

    public UnitBase FindClosestAliveEnemy(
        Vector3 fromPosition,
        float maxDistance)
    {
        return FindClosest(
            fromPosition,
            maxDistance,
            _roster.ActiveEnemies);
    }

    public UnitBase FindClosestAliveAlly(
        Vector3 fromPosition,
        float maxDistance)
    {
        return FindClosest(
            fromPosition,
            maxDistance,
            _roster.ActiveAllies);
    }

    public UnitBase FindFarthestAliveAlly(Vector3 fromPosition)
    {
        UnitBase result = null;
        float farthestDistance = float.MinValue;

        foreach (var ally in _roster.ActiveAllies)
        {
            if (ally == null || !ally.IsAlive) continue;

            float distance = Vector2.Distance(
                fromPosition,
                ally.transform.position);
            if (distance > farthestDistance)
            {
                result = ally;
                farthestDistance = distance;
            }
        }

        return result;
    }

    public UnitBase FindHighestHpAliveAlly()
    {
        UnitBase result = null;
        float highestHp = float.MinValue;

        foreach (var ally in _roster.ActiveAllies)
        {
            if (ally == null || !ally.IsAlive) continue;

            if (ally.CurrentHp > highestHp)
            {
                result = ally;
                highestHp = ally.CurrentHp;
            }
        }

        return result;
    }

    public void GetAliveEnemiesInRadius(
        Vector3 center,
        float radius,
        List<UnitBase> result)
    {
        GetAliveUnitsInRadius(
            center,
            radius,
            _roster.ActiveEnemies,
            result);
    }

    public void GetAliveAlliesInRadius(
        Vector3 center,
        float radius,
        List<UnitBase> result)
    {
        GetAliveUnitsInRadius(
            center,
            radius,
            _roster.ActiveAllies,
            result);
    }

    public void GetEnemiesInLine(
        Vector3 origin,
        Vector3 direction,
        float distance,
        float halfWidth,
        List<UnitBase> result)
    {
        result.Clear();
        var normalizedDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector3.right;

        foreach (var enemy in _roster.ActiveEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;

            var offset = enemy.transform.position - origin;
            var forwardDistance = Vector3.Dot(offset, normalizedDirection);
            if (forwardDistance < 0f || forwardDistance > distance) continue;

            var lateralOffset = offset - normalizedDirection * forwardDistance;
            if (lateralOffset.magnitude <= halfWidth)
            {
                result.Add(enemy);
            }
        }

        result.Sort((left, right) =>
            Vector2.Distance(origin, left.transform.position).CompareTo(
                Vector2.Distance(origin, right.transform.position)));
    }

    private static void GetAliveUnitsInRadius(
        Vector3 center,
        float radius,
        IReadOnlyList<UnitBase> candidates,
        List<UnitBase> result)
    {
        result.Clear();
        float sqrRadius = radius * radius;

        foreach (var candidate in candidates)
        {
            if (candidate == null || !candidate.IsAlive) continue;

            if ((candidate.transform.position - center).sqrMagnitude <= sqrRadius)
            {
                result.Add(candidate);
            }
        }
    }

    private static UnitBase FindClosest(
        Vector3 fromPosition,
        float maxDistance,
        IReadOnlyList<UnitBase> candidates)
    {
        UnitBase best = null;
        var bestDistance = maxDistance;

        foreach (var candidate in candidates)
        {
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            var distance = Vector2.Distance(
                fromPosition,
                candidate.transform.position);
            if (distance > bestDistance)
            {
                continue;
            }

            best = candidate;
            bestDistance = distance;
        }

        return best;
    }
}
