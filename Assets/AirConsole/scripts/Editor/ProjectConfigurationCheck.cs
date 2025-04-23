#if !DISABLE_AIRCONSOLE
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NDream.AirConsole.Editor {
    public abstract class UnityVersionCheck {
        [InitializeOnLoadMethod]
        private static void CheckUnityVersions() {
#if !UNITY_2019_4_OR_NEWER
            EditorUtility.DisplayDialog("Unsupported", $"AirConsole {Settings.VERSION} requires Unity 2019.4 or newer",
                "I understand");
            EditorApplication.isPlaying = false;
#endif
        }
    }

    public abstract class ProjectConfigurationCheck : IPreprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            switch (report.summary.platform) {
                case BuildTarget.Android:
                    CheckAndroidPlayerSettings();
                    break;

                case BuildTarget.WebGL:
                    CheckWebGLPlayerSettings();
                    break;

                default:
                    throw new UnityException($"AirConsole Plugin does not support platform {report.summary.platform}");
            }
        }

#if UNITY_WEBGL
        [InitializeOnLoadMethod]
#endif
        private static void CheckWebGLPlayerSettings() {
            if (EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC) {
                Debug.LogWarning("AirConsole recommends 'ASTC' as the 'Texture Compression' for WebGL builds for improved mobile performance.");
            }
            
            string expectedTemplateName = Settings.WEBTEMPLATE_PATH.Split('/').Last();
            string[] templateUri = PlayerSettings.WebGL.template.Split(':');
            
            if (templateUri.Length != 2 || templateUri[0].ToUpper() == "APPLICATION" || (templateUri[1] != expectedTemplateName && Settings.TEMPLATE_NAMES.Contains(templateUri[1]))) {
                string incompatibleTemplateMessage =
                    $"Unity version \"{Application.unityVersion}\" needs the AirConsole WebGL template \"{expectedTemplateName}\" to work.\nPlease change the WebGL template in your Project Settings under Player (WebGL platform tab) > Resolution and Presentation > WebGL Template.";
                Debug.LogError(incompatibleTemplateMessage);
                
                if (EditorUtility.DisplayDialog("Incompatible WebGL Template", incompatibleTemplateMessage, "Open Player Settings", "Cancel"))
                {
                    SettingsService.OpenProjectSettings("Project/Player");
                }
            }
        }
        
#if UNITY_ANDROID
        [InitializeOnLoadMethod]
#endif
        private static void CheckAndroidPlayerSettings() {
            EnforceAndroidPlayerSettings();
            EnforceAndroidTVSettings();
        }

        private static void EnforceAndroidPlayerSettings() {
            // The internet permission is required for the AirConsole Unity Plugin.
            PlayerSettings.Android.forceInternetPermission = true;

            // To ensure Google Play compatibility, we require a target SDK of 34 or higher.
            const int requiredAndroidTargetSdk = 34;
            if ((int)PlayerSettings.Android.targetSdkVersion < requiredAndroidTargetSdk) {
                Debug.LogError(
                    $"AirConsole requires 'Target SDK Version' of {requiredAndroidTargetSdk} or higher.\n"
                    + $"We are updating the Android settings now.");
            }

            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)requiredAndroidTargetSdk;

            PlayerSettings.Android.ARCoreEnabled = false;
            PlayerSettings.Android.androidIsGame = true;

            UpdateAndroidPlayerSettings();

            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            if (EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC) {
                Debug.LogWarning("AirConsole recommends 'ASTC' as the 'Texture Compression' for Android builds for improved mobile performance.");
            }
        }

        private static void UpdateAndroidPlayerSettings() {
            SerializedObject playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);

            SerializedProperty filterTouchesProperty = playerSettings.FindProperty("AndroidFilterTouchesWhenObscured");
            filterTouchesProperty.boolValue = false;

            SerializedProperty androidGamePadSupportLevel = playerSettings.FindProperty("androidGamepadSupportLevel");
            androidGamePadSupportLevel.intValue = 0;

            playerSettings.ApplyModifiedProperties();
        }

        private static void EnforceAndroidTVSettings() {
            PlayerSettings.Android.androidTVCompatibility = true;
#if UNITY_ANDROID
            if((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != AndroidArchitecture.ARM64 
               || (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) != AndroidArchitecture.ARMv7) {
                Debug.LogWarning("AirConsole for TV requires 'Target Architectures' to be set to ARMv7 and ARM64 in Player Settings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            }
#endif
        }

    }
}
#endif