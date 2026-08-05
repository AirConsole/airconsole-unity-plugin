#if !DISABLE_AIRCONSOLE
namespace NDream.Unity {
    #region Imports
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;
    #endregion Imports

    /// <summary>
    /// CI-safe, headless build entry points for the AirConsole Unity plugin.
    ///
    /// Unlike <see cref="NDream.AirConsole.Editor.BuildHelper"/>, these methods:
    ///   - take NO parameters, so Unity's <c>-executeMethod</c> resolves them reliably,
    ///   - never mutate the git repository (no auto-commit), and
    ///   - never open the built player (no <c>BuildOptions.ShowBuiltPlayer</c>),
    /// which makes them safe to drive from the cross-repo build orchestrator
    /// (airconsole-platform/scripts/unity-stack.sh) and from CI.
    ///
    /// Project configuration validation (ProjectConfigurationCheck) and platform
    /// post-processing (Android manifest/gradle, WebGL JS generation) run
    /// automatically as build pre/post-process hooks during BuildPlayer.
    ///
    /// The output name defaults to "&lt;yyyyMMdd-HHmm&gt;-&lt;bundleId&gt;" and can be
    /// overridden with <c>-CustomArgs "buildName=&lt;name&gt;"</c>.
    ///
    /// Invoke:
    ///   Unity -batchmode -quit -nographics -projectPath . \
    ///     -buildTarget WebGL   -executeMethod NDream.Unity.Builder.BuildWebGL  -logFile -
    ///   Unity -batchmode -quit -nographics -projectPath . \
    ///     -buildTarget Android -executeMethod NDream.Unity.Builder.BuildAndroid -logFile -
    /// </summary>
    public static class Builder {
        private const string BasePath = "TestBuilds";

        public static void BuildWebGL() => Run(BuildTarget.WebGL, Path.Combine(BasePath, "Web"), isApk: false);

        public static void BuildAndroid() => Run(BuildTarget.Android, Path.Combine(BasePath, "Android"), isApk: true);

        private static void Run(BuildTarget target, string outputDirectory, bool isApk) {
            AssetDatabase.SaveAssets();

            string buildName = ArgValue("buildName")
                ?? $"{DateTime.Now:yyyyMMdd-HHmm}-{PlayerSettings.applicationIdentifier}";
            Directory.CreateDirectory(outputDirectory);
            string outputPath = isApk
                ? Path.Combine(outputDirectory, buildName + ".apk")
                : Path.Combine(outputDirectory, buildName);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0) {
                throw new BuildFailedException("No scenes are enabled in Build Settings.");
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None,
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded) {
                throw new BuildFailedException($"{target} build {summary.result}: {summary.totalErrors} error(s).");
            }

            Debug.Log($"[Builder] {target} build succeeded: {outputPath} ({summary.totalSize} bytes)");
        }

        /// <summary>Reads "key=value" or "-key value" from the process command line; null if absent.</summary>
        private static string ArgValue(string key) {
            string[] args = Environment.GetCommandLineArgs();
            string prefix = key + "=";
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-" + key && i + 1 < args.Length) {
                    return args[i + 1];
                }
                if (args[i].StartsWith(prefix, StringComparison.Ordinal)) {
                    return args[i].Substring(prefix.Length);
                }
            }
            return null;
        }
    }
}
#endif
