#if UNITY_EDITOR
using NUnit.Framework;

public class BattleUnitModifiersTests
{
    [Test]
    public void GetRosterSnapshot_AppliesRetainedBattleMultipliers()
    {
        var modifiers = new BattleUnitModifiers();
        modifiers.Apply(EItem.BattleClock, 0.12f, 0f, 0f);
        modifiers.Apply(EItem.FieldArmor, 0.15f, 0f, 0f);

        UnitModifierSnapshot snapshot = modifiers.GetRosterSnapshot(5);

        Assert.That(snapshot.AttackMultiplier, Is.EqualTo(1f));
        Assert.That(snapshot.AttackRateMultiplier, Is.EqualTo(1.12f));
        Assert.That(snapshot.HpMultiplier, Is.EqualTo(1.15f));
    }

    [Test]
    public void GetRosterSnapshot_CapsDiversityBonus()
    {
        var modifiers = new BattleUnitModifiers();
        modifiers.Apply(EItem.DiversityEmblem, 0.1f, 0.25f, 0f);

        UnitModifierSnapshot snapshot = modifiers.GetRosterSnapshot(5);

        Assert.That(snapshot.AttackMultiplier, Is.EqualTo(1.25f));
        Assert.That(snapshot.HpMultiplier, Is.EqualTo(1.25f));
    }
}
#endif
