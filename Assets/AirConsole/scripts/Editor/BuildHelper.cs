using System.Diagnostics;

namespace NDream.AirConsole.Editor {
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    public static class BuildHelper {
        private const string BasePath = "TestBuilds";

        [MenuItem("Tools/AirConsole/Build Web")]
        public static void BuildWeb() {
            ProjectConfigurationCheck.CheckSettings(BuildTarget.WebGL);
            AssetDatabase.SaveAssets();
            if (CommitPendingChanges(out string timestamp, out string commitHash)) {
                return;
            }

            string bundleId = PlayerSettings.applicationIdentifier;
            string buildName = $"{timestamp}-{bundleId}-{commitHash}";
            string outputDirectory = Path.Combine(BasePath, "Web");
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, buildName);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0) {
                Debug.LogError("No scenes are enabled in Build Settings!");
                return;
            }

            BuildForPlatform(scenes, outputPath, BuildTarget.WebGL);

#if UNITY_EDITOR_OSX
            ZipFolder(outputPath);
#endif
        }

        [MenuItem("Tools/AirConsole/Build Android")]
        public static void BuildAndroid() {
            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);
            AssetDatabase.SaveAssets();
            if (CommitPendingChanges(out string timestamp, out string commitHash)) {
                return;
            }

            string bundleId = PlayerSettings.applicationIdentifier;
            string buildName = $"{timestamp}-{bundleId}-{commitHash}";
            string outputDirectory = Path.Combine(BasePath, "Android");
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, buildName + ".apk");

            // 7. Get enabled scenes from EditorBuildSettings
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0) {
                Debug.LogError("No scenes are enabled in Build Settings!");
                return;
            }

            BuildForPlatform(scenes, outputPath, BuildTarget.Android);
        }

        /// <summary>
        /// Determines whether there are any uncommitted changes in the repository.
        /// </summary>
        /// <returns>True if there are changes, false otherwise.</returns>
        private static bool HasUncommittedChanges() {
            try {
                ProcessStartInfo startInfo = new() {
                    FileName = "git",
                    Arguments = "diff-index --quiet HEAD --",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                using (Process process = Process.Start(startInfo)) {
                    process.WaitForExit();
                    return process.ExitCode != 0;
                }
            } catch (Exception ex) {
                Debug.LogError("Error checking for uncommitted changes: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Runs a git command and returns its output as a string.
        /// </summary>
        /// <param name="arguments">The git command arguments.</param>
        /// <returns>The output of the command.</returns>
        private static string RunGitCommand(string arguments) {
            try {
                ProcessStartInfo startInfo = new() {
                    FileName = "git",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                using (Process process = Process.Start(startInfo)) {
                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(error)) {
                        throw new ApplicationException($"Git error: {error}");
                    }

                    return output;
                }
            } catch (Exception ex) {
                Debug.LogError($"Exception when running git command: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds the project for the specified platform.
        /// </summary>
        /// <param name="scenes">Array of scenes to include in the build.</param>
        /// <param name="outputPath">The output path for the built project.</param>
        /// <param name="target">The target platform for the build.</param>
        private static void BuildForPlatform(string[] scenes, string outputPath, BuildTarget target) {
            BuildPlayerOptions buildPlayerOptions = new() {
                scenes = scenes,
                locationPathName = outputPath,
                target = target, options = BuildOptions.ShowBuiltPlayer
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            switch (summary.result) {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded: {summary.totalSize} bytes\nBuild Path: {outputPath}");
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed.");
                    break;
            }
        }

        /// <summary>
        /// Zips the specified folder into a file named "<foldername>.zip" placed next to the folder.
        /// This function uses the OS X 'zip' command.
        /// </summary>
        /// <param name="folderPath">The full path to the folder to zip.</param>
        /// <returns>True if the zip operation was successful.</returns>
        private static bool ZipFolder(string folderPath) {
            if (!Directory.Exists(folderPath)) {
                Debug.LogError("Folder does not exist: " + folderPath);
                return false;
            }

            string parentDir = Path.GetDirectoryName(folderPath);
            string folderName = new DirectoryInfo(folderPath).Name;
            string zipFilePath = folderName + ".zip";

            if (File.Exists(zipFilePath)) {
                File.Delete(zipFilePath);
            }

            Process process = new();
            process.StartInfo.FileName = "zip";
            process.StartInfo.Arguments = $"-r \"{zipFilePath}\" \"{folderName}/\"";
            process.StartInfo.WorkingDirectory = parentDir;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            try {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0) {
                    string errorOutput = process.StandardError.ReadToEnd();
                    Debug.LogError("Error zipping folder: " + errorOutput);
                    return false;
                }

                return true;
            } catch (Exception ex) {
                Debug.LogError("Exception during zipping: " + ex.Message);
                return false;
            }
        }

        private static bool CommitPendingChanges(out string timestamp, out string commitHash) {
            timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");

            if (HasUncommittedChanges()) {
                string commitResult = RunGitCommand($"commit -am \"build: {timestamp}\"");
                Debug.Log("Git commit output: " + commitResult);
            }
            // else
            // {
            //     string tagName = $"build-{timestamp}";
            //     string tagResult = RunGitCommand($"tag -a \"{tagName}\" -m \"build: {timestamp}\"");
            //     Debug.Log("Git tag output: " + tagResult);
            // }

            commitHash = RunGitCommand("rev-parse --short HEAD").Trim();
            if (!string.IsNullOrEmpty(commitHash)) {
                return false;
            }

            Debug.LogError("Failed to retrieve git commit hash.");
            return true;
        }
    }
}