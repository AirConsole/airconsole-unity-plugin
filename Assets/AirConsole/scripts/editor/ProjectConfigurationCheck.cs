#if !DISABLE_AIRCONSOLE
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NDream.AirConsole.Editor {
    public abstract class UnityVersionCheck {
        [InitializeOnLoadMethod]
        private static void CheckUnityVersions() {
            if (IsSupportedUnityVersion()) {
                return;
            }

            string message = $"AirConsole {Settings.VERSION} requires Unity 2022.3 or newer";
            EditorUtility.DisplayDialog("Unsupported", message, "I understand");
            Debug.LogError(message);
            throw new UnityException(message);
        }

        public static bool IsSupportedUnityVersion() {
#if !UNITY_2022_3_OR_NEWER && !UNITY_6000_0_OR_NEWER
            return false;
#endif
            return true;
        }
    }

    public abstract class UnityPlatform {
        [InitializeOnLoadMethod]
        private static void CheckPlatform() {
            BuildTargetGroup buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            if(buildTarget == BuildTargetGroup.Android || buildTarget == BuildTargetGroup.WebGL) {
                return;
            }

            Debug.LogWarning($"AirConsole Plugin does not support platform {buildTarget}, switching to WebGL.\n"
                + "To disable AirConsole for this build, add the scripting define symbol 'DISABLE_AIRCONSOLE' in the Player Settings.");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }
    }

    public abstract class ProjectConfigurationCheck : IPreprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            CheckSettings(report.summary.platform);
        }

        public static void CheckSettings(BuildTarget platform) {
            EnsureSharedPlayerSettings();

            switch (platform) {
                case BuildTarget.Android:
                    EnsureAndroidPlayerSettings();
                    EnsureAndroidRenderSettings();
                    break;

                case BuildTarget.WebGL:
                    EnsureWebGLPlayerSettings();
                    EnsureWebRenderSettings();
                    break;

                default:
                    throw new UnityException($"AirConsole Plugin does not support platform {platform}");
            }

            Debug.Log("AirConsole Plugin configuration checks completed successfully.");
        }

        [InitializeOnLoadMethod]
        private static void EnsureSharedPlayerSettings() {
            
            string expectedTemplateName = Settings.WEBTEMPLATE_PATH.Split('/').Last();
            string[] templateUri = PlayerSettings.WebGL.template.Split(':');
        }
        [InitializeOnLoadMethod]
        private static void EnsureWebGLPlayerSettings() {
            
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
        

        [InitializeOnLoadMethod]
        private static void EnsureAndroidPlayerSettings() {
        }

        private static void EnsureAndroidPlatformSettings() {
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
        private static void DisableUndesirableAndroidFeatures() {
            }
        }

            SerializedObject playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        private static void UpdateAndroidPlayerSettingsInProperties() {

            SerializedProperty filterTouchesProperty = playerSettings.FindProperty("AndroidFilterTouchesWhenObscured");
            filterTouchesProperty.boolValue = false;

            SerializedProperty androidGamePadSupportLevel = playerSettings.FindProperty("androidGamepadSupportLevel");
            androidGamePadSupportLevel.intValue = 0;

            playerSettings.ApplyModifiedProperties();
        }

        private static void EnsureWebRenderSettings() {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            #if !UNITY_6000_0_OR_NEWER
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(activeBuildTarget)) {
                Debug.LogError(
                    "AirConsole on web requires 'Auto Graphics API' to be disabled in Player Settings to enable WebGL1.\n"
                    + "Updating the settings now.");
                PlayerSettings.SetUseDefaultGraphicsAPIs(activeBuildTarget, false); 
            }

            if (!PlayerSettings.GetGraphicsAPIs(activeBuildTarget).Contains(GraphicsDeviceType.OpenGLES2)) {
                Debug.LogWarning($"AirConsole on web requires WebGL 1 to be enabled in Player Settings.\n"
                                 + "Appending WebGL1 to the graphics APIs now.");
                GraphicsDeviceType[] graphicsAPIs =
                    PlayerSettings.GetGraphicsAPIs(activeBuildTarget).Append(GraphicsDeviceType.OpenGLES2).ToArray();
                PlayerSettings.SetGraphicsAPIs(activeBuildTarget, graphicsAPIs);
            }
            #endif
        }
        
        private static void EnsureAndroidRenderSettings() {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            PlayerSettings.use32BitDisplayBuffer = true;

#if !UNITY_6000_0_OR_NEWER
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(activeBuildTarget)) {
                Debug.LogError(
                    "AirConsole for AndroidTV requires 'Auto Graphics API' to be disabled in Player Settings to enable OpenGL ES2.\n"
                    + "We are updating the Android settings now.");
                PlayerSettings.SetUseDefaultGraphicsAPIs(activeBuildTarget, false);
            }

            if (!PlayerSettings.GetGraphicsAPIs(activeBuildTarget).Contains(GraphicsDeviceType.OpenGLES2)) {
                Debug.LogWarning($"AirConsole on Android requires 'OpenGL ES2' to be enabled in Player Settings.\n"
                                 + "We append Open GL ES2 to the Android Graphics APIs now.");
                GraphicsDeviceType[] graphicsAPIs =
                    PlayerSettings.GetGraphicsAPIs(activeBuildTarget).Append(GraphicsDeviceType.OpenGLES2).ToArray();
                PlayerSettings.SetGraphicsAPIs(activeBuildTarget, graphicsAPIs);
            }
#endif

            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android)
                && PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).First() != GraphicsDeviceType.Vulkan) {
                Debug.LogWarning("AirConsole requires 'Vulkan' or AutoGraphics API to be enabled in Player Settings for Automotive.\n"
                                 + "Prepending Vulkan for Android Graphics APIs now.");
                GraphicsDeviceType[] graphicsAPIs =
                    PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)
                        .Where(api => api != GraphicsDeviceType.Vulkan)
                        .Prepend(GraphicsDeviceType.Vulkan)
                        .ToArray();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsAPIs);
            }

            if (PlayerSettings.vulkanNumSwapchainBuffers > 2) {
                Debug.LogWarning(
                    $"AirConsole recommends a maximum of 2 SwapChain Buffers for Vulkan for best sustained performance and low "
                    + $"input latency.\n"
                    + $"Updating the Player Settings now.");
                PlayerSettings.vulkanNumSwapchainBuffers = 2;
            }
        }
        
        [MenuItem("Tools/AirConsole/Check Android Config")]
        public static void CheckAndroid() {
            CheckSettings(BuildTarget.Android);
        }
        
        [MenuItem("Tools/AirConsole/Check Web Config")]
        public static void CheckWeb() {
            CheckSettings(BuildTarget.WebGL);
        }
        
        private static bool IsDesirableTextureCompressionFormat(BuildTargetGroup targetGroup) {
            TextureCompressionFormat format = GetDefaultTextureCompressionFormat(targetGroup);
            return format is TextureCompressionFormat.ASTC or TextureCompressionFormat.ETC2 &&
                   (targetGroup == BuildTargetGroup.Android 
                       ? EditorUserBuildSettings.androidBuildSubtarget is MobileTextureSubtarget.ASTC or MobileTextureSubtarget.ETC2
                       : EditorUserBuildSettings.webGLBuildSubtarget is WebGLTextureSubtarget.ASTC or WebGLTextureSubtarget.ETC2);
        }

        private static TextureCompressionFormat GetDefaultTextureCompressionFormat(BuildTargetGroup platform) {
            Type playerSettingsType = typeof(PlayerSettings);
    
            MethodInfo methodInfo = playerSettingsType.GetMethod(
                "GetDefaultTextureCompressionFormat", 
                BindingFlags.NonPublic | BindingFlags.Static);
    
            if (methodInfo != null)
            {
                return (TextureCompressionFormat)methodInfo.Invoke(null, new object[] { platform });
            }
            
            return TextureCompressionFormat.Unknown;
        }

        private static void SetPlayerSettingsTextureFormat(BuildTargetGroup platform, TextureCompressionFormat format) {
            Type playerSettingsType = typeof(PlayerSettings);
    
            MethodInfo methodInfo = playerSettingsType.GetMethod(
                "SetDefaultTextureCompressionFormat", 
                BindingFlags.NonPublic | BindingFlags.Static);
    
            if (methodInfo != null)
            {
                methodInfo.Invoke(null, new object[] { platform, (int)format });
            }
        }
            
        // Extracted from UnityEditor.TextureCompressionFormat
        private enum TextureCompressionFormat
        {
            Unknown,
            ETC,
            ETC2,
            ASTC,
            PVRTC,
            DXTC,
            BPTC,
            DXTC_RGTC,
        }
    }
}
#endif