#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace NDream.AirConsole.Editor {
    public class SettingWindow : EditorWindow {
        private GUIStyle styleBlack = new();
        private bool groupEnabled = false;
        private static Texture2D bg;
        private static Texture logo;
        private static Texture logoSmall;
        private static GUIContent titleInfo;

        public void OnEnable() {
            // get images
            bg = (Texture2D)Resources.Load("AirConsoleBg");
            logo = (Texture)Resources.Load("AirConsoleLogoText");
            logoSmall = (Texture)Resources.Load("AirConsoleLogoSmall");
            titleInfo = new GUIContent("AirConsole", logoSmall, "AirConsole Settings");

            // setup style for airconsole logo
            styleBlack.normal.background = bg;
            styleBlack.normal.textColor = Color.white;
            styleBlack.margin.top = 5;
            styleBlack.padding.right = 5;
        }

        [MenuItem("Window/AirConsole/Settings")]
        private static void Init() {
            SettingWindow window = (SettingWindow)GetWindow(typeof(SettingWindow));
            window.titleContent = titleInfo;
            window.Show();
        }

        private void OnGUI() {
            // show logo & version
            EditorGUILayout.BeginHorizontal(styleBlack, GUILayout.Height(30));
            GUILayout.Label(logo, GUILayout.Width(128), GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.Label("v" + Settings.VERSION, styleBlack);
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("AirConsole Settings", EditorStyles.boldLabel);

            Settings.webSocketPort = EditorGUILayout.IntField("Websocket Port", Settings.webSocketPort, GUILayout.MaxWidth(200));
            EditorPrefs.SetInt("webSocketPort", Settings.webSocketPort);

            Settings.webServerPort = EditorGUILayout.IntField("Webserver Port", Settings.webServerPort, GUILayout.MaxWidth(200));
            EditorPrefs.SetInt("webServerPort", Settings.webServerPort);

            EditorGUILayout.LabelField("Webserver is running", Extentions.webserver.IsRunning().ToString());

            GUILayout.BeginHorizontal();

            GUILayout.Space(150);
            if (GUILayout.Button("Stop", GUILayout.MaxWidth(60))) {
                Extentions.webserver.Stop();
            }

            if (GUILayout.Button("Restart", GUILayout.MaxWidth(60))) {
                Extentions.webserver.Restart();
            }

            GUILayout.EndHorizontal();

            groupEnabled = EditorGUILayout.BeginToggleGroup("Debug Settings", groupEnabled);

            Settings.debug.info = EditorGUILayout.Toggle("Info", Settings.debug.info);
            EditorPrefs.SetBool("debugInfo", Settings.debug.info);

            Settings.debug.warning = EditorGUILayout.Toggle("Warning", Settings.debug.warning);
            EditorPrefs.SetBool("debugWarning", Settings.debug.warning);

            Settings.debug.error = EditorGUILayout.Toggle("Error", Settings.debug.error);
            EditorPrefs.SetBool("debugError", Settings.debug.error);

            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.BeginHorizontal(styleBlack);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset Settings", GUILayout.MaxWidth(110))) {
                Extentions.ResetDefaultValues();
            }

            GUILayout.EndHorizontal();
        }
    }
}
#endif