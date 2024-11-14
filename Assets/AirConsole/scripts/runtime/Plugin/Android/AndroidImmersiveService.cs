#if !DISABLE_AIRCONSOLE

using System;
using System.Collections;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class AndroidImmersiveService {
        private AndroidJavaObject androidImmersiveService;


        private const int VIEW_SYSTEM_UI_FLAG_FULLSCREEN = 0x00000004;
        private const int VIEW_SYSTEM_UI_FLAG_HIDE_NAVIGATION = 0x00000002;
        private const int VIEW_SYSTEM_UI_FLAG_IMMERSIVE_STICKY = 0x00001000;
        private AndroidJavaObject _decorView;
        private Coroutine _reenterImmersiveMode;

        public void AndroidImmersiveServiceOld() {
            AirConsoleLogger.LogDevelopment("AndroidImmersiveService");
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var window = activity.Call<AndroidJavaObject>("getWindow");
            AndroidJavaObject _decorView = window.Call<AndroidJavaObject>("getDecorView");
        }
#endif

#if UNITY_ANDROID
            EnterImmersiveMode();
            AirConsole.instance.StartCoroutine(UpdateLoop());
#endif
        }

        IEnumerator UpdateLoop() {
            while (true) {
                if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began) {
                    ExitImmersiveMode();
                    if (_reenterImmersiveMode != null) {
                        AirConsole.instance.StopCoroutine(_reenterImmersiveMode);
                    }

                    _reenterImmersiveMode = AirConsole.instance.StartCoroutine(ReenterImmersiveMode());
                }

                yield return null;
            }
        }

        IEnumerator ReenterImmersiveMode() {
            yield return new WaitForSeconds(8);
            EnterImmersiveMode();
        }

        void EnterImmersiveMode() {
            AirConsoleLogger.LogDevelopment("EnterImmersiveMode");
            Screen.fullScreen = true;
        }

        void ExitImmersiveMode() {
            AirConsoleLogger.LogDevelopment("ExitImmersiveMode");
            Screen.fullScreen = false;
        }

        public AndroidImmersiveService() {
#if UNITY_ANDROID
#if !UNITY_EDITOR
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create an instance of your Java plugin class
            androidImmersiveService = new("com.airconsole.unityandroidlibrary.AndroidImmersiveService", context);
            androidImmersiveService.Call("maintainImmersiveModeOnSystemUIChange");
#endif
#else
            throw new NotSupportedException("AndroidImmersiveService is only supported on Android and Android in Unity");
#endif
        }
    }
}

#endif