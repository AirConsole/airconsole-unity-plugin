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
    internal abstract class EditorNotificationService {
        /// <summary>
        /// Displays an error dialog and logs an error message, optionally providing instructions to disable AirConsole.
        /// </summary>
        /// <param name="message">The error message to display and log.</param>
        /// <param name="addAirConsoleDisable">
        /// If true, appends instructions to disable AirConsole to the error message.
        /// </param>
        /// <param name="title">The title of the error dialog. Defaults to "Unsupported".</param>
        /// <exception cref="UnityException">Always thrown with the provided error message.</exception>
        internal static void InvokeError(string message, bool addAirConsoleDisable = false, string title = "Unsupported") {
            EditorUtility.DisplayDialog(title, message, "I understand");
            if (addAirConsoleDisable) {
                message +=
                    "\nTo disable AirConsole for this build, add the scripting define symbol 'DISABLE_AIRCONSOLE' in the Player Settings.";
            }

            Debug.LogError(message);
            throw new BuildFailedException(message);
        }
    }

    public abstract class UnityVersionCheck {
        [InitializeOnLoadMethod]
        private static void CheckUnityVersions() {
            if (IsSupportedUnityVersion()) {
                return;
            }

            EditorNotificationService.InvokeError($"AirConsole {Settings.VERSION} requires Unity 2022.3 or newer!", true);
        }

        public static bool IsSupportedUnityVersion() => Settings.IsUnity2022OrHigher();
    }

    public abstract class UnityPlatform {
        [InitializeOnLoadMethod]
        private static void CheckPlatform() {
            BuildTargetGroup buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (buildTarget is BuildTargetGroup.Android or BuildTargetGroup.WebGL) {
                return;
            }

            if (IsPlatformSupported(BuildTarget.WebGL)) {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            } else if (IsPlatformSupported(BuildTarget.Android)) {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            } else {
                EditorNotificationService.InvokeError($"AirConsole {Settings.VERSION} requires the WebGL or Android module to be present!",
                    true);
            }
        }

        private static bool IsPlatformSupported(BuildTarget buildTarget) {
            Type moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
            MethodInfo IsPlatformSupportLoadedByBuildTarget = moduleManager.GetMethod("IsPlatformSupportLoadedByBuildTarget",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (IsPlatformSupportLoadedByBuildTarget != null) {
                return (bool)IsPlatformSupportLoadedByBuildTarget.Invoke(null, new object[] { buildTarget });
            }

            return true;
        }
    }

    public abstract class ProjectConfigurationCheck : IPreprocessBuildWithReport {
        public int callbackOrder {
            get => 999;
        }

        public void OnPreprocessBuild(BuildReport report) {
            CheckSettings(report.summary.platform);
        }

        internal static void CheckSettings(BuildTarget platform) {
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
                    throw new BuildFailedException($"AirConsole Plugin does not support platform {platform}");
            }

            Debug.Log($"AirConsole Plugin configuration checks for {platform} completed successfully.");
        }

        [InitializeOnLoadMethod]
        private static void EnsureSharedPlayerSettings() {
            PlayerSettings.resetResolutionOnWindowResize = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            if (BuildHelper.IsInternalBuild) {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            } else {
                if (PlayerSettings.insecureHttpOption == InsecureHttpOption.AlwaysAllowed) {
                    Debug.LogError(
                        "AirConsole does not allow HTTP web requests. Please create a development build if you want to develop with insecure endpoints.");
                    PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
                }
            }

            if (!UnityVersionCheck.IsSupportedUnityVersion()) {
                string message = $"AirConsole {Settings.VERSION} requires Unity 2022.3 or newer. You are using {Application.unityVersion}.";
                Debug.LogError(message);
                throw new BuildFailedException(message);
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
            VerifyWebGLTemplate();

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
                throw new BuildFailedException("Please update the WebGL build and player settings to continue.");
            }
        }

        private static void VerifyWebGLTemplate() {
            string expectedTemplateName = Settings.WEBTEMPLATE_PATH.Split('/').Last();
            string[] templateUri = PlayerSettings.WebGL.template.Split(':');
            if (templateUri.Length != 2
                || templateUri[0].ToUpper() == "APPLICATION"
                || (templateUri[1] != expectedTemplateName && Settings.TEMPLATE_NAMES.Contains(templateUri[1]))) {
                string incompatibleTemplateMessage =
                    $"Unity version \"{Application.unityVersion}\" needs the AirConsole WebGL template \"{expectedTemplateName}\" to work.\nPlease change the WebGL template in your Project Settings under Player (WebGL platform tab) > Resolution and Presentation > WebGL Template.";
                Debug.LogError(incompatibleTemplateMessage);

                if (EditorUtility.DisplayDialog("Incompatible WebGL Template", incompatibleTemplateMessage, "Open Player Settings",
                        "Cancel")) {
                    // In Unity 6 this needs to be done with a delay call, otherwise it breaks the window layout when Project Settings are docked already.
                    EditorApplication.delayCall = () => SettingsService.OpenProjectSettings("Project/Player");
                }
            }
        }

        [InitializeOnLoadMethod]
        private static void EnsureAndroidPlayerSettings() {
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            if (PlayerSettings.muteOtherAudioSources) {
                Debug.Log("AirConsole requires 'mute other audio sources' to be disabled for automotive compatibility");
                PlayerSettings.muteOtherAudioSources = false;
            }

            if (!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android)) {
                Debug.LogWarning("To ensure optimal performance and thermal load, 'Multithreaded rendering' is enabled now.\n"
                                 + "We are updating the Android settings now.");
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            }

            if (!IsDesirableTextureCompressionFormat(BuildTargetGroup.Android)) {
                Debug.LogError("AirConsole requires 'ASTC' or 'ETC2' as the texture compression format.");
                throw new BuildFailedException("Please update the Android Build and Player settings to continue.");
            }

            UpdateAndroidPlayerSettingsInProperties();
            EnsureAndroidPlatformSettings();
            MaintainChallengingAndroidFeatures();

#if AIRCONSOLE_DEVELOPMENT
            PlayerSettings.Android.bundleVersionCode = SecondsSinceStartOf2025();
            Version version = Version.Parse(PlayerSettings.bundleVersion);
            PlayerSettings.bundleVersion
                = new Version(version.Major, version.Minor, version.Build, PlayerSettings.Android.bundleVersionCode).ToString();
#endif
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
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26) {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
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

        private static void MaintainChallengingAndroidFeatures() {
            PlayerSettings.Android.ARCoreEnabled = false;
            PlayerSettings.Android.androidTargetDevices = AndroidTargetDevices.PhonesTabletsAndTVDevicesOnly;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.chromeosInputEmulation = false;

            // Automotive first settings. Fullscreen will be overriden based on it being a car or not at launch.
            PlayerSettings.Android.resizableWindow = true;
            PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
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
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL)) {
                return;
            }

            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
            if (!graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES3)) {
                Debug.LogError("AirConsole WebGL requires either 'Auto Graphics API' or WebGL2 to be present\nUpdating the settings now.");

                graphicsAPIs = graphicsAPIs.ToList().Prepend(GraphicsDeviceType.OpenGLES3).ToArray();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, graphicsAPIs);
            }
        }

        private static void EnsureAndroidRenderSettings() {
            PlayerSettings.use32BitDisplayBuffer = true;

            // if (!PlayerSettings.vulkanEnableLateAcquireNextImage) {
            //     // Debug.LogWarning("Late Acquire has been disabled for Vulkan as this has negative side effects and performance impact.");
            //     PlayerSettings.vulkanEnableLateAcquireNextImage = true;
            // }

            if (PlayerSettings.vulkanNumSwapchainBuffers < 3) {
                Debug.LogWarning("The Vulkan Swapchain must contain at least 3 buffers");
                PlayerSettings.vulkanNumSwapchainBuffers = 3;
            }

            // if the profiler shows significant 'semaphore WaitForSignal' blocks, we need to invert this!
            if (!PlayerSettings.Android.optimizedFramePacing) {
                Debug.LogWarning("Enabling optimized frame pacing for improved frame consistency and performance on Android.");
                PlayerSettings.Android.optimizedFramePacing = true;
            }

            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android)) {
                return;
            }

            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);

            if (graphicsAPIs.First() != GraphicsDeviceType.Vulkan) {
                Debug.LogWarning("AirConsole requires either 'Auto Graphics API' or Vulkan to be the first API.");
            }
        }

        private static int SecondsSinceStartOf2025() {
            DateTime startOfYear = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = DateTime.UtcNow;

            return (int)(now - startOfYear).TotalSeconds;
        }

        #region Check Texture format usage

        private static bool IsDesirableTextureCompressionFormat(BuildTargetGroup targetGroup) {
            TextureCompressionFormat format = GetDefaultTextureCompressionFormat(targetGroup);
            // Either the Texture Default settings in Player Settings are ETC2 | ASTC or the platforms build settings Texture Compression is
            return format is TextureCompressionFormat.ASTC or TextureCompressionFormat.ETC2
                   || (targetGroup == BuildTargetGroup.Android
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
                    throw new BuildFailedException($"Unsupported BuildTargetGroup {group}");
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
