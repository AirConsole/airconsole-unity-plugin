#if !DISABLE_AIRCONSOLE && UNITY_EDITOR && UNITY_2022_3_OR_NEWER
namespace NDream.Unity {
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    /// <summary>
    /// Imports the AirConsole code package after the plugin .unitypackage is imported, and removes layouts of older versions.
    /// </summary>
    public abstract class ProjectCodeUpdater {
        private const string CODE_PACKAGE_ASSET_PATH = "Assets/AirConsole/airconsole-code.unitypackage";

        public static string CodePackagePath {
            get => Path.GetFullPath(Path.Combine(Application.dataPath, "..", CODE_PACKAGE_ASSET_PATH));
        }

        private static string CodePackageName => Path.GetFileNameWithoutExtension(CODE_PACKAGE_ASSET_PATH);

        // SessionState survives the domain reloads of the import and is cleared when the editor restarts.
        private const string IMPORT_PENDING_KEY = "AirConsole.ProjectCodeUpdater.ImportPending";

        [InitializeOnLoadMethod]
        public static void ValidateProjectForImport() {
            // Asset import worker processes load editor code too, but must not change the AssetDatabase.
            if (AssetDatabase.IsAssetImportWorkerProcess()) {
                return;
            }

            bool isInPluginProject =
                File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "Packager", "Editor", "Packager.cs")));
            if (isInPluginProject) {
                return;
            }

            // ImportPackage completes after a domain reload, so the handlers are registered again on every load.
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            ImportCodePackage();
        }

        private static void ImportCodePackage() {
            if (!File.Exists(CodePackagePath)) {
                AssetDatabase.DeleteAsset($"Assets/AirConsole/{nameof(ProjectCodeUpdater)}.cs");
                return;
            }

            if (SessionState.GetBool(IMPORT_PENDING_KEY, false)) {
                return;
            }

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
        }

        private static void ExecuteCodePackageImport() {
            if (!File.Exists(CodePackagePath) || SessionState.GetBool(IMPORT_PENDING_KEY, false)) {
                return;
            }

            // In 2.6.0 and 2.6.1, unity-webview was one 'unity-webview' assembly (root asmdef, Plugins/WebViewObject.cs). 2.6.2 moved it
            // into Runtime/ and Editor/ assemblies. ImportPackage never deletes files, so the old assembly would stay: a second
            // WebViewObject (CS0433 where Assembly-CSharp uses it) and a second build postprocessor. Delete it in the same editor tick as
            // the import: once it is gone the old plugin code cannot compile, so this updater could not run again to finish the upgrade.
            if (File.Exists(Path.Combine(Application.dataPath, "AirConsole", "unity-webview", "unity-webview.asmdef"))) {
                AssetDatabase.DeleteAsset("Assets/AirConsole/unity-webview");
            }

            SessionState.SetBool(IMPORT_PENDING_KEY, true);
            AssetDatabase.ImportPackage(CodePackagePath, false);
        }

        // Keep this updater and the code package until the import has completed. An interrupted import runs again on the next load;
        // after a failed import the user must import the code package manually.
        private static void OnImportPackageCompleted(string packageName) {
            if (packageName != CodePackageName) {
                return;
            }

            SessionState.EraseBool(IMPORT_PENDING_KEY);
            AssetDatabase.DeleteAsset($"Assets/AirConsole/{nameof(ProjectCodeUpdater)}.cs");
            AssetDatabase.DeleteAsset(CODE_PACKAGE_ASSET_PATH);
            EditorUtility.DisplayDialog("Success", "The AirConsole Plugin has been successfully imported", "ok");
        }

        private static void OnImportPackageFailed(string packageName, string errorMessage) {
            if (packageName != CodePackageName) {
                return;
            }

            SessionState.EraseBool(IMPORT_PENDING_KEY);
            Debug.LogError($"AirConsole: importing {CodePackagePath} failed: {errorMessage}. The project does not compile until "
                           + "it is imported: import it now with Assets > Import Package > Custom Package, before you restart Unity.");
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
