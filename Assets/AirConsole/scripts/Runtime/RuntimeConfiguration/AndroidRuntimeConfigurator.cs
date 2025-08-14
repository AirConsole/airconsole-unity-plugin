#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole {
    using Android.Plugin;
    using UnityEngine;
    using Screen = UnityEngine.Device.Screen;

    // Used in platform dependant dependency injection.
    // ReSharper disable once UnusedType.Global
    internal class AndroidRuntimeConfigurator : IRuntimeConfigurator {
        internal AndroidRuntimeConfigurator() {
            ApplyRequiredSettings();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void RefreshConfiguration() {
            ApplyRequiredSettings();
        }

        private void ApplyRequiredSettings() {
            Application.runInBackground = false;

            // To ensure consistent behavior and layout on cars where custom safe areas can be in use,
            //  we ensure to run in fullscreen for it to be treated correctly.
            // In the optimal case we could use _pluginManager.IsAutomotiveDevice() to decide more granularly.
            Screen.fullScreen = true; 
            // Car OEMs can modify some of the standard android behavior so we want to make sure to be vSync aligned.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.CeilToInt((float)Screen.currentResolution.refreshRateRatio.value);
        }
    }
}
#endif