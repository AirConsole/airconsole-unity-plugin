#region

using System.Collections.Generic;
using NDream.AirConsole;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#endregion

namespace NDream.Unity
{
    public static class Packager
    {

        [MenuItem("Tools/AirConsole/Marc is a Monkey")]
        public static void FixMarcIsAMonkey() {
            EditorApplication.UnlockReloadAssemblies();
            
            string packageCache = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));
            string webviewPackagePath = Directory.GetDirectories(packageCache).FirstOrDefault(d => d.Contains("com.airconsole.unity-webview"));
            webviewPackagePath = Path.Combine(webviewPackagePath, "Assets");
            
            string targetPath = Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "unity-webview"));
            
            MoveSubDirectories(webviewPackagePath, targetPath);
        }
        
        [MenuItem("Tools/AirConsole/Package Plugin")]
        public static void Export()
        {
            Debug.ClearDeveloperConsole();
            string outputPath = Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{Settings.VERSION}.unitypackage"));
            Debug.Log($"Exporting to {outputPath}");

            string packageCache = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));
            string webviewPackagePath = Directory.GetDirectories(packageCache).FirstOrDefault(d => d.Contains("com.airconsole.unity-webview"));

            if(!Directory.Exists(webviewPackagePath))
            {
                EditorUtility.DisplayDialog("Error", "Can not find airconsole webview package", "OK");
                Debug.LogError("Can not find airconsole webview package");
                return;
            }
            
            string webviewPackagePathAssets = Path.Combine(webviewPackagePath, "Assets");
            
            EditorApplication.LockReloadAssemblies();
            string targetPath = Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "unity-webview"));
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            
            MoveSubDirectories(webviewPackagePathAssets, targetPath);
            List<string> filesToDelete = new();
            CopyFiles(webviewPackagePath, targetPath, filesToDelete);
            // Directory.Move(webviewPackagePath, targetPath);
            AssetDatabase.Refresh();
            
            AssetDatabase.ExportPackage(new[] { "Assets/AirConsole", "Assets/Plugins", "Assets/WebGLTemplates" },
                                        outputPath, ExportPackageOptions.Recurse);
          
            foreach (string file in filesToDelete) {
                File.Delete(file);
            }
            MoveSubDirectories(targetPath,webviewPackagePathAssets);
            // Directory.Move(targetPath, webviewPackagePath);
            DeleteUnityProjectDirectory(targetPath);
            AssetDatabase.Refresh();
            EditorApplication.UnlockReloadAssemblies();
            Debug.ClearDeveloperConsole();

            DeleteOldUnityPackages(outputPath, Settings.VERSION);
            
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "git",
                Arguments = $"add {Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "airconsole-unity-plugin-v2.*"))}",
            };
            Process proc = new Process()
            {
                StartInfo = startInfo,
            };
            if(proc.Start()) {
                proc.WaitForExit();
            }
            else {
                Debug.LogError("Failed to add package to git");
            }

            Application.OpenURL("file://" + Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", outputPath)));
        }

        private static void DeleteUnityProjectDirectory(string targetPath) {
            if (!Directory.Exists(targetPath)) {
                return;
            }

            Directory.Delete(targetPath, true);
            File.Delete(targetPath + ".meta");
        }

        private static void DeleteOldUnityPackages(string outputPath, string newVersion) {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(outputPath), "airconsole-unity-plugin-*.*");
            foreach (string file in files) {
                if (!file.Contains(newVersion)) {
                    File.Delete(file);
                }
            }
        }

        private static void MoveSubDirectories(string sourcePath, string targetPath) {
            IEnumerable<string> sources = Directory.GetDirectories(sourcePath);
            sources = sources.Concat(Directory.GetFiles(sourcePath));
            Debug.Log($"Files to copy:\n{string.Join("\n", sources)}");
            foreach (string source in sources) {
                string target = Path.Combine(targetPath, Path.GetFileName(source));
                Directory.Move(source, target);
            }
        }

        private static void CopyFiles(string sourcePath, string targetPath, List<string> copiedFiles, string filter = "*.asmdef*") {
            IEnumerable<string> sources = Directory.GetFiles(sourcePath, filter);
            Debug.Log($"Files to copy:\n{string.Join("\n", sources)}");
            foreach (string source in sources) {
                string target = Path.Combine(targetPath, Path.GetFileName(source));
                File.Copy(source, target, true);
                copiedFiles.Add(target);
            }
        }
    }
}