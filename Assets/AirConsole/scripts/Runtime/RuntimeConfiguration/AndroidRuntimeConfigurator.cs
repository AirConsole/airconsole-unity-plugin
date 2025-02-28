#if !DISABLE_AIRCONSOLE
#if !UNITY_ANDROID
#undef AIRCONSOLE_AUTOMOTIVE
#endif
using NDream.AirConsole.Android.Plugin;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace NDream.AirConsole {
    // Used in AirConsole.cs based on #if directives
    // ReSharper disable once UnusedType.Global
    public class AndroidRuntimeConfigurator : IRuntimeConfigurator {
        private readonly DataProviderPlugin _androidPlugin;

        public AndroidRuntimeConfigurator(DataProviderPlugin dataProvider) {
            _androidPlugin = dataProvider;
            Application.runInBackground = false;
            Screen.fullScreen = !_androidPlugin.IsAutomotiveDevice();

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // TODO(android-native): Upgrade to 2022 LTS
            Application.targetFrameRate = Mathf.CeilToInt((float)Screen.currentResolution.refreshRateRatio.value);
            QualitySettings.vSyncCount = 0;
        }

        public void RefreshConfiguration() {
            Application.runInBackground = false;
            QualitySettings.vSyncCount = 0;

            Screen.fullScreen = !_androidPlugin.IsAutomotiveDevice();
        }
    }
}
#endif