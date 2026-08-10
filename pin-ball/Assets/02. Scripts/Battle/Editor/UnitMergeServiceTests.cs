#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;
using UnityEngine;

public class UnitMergeServiceTests
{
    private readonly List<GameObject> _objects = new();
    private FakeUnitDataSource _dataSource;
    private UnitMergeService _service;

    [SetUp]
    public void SetUp()
    {
        _dataSource = new FakeUnitDataSource
        {
            AllyCommonValue = new AllyCommonData
            {
                maxLevel = 10,
                classLevel = 3
            }
        };
        _dataSource.Add("warrior", null);
        _dataSource.Add("ranger", null);
        _dataSource.Add("z_paladin", "warrior");
        _dataSource.Add("a_knight", "warrior");
        _service = new UnitMergeService(_dataSource);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject gameObject in _objects)
        {
            if (gameObject != null) Object.DestroyImmediate(gameObject);
        }

        _objects.Clear();
    }

    [Test]
    public void TryBegin_RejectsNullAndSameUnits()
    {
        AllyUnit ally = CreateAlly("warrior", 1, Vector3.zero);

        Assert.That(_service.TryBegin(null, ally).Type,
            Is.EqualTo(UnitMergeDecisionType.Rejected));
        Assert.That(_service.TryBegin(ally, ally).Type,
            Is.EqualTo(UnitMergeDecisionType.Rejected));
    }

    [Test]
    public void TryBegin_RejectsDifferentRootJobs()
    {
        AllyUnit warrior = CreateAlly("warrior", 1, Vector3.zero);
        AllyUnit ranger = CreateAlly("ranger", 1, Vector3.right);

        UnitMergeDecision decision = _service.TryBegin(warrior, ranger);

        Assert.That(decision.Type, Is.EqualTo(UnitMergeDecisionType.Rejected));
        Assert.That(_service.IsReserved(warrior), Is.False);
        Assert.That(_service.IsReserved(ranger), Is.False);
    }

    [Test]
    public void TryBegin_RejectsMaximumLevel()
    {
        AllyUnit source = CreateAlly("warrior", 10, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 9, Vector3.right);

        UnitMergeDecision decision = _service.TryBegin(source, target);

        Assert.That(decision.Type, Is.EqualTo(UnitMergeDecisionType.Rejected));
    }

    [Test]
    public void TryBegin_ReturnsImmediateBaseMergeBelowClassLevel()
    {
        AllyUnit source = CreateAlly("warrior", 1, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 1, new Vector3(2f, 3f));

        UnitMergeDecision decision = _service.TryBegin(source, target);

        Assert.That(decision.Type, Is.EqualTo(UnitMergeDecisionType.Immediate));
        Assert.That(decision.ResultUnitId, Is.EqualTo("warrior"));
        Assert.That(decision.ResultLevel, Is.EqualTo(2));
        Assert.That(decision.ResultPosition, Is.EqualTo(new Vector3(2f, 3f)));
        Assert.That(_service.IsReserved(source), Is.True);
        Assert.That(_service.IsReserved(target), Is.True);
    }

    [Test]
    public void Complete_ReleasesImmediateMergeReservations()
    {
        AllyUnit source = CreateAlly("warrior", 1, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 1, Vector3.right);
        UnitMergeDecision decision = _service.TryBegin(source, target);

        _service.Complete(decision);

        Assert.That(_service.IsReserved(source), Is.False);
        Assert.That(_service.IsReserved(target), Is.False);
    }

    [Test]
    public void TryBegin_ReturnsTwoSortedEvolutionChoicesAtClassLevel()
    {
        AllyUnit source = CreateAlly("warrior", 2, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 2, Vector3.right);

        UnitMergeDecision decision = _service.TryBegin(source, target);

        Assert.That(decision.Type,
            Is.EqualTo(UnitMergeDecisionType.EvolutionRequired));
        Assert.That(decision.FirstChoice.id, Is.EqualTo("a_knight"));
        Assert.That(decision.SecondChoice.id, Is.EqualTo("z_paladin"));
    }

    [Test]
    public void TryBegin_InvalidEvolutionCountRejectsAndReleasesReservations()
    {
        _dataSource.Allies.Remove("z_paladin");
        AllyUnit source = CreateAlly("warrior", 2, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 2, Vector3.right);

        UnitMergeDecision decision = _service.TryBegin(source, target);

        Assert.That(decision.Type, Is.EqualTo(UnitMergeDecisionType.Rejected));
        Assert.That(decision.RestoreSourcePosition, Is.True);
        Assert.That(_service.IsReserved(source), Is.False);
        Assert.That(_service.IsReserved(target), Is.False);
    }

    [Test]
    public void TryBegin_AdvancedUnitKeepsAdvancedJobId()
    {
        AllyUnit source = CreateAlly("a_knight", 4, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 4, Vector3.right);

        UnitMergeDecision decision = _service.TryBegin(source, target);

        Assert.That(decision.Type, Is.EqualTo(UnitMergeDecisionType.Immediate));
        Assert.That(decision.ResultUnitId, Is.EqualTo("a_knight"));
        Assert.That(decision.ResultLevel, Is.EqualTo(5));
    }

    [Test]
    public void TryChooseEvolution_InvalidIdLeavesPendingDecisionIntact()
    {
        AllyUnit source = CreateAlly("warrior", 2, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 2, Vector3.right);
        UnitMergeDecision pending = _service.TryBegin(source, target);
        Assert.That(pending.Type,
            Is.EqualTo(UnitMergeDecisionType.EvolutionRequired));

        bool invalid = _service.TryChooseEvolution("missing", out _);
        bool valid = _service.TryChooseEvolution(
            "a_knight",
            out UnitMergeDecision chosen);

        Assert.That(invalid, Is.False);
        Assert.That(valid, Is.True);
        Assert.That(chosen.ResultUnitId, Is.EqualTo("a_knight"));
        Assert.That(_service.IsReserved(source), Is.True);
        Assert.That(_service.IsReserved(target), Is.True);
    }

    [Test]
    public void TryChooseAutomaticEvolution_AlwaysChoosesSecondSortedCandidate()
    {
        AllyUnit source = CreateAlly("warrior", 2, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 2, Vector3.right);
        UnitMergeDecision pending = _service.TryBegin(source, target);
        Assert.That(pending.Type,
            Is.EqualTo(UnitMergeDecisionType.EvolutionRequired));

        bool selected = _service.TryChooseAutomaticEvolution(
            out UnitMergeDecision chosen);

        Assert.That(selected, Is.True);
        Assert.That(chosen.ResultUnitId, Is.EqualTo("z_paladin"));
        Assert.That(chosen.ResultLevel, Is.EqualTo(3));
    }

    [Test]
    public void CancelPendingEvolution_ReleasesBothReservations()
    {
        AllyUnit source = CreateAlly("warrior", 2, Vector3.zero);
        AllyUnit target = CreateAlly("warrior", 2, Vector3.right);
        UnitMergeDecision pending = _service.TryBegin(source, target);
        Assert.That(pending.Type,
            Is.EqualTo(UnitMergeDecisionType.EvolutionRequired));

        _service.CancelPendingEvolution();

        Assert.That(_service.IsReserved(source), Is.False);
        Assert.That(_service.IsReserved(target), Is.False);
    }

    private AllyUnit CreateAlly(string unitId, int level, Vector3 position)
    {
        var gameObject = new GameObject(unitId);
        _objects.Add(gameObject);
        gameObject.transform.position = position;
        var ally = gameObject.AddComponent<AllyUnit>();
        SetBackingField(ally, "<UnitId>k__BackingField", unitId);
        SetBackingField(ally, "<Level>k__BackingField", level);
        return ally;
    }

    private static void SetBackingField(
        AllyUnit ally,
        string fieldName,
        object value)
    {
        FieldInfo field = typeof(AllyUnit).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(ally, value);
    }

    private sealed class FakeUnitDataSource : IUnitDataSource
    {
        public readonly Dictionary<string, AllyUnitData> Allies = new();
        public AllyCommonData AllyCommonValue;

        public AllyCommonData AllyCommon => AllyCommonValue;
        public EnemyCommonData EnemyCommon => null;

        public void Add(string id, string previousJob)
        {
            Allies.Add(id, new AllyUnitData
            {
                id = id,
                previousJob = previousJob
            });
        }

        public bool TryGetAllyUnit(string id, out AllyUnitData result)
        {
            return Allies.TryGetValue(id, out result);
        }

        public bool TryGetEnemyUnit(string id, out EnemyUnitData result)
        {
            result = null;
            return false;
        }

        public bool TryGetRootAllyJob(string unitId, out AllyUnitData rootJob)
        {
            rootJob = null;
            if (!Allies.TryGetValue(unitId, out AllyUnitData current)) return false;

            while (!string.IsNullOrEmpty(current.previousJob))
            {
                if (!Allies.TryGetValue(current.previousJob, out current)) return false;
            }

            rootJob = current;
            return true;
        }

        public void GetNextAllyJobs(
            string previousJobId,
            List<AllyUnitData> result)
        {
            result.Clear();
            foreach (AllyUnitData ally in Allies.Values)
            {
                if (ally.previousJob == previousJobId) result.Add(ally);
            }
        }
    }
}
#endif
