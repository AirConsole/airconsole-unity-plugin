using System;
using Newtonsoft.Json;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class DataProviderPlugin {
        private AndroidJavaObject dataProviderHelper;

        public DataProviderPlugin() {
#if UNITY_ANDROID && ! UNITY_EDITOR 
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create an instance of your Java plugin class
            AndroidJavaObject dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);
#elif UNITY_ANDROID
            // Do we need to do anything
#else
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
#endif
        }

        // Import the Java class method using JNI
        public ClientConfiguration QueryClientData() {
            ClientConfiguration result = new();
#if UNITY_ANDROID && !UNITY_EDITOR 

            if (dataProviderHelper != null) {
                // Call the method and get the result
                String jsonResponse = dataProviderHelper.Call<string>("queryClientData");
                result = JsonConvert.DeserializeObject<ClientConfiguration>(jsonResponse);
            }
#elif UNITY_ANDROID
            // Do we need to do anything?            
#else
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
#endif
            return result;
        }

        public String GetLocalConfiguration(string key, string defaultValue) {
            string result = defaultValue;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (dataProviderHelper != null) {
                // Call the method and get the result
                result  = dataProviderHelper.Call<string>("getLocalConfig", key, defaultValue);
            }
#elif UNITY_ANDROID
            // Do we need to do anything?            
#else
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
#endif
            return result;
        }

        public void SetLocalConfiguration(string key, string value) {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (dataProviderHelper != null) {
                dataProviderHelper.Call<string>("setLocalConfig", key, value);
            }
#elif UNITY_ANDROID
            // Do we need to do anything?            
#else
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
#endif
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