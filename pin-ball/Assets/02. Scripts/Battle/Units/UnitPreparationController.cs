using System.Linq;

using UnityEngine;

public sealed class UnitPreparationController
{
    private readonly UnitRoster _roster;
    private readonly IUnitDataSource _dataSource;
    private readonly UnitPlacementService _placementService;
    private readonly UnitMergeService _mergeService;
    private AllyUnit _pendingSource;
    private Vector3 _pendingSourcePosition;

    public UnitPreparationController(
        UnitRoster roster,
        IUnitDataSource dataSource,
        BattleAreaBounds battleArea)
    {
        _roster = roster;
        _dataSource = dataSource;
        _placementService = new UnitPlacementService(battleArea);
        _mergeService = new UnitMergeService(dataSource);
    }

    public bool CanDrag(AllyUnit ally, bool canUsePreparationActions)
    {
        return ally != null &&
               canUsePreparationActions &&
               _roster.OwnedAllies.Contains(ally) &&
               !_mergeService.IsReserved(ally) &&
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

    public void CancelPendingEvolution()
    {
        _mergeService.CancelPendingEvolution();
        if (_pendingSource != null)
        {
            _pendingSource.transform.position = _pendingSourcePosition;
        }

        _pendingSource = null;
    }
}
