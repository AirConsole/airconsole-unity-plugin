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
            if (buildTarget == BuildTargetGroup.Android || buildTarget == BuildTargetGroup.WebGL) {
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
 
            if (PlayerSettings.allowUnsafeCode) {
                Debug.LogError("AirConsole does not allow for unsafe code to ensure games can be made available on Automotive platforms.\n"
                               + "We are updating the Android settings now.");
                PlayerSettings.allowUnsafeCode = false;
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

            if (PlayerSettings.WebGL.memoryGrowthMode != WebGLMemoryGrowthMode.None) {
                Debug.LogWarning(
                    "For performance and stability on automotive, AirConsole requires 'Memory Growth Mode' to be set to None in WebGL PlayerSettings with the games maximum memory usage set.\n"
                    + "Updating the WebGL settings now.");
                PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.None;
                PlayerSettings.WebGL.initialMemorySize = Mathf.Min(512,
                    Mathf.Max(PlayerSettings.WebGL.initialMemorySize, PlayerSettings.WebGL.maximumMemorySize));
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
                throw new UnityException("Please update the Android Build and Player settings to continue.");
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

            PlayerSettings.Android.renderOutsideSafeArea = true;

            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)requiredAndroidTargetSdk;
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23) {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            }

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
        }

        private static void DisableUndesirableAndroidFeatures() {
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
            GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.OpenGLES3 };

#if !UNITY_6000_0_OR_NEWER
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL)) {
                Debug.LogError(
                    "AirConsole WebGL requires 'Auto Graphics API' to be disabled to enable WebGL1.\nUpdating the settings now.");
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            }

            graphicsAPIs = graphicsAPIs.ToList().Append(GraphicsDeviceType.OpenGLES2).ToArray();
#endif
            if (!PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL).SequenceEqual(graphicsAPIs)) {
                Debug.LogWarning("AirConsole requires WebGL2, WebGL1 to be enabled in the WebGL Player Settings.\n"
                                 + "Updating the WebGL Graphics APIs now.");
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, graphicsAPIs);
            }
        }

        private static void EnsureAndroidRenderSettings() {
            PlayerSettings.use32BitDisplayBuffer = true;

#if !UNITY_6000_0_OR_NEWER
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android)) {
                Debug.LogError(
                    "AirConsole Android requires 'Auto Graphics API' to be disabled to enable OpenGL ES2.\n"
                    + "Updating the Android settings now.");
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            }
#endif

            GraphicsDeviceType[] graphicsAPIs = { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 };
#if !UNITY_6000_0_OR_NEWER
            graphicsAPIs = graphicsAPIs.ToList().Append(GraphicsDeviceType.OpenGLES2).ToArray();
#endif

            if (!PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(graphicsAPIs)) {
                Debug.LogWarning($"AirConsole requires {string.Join(',', graphicsAPIs)} to be enabled in the Android Player Settings.\n"
                                 + "Updating the Android Graphics APIs now.");
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsAPIs);
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

        #region Check Texture format usage

        private static bool IsDesirableTextureCompressionFormat(BuildTargetGroup targetGroup) {
            TextureCompressionFormat format = GetDefaultTextureCompressionFormat(targetGroup);
            return format is TextureCompressionFormat.ASTC or TextureCompressionFormat.ETC2
                   && (targetGroup == BuildTargetGroup.Android
                       ? EditorUserBuildSettings.androidBuildSubtarget is MobileTextureSubtarget.ASTC or MobileTextureSubtarget.ETC2
                       : EditorUserBuildSettings.webGLBuildSubtarget is WebGLTextureSubtarget.ASTC or WebGLTextureSubtarget.ETC2);
        }

        private static TextureCompressionFormat GetDefaultTextureCompressionFormat(BuildTargetGroup buildTargetGroup) {
            Type playerSettingsType = typeof(PlayerSettings);

            MethodInfo methodInfo = playerSettingsType.GetMethod(
                "GetDefaultTextureCompressionFormat",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (methodInfo != null) {
#if UNITY_6000_0_OR_NEWER
                return (TextureCompressionFormat)methodInfo.Invoke(null, new object[] { GetBuildTargetFromGroup(buildTargetGroup) });
#else
                return (TextureCompressionFormat)methodInfo.Invoke(null, new object[] { buildTargetGroup });
#endif
            }

            return TextureCompressionFormat.Unknown;
        }

        private static void SetPlayerSettingsTextureFormat(BuildTargetGroup buildTargetGroup, TextureCompressionFormat format) {
            Type playerSettingsType = typeof(PlayerSettings);

            MethodInfo methodInfo = playerSettingsType.GetMethod(
                "SetDefaultTextureCompressionFormat",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (methodInfo != null) {
#if UNITY_6000_0_OR_NEWER
                methodInfo.Invoke(null, new object[] { GetBuildTargetFromGroup(buildTargetGroup), format });
#else
                methodInfo.Invoke(null, new object[] { buildTargetGroup, (int)format });
#endif
            }
        }

        private static BuildTarget GetBuildTargetFromGroup(BuildTargetGroup group) {
            switch (group) {
                case BuildTargetGroup.Android:
                    return BuildTarget.Android;
                case BuildTargetGroup.WebGL:
                    return BuildTarget.WebGL;
                default:
                    throw new UnityException($"Unsupported BuildTargetGroup {group}");
            }
        }

        // Extracted from UnityEditor.TextureCompressionFormat
        private enum TextureCompressionFormat {
            Unknown,
            ETC,
            ETC2,
            ASTC,
            PVRTC,
            DXTC,
            BPTC,
            DXTC_RGTC
        }

        #endregion Check Texture format usage
    }
}
#endif
