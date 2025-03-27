#if !DISABLE_AIRCONSOLE

// This class is used in AndroidJNI of DataProviderPlugin.cs
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace NDream.AirConsole.Android.Plugin {
    using System;
    using UnityEngine;

    /// <summary>
    /// AndroidJNI Callback Class implementation for DataProviderPlugin
    /// </summary>
    internal class UnityPluginStringCallback : AndroidJavaProxy {
        private readonly Action<string> _successCallback;
        private readonly Action<string> _failureCallback;

        public UnityPluginStringCallback(Action<string> successCallback, Action<string> failureCallback)
            : base("com.airconsole.unityandroidlibrary.UnityPluginStringCallback") {
            _successCallback = successCallback ?? throw new ArgumentNullException(nameof(successCallback));
            _failureCallback = failureCallback ?? throw new ArgumentNullException(nameof(failureCallback));
        }

        // Implements onSuccess, called in Android natively.
        // ReSharper disable once InconsistentNaming
        public void onSuccess(string message) {
            if (_successCallback != null) {
                AirConsoleLogger.LogDevelopment($"UnityPluginStringCallback received message: {message}");
                _successCallback.Invoke(message);
            } else {
                Debug.LogWarning("Success callback is not assigned.");
            }
        }

        // Implements onFailure, called in Android natively.
        // ReSharper disable once InconsistentNaming
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
#endif