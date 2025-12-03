#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole {
    using UnityEngine;
    using Screen = UnityEngine.Device.Screen;

    // Used in AirConsole.cs based on #if directives
    // ReSharper disable once UnusedType.Global
    public class WebGLRuntimeConfigurator : IRuntimeConfigurator {
        public WebGLRuntimeConfigurator() {
            ApplyRequiredSettings();

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void RefreshConfiguration() {
            ApplyRequiredSettings();
        }

        private void ApplyRequiredSettings() {
            Application.runInBackground = true;
            Screen.fullScreen = true;
            Application.targetFrameRate = -1;
        }
    }
}
#endif
