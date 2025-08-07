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
        
        internal PluginManager(AirConsole airConsole) {
            AirConsoleLogger.LogDevelopment(() => $"{nameof(PluginManager)} created.");

            GenericUnityPluginCallback<bool> pauseCallback = new(HandlePlatformPauseEvent);
            
            UnityPluginStringCallback callback = new(
                url => {
                    IsInitialized = true;
                    ConnectionUrl = url;
                    OnConnectionUrlReceived?.Invoke(url);
                },
                error => { AirConsoleLogger.Log(() => $"AndroidDataProvider initialization failed with {error}"); }
            );
            
            UnityPluginExecutionCallback reloadCallback = new(() => { OnReloadWebview?.Invoke(); });
            _service =
                UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.PluginManager",
                    pauseCallback,
                    reloadCallback,
                    Settings.AIRCONSOLE_BASE_URL,
                    callback);

            _airConsole = airConsole;
            _airConsole.UnityPause += OnPause;
            _airConsole.UnityResume += OnResume;
            _airConsole.UnityDestroy += OnDestroy;
        }

        internal void ReportPlatformReady() {
            AirConsoleLogger.LogDevelopment(() => "ReportPlatformReady called.");

            _service.Call("reportPlatformReady");
        }
        
//         /// <summary>
//         /// Writes client identification related information using the native library
//         /// </summary>
//         /// <param name="connectCode">The screen connectCode to write.</param>
//         /// <param name="uid">The screen uid to write.</param>
         internal void WriteClientIdentification(string connectCode, string uid) {
             AirConsoleLogger.LogDevelopment(() => $"WriteClientIdentification w/ connectCode: {connectCode}, uid: {uid}");
             _service?.Call("writeClientIdentification", connectCode, uid);
         }
        
         internal bool IsTV() {
             return _service != null && _service.Call<bool>("isTV");
         }
         internal bool IsAutomotive() {
             return _service != null && _service.Call<bool>("isAutomotive");
         }
         internal bool IsNormalDevice() {
             return _service != null && _service.Call<bool>("isNormalDevice");
         }

        internal void InitializeOfflineCheck() {
            AirConsoleLogger.LogDevelopment(() => "InitializeOfflineCheck called.");

            _service.Call("initializeOfflineCheck");
        }

        private void OnPause() {
            AirConsoleLogger.LogDevelopment(() => "OnPause called.");

            _service.Call("onPause");
        }

        private void OnResume() {
            AirConsoleLogger.LogDevelopment(() => "OnResume called.");

            _service.Call("onResume");
        }

        private void OnDestroy() {
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