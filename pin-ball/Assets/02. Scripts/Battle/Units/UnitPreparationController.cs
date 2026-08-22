using System.Linq;

using UnityEngine;

public sealed class UnitPreparationController
{
    private readonly UnitRoster _roster;
    private readonly UnitPlacementService _placementService;

    public UnitPreparationController(
        UnitRoster roster,
        BattleAreaBounds battleArea)
    {
        _roster = roster;
        _placementService = new UnitPlacementService(battleArea);
    }

    public bool CanDrag(AllyUnit ally, bool canUsePreparationActions)
    {
        return ally != null &&
               canUsePreparationActions &&
               _roster.OwnedAllies.Contains(ally) &&
               ally.IsAlive;
    }

    public bool TryPlaceInFreeGridSlot(AllyUnit ally) =>
        _placementService.TryPlaceInFreeGridSlot(ally);

    public bool TryGetSavedPosition(AllyUnit ally, out Vector3 position) =>
        _placementService.TryGetSavedPosition(ally, out position);

    public void Remove(AllyUnit ally) => _placementService.Remove(ally);

    public bool IsValidPlacement(AllyUnit ally, Vector3 position) =>
        _placementService.IsValidPlacement(ally, position);

    public bool TrySave(AllyUnit ally, Vector3 position) =>
        _placementService.TrySave(ally, position);

}
