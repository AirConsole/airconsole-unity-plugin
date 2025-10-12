
#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor {
    using System.Text;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;
    using UnityEngine.Rendering;
    
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

            AirConsoleLogger.LogError(() => message);
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

            AirConsoleLogger.Log(() => $"AirConsole Plugin configuration checks for {platform} completed successfully.");
        }

        [InitializeOnLoadMethod]
        private static void EnsureSharedPlayerSettings() {
            Inspector airconsoleInspector = Editor.CreateInstance<Inspector>();
            airconsoleInspector.UpdateAirConsoleConstructorSettings();
            
            PlayerSettings.resetResolutionOnWindowResize = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            if (BuildHelper.IsInternalBuild) {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            } else {
                if (PlayerSettings.insecureHttpOption == InsecureHttpOption.AlwaysAllowed) {
                    AirConsoleLogger.LogError(() =>
                        "AirConsole does not allow HTTP web requests. Please create a development build if you want to develop with insecure endpoints.");
                    PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
                }
            }

            if (!UnityVersionCheck.IsSupportedUnityVersion()) {
                string message = $"AirConsole {Settings.VERSION} requires Unity 2022.3 or newer. You are using {Application.unityVersion}.";
                AirConsoleLogger.LogError(() => message);
                throw new BuildFailedException(message);
            }

            bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
            if (PlayerSettings.runInBackground != shouldRunInBackground) {
                AirConsoleLogger.Log(() =>
                    $"AirConsole needs 'Run In Background' to be {shouldRunInBackground} in PlayerSettings for {EditorUserBuildSettings.activeBuildTarget}.\n"
                    + $"Updating the settings now.");
                PlayerSettings.runInBackground = shouldRunInBackground;
            }

            if (PlayerSettings.allowUnsafeCode) {
                AirConsoleLogger.LogError(() =>
                    "AirConsole does not allow for unsafe code to ensure games can be made available on Automotive platforms.\n"
                    + "We are updating the Android settings now.");
                PlayerSettings.allowUnsafeCode = false;
            }
        }

        [InitializeOnLoadMethod]
        private static void EnsureWebGLPlayerSettings() {
            VerifyWebGLTemplate();

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.nameFilesAsHashes = false; // We upload into timestamp based folders. This is not necessary.

            if (PlayerSettings.WebGL.dataCaching) {
                AirConsoleLogger.LogWarning(() =>
                    "AirConsole requires 'Data Caching' to be disabled to avoid interference with automotive requirements.\n"
                    + "Updating the WebGL settings now.");
                PlayerSettings.WebGL.dataCaching = false;
            }

            const int maximumInitialMemorySize = 1024;
            if (PlayerSettings.WebGL.memoryGrowthMode != WebGLMemoryGrowthMode.None) {
                int computedSize = Mathf.Min(maximumInitialMemorySize,
                    Mathf.Max(PlayerSettings.WebGL.initialMemorySize, PlayerSettings.WebGL.maximumMemorySize));
                StringBuilder sb = new();
                sb.AppendLine(
                    "For performance and stability on automotive, AirConsole requires 'Memory Growth Mode' to be set to None in WebGL PlayerSettings.");
                sb.AppendLine(
                    "Use 'Initial Memory Size (MB)' in Player Settings > Publishing Settings of WebGL to define the amount of memory your game actually needs.");
                sb.AppendLine("We recommend to measure the maximum used memory and add 10% more for safety.");
                sb.AppendLine(
                    $"Updating the WebGL settings to Memory Growth Mode 'None' and an initial memory size of {computedSize}mb based on configured values and limits.");

                AirConsoleLogger.LogWarning(() => sb.ToString());
                PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.None;
                PlayerSettings.WebGL.initialMemorySize = computedSize;
            }

            if (PlayerSettings.WebGL.initialMemorySize > maximumInitialMemorySize) {
                PlayerSettings.WebGL.initialMemorySize = maximumInitialMemorySize;
                AirConsoleLogger.LogWarning(() =>
                    $"Reducing WebGL initialMemorySize to the reliably supported maximum of {maximumInitialMemorySize}.\nPlease ensure the game doesn't need more RAM.");
            }

            if (!IsDesirableTextureCompressionFormat(BuildTargetGroup.WebGL)) {
                AirConsoleLogger.LogError(() => "AirConsole requires 'ASTC' or 'ETC2' as the texture compression format.");
                throw new BuildFailedException("Please update the WebGL build or player settings to build for AirConsole WebGL.");
            }
        }

        private static void VerifyWebGLTemplate() {
            ValidateApiUsage();
            EnsureRequiredDependenciesInHtml();

            string expectedTemplateName = Settings.WEBTEMPLATE_PATH.Split('/').Last();
            string[] templateUri = PlayerSettings.WebGL.template.Split(':');
            if (templateUri.Length != 2
                || templateUri[0].ToUpper() == "APPLICATION"
                || (templateUri[1] != expectedTemplateName && Settings.TEMPLATE_NAMES.Contains(templateUri[1]))) {
                string incompatibleTemplateMessage =
                    $"Unity version \"{Application.unityVersion}\" needs the AirConsole WebGL template \"{expectedTemplateName}\" to work.\nPlease change the WebGL template in your Project Settings under Player (WebGL platform tab) > Resolution and Presentation > WebGL Template.";
                AirConsoleLogger.LogError(() => incompatibleTemplateMessage);

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
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            if (PlayerSettings.muteOtherAudioSources) {
                AirConsoleLogger.Log(() => "AirConsole requires 'mute other audio sources' to be disabled for automotive compatibility");
                PlayerSettings.muteOtherAudioSources = false;
            }

            if (!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android)) {
                AirConsoleLogger.LogWarning(() =>
                    "To ensure optimal performance and thermal load, 'Multithreaded rendering' is enabled now.\n"
                    + "We are updating the Android settings now.");
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            }

            if (!IsDesirableTextureCompressionFormat(BuildTargetGroup.Android)) {
                AirConsoleLogger.LogError(() => "AirConsole requires 'ASTC' or 'ETC2' as the texture compression format.");
                throw new BuildFailedException("Please update the Android Build or Player settings to build for AirConsole WebGL");
            }

            UpdateAndroidPlayerSettingsInProperties();
            EnsureAndroidPlatformSettings();
            MaintainChallengingAndroidFeatures();

#if !AIRCONSOLE_DEVELOPMENT
            return;
#endif

            // We don't want to create a warning for this but we also don't want it to be in use under normal circumstances
#pragma warning disable 0162 // Unreachable code detected
            PlayerSettings.Android.bundleVersionCode = SecondsSinceStartOf2025();
            Version version = Version.Parse(PlayerSettings.bundleVersion);

            // Undefined values can come back as -1 which breaks when creating the new version, so we ensure valid value ranges. 
            version = new Version(
                Mathf.Clamp(version.Major, 0, version.Major),
                Mathf.Clamp(version.Minor, 0, version.Minor),
                Mathf.Clamp(version.Build, 0, version.Build));
            PlayerSettings.bundleVersion
                = new Version(version.Major, version.Minor, version.Build, PlayerSettings.Android.bundleVersionCode).ToString();
#pragma warning restore 0162 //  Unreachable code detected
        }

        private static void EnsureAndroidPlatformSettings() {
            PlayerSettings.Android.forceInternetPermission = true;

            // To ensure Google Play compatibility, we require a target SDK of 35 or higher.
            const int requiredAndroidTargetSdk = 35;
            if ((int)PlayerSettings.Android.targetSdkVersion < requiredAndroidTargetSdk) {
                AirConsoleLogger.LogError(() => $"AirConsole requires 'Target SDK Version' of {requiredAndroidTargetSdk} or higher.\n"
                                                + "We are updating the Android settings now.");
            }

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
                AirConsoleLogger.LogWarning(() =>
                    "AirConsole recommends 'Preferred Install Location' to be set to Auto in Android PlayerSettings.\n"
                    + "We are updating the Android settings now.");
                PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            }
        }

        private static void MaintainChallengingAndroidFeatures() {
            PlayerSettings.Android.ARCoreEnabled = false;
            PlayerSettings.Android.androidTargetDevices = AndroidTargetDevices.PhonesTabletsAndTVDevicesOnly;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.chromeosInputEmulation = false;


            if (EditorPrefs.GetBool("customBuild", false) == true) {
                PlayerSettings.Android.renderOutsideSafeArea = EditorPrefs.GetBool("renderOutsideSafeArea", false);
                PlayerSettings.Android.resizableWindow = EditorPrefs.GetBool("resizableWindow", true);
                PlayerSettings.Android.fullscreenMode = (FullScreenMode)EditorPrefs.GetInt("fullscreenMode", 1);
                PlayerSettings.Android.startInFullscreen = EditorPrefs.GetBool("startInFullscreen", true);
            } else {
                // This setting must be false. Otherwise the game will go full screen beyond the boundaries of the 3rd party safe area manager of BMW.
                PlayerSettings.Android.renderOutsideSafeArea = false;

                // Automotive first settings. Fullscreen will be overriden based on it being a car or not at launch.
                PlayerSettings.Android.resizableWindow = true;

                // Set fullscreenMode to FullScreenMode.FullScreenWindow
                PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;

                // If we don't do this, the margin calculations for the webview will be wrong. The initial size when the webview is negatively
                // impacted by the bottom bar that impacts the layout but is not visible.
                // When the layout corrects, the webview does not resize.
                PlayerSettings.Android.startInFullscreen = true;
            }

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
                AirConsoleLogger.LogError(() =>
                    "AirConsole WebGL requires either 'Auto Graphics API' or WebGL2 to be present\nUpdating the settings now.");

                graphicsAPIs = graphicsAPIs.ToList().Prepend(GraphicsDeviceType.OpenGLES3).ToArray();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, graphicsAPIs);
            }
        }

        private static void EnsureAndroidRenderSettings() {
            PlayerSettings.use32BitDisplayBuffer = true;

            // if (!PlayerSettings.vulkanEnableLateAcquireNextImage) {
            //     // AirConsoleLogger.LogWarning("Late Acquire has been disabled for Vulkan as this has negative side effects and performance impact.");
            //     PlayerSettings.vulkanEnableLateAcquireNextImage = true;
            // }

            if (PlayerSettings.vulkanNumSwapchainBuffers < 3) {
                AirConsoleLogger.LogWarning(() => "The Vulkan Swapchain must contain at least 3 buffers");
                PlayerSettings.vulkanNumSwapchainBuffers = 3;
            }

            // if the profiler shows significant 'semaphore WaitForSignal' blocks, we need to invert this!
            if (!PlayerSettings.Android.optimizedFramePacing) {
                AirConsoleLogger.LogWarning(() =>
                    "Enabling optimized frame pacing for improved frame consistency and performance on Android.");
                PlayerSettings.Android.optimizedFramePacing = true;
            }

            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android)) {
                return;
            }

            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);

            if (graphicsAPIs.First() != GraphicsDeviceType.Vulkan) {
                AirConsoleLogger.LogWarning(() => "AirConsole requires either 'Auto Graphics API' or Vulkan to be the first API.");
            }
        }

        private static int SecondsSinceStartOf2025() {
            DateTime startOfYear = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = DateTime.UtcNow;

            return (int)(now - startOfYear).TotalSeconds;
        }

        #region Controller Screen Consistency Checks
        private static void ValidateApiUsage() {
            string webGLTemplateDirectory = PreBuildProcessing.GetWebGLTemplateDirectory();
            AirConsoleLogger.Log(() => $"Validating API Usage in {webGLTemplateDirectory}");
            if (!VerifyReferencedAirConsoleApiVersion(Path.Combine(webGLTemplateDirectory, "index.html"), Settings.RequiredMinimumVersion)

                // || !VerifyAPIUsage(pathToBuiltProject + "/screen.html", Settings.RequiredMinimumVersion)
                || !VerifyReferencedAirConsoleApiVersion(Path.Combine(webGLTemplateDirectory, "controller.html"),
                    Settings.RequiredMinimumVersion)) {
                AirConsoleLogger.LogError(() => "Outdated AirConsole API detected. Please check the previous logs to address the problem.");
                throw new BuildFailedException(
                    "Build failed. Outdated AirConsole API detected. Please see Error Logs for more information.");
            }
        }

        private static bool VerifyReferencedAirConsoleApiVersion(string pathToHtml, Version requiredApiVersion) {
            if (!File.Exists(pathToHtml)) {
                AirConsoleLogger.Log(() => $"File {pathToHtml} does not exist.");
                return true;
            }

            // Check if the reference to airconsole-Major.Minor.Patch.js is at least as big as requiredMinimumVersion.
            //  Ensure that the reference is not 'airconsole-latest.js'.
            string fileContent = File.ReadAllText(pathToHtml);
            string apiVersion = $"airconsole-{requiredApiVersion.Major}.{requiredApiVersion.Minor}.{requiredApiVersion.Build}.js";

            // If airconsole-latest usage is detected, we need to inform the game developer to use the specified version.
            //  We do not want Unity games to use latest due to prior implicit assumptions and requirements that might not be met anymore.
            Regex regexAirconsoleLatest = new(@"(?<!<!--.*)airconsole-latest\.js", RegexOptions.IgnoreCase);
            bool foundAirconsoleLatest = regexAirconsoleLatest.IsMatch(fileContent);
            if (foundAirconsoleLatest) {
                AirConsoleLogger.LogError(() => $"{pathToHtml} uses airconsole-latest.js. Please fix it to use airconsole-{apiVersion}.js");

                return false;
            }

            Regex regexAirconsoleApiVersion = new(@"(?<!<!--\s*)<script[^>]*src\s*=\s*[""'].*airconsole-(\d+)\.(\d+)\.(\d+)\.js[""'][^>]*>",
                RegexOptions.IgnoreCase);

            // Regex regexAirconsoleApiVersion = new(@"(?<!<!--\s*)airconsole-(\d+)\.(\d+)\.(\d+)\.js", RegexOptions.IgnoreCase);
            MatchCollection matches = regexAirconsoleApiVersion.Matches(fileContent);

            switch (matches.Count) {
                // No references to the airconsole API do not yield working AirConsole builds, so we can safely stop the build and inform
                //  the developer.
                case < 1:
                    AirConsoleLogger.LogError(() =>
                        $"No reference to {apiVersion} found in {pathToHtml}. Please ensure that you correctly reference it.");
                    return false;

                // Multiple reference to the airconsole API break behavior because they override the AirConsole DOM window setup.
                //  As such we want to inform the game developer.
                case > 1:
                    AirConsoleLogger.LogError(() =>
                        $"Multiple airconsole-*.js references found in {pathToHtml}. Please ensure only one reference is present.");
                    return false;
            }

            // If we detect versioned airconsole-X.Y.Z.js references, check their version and request an update if necessary.
            Match match = matches[0];
            int major = int.Parse(match.Groups[1].Value);
            int minor = int.Parse(match.Groups[2].Value);
            int revision = int.Parse(match.Groups[3].Value);

            Version referencedVersion = new(major, minor, revision);
            if (referencedVersion == requiredApiVersion) {
                AirConsoleLogger.LogDevelopment(() => $"Valid API reference {match.Groups[0]} found in {pathToHtml}.");
            } else {
                AirConsoleLogger.LogError(() =>
                    $"airconsole-{major}.{minor}.{revision}.js found. This does not match the required version, please use {apiVersion} instead.");
                return false;
            }

            return true;
        }

        private static void EnsureRequiredDependenciesInHtml() {
            string webGLTemplateDirectory = PreBuildProcessing.GetWebGLTemplateDirectory();
            string indexPath = Path.Combine(webGLTemplateDirectory, "index.html");

            if (!File.Exists(indexPath)) {
                return;
            }

            string[] dependencies = { "airconsole-settings.js", "airconsole-unity-plugin.js" };

            string fileContent = File.ReadAllText(indexPath);
            string updatedContent = fileContent;
            Regex regex = new(@"<script\s+src\s*=\s*[""'].*airconsole-[\d\.]+\.js[""']\s*><\/script>");
            bool contentChanged = false;

            foreach (string dependency in dependencies) {
                if (updatedContent.Contains(dependency)) {
                    AirConsoleLogger.LogDevelopment(() => $"{dependency} already included in index.html.");
                    continue;
                }

                Match match = regex.Match(updatedContent);
                if (match.Success) {
                    string matchedLine = match.Value;
                    string replacement = matchedLine + $"\n    <script src=\"{dependency}\"></script>";
                    updatedContent = updatedContent.Replace(matchedLine, replacement);
                    contentChanged = true;

                    AirConsoleLogger.Log(() => $"Automatically added {dependency} to index.html.");
                } else {
                    string apiVersion
                        = $"airconsole-{Settings.RequiredMinimumVersion.Major}.{Settings.RequiredMinimumVersion.Minor}.{Settings.RequiredMinimumVersion.Build}.js";
                    AirConsoleLogger.LogWarning(() =>
                        $"Could not find {apiVersion} script tag in index.html to inject {dependency}.");
                }
            }

            if (!contentChanged) {
                return;
            }

            string directoryName = Path.GetDirectoryName(indexPath) ?? ".";
            string tempFileName = Path.Combine(directoryName, $"{Path.GetFileName(indexPath)}.{Guid.NewGuid():N}.tmp");

            try {
                File.WriteAllText(tempFileName, updatedContent);
                if (File.Exists(indexPath)) {
                    File.Delete(indexPath);
                }

                File.Move(tempFileName, indexPath);
            } catch (Exception exception) {
                AirConsoleLogger.LogError(() => $"Failed updating {indexPath}: {exception.Message}");
                throw;
            } finally {
                if (File.Exists(tempFileName)) {
                    File.Delete(tempFileName);
                }
            }
        }
        #endregion Controller Screen Consistency Checks

        #region Check Texture format usage
        /// <summary>
        /// Checks if WebGL | Android use a texture format suitable for mobile SoC usage.
        /// </summary>
        /// <param name="buildTargetGroup">Build target group to check the texture compression for. Supported: WebGL, Android</param>
        /// <returns>True, if the texture compression format is desirable (hardware supported). False otherwise.</returns>
        private static bool IsDesirableTextureCompressionFormat(BuildTargetGroup buildTargetGroup) {
            TextureCompressionFormat format = GetDefaultTextureCompressionFormat(buildTargetGroup);

            // Either the Texture Default settings in Player Settings are ETC2 | ASTC or the platforms build settings Texture Compression must be.
            // We do at this point only support WebGL and Android and only check for these two build targets.
            return format is TextureCompressionFormat.ASTC or TextureCompressionFormat.ETC2
                   || (buildTargetGroup == BuildTargetGroup.Android
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
                bool isUnity6 = true;
#else
                bool isUnity6 = false;
#endif
                return isUnity6
                    ? (TextureCompressionFormat)methodInfo.Invoke(null, new object[] { GetBuildTargetFromGroup(buildTargetGroup) })
                    : (TextureCompressionFormat)methodInfo.Invoke(null, new object[] { buildTargetGroup });
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
                bool isUnity6 = true;
#else
                bool isUnity6 = false;
#endif
                if (isUnity6) {
                    methodInfo.Invoke(null, new object[] { GetBuildTargetFromGroup(buildTargetGroup), format });
                } else {
                    methodInfo.Invoke(null, new object[] { buildTargetGroup, (int)format });
                }
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
