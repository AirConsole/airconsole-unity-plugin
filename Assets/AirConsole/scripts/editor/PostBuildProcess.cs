#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

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

				// save last port path
				EditorPrefs.SetString ("airconsolePortPath", pathToBuiltProject);

				// modify Unity Loader
				string jsFile = File.ReadAllText (pathToBuiltProject + "/Release/UnityLoader.js");
				jsFile = "if (typeof Unity == 'undefined') {" + jsFile + "}";
				File.WriteAllText (pathToBuiltProject + "/Release/UnityLoader.js", jsFile);


			}
		}
	}
}
#endif
