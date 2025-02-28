#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NDream.AirConsole.Editor
{
    public class PreBuildProcessing : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;

        private const string ANDROID_MANIFEST_PATH = "Assets/Plugins/Android/AndroidManifest.xml";

        private const string UNITY6_ANDROID_ACTIVITY_NAME = "com.unity3d.player.UnityPlayerGameActivity";
        private const string UNITY_ANDROID_ACTIVITY_NAME = "com.unity3d.player.UnityPlayerActivity";

        private const string UNITY6_ANDROID_ACTIVITY_THEME = "@style/BaseUnityGameActivityTheme";


        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) {
                ValidateAndroidManifest();
            } else if (report.summary.platform == BuildTarget.WebGL) {
                CheckWebGLSetup();
            }
        }


        private static void ValidateAndroidManifest()
        {
            string disabledManifestPath = Path.GetFullPath($"{ANDROID_MANIFEST_PATH}.DISABLED");
            string manifestPath = Path.GetFullPath(ANDROID_MANIFEST_PATH);

            if (File.Exists(disabledManifestPath))
            {
                File.Move(disabledManifestPath, manifestPath);
            }

            if (File.Exists(manifestPath))
            {
                XDocument manifest = XDocument.Load(manifestPath);

                if (manifest.Root == null) return;

                XElement[] applicationElements = manifest.Root.Elements("application").ToArray();
                if (!applicationElements.Any()) return;

                XElement[] activityElements = applicationElements.Elements("activity").ToArray();
                if (!activityElements.Any()) return;

                XName nameAttribute = XName.Get("name", "http://schemas.android.com/apk/res/android");
                XName themeAttribute = XName.Get("theme", "http://schemas.android.com/apk/res/android");

                foreach (XElement activityElement in activityElements)
                {
                    XAttribute name = activityElement.Attribute(nameAttribute);
                    if (name == null)
                        continue;

                    string activityName = name.Value;

                    if (activityName == UNITY6_ANDROID_ACTIVITY_NAME || activityName == UNITY_ANDROID_ACTIVITY_NAME)
                    {
#if UNITY_6000_0_OR_NEWER
                        name.Value = UNITY6_ANDROID_ACTIVITY_NAME;

                        XAttribute theme = activityElement.Attribute(themeAttribute);
                        if (theme == null)
                        {
                            activityElement.SetAttributeValue(themeAttribute, UNITY6_ANDROID_ACTIVITY_THEME);
                        }
#else
                    name.Value = UNITY_ANDROID_ACTIVITY_NAME;
#endif

                        break;
                    }
                }

                manifest.Save(manifestPath);
            }
            else
            {
                throw new UnityException(
                    $"{ANDROID_MANIFEST_PATH} does not exist. AirConsole for Android TV requires specific settings. Please reimport the AirConsole package to recreate the correct AndroidManifest.");
            }
        }

        private static void CheckWebGLSetup()
        {
#if UNITY_WEBGL
            if (string.IsNullOrEmpty(PlayerSettings.WebGL.template))
            {
                EditorUtility.DisplayDialog("Error", "No WebGL Template configured", "Cancel");
                throw new UnityException("WebGL template not configured");
            }

            if (Directory.Exists(GetWebGLTemplateDirectory()))
            {
                string templatePath = GetWebGLTemplateDirectory();
                if (!Directory.GetFiles(templatePath).Any(filename => filename.EndsWith("controller.html")))
                {
                    EditorUtility.DisplayDialog("Error",
                        "The controller has not yet been generated. Please execute the game at least once in play mode.",
                        "Cancel");
                    throw new UnityException("Controller missing in WebGL template location.");
                }

                if (!Directory.GetFiles(templatePath).Any(filename => filename.EndsWith("airconsole-unity-plugin.js")))
                {
                    EditorUtility.DisplayDialog("Error",
                        "airconsole-unity-plugin missing. Please set up your airconsole plugin again",
                        "Cancel");
                    throw new UnityException("Unity template incomplete");
                }
            }
#endif
        }

        private static string GetWebGLTemplateDirectory()
        {
            return Path.GetFullPath("Assets/WebGLTemplates/" + PlayerSettings.WebGL.template.Split(':')[1]);
        }
    }
}
#endif
