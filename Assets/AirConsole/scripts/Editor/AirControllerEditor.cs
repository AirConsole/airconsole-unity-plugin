using UnityEngine;
using System.Collections;
using UnityEditor;
using AirConsole;

[CustomEditor(typeof(AirController))]
public class LevelScriptEditor : Editor {

    public override void OnInspectorGUI() {

        Texture logo = (Texture)Resources.Load("AirConsoleLogo");
        GUILayout.Label(logo);

        // Show default inspector property editor
        DrawDefaultInspector();

    }
}