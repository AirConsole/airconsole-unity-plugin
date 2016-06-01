#if !DISABLE_AIRCONSOLE
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace NDream.AirConsole.Editor {
	[CustomEditor(typeof(AirConsole))]
	public class Inspector : UnityEditor.Editor {

		GUIStyle styleBlack = new GUIStyle ();
		Texture2D bg;
		Texture logo;
		AirConsole controller;
		private SerializedProperty gameId;
		private SerializedProperty gameVersion;

		public void OnEnable () {

			// get logos
			bg = (Texture2D)Resources.Load ("AirConsoleBg");
			logo = (Texture)Resources.Load ("AirConsoleLogoText");

			// setup style for airconsole logo
			styleBlack.normal.background = bg;
			styleBlack.normal.textColor = Color.white;
			styleBlack.alignment = TextAnchor.MiddleRight;
			styleBlack.margin.top = 5;
			styleBlack.margin.bottom = 5;
			styleBlack.padding.right = 2;
			styleBlack.padding.bottom = 2;
		}

		public override void OnInspectorGUI () {

			controller = (AirConsole)target;

			// show logo & version
			EditorGUILayout.BeginHorizontal (styleBlack, GUILayout.Height (30));
			GUILayout.Label (logo, GUILayout.Width (128), GUILayout.Height (30));
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("v" + Settings.VERSION, styleBlack);
			EditorGUILayout.EndHorizontal ();

			// show default inspector property editor withouth script referenz
			serializedObject.Update ();
			DrawPropertiesExcluding (serializedObject, new string[] { "m_Script" });
			serializedObject.ApplyModifiedProperties ();

			EditorGUILayout.BeginHorizontal (styleBlack);
			// check if a port was exported
			if (System.IO.File.Exists (EditorPrefs.GetString ("airconsolePortPath") + "/screen.html")) {

				if (GUILayout.Button ("Open Exported Port", GUILayout.MaxWidth (130))) {

					Extentions.OpenBrowser (controller, EditorPrefs.GetString ("airconsolePortPath"));
				}
			}

			GUILayout.FlexibleSpace ();

			if (GUILayout.Button ("Settings")) {
				SettingWindow window = (SettingWindow)EditorWindow.GetWindow (typeof(SettingWindow));
				window.Show ();
			}

			EditorGUILayout.EndHorizontal ();
		}
	}
}
#endif