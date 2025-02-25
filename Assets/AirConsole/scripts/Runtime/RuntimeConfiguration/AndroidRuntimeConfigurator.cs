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
            
            // TODO(android-native): Upgrade to 2022 LTS
            Application.targetFrameRate = Mathf.CeilToInt((float)Screen.currentResolution.refreshRateRatio.value);
            QualitySettings.vSyncCount = 0;
        }
        
        public void RefreshConfiguration() {
            Application.runInBackground = false;
            QualitySettings.vSyncCount = 0;
        }
    }
}
#endif