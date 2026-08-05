#if !DISABLE_AIRCONSOLE
namespace NDream.Unity {
    #region Imports
    using System;
    using System.IO;
    using System.Linq;
    using NDream.AirConsole;
    using NDream.AirConsole.Editor;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    #endregion Imports

    /// <summary>
    /// CI-safe, headless build entry points for the AirConsole Unity plugin.
    ///
    /// Unlike <see cref="NDream.AirConsole.Editor.BuildHelper"/>, these methods:
    ///   - take NO parameters, so Unity's <c>-executeMethod</c> resolves them reliably,
    ///   - never auto-commit or push (the ProjectConfigurationCheck pre-build hook does
    ///     still rewrite ProjectSettings.asset, so expect a dirty tree after a build), and
    ///   - never open the built player (no <c>BuildOptions.ShowBuiltPlayer</c>), and
    ///   - for WebGL, open the first enabled build scene and regenerate the WebGL
    ///     template's controller.html from that scene's AirConsole.controllerHtml before
    ///     building, since the Play-Mode-triggered copy Extentions.OpenBrowser performs
    ///     never fires in batchmode,
    /// which makes them safe to drive from a headless build orchestrator and from CI.
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

        public static void BuildWebGL() {
            EnsureWebGLControllerTemplate();
            Run(BuildTarget.WebGL, Path.Combine(BasePath, "Web"), isApk: false);
        }

        public static void BuildAndroid() => Run(BuildTarget.Android, Path.Combine(BasePath, "Android"), isApk: true);

        /// <summary>
        /// Regenerates the WebGL template's controller.html from the first enabled build
        /// scene's AirConsole.controllerHtml. Unity's Play Mode hook (Extentions.OpenBrowser)
        /// normally does this copy, but that hook is triggered by entering Play Mode, which
        /// never happens in a batchmode build.
        /// </summary>
        private static void EnsureWebGLControllerTemplate() {
            string firstScene = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path;
            if (firstScene == null) {
                throw new BuildFailedException("No scenes are enabled in Build Settings.");
            }

            EditorSceneManager.OpenScene(firstScene, OpenSceneMode.Single);

            AirConsole controller = AirConsole.ACFindObjectOfType<AirConsole>();
            if (!Extentions.TryCopyControllerHtmlToTemplate(controller)) {
                throw new BuildFailedException(
                    "WebGL build requires an AirConsole component with a controllerHtml asset assigned "
                    + $"in the first enabled build scene ({firstScene}). Assign AirConsole.controllerHtml and re-run.");
            }
        }

        private static void Run(BuildTarget target, string outputDirectory, bool isApk) {
            // Some pre-build hooks read EditorUserBuildSettings.activeBuildTarget rather than the target
            // being built, so switch explicitly instead of relying on -buildTarget having been passed.
            if (EditorUserBuildSettings.activeBuildTarget != target) {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
            }

            AssetDatabase.SaveAssets();

            string requestedName = ArgValue("buildName");
            string buildName;
            if (string.IsNullOrEmpty(requestedName)) {
                buildName = $"{DateTime.Now:yyyyMMdd-HHmm}-{PlayerSettings.applicationIdentifier}";
            } else {
                // Strip any directory part so a caller supplied name cannot escape outputDirectory.
                buildName = Path.GetFileName(requestedName.TrimEnd('/', '\\'));
                if (string.IsNullOrEmpty(buildName) || buildName == "." || buildName == "..") {
                    throw new BuildFailedException($"Invalid buildName '{requestedName}'.");
                }
            }

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
