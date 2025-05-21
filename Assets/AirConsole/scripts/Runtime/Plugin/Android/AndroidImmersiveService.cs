#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;

    public class AndroidImmersiveService {
        private AndroidJavaObject _androidImmersiveService;

        public AndroidImmersiveService() {
            AirConsoleLogger.LogDevelopment("AndroidImmersiveService created.");
            _androidImmersiveService =
                UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.AndroidImmersiveService");
            _androidImmersiveService?.Call("maintainImmersiveModeOnSystemUIChange");
        }
    }
}

#endif