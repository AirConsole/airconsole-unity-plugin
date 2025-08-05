#if !DISABLE_AIRCONSOLE
using System;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    internal class UnityPluginExecutionCallback : AndroidJavaProxy {
        private readonly Action _executionCallback;

        public UnityPluginExecutionCallback(Action executionCallback)
            : base("com.airconsole.unityandroidlibrary.UnityPluginExecutionCallback") {
            _executionCallback = executionCallback ?? throw new ArgumentNullException(nameof(executionCallback));
        }

        // Implements onExecute, called in Android natively.
        // ReSharper disable once InconsistentNaming
        public void onExecute() {
            if (_executionCallback != null) {
                AirConsoleLogger.LogDevelopment(() => $"UnityPluginExecutionCallback executed");

                _executionCallback.Invoke();
            } else {
                AirConsoleLogger.LogDevelopment(() => "UnityPluginExecutionCallback execution callback is not assigned.");
            }
        }
    }
}
#endif