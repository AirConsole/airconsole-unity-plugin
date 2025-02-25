#if !DISABLE_AIRCONSOLE
#if UNITY_ANDROID && !UNITY_EDITOR
#define ANDROID_NATIVE
#endif

using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class AndroidImmersiveService {
        private AndroidJavaObject androidImmersiveService;

        public AndroidImmersiveService() {
#if ANDROID_NATIVE
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create an instance of your Java plugin class
            androidImmersiveService = new AndroidJavaObject("com.airconsole.unityandroidlibrary.AndroidImmersiveService", context);
            if (androidImmersiveService != null) {
                androidImmersiveService.Call("maintainImmersiveModeOnSystemUIChange");
            } else {
                Debug.LogError("AndroidImmersiveService is not found. Immersive mode will not be maintained.");
            }
#else
            AirConsoleLogger.LogDevelopment("AndroidImmersiveService created.");
#endif
        }
    }
}

#endif