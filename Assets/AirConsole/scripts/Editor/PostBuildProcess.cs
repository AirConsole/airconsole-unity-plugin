#if !DISABLE_AIRCONSOLE
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
				// Check if screen.html already exists
				if (File.Exists (pathToBuiltProject + "/screen.html")) {
					File.Delete (pathToBuiltProject + "/screen.html");
				}

				// Renaming index.html to screen.html
				File.Move (pathToBuiltProject + "/index.html", pathToBuiltProject + "/screen.html");

				// Check if game.json already exists
				if (File.Exists (pathToBuiltProject + "/Build/game.json")) {
				  File.Delete (pathToBuiltProject + "/Build/game.json");
				}

				string configuration_file_path = pathToBuiltProject + "/Build/" + Path.GetFileName (pathToBuiltProject) + ".json";

				// Rename JSON configuration to game.json (Only for Unity versions < 2020.x)
				// See https://forum.unity.com/threads/changes-to-the-webgl-loader-and-templates-introduced-in-unity-2020-1.817698/
				// for details, the build config is no longer stored in a JSON file but embedded into the HTML
				if (File.Exists (configuration_file_path)) {
					File.Move (configuration_file_path, pathToBuiltProject + "/Build/game.json");
				}

				// Save last port path
				EditorPrefs.SetString ("airconsolePortPath", pathToBuiltProject);
			}
		}
	}
}
#endif
