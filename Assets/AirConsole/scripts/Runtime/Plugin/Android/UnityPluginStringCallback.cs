using System;
using UnityEngine;
// This class is used in AndroidJNI of DataProviderPlugin.cs
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace NDream.AirConsole.Android.Plugin {
    public class UnityPluginStringCallback : AndroidJavaProxy {
        private readonly Action<string> _successCallback;
        private readonly Action<string> _failureCallback;

        public UnityPluginStringCallback(Action<string> successCallback, Action<string> failureCallback) : base("com.airconsole.unityandroidlibrary.UnityPluginStringCallback") {
            _successCallback = successCallback;
            _failureCallback = failureCallback;
        }

        public void onSuccess(string message) {
            AirConsoleLogger.LogDevelopment($"UnityPluginStringCallback receive {message}");
            _successCallback?.Invoke(message);
        }

        public void onFailure(string error) {
            Debug.LogError($"UnityPluginStringCallback received error from Java: {error}");
            _failureCallback?.Invoke("No failure callback provided"); 
        }
    }
}