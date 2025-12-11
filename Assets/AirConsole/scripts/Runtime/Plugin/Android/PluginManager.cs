using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class PluginManager {
        private readonly AndroidJavaObject _service;
        private readonly AirConsole _airConsole;

        internal event Action OnReloadWebview;

        internal bool IsInitialized { get; private set; }
        internal string ConnectionUrl { get; private set; }

        /// <summary>
        /// Invoked, when the connection url for for the webview has been resolved.
        /// </summary>
        /// <remarks>Currently only supports UNITY_ANDROID && !UNITY_EDITOR scenarios.</remarks>
        // ReSharper disable once EventNeverSubscribedTo.Global
        internal event Action<string> OnConnectionUrlReceived;

        internal event Action<float> OnUpdateVolume;
        internal event Action<string> OnAudioFocusChange;
        
        internal PluginManager(AirConsole airConsole) {
            GenericUnityPluginCallback<bool> pauseCallback = new(HandlePlatformPauseEvent);

            UnityPluginStringCallback callback = new(
                url => {
                    IsInitialized = true;
                    ConnectionUrl = url;
                    OnConnectionUrlReceived?.Invoke(url);
                },
                error => { AirConsoleLogger.Log(() => $"AndroidDataProvider initialization failed with {error}"); }
            );

            GenericUnityPluginCallback<float> onVolumeChangeCallback = new(volume => {
                AirConsoleLogger.LogDevelopment(() => $"Volume changed to {volume}");
                OnUpdateVolume?.Invoke(volume);
            });

            GenericUnityPluginCallback<string> onAudioFocusChangeCallback = new(focusEvent => {
                AirConsoleLogger.LogDevelopment(() => $"Audio focus event received: {focusEvent}");

                OnAudioFocusChange?.Invoke(focusEvent);
            });
            
            UnityPluginExecutionCallback reloadCallback = new(() => { OnReloadWebview?.Invoke(); });
            _service =
                UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.PluginManager",
                    pauseCallback,
                    reloadCallback,
                    Settings.AIRCONSOLE_BASE_URL,
                    callback,
                    onVolumeChangeCallback,
                    onAudioFocusChangeCallback);

            _airConsole = airConsole;
            if (!_airConsole.IsAutomotive()) {
                _airConsole.UnityPause += AbandonAudioFocus;
                _airConsole.UnityResume += ResumeAudioFocus;
            }

            _airConsole.UnityDestroy += OnDestroy;
            
            AirConsoleLogger.LogDevelopment(() => $"{nameof(PluginManager)} created.");
        }

        internal void ReportPlatformReady() {
            AirConsoleLogger.LogDevelopment(() => "ReportPlatformReady called.");

            _service.Call("reportPlatformReady");
        }

        private void ResumeAudioFocus() => RequestAudioFocus();

        internal bool RequestAudioFocus() {
            AirConsoleLogger.LogDevelopment(() => "RequestAudioFocus called.");
            return _service.Call<bool>("requestAudioFocus");
        }

        internal void AbandonAudioFocus() {
            AirConsoleLogger.LogDevelopment(() => "AbandonAudioFocus called.");
            _service.Call("abandonAudioFocus");
        }
        
//         /// <summary>
//         /// Writes client identification related information using the native library
//         /// </summary>
//         /// <param name="connectCode">The screen connectCode to write.</param>
//         /// <param name="uid">The screen uid to write.</param>
        internal void WriteClientIdentification(string connectCode, string uid) {
            AirConsoleLogger.LogDevelopment(() =>
                $"WriteClientIdentification w/ connectCode: {connectCode}, uid: {uid}");
            _service?.Call("writeClientIdentification", connectCode, uid);
        }

        internal bool IsTV() => _service != null && _service.Call<bool>("isTV");

        internal bool IsAutomotive() => _service != null && _service.Call<bool>("isAutomotive");

        internal bool IsNormalDevice() => _service != null && _service.Call<bool>("isNormalDevice");

        internal void InitializeOfflineCheck() {
            AirConsoleLogger.LogDevelopment(() => "InitializeOfflineCheck called.");

            _service.Call("initializeOfflineCheck");
        }

        private void OnDestroy() {
            _airConsole.UnityPause -= AbandonAudioFocus;
            _airConsole.UnityResume -= ResumeAudioFocus;
            _airConsole.UnityDestroy -= OnDestroy;
            AirConsoleLogger.LogDevelopment(() => "OnDestroy called.");

            _service.Call("onDestroy");
        }

        private static void HandlePlatformPauseEvent(bool isPaused) {
            AirConsoleLogger.LogDevelopment(() => $"HandlePlatformPauseEvent called with {isPaused}");

            PlatformMessageBroker.SendPlatformMessage(isPaused ? "client_pause" : "client_resume");
        }

        private abstract class PlatformMessageBroker {
            /// <summary>
            /// Sends a message of a given type to the platform.
            /// </summary>
            /// <param name="type">Type of the message as determined by the Android requirements.</param>
            internal static void SendPlatformMessage(string type) {
                JObject msg = new() {
                    { "action", "sendPlatformMessage" },
                    { "type", type }
                };
                AirConsole.instance.SendPlatformMessage(msg);
            }
        }
    }
}
