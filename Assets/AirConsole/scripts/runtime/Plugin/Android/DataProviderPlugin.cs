using System;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class DataProviderPlugin {
        private AndroidJavaObject dataProviderHelper;

        private bool CheckLibrary() {
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create an instance of your Java plugin class
            dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);

            return dataProviderHelper != null;
        }

        public DataProviderPlugin() {
#if UNITY_ANDROID
#if !UNITY_EDITOR
            if (!CheckLibrary()) {
                Debug.Log("DataProviderPlugin is not initialized");
                return;
            }
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create an instance of your Java plugin class
            dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);
            dataProviderHelper.Call("init", Settings.AIRCONSOLE_BASE_URL);
#endif
#else
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
#endif
        }

        /// <summary>
        /// Returns the connection base url
        /// </summary>
        /// <returns>The connection base url to append to the backend service base url</returns>
        public String GetConnectionBaseUrl() {
            // return "client?id=bmw-idc-23&runtimePlatform=android&unityPluginVersion=2.6.0";
            // return "client?id=androidunity-4.0&runtimePlatform=android&unityPluginVersion=2.6.0";
            // but should be
            //      return "client?id=androidunity-2.60&runtimePlatform=android&unityPluginVersion=2.6.0";
#if UNITY_ANDROID
#if !UNITY_EDITOR
            if (dataProviderHelper != null) {
                return $"client?{dataProviderHelper.Call<string>("getConnectionUrl")}&unityPluginVersion={ComputeUrlVersion(Settings.VERSION)}";
            } else {
              Debug.Log("DataProviderPlugin is not initialized");
            }
#else
            string urlVersion = ComputeUrlVersion(Settings.VERSION);
            return $"client?id=androidunity-{urlVersion}&runtimePlatform=android&unityPluginVersion={urlVersion}";
#endif
#endif
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
        }

        private static string ComputeUrlVersion(string version) {
            string[] split = version.Split('.');
            return $"{split[0]}.{split[1]}{split[2]}";
        }
    }
}