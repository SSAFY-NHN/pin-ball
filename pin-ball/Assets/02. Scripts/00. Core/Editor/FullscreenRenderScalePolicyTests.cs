using NUnit.Framework;

public sealed class FullscreenRenderScalePolicyTests
{
    [TestCase(1920, 1080, false)]
    [TestCase(1920, 1080, true)]
    [TestCase(2560, 1440, false)]
    [TestCase(0, 1440, true)]
    [TestCase(2560, 0, true)]
    public void Resolve_NonQhdFullscreenCondition_KeepsFullResolution(
        int width,
        int height,
        bool isFullScreen)
    {
        Assert.That(
            FullscreenRenderScalePolicy.Resolve(width, height, isFullScreen),
            Is.EqualTo(1f));
    }

    [TestCase(2560, 1440)]
    [TestCase(3840, 2160)]
    public void Resolve_QhdOrLargerFullscreen_UsesReducedRenderScale(
        int width,
        int height)
    {
        Assert.That(
            FullscreenRenderScalePolicy.Resolve(width, height, true),
            Is.EqualTo(0.85f));
    }

    [Test]
    public void TargetFrameRate_CapsWebGlWorkAtSixtyFrames()
    {
        Assert.That(FullscreenRenderScalePolicy.TargetFrameRate, Is.EqualTo(60));
    }
}
