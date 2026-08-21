#if UNITY_EDITOR
using NUnit.Framework;

public sealed class WaveResolutionTests
{
    [Test]
    public void TryBegin_TracksOnePendingResultAndDeadline()
    {
        var state = new WaveResolutionState();

        Assert.That(state.TryBegin(
            EWaveResolutionResult.Cleared,
            3,
            10f,
            2f), Is.True);
        Assert.That(state.TryBegin(
            EWaveResolutionResult.Failed,
            3,
            10f,
            2f), Is.False);
        Assert.That(state.Result, Is.EqualTo(EWaveResolutionResult.Cleared));
        Assert.That(state.WaveNumber, Is.EqualTo(3));
        Assert.That(state.EndsAt, Is.EqualTo(12f));
        Assert.That(state.IsElapsed(11.99f), Is.False);
        Assert.That(state.IsElapsed(12f), Is.True);
    }

    [Test]
    public void Clear_RemovesPendingResolution()
    {
        var state = new WaveResolutionState();
        state.TryBegin(EWaveResolutionResult.Failed, 2, 5f, 1f);

        state.Clear();

        Assert.That(state.IsPending, Is.False);
        Assert.That(state.WaveNumber, Is.Zero);
        Assert.That(state.EndsAt, Is.Zero);
    }
}
#endif
