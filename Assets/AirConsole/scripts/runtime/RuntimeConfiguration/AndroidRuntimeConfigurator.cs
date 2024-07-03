#if !DISABLE_AIRCONSOLE
#if !UNITY_ANDROID
#undef AIRCONSOLE_AUTOMOTIVE
#endif
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace NDream.AirConsole {
    // Used in AirConsole.cs based on #if directives
    // ReSharper disable once UnusedType.Global
    public class AndroidRuntimeConfigurator : IRuntimeConfigurator {
        public AndroidRuntimeConfigurator() {
            Application.runInBackground = false;
            Screen.fullScreen = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 0;
        }
        
        public void RefreshConfiguration() {
            Application.runInBackground = false;
        }
    }
}
#endif