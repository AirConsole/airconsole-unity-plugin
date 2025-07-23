#if !DISABLE_AIRCONSOLE && UNITY_EDITOR
namespace NDream.Unity {
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    /// <summary>
    /// This is responsible to guide developers through the possible steps for a project upgrade to 2.6.0.
    /// </summary>
    public abstract class ProjectCodeUpdater {
        public static string CodePackagePath {
            get => Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "airconsole-code.unitypackage"));
        }

        [InitializeOnLoadMethod]
        public static void ValidateProjectForImport() {
            bool isInPluginProject =
                File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "Packager", "Editor", "Packager.cs")));
            if (isInPluginProject) {
                return;
            }

            string pathToAirConsole =
                Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "scripts", "AirConsole.cs"));
            bool upgradeInstructionsNotFollowed = File.Exists(pathToAirConsole);

            if (upgradeInstructionsNotFollowed) {
                string upgradeInstructionUrl =
                    "https://github.com/AirConsole/airconsole-unity-plugin/wiki/Upgrading-the-Unity-Plugin-to-a-newer-version";
                EditorUtility.DisplayDialog("Upgrade Error",
                    "Please follow the upgrade instructions for Unity v2.6.0 and newer before updating the AirConsole plugin",
                    "I understand");
                Application.OpenURL(upgradeInstructionUrl);
                throw new BuildFailedException(
                    $"Please visit {upgradeInstructionUrl} and follow the upgrade instructions for v2.6.0 and newer");
            }

            ImportCodePackage();
            EditorUtility.DisplayDialog("Success", "The AirConsole Plugin has been successfully imported", "ok");
        }

        private static void ImportCodePackage() {
            string packagePath = CodePackagePath;
            if (File.Exists(packagePath)) {
                AssetDatabase.ImportPackage(packagePath, false);
                AssetDatabase.DeleteAsset($"Assets/AirConsole/{nameof(ProjectCodeUpdater)}.cs");
                AssetDatabase.DeleteAsset(packagePath.Replace(Application.dataPath, "Assets"));
            } else {
                AssetDatabase.DeleteAsset($"Assets/AirConsole/{nameof(ProjectCodeUpdater)}.cs");
            }
        }
    }
}
#endif
