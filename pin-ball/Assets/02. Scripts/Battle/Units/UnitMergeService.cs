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
        string resultJobId = GetJobDepth(sourceJob) > GetJobDepth(targetJob)
            ? sourceJob.id
            : targetJob.id;
        _reservations.Add(source);
        _reservations.Add(target);

        int firstClassLevel = Mathf.Clamp(
            _dataSource.AllyCommon.firstClassLevel, 1, maxLevel);
        int secondClassLevel = Mathf.Clamp(
            _dataSource.AllyCommon.secondClassLevel, firstClassLevel, maxLevel);
        bool isEvolutionLevel =
            resultLevel == firstClassLevel || resultLevel == secondClassLevel;
        if (!isEvolutionLevel)
        {
            return UnitMergeDecision.Immediate(
                source,
                target,
                resultJobId,
                resultLevel,
                target.transform.position);
        }

        _dataSource.GetNextAllyJobs(resultJobId, _evolutionCandidates);
        _evolutionCandidates.Sort((left, right) =>
            string.CompareOrdinal(left?.id, right?.id));
        int expectedCount = resultLevel == firstClassLevel ? 2 : 1;
        if (_evolutionCandidates.Count != expectedCount)
        {
            _reservations.Remove(source);
            _reservations.Remove(target);
            _evolutionCandidates.Clear();
            return UnitMergeDecision.Rejected(true);
        }

        if (expectedCount == 1)
        {
            var nextJob = _evolutionCandidates[0];
            _evolutionCandidates.Clear();
            return UnitMergeDecision.Immediate(
                source,
                target,
                nextJob.id,
                resultLevel,
                target.transform.position);
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

    public bool TryChooseAutomaticEvolution(
        out UnitMergeDecision decision)
    {
        decision = null;
        // TODO: Restore a configurable choice when the alternate jobs are ready.
        if (_pendingEvolution?.SecondChoice == null) return false;

        return TryChooseEvolution(
            _pendingEvolution.SecondChoice.id,
            out decision);
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

    private void ClearPendingEvolution()
    {
        _pendingEvolution = null;
        _evolutionCandidates.Clear();
    }

    private int GetJobDepth(AllyUnitData job)
    {
        int depth = 0;
        while (job != null && !string.IsNullOrEmpty(job.previousJob) &&
               _dataSource.TryGetAllyUnit(job.previousJob, out job))
        {
            depth++;
        }

        return depth;
    }
}
