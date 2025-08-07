#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Editor {
    using UnityEditor;
    using UnityEditor.Callbacks;
    using System.IO;
    using System;
    using System.Text.RegularExpressions;
    using UnityEngine;

    public class PostBuildProcess {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
            if (target == BuildTarget.WebGL) {
                ValidateApiUsage();

                // Copy index.html to screen.html and overwrite it if necessary.
                File.Copy(pathToBuiltProject + "/index.html", pathToBuiltProject + "/screen.html", true);

                // Save last port path
                EditorPrefs.SetString("airconsolePortPath", pathToBuiltProject);

            }
        }

        private static void ValidateApiUsage() {
            string webGLTemplateDirectory = PreBuildProcessing.GetWebGLTemplateDirectory();
            if (!VerifyAPIUsage(Path.Combine(webGLTemplateDirectory, "index.html"), Settings.RequiredMinimumVersion)
                // || !VerifyAPIUsage(pathToBuiltProject + "/screen.html", Settings.RequiredMinimumVersion)
                || !VerifyAPIUsage(Path.Combine(webGLTemplateDirectory, "controller.html"), Settings.RequiredMinimumVersion)) {
                AirConsoleLogger.LogError(() => "Outdated AirConsole API detected. Please check the previous logs to address the problem.");
                throw new UnityException("Build failed. Outdated AirConsole API detected");
            }
        }

        private static bool VerifyAPIUsage(string pathToHtml, Version requiredApiVersion) {
            // Using regex, check if the reference to airconsole-Major.Minor.Patch is at least as big as requiredMinimumVersion. Also ensure that it is not airconsole-latest.js
            string reference = File.ReadAllText(pathToHtml);
            Regex regex = new(@"airconsole-(\d+)\.(\d+)\.(\d+)\.js", RegexOptions.IgnoreCase);
            Match match = regex.Match(reference);
            string apiVersion = $"airconsole-{requiredApiVersion.Major}.{requiredApiVersion.Minor}.{requiredApiVersion.Build}.js";
            if (match.Success && !reference.Contains("airconsole-latest.js")) {
                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);
                int revision = int.Parse(match.Groups[3].Value);

                Version referencedVersion = new(major, minor, revision);
                if (referencedVersion >= requiredApiVersion) {
                    AirConsoleLogger.LogDevelopment(() => $"Valid API reference {match.Groups[0]} found.");
                } else {
                    AirConsoleLogger.LogError(
                        () => $"airconsole-{major}.{minor}.{revision}.js found. This is outdated, please update to {apiVersion}");
                    return false;
                }
            } else if (reference.Contains("airconsole-latest.js")) {
                AirConsoleLogger.LogError(
                    () => $"{pathToHtml} uses airconsole-latest.js. Please fix it to use {apiVersion}");

                return false;
            }

            return true;
        }
    }
}
#endif