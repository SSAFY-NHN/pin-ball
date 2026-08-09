#if UNITY_EDITOR
using NUnit.Framework;

public class WaveHudStateTests
{
    private readonly WaveHudState _state = new();

    [TestCase(1, 1, EWaveHudNodeState.Current)]
    [TestCase(1, 2, EWaveHudNodeState.Idle)]
    [TestCase(2, 1, EWaveHudNodeState.Complete)]
    [TestCase(5, 5, EWaveHudNodeState.Elite05)]
    [TestCase(9, 9, EWaveHudNodeState.Elite09)]
    [TestCase(10, 10, EWaveHudNodeState.Boss10)]
    public void ResolveNodeState_ReturnsExpectedState(
        int currentWave,
        int nodeWave,
        EWaveHudNodeState expected)
    {
        Assert.That(
            _state.ResolveNodeState(currentWave, nodeWave),
            Is.EqualTo(expected));
    }

    [TestCase(1, 1, false)]
    [TestCase(2, 1, true)]
    [TestCase(10, 9, true)]
    public void IsConnectorComplete_CompletesOnlyBeforeCurrentWave(
        int currentWave,
        int connectorAfterWave,
        bool expected)
    {
        Assert.That(
            _state.IsConnectorComplete(
                currentWave,
                connectorAfterWave),
            Is.EqualTo(expected));
    }

    [TestCase(10, true)]
    [TestCase(9, false)]
    [TestCase(11, false)]
    public void IsSupportedWaveCount_AcceptsExactlyTen(
        int waveCount,
        bool expected)
    {
        Assert.That(
            _state.IsSupportedWaveCount(waveCount),
            Is.EqualTo(expected));
    }
}
#endif
