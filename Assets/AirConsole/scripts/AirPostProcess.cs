#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class AirPostProcess {

    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {

        if (target == BuildTarget.WebGL) {

            // check if screen.html already exists
            if (System.IO.File.Exists(pathToBuiltProject + "/screen.html")) {
                System.IO.File.Delete(pathToBuiltProject + "/screen.html");
            }

            // rename index.html to screen.html
            System.IO.File.Move(pathToBuiltProject + "/index.html", pathToBuiltProject + "/screen.html");

        }

    }
}
#endif