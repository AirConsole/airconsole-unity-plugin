#if !DISABLE_AIRCONSOLE
using System.Collections;
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
            Rect expectedRect = new(0.2f * Screen.width, (1 - 0.1f - 0.8f) * Screen.height, 0.6f * Screen.width, 0.8f * Screen.height);
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
                Assert.AreEqual($"OnSetSafeArea called without safeArea property in the message: {expectedMessage.ToString()}", e.Message);
            }

            yield return null;
        }

        public class AirConsoleTestRunner : AirConsole, IMonoBehaviourTest {
            private int frameCount;

            public bool IsTestFinished => frameCount > 10;

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

            internal new void SetSafeArea(JObject message) {
                base.SetSafeArea(message);
            }

            internal void Initialize() {
                Awake();
                Start();
            }
        }
    }
}
#endif