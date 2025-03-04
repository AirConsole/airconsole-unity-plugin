using System;
using UnityEngine;
// This class is used in AndroidJNI of DataProviderPlugin.cs
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace NDream.AirConsole.Android.Plugin {
    /// <summary>
    /// AndroidJNI Callback Class implementation for DataProviderPlugin
    /// </summary>
    public class UnityPluginStringCallback : AndroidJavaProxy {
        private readonly Action<string> _successCallback;
        private readonly Action<string> _failureCallback;

        public UnityPluginStringCallback(Action<string> successCallback, Action<string> failureCallback)
            : base("com.airconsole.unityandroidlibrary.UnityPluginStringCallback") {
            _successCallback = successCallback ?? throw new ArgumentNullException(nameof(successCallback));
            _failureCallback = failureCallback ?? throw new ArgumentNullException(nameof(failureCallback));
        }

        public void onSuccess(string message) {
            if (_successCallback != null) {
                AirConsoleLogger.LogDevelopment($"UnityPluginStringCallback received message: {message}");
                _successCallback.Invoke(message);
            } else {
                Debug.LogWarning("Success callback is not assigned.");
            }
        }

        public void onFailure(string error) {
            if (_failureCallback != null) {
                Debug.LogError($"UnityPluginStringCallback failed with error: {error}");
                _failureCallback.Invoke(error);
            } else {
                Debug.LogWarning("Failure callback is not assigned.");
            }
        }
    }
}