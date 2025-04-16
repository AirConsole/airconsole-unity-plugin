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
#if AIRCONSOLE_ANDROID
            AndroidJavaObject context = UnityAndroidObjectProvider.GetUnityContext(); 
            audioFocusPlugin = new AndroidJavaObject("com.airconsole.unityandroidlibrary.AudioFocusService", context);
#endif
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
#if AIRCONSOLE_ANDROID
            bool granted = audioFocusPlugin.Call<bool>("requestAudioFocus");
            AirConsoleLogger.Log("Audio focus granted: " + granted);
#endif
        }

        private void AbandonFocus()
        {
#if AIRCONSOLE_ANDROID
            audioFocusPlugin.Call("abandonAudioFocus");
#endif
        } 
    }
}
#endif