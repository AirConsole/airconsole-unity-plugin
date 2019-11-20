﻿#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEditor.Build;

namespace NDream.AirConsole.Editor {
	public class PostBuildProcess {

		[PostProcessBuildAttribute(1)]
		public static void OnPostprocessBuild (BuildTarget target, string pathToBuiltProject) {

			if (target == BuildTarget.WebGL) {

				// check if screen.html already exists
				if (File.Exists (pathToBuiltProject + "/screen.html")) {
					File.Delete (pathToBuiltProject + "/screen.html");
				}

				// rename index.html to screen.html
				File.Move (pathToBuiltProject + "/index.html", pathToBuiltProject + "/screen.html");

				// check if game.json already exists
				if (File.Exists (pathToBuiltProject + "/Build/game.json")) {
					File.Delete (pathToBuiltProject + "/Build/game.json");
				}
				
				// rename json configuration to game.json
				File.Move (pathToBuiltProject + "/Build/" + Path.GetFileName (pathToBuiltProject) + ".json", pathToBuiltProject + "/Build/game.json");

				// save last port path
				EditorPrefs.SetString ("airconsolePortPath", pathToBuiltProject);

			} 
		}
	}
}
#endif
