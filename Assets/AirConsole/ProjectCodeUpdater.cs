#if !DISABLE_AIRCONSOLE && UNITY_EDITOR && UNITY_2022_3_OR_NEWER
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

            ImportCodePackage();
            EditorUtility.DisplayDialog("Success", "The AirConsole Plugin has been successfully imported", "ok");
        }

        private static void ImportCodePackage() {
            if (File.Exists(CodePackagePath)) {
                // In 2.6.0, this was moved to Assets/AirConsole/scripts/Editor/Assets/AirConsoleIcon.png with editor icon focused import settings.
                AssetDatabase.DeleteAsset("Assets/AirConsole/resources/AirConsoleLogo.png");

                if (RequiresStructureCleanup()) {
                    AssetDatabase.DeleteAsset("Assets/AirConsole/examples");
                    AssetDatabase.DeleteAsset("Assets/AirConsole/scripts");
                    AssetDatabase.DeleteAsset("Assets/AirConsole/unity-webview");
                    AssetDatabase.Refresh();
                }

                // Because the AssetDatabase refresh happens asynchronously at the end of the editor loop, we must use delayedCall to
                // execute the package import. Otherwise, files like AirConsole.cs that must be imported in the 'scripts/Runtime' directory
                // would be located outside and break compilation.
                // Without this, AirConsole.cs would be imported in Assets/AirConsole/scripts instead of Assets/AirConsole/scripts/Runtime.
                EditorApplication.delayCall += () => ExecuteCodePackageImport();
            } else {
                AssetDatabase.DeleteAsset($"Assets/AirConsole/{nameof(ProjectCodeUpdater)}.cs");
            }
        }

        private static void ExecuteCodePackageImport() {
            AssetDatabase.ImportPackage(CodePackagePath, false);
            AssetDatabase.DeleteAsset($"Assets/AirConsole/{nameof(ProjectCodeUpdater)}.cs");
            AssetDatabase.DeleteAsset(CodePackagePath.Replace(Application.dataPath, "Assets"));
        }

        private static bool RequiresStructureCleanup() {
            string legacyAirConsolePath = Path.Combine(Application.dataPath, "AirConsole", "scripts", "AirConsole.cs");
            string runtimeAirConsolePath = Path.Combine(Application.dataPath, "AirConsole", "scripts", "Runtime", "AirConsole.cs");

            bool legacyExists = File.Exists(legacyAirConsolePath);
            bool runtimeExists = File.Exists(runtimeAirConsolePath);

            return legacyExists && !runtimeExists;
        }
    }
}
#endif
