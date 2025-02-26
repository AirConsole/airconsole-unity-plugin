#if !DISABLE_AIRCONSOLE
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace NDream.AirConsole {
    // Used in AirConsole.cs based on #if directives
    // ReSharper disable once UnusedType.Global
    public class WebGLRuntimeConfigurator : IRuntimeConfigurator {
        public WebGLRuntimeConfigurator() {
            Application.runInBackground = true;
            Screen.fullScreen = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = -1;
        }
        
        public void RefreshConfiguration() {
            Application.runInBackground = true;
        }
    }
}
#endif