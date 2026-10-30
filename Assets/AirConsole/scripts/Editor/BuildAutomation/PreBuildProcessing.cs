#if !DISABLE_AIRCONSOLE && UNITY_EDITOR

namespace NDream.AirConsole.Editor {
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    public class PreBuildProcessing : IPreprocessBuildWithReport {
        public int callbackOrder {
            get => 1;
        }

        public void OnPreprocessBuild(BuildReport report) {
            if (report.summary.platform == BuildTarget.WebGL) {
                CheckWebGLSetup();
            }
        }

        internal static void CheckWebGLSetup() {
            if (string.IsNullOrEmpty(PlayerSettings.WebGL.template)) {
                EditorUtility.DisplayDialog("Error", "No WebGL Template configured", "Cancel");
                throw new BuildFailedException("WebGL template not configured");
            }

            // ProjectConfigurationCheck runs after this check. Without this, a wrong template fails on its missing controller.html.
            ProjectConfigurationCheck.SelectWebGLTemplateIfUnityBuiltIn();
            if (!ProjectConfigurationCheck.IsCompatibleWebGLTemplate(PlayerSettings.WebGL.template)) {
                throw new BuildFailedException(ProjectConfigurationCheck.IncompatibleWebGLTemplateMessage);
            }

            if (Directory.Exists(GetWebGLTemplateDirectory())) {
                string templatePath = GetWebGLTemplateDirectory();
                if (!Directory.GetFiles(templatePath).Any(filename => filename.EndsWith("controller.html"))) {
                    EditorUtility.DisplayDialog("Error",
                        "The controller has not yet been generated. Please execute the game at least once in play mode.",
                        "OK");
                    throw new BuildFailedException("Controller missing in WebGL template location.");
                }

                if (!Directory.GetFiles(templatePath)
                        .Any(filename => filename.EndsWith("airconsole-unity-plugin.js"))) {
                    EditorUtility.DisplayDialog("Error",
                        "airconsole-unity-plugin missing. Please set up your airconsole plugin again",
                        "Cancel");
                    throw new BuildFailedException("Unity template incomplete");
                }
            }
        }

        internal static string GetWebGLTemplateDirectory() =>
            Path.GetFullPath("Assets/WebGLTemplates/" + PlayerSettings.WebGL.template.Split(':')[1]);
    }
}
#endif
