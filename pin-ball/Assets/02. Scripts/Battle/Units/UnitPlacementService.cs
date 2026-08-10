using System.Collections.Generic;

using UnityEngine;

public sealed class UnitPlacementService
{
    private readonly BattleAreaBounds _battleArea;
    private readonly Dictionary<AllyUnit, Vector3> _savedPositions = new();

    public UnitPlacementService(BattleAreaBounds battleArea)
    {
        _battleArea = battleArea;
    }

    public bool IsValidPlacement(AllyUnit ally, Vector3 position)
    {
        return ally != null &&
               _battleArea != null &&
               _battleArea.ContainsAllyPlacement(position, GetPadding(ally));
    }

    public bool TrySave(AllyUnit ally, Vector3 position)
    {
        if (!IsValidPlacement(ally, position)) return false;

        _savedPositions[ally] = position;
        return true;
    }

    public bool TryGetSavedPosition(AllyUnit ally, out Vector3 position)
    {
        if (ally != null && _savedPositions.TryGetValue(ally, out position))
        {
            return true;
        }

        position = default;
        return false;
    }

    public bool TryPlaceInFreeGridSlot(AllyUnit ally)
    {
        if (ally == null || _battleArea == null) return false;

        float padding = GetPadding(ally);
        if (!TryFindFreeGridSlot(padding, out Vector3 candidate)) return false;

        ally.transform.position = candidate;
        _savedPositions[ally] = candidate;
        return true;
    }

    public bool TryFindFreeGridSlot(
        float padding,
        out Vector3 position)
    {
        position = default;
        if (_battleArea == null) return false;

        float safePadding = Mathf.Max(0f, padding);
        float minimumDistance = safePadding * 2f + 0.15f;
        for (var gridIndex = 0; _battleArea.TryGetAllyGridPosition(
                 gridIndex,
                 safePadding,
                 out Vector3 candidate); gridIndex++)
        {
            if (IsGridPositionOccupied(
                    candidate,
                    _savedPositions.Values,
                    minimumDistance)) continue;

            position = candidate;
            return true;
        }

        return false;
    }

    public void Remove(AllyUnit ally)
    {
        if (ally != null) _savedPositions.Remove(ally);
    }

    public static float GetPadding(AllyUnit ally)
    {
        if (ally == null) return 0f;

        var unitCollider = ally.GetComponentInChildren<Collider2D>();
        return unitCollider == null
            ? 0f
            : Mathf.Max(
                unitCollider.bounds.extents.x,
                unitCollider.bounds.extents.y);
    }

    private static bool IsGridPositionOccupied(
        Vector3 candidate,
        IEnumerable<Vector3> occupiedPositions,
        float minimumDistance)
    {
        foreach (Vector3 occupied in occupiedPositions)
        {
            if (Vector2.Distance(candidate, occupied) < minimumDistance)
            {
                return true;
            }
        }

        return false;
    }
}
