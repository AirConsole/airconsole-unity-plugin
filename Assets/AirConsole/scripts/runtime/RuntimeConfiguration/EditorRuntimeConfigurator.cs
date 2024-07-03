#if !DISABLE_AIRCONSOLE
#if !UNITY_ANDROID
#undef AIRCONSOLE_AUTOMOTIVE
#endif
using UnityEngine;

namespace NDream.AirConsole {
    public class EditorRuntimeConfigurator : IRuntimeConfigurator {
        public EditorRuntimeConfigurator() {
            Application.runInBackground = true;
            Application.targetFrameRate = 0;
        }
        
        public void RefreshConfiguration() {
            Application.runInBackground = true;
        }
    }
}
#endif