#if !DISABLE_AIRCONSOLE
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
    public class Packager
    {
        [MenuItem("Tools/AirConsole/Unlock Assemblies")]
        public static void UnlockAssemblies() {
            EditorApplication.UnlockReloadAssemblies();
        }
        
        [MenuItem("Tools/AirConsole/Package Plugin")]
        public static void Export()
        {
            Debug.ClearDeveloperConsole();
            string outputPath = Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{Settings.VERSION}.unitypackage"));
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
            File.Move(Path.Combine(webviewPackagePath, "unity-webview.asmdef"), Path.Combine(targetPath, "unity-webview.asmdef"));
            File.Move(Path.Combine(webviewPackagePath, "unity-webview.asmdef.meta"), Path.Combine(targetPath, "unity-webview.asmdef.meta"));
            AssetDatabase.Refresh();

            AssetDatabase.ExportPackage(new[] { "Assets/AirConsole", "Assets/Plugins", "Assets/WebGLTemplates" }, outputPath,
                ExportPackageOptions.Recurse);

            File.Move(Path.Combine(targetPath, "unity-webview.asmdef"), Path.Combine(webviewPackagePath, "unity-webview.asmdef"));
            File.Move(Path.Combine(targetPath, "unity-webview.asmdef.meta"), Path.Combine(webviewPackagePath, "unity-webview.asmdef.meta"));
            MoveSubDirectories(targetPath, webviewPackagePathAssets);
            DeleteAssetDatabaseDirectory(targetPath);
            AssetDatabase.Refresh();
            EditorApplication.UnlockReloadAssemblies();
            Debug.ClearDeveloperConsole();

            DeleteOldUnityPackages(outputPath, Settings.VERSION);

            ProcessStartInfo startInfo = new()
            {
                FileName = "git",
                Arguments = $"add {Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "airconsole-unity-plugin-v2.*"))}",
            };
            Process proc = new()
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

        private static void DeleteAssetDatabaseDirectory(string targetPath) {
            if (!Directory.Exists(targetPath)) {
                return;
            }

            File.Delete(targetPath + ".meta");
            Directory.Delete(targetPath);
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