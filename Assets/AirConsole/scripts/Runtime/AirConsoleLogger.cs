
using System.Runtime.CompilerServices;
#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole {
    using System;
    using UnityEngine;
    using Android.Plugin;
    using Object = UnityEngine.Object;

    public static class AirConsoleLogger {
#if AIRCONSOLE_DEVELOPMENT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsDevelopmentLoggingEnabled() => true;
#else
        private static bool? _developmentLoggingEnabled;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDevelopmentLoggingEnabled() {
            if (_developmentLoggingEnabled == null) {
                _developmentLoggingEnabled = AndroidIntentUtils.GetIntentExtraBool("development_logging", false);
            }

            return _developmentLoggingEnabled.Value;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogDevelopment(Func<string> messageFunction) {
            if (IsDevelopmentLoggingEnabled()) {
                Debug.Log($"AC UNITY DEVELOPMENT: {messageFunction()}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        internal static void LogEditor(string message) => Debug.Log($"AC UNITY EDITOR: {message}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDebugLoggingEnabled() => Debug.unityLogger != null && Debug.unityLogger.IsLogTypeAllowed(LogType.Log);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(Func<string> messageFunction) {
            if (IsDebugLoggingEnabled()) {
                Debug.Log(messageFunction());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(Func<string> messageFunction, Object context) {
            if (IsDebugLoggingEnabled()) {
                Debug.Log(messageFunction(), context);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWarningLoggingEnabled() => Debug.unityLogger != null && Debug.unityLogger.IsLogTypeAllowed(LogType.Warning);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(Func<string> messageFunction) {
            if (IsWarningLoggingEnabled()) {
                Debug.LogWarning(messageFunction());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(Func<string> messageFunction, Object context) {
            if (IsWarningLoggingEnabled()) {
                Debug.LogWarning(messageFunction, context);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsErrorLoggingEnabled() => Debug.unityLogger != null && Debug.unityLogger.IsLogTypeAllowed(LogType.Error);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(Func<string> messageFunction) {
            if (IsErrorLoggingEnabled()) {
                Debug.LogError(messageFunction());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(Func<string> messageFunction, Object context) {
            if (IsErrorLoggingEnabled()) {
                Debug.LogError(messageFunction(), context);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsExceptionLoggingEnabled() =>
            Debug.unityLogger != null && Debug.unityLogger.IsLogTypeAllowed(LogType.Exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException(Exception exception) {
            if (IsExceptionLoggingEnabled()) {
                Debug.LogException(exception);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException(Exception exception, Object context) {
            if (IsExceptionLoggingEnabled()) {
                Debug.LogException(exception, context);
            }
        }
    }
}
#endif
