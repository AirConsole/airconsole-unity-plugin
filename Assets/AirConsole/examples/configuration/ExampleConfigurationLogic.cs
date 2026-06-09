namespace NDream.AirConsole.Examples {
    using UnityEngine;
    using Newtonsoft.Json.Linq;

    public class ExampleConfigurationLogic : MonoBehaviour {
        public TMPro.TMP_Text logWindow;

#if !DISABLE_AIRCONSOLE
        private void Awake() {
            AirConsole.instance.onReady += OnReady;
            AirConsole.instance.onConnect += OnConnect;
            logWindow.text = "Waiting for AirConsole...\n";
        }

        private void OnReady(string code) {
            logWindow.text = "AirConsole ready!\n\n";

            JToken config = AirConsole.instance.GetGameConfiguration();
            if (config == null) {
                logWindow.text += "Configuration: not provided by platform\n";
                return;
            }

            logWindow.text += "=== Platform Configuration ===\n";

            // Video capability flags
            JToken transparentVideoSupport = config["transparentVideoSupport"];
            if (transparentVideoSupport != null) {
                logWindow.text += "Supports transparent video: " + (bool)transparentVideoSupport + "\n";
            }

            JToken unityVideoSupport = config["unityVideoSupport"];
            if (unityVideoSupport != null) {
                logWindow.text += "Supports video playback in Unity: " + (bool)unityVideoSupport + "\n";
            }

            // Demonstrate safe access for future/optional fields
            JToken touchScreen = config["touchScreen"];
            logWindow.text += "Touch Screen: " + (touchScreen != null ? touchScreen.ToString() : "N/A") + "\n";
        }

        private void OnConnect(int deviceId) {
            logWindow.text += "Device " + deviceId + " connected\n";
        }

        private void OnDestroy() {
            if (AirConsole.instance != null) {
                AirConsole.instance.onReady -= OnReady;
                AirConsole.instance.onConnect -= OnConnect;
            }
        }
#endif
    }
}
