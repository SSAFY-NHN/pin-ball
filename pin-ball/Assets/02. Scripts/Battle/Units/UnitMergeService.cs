using System.Collections.Generic;

using UnityEngine;

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

    public UnitMergeDecision TryBegin(AllyUnit source, AllyUnit target)
    {
        if (source == null || target == null || source == target ||
            _dataSource == null || _dataSource.AllyCommon == null ||
            _pendingEvolution != null ||
            IsReserved(source) || IsReserved(target))
        {
            return UnitMergeDecision.Rejected(false);
        }

        if (!_dataSource.TryGetAllyUnit(source.UnitId, out AllyUnitData sourceJob) ||
            !_dataSource.TryGetAllyUnit(target.UnitId, out AllyUnitData targetJob) ||
            !_dataSource.TryGetRootAllyJob(source.UnitId, out AllyUnitData sourceRoot) ||
            !_dataSource.TryGetRootAllyJob(target.UnitId, out AllyUnitData targetRoot) ||
            sourceRoot == null || targetRoot == null ||
            sourceRoot.id != targetRoot.id)
        {
            return UnitMergeDecision.Rejected(false);
        }

        int maxLevel = Mathf.Max(1, _dataSource.AllyCommon.maxLevel);
        int highestLevel = Mathf.Max(source.Level, target.Level);
        if (highestLevel >= maxLevel)
        {
            return UnitMergeDecision.Rejected(false);
        }

        int resultLevel = highestLevel + 1;
        string resultJobId = GetMergeResultJobId(sourceJob, targetJob);
        _reservations.Add(source);
        _reservations.Add(target);

        int classLevel = Mathf.Clamp(
            _dataSource.AllyCommon.classLevel,
            1,
            maxLevel);
        bool requiresEvolution =
            string.IsNullOrEmpty(targetJob.previousJob) &&
            string.IsNullOrEmpty(sourceJob.previousJob) &&
            resultLevel == classLevel;
        if (!requiresEvolution)
        {
            return UnitMergeDecision.Immediate(
                source,
                target,
                resultJobId,
                resultLevel,
                target.transform.position);
        }

        _dataSource.GetNextAllyJobs(sourceRoot.id, _evolutionCandidates);
        _evolutionCandidates.Sort((left, right) =>
            string.CompareOrdinal(left?.id, right?.id));
        if (_evolutionCandidates.Count != 2)
        {
            _reservations.Remove(source);
            _reservations.Remove(target);
            _evolutionCandidates.Clear();
            return UnitMergeDecision.Rejected(true);
        }

        _pendingEvolution = UnitMergeDecision.EvolutionRequired(
            source,
            target,
            resultLevel,
            target.transform.position,
            _evolutionCandidates[0],
            _evolutionCandidates[1]);
        return _pendingEvolution;
    }

    public bool TryChooseEvolution(
        string unitId,
        out UnitMergeDecision decision)
    {
        decision = null;
        if (_pendingEvolution == null) return false;

        bool isFirst = _pendingEvolution.FirstChoice != null &&
                       _pendingEvolution.FirstChoice.id == unitId;
        bool isSecond = _pendingEvolution.SecondChoice != null &&
                        _pendingEvolution.SecondChoice.id == unitId;
        if (!isFirst && !isSecond) return false;

        decision = UnitMergeDecision.Immediate(
            _pendingEvolution.Source,
            _pendingEvolution.Target,
            unitId,
            _pendingEvolution.ResultLevel,
            _pendingEvolution.ResultPosition);
        return true;
    }

    public void Complete(UnitMergeDecision decision)
    {
        if (decision == null) return;

        _reservations.Remove(decision.Source);
        _reservations.Remove(decision.Target);
        if (_pendingEvolution != null &&
            _pendingEvolution.Source == decision.Source &&
            _pendingEvolution.Target == decision.Target)
        {
            ClearPendingEvolution();
        }
    }

    public void CancelPendingEvolution()
    {
        if (_pendingEvolution == null) return;

        AllyUnit source = _pendingEvolution.Source;
        AllyUnit target = _pendingEvolution.Target;
        _reservations.Remove(source);
        _reservations.Remove(target);
        if (source != null) source.SetMergeReserved(false);
        if (target != null) target.SetMergeReserved(false);
        ClearPendingEvolution();
    }

    private static string GetMergeResultJobId(
        AllyUnitData sourceJob,
        AllyUnitData targetJob)
    {
        bool sourceAdvanced = !string.IsNullOrEmpty(sourceJob.previousJob);
        bool targetAdvanced = !string.IsNullOrEmpty(targetJob.previousJob);

        if (targetAdvanced) return targetJob.id;
        if (sourceAdvanced) return sourceJob.id;
        return targetJob.id;
    }

    private void ClearPendingEvolution()
    {
        _pendingEvolution = null;
        _evolutionCandidates.Clear();
    }
}
