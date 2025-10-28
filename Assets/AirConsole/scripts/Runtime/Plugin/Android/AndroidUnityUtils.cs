namespace NDream.AirConsole.Android.Plugin {
    // ReSharper disable RedundantUsingDirective
    using UnityEngine;

    internal static class AndroidIntentUtils {
        public static string GetIntentExtraString(string key, string defaultValue) {
#if !UNITY_ANDROID || UNITY_EDITOR
            return defaultValue;
#endif
            try {
                using (AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer")) {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
                    return intent.Call<string>("getStringExtra", key) ?? defaultValue;
                }
            } catch (System.Exception e) {
                AirConsoleLogger.LogWarning(() => "Error getting intent extra: " + e);
                return defaultValue;
            }
        }

        public static bool GetIntentExtraBool(string key, bool defaultValue) {
#if !UNITY_ANDROID || UNITY_EDITOR
            return defaultValue;
#endif
            try
            {
                using (AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"); 
                    return intent.Call<bool>("getBooleanExtra", key, defaultValue);
                }
            }
            catch (System.Exception e)
            {
                AirConsoleLogger.LogWarning(() => "Error getting intent extra: " + e);
                return defaultValue;
            }
        }
    }
    // ReSharper enable RedundantUsingDirective
}