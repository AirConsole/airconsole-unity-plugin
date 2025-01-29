using System;
using Newtonsoft.Json;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class UnityPluginStringCallback : AndroidJavaProxy {
        private Action<string> _successCallback;
        private Action<string> _failureCallback;

        public UnityPluginStringCallback(Action<string> successCallback, Action<string> failureCallback) : base("com.airconsole.unityandroidlibrary.UnityPluginStringCallback") {
            _successCallback = successCallback;
            _failureCallback = failureCallback;
        }

        public void onSuccess(string message) {
            _successCallback?.Invoke(message);
        }

        public void onFailure(string message) {
            Debug.LogError($"Received error from Java: {message}");
            _failureCallback?.Invoke("No failure callback provided"); 
        }
    }
}