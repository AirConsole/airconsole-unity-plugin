#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;

    public class AndroidImmersiveService {
        public AndroidImmersiveService() {
            AirConsoleLogger.LogDevelopment(() => "AndroidImmersiveService created.");

            AndroidJavaObject androidImmersiveService
                = UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.AndroidImmersiveService");
            androidImmersiveService?.Call("maintainImmersiveModeOnSystemUIChange");
        }
    }
}

#endif