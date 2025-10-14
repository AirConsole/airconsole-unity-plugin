#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Examples {
    using NDream.AirConsole;
    using UnityEngine;

    /// <summary>
    /// Example Audio Player that starts playing when the game is ready and stops when the game ends.
    /// It also listens to volume changes and adjusts the audio accordingly.
    /// </summary>
    public class AudioManager : MonoBehaviour {
        // We are using a Platform Script Defines here to avoid extra code to handle non-Android runtime platforms.
#if UNITY_ANDROID && !UNITY_EDITOR 
        private void Awake() {
            // Until OnReady is called, we don't want any audio from Unity playing as the webview Player Lobby overlay will be shown.
            AudioListener.pause = true;

            AirConsole.instance.OnGameAudioFocusChanged += HandleGameAudioFocusChange;
        }

        private void OnDestroy() {
            if (AirConsole.instance) {
                AirConsole.instance.OnGameAudioFocusChanged -= HandleGameAudioFocusChange;
            }
        }

        private void HandleGameAudioFocusChange(bool hasAudioFocus, float newMaximumVolume) {
            AirConsoleLogger.Log(() => $"HandleGameAudioFocusChange({hasAudioFocus},{newMaximumVolume}");
            AudioListener.volume = newMaximumVolume;
            AudioListener.pause = !hasAudioFocus;
        }
#endif
    }
}
#endif
