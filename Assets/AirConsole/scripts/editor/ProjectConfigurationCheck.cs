#if !DISABLE_AIRCONSOLE
using System;
using System.Linq;
using System.Reflection;
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
            PlayerSettings.resetResolutionOnWindowResize = true;
            
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

            if (PlayerSettings.stripEngineCode == false) {
                Debug.LogError("AirConsole requires 'Strip Engine Code' to be enabled in Player Settings.\n"
                               + $"We are updating the settings now.");
                PlayerSettings.stripEngineCode = true;
            }

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
        private static void EnsureWebGLPlayerSettings() {
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.nameFilesAsHashes = false; // We upload into timestamp based folders. This is not necessary.

            if (PlayerSettings.WebGL.dataCaching) {
                Debug.LogWarning("AirConsole requires 'Data Caching' to be disabled to avoid interference with automotive requirements.\n"
                                 + "Updating the WebGL settings now.");
                PlayerSettings.WebGL.dataCaching = false;
            }

            if (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Disabled) {
                Debug.LogWarning("AirConsole requires 'Data Caching' to be disabled to avoid interference with automotive requirements.\n"
                                 + "Adapting the WebGL settings now.");
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            }

            if (PlayerSettings.WebGL.memoryGrowthMode != WebGLMemoryGrowthMode.None) {
                Debug.LogWarning(
                    "For performance and stability on automotive, AirConsole requires 'Memory Growth Mode' to be set to None in WebGL PlayerSettings with the games maximum memory usage set.\n"
                    + "Updating the WebGL settings now.");
                PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.None;
                PlayerSettings.WebGL.initialMemorySize = Mathf.Min(512, Mathf.Max(PlayerSettings.WebGL.initialMemorySize, PlayerSettings.WebGL.maximumMemorySize));
            }

            if (PlayerSettings.WebGL.memorySize > 512) {
                Debug.LogWarning("AirConsole recommends 'Initial Memory Size' stay at or below 512MB for automotive compatibility.\n"
                                 + "We are updating the WebGL settings now.");
                PlayerSettings.WebGL.initialMemorySize = 512;
            }
            
            if (!IsDesirableTextureCompressionFormat(BuildTargetGroup.WebGL)) {
                Debug.LogError("AirConsole requires 'ASTC' or 'ETC2' as the texture compression format.");
                throw new UnityException("Please update the WebGL build and player settings to continue.");
            }
        }

        [InitializeOnLoadMethod]
        private static void EnsureAndroidPlayerSettings() {

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

            if (!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android)) {
                Debug.LogWarning("To ensure optimal performance and thermal load, 'Multithreaded rendering' is enabled now.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            }

            if (!IsDesirableTextureCompressionFormat(BuildTargetGroup.Android)) {
                Debug.LogError("AirConsole requires 'ASTC' or 'ETC2' as the texture compression format.");
                throw new UnityException("Please update the Build and Player settings to continue.");
            }

            UpdateAndroidPlayerSettingsInProperties();
            EnsureAndroidPlatformSettings();
            DisableUndesirableAndroidFeatures();
        }

        private static void EnsureAndroidPlatformSettings() {
            PlayerSettings.Android.forceInternetPermission = true;

            // To ensure Google Play compatibility, we require a target SDK of 34 or higher.
            const int requiredAndroidTargetSdk = 34;
            if ((int)PlayerSettings.Android.targetSdkVersion < requiredAndroidTargetSdk) {
                Debug.LogError(
                    $"AirConsole requires 'Target SDK Version' of {requiredAndroidTargetSdk} or higher.\n"
                    + "We are updating the Android settings now.");
            }

            PlayerSettings.Android.renderOutsideSafeArea = true; // required for the webview

            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)requiredAndroidTargetSdk;
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23) {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            }
            
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            if (PlayerSettings.Android.fullscreenMode != FullScreenMode.FullScreenWindow) {
                Debug.LogWarning("AirConsole requires 'Fullscreen Mode' to be set to FullScreenWindow in Android PlayerSettings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
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

            if (PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto) {
                Debug.LogWarning("AirConsole recommends 'Preferred Install Location' to be set to Auto in Android PlayerSettings.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            }
        }

        private static void DisableUndesirableAndroidFeatures() {
            if (PlayerSettings.allowUnsafeCode) {
                Debug.LogError("AirConsole does not allow for unsafe code to ensure games can be made available on Automotive platforms.\n"
                               + "We are updating the Android settings now.");
                PlayerSettings.allowUnsafeCode = false;
            }

            PlayerSettings.Android.ARCoreEnabled = false;
            PlayerSettings.Android.androidTargetDevices = AndroidTargetDevices.PhonesTabletsAndTVDevicesOnly;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.chromeosInputEmulation = false;
        }

        private static void UpdateAndroidPlayerSettingsInProperties() {
            SerializedObject playerSettings = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);

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