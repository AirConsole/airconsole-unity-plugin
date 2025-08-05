using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class PluginManager {
        private readonly AndroidJavaObject _service;
        private readonly AirConsole _airConsole;

        internal event Action OnReloadWebview;

        internal PluginManager(AirConsole airConsole) {
            AirConsoleLogger.LogDevelopment(() => $"{nameof(PluginManager)} created.");

            GenericUnityPluginCallback<bool> pauseCallback = new(HandlePlatformPauseEvent);

            UnityPluginExecutionCallback reloadCallback = new(() => { OnReloadWebview?.Invoke(); });
            _service =
                UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.PluginManager",
                    pauseCallback,
                    reloadCallback);

            _airConsole = airConsole;
            _airConsole.UnityPause += OnPause;
            _airConsole.UnityResume += OnResume;
            _airConsole.UnityDestroy += OnDestroy;
        }

        internal void ReportPlatformReady() {
            AirConsoleLogger.LogDevelopment(() => "ReportPlatformReady called.");

            _service.Call("reportPlatformReady");
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