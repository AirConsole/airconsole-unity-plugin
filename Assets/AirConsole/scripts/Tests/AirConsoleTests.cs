using System.Collections;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NDream.AirConsole.Tests {
    public class AirConsoleTests {
        private AirConsoleTestRunner target;
        
        [TearDown]
        public void TearDown() {
           if(target != null) {
               Object.DestroyImmediate(target.gameObject);
           } 
        }
        
        [UnityTest]
        [Timeout(300)]
        public IEnumerator SetSafeArea_WithValidMessage_SafeAreaChangedIsInvoked() {
            bool testIsDone = false;
            JObject expectedMessage = JObject.FromObject(
                new {
                    action = "onSetSafeArea",
                    safeArea = JObject.FromObject(new { left = 0.2f, top = 0.1f, width = 0.6f, height = 0.8f })
                });
            Rect expectedRect = new(0.2f * Screen.width, (1-0.1f) * Screen.height, 0.6f * Screen.width, 0.8f * Screen.height);
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.OnSafeAreaChanged += rect => {
                Assert.AreEqual(expectedRect, rect);
                Assert.AreEqual(expectedRect, target.SafeArea);
                testIsDone = true;
            };

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
            }

           public new void Update() {
                frameCount++;
                base.Update();
            }

            internal new void SetSafeArea(JObject message) {
                base.SetSafeArea(message);
            }
        }
    }
}