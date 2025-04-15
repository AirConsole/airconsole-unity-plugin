#if !DISABLE_AIRCONSOLE
#if UNITY_ANDROID && !UNITY_EDITOR
#define AIRCONSOLE_ANDROID
#endif

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;

    public class AndroidImmersiveService {
        private AndroidJavaObject _androidImmersiveService;

        public AndroidImmersiveService() {
#if AIRCONSOLE_ANDROID
            AndroidJavaObject context = UnityAndroidObjectProvider.GetUnityContext(); 

            // Create an instance of your Java plugin class
            _androidImmersiveService = new AndroidJavaObject("com.airconsole.unityandroidlibrary.AndroidImmersiveService", context);
            if (_androidImmersiveService != null) {
                _androidImmersiveService.Call("maintainImmersiveModeOnSystemUIChange");
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