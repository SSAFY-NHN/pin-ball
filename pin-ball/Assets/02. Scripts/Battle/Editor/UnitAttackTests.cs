#if UNITY_EDITOR
using NUnit.Framework;

public class UnitAttackTests
{
    [Test]
    public void TrySchedule_UsesInverseAttackRate()
    {
        var attack = new UnitAttack();
        Assert.That(attack.TrySchedule(5f, 2f), Is.True);
        Assert.That(attack.NextAttackTime, Is.EqualTo(5.5f));
        Assert.That(attack.TrySchedule(5.25f, 2f), Is.False);
    }
}
#endif
