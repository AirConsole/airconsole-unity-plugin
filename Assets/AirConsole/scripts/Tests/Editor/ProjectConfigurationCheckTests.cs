#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor.Tests {
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.TestTools;

    public class ProjectConfigurationCheckTests {
        private bool _renderOutsideSafeArea;
        private bool _resizableWindow;
        private FullScreenMode _fullscreenMode;
        private bool _startInFullscreen;
        private bool _useDefaultGraphicsApis;
        private GraphicsDeviceType[] _graphicsApis;
        private AndroidSdkVersions _targetSdkVersion;
        private AndroidSdkVersions _minSdkVersion;
        private string _webGLTemplate;
        private Dictionary<string, string> _templateCustomValues;

        // CheckSettings also writes the setting below. The [InitializeOnLoadMethod] checks do not, so restore it.
        private bool _use32BitDisplayBuffer;

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
            _targetSdkVersion = PlayerSettings.Android.targetSdkVersion;
            _minSdkVersion = PlayerSettings.Android.minSdkVersion;
            _use32BitDisplayBuffer = PlayerSettings.use32BitDisplayBuffer;
            _webGLTemplate = PlayerSettings.WebGL.template;
            _templateCustomValues = new Dictionary<string, string>();
            foreach (string key in TemplateCustomKeys) {
                _templateCustomValues[key] = PlayerSettings.GetTemplateCustomValue(key);
            }

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
            PlayerSettings.Android.targetSdkVersion = _targetSdkVersion;
            PlayerSettings.Android.minSdkVersion = _minSdkVersion;
            PlayerSettings.use32BitDisplayBuffer = _use32BitDisplayBuffer;
            PlayerSettings.WebGL.template = _webGLTemplate;
            RemoveTemplateCustomKeysExcept(_templateCustomValues.Keys);
            foreach (KeyValuePair<string, string> keyValue in _templateCustomValues) {
                PlayerSettings.SetTemplateCustomValue(keyValue.Key, keyValue.Value);
            }
        }

        private static string[] TemplateCustomKeys {
            get => (string[])ProjectConfigurationCheck.TemplateCustomKeysProperty.GetValue(null) ?? Array.Empty<string>();
            set => ProjectConfigurationCheck.TemplateCustomKeysProperty.SetValue(null, value);
        }

        // The templateCustomKeys setter only adds keys, so the test keys are removed from the serialized settings.
        private static void RemoveTemplateCustomKeysExcept(ICollection<string> keysToKeep) {
            SerializedObject playerSettings =
                new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            SerializedProperty templateCustomTags = playerSettings.FindProperty("m_TemplateCustomTags");
            for (int i = templateCustomTags.arraySize - 1; i >= 0; i--) {
                string key = templateCustomTags.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;
                if (!keysToKeep.Contains(key)) {
                    templateCustomTags.DeleteArrayElementAtIndex(i);
                }
            }

            playerSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ProjectConfigurationCheck_IsABuildPreprocessorThatUnityCanCreate() {
            Assert.That(TypeCache.GetTypesDerivedFrom<IPreprocessBuildWithReport>(),
                Has.Member(typeof(ProjectConfigurationCheck)));
            Assert.That(Activator.CreateInstance(typeof(ProjectConfigurationCheck)),
                Is.InstanceOf<IPreprocessBuildWithReport>());
        }

        [Test]
        public void CheckSettings_UnsupportedPlatform_StopsTheBuild() {
            BuildFailedException exception = Assert.Throws<BuildFailedException>(() =>
                ProjectConfigurationCheck.CheckSettings(BuildTarget.StandaloneOSX));

            Assert.That(exception.Message, Does.Contain(nameof(BuildTarget.StandaloneOSX)));
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

        [TestCase(34)]
        public void CheckSettings_Android_RaisesTargetSdkToMinimum(int configured) {
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)configured;
            LogAssert.Expect(LogType.Error, new Regex("'Target SDK Version' of 35 or higher"));

            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);

            Assert.That((int)PlayerSettings.Android.targetSdkVersion, Is.EqualTo(35));
        }

        [TestCase(0)]
        [TestCase(35)]
        [TestCase(36)]
        public void CheckSettings_Android_KeepsTargetSdkAtOrAboveMinimum(int configured) {
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)configured;

            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);

            Assert.That((int)PlayerSettings.Android.targetSdkVersion, Is.EqualTo(configured));
        }

        // Unity 6000.6 and newer reject API level 25 in the setter with an error log, so the case cannot be set up there.
#if !UNITY_6000_6_OR_NEWER
        [TestCase(25, 26)]
#endif
        [TestCase(26, 26)]
        [TestCase(30, 30)]
        public void CheckSettings_Android_RaisesMinSdkToMinimum(int configured, int expected) {
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)configured;

            ProjectConfigurationCheck.CheckSettings(BuildTarget.Android);

            Assert.That((int)PlayerSettings.Android.minSdkVersion, Is.EqualTo(expected));
        }

        [TestCase("APPLICATION:Default")]
        [TestCase("APPLICATION:Minimal")]
        public void SelectWebGLTemplateIfUnityBuiltIn_SelectsTheAirConsoleTemplate(string template) {
            PlayerSettings.WebGL.template = template;

            ProjectConfigurationCheck.SelectWebGLTemplateIfUnityBuiltIn();

            Assert.That(PlayerSettings.WebGL.template,
                Is.EqualTo($"PROJECT:{ProjectConfigurationCheck.ExpectedWebGLTemplateName}"));
        }

        [Test]
        public void SelectWebGLTemplateIfUnityBuiltIn_AddsTheTemplateVariablesAndKeepsTheOthers() {
            TemplateCustomKeys = new[] { "GAME_VARIABLE" };
            PlayerSettings.SetTemplateCustomValue("GAME_VARIABLE", "4");
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            ProjectConfigurationCheck.SelectWebGLTemplateIfUnityBuiltIn();

            Assert.That(TemplateCustomKeys, Is.SupersetOf(ProjectConfigurationCheck.AirConsoleWebGLTemplateVariables));
            Assert.That(TemplateCustomKeys, Has.Member("GAME_VARIABLE"));
            Assert.That(PlayerSettings.GetTemplateCustomValue("GAME_VARIABLE"), Is.EqualTo("4"));
        }

        [Test]
        public void SelectWebGLTemplateIfUnityBuiltIn_KeepsAProjectTemplate() {
            PlayerSettings.WebGL.template = "PROJECT:GameTemplate";

            ProjectConfigurationCheck.SelectWebGLTemplateIfUnityBuiltIn();

            Assert.That(PlayerSettings.WebGL.template, Is.EqualTo("PROJECT:GameTemplate"));
        }

        [Test]
        public void SelectWebGLTemplateIfUnityBuiltIn_KeepsTheAirConsoleTemplateOfTheOtherUnityVersion() {
            string template = $"PROJECT:{OtherAirConsoleTemplateName}";
            PlayerSettings.WebGL.template = template;

            ProjectConfigurationCheck.SelectWebGLTemplateIfUnityBuiltIn();

            Assert.That(PlayerSettings.WebGL.template, Is.EqualTo(template));
        }

        [Test]
        public void CheckWebGLSetup_AirConsoleTemplateOfTheOtherUnityVersion_FailsWithTheTemplateMessage() {
            PlayerSettings.WebGL.template = $"PROJECT:{OtherAirConsoleTemplateName}";

            BuildFailedException exception = Assert.Throws<BuildFailedException>(PreBuildProcessing.CheckWebGLSetup);

            Assert.That(exception.Message, Is.EqualTo(ProjectConfigurationCheck.IncompatibleWebGLTemplateMessage));
        }

        private static string OtherAirConsoleTemplateName =>
            Array.Find(Settings.TEMPLATE_NAMES, name => name != ProjectConfigurationCheck.ExpectedWebGLTemplateName);

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
