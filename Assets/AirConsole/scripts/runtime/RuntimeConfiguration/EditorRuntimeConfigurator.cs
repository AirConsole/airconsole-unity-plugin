using UnityEngine;
using UnityEngine.Android;

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