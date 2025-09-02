#if !DISABLE_AIRCONSOLE && UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;
using UnityEngine.Networking;
using System.Linq;
using UnityEditorInternal;

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
            // Start background update check once per editor session
            BeginBackgroundUpdateCheck();

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

            if (s_UpdateCheckInProgress) {
                GUILayout.Label("Checking for updates…", GUILayout.MaxWidth(160));
            } else if (s_UpdateAvailable) {
                if (GUILayout.Button($"Update Plugin → v{(s_LatestVersion != null ? s_LatestVersion.ToString() : "?")}", GUILayout.MaxWidth(200))) {
                    TryUpdatePluginFromGithub();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        internal void UpdateAirConsoleConstructorSettings() {
            ReadConstructorSettings();
            WriteConstructorSettings();
        }

        private void ReadConstructorSettings() {
            if (!File.Exists(SettingsPath)) {
                return;
            }

            string persistedSettings = File.ReadAllText(SettingsPath);
            translationValue = persistedSettings.Contains(TRANSLATION_ACTIVE);
            inactivePlayersSilencedValue = !persistedSettings.Contains(INACTIVE_PLAYERS_SILENCED_INACTIVE);
            nativeGameSizingSupportedValue = !persistedSettings.Contains(ANDROID_NATIVE_GAME_SIZING_INACTIVE);
        }

        private void WriteConstructorSettings() {
            try {
                File.WriteAllText(SettingsPath,
                    $"{(translationValue ? TRANSLATION_ACTIVE : TRANSLATION_INACTIVE)}\n"
                    + $"{(inactivePlayersSilencedValue ? INACTIVE_PLAYERS_SILENCED_ACTIVE : INACTIVE_PLAYERS_SILENCED_INACTIVE)}\n"
                    + $"{(nativeGameSizingSupportedValue ? ANDROID_NATIVE_GAME_SIZING_ACTIVE : ANDROID_NATIVE_GAME_SIZING_INACTIVE)}\n"
                    + GenerateGameInformation());
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

        // ---------- Self-Update support (GitHub releases) ----------
        private static readonly string GITHUB_LATEST_API = "https://api.github.com/repos/airconsole/airconsole-unity-plugin/releases/latest";
        private static bool s_UpdateCheckStarted;
        private static bool s_UpdateCheckInProgress;
        private static bool s_UpdateAvailable;
        private static Version s_LatestVersion;
        private static GithubRelease s_CachedLatestRelease;
        private static UnityWebRequest s_CheckRequest;
        private static UnityWebRequestAsyncOperation s_CheckOp;

        private static void BeginBackgroundUpdateCheck() {
            if (s_UpdateCheckStarted || s_UpdateCheckInProgress)
                return;
            s_UpdateCheckStarted = true;
            s_UpdateCheckInProgress = true;
            try {
                s_CheckRequest = UnityWebRequest.Get(GITHUB_LATEST_API);
                s_CheckRequest.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                s_CheckOp = s_CheckRequest.SendWebRequest();
                EditorApplication.update += PollUpdateCheck;
            } catch (Exception e) {
                s_UpdateCheckInProgress = false;
                AirConsoleLogger.LogError(() => $"Failed to start update check: {e.Message}");
            }
        }

        private static void PollUpdateCheck() {
            if (s_CheckOp == null)
                return;
#if UNITY_2020_2_OR_NEWER
            bool done = s_CheckOp.isDone && s_CheckRequest.result != UnityWebRequest.Result.InProgress;
            bool success = s_CheckRequest.result == UnityWebRequest.Result.Success;
#else
            bool done = s_CheckOp.isDone;
            bool success = !s_CheckRequest.isNetworkError && !s_CheckRequest.isHttpError;
#endif
            if (!done) return;

            try {
                if (success) {
                    var json = s_CheckRequest.downloadHandler.text;
                    var release = JsonUtility.FromJson<GithubRelease>(json);
                    s_CachedLatestRelease = release;
                    var latestTag = (release?.tag_name ?? release?.name ?? "").Trim();
                    var latestVersionStr = latestTag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? latestTag.Substring(1) : latestTag;
                    if (Version.TryParse(Settings.VERSION, out var current) && Version.TryParse(latestVersionStr, out var latest)) {
                        s_LatestVersion = latest;
                        s_UpdateAvailable = latest > current;
                    } else {
                        s_UpdateAvailable = false;
                    }
                } else {
                    AirConsoleLogger.LogWarning(() => $"Update check failed: {s_CheckRequest.responseCode} {s_CheckRequest.error}");
                    s_UpdateAvailable = false;
                }
            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Update check error: {ex.Message}");
                s_UpdateAvailable = false;
            } finally {
                s_UpdateCheckInProgress = false;
                s_CheckRequest.Dispose();
                s_CheckRequest = null;
                s_CheckOp = null;
                EditorApplication.update -= PollUpdateCheck;
                InternalEditorUtility.RepaintAllViews();
            }
        }

        [Serializable]
        private class GithubAsset {
            public string name;
            public string browser_download_url;
        }

        [Serializable]
        private class GithubRelease {
            public string tag_name;
            public string name;
            public GithubAsset[] assets;
            public string html_url;
        }

        private static void TryUpdatePluginFromGithub() {
            try {
                EditorUtility.DisplayProgressBar("AirConsole", "Checking latest release…", 0.1f);
                GithubRelease release = s_CachedLatestRelease;
                if (release == null) {
                    string json = DownloadString(GITHUB_LATEST_API, out long _);
                    if (string.IsNullOrEmpty(json)) {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("AirConsole", "Failed to retrieve release info.", "OK");
                        return;
                    }
                    release = JsonUtility.FromJson<GithubRelease>(json);
                }
                if (release == null) {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("AirConsole", "Could not parse release info.", "OK");
                    return;
                }

                string latestTag = (release.tag_name ?? release.name ?? "").Trim();
                string latestVersionStr = latestTag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                    ? latestTag.Substring(1)
                    : latestTag;

                Version current, latest;
                bool currentOk = Version.TryParse(Settings.VERSION, out current);
                bool latestOk = Version.TryParse(latestVersionStr, out latest);

                if (currentOk && latestOk && latest <= current) {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("AirConsole", $"Plugin is up to date (v{Settings.VERSION}).", "OK");
                    return;
                }

                string msg = latestOk
                    ? $"Update available: v{Settings.VERSION} → v{latest}"
                    : $"A newer release may be available: '{latestTag}'.\nCurrent: v{Settings.VERSION}";
                bool doUpdate = EditorUtility.DisplayDialog("AirConsole", msg + "\n\nDownload and import the latest release?", "Update", "Cancel");
                if (!doUpdate) {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                GithubAsset pkg = null;
                if (release.assets != null && release.assets.Length > 0) {
                    pkg = release.assets.FirstOrDefault(a => a != null && !string.IsNullOrEmpty(a.name) && a.name.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                          ?? release.assets.FirstOrDefault(a => a != null && !string.IsNullOrEmpty(a.name) && a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                }

                if (pkg == null || string.IsNullOrEmpty(pkg.browser_download_url)) {
                    EditorUtility.ClearProgressBar();
                    if (!string.IsNullOrEmpty(release.html_url)) {
                        bool open = EditorUtility.DisplayDialog("AirConsole", "Could not find a .unitypackage asset in the latest release.", "Open Releases", "Close");
                        if (open) Application.OpenURL(release.html_url);
                    } else {
                        EditorUtility.DisplayDialog("AirConsole", "Could not find downloadable assets for the latest release.", "OK");
                    }
                    return;
                }

                string tempPath = Path.Combine(Path.GetTempPath(), pkg.name ?? ("airconsole-plugin-" + Guid.NewGuid().ToString("N") + ".unitypackage"));
                EditorUtility.DisplayProgressBar("AirConsole", "Downloading package…", 0.4f);
                long contentLength;
                byte[] data = DownloadBytes(pkg.browser_download_url, out contentLength);
                if (data == null || data.Length == 0) {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("AirConsole", "Failed to download the package.", "OK");
                    return;
                }

                File.WriteAllBytes(tempPath, data);
                EditorUtility.DisplayProgressBar("AirConsole", "Importing package…", 0.9f);

                if (tempPath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase)) {
                    AssetDatabase.ImportPackage(tempPath, true);
                } else {
                    bool open = EditorUtility.DisplayDialog("AirConsole", "Downloaded asset is not a .unitypackage. Open in browser instead?", "Open", "Cancel");
                    if (open) Application.OpenURL(pkg.browser_download_url);
                }

                EditorUtility.ClearProgressBar();
            } catch (Exception ex) {
                EditorUtility.ClearProgressBar();
                AirConsoleLogger.LogError(() => $"Update failed: {ex.Message}");
                EditorUtility.DisplayDialog("AirConsole", "Update failed. See console for details.", "OK");
            }
        }

        private static string DownloadString(string url, out long contentLength) {
            contentLength = -1;
            using (UnityWebRequest req = UnityWebRequest.Get(url)) {
                req.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                var op = req.SendWebRequest();
                while (!op.isDone) {
                    EditorUtility.DisplayProgressBar("AirConsole", "Contacting GitHub…", req.downloadProgress);
                }
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    AirConsoleLogger.LogError(() => $"HTTP error: {req.responseCode} {req.error}");
                    return null;
                }
                contentLength = req.GetResponseHeaders() != null && req.GetResponseHeaders().ContainsKey("CONTENT-LENGTH")
                    ? long.Parse(req.GetResponseHeader("CONTENT-LENGTH"))
                    : -1;
                return req.downloadHandler.text;
            }
        }

        private static byte[] DownloadBytes(string url, out long contentLength) {
            contentLength = -1;
            using (UnityWebRequest req = UnityWebRequest.Get(url)) {
                req.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                var op = req.SendWebRequest();
                while (!op.isDone) {
                    EditorUtility.DisplayProgressBar("AirConsole", "Downloading…", req.downloadProgress);
                }
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    AirConsoleLogger.LogError(() => $"HTTP error: {req.responseCode} {req.error}");
                    return null;
                }
                contentLength = req.GetResponseHeaders() != null && req.GetResponseHeaders().ContainsKey("CONTENT-LENGTH")
                    ? long.Parse(req.GetResponseHeader("CONTENT-LENGTH"))
                    : -1;
                return req.downloadHandler.data;
            }
        }
    }
}
#endif