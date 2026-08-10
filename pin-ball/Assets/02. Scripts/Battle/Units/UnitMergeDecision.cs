using UnityEngine;

public enum UnitMergeDecisionType
{
    Rejected,
    Immediate,
    EvolutionRequired
}

public sealed class UnitMergeDecision
{
    public UnitMergeDecisionType Type { get; }
    public AllyUnit Source { get; }
    public AllyUnit Target { get; }
    public string ResultUnitId { get; }
    public int ResultLevel { get; }
    public Vector3 ResultPosition { get; }
    public AllyUnitData FirstChoice { get; }
    public AllyUnitData SecondChoice { get; }
    public bool RestoreSourcePosition { get; }

    private UnitMergeDecision(
        UnitMergeDecisionType type,
        AllyUnit source,
        AllyUnit target,
        string resultUnitId,
        int resultLevel,
        Vector3 resultPosition,
        AllyUnitData firstChoice,
        AllyUnitData secondChoice,
        bool restoreSourcePosition)
    {
        Type = type;
        Source = source;
        Target = target;
        ResultUnitId = resultUnitId;
        ResultLevel = resultLevel;
        ResultPosition = resultPosition;
        FirstChoice = firstChoice;
        SecondChoice = secondChoice;
        RestoreSourcePosition = restoreSourcePosition;
    }

    public static UnitMergeDecision Rejected(bool restoreSourcePosition)
    {
        return new UnitMergeDecision(
            UnitMergeDecisionType.Rejected,
            null,
            null,
            null,
            0,
            default,
            null,
            null,
            restoreSourcePosition);
    }

    public static UnitMergeDecision Immediate(
        AllyUnit source,
        AllyUnit target,
        string resultUnitId,
        int resultLevel,
        Vector3 resultPosition)
    {
        return new UnitMergeDecision(
            UnitMergeDecisionType.Immediate,
            source,
            target,
            resultUnitId,
            resultLevel,
            resultPosition,
            null,
            null,
            false);
    }

    public static UnitMergeDecision EvolutionRequired(
        AllyUnit source,
        AllyUnit target,
        int resultLevel,
        Vector3 resultPosition,
        AllyUnitData firstChoice,
        AllyUnitData secondChoice)
    {
        return new UnitMergeDecision(
            UnitMergeDecisionType.EvolutionRequired,
            source,
            target,
            null,
            resultLevel,
            resultPosition,
            firstChoice,
            secondChoice,
            false);
    }
}
