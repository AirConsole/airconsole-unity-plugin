#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole {
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class AirConsoleLogger {
        [System.Diagnostics.Conditional("AIRCONSOLE_DEVELOPMENT")]
        public static void LogDevelopment(string message) => Debug.Log($"AC UNITY DEVELOPMENT: {message}");

        [System.Diagnostics.Conditional("AIRCONSOLE_DEVELOPMENT")]
        public static void LogDevelopment(string message, Object context) => Debug.Log($"AC UNITY DEVELOPMENT: {message}", context);

        public static void Log(string message) => Debug.Log(message);
        public static void Log(string message, Object context) => Debug.Log(message, context);
        public static void LogWarning(string message) => Debug.LogWarning(message);
        public static void LogWarning(string message, Object context) => Debug.LogWarning(message, context);
        public static void LogError(string message) => Debug.LogError(message);
        public static void LogError(string message, Object context) => Debug.LogError(message, context);
        public static void LogException(Exception exception) => Debug.LogException(exception);
        public static void LogException(Exception exception, Object context) => Debug.LogException(exception, context);
    }
}
#endif
