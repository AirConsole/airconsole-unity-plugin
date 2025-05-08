#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;

    internal class AudioFocusService {
        private readonly AndroidJavaObject _audioFocusService;

        internal AudioFocusService() {
            AirConsoleLogger.LogDevelopment("AudioFocusService created.");
            _audioFocusService = UnityAndroidObjectProvider.GetInstanceOfClass("com.airconsole.unityandroidlibrary.AudioFocusService");
            RequestFocus();
            AirConsole.instance.OnApplicationFocusChanged += HandleApplicationFocusChanged;
        }

        internal void Destroy() {
            AirConsole.instance.OnApplicationFocusChanged -= HandleApplicationFocusChanged;
        }

        private void HandleApplicationFocusChanged(bool hasFocus) {
            if (hasFocus) {
                RequestFocus();
            } else {
                AbandonFocus();
            }
        }

        private void RequestFocus() {
            AirConsoleLogger.LogDevelopment("AudioFocusService requesting focus");

            if (_audioFocusService == null) {
                return;
            }

            bool granted = _audioFocusService.Call<bool>("requestAudioFocus");
            AirConsoleLogger.LogDevelopment("Audio focus granted: " + granted);
        }

        private void AbandonFocus() {
            AirConsoleLogger.LogDevelopment("AudioFocusService abandoning focus.");
            _audioFocusService?.Call("abandonAudioFocus");
        }
    }
}
#endif