#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGlPerformanceBuild
{
    public static BuildPlayerOptions CreateOptions(bool development, string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("At least one enabled build scene is required.");
        }

        return new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = development
                ? BuildOptions.Development | BuildOptions.ConnectWithProfiler
                : BuildOptions.None,
        };
    }

    public static void BuildDevelopment()
    {
        Build(true, "WebGLPerformanceDevelopment");
    }

    public static void BuildRelease()
    {
        Build(false, "WebGLPerformanceRelease");
    }

    private static void Build(bool development, string directoryName)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputPath = Path.Combine(projectRoot, ".utmp", directoryName);
        Directory.CreateDirectory(outputPath);

        BuildReport report = BuildPipeline.BuildPlayer(CreateOptions(development, outputPath));
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }
    }
}
#endif
