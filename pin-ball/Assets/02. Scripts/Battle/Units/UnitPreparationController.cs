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

    public void BeginLineageHighlight(AllyUnit source)
    {
        EndLineageHighlight();
        if (source == null || _dataSource == null ||
            !_dataSource.TryGetRootAllyJob(source.UnitId, out var sourceRoot))
        {
            return;
        }

        foreach (var ally in _roster.OwnedAllies)
        {
            if (ally == null || ally == source || ally.IsInPool ||
                !ally.IsAlive || !ally.gameObject.activeInHierarchy ||
                !_dataSource.TryGetRootAllyJob(ally.UnitId, out var allyRoot))
            {
                continue;
            }

            ally.SetLineageHighlighted(allyRoot.id == sourceRoot.id);
        }
    }

    public void EndLineageHighlight()
    {
        foreach (var ally in _roster.OwnedAllies)
        {
            ally?.SetLineageHighlighted(false);
        }
    }

    public UnitMergeDecision TryBeginMerge(
        AllyUnit source,
        AllyUnit target,
        Vector3 sourceOriginalPosition)
    {
        UnitMergeDecision decision = _mergeService.TryBegin(source, target);
        if (decision.Type == UnitMergeDecisionType.Rejected)
        {
            if (decision.RestoreSourcePosition && source != null)
            {
                source.transform.position = sourceOriginalPosition;
            }

            return decision;
        }

        if (decision.Type == UnitMergeDecisionType.EvolutionRequired)
        {
            _pendingSource = source;
            _pendingSourcePosition = sourceOriginalPosition;
            decision.Source.SetMergeReserved(true);
            decision.Target.SetMergeReserved(true);
        }

        return decision;
    }

    public bool TryChooseEvolution(
        string unitId,
        out UnitMergeDecision decision) =>
        _mergeService.TryChooseEvolution(unitId, out decision);

    public void Complete(UnitMergeDecision decision)
    {
        _mergeService.Complete(decision);
        _pendingSource = null;
    }

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
