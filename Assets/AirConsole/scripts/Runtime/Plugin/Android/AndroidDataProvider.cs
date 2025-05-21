#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Android.Plugin {
    using System;
    using UnityEngine;

    internal class AndroidDataProvider {
        private const int UI_MODE_TYPE_NORMAL = 1;
        private const int UI_MODE_TYPE_CAR = 3;
        private const int UI_MODE_TYPE_TELEVISION = 4;

        private AndroidJavaObject _dataProviderPlugin;

        internal bool DataProviderInitialized { get; private set; }
        internal string ConnectionUrl { get; private set; }

        /// <summary>
        /// Invoked, when the connection url for for the webview has been resolved.
        /// </summary>
        /// <remarks>Currently only supports UNITY_ANDROID && !UNITY_EDITOR scenarios.</remarks>
        // ReSharper disable once EventNeverSubscribedTo.Global
        internal event Action<string> OnConnectionUrlReceived;

        internal AndroidDataProvider() {
            AirConsoleLogger.LogDevelopment("DataProviderPlugin created");
            _dataProviderPlugin = UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.DataProviderService");
            if (_dataProviderPlugin == null) {
                AirConsoleLogger.LogWarning("AndroidDataProvider native could not be initialized");
                return;
            }

            UnityPluginStringCallback callback = new(
                url => {
                    DataProviderInitialized = true;
                    ConnectionUrl = url;
                    OnConnectionUrlReceived?.Invoke(url);
                },
                error => { AirConsoleLogger.Log($"AndroidDataProvider initialization failed with {error}"); }
            );

            _dataProviderPlugin.Call("init", Settings.AIRCONSOLE_BASE_URL, callback);
        }

        /// <summary>
        /// Writes client identification related information using the native library
        /// </summary>
        /// <param name="connectCode">The screen connectCode to write.</param>
        /// <param name="uid">The screen uid to write.</param>
        internal void WriteClientIdentification(string connectCode, string uid) {
            AirConsoleLogger.LogDevelopment($"WriteClientIdentification w/ connectCode: {connectCode}, uid: {uid}");
            _dataProviderPlugin?.Call("writeClientIdentification", connectCode, uid);
        }

        // ReSharper disable once UnusedMember.Global
        internal bool IsTvDevice() => GetUiModeTypeMask() == UI_MODE_TYPE_TELEVISION;

        // ReSharper disable once UnusedMember.Global
        internal bool IsAutomotiveDevice() => GetUiModeTypeMask() == UI_MODE_TYPE_CAR;

        // ReSharper disable once UnusedMember.Global
        internal bool IsNormalDevice() => GetUiModeTypeMask() == UI_MODE_TYPE_NORMAL;

        private int GetUiModeTypeMask() {
            if (_dataProviderPlugin == null) {
                AirConsoleLogger.LogDevelopment("AndroidDataProvider not initialized, return default");
                return UI_MODE_TYPE_NORMAL;
            }

            return _dataProviderPlugin.Call<int>("getUiModeTypeMask");
        }
    }
}
#endif