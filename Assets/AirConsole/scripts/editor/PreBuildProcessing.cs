#if !DISABLE_AIRCONSOLE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NDream.AirConsole.Editor {
    public class PreBuildProcessing : IPreprocessBuildWithReport {
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report) {
            CheckWebGLSetup();

            Debug.Log("Used Python path: " + Environment.GetEnvironmentVariable("EMSDK_PYTHON"));

            // In case you get a Build exception from Unity such as:
            //   System.ComponentModel.Win32Exception (2): No such file or directory)
            // Make sure that the correct Python version is installed and can be found by Unity during
            // the Build process.

            // If you need to set the Python path manually you can use the code below, uncomment it and
            // set "EMSDK_PYTHON" to the the Python 3 (Or Python 2 for old Unity versions) path:
#if !UNITY_2020_1_OR_NEWER && UNITY_EDITOR_OSX
        System.Environment.SetEnvironmentVariable("EMSDK_PYTHON", Settings.Python2Path);
#endif
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