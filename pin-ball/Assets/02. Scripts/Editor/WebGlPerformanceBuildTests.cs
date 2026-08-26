#if UNITY_EDITOR
using NUnit.Framework;

using UnityEditor;

public sealed class WebGlPerformanceBuildTests
{
    [Test]
    public void CreateOptions_Development_UsesProfilerWebGlBuild()
    {
        BuildPlayerOptions options = WebGlPerformanceBuild.CreateOptions(
            true,
            "Temp/WebGLDevelopment");

        Assert.That(options.target, Is.EqualTo(BuildTarget.WebGL));
        Assert.That(
            options.locationPathName,
            Is.EqualTo("Temp/WebGLDevelopment"));
        Assert.That(
            options.options.HasFlag(BuildOptions.Development),
            Is.True);
        Assert.That(
            options.options.HasFlag(BuildOptions.ConnectWithProfiler),
            Is.True);
        Assert.That(options.scenes, Is.Not.Empty);
    }

    [Test]
    public void CreateOptions_Release_DisablesDevelopmentFlags()
    {
        BuildPlayerOptions options = WebGlPerformanceBuild.CreateOptions(
            false,
            "Temp/WebGLRelease");

        Assert.That(options.target, Is.EqualTo(BuildTarget.WebGL));
        Assert.That(options.options, Is.EqualTo(BuildOptions.None));
        Assert.That(options.scenes, Is.Not.Empty);
    }
}
#endif
