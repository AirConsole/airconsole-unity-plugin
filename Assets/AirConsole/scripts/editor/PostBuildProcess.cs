#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

namespace NDream.AirConsole.Editor {
    public class PostBuildProcess {

        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {

            if (target == BuildTarget.WebGL) {

                // check if screen.html already exists
                if (File.Exists(pathToBuiltProject + "/screen.html")) {
                    File.Delete(pathToBuiltProject + "/screen.html");
                }

                // rename index.html to screen.html
                File.Move(pathToBuiltProject + "/index.html", pathToBuiltProject + "/screen.html");

                // save last port path
                EditorPrefs.SetString("airconsolePortPath", pathToBuiltProject);
            }

            if (target == BuildTarget.Android) {

                string pathFolder = Path.GetDirectoryName(pathToBuiltProject);
                string fileName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
                pathFolder += "/" + fileName + "_ScreenData";

                // move screen.html & unity plugin to 
                DirectoryCopy(Application.dataPath + Settings.WEBTEMPLATE_PATH, pathFolder, true);

                // check if screen.html already exists
                if (File.Exists(pathFolder + "/screen.html")) {
                    File.Delete(pathFolder + "/screen.html");
                }

                // rename index.html to screen.html
                File.Move(pathFolder + "/index.html", pathFolder + "/screen.html");

                // add bundleId to javascript file
                string jsFile = File.ReadAllText(pathFolder + "/airconsole-unity-plugin.js");
                jsFile = jsFile.Replace("var bundleId;", "var bundleId = \"" + PlayerSettings.bundleIdentifier +"\";");
                File.WriteAllText(pathFolder + "/airconsole-unity-plugin.js", jsFile);
            }


        }


        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName)) {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs) {
                foreach (DirectoryInfo subdir in dirs) {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
#endif
