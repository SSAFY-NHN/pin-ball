#if UNITY_EDITOR
using NUnit.Framework;

public class UnitStatusEffectsTests
{
    [Test]
    public void Refresh_RestoresEveryExpiredEffectToNeutral()
    {
        var effects = new UnitStatusEffects();
        effects.ApplyAttackRateMultiplier(2f, 1f, 0f);
        effects.ApplyAttackDamageMultiplier(3f, 1f, 0f);
        effects.ApplyDefenseMultiplier(4f, 1f, 0f);
        effects.ApplyMoveSpeedMultiplier(0.5f, 1f, 0f);
        effects.ApplyDamageReduction(0.25f, 1f, 0f);
        effects.ApplyStun(1f, 0f);
        effects.ApplyKnockbackImmunity(1f, 0f);

        Assert.That(effects.IsStunned(0.5f), Is.True);
        Assert.That(effects.IsKnockbackImmune(0.5f), Is.True);
        effects.Refresh(1f);

        Assert.That(effects.AttackRateMultiplier, Is.EqualTo(1f));
        Assert.That(effects.AttackDamageMultiplier, Is.EqualTo(1f));
        Assert.That(effects.DefenseMultiplier, Is.EqualTo(1f));
        Assert.That(effects.MoveSpeedMultiplier, Is.EqualTo(1f));
        Assert.That(effects.DamageReduction, Is.Zero);
        Assert.That(effects.IsStunned(1f), Is.False);
        Assert.That(effects.IsKnockbackImmune(1f), Is.False);
    }

    [Test]
    public void DamageReduction_WeakerApplicationDoesNotReplaceActiveValue()
    {
        var effects = new UnitStatusEffects();
        effects.ApplyDamageReduction(0.5f, 2f, 0f);
        effects.ApplyDamageReduction(0.2f, 4f, 1f);

        Assert.That(effects.DamageReduction, Is.EqualTo(0.5f));
        effects.Refresh(5f);
        Assert.That(effects.DamageReduction, Is.Zero);
    }
}
#endif
