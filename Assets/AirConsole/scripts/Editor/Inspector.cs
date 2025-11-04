#if !DISABLE_AIRCONSOLE && UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;
using NDream.AirConsole;

namespace NDream.AirConsole.Editor {
    [CustomEditor(typeof(AirConsole))]
    public class Inspector : UnityEditor.Editor {
        private GUIStyle styleBlack = new();
        private Texture2D bg;
        private Texture logo;
        private AirConsole controller;
        private SerializedProperty gameId;
        private SerializedProperty gameVersion;
        private bool translationValue;
        private bool inactivePlayersSilencedValue;
        private bool nativeGameSizingSupportedValue;
        private const string TRANSLATION_ACTIVE = "var AIRCONSOLE_TRANSLATION = true;";
        private const string TRANSLATION_INACTIVE = "var AIRCONSOLE_TRANSLATION = false;";
        private const string INACTIVE_PLAYERS_SILENCED_ACTIVE = "var AIRCONSOLE_INACTIVE_PLAYERS_SILENCED = true;";
        private const string INACTIVE_PLAYERS_SILENCED_INACTIVE = "var AIRCONSOLE_INACTIVE_PLAYERS_SILENCED = false;";
        private const string ANDROID_NATIVE_GAME_SIZING_ACTIVE = "var AIRCONSOLE_ANDROID_NATIVE_GAMESIZING = true;";
        private const string ANDROID_NATIVE_GAME_SIZING_INACTIVE = "var AIRCONSOLE_ANDROID_NATIVE_GAMESIZING = false;";
        private const string AIRCONSOLE_RUNTIME_SETTINGS_ASSET_FILE = AirconsoleRuntimeSettings.ResourceName + ".asset";

        private static string SettingsPath => Application.dataPath + Settings.WEBTEMPLATE_PATH + "/airconsole-settings.js";

        [InitializeOnLoadMethod]
        private static void Migration() {
            MigrateVersion250(Application.dataPath + Settings.WEBTEMPLATE_PATH + "/translation.js", SettingsPath);
        }

        public void Awake() {
            ReadConstructorSettings();
        }

        public void OnEnable() {
            LoadResources();
            SetupStyle();
        }

        private void LoadResources() {
            bg = (Texture2D)Resources.Load("AirConsoleBg");
            logo = (Texture)Resources.Load("AirConsoleLogoText");
        }

        private void SetupStyle() {
            styleBlack.normal.background = bg;
            styleBlack.normal.textColor = Color.white;
            styleBlack.alignment = TextAnchor.MiddleRight;
            styleBlack.margin.top = 5;
            styleBlack.margin.bottom = 5;
            styleBlack.padding.right = 2;
            styleBlack.padding.bottom = 2;
        }

        public override void OnInspectorGUI() {
            controller = (AirConsole)target;

            ShowLogoAndVersion();
            ShowDefaultProperties();
            DrawSettingsToggles();

#if UNITY_ANDROID
            ValidateAndroidGameVersion();
#endif

            ShowAdditionalProperties();
            ShowButtons();
        }

        private void ShowLogoAndVersion() {
            EditorGUILayout.BeginHorizontal(styleBlack, GUILayout.Height(30));
            GUILayout.Label(logo, GUILayout.Width(128), GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.Label("v" + Settings.VERSION, styleBlack);
            EditorGUILayout.EndHorizontal();
        }

        private void ShowDefaultProperties() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("controllerHtml"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoScaleCanvas"));
        }

        [Conditional("UNITY_ANDROID")]
        private void AndroidOnlyHelpBox(string message, MessageType messageType = MessageType.Info) {
            EditorGUILayout.HelpBox(message, messageType, true);
        }

        private void DrawSettingsToggles() {
            DrawToggle("Translation", ref translationValue);
            DrawToggle("Silence Player", ref inactivePlayersSilencedValue);

            DrawToggle(new GUIContent("Native Game Sizing", "Enables SafeArea support with fullscreen webview overlay"),
                ref nativeGameSizingSupportedValue);
            if (!nativeGameSizingSupportedValue) {
                AndroidOnlyHelpBox("Android for Automotive requires you to enable this and to implement the OnSafeAreaChanged "
                                   + "event handler provided by the AirConsole instance, enabling you to only render your game content"
                                   + " in relevant area", MessageType.Warning);
            }
        }

        private void DrawToggle(string label, ref bool value) {
            bool oldValue = value;
            value = EditorGUILayout.Toggle(label, value);
            if (oldValue != value) {
                WriteConstructorSettings();
            }
        }

        private void DrawToggle(GUIContent content, ref bool value) {
            bool oldValue = value;
            value = EditorGUILayout.Toggle(content, value);
            if (oldValue != value) {
                WriteConstructorSettings();
            }
        }

        private void ValidateAndroidGameVersion() {
            string androidGameVersion = serializedObject.FindProperty("androidGameVersion").stringValue;
            if (string.IsNullOrEmpty(androidGameVersion) || !Regex.IsMatch(androidGameVersion, @"^\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}$")) {
                EditorGUILayout.HelpBox("Please enter a valid Game Version for Android", MessageType.Error);
            }
        }

        private void ShowAdditionalProperties() {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("androidGameVersion"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("androidUIResizeMode"));
            if (serializedObject.FindProperty("androidUIResizeMode").enumValueIndex > (int)AndroidUIResizeMode.ResizeCamera
                && nativeGameSizingSupportedValue) {
                AndroidOnlyHelpBox("Android with native game sizing requires SafeAreas.\n"
                                   + "In this mode, AirConsole no longer supports UI Reference Resolution Scaling.",
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("webViewLoadingSprite"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("browserStartMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("devGameId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("devLanguage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LocalIpOverride"));

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowButtons() {
            EditorGUILayout.BeginHorizontal(styleBlack);
            // check if a port was exported
            if (File.Exists(EditorPrefs.GetString("airconsolePortPath") + "/screen.html")) {
                if (GUILayout.Button("Open Exported Port", GUILayout.MaxWidth(130))) {
                    Extentions.OpenBrowser(controller, EditorPrefs.GetString("airconsolePortPath"));
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Settings")) {
                SettingWindow window = (SettingWindow)EditorWindow.GetWindow(typeof(SettingWindow));
                window.Show();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(styleBlack);
            if (GUILayout.Button("Upgrade instructions", GUILayout.MaxWidth(130))) {
                OpenUpgradeInstructions();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        internal void UpdateAirConsoleConstructorSettings() {
            ReadConstructorSettings();
            WriteConstructorSettings();
        }

        private void ReadConstructorSettings() {
            bool hasNativeGameSizingValue = false;
            if (File.Exists(SettingsPath)) {
                string persistedSettings = File.ReadAllText(SettingsPath);
                translationValue = persistedSettings.Contains(TRANSLATION_ACTIVE);
                inactivePlayersSilencedValue = !persistedSettings.Contains(INACTIVE_PLAYERS_SILENCED_INACTIVE);
                nativeGameSizingSupportedValue = !persistedSettings.Contains(ANDROID_NATIVE_GAME_SIZING_INACTIVE);
                hasNativeGameSizingValue = true;
            } else {
                AirconsoleRuntimeSettings asset = LoadAirconsoleRuntimeSettingsAsset();
                if (asset != null) {
                    nativeGameSizingSupportedValue = asset.NativeGameSizingSupported;
                    hasNativeGameSizingValue = true;
                }
            }

            if (hasNativeGameSizingValue) {
                PersistAirconsoleRuntimeSettings(nativeGameSizingSupportedValue);
            }
        }

        private void WriteConstructorSettings() {
            try {
                File.WriteAllText(SettingsPath,
                    $"{(translationValue ? TRANSLATION_ACTIVE : TRANSLATION_INACTIVE)}\n"
                    + $"{(inactivePlayersSilencedValue ? INACTIVE_PLAYERS_SILENCED_ACTIVE : INACTIVE_PLAYERS_SILENCED_INACTIVE)}\n"
                    + $"{(nativeGameSizingSupportedValue ? ANDROID_NATIVE_GAME_SIZING_ACTIVE : ANDROID_NATIVE_GAME_SIZING_INACTIVE)}\n"
                    + GenerateGameInformation());
                PersistAirconsoleRuntimeSettings(nativeGameSizingSupportedValue);
            } catch (IOException e) {
                AirConsoleLogger.LogError(() => $"Failed to write settings file at {SettingsPath}: {e.Message}");
            }
        }

        private static string GenerateGameInformation() =>
            $"window.UNITY_VERSION = '{Application.unityVersion}';\n"
            + $"window.AIRCONSOLE_VERSION = '{Settings.VERSION}';";

        private static void MigrateVersion250(string originalPath, string newPath) {
            if (!File.Exists(originalPath)) {
                return;
            }

            if (!File.Exists(newPath)) {
                AirConsoleLogger.LogWarning(() => "Update settings file to new version, renaming from translation.js to game-settings.js");
                File.Move(originalPath, newPath);
                File.AppendAllText(newPath, $"\n{INACTIVE_PLAYERS_SILENCED_INACTIVE}");
            } else {
                AirConsoleLogger.LogError(() => $"game-settings.js found [{newPath}]. Deleting prior translation.js [{originalPath}].");
                File.Delete(originalPath);
            }
        }

        private static void OpenUpgradeInstructions() {
            Application.OpenURL(
                "https://github.com/AirConsole/airconsole-unity-plugin/wiki/Upgrading-the-Unity-Plugin-to-a-newer-version");
        }

        private static AirconsoleRuntimeSettings LoadAirconsoleRuntimeSettingsAsset() {
            string assetPath = GetNativeGameSizingSettingsAssetPath(false);
            return string.IsNullOrEmpty(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<AirconsoleRuntimeSettings>(assetPath);
        }

        private static void PersistAirconsoleRuntimeSettings(bool value) {
            string assetPath = GetNativeGameSizingSettingsAssetPath(true);
            if (string.IsNullOrEmpty(assetPath)) {
                return;
            }

            AirconsoleRuntimeSettings asset = AssetDatabase.LoadAssetAtPath<AirconsoleRuntimeSettings>(assetPath);
            if (asset == null) {
                asset = CreateInstance<AirconsoleRuntimeSettings>();
                asset.SetNativeGameSizingSupported(value);
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                return;
            }

            if (asset.NativeGameSizingSupported == value) {
                return;
            }

            asset.SetNativeGameSizingSupported(value);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static string GetNativeGameSizingSettingsAssetPath(bool ensureFolderExists) {
            string resourcesFolder = GetResourcesFolderRelativeToAirConsole(ensureFolderExists);
            if (string.IsNullOrEmpty(resourcesFolder)) {
                return null;
            }

            return $"{resourcesFolder}/{AIRCONSOLE_RUNTIME_SETTINGS_ASSET_FILE}";
        }

        private static string GetResourcesFolderRelativeToAirConsole(bool ensureFolderExists) {
            string airConsoleDirectory = GetAirConsoleScriptDirectory();
            if (string.IsNullOrEmpty(airConsoleDirectory)) {
                return null;
            }

            string resourcesFolder = $"{airConsoleDirectory}/Resources";
            if (AssetDatabase.IsValidFolder(resourcesFolder)) {
                return resourcesFolder;
            }

            if (!ensureFolderExists) {
                return resourcesFolder;
            }

            string parentFolder = Path.GetDirectoryName(resourcesFolder)?.Replace("\\", "/");
            string folderName = Path.GetFileName(resourcesFolder);
            if (string.IsNullOrEmpty(parentFolder) || string.IsNullOrEmpty(folderName)) {
                AirConsoleLogger.LogError(() => $"Failed to resolve Resources folder relative to {airConsoleDirectory}");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(parentFolder)) {
                AirConsoleLogger.LogError(() => $"Resources parent folder not found: {parentFolder}");
                return null;
            }

            AssetDatabase.CreateFolder(parentFolder, folderName);
            return resourcesFolder;
        }

        private static string GetAirConsoleScriptDirectory() {
            string[] guids = AssetDatabase.FindAssets("AirConsole t:MonoScript");
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/AirConsole.cs")) {
                    return Path.GetDirectoryName(path)?.Replace("\\", "/");
                }
            }

            AirConsoleLogger.LogError(() => "Unable to locate AirConsole.cs via AssetDatabase.");
            return null;
        }
    }
}
#endif
