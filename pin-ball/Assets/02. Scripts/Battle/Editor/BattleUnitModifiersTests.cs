#if UNITY_EDITOR
using NUnit.Framework;

public class BattleUnitModifiersTests
{
    [Test]
    public void GetRosterSnapshot_AppliesBattleMultipliers()
    {
        var modifiers = new BattleUnitModifiers();
        modifiers.Apply(EItem.AttackManual, 0.1f, 0f, 0f);
        modifiers.Apply(EItem.BattleClock, 0.12f, 0f, 0f);
        modifiers.Apply(EItem.FieldArmor, 0.15f, 0f, 0f);

        UnitModifierSnapshot snapshot = modifiers.GetRosterSnapshot(5);

        Assert.That(snapshot.AttackMultiplier, Is.EqualTo(1.1f));
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

    [Test]
    public void ShouldDuplicate_ReturnsFalseForWrongMergeTier()
    {
        var modifiers = CreateDuplicationModifiers();
        var data = CreateSpawnData(1);

        bool duplicated = modifiers.ShouldDuplicate(data, 0.25f, out int count);

        Assert.That(duplicated, Is.False);
        Assert.That(count, Is.Zero);
    }

    [Test]
    public void ShouldDuplicate_ReturnsFalseWhenRollIsGreaterThanChance()
    {
        var modifiers = CreateDuplicationModifiers();
        var data = CreateSpawnData(2);

        bool duplicated = modifiers.ShouldDuplicate(data, 0.5001f, out int count);

        Assert.That(duplicated, Is.False);
        Assert.That(count, Is.Zero);
    }

    [Test]
    public void ShouldDuplicate_ReturnsTrueWhenRollEqualsChance()
    {
        var modifiers = CreateDuplicationModifiers();
        var data = CreateSpawnData(2);

        bool duplicated = modifiers.ShouldDuplicate(data, 0.5f, out int count);

        Assert.That(duplicated, Is.True);
        Assert.That(count, Is.EqualTo(3));
    }

    private static BattleUnitModifiers CreateDuplicationModifiers()
    {
        var modifiers = new BattleUnitModifiers();
        modifiers.Apply(EItem.DuplicationSeal, 0.5f, 2f, 3f);
        return modifiers;
    }

    private static BattleUnitSpawnData CreateSpawnData(int mergeTier)
    {
        return new BattleUnitSpawnData
        {
            Modifier = new BattleUnitModifier
            {
                MergeTier = mergeTier
            }
        };
    }
}
#endif
