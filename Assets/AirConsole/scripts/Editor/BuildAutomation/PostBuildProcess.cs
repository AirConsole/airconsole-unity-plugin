#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Editor {
    using UnityEditor;
    using UnityEditor.Callbacks;
    using System.IO;

    public class PostBuildProcess {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
            if (target == BuildTarget.WebGL) {
                // Copy index.html to screen.html and overwrite it if necessary.
                File.Copy(pathToBuiltProject + "/index.html", pathToBuiltProject + "/screen.html", true);

                // Save last port path
                EditorPrefs.SetString("airconsolePortPath", pathToBuiltProject);
            }
        }
    }
}
#endif