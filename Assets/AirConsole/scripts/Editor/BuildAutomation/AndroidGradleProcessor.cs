#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Editor {
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal abstract class AndroidGradleProcessor {
        private const string PROGUARD_CLASSMEMBERS = "-keepclasseswithmembers class com.airconsole.unityandroidlibrary.** {*;}";

        internal static void Execute(string basePath) {
            UpdateMainGradleProperties(Path.GetFullPath(Path.Combine(basePath, "..")), "gradle.properties");
            UpdateMainGradleTemplate(Path.GetFullPath(basePath), "build.gradle");
            UpdateProGuard(Path.GetFullPath(basePath), "proguard-unity.txt");
            AirConsoleLogger.LogDevelopment(() => "Updated gradle files for AirConsole Android build");
        }

        private static void UpdateProGuard(string basePath, string proguardUnityTxt) {
            string filePath = Path.Combine(basePath, proguardUnityTxt);
            string fileText = File.ReadAllText(filePath);

            if (!fileText.Contains(PROGUARD_CLASSMEMBERS)) {
                fileText += $"\n{PROGUARD_CLASSMEMBERS}";
            }

            File.WriteAllText(filePath, fileText);
        }

        private static void UpdateMainGradleTemplate(string basePath, string gradleTemplateName) {
            string gradleTemplatePath = Path.Combine(basePath, gradleTemplateName);
            string[] initialLines = File.ReadAllText(gradleTemplatePath).Replace("\r\n", "\n").Replace("\r", "\n").Split(new[] { '\n' });

            List<string> lines = new();
            bool inDependencies = false;
            foreach (string line in initialLines) {
                if (line == "dependencies {") {
                    inDependencies = true;
                } else if (inDependencies && line.Contains("}")) {
                    inDependencies = false;
                    AddImplementationLineIfNotPresent(initialLines, "com.android.volley:volley:1.2.1", lines);
                    AddImplementationLineIfNotPresent(initialLines, "androidx.appcompat:appcompat:1.6.1", lines);
                    AddImplementationLineIfNotPresent(initialLines, "androidx.security:security-crypto:1.0.0", lines);
                }

                lines.Add(line);
            }

            if (lines.Count > initialLines.Length) {
                File.WriteAllText(gradleTemplatePath, string.Join("\n", lines) + "\n");
                AirConsoleLogger.LogDevelopment(() =>
                    $"Gradle templates updated from {string.Join("\n", initialLines)} to {string.Join("\n", lines)} for {gradleTemplatePath}");
            } else {
                AirConsoleLogger.LogDevelopment(() =>
                    $"Gradle main template was {string.Join("\n", initialLines)}, no update for {gradleTemplatePath}");
            }
        }

        private static void UpdateMainGradleProperties(string basePath, string gradleProperties) {
            string gradlePropertiesPath = Path.Combine(basePath, gradleProperties);
            string initialLines = "";
            string lines = "";
            if (File.Exists(gradlePropertiesPath)) {
                initialLines = File.ReadAllText(gradlePropertiesPath).Replace("\r\n", "\n").Replace("\r", "\n") + "\n";
                lines = initialLines;
            }

            if (!lines.Contains("android.useAndroidX=true")) {
                lines = "# AirConsole\nandroid.useAndroidX=true\n\n# Unity provided\n" + lines;
            }

            if (lines != initialLines) {
                File.WriteAllText(gradlePropertiesPath, lines);
                AirConsoleLogger.LogDevelopment(() =>
                    $"Gradle templates updated from {initialLines} to {lines} for {gradlePropertiesPath}");
            } else {
                AirConsoleLogger.LogDevelopment(() => $"Gradle properties were {initialLines}, no update for {gradlePropertiesPath}");
            }
        }

        private static void AddImplementationLineIfNotPresent(string[] lines, string line, List<string> newLines) {
            if (!lines.Any(it => it.Contains(line))) {
                newLines.Add($"    implementation '{line}'");
            }
        }
    }
}

#endif