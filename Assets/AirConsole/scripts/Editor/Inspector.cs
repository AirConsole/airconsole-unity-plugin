#if !DISABLE_AIRCONSOLE && UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

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
        private bool _inactiveNativeGameSizingValue;

        private const string TRANSLATION_ACTIVE = "var AIRCONSOLE_TRANSLATION = true;";
        private const string TRANSLATION_INACTIVE = "var AIRCONSOLE_TRANSLATION = false;";
        private const string INACTIVE_PLAYERS_SILENCED_ACTIVE = "var AIRCONSOLE_INACTIVE_PLAYERS_SILENCED = true;";
        private const string INACTIVE_PLAYERS_SILENCED_INACTIVE = "var AIRCONSOLE_INACTIVE_PLAYERS_SILENCED = false;";
        private const string ANDROID_NATIVE_GAME_SIZING_ACTIVE = "var AIRCONSOLE_ANDROID_NATIVE_GAMESIZING = true;";
        private const string ANDROID_NATIVE_GAME_SIZING_INACTIVE = "var AIRCONSOLE_ANDROID_NATIVE_GAMESIZING = false;";

        private static readonly string SettingsPath = Application.dataPath + Settings.WEBTEMPLATE_PATH + "/airconsole-settings.js";
        private static readonly string TranslationFilePath = Application.dataPath + Settings.WEBTEMPLATE_PATH + "/translation.js";

        [InitializeOnLoadMethod]
        private static void Migration() {
            MigrateVersion250(TranslationFilePath, SettingsPath);
        }

        public void Awake() {
            if (!File.Exists(SettingsPath)) return;

            string persistedSettings = File.ReadAllText(SettingsPath);
            translationValue = persistedSettings.Contains(TRANSLATION_ACTIVE);
            inactivePlayersSilencedValue = !persistedSettings.Contains(INACTIVE_PLAYERS_SILENCED_INACTIVE);
            _inactiveNativeGameSizingValue = !persistedSettings.Contains(ANDROID_NATIVE_GAME_SIZING_INACTIVE);
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

        private void DrawSettingsToggles() {
            DrawToggle("Translation", ref translationValue);
            DrawToggle("Silence Player", ref inactivePlayersSilencedValue);
            DrawToggle("Native Game Sizing", ref _inactiveNativeGameSizingValue);
        }

        private void DrawToggle(string label, ref bool value) {
            bool oldValue = value;
            value = EditorGUILayout.Toggle(label, value);
            if (oldValue != value) WriteConstructorSettings(SettingsPath);
        }

        private void ValidateAndroidGameVersion() {
            string androidGameVersion = serializedObject.FindProperty("androidGameVersion").stringValue;
            if (string.IsNullOrEmpty(androidGameVersion) || !Regex.IsMatch(androidGameVersion, @"^\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}$")) {
                EditorGUILayout.HelpBox("Please enter a valid Game Version for Android", MessageType.Error);
            }
        }

        private void ShowAdditionalProperties() {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("androidUIResizeMode"));
            if (serializedObject.FindProperty("androidUIResizeMode").enumValueIndex > (int)AndroidUIResizeMode.ResizeCamera) {
                EditorGUILayout.HelpBox("Android now uses SafeAreas.\n"
                                        + "We no longer support UI Reference Resolution Scaling.\n"
                                        + "You can use the 'AirConsole/AutoscaleCamera' component on your camera or.\n"
                                        + "use the event OnSafeAreaChanged to control this yourself.", MessageType.Warning);
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

        private void WriteConstructorSettings(string path) {
            try {
                File.WriteAllText(path,
                    $"{(translationValue ? TRANSLATION_ACTIVE : TRANSLATION_INACTIVE)}\n"
                    + $"{(inactivePlayersSilencedValue ? INACTIVE_PLAYERS_SILENCED_ACTIVE : INACTIVE_PLAYERS_SILENCED_INACTIVE)}\n"
                    + $"{(_inactiveNativeGameSizingValue ? ANDROID_NATIVE_GAME_SIZING_ACTIVE : ANDROID_NATIVE_GAME_SIZING_INACTIVE)}");
            } catch (IOException e) {
                Debug.LogError($"Failed to write settings file: {e.Message}");
            }
        }

        private static void MigrateVersion250(string originalPath, string newPath) {
            if (!File.Exists(originalPath)) return;

            if (!File.Exists(newPath)) {
                Debug.LogWarning("Update settings file to new version, renaming from translation.js to game-settings.js");
                File.Move(originalPath, newPath);
                File.AppendAllText(newPath, $"\n{INACTIVE_PLAYERS_SILENCED_INACTIVE}");
            } else {
                Debug.LogError($"game-settings.js found [{newPath}]. Deleting prior translation.js [{originalPath}].");
                File.Delete(originalPath);
            }
        }

        private static void OpenUpgradeInstructions() {
            Application.OpenURL(
                "https://github.com/AirConsole/airconsole-unity-plugin/wiki/Upgrading-the-Unity-Plugin-to-a-newer-version");
        }
    }
}
#endif
