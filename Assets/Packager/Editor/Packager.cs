#region
using NDream.AirConsole;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Debug = UnityEngine.Debug;
#endregion

namespace NDream.Unity
{
    public class Packager
    {
        [MenuItem("Tools/AirConsole/Package Plugin")]
        public static void Export()
        {
            string outputPath = Path.GetFullPath(Path.Combine("Builds", $"airconsole-unity-plugin-v{Settings.VERSION}.unitypackage"));
            Debug.Log($"Exporting to {outputPath}");

            string packageCache = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));
            string webviewPackagePath = Directory.GetDirectories(packageCache).FirstOrDefault(d => d.Contains("com.airconsole.webview"));

            if(!Directory.Exists(webviewPackagePath))
            {
                EditorUtility.DisplayDialog("Error", "Can not find airconsole webview package", "OK");
                Debug.LogError("Can not find airconsole webview package");
                return;
            }
            
            EditorApplication.LockReloadAssemblies();
            string targetPath = Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "unity-webview"));
            if(Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            Directory.Move(webviewPackagePath, targetPath);
            AssetDatabase.Refresh();
            
            AssetDatabase.ExportPackage(new[] { "Assets/AirConsole", "Assets/Plugins", "Assets/WebGLTemplates" },
                                        outputPath, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
           
            Directory.Move(targetPath, webviewPackagePath); 
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

        private static void DeleteOldUnityPackages(string outputPath, string newVersion) {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(outputPath), "airconsole-unity-plugin-*.*");
            foreach (string file in files) {
                if (!file.Contains(newVersion)) {
                    File.Delete(file);
                }
            }
        }
    }
}