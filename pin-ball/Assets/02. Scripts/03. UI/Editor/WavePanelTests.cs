#if UNITY_EDITOR
using NUnit.Framework;

public sealed class WavePanelTests
{
    [TestCase(60f, "전투 시작 (60)")]
    [TestCase(59.01f, "전투 시작 (60)")]
    [TestCase(1f, "전투 시작 (1)")]
    [TestCase(0f, "전투 시작 (0)")]
    public void FormatStartButtonLabel_CeilsRemainingSeconds(
        float remainingTime,
        string expected)
    {
        Assert.That(
            WavePanel.FormatStartButtonLabel(remainingTime),
            Is.EqualTo(expected));
    }
}
#endif
