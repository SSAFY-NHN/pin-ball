using NUnit.Framework;

using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class WebGlFullscreenQualityControllerTests
{
    private GameObject host;
    private UniversalRenderPipelineAsset pipelineAsset;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("WebGL Fullscreen Quality Test");
        pipelineAsset = ScriptableObject.CreateInstance<
            UniversalRenderPipelineAsset>();
        pipelineAsset.renderScale = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        Object.DestroyImmediate(pipelineAsset);
    }

    [Test]
    public void ApplyIfChanged_QhdFullscreen_AppliesReducedScaleOnce()
    {
        var controller = host.AddComponent<
            WebGlFullscreenQualityController>();
        controller.Configure(pipelineAsset);

        Assert.That(controller.ApplyIfChanged(2560, 1440, true), Is.True);
        Assert.That(pipelineAsset.renderScale, Is.EqualTo(0.85f));
        Assert.That(controller.ApplyIfChanged(2560, 1440, true), Is.False);
        Assert.That(pipelineAsset.renderScale, Is.EqualTo(0.85f));
    }

    [Test]
    public void ApplyIfChanged_WindowedAfterQhd_RestoresFullScale()
    {
        var controller = host.AddComponent<
            WebGlFullscreenQualityController>();
        controller.Configure(pipelineAsset);
        controller.ApplyIfChanged(2560, 1440, true);

        Assert.That(controller.ApplyIfChanged(2560, 1440, false), Is.True);
        Assert.That(pipelineAsset.renderScale, Is.EqualTo(1f));
    }

    [Test]
    public void ApplyIfChanged_NullAsset_FailsWithoutChangingState()
    {
        var controller = host.AddComponent<
            WebGlFullscreenQualityController>();
        controller.Configure(null);

        Assert.That(controller.ApplyIfChanged(2560, 1440, true), Is.False);
    }

    [Test]
    public void RestoreOriginalRenderScale_RestoresConfiguredScale()
    {
        pipelineAsset.renderScale = 0.9f;
        var controller = host.AddComponent<
            WebGlFullscreenQualityController>();
        controller.Configure(pipelineAsset);
        controller.ApplyIfChanged(2560, 1440, true);

        controller.RestoreOriginalRenderScale();

        Assert.That(pipelineAsset.renderScale, Is.EqualTo(0.9f));
    }
}
