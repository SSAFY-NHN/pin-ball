using System.Collections.Generic;

public sealed class UnitMergeService
{
    private readonly IUnitDataSource _dataSource;
    private readonly HashSet<AllyUnit> _reservations = new();
    private readonly List<AllyUnitData> _evolutionCandidates = new();
    private UnitMergeDecision _pendingEvolution;

    public UnitMergeService(IUnitDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public bool IsReserved(AllyUnit ally)
    {
        return ally != null && _reservations.Contains(ally);
    }

    public void CancelPendingEvolution()
    {
    }
}
