#if UNITY_EDITOR
using NUnit.Framework;

public sealed class WaveHudStateTests
{
    [TestCase(1, 1, EWaveHudNodeState.Current)]
    [TestCase(1, 2, EWaveHudNodeState.Locked)]
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
            new WaveHudState().ResolveNodeState(currentWave, nodeWave),
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
            new WaveHudState().IsConnectorComplete(
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
            new WaveHudState().IsSupportedWaveCount(waveCount),
            Is.EqualTo(expected));
    }

    [Test]
    public void FormatChances_LabelsPlayerHpAsChances()
    {
        Assert.That(StatusPanel.FormatChances(2, 3), Is.EqualTo("기회 2/3"));
    }

    [Test]
    public void FormatDefenseLines_ShowsBothTeams()
    {
        Assert.That(
            StatusPanel.FormatDefenseLines(12, 20, 7, 20),
            Is.EqualTo("아군 12/20 | 적 7/20"));
    }
}
#endif
