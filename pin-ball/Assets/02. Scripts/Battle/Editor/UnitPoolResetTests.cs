#if UNITY_EDITOR
using System.Reflection;

using NUnit.Framework;
using UnityEngine;

public sealed class UnitPoolResetTestUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Ally;
    protected override void Tick() { }
}

public class UnitPoolResetTests
{
    [Test]
    public void RestoreForPreparation_ResetsAllComposedCombatState()
    {
        var unitObject = new GameObject("unit");
        var areaObject = new GameObject("area");
        try
        {
            var unit = unitObject.AddComponent<UnitPoolResetTestUnit>();
            var area = areaObject.AddComponent<BattleAreaBounds>();
            var roster = new UnitRoster();
            var context = new UnitCombatContext(
                new UnitTargetFinder(roster),
                area,
                _ => { });
            unit.Initialize(new BattleUnitStats
            {
                MaxHp = 100f,
                Defense = 0f,
                AttackDamage = 10f,
                AttackRate = 1f,
                AttackRange = 1f,
                MoveSpeed = 1f
            }, context);
            unit.TakeDamage(20f);
            unit.ApplyAttackRateMultiplier(2f, 10f);
            unit.ApplyDamageReduction(0.5f, 10f);
            unit.ApplyDamageOverTime(10f, 2f);
            unit.MarkReturnedToPool();
            unit.RestoreForPreparation(Vector3.one);

            var status = GetField<UnitStatusEffects>(unit, "_statusEffects");
            var scheduler = GetField<UnitEffectScheduler>(unit, "_effectScheduler");
            Assert.That(unit.CurrentHp, Is.EqualTo(100f));
            Assert.That(unit.State, Is.EqualTo(EBattleUnitState.Idle));
            Assert.That(unit.IsInPool, Is.False);
            Assert.That(status.AttackRateMultiplier, Is.EqualTo(1f));
            Assert.That(status.DamageReduction, Is.Zero);
            Assert.That(
                GetField<UnitBase>(unit, "_currentTarget"),
                Is.Null);
            Assert.That(
                GetField<int>(scheduler, "_remainingDamageTicks"),
                Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(unitObject);
            Object.DestroyImmediate(areaObject);
        }
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            field = instance.GetType().BaseType?.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        return (T)field?.GetValue(instance);
    }
}
#endif
