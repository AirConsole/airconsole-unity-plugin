#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole {
    using Android.Plugin;
    using UnityEngine;
    using Screen = UnityEngine.Device.Screen;

    // Used in platform dependant dependency injection.
    // ReSharper disable once UnusedType.Global
    internal class AndroidRuntimeConfigurator : IRuntimeConfigurator {
        private readonly AndroidDataProvider _androidPlugin;

        internal AndroidRuntimeConfigurator(AndroidDataProvider dataProvider) {
            _androidPlugin = dataProvider;

            ApplyRequiredSettings();

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void RefreshConfiguration() {
            ApplyRequiredSettings();
        }

        private void ApplyRequiredSettings() {
            Application.runInBackground = false;
            Screen.fullScreen = !_androidPlugin.IsAutomotiveDevice();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.CeilToInt((float)Screen.currentResolution.refreshRateRatio.value);
        }
    }
}
#endif