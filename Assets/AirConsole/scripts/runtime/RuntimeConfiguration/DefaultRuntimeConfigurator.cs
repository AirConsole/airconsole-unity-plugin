using UnityEngine;
using UnityEngine.Android;
using Screen = UnityEngine.Device.Screen;

namespace NDream.AirConsole {
    public class DefaultRuntimeConfigurator : IRuntimeConfigurator {
        public DefaultRuntimeConfigurator() {
            Application.runInBackground = false;
            Screen.fullScreen = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = Application.platform == RuntimePlatform.WebGLPlayer ? -1 : 0;
        }
        
        public void RefreshConfiguration() {
            Application.runInBackground = false;
        }
    }
}