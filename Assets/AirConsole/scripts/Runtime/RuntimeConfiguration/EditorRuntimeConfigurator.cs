#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole {
    using UnityEngine;

    public class EditorRuntimeConfigurator : IRuntimeConfigurator {
        public EditorRuntimeConfigurator() {
            ApplyRequiredSettings();
        }

        public void RefreshConfiguration() {
            ApplyRequiredSettings();
        }

        private void ApplyRequiredSettings() {
            Application.runInBackground = true;
            Application.targetFrameRate = 0;
        }
    }
}
#endif