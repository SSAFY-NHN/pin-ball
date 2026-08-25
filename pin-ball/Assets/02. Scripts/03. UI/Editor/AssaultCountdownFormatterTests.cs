#if UNITY_EDITOR
using NUnit.Framework;

public sealed class AssaultCountdownFormatterTests
{
    [TestCase(0f, "강화 증원까지 00:60")]
    [TestCase(0.01f, "강화 증원까지 00:60")]
    [TestCase(59f, "강화 증원까지 00:01")]
    [TestCase(59.99f, "강화 증원까지 00:01")]
    [TestCase(60f, "최후 공세까지 00:30")]
    [TestCase(89.99f, "최후 공세까지 00:01")]
    [TestCase(90f, "최후 공세 진행 중")]
    public void Format_ReturnsPhaseCountdownAtBoundaries(
        float elapsedTime,
        string expected)
    {
        Assert.That(
            AssaultCountdownFormatter.Format(elapsedTime),
            Is.EqualTo(expected));
    }
}
#endif
