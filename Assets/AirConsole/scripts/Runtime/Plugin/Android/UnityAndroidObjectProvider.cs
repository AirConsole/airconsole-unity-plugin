using System.Collections.Generic;
using System.Linq;
#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;

    internal abstract class UnityAndroidObjectProvider {
        // ReSharper disable once MemberCanBePrivate.Global
        internal static AndroidJavaObject GetUnityContext() {
            if (!AirConsole.IsAndroidRuntime) {
                throw new UnityException("UnityAndroidObjectProvider is only supported on Unity Android builds.");
            }

#if UNITY_6000_0_OR_NEWER
            return UnityEngine.Android.AndroidApplication.currentContext;
#endif

            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        // ReSharper disable once MemberCanBePrivate.Global
        internal static AndroidJavaObject GetUnityActivity() {
            if (!AirConsole.IsAndroidRuntime) {
                throw new UnityException("UnityAndroidObjectProvider is only supported on Unity Android builds.");
            }
            
#if UNITY_6000_0_OR_NEWER
            return UnityEngine.Android.AndroidApplication.currentActivity;
#endif

            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        // ReSharper disable once MemberCanBePrivate.Global
        internal static AndroidJavaObject GetInstanceOfClass(string className) {
            AndroidJavaObject result = null;
            if (AirConsole.IsAndroidRuntime) {
                result = new AndroidJavaObject(className, GetUnityActivity());
            }

            AirConsoleLogger.LogDevelopment($"UnityAndroidObjectProvider.GetInstanceOfClass({className}) was successful: {result != null}");
            return result;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        internal static AndroidJavaObject GetInstanceOfClass(string className, params object[] parameters) {
            AndroidJavaObject result = null;
            if (AirConsole.IsAndroidRuntime) {
                IEnumerable<object> parametersList = new List<object>(parameters);
                parametersList = parametersList.Prepend(GetUnityActivity());
                result = new AndroidJavaObject(className, parametersList.ToArray());
            }

            AirConsoleLogger.LogDevelopment($"UnityAndroidObjectProvider.GetInstanceOfClass({className}) was successful: {result != null}");
            return result;
        }
    }
}
#endif