using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace NDream.AirConsole.Editor {
    public class PostBuildProcess {

        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {

            if (target == BuildTarget.WebGL) {

                // check if screen.html already exists
                if (System.IO.File.Exists(pathToBuiltProject + "/screen.html")) {
                    System.IO.File.Delete(pathToBuiltProject + "/screen.html");
                }

                // rename index.html to screen.html
                System.IO.File.Move(pathToBuiltProject + "/index.html", pathToBuiltProject + "/screen.html");

                // save last port path
                EditorPrefs.SetString("airconsolePortPath", pathToBuiltProject);
            }
        }
    }
}
