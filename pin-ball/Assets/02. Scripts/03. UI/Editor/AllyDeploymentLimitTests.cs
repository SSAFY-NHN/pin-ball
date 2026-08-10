#if UNITY_EDITOR
using NUnit.Framework;

public class AllyDeploymentLimitTests
{
    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(5, true)]
    [TestCase(6, false)]
    [TestCase(7, false)]
    public void CanStartWaveWithAllyCount_UsesFiveUnitLimit(
        int count,
        bool expected)
    {
        Assert.That(
            UnitManager.CanStartWaveWithAllyCount(count),
            Is.EqualTo(expected));
    }

    [TestCase(0, true)]
    [TestCase(5, true)]
    [TestCase(6, true)]
    [TestCase(7, false)]
    public void CanLaunchPinballWithAllyCount_AllowsExactlySix(
        int count,
        bool expected)
    {
        Assert.That(
            UnitManager.CanLaunchPinballWithAllyCount(count),
            Is.EqualTo(expected));
    }

    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(5, false)]
    [TestCase(6, true)]
    public void ShouldWarnAllyCount_WarnsWhenWaveCannotStart(
        int count,
        bool expected)
    {
        Assert.That(
            StatusPanel.ShouldWarnAllyCount(count),
            Is.EqualTo(expected));
    }
}
#endif
