#if !DISABLE_AIRCONSOLE && AIRCONSOLE_DEVELOPMENT

using UnityEditor;

namespace NDream.AirConsole.Editor {
    public class DevelopmentTools {
        [MenuItem("Tools/AirConsole/Development/Validate Android Configuration", false, 1)]
        public static void ValidateAndroidConfigurationMenuAction() {
            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);
        }

        [MenuItem("Tools/AirConsole/Development/Validate Web Configuration", false, 2)]
        public static void ValidateWebConfigurationMenuAction() {
            ProjectConfigurationCheck.CheckSettings(BuildTarget.WebGL);
        }

        [MenuItem("Tools/AirConsole/Development/Reset last plugin version", false, 21)]
        public static void ResetLastPluginVersionMenuAction() {
            PluginVersionTracker.DeleteLastPluginVersionKey();
        }

        [MenuItem("Tools/AirConsole/Development/Update Android Manifest", false, 41)]
        public static void UpdateAndroidManifestMenuAction() {
            AndroidManifestProcessor.UpdateAndroidManifest();
        }

        /// <summary>
        /// Handy little tool required when exceptions in 'EditorApplication.LockReloadAssemblies' blocks freeze assembly reloads
        /// until the next Unity restart.
        /// </summary>
        [MenuItem("Tools/AirConsole/Development/Unlock Assemblies", false, 99)]
        public static void UnlockAssembliesMenuAction() {
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();
        }
    }
}
#endif