using UnityEngine;
using System.Collections;
using UnityEditor;
using AirConsole;

[CustomEditor(typeof(AirController))]
public class LevelScriptEditor : Editor {

    GUIStyle styleBlack = new GUIStyle();

    void OnEnable() {
    }

    public override void OnInspectorGUI() {

        AirController controller = (AirController)target;

        styleBlack.normal.background = MakeTex(1, 1, HexToColor("1f1f1f"));
        styleBlack.normal.textColor = Color.white;
        styleBlack.margin.top = 5;
        styleBlack.padding.right = 5;


        // show logo & version
        EditorGUILayout.BeginHorizontal(styleBlack, GUILayout.Height(30));
        Texture logo = (Texture)Resources.Load("AirConsoleLogoText");
        GUILayout.Label(logo, GUILayout.Width(128), GUILayout.Height(30));
        GUILayout.FlexibleSpace();
        GUILayout.Label("v"+AirController.VERSION, styleBlack);
        EditorGUILayout.EndHorizontal();

        // Show default inspector property editor withouth script referenz
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, new string[]{"m_Script"});
        serializedObject.ApplyModifiedProperties();

        // check if a port was exported
        if (System.IO.File.Exists(EditorPrefs.GetString("airconsolePortPath") + "/screen.html")) {

            if (GUILayout.Button("Open Exported Port", GUILayout.MaxWidth(150))) {

                AirWebserver ws = new AirWebserver(controller.settings.webServerPort, controller.debug, controller.browserStartMode, EditorPrefs.GetString("airconsolePortPath"));
                ws.Start();
            }
        }

    }

    private Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    string ColorToHex(Color32 color) {
        string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
        return hex;
    }

    Color HexToColor(string hex) {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }
}