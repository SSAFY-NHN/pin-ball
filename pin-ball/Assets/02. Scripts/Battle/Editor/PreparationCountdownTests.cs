#if UNITY_EDITOR
using NUnit.Framework;

public sealed class PreparationCountdownTests
{
    [Test]
    public void Advance_ExpiresOnceAtZeroAndStaysExpired()
    {
        var countdown = new PreparationCountdown(60f);

        Assert.That(countdown.RemainingTime, Is.EqualTo(60f));
        Assert.That(countdown.Advance(0f), Is.False);
        Assert.That(countdown.Advance(59.25f), Is.False);
        Assert.That(countdown.RemainingTime, Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(countdown.Advance(0.75f), Is.True);
        Assert.That(countdown.RemainingTime, Is.Zero);
        Assert.That(countdown.Advance(1f), Is.False);
        Assert.That(countdown.RemainingTime, Is.Zero);
    }

    [Test]
    public void Reset_RestoresDurationAndAllowsAnotherExpiration()
    {
        var countdown = new PreparationCountdown(60f);
        countdown.Advance(60f);

        countdown.Reset();

        Assert.That(countdown.RemainingTime, Is.EqualTo(60f));
        Assert.That(countdown.Advance(60f), Is.True);
    }
}
#endif
