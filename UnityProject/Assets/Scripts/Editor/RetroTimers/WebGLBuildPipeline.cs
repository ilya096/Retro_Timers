using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RetroTimers.EditorTools
{
    public static class WebGLBuildPipeline
    {
        private const string DefaultOutputPath = "Builds/WebGL";

        [MenuItem("RetroTimers/Build WebGL")]
        public static void BuildWebGLMenu()
        {
            BuildWebGL(DefaultOutputPath);
        }

        public static void BuildGithubPages()
        {
            BuildWebGL(GetCommandLineValue("-buildOutput") ?? DefaultOutputPath);
        }

        private static void BuildWebGL(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("WebGL output path is empty.", nameof(outputPath));
            }

            TimeRewindTestSceneBuilder.BuildScene();
            ConfigureGithubPagesPlayerSettings();

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
            }

            Directory.CreateDirectory(outputPath);

#pragma warning disable 618
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
#pragma warning restore 618

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
            }

            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);
            Debug.Log($"RetroTimers WebGL build completed at {outputPath}");
        }

        private static void ConfigureGithubPagesPlayerSettings()
        {
            PlayerSettings.companyName = "RetroTimers";
            PlayerSettings.productName = "RetroTimers";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
        }

        private static string GetCommandLineValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
