#if UNITY_EDITOR
using NUnit.Framework;

public class UnitHealthTests
{
    [Test]
    public void TakeDamage_AppliesDefenseReductionAndShieldInOrder()
    {
        var health = new UnitHealth(100f);
        health.ApplyShield(20f, 5f, 0f);

        UnitDamageResult result = health.TakeDamage(100f, 100f, 0f, 0f, 0f);

        Assert.That(result.AbsorbedDamage, Is.EqualTo(20f));
        Assert.That(result.AppliedDamage, Is.EqualTo(30f));
        Assert.That(health.CurrentHp, Is.EqualTo(70f));
    }

    [Test]
    public void HealingAndMaximumHpScaling_PreserveCapsAndRatio()
    {
        var health = new UnitHealth(100f);
        health.TakeDamage(50f, 0f, 0f, 0f, 1f);
        health.ScaleMaximumHp(2f);
        health.Heal(200f);

        Assert.That(health.MaxHp, Is.EqualTo(200f));
        Assert.That(health.CurrentHp, Is.EqualTo(200f));
    }

    [Test]
    public void Damage_ClampsArmorIgnoreAndHpFloor()
    {
        var health = new UnitHealth(10f);
        UnitDamageResult result = health.TakeDamage(100f, 100f, 2f, 0f, 3f);

        Assert.That(result.AppliedDamage, Is.EqualTo(10f));
        Assert.That(health.CurrentHp, Is.Zero);
        Assert.That(result.Died, Is.True);
    }
}
#endif
