#if UNITY_EDITOR
using System.Collections.Generic;

using NUnit.Framework;

public class UnitEffectSchedulerTests
{
    [Test]
    public void DamageOverTime_UsesCeilingTicksAndDoesNotCatchUp()
    {
        var scheduler = new UnitEffectScheduler();
        var damage = new List<float>();
        scheduler.ScheduleDamageOverTime(9f, 2.1f, 0.25f, 0f);

        scheduler.Tick(0.6f, (value, _) => damage.Add(value), null);
        scheduler.Tick(0.7f, (value, _) => damage.Add(value), null);
        scheduler.Tick(10f, (value, _) => damage.Add(value), null);

        Assert.That(damage, Is.EqualTo(new[] { 3f, 3f }));
    }

    [Test]
    public void NewDamageOverTime_ReplacesOldAndDelayedSlowFiresOnce()
    {
        var scheduler = new UnitEffectScheduler();
        float damage = 0f;
        int slowCount = 0;
        scheduler.ScheduleDamageOverTime(10f, 2f, 0f, 0f);
        scheduler.ScheduleDamageOverTime(3f, 1f, 0f, 0f);
        scheduler.ScheduleSlow(0.5f, 0.75f, 2f, 1f, 0f);

        scheduler.Tick(1f, (value, _) => damage += value, (_, _, _) => slowCount++);
        scheduler.Tick(2f, (value, _) => damage += value, (_, _, _) => slowCount++);

        Assert.That(damage, Is.EqualTo(3f));
        Assert.That(slowCount, Is.EqualTo(1));
    }
}
#endif
