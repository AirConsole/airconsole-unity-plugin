#if !DISABLE_AIRCONSOLE
#region

using System.Collections.Generic;
using NDream.AirConsole;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#endregion

namespace NDream.Unity
{
    public class Packager
    {
        [MenuItem("Tools/AirConsole/Unlock Assemblies")]
        public static void UnlockAssemblies() {
            EditorApplication.UnlockReloadAssemblies();
        }

        [MenuItem("Tools/AirConsole/Package Plugin")]
        public static void Export() {
            string outputPath = Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{Settings.VERSION}.unitypackage"));
            ExportPackage(outputPath);
            DeleteOldUnityPackages(outputPath, Settings.VERSION);

            AddPackageToGit();

            OpenPath(outputPath);
        }

        [MenuItem("Tools/AirConsole/Package Plugin Release Candidate")]
        public static void ExportReleaseCandidate() {
            if (VerifyReleaseVersionExists(Settings.VERSION)) {
                EditorUtility.DisplayDialog("Package Error",
                    $"Version {Settings.VERSION} already exists.\nYou can not create a release candidate for it",
                    "OK");
                AirConsoleLogger.LogError($"Exporting unitypackage for release version {Settings.VERSION} not allowed.");
                return;
            }

            int rcVersion = GetNextReleaseCandidateVersion(Settings.VERSION);
            string outputPath =
                Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{Settings.VERSION}-rc{rcVersion}.unitypackage"));
            ExportPackage(outputPath);
            AddPackageToGit();
            OpenPath(outputPath);
        }

        private static void RemoveControllersFromWebGlTemplates() => Directory
            .GetFiles(Path.Combine(Application.dataPath, "WebGlTemplates"), "controller.html", SearchOption.AllDirectories)
            .ToList()
            .ForEach(File.Delete);

        private static void RemoveAirConsolePreferences() =>
            File.Delete(Path.Combine(Application.dataPath, "AirConsole", "airconsole.prefs"));
        
        private static bool VerifyReleaseVersionExists(string version) =>
            File.Exists(Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{version}.unitypackage")));

        private static int GetNextReleaseCandidateVersion(string version) {
            string[] files = Directory.GetFiles(Path.GetFullPath("Builds"),
                $"airconsole-unity-plugin-v{version}-rc*.unitypackage");
            Regex extractVersion = new($"airconsole-unity-plugin-v.*-rc([0-9]+).unitypackage");
            string fileName = files.LastOrDefault();
            if (File.Exists(fileName)) {
                return int.Parse(extractVersion.Match(fileName).Groups[1].Value) + 1;
            }

            return 1;
        }

        private static void ExportPackage(string outputPath) {
            Debug.ClearDeveloperConsole();
            Debug.Log($"Exporting to {outputPath}");

            string packageCache = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));
            string webviewPackagePath =
                Directory.GetDirectories(packageCache).FirstOrDefault(d => d.Contains("com.airconsole.unity-webview"));

            if(!Directory.Exists(webviewPackagePath))
            {
                EditorUtility.DisplayDialog("Error", "Can not find airconsole webview package", "OK");
                Debug.LogError("Can not find airconsole webview package");
                return;
            }

            string webviewPackagePathAssets = Path.Combine(webviewPackagePath, "Assets");

            string targetPath = Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "unity-webview")); 
            DeleteAssetDatabaseDirectory(targetPath); 
            AssetDatabase.Refresh();
            
            EditorApplication.LockReloadAssemblies();

            MoveSubDirectories(webviewPackagePathAssets, targetPath);
            RemoveControllersFromWebGlTemplates();
            RemoveAirConsolePreferences();
            // TODO(2.6.0): Remove these 2 lines when unity-webview 1.1.7 is ready
            File.Move(Path.Combine(webviewPackagePath, "unity-webview.asmdef"), Path.Combine(targetPath, "unity-webview.asmdef"));
            File.Move(Path.Combine(webviewPackagePath, "unity-webview.asmdef.meta"), Path.Combine(targetPath, "unity-webview.asmdef.meta"));
            AssetDatabase.Refresh();

            string packagePath = PackageCode();
            IEnumerable<string> airconsoleDirectories = Directory.GetDirectories(Path.Combine(Application.dataPath, "AirConsole"))
                .Where(it => !it.ToLower().Contains("scripts") && !it.ToLower().Contains("unity-webview"))
                .Select(it => it.Replace(Application.dataPath, "Assets"));
            airconsoleDirectories = airconsoleDirectories.Append(packagePath);
            airconsoleDirectories = airconsoleDirectories.Append("Assets/WebGLTemplates");

            AssetDatabase.ExportPackage(airconsoleDirectories.ToArray(),
                // new[] { 
                //     "Assets/AirConsole/examples", "Assets/AirConsole/extras", "Assets/AirConsole", "Assets/Plugins", "Assets/WebGLTemplates"
                // }, 
                outputPath, ExportPackageOptions.Recurse);

            // TODO(2.6.0): Remove these 2 lines when unity-webview 1.1.7 is ready
            File.Move(Path.Combine(targetPath, "unity-webview.asmdef"), Path.Combine(webviewPackagePath, "unity-webview.asmdef"));
            File.Move(Path.Combine(targetPath, "unity-webview.asmdef.meta"), Path.Combine(webviewPackagePath, "unity-webview.asmdef.meta"));
            MoveSubDirectories(targetPath, webviewPackagePathAssets);
            DeleteAssetDatabaseDirectory(targetPath);
            AssetDatabase.Refresh();
            EditorApplication.UnlockReloadAssemblies();
            Debug.ClearDeveloperConsole();

        }

        /// <summary>
        /// Packages the script and unity-webview directory
        /// </summary>
        /// <returns>the path to the unity package</returns>
        private static string PackageCode() {
            string result = Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "airconsole-sourcecode.unitypackage"));
            AssetDatabase.ExportPackage(new[] { "Assets/AirConsole/scripts", "Assets/Airconsole/unity-webview" },
                result,
                ExportPackageOptions.Recurse);
            return result.Replace(Application.dataPath, "Assets");
        }
        
        private static void OpenPath(string outputPath) {
            Application.OpenURL("file://" + Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", outputPath)));
        }

        private static void AddPackageToGit() {
            ProcessStartInfo startInfo = new() {
                FileName = "git",
                Arguments = $"add {Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "airconsole-unity-plugin-v*"))}"
            };
            Process proc = new() {
                StartInfo = startInfo
            };
            if (proc.Start()) {
                proc.WaitForExit();
            } else {
                Debug.LogError("Failed to add package to git");
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
            Debug.Log($"Files to copy:\n{string.Join("\n", sources)}");
            foreach (string source in sources) {
                string target = Path.Combine(targetPath, Path.GetFileName(source));
                Directory.Move(source, target);
            }
        }
    }
}
#endif