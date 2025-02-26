#if !DISABLE_AIRCONSOLE
#if !UNITY_ANDROID
#undef AIRCONSOLE_AUTOMOTIVE
#endif

using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDream.AirConsole.Editor {
    public abstract class UnityVersionCheck {
        [InitializeOnLoadMethod]
        private static void CheckUnityVersions() {
            if (IsSupportedUnityVersion()) {
                return;
            }

            EditorUtility.DisplayDialog("Unsupported", $"AirConsole {Settings.VERSION} requires Unity 2022.3 or newer",
                "I understand");
            EditorApplication.isPlaying = false;
        }

    public static bool IsSupportedUnityVersion() {
#if !UNITY_2022_3_OR_NEWER && !UNITY_6000_0_OR_NEWER
            return false; 
#endif
            return true;
        }
    }

    public abstract class ProjectConfigurationCheck : IPreprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            CheckSettings(report.summary.platform);
        }

        public static void CheckSettings(BuildTarget platform) {
            CheckGeneralPlayerSettings();

            switch (platform) {
                case BuildTarget.Android:
                    CheckAndroidPlayerSettings();
                    break;

                case BuildTarget.WebGL:
                    CheckWebGLPlayerSettings();
                    break;

                default:
                    throw new UnityException($"AirConsole Plugin does not support platform {platform}");
            }
            
            Debug.Log("AirConsole Plugin configuration checks completed successfully.");
        }

        [InitializeOnLoadMethod]
        private static void CheckGeneralPlayerSettings() {
            if (!UnityVersionCheck.IsSupportedUnityVersion()) {
                Debug.LogError("AirConsole Unity Plugin 2.6.0 and above require Unity 2022.3 LTS or newer");
                throw new UnityException("Unity Version " + Application.unityVersion);
            }
            
            bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
            if (PlayerSettings.runInBackground != shouldRunInBackground) {
                Debug.Log(
                    $"AirConsole needs 'Run In Background' to be {shouldRunInBackground} in PlayerSettings for {EditorUserBuildSettings.activeBuildTarget}.\n"
                    + $"Updating the settings now.");
                PlayerSettings.runInBackground = shouldRunInBackground;
            }

            // TODO(Marc): These should be done in production builds only
            if (PlayerSettings.stripEngineCode == false) {
                Debug.LogError("AirConsole requires 'Strip Engine Code' to be enabled in Player Settings.\n"
                               + $"We are updating the settings now.");
                PlayerSettings.stripEngineCode = true;
            }

            // TODO(Marc): These should be done in production builds only
            if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android) == ManagedStrippingLevel.Disabled) {
                Debug.LogWarning("AirConsole requires 'Managed Stripping Level' to be enabled in Player Settings with at minimum Low.\n"
                                 + $"We are updating the settings now.");
                PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            }

            if (PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) != ScriptingImplementation.IL2CPP) {
                Debug.LogWarning("AirConsole requires 'Scripting Backend' to be set to IL2CPP in Player Settings.\n"
                                 + $"We are updating the settings now.");
                PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, ScriptingImplementation.IL2CPP);
            }
        }

        [InitializeOnLoadMethod]
        private static void CheckWebGLPlayerSettings() {
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;

            EnforceOpenGLESSettings();
        }

        [InitializeOnLoadMethod]
        private static void CheckAndroidPlayerSettings() {
            EnforceAndroidPlayerSettings();
            EnforceAndroidTVSettings();
            EnforceAndroidAutomotiveSettings();
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
            
            // TODO(android-native): this is a test to see if this help with the incorrect resolution when the bottom bar comes in and goes out again in fullscreen windowed mode.
            PlayerSettings.resetResolutionOnWindowResize = true;
            PlayerSettings.Android.renderOutsideSafeArea = true; // required for the webview
            
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)requiredAndroidTargetSdk;
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23) {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            }
                

            // TODO(android-native): At this point we do not have reason to assume that APK expansion files will be supported on car, but how can we deal with this between car and tv?
#if AIRCONSOLE_AUTOMOTIVE
            PlayerSettings.Android.useAPKExpansionFiles = false;
            PlayerSettings.use32BitDisplayBuffer = true;
#endif

            
            PlayerSettings.Android.ARCoreEnabled = false;
            PlayerSettings.Android.androidTargetDevices = AndroidTargetDevices.PhonesTabletsAndTVDevicesOnly;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.chromeosInputEmulation = false;

            UpdateAndroidPlayerSettings();

            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            if (PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto) {
                Debug.LogWarning("AirConsole recommends 'Preferred Install Location' to be set to Auto in Android PlayerSettings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            }

            if (PlayerSettings.Android.fullscreenMode != FullScreenMode.FullScreenWindow) {
                Debug.LogWarning("AirConsole requires 'Fullscreen Mode' to be set to FullScreenWindow in Android PlayerSettings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
            }

            if (!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android)) {
                Debug.LogWarning("AirConsole recommends 'Mobile Multithreaded Rendering' to be enabled in Android PlayerSettings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            }

            if (!PlayerSettings.Android.renderOutsideSafeArea) {
                Debug.LogWarning("AirConsole recommends 'Render Outside Safe Area' to be enabled in Android PlayerSettings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.renderOutsideSafeArea = true;
            }

            if (!PlayerSettings.Android.startInFullscreen) {
                Debug.LogWarning("AirConsole recommends 'Start In Fullscreen' to be enabled in the Android PlayerSettings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.startInFullscreen = true;
            }

            if (EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC) {
                Debug.LogWarning("AirConsole recommends 'ASTC' as the 'Texture Compression' for Android builds.\n"
                                 + "We are updating the Android settings now.");
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
                Debug.LogWarning("AirConsole for TV requires 'Target Architectures' to be set to ARMv7 and ARM64 in Player Settings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            }
#endif
            EnforceOpenGLESSettings();
        }

        private static void EnforceOpenGLESSettings() {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            if (PlayerSettings.GetUseDefaultGraphicsAPIs(activeBuildTarget)) {
                Debug.LogError(
                    "AirConsole for AndroidTV requires 'Auto Graphics API' to be disabled in Player Settings to enable OpenGL ES2.\n"
                    + "We are updating the Android settings now.");
                PlayerSettings.SetUseDefaultGraphicsAPIs(activeBuildTarget, false);
            }

            if (!PlayerSettings.GetGraphicsAPIs(activeBuildTarget).Contains(GraphicsDeviceType.OpenGLES2)) {
                Debug.LogWarning($"AirConsole for {activeBuildTarget} requires 'OpenGL ES2' to be enabled in Player Settings.\n"
                                 + "We append Open GL ES2 to the Android Graphics APIs now.");
                GraphicsDeviceType[] graphicsAPIs =
                    PlayerSettings.GetGraphicsAPIs(activeBuildTarget).Append(GraphicsDeviceType.OpenGLES2).ToArray();
                PlayerSettings.SetGraphicsAPIs(activeBuildTarget, graphicsAPIs);
            }
        }

        private static void EnforceAndroidAutomotiveSettings() {
#if AIRCONSOLE_AUTOMOTIVE
            if (PlayerSettings.allowUnsafeCode) {
                Debug.LogError("AirConsole for Automotive requires 'Allow Unsafe Code' to be disabled in Player Settings.\n"
                               + "We are updating the Android settings now.");
                PlayerSettings.allowUnsafeCode = false;
            }
            
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != AndroidArchitecture.ARM64) {
                Debug.LogWarning(
                    "AirConsole for Automotive requires 'Target Architectures' to be set to ARM64 in Player Settings.\n"
                    + "We are updating the Android settings now.");
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            }

            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android)
                && !PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Contains(GraphicsDeviceType.Vulkan)) {
                Debug.LogWarning("AirConsole requires 'Vulkan' or AutoGraphics API to be enabled in Player Settings for Automotive.\n"
                                 + "Prepending Vulkan for Android Graphics APIs now.");
                GraphicsDeviceType[] graphicsAPIs =
                    PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Prepend(GraphicsDeviceType.Vulkan).ToArray();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsAPIs);
            }
#endif
            EnforceVulkanSettings();
        }

        private static void EnforceVulkanSettings() {
#if AIRCONSOLE_AUTOMOTIVE
            if (PlayerSettings.vulkanNumSwapchainBuffers > 2) {
                Debug.LogWarning($"AirConsole recommends a maximum of 2 SwapChain Buffers for Vulkan on Automotive for best sustained performance and input latency.\n"
                                 + $"We are updating the Android settings now.");
                PlayerSettings.vulkanNumSwapchainBuffers = 2;
            }

            if (!PlayerSettings.vulkanEnableLateAcquireNextImage) {
                Debug.LogWarning($"AirConsole recommends enabling 'Late Acquire Next Image' for Vulkan on Automotive for best sustained performance.\n"
                                 + $"We are updating the Android settings now.");
                PlayerSettings.vulkanEnableLateAcquireNextImage = true;
            }
#endif
        }
    }
}
#endif