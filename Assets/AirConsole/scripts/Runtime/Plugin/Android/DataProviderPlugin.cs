#if UNITY_ANDROID && !UNITY_EDITOR
#define AIRCONSOLE_ANDROID
#endif
using System;
using UnityEngine;


namespace NDream.AirConsole.Android.Plugin {
    public class DataProviderPlugin {
        private AndroidJavaObject dataProviderHelper;

        public bool DataProviderInitialized { get; private set; }
        public string ConnectionUrl { get; private set; }
        public event Action<string> OnConnectionUrlReceived;

        private const int UI_MODE_TYPE_TELEVISION = 4;
        private const int UI_MODE_TYPE_CAR = 3;
        private const int UI_MODE_TYPE_NORMAL = 1;

        // ReSharper disable once UnusedMember.Local -- Used by DataProviderPlugin() in Android NonEditor only
        private bool CheckLibrary() {
            if (dataProviderHelper != null) return true;

            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);

            return dataProviderHelper != null;
        }

        public DataProviderPlugin() {
#if AIRCONSOLE_ANDROID
            if (!CheckLibrary()) {
                Debug.LogWarning("DataProviderPlugin native plugin could not be initialized");
                return;
            }

            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            UnityPluginStringCallback callback = new(
                url => {
                    DataProviderInitialized = true;
                    ConnectionUrl = url;
                    OnConnectionUrlReceived?.Invoke(url);
                    Debug.Log($"Received URL: {url}");
                },
                error => { Debug.LogError($"DataProviderPlugin initialization failed with {error}"); }
            );

            // Create an instance of your Java plugin class
            dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);
            dataProviderHelper.Call("init", Settings.AIRCONSOLE_BASE_URL, callback);
#endif
            AirConsoleLogger.LogDevelopment("DataProviderPlugin created.");
        }

        public bool IsTvDevice() {
            return GetDeviceTypeMask() == UI_MODE_TYPE_TELEVISION;
        }

        public bool IsAutomotiveDevice() {
            return GetDeviceTypeMask() == UI_MODE_TYPE_CAR;
        }

        public bool IsNormalDevice() {
            return GetDeviceTypeMask() == UI_MODE_TYPE_NORMAL;
        }

        private int GetDeviceTypeMask() {
#if AIRCONSOLE_ANDROID
            if (!CheckLibrary()) {
                Debug.LogWarning("DataProviderPlugin native plugin could not be initialized");
                return 0;
            }
            return dataProviderHelper.Call<int>("getDeviceTypeMask");
#endif
            return 0;
        }
    }
}