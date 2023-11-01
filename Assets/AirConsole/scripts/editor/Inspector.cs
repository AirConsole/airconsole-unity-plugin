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
		private bool translationValue;
		private const string TRANSLATION_ACTIVE = "var AIRCONSOLE_TRANSLATION = true;";
		private const string TRANSLATION_INACTIVE = "var AIRCONSOLE_TRANSLATION = false;";

		public void Awake()
		{
			string path = Application.dataPath + Settings.WEBTEMPLATE_PATH + "/translation.js";
			if (System.IO.File.Exists(path))
			{
				translationValue = System.IO.File.ReadAllText(path).Equals(TRANSLATION_ACTIVE);
			}

		}

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

			// show default inspector property editor withouth script reference
			serializedObject.Update ();
	
			EditorGUILayout.PropertyField(serializedObject.FindProperty("controllerHtml"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("autoScaleCanvas"));
			DrawTranslationsToggle();
	
			EditorGUILayout.PropertyField(serializedObject.FindProperty("androidTvGameVersion"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("androidUIResizeMode"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("webViewLoadingSprite"));
			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("browserStartMode"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("devGameId"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("LocalIpOverride"));
			
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

		private void DrawTranslationsToggle() {
			//translation bool
			bool oldTranslationValue = translationValue;
			translationValue = EditorGUILayout.Toggle("Translation", translationValue);
			if(oldTranslationValue != translationValue) {
				string path = Application.dataPath + Settings.WEBTEMPLATE_PATH + "/translation.js";

				if(translationValue) {
					System.IO.File.WriteAllText(path, TRANSLATION_ACTIVE);
				} else {
					System.IO.File.WriteAllText(path, TRANSLATION_INACTIVE);
				}
			}
		}
	}
}
#endif