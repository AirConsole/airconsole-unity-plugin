#if UNITY_ANDROID && !UNITY_EDITOR
#define AIRCONSOLE_ANDROID
#endif

namespace NDream.AirConsole.Android.Plugin {
    using System;
    using UnityEngine;

    internal class AndroidDataProvider {
        private const int UI_MODE_TYPE_NORMAL = 1;
        private const int UI_MODE_TYPE_CAR = 3;
        private const int UI_MODE_TYPE_TELEVISION = 4;

#if AIRCONSOLE_ANDROID
        private AndroidJavaObject _dataProviderHelper;

        internal bool DataProviderInitialized { get; private set; }
        internal string ConnectionUrl { get; private set; }
        internal event Action<string> OnConnectionUrlReceived;

        private bool CheckLibrary() {
            if (_dataProviderHelper != null) {
                return true;
            }

            AndroidJavaObject context = UnityAndroidObjectProvider.GetUnityContext(); 
            _dataProviderHelper = new AndroidJavaObject("com.airconsole.unityandroidlibrary.DataProviderService", context);

            return _dataProviderHelper != null;
        }
#endif

        internal AndroidDataProvider() {
#if AIRCONSOLE_ANDROID
            if (!CheckLibrary()) {
                AirConsoleLogger.LogWarning("AndroidDataProvider native could not be initialized");
                return;
            }

            UnityPluginStringCallback callback = new(
                url => {
                    DataProviderInitialized = true;
                    ConnectionUrl = url;
                    OnConnectionUrlReceived?.Invoke(url);
                    AirConsoleLogger.LogDevelopment($"Received URL: {url}");
                },
                error => { AirConsoleLogger.Log($"AndroidDataProvider initialization failed with {error}"); }
            );

            _dataProviderHelper.Call("init", Settings.AIRCONSOLE_BASE_URL, callback);
#endif
            AirConsoleLogger.LogDevelopment("DataProviderPlugin created.");
        }

        /// <summary>
        /// Writes client identification related information using the native library
        /// </summary>
        /// <param name="connectCode">The screen connectCode to write.</param>
        /// <param name="uid">The screen uid to write.</param>
        internal void WriteClientIdentification(String connectCode, String uid) {
            AirConsoleLogger.LogDevelopment($"WriteClientIdentification w/ connectCode: {connectCode}, uid: {uid}");
#if AIRCONSOLE_ANDROID
            _dataProviderHelper.Call("writeClientIdentification", connectCode, uid);
#endif
        }

        // ReSharper disable once UnusedMember.Global
        internal bool IsTvDevice() => GetUiModeTypeMask() == UI_MODE_TYPE_TELEVISION;

        // ReSharper disable once UnusedMember.Global
        internal bool IsAutomotiveDevice() => GetUiModeTypeMask() == UI_MODE_TYPE_CAR;

        // ReSharper disable once UnusedMember.Global
        internal bool IsNormalDevice() => GetUiModeTypeMask() == UI_MODE_TYPE_NORMAL;

        private int GetUiModeTypeMask() {
#if AIRCONSOLE_ANDROID
            if (!CheckLibrary()) {
                AirConsoleLogger.LogWarning("AndroidDataProvider native could not be initialized");
                return UI_MODE_TYPE_NORMAL;
            }

            return _dataProviderHelper.Call<int>("getUiModeTypeMask");
#endif
            return UI_MODE_TYPE_NORMAL;
        }
    }
}