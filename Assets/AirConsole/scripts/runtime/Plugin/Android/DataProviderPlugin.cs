using System;
using Newtonsoft.Json;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class DataProviderPlugin {
        // Import the Java class method using JNI
        public static ClientConfiguration QueryClientData() {
            ClientConfiguration result = new();
#if UNITY_ANDROID && !UNITY_EDITOR || true
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create an instance of your Java plugin class
            AndroidJavaObject dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);

            // Call the method and get the result
            String jsonResponse = dataProviderHelper.Call<string>("queryClientData");
            result = JsonConvert.DeserializeObject<ClientConfiguration>(jsonResponse);
#elif UNITY_ANDROID
            // Do we need to do anything?            
#else
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
#endif
            return result;
        }
    }

    public class ClientConfiguration {
        [JsonProperty("clientId")]
        public string Id { get; private set; } = $"androidunity-{ComputeUrlVersion(Settings.VERSION)}";

        [JsonProperty("clientPlatform")]
        public string Platform { get; private set; } = "androidunity";
        
        [JsonProperty("premiumId")]
        public string PremiumId { get; private set; }

        public override string ToString() {
            return $"Client Id: \"{Id}\", Platform: \"{Platform}\", Premium Id: \"{PremiumId}\"";
        }

        private static string ComputeUrlVersion(string version) {
            string[] split = version.Split('.');
            return $"{split[0]}.{split[1]}{split[2]}";
        }
    }
}