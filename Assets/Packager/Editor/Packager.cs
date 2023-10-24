#region
using NDream.AirConsole;
using System;
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
            CopyDirectory(webviewPackagePath, targetPath, true, filename => !filename.Contains(".asmdef"));
            AssetDatabase.Refresh();
            
            AssetDatabase.ExportPackage(new[] { "Assets/AirConsole", "Assets/Edtor", "Assets/Plugins", "Assets/WebGLTemplates" },
                                        outputPath, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
            
            Directory.Delete(targetPath, true);
            AssetDatabase.Refresh();
            EditorApplication.UnlockReloadAssemblies();
            Debug.ClearDeveloperConsole();

            string oldOutputPath = outputPath.Replace($"v{Settings.VERSION}", $"v{DecrementVersion(Settings.VERSION)}");
            if(File.Exists(oldOutputPath)) File.Delete(oldOutputPath);
            
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

        private static string DecrementVersion(string version) {
            string[] versionSplit = version.Split('.');
            if(versionSplit.Length != 2) 
                throw new ArgumentException($"Invalid version {version}, not MAJOR.MINOR");
            string minor = versionSplit[1];
            if(int.TryParse(minor, out int minorVersion)) {
                return $"{versionSplit[0]}.{(minorVersion-1)}";
            }
            throw new ArgumentException($"Invalid version {version}, MINOR ${minor} is not a number");
        }

        // adapted from https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, Func<string, bool> include)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                if(!include(file.FullName)) continue;
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true, include);
                }
            }
        }
    }
}