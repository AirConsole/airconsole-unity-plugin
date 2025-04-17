#if !DISABLE_AIRCONSOLE
#if UNITY_ANDROID && !UNITY_EDITOR
#define AIRCONSOLE_ANDROID
#endif

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;

    internal abstract class UnityAndroidObjectProvider {
        internal static AndroidJavaObject GetUnityContext() {
#if !AIRCONSOLE_ANDROID
            throw new UnityException("UnityAndroidObjectProvider is only supported on Unity Android builds.");
#endif

#if UNITY_6000_0_OR_NEWER
            return UnityEngine.Android.AndroidApplication.currentContext;
#endif

            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        internal static AndroidJavaObject GetUnityActivity() {
#if !AIRCONSOLE_ANDROID
            throw new UnityException("UnityAndroidObjectProvider is only supported on Unity Android builds.");
#endif
#if UNITY_6000_0_OR_NEWER
            return UnityEngine.Android.AndroidApplication.currentActivity;
#endif

            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        internal static AndroidJavaObject GetInstanceOfClass(string className) {
            AndroidJavaObject result = null;
#if AIRCONSOLE_ANDROID
            result = new AndroidJavaObject(className, GetUnityContext());
#endif
            AirConsoleLogger.LogDevelopment($"UnityAndroidObjectProvider.GetInstanceOfClass({className}) was successful: {result != null}");
            return result;
        }
    }
}
#endif