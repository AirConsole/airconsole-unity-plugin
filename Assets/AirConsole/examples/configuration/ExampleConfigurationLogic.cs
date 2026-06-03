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

            // Video format support
            JToken formats = config["supportedVideoFormats"];
            if (formats != null) {
                logWindow.text += "Video Formats: " + formats + "\n";
            }

            // Graphics quality tier
            JToken tier = config["graphicsQualityTier"];
            if (tier != null) {
                logWindow.text += "Graphics Tier: " + (string)tier + "\n";
            }

            // Video capability flags
            JToken transparent = config["transparentVideoSupported"];
            if (transparent != null) {
                logWindow.text += "Transparent Video: " + (bool)transparent + "\n";
            }

            JToken unityVideo = config["unityVideoSupported"];
            if (unityVideo != null) {
                logWindow.text += "Unity Video: " + (bool)unityVideo + "\n";
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
