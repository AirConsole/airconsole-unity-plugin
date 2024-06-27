using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDream.AirConsole.Editor {
    public abstract class UnityVersionCheck {
        [InitializeOnLoadMethod]
        private static void CheckVersions() {
#if !UNITY_2021_3_OR_NEWER && !UNITY_2022_3_OR_NEWER
            EditorUtility.DisplayDialog("Unsupported", $"AirConsole Unity Plugin {Settings.VERSION} requires Unity 2021.3 or 2022.3",
                "I understand");
            EditorApplication.isPlaying = false;
#endif

#if UNITY_6 || UNITY_6_OR_NEWER 
            EditorUtility.DisplayDialog("Unity 6 and newer are not allowed",
                $"AirConsole Unity Plugin {Settings.VERSION} does not allow games to be built with Unity 6", "I understand");
            EditorApplication.isPlaying = false;
#endif
        } 
    }
    
    public abstract class CompatibilityCheck : IPreprocessBuildWithReport {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report) {
            CheckGeneralPlayerSettings();

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
        
        [InitializeOnLoadMethod]
        private static void CheckGeneralPlayerSettings() {
            // The internet permission is required for the AirConsole Unity Plugin so we enforce it by default
            PlayerSettings.Android.forceInternetPermission = true;
            
            // TODO(Marc): These should be done in production builds only
            if (PlayerSettings.stripEngineCode == false) {
                Debug.LogError("AirConsole Unity Plugin requires 'Strip Engine Code' to be enabled in Player Settings. Code Stripping has been enabled again.");
                PlayerSettings.stripEngineCode = true;
            }

            // TODO(Marc): These should be done in production builds only
            if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android) == ManagedStrippingLevel.Disabled) {
                Debug.LogWarning("AirConsole Unity Plugin requires 'Managed Stripping Level' to be enabled in Player Settings with at minimum Low. Switching stripping level to low.");
                PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            }

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP) {
                Debug.LogWarning("AirConsole requires 'Scripting Backend' to be set to IL2CPP in Player Settings. We are updating the Android settings now.");
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);    
            }
        }

        [InitializeOnLoadMethod]
        private static void CheckWebGLPlayerSettings() {
            EnforceOpenGLESSettings();
        }
        [InitializeOnLoadMethod]
        private static void CheckAndroidPlayerSettings() {
            EnforceAndroidPlayerSettings();
            EnforceAndroidTVSettings();
            EnforceAndroidAutomotiveSettings();
        }

        private static void EnforceAndroidPlayerSettings() {
            const int requiredAndroidTargetSdk = 34;
            
            if ((int)PlayerSettings.Android.targetSdkVersion < requiredAndroidTargetSdk) {
                Debug.LogError($"AirConsole Unity Plugin requires 'Target SDK Version' to be set to {requiredAndroidTargetSdk} or higher in Android PlayerSettings. We are updating the Android settings now.");
            }
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)requiredAndroidTargetSdk;

            PlayerSettings.Android.ARCoreEnabled = false;
            PlayerSettings.Android.androidTargetDevices= AndroidTargetDevices.PhonesTabletsAndTVDevicesOnly;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.chromeosInputEmulation = false;

            UpdateAndroidPlayerSettings();
            
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            
            if (PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto) {
                Debug.LogWarning("AirConsole Unity Plugin recommends 'Preferred Install Location' to be set to Auto in Android PlayerSettings. We are updating the Android settings now.");
                PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            }
            
            if (PlayerSettings.Android.fullscreenMode != FullScreenMode.FullScreenWindow) {
                Debug.LogWarning("AirConsole Unity Plugin requires 'Fullscreen Mode' to be set to FullScreenWindow in Android PlayerSettings. We are updating the Android settings now.");
                PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
            }

            if (!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android)) {
                Debug.LogWarning("AirConsole Unity Plugin recommends 'Mobile Multithreaded Rendering' to be enabled in Android PlayerSettings. We are updating the Android settings now.");
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            }

            if (!PlayerSettings.Android.renderOutsideSafeArea) {
                Debug.LogWarning("AirConsole Unity Plugin recommends 'Render Outside Safe Area' to be enabled in Android PlayerSettings. We are updating the Android settings now.");
                PlayerSettings.Android.renderOutsideSafeArea = true;
            }

            if (!PlayerSettings.Android.startInFullscreen) {
                Debug.LogWarning("AirConsole Unity Plugin recommends 'Start In Fullscreen' to be enabled in the Android PlayerSettings. We are updating the Android settings now.");
                PlayerSettings.Android.startInFullscreen = true;
            }
            
            if (EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC) {
                Debug.LogWarning(
                    "AirConsole Unity Plugin recommends 'ASTC' as the 'Texture Compression' for Android builds. We are updating the Android settings now.");
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            }
        }

        private static void UpdateAndroidPlayerSettings() {
            SerializedObject playerSettings = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);

            SerializedProperty filterTouchesProperty = playerSettings.FindProperty("AndroidFilterTouchesWhenObscured");
            filterTouchesProperty.boolValue = false;

            SerializedProperty sustainedPerformanceProperty = playerSettings.FindProperty("AndroidEnableSustainedPerformanceMode");
            sustainedPerformanceProperty.boolValue = true;

            SerializedProperty androidGamePadSupportLevel = playerSettings.FindProperty("androidGamepadSupportLevel");
            androidGamePadSupportLevel.intValue = 0;

            playerSettings.ApplyModifiedProperties();
        }
        
        private static void EnforceAndroidTVSettings() {
            PlayerSettings.Android.androidTVCompatibility = true;
#if UNITY_ANDROID && !AIRCONSOLE_AUTOMOTIVE
            if((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != AndroidArchitecture.ARM64 
               || (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) != AndroidArchitecture.ARMv7) {
                Debug.LogWarning("AirConsole Unity Plugin for TV requires 'Target Architectures' to be set to ARMv7 and ARM64 in Player Settings. We are updating the Android settings now.");
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            }

            EnforceOpenGLESSettings();
#endif
        }

        private static void EnforceOpenGLESSettings() {
#if UNITY_WEBGL
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android)) {
                Debug.LogError("AirConsole Unity Plugin for AndroidTV requires 'Auto Graphics API' to be disabled in Player Settings to enable OpenGL ES2. We are updating the Android settings now.");
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            }

            Debug.LogWarning("AirConsole Unity Plugin for AndroidTV requires 'OpenGL ES 2' to be enabled in Player Settings."
                             + "Unless AutoGraphics API is active, we prepend it for Android targets.");
            GraphicsDeviceType[] graphicsAPIs =
                PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Append(GraphicsDeviceType.OpenGLES2).ToArray();
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsAPIs);
#endif
        }

        private static void EnforceAndroidAutomotiveSettings() {
#if UNITY_ANDROID && AIRCONSOLE_AUTOMOTIVE
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != AndroidArchitecture.ARM64) {
                Debug.LogWarning(
                    "AirConsole Unity Plugin for Automotive requires 'Target Architectures' to be set to ARM64 in Player Settings. We are updating the Android settings now.");
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            }
            
            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) && !PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Contains(GraphicsDeviceType.Vulkan)) {
                Debug.LogWarning("AirConsole Unity Plugin requires 'Vulkan' to be enabled in Player Settings. Unless AutoGraphics API is active, we prepend it for Android targets.");
                GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Prepend(GraphicsDeviceType.Vulkan).ToArray();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsAPIs);
            }

            EnforceVulkanSettings();
#endif
        }

        private static void EnforceVulkanSettings() {
#if AIRCONSOLE_AUTOMOTIVE
            if (PlayerSettings.vulkanNumSwapchainBuffers > 2) {
                Debug.LogWarning(
                    $"AirConsole recommends a maximum of 2 SwapChain Buffers for Vulkan on Automotive for best sustained performance. We are updating the Android settings now.");
                PlayerSettings.vulkanNumSwapchainBuffers = 2;
            }

            if (!PlayerSettings.vulkanEnableLateAcquireNextImage) {
                Debug.LogWarning(
                    $"AirConsole recommends enabling 'Late Acquire Next Image' for Vulkan on Automotive for best sustained performance. We are updating the Android settings now.");
                PlayerSettings.vulkanEnableLateAcquireNextImage = true;
            }
#endif
        }
    }
}