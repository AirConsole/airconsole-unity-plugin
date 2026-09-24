#if !DISABLE_AIRCONSOLE

namespace NDream.Unity {
    #region Imports
    using System;
    using System.Collections.Generic;
    using AirConsole;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    #endregion Imports

    public class Packager {
        [MenuItem("Tools/AirConsole/Unlock Assemblies")]
        public static void UnlockAssemblies() {
            EditorApplication.UnlockReloadAssemblies();
        }

        [MenuItem("Tools/AirConsole/Package Plugin")]
        public static void Export() {
            string outputPath = Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{Settings.VERSION}.unitypackage"));
            ExportPackage(outputPath);
            DeleteOldUnityPackages(outputPath, Settings.VERSION);
            StampChangelog();

            AddToGit(PackageGlob, ChangelogPath);

            OpenPath(outputPath);
        }

        [MenuItem("Tools/AirConsole/Package Plugin Release Candidate")]
        public static void ExportReleaseCandidate() {
            if (VerifyReleaseVersionExists(Settings.VERSION)) {
                EditorUtility.DisplayDialog("Package Error",
                    $"Version {Settings.VERSION} already exists.\nYou can not create a release candidate for it",
                    "OK");
                AirConsoleLogger.LogError(() => $"Exporting unitypackage for release version {Settings.VERSION} not allowed.");
                return;
            }

            int rcVersion = GetNextReleaseCandidateVersion(Settings.VERSION);
            string outputPath =
                Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{Settings.VERSION}-rc{rcVersion}.unitypackage"));
            ExportPackage(outputPath);
            AddToGit(PackageGlob);
            OpenPath(outputPath);
        }

        private static void RemoveControllersFromWebGlTemplates() => Directory
            .GetFiles(Path.Combine(Application.dataPath, "WebGLTemplates"), "controller.html", SearchOption.AllDirectories)
            .ToList()
            .ForEach(File.Delete);

        private static void RemoveAirConsolePreferences() =>
            File.Delete(Path.Combine(Application.dataPath, "AirConsole", "airconsole.prefs"));

        private static bool VerifyReleaseVersionExists(string version) =>
            File.Exists(Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{version}.unitypackage")));

        private static int GetNextReleaseCandidateVersion(string version) {
            string[] files = Directory.GetFiles(Path.GetFullPath("Builds"),
                $"airconsole-unity-plugin-v{version}-rc*.unitypackage");
            Regex extractVersion = new(@"airconsole-unity-plugin-v.*-rc([0-9]+)\.unitypackage");
            int maxVersion = 0;
            foreach (string file in files) {
                Match match = extractVersion.Match(Path.GetFileName(file));
                if (match.Success) {
                    int v = int.Parse(match.Groups[1].Value);
                    if (v > maxVersion) {
                        maxVersion = v;
                    }
                }
            }
            return maxVersion + 1;
        }

        private static void ExportPackage(string outputPath) {
            Debug.ClearDeveloperConsole();
            AirConsoleLogger.Log(() => $"Exporting to {outputPath}");

            RemoveControllersFromWebGlTemplates();
            RemoveAirConsolePreferences();
            AssetDatabase.Refresh();

            string packagePath = PackageCode();
            AssetDatabase.Refresh();
            CollectPackageInclusionPaths(packagePath, out IEnumerable<string> packageInclusionPaths);
            AssetDatabase.ExportPackage(packageInclusionPaths.ToArray(), outputPath, ExportPackageOptions.Recurse);

            CleanupCodePackage();
            
            Debug.ClearDeveloperConsole();
        }

        /// <summary>
        /// Collects the file and directory paths to be included in the AirConsole Unity package.
        /// Excludes certain subdirectories and files from the package.
        /// </summary>
        /// <param name="packagePath">The main package directory to include.</param>
        /// <param name="airconsoleInclusions">
        /// Output: An enumerable of asset paths to include in the unity package.
        /// </param>
        private static void CollectPackageInclusionPaths(string packagePath, out IEnumerable<string> airconsoleInclusions) {
            airconsoleInclusions = Directory.GetDirectories(Path.Combine(Application.dataPath, "AirConsole"))
                .Where(it => !it.ToLower().Contains("scripts")
                             && !it.ToLower().Contains("unity-webview")
                             && !it.ToLower().Contains("examples"))
                .Select(it => it.Replace(Application.dataPath, "Assets"));
            airconsoleInclusions = airconsoleInclusions.Append(packagePath);
            airconsoleInclusions = airconsoleInclusions.Append($"Assets/AirConsole/{nameof(ProjectCodeUpdater)}.cs");
            airconsoleInclusions = airconsoleInclusions.Append($"Assets/AirConsole/scripts/SupportCheck");
            CollectWebGlTemplateFiles("Assets/WebGLTemplates/AirConsole-2020", ref airconsoleInclusions);
            CollectWebGlTemplateFiles("Assets/WebGLTemplates/AirConsole-U6", ref airconsoleInclusions);
        }

        /// <summary>
        /// Recursively collects all files for a given WebGL template, excluding files that need to be ignored and appends them to the
        /// provided <paramref name="airconsoleInclusions"/> collection.
        /// </summary>
        /// <param name="path">The root directory path to search for files.</param>
        /// <param name="airconsoleInclusions">
        /// A reference to an <see cref="IEnumerable{string}"/> collection to which found file paths will be appended.
        /// </param>
        private static void CollectWebGlTemplateFiles(string path, ref IEnumerable<string> airconsoleInclusions) {
            string[] files = Directory.GetFiles(path);
            foreach (string file in files) {
                if (!file.Contains("airconsole-settings.js") && !file.Contains(".DS_Store")) {
                    airconsoleInclusions = airconsoleInclusions.Append(file);
                }
            }

            foreach (string directory in Directory.GetDirectories(path)) {
                CollectWebGlTemplateFiles(directory, ref airconsoleInclusions);
            }
        }
        

        private static string PackageCode() {
            string unityPackagePath = ProjectCodeUpdater.CodePackagePath;

            AssetDatabase.ExportPackage(
                new[] { "Assets/AirConsole/scripts", "Assets/AirConsole/unity-webview", "Assets/AirConsole/examples" },
                unityPackagePath,
                ExportPackageOptions.Recurse);
            return unityPackagePath.Replace(Application.dataPath, "Assets");
        }

        private static void CleanupCodePackage() {
            string unityPackagePath = ProjectCodeUpdater.CodePackagePath;

            if (!File.Exists(unityPackagePath)) {
                return;
            }

            // DeleteAsset, not File.Delete: PackageCode writes this package inside Assets/ and the
            // following Refresh imports it, so Unity owns a .meta for it. File.Delete removes only
            // the package and leaves airconsole-code.unitypackage.meta orphaned in the repository.
            string assetPath = unityPackagePath.Replace(Application.dataPath, "Assets");
            if (!AssetDatabase.DeleteAsset(assetPath)) {
                File.Delete(unityPackagePath);
                AirConsoleLogger.LogWarning(() =>
                    $"Could not delete {assetPath} through the AssetDatabase; removed the file directly. "
                    + "Check for a leftover .meta.");
            }
        }

        private static void OpenPath(string outputPath) {
            Application.OpenURL("file://" + Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", outputPath)));
        }

        private static string PackageGlob =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "airconsole-unity-plugin-v*"));

        private static string ChangelogPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CHANGELOG.md"));

        /// <summary>
        /// Renames the "## [Unreleased]" heading of CHANGELOG.md to "## [VERSION] - yyyy-MM-dd" and adds a new, empty
        /// "## [Unreleased]" section with "### Added" above it for the next version.
        /// scripts/release.py reads the dated section as the release notes.
        /// </summary>
        private static void StampChangelog() {
            string changelog = File.ReadAllText(ChangelogPath);
            if (changelog.Contains($"## [{Settings.VERSION}]")) {
                return;
            }

            Regex unreleased = new(@"^## \[Unreleased\]", RegexOptions.Multiline);
            if (!unreleased.IsMatch(changelog)) {
                AirConsoleLogger.LogError(() => $"CHANGELOG.md has no '## [Unreleased]' heading to release as {Settings.VERSION}.");
                return;
            }

            string released = $"## [Unreleased]\n\n### Added\n\n## [{Settings.VERSION}] - {DateTime.Now:yyyy-MM-dd}";
            File.WriteAllText(ChangelogPath, unreleased.Replace(changelog, released, 1));
        }

        private static void AddToGit(params string[] paths) {
            ProcessStartInfo startInfo = new() {
                FileName = "git",
                Arguments = "add " + string.Join(" ", paths)
            };
            Process proc = new() {
                StartInfo = startInfo
            };
            if (proc.Start()) {
                proc.WaitForExit();
            } else {
                AirConsoleLogger.LogError(() => "Failed to add package to git");
            }
        }

        private static void DeleteAssetDatabaseDirectory(string targetPath) {
            if (!Directory.Exists(targetPath)) {
                return;
            }

            File.Delete(targetPath + ".meta");
            Directory.Delete(targetPath);
        }

        private static void DeleteOldUnityPackages(string outputPath, string newVersion) {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(outputPath), "airconsole-unity-plugin-*.*");
            Regex releaseVersionRegex = new($"airconsole-unity-plugin-v{newVersion}.unitypackage");
            foreach (string file in files) {
                if (!releaseVersionRegex.IsMatch(file)) {
                    File.Delete(file);
                }
            }
        }

        private static void MoveSubDirectories(string sourcePath, string targetPath) {
            if (!Directory.Exists(targetPath)) {
                Directory.CreateDirectory(targetPath);
            }

            IEnumerable<string> sources = Directory.GetDirectories(sourcePath);
            sources = sources.Concat(Directory.GetFiles(sourcePath));
            foreach (string source in sources) {
                string target = Path.Combine(targetPath, Path.GetFileName(source));
                Directory.Move(source, target);
            }
        }
    }
}
#endif
