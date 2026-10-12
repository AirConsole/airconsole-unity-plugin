#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor.Tests {
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class ProjectConfigurationCheckTests {
        private bool _renderOutsideSafeArea;
        private bool _resizableWindow;
        private FullScreenMode _fullscreenMode;
        private bool _startInFullscreen;
        private bool _useDefaultGraphicsApis;
        private GraphicsDeviceType[] _graphicsApis;

        // CheckSettings also writes the settings below. The [InitializeOnLoadMethod] checks do not, so restore them.
        private bool _use32BitDisplayBuffer;
        private uint _vulkanNumSwapchainBuffers;
        private bool _optimizedFramePacing;

        private LogType _filterLogType;
        private readonly List<string> _warnings = new();

        [SetUp]
        public void SetUp() {
            _renderOutsideSafeArea = PlayerSettings.Android.renderOutsideSafeArea;
            _resizableWindow = ResizableWindow;
            _fullscreenMode = PlayerSettings.Android.fullscreenMode;
            _startInFullscreen = StartInFullscreen;
            _useDefaultGraphicsApis = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            _graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            _use32BitDisplayBuffer = PlayerSettings.use32BitDisplayBuffer;
            _vulkanNumSwapchainBuffers = PlayerSettings.vulkanNumSwapchainBuffers;
            _optimizedFramePacing = PlayerSettings.Android.optimizedFramePacing;

            // AirConsoleLogger drops warnings that the logger filters out, which would hide the warning under test.
            _filterLogType = Debug.unityLogger.filterLogType;
            Debug.unityLogger.filterLogType = LogType.Log;
            _warnings.Clear();
            Application.logMessageReceived += CollectWarning;
        }

        [TearDown]
        public void TearDown() {
            Application.logMessageReceived -= CollectWarning;
            Debug.unityLogger.filterLogType = _filterLogType;
            PlayerSettings.Android.renderOutsideSafeArea = _renderOutsideSafeArea;
            ResizableWindow = _resizableWindow;
            PlayerSettings.Android.fullscreenMode = _fullscreenMode;
            StartInFullscreen = _startInFullscreen;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, _graphicsApis);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, _useDefaultGraphicsApis);
            PlayerSettings.use32BitDisplayBuffer = _use32BitDisplayBuffer;
            PlayerSettings.vulkanNumSwapchainBuffers = _vulkanNumSwapchainBuffers;
            PlayerSettings.Android.optimizedFramePacing = _optimizedFramePacing;
        }

        [Test]
        public void CheckSettings_Android_KeepsDeveloperDisplaySettings() {
            PlayerSettings.Android.renderOutsideSafeArea = true;
            ResizableWindow = false;

            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);

            Assert.That(PlayerSettings.Android.renderOutsideSafeArea, Is.True);
            Assert.That(ResizableWindow, Is.False);
        }

        [Test]
        public void CheckSettings_Android_EnforcesFullscreenStart() {
            PlayerSettings.Android.fullscreenMode = FullScreenMode.Windowed;
            StartInFullscreen = false;

            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);

            Assert.That(PlayerSettings.Android.fullscreenMode, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(StartInFullscreen, Is.True);
        }

        [Test]
        public void CheckSettings_Android_AcceptsNonVulkanFirstGraphicsApi() {
            GraphicsDeviceType[] openGlFirst = { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan };
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, openGlFirst);

            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);

            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), Is.EqualTo(openGlFirst));
            Assert.That(_warnings, Has.None.Contains("first API"));
        }

        private void CollectWarning(string message, string stackTrace, LogType type) {
            if (type == LogType.Warning) {
                _warnings.Add(message);
            }
        }

        private static bool ResizableWindow {
#if UNITY_6000_0_OR_NEWER
            get => PlayerSettings.Android.resizeableActivity;
            set => PlayerSettings.Android.resizeableActivity = value;
#else
            get => PlayerSettings.Android.resizableWindow;
            set => PlayerSettings.Android.resizableWindow = value;
#endif
        }

        private static bool StartInFullscreen {
#if UNITY_6000_6_OR_NEWER
            get => (PlayerSettings.Android.requestedVisibleInsets & AndroidWindowInsetsType.NavigationBars) == 0;
            set {
                if (value) {
                    PlayerSettings.Android.requestedVisibleInsets &= ~AndroidWindowInsetsType.NavigationBars;
                } else {
                    PlayerSettings.Android.requestedVisibleInsets |= AndroidWindowInsetsType.NavigationBars;
                }
            }
#else
            get => PlayerSettings.Android.startInFullscreen;
            set => PlayerSettings.Android.startInFullscreen = value;
#endif
        }
    }
}
#endif
