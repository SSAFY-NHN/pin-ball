#if UNITY_EDITOR
using NUnit.Framework;

public class WaveResultPanelTests
{
    [TestCase(EWaveResolutionResult.Cleared, "웨이브 클리어")]
    [TestCase(EWaveResolutionResult.Failed, "방어 실패")]
    public void ResolveCopy_ReturnsOutcomeSpecificText(
        EWaveResolutionResult result,
        string expected)
    {
        Assert.That(
            WaveResultPanel.ResolveCopy(
                result,
                "웨이브 클리어",
                "방어 실패"),
            Is.EqualTo(expected));
    }
}
#endif
