#if !DISABLE_AIRCONSOLE // && AIRCONSOLE_DEVELOPMENT

namespace NDream.AirConsole.Editor {
    using UnityEngine;
    using UnityEditor;

    public abstract class DevelopmentTools {
        [MenuItem("Tools/AirConsole/Development/Validate Android Configuration", false, 1)]
        public static void ValidateAndroidConfigurationMenuAction() {
            Debug.ClearDeveloperConsole();
            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);
            UpdateAirConsoleConstructorSettings();
        }

        [MenuItem("Tools/AirConsole/Development/Validate Web Configuration", false, 2)]
        public static void ValidateWebConfigurationMenuAction() {
            Debug.ClearDeveloperConsole();
            ProjectConfigurationCheck.CheckSettings(BuildTarget.WebGL);
            UpdateAirConsoleConstructorSettings();
        }

        private static void UpdateAirConsoleConstructorSettings() {
            Inspector instance = Editor.CreateInstance<Inspector>();
            instance.UpdateAirConsoleConstructorSettings();
        }

        [MenuItem("Tools/AirConsole/Development/Reset last plugin version", false, 21)]
        public static void ResetLastPluginVersionMenuAction() {
            PluginVersionTracker.DeleteLastPluginVersionKey();
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

        [MenuItem("Tools/AirConsole/Build/Web", false, 61)]
        public static void BuildWeb() => BuildHelper.BuildWeb();

        [MenuItem("Tools/AirConsole/Build/Android", false, 62)]
        public static void BuildAndroid() => BuildHelper.BuildAndroid();

        [MenuItem("Tools/AirConsole/Build/Android Internal", false, 63)]
        public static void BuildAndroidInternal() => BuildHelper.BuildAndroidInternal();
    }
}
#endif
