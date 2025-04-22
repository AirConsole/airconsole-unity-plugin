#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;

    internal abstract class UnityAndroidObjectProvider {
        // ReSharper disable once MemberCanBePrivate.Global
        internal static AndroidJavaObject GetUnityContext() {
            if (!AirConsole.IsAndroidOrEditor) {
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
            if (!AirConsole.IsAndroidOrEditor) {
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
            if (AirConsole.IsAndroidOrEditor) {
                result = new AndroidJavaObject(className, GetUnityContext());
            }

            AirConsoleLogger.LogDevelopment($"UnityAndroidObjectProvider.GetInstanceOfClass({className}) was successful: {result != null}");
            return result;
        }
    }
}
#endif