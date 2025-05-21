using System;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class OfflineOverlayService {
        internal event Action OnReloadWebview;

        private readonly AndroidJavaObject _service;

        internal OfflineOverlayService() {
            AirConsoleLogger.LogDevelopment($"{nameof(OfflineOverlayService)} created.");

            UnityPluginExecutionCallback callback = new(() => { OnReloadWebview?.Invoke(); });
            _service =
                UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.OfflineOverlayService", callback);
        }

        internal void Destroy() {
            AirConsoleLogger.LogDevelopment("Destroy called.");
            _service.Call("destroy");
        }

        internal void InitializeOfflineCheck() {
            AirConsoleLogger.LogDevelopment("InitializeOfflineCheck called.");
            _service.Call("initializeOfflineCheck");
        }

        internal void ReportPlatformReady() {
            AirConsoleLogger.LogDevelopment("ReportPlatformReady called.");
            _service.Call("reportPlatformReady");
        }

        private void HandleWebviewReload() { }

        private class ExecuteReloadHandler : UnityPluginExecutionCallback {
            public ExecuteReloadHandler(Action executionCallback) : base(executionCallback) { }
        }
    }
}