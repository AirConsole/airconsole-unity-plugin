#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Editor {
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    internal abstract class AndroidGradleProcessor {
        private const string PROGUARD_CLASSMEMBERS
            = "-keepclasseswithmembers class com.airconsole.unityandroidlibrary.** {*;}";

        internal static void Execute(string basePath) {
            UpdateMainGradleProperties(Path.GetFullPath(Path.Combine(basePath, "..")), "gradle.properties");

            UpdateLibraryGradleTemplate(Path.GetFullPath(basePath), "build.gradle");
            UpdateProGuard(Path.GetFullPath(basePath), "proguard-unity.txt");
            AirConsoleLogger.LogDevelopment(() => "Updated gradle files for AirConsole Android build");
        }

        private static void UpdateMainGradleTemplate(string basePath, string buildGradle) {
            string[] lines = File.ReadAllLines(Path.Combine(basePath, buildGradle));

            Regex applicationVersionExtractor
                = new(@"id 'com.android.application' version '(?<Major>\d+)\.(?<Minor>\d+)\.(?<Build>\d+)' apply false");
            Regex libraryVersionVersionExtractor
                = new(@"id 'com.android.library' version '(?<Major>\d+)\.(?<Minor>\d+)\.(?<Build>\d+)' apply false");
            bool updated = false;
            for (int i = 0; i < lines.Length; i++) {
                // regex to check the used gradle version
                Match match = applicationVersionExtractor.Match(lines[i]);
                if (match.Success) {
                    if (int.Parse(match.Groups["Major"].Value) < 8
                        || int.Parse(match.Groups["Minor"].Value) < 1
                        || int.Parse(match.Groups["Build"].Value) < 1) {
                        lines[i] = "id 'com.android.application' version '8.1.1' apply false";
                        updated = true;
                    }
                } else {
                    match = libraryVersionVersionExtractor.Match(lines[i]);
                    if (match.Success) {
                        if (int.Parse(match.Groups["Major"].Value) < 8
                            || int.Parse(match.Groups["Minor"].Value) < 1
                            || int.Parse(match.Groups["Build"].Value) < 1) {
                            lines[i] = "id 'com.android.library' version '8.1.1' apply false";
                            updated = true;
                        }
                    }
                }
            }

            if (updated) {
                File.WriteAllLines(Path.Combine(basePath, buildGradle), lines);
                AirConsoleLogger.LogDevelopment(() => "Updated gradle main template");
            }
        }

        private static void UpdateGradleWrapperVersion(string basePath, string gradleWrapperProperties) {
            string[] lines = File.ReadAllLines(Path.Combine(basePath, gradleWrapperProperties));
            Regex versionExtractor
                = new(@"^distributionUrl=.*/gradle-(?<Major>\d+)\.(?<Minor>\d+)\.(?<Build>\d+)-bin.zip$");
            for (int i = 0; i < lines.Length; i++) {
                // regex to check the used gradle version
                Match match = versionExtractor.Match(lines[i]);
                if (lines[i].StartsWith("distributionUrl=") && match.Success
                    && (int.Parse(match.Groups["Major"].Value) < 8
                        || int.Parse(match.Groups["Minor"].Value) < 1
                        || int.Parse(match.Groups["Build"].Value) < 1)) {
                    lines[i] = "distributionUrl=https\\://services.gradle.org/distributions/gradle-8.1.1-bin.zip";
                    File.WriteAllLines(Path.Combine(basePath, gradleWrapperProperties), lines);
                    AirConsoleLogger.LogDevelopment(() => $"Updated gradle wrapper to {lines[i]}");
                    return;
                }
            }
        }

        private static void UpdateProGuard(string basePath, string proguardUnityTxt) {
            string filePath = Path.Combine(basePath, proguardUnityTxt);
            string fileText = File.ReadAllText(filePath);

            if (!fileText.Contains(PROGUARD_CLASSMEMBERS)) {
                fileText += $"\n{PROGUARD_CLASSMEMBERS}";
            }

            File.WriteAllText(filePath, fileText);
        }

        private static void UpdateLibraryGradleTemplate(string basePath, string gradleTemplateName) {
            string gradleTemplatePath = Path.Combine(basePath, gradleTemplateName);
            string[] initialLines = File.ReadAllText(gradleTemplatePath)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split(new[] { '\n' });

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

            if (!lines.Contains("android.suppressUnsupportedCompileSdk")) {
                lines += "\nandroid.suppressUnsupportedCompileSdk=35,34";
            }

            if (lines != initialLines) {
                File.WriteAllText(gradlePropertiesPath, lines);
                AirConsoleLogger.LogDevelopment(() =>
                    $"Gradle templates updated from {initialLines} to {lines} for {gradlePropertiesPath}");
            } else {
                AirConsoleLogger.LogDevelopment(() =>
                    $"Gradle properties were {initialLines}, no update for {gradlePropertiesPath}");
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
