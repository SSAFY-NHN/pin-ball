using System.Collections.Generic;

public interface IUnitDataSource
{
    AllyCommonData AllyCommon { get; }
    EnemyCommonData EnemyCommon { get; }
    bool TryGetAllyUnit(string id, out AllyUnitData result);
    bool TryGetEnemyUnit(string id, out EnemyUnitData result);
    bool TryGetRootAllyJob(string unitId, out AllyUnitData rootJob);
    void GetNextAllyJobs(string previousJobId, List<AllyUnitData> result);
}
