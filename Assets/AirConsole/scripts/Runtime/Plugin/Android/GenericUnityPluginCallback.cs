#if !DISABLE_AIRCONSOLE
using System;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    internal class GenericUnityPluginCallback<T> : AndroidJavaProxy {
        private readonly Action<T> _callback;

        public GenericUnityPluginCallback(Action<T> callback)
            : base("com.airconsole.unityandroidlibrary.GenericUnityPluginCallback") {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        // Implements onExecute, called in Android natively.
        // ReSharper disable once InconsistentNaming
        public void onExecute(T value) {
            if (_callback != null) {
                AirConsoleLogger.LogDevelopment(() => $"GenericUnityPluginCallback executed {value}");

                _callback.Invoke(value);
            } else {
                AirConsoleLogger.LogDevelopment(() => "Execution callback is not assigned.");
            }
        }
    }
}
#endif
