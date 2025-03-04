#if !DISABLE_AIRCONSOLE
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NDream.AirConsole.Editor {
    public class PreBuildProcessing : IPreprocessBuildWithReport {
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report) {
            CheckWebGLSetup();
        }

        private static void CheckWebGLSetup() {
#if UNITY_WEBGL 
            if (string.IsNullOrEmpty(PlayerSettings.WebGL.template)) {
                EditorUtility.DisplayDialog("Error", "No WebGL Template configured", "Cancel");
                throw new UnityException("WebGL template not configured");
            }

            if (Directory.Exists(GetWebGLTemplateDirectory())) {
                string templatePath = GetWebGLTemplateDirectory();
                if (!Directory.GetFiles(templatePath).Any(filename => filename.EndsWith("controller.html"))) {
                    EditorUtility.DisplayDialog("Error",
                        "The controller has not yet been generated. Please execute the game at least once in play mode.",
                        "Cancel");
                    throw new UnityException("Controller missing in WebGL template location.");
                }

                if (!Directory.GetFiles(templatePath).Any(filename => filename.EndsWith("airconsole-unity-plugin.js"))) {
                    EditorUtility.DisplayDialog("Error",
                        "airconsole-unity-plugin missing. Please set up your airconsole plugin again",
                        "Cancel");
                    throw new UnityException("Unity template incomplete");
                }
            }
#endif
        }

        private static string GetWebGLTemplateDirectory() {
            return Path.GetFullPath("Assets/WebGLTemplates/" + PlayerSettings.WebGL.template.Split(':')[1]);
        }
    }
}
#endif
#endif