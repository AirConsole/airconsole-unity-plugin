#if !DISABLE_AIRCONSOLE
#if UNITY_ANDROID && !UNITY_EDITOR
#define AIRCONSOLE_ANDROID
#endif

namespace NDream.AirConsole.Android.Plugin {
    using UnityEngine;
    internal class AudioFocusService {
        private AndroidJavaObject audioFocusPlugin;

        internal AudioFocusService()
        {
            AndroidJavaObject context = UnityAndroidObjectProvider.GetUnityContext(); 
            audioFocusPlugin = new AndroidJavaObject("com.airconsole.unityandroidlibrary.AudioFocusService", context);
            RequestFocus();
            AirConsole.instance.OnApplicationFocusChanged += OnApplicationFocus;
        }

        internal void Destroy () {
            AirConsole.instance.OnApplicationFocusChanged -= OnApplicationFocus;
        }

        private void OnApplicationFocus(bool hasFocus) {
            if (hasFocus) {
                RequestFocus();
            } else {
                AbandonFocus();
            }
        }

        private void RequestFocus()
        {
            bool granted = audioFocusPlugin.Call<bool>("requestAudioFocus");
            AirConsoleLogger.Log("Audio focus granted: " + granted);
        }

        private void AbandonFocus()
        {
            audioFocusPlugin.Call("abandonAudioFocus");
        } 
    }
}
#endif