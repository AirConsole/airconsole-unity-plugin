#if !DISABLE_AIRCONSOLE
#if !UNITY_ANDROID
#undef AIRCONSOLE_AUTOMOTIVE
#endif
using System.Linq;
using UnityEngine;
using UnityEngine.Android;

namespace NDream.AirConsole {
    // Used in AirConsole.cs based on #if directives
    // ReSharper disable once UnusedType.Global
    public class AutomotiveRuntimeConfigurator : IRuntimeConfigurator {
        public AutomotiveRuntimeConfigurator() {
            Application.runInBackground = false;
            Screen.fullScreen = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 0;
            
            // On automotive we want to ensure that we are not negatively impacted by external pressure.
            AndroidDevice.SetSustainedPerformanceMode(true);
        }
        
        public void RefreshConfiguration() {
            if(Input.touchCount > 0 && Input.touches.Any(it => it.phase == TouchPhase.Ended)) {
                Screen.fullScreen = !Screen.fullScreen;
            }
            Application.runInBackground = false;
            // Screen.fullScreen = false;
        }
    }
}
#endif