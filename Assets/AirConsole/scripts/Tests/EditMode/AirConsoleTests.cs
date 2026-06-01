#if !DISABLE_AIRCONSOLE
using System.Collections;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace NDream.AirConsole.EditMode.Tests {
    internal static class RectTestHelper {
        public static void AreRectsEqual(Rect expected, Rect target, string message) {
            Assert.IsTrue(Mathf.Approximately(expected.x, target.x),
                $"{(message != null ? $"{message}: " : string.Empty)}expected and target x not approximately equal: {expected.x} {target.x}");
            Assert.IsTrue(Mathf.Approximately(expected.y, target.y),
                $"{(message != null ? $"{message}: " : string.Empty)}expected and target y not approximately equal: {expected.y} {target.y}");
            Assert.IsTrue(Mathf.Approximately(expected.width, target.width),
                $"{(message != null ? $"{message}: " : string.Empty)}expected and target width not approximately equal: {expected.width} {target.width}");
            Assert.IsTrue(Mathf.Approximately(expected.height, target.height),
                $"{(message != null ? $"{message}: " : string.Empty)}expected and target height not approximately equal: {expected.height} {target.height}");
        }
    }

    public class AirConsoleTests {
        private AirConsoleTestRunner target;

        [TearDown]
        public void TearDown() {
            if (target != null) {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [UnityTest]
        [Timeout(300)]
        public IEnumerator SetSafeArea_WithValidMessage_SafeAreaChangedIsInvoked() {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) {
                Assert.Inconclusive("This test requires an Android build target");
            }

            bool testIsDone = false;
            JObject expectedMessage = JObject.FromObject(
                new {
                    action = "onSetSafeArea",
                    safeArea = JObject.FromObject(new { left = 0.2f, top = 0.1f, width = 0.6f, height = 0.8f })
                });
            Rect expectedRect = new(0.2f * Screen.width, (1 - 0.1f - 0.8f) * Screen.height, 0.6f * Screen.width,
                0.8f * Screen.height);
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.OnSafeAreaChanged += rect => {
                RectTestHelper.AreRectsEqual(expectedRect, rect, "onSafeAreaChanged rect matches expectation");
                RectTestHelper.AreRectsEqual(expectedRect, target.SafeArea, "target.SafeArea matches expectation");
                testIsDone = true;
            };
            target.Initialize();

            target.SetSafeArea(expectedMessage);
            target.Update();

            while (!testIsDone) {
                yield return null;
            }
        }

        [UnityTest]
        [Timeout(300)]
        public IEnumerator SetSafeArea_WithInvalidMessage_ExceptionIsRaised() {
            bool testIsDone = false;
            JObject expectedMessage = JObject.FromObject(
                new {
                    action = "onSetSafeArea"
                });
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();

            try {
                target.SetSafeArea(expectedMessage);
            } catch (UnityException e) {
                Assert.AreEqual(
                    $"OnSetSafeArea called without safeArea property in the message: {expectedMessage.ToString()}",
                    e.Message);
            }

            yield return null;
        }

        [Test]
        [Timeout(300)]
        public void GetConfiguration_AfterReady_ReturnsConfiguration() {
            JObject configuration = JObject.FromObject(new {
                supportedVideoFormats = new[] { "vp9", "h264", "vp8" },
                transparentVideoSupported = true,
                unityVideoSupported = true,
                graphicsQualityTier = "high"
            });
            JObject readyMessage = JObject.FromObject(new {
                action = "ready",
                code = "test123",
                device_id = 0,
                server_time_offset = 0,
                location = "http://test.airconsole.com",
                devices = new object[] { new { location = "http://test.airconsole.com" } },
                configuration
            });
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();

            target.SimulateReady(readyMessage);
            target.Update();

            JToken result = target.GetConfiguration();
            Assert.IsNotNull(result, "Configuration should not be null after ready");
            Assert.AreEqual("high", (string)result["graphicsQualityTier"]);
            Assert.AreEqual(true, (bool)result["transparentVideoSupported"]);
            Assert.AreEqual(true, (bool)result["unityVideoSupported"]);
            string[] formats = result["supportedVideoFormats"].ToObject<string[]>();
            Assert.AreEqual(new[] { "vp9", "h264", "vp8" }, formats);
        }

        [Test]
        [Timeout(300)]
        public void GetConfiguration_BeforeReady_ThrowsNotReadyException() {
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();

            // GetConfiguration() must throw before the READY message has been received.
            Assert.Throws<AirConsole.NotReadyException>(() => target.GetConfiguration());
        }

        [Test]
        [Timeout(300)]
        public void GetConfiguration_AfterResetCaches_ReturnsNull() {
            JObject configuration = JObject.FromObject(new {
                supportedVideoFormats = new[] { "vp9", "h264", "vp8" },
                transparentVideoSupported = true,
                unityVideoSupported = true,
                graphicsQualityTier = "high"
            });
            JObject readyMessage = JObject.FromObject(new {
                action = "ready",
                code = "test123",
                device_id = 0,
                server_time_offset = 0,
                location = "http://test.airconsole.com",
                devices = new object[] { new { location = "http://test.airconsole.com" } },
                configuration
            });
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();

            target.SimulateReady(readyMessage);
            target.Update();
            Assert.IsNotNull(target.GetConfiguration(), "Should have config after ready");

            // Simulate a reconnect / reload that clears caches — the field must be null
            // (not stale) before the subsequent ready message arrives.
            target.SimulateResetCaches();
            Assert.IsNull(target.GetConfiguration(), "Configuration should be null after cache reset");
        }

        [Test]
        [Timeout(300)]
        public void GetConfiguration_WhenReadyDataLacksConfiguration_ReturnsNull() {
            // Ready message without a "configuration" key — server may omit the field.
            JObject readyMessage = JObject.FromObject(new {
                action = "ready",
                code = "test123",
                device_id = 0,
                server_time_offset = 0,
                location = "http://test.airconsole.com",
                devices = new object[] { new { location = "http://test.airconsole.com" } }
            });
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();

            target.SimulateReady(readyMessage);
            target.Update();

            Assert.IsNull(target.GetConfiguration(), "Configuration should be null when not present in ready data");
        }

        [Test]
        [Timeout(300)]
        public void GetConfiguration_WithEmptyConfigObject_ReturnsEmptyJToken() {
            JObject configuration = JObject.FromObject(new { });
            JObject readyMessage = JObject.FromObject(new {
                action = "ready",
                code = "test123",
                device_id = 0,
                server_time_offset = 0,
                location = "http://test.airconsole.com",
                devices = new object[] { new { location = "http://test.airconsole.com" } },
                configuration
            });
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();

            target.SimulateReady(readyMessage);
            target.Update();

            JToken result = target.GetConfiguration();
            Assert.IsNotNull(result, "Configuration should not be null");
            Assert.AreEqual(JTokenType.Object, result.Type, "Configuration should be a JObject");
            Assert.AreEqual(0, result.Children().Count(), "Empty configuration should have no children");
        }

        [Test]
        [Timeout(300)]
        public void GetConfiguration_AfterSecondReady_ReturnsUpdatedConfiguration() {
            JObject firstConfiguration = JObject.FromObject(new {
                graphicsQualityTier = "low"
            });
            JObject firstReadyMessage = JObject.FromObject(new {
                action = "ready",
                code = "test123",
                device_id = 0,
                server_time_offset = 0,
                location = "http://test.airconsole.com",
                devices = new object[] { new { location = "http://test.airconsole.com" } },
                configuration = firstConfiguration
            });
            JObject secondConfiguration = JObject.FromObject(new {
                graphicsQualityTier = "high"
            });
            JObject secondReadyMessage = JObject.FromObject(new {
                action = "ready",
                code = "test123",
                device_id = 0,
                server_time_offset = 0,
                location = "http://test.airconsole.com",
                devices = new object[] { new { location = "http://test.airconsole.com" } },
                configuration = secondConfiguration
            });
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();

            target.SimulateReady(firstReadyMessage);
            target.Update();
            Assert.AreEqual("low", (string)target.GetConfiguration()["graphicsQualityTier"], "First ready should have low quality");

            target.SimulateReady(secondReadyMessage);
            target.Update();
            Assert.AreEqual("high", (string)target.GetConfiguration()["graphicsQualityTier"], "Second ready should have high quality");
        }

        [Test]
        [Timeout(300)]
        public void GetConfiguration_WithPartialFields_ReturnsOnlyProvidedFields() {
            JObject configuration = JObject.FromObject(new {
                graphicsQualityTier = "medium"
            });
            JObject readyMessage = JObject.FromObject(new {
                action = "ready",
                code = "test123",
                device_id = 0,
                server_time_offset = 0,
                location = "http://test.airconsole.com",
                devices = new object[] { new { location = "http://test.airconsole.com" } },
                configuration
            });
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();

            target.SimulateReady(readyMessage);
            target.Update();

            JToken result = target.GetConfiguration();
            Assert.AreEqual("medium", (string)result["graphicsQualityTier"], "Should have graphicsQualityTier");
            Assert.IsNull(result["supportedVideoFormats"], "supportedVideoFormats should be absent");
            Assert.IsNull(result["transparentVideoSupported"], "transparentVideoSupported should be absent");
        }

        public class AirConsoleTestRunner : AirConsole, IMonoBehaviourTest {
            private int frameCount;

            public bool IsTestFinished {
                get => frameCount > 10;
            }

            private void Awake() {
                androidGameVersion = "1";
                base.Awake();
            }

            private void Start() {
                base.Start();
            }

            internal new void Update() {
                frameCount++;
                base.Update();
            }
            
            internal new void FixedUpdate() => base.FixedUpdate();
            internal new void LateUpdate() => base.LateUpdate();
            

            internal new void SetSafeArea(JObject message) {
                base.SetSafeArea(message);
            }

            internal void SimulateReady(JObject message) {
                // Set wsListener._isReady so IsAirConsoleUnityPluginReady() returns true,
                // matching what WebsocketListener.ProcessMessage() does when receiving "onReady".
                var wsListenerField = typeof(AirConsole).GetField("wsListener",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var wsListenerInstance = wsListenerField?.GetValue(this);
                if (wsListenerInstance != null) {
                    var isReadyField = wsListenerInstance.GetType().GetField("_isReady",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    isReadyField?.SetValue(wsListenerInstance, true);
                }

                // Use reflection to invoke the private OnReady method
                var method = typeof(AirConsole).GetMethod("OnReady",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(method, "AirConsole.OnReady private method not found; update SimulateReady if the method was renamed.");
                method.Invoke(this, new object[] { message });
            }

            internal void SimulateResetCaches() {
                // Use reflection to invoke the private ResetCaches method.
                // Passes a no-op action because we do not need the post-clear callback in tests.
                var method = typeof(AirConsole).GetMethod("ResetCaches",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(method, "AirConsole.ResetCaches private method not found; update SimulateResetCaches if the method was renamed.");
                method.Invoke(this, new object[] { (System.Action)(() => { }) });
            }

            internal void Initialize() {
                Awake();
                Start();
            }
        }
    }
}
#endif
