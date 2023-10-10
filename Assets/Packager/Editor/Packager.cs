#region
using NDream.AirConsole;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
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

            string targetPath = Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "unity-webview"));
            if(Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            Directory.Move(webviewPackagePath, targetPath);
            Debug.Log($"Moved {webviewPackagePath} to {targetPath}");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            AssetDatabase.ExportPackage(new[] { "Assets/AirConsole", "Assets/Edtor", "Assets/Plugins", "Assets/WebGLTemplates" },
                                        outputPath, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

            Directory.Move(targetPath, webviewPackagePath);
            Debug.Log($"Moved {targetPath} to {webviewPackagePath}");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            Application.OpenURL("file://" + Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", outputPath)));
        }
    }
}