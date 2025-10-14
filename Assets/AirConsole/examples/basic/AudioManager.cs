#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Examples {
    using NDream.AirConsole;
    using UnityEngine;

    /// <summary>
    /// Example Audio Player that starts playing when the game is ready and stops when the game ends.
    /// It also listens to volume changes and adjusts the audio accordingly.
    /// </summary>
    public class AudioManager : MonoBehaviour {
        private void Awake() {
            // Initially pause all audio until OnReady is called.
            AudioListener.pause = true;
            AirConsole.instance.onReady += HandleOnReady;

            // Until OnReady is called, we don't want any audio from Unity playing as the Player Lobby overlay will be shown.
            AudioListener.pause = true;
            AirConsole.instance.OnMaximumVolumeChanged += HandleAudioVolumeChange;
        }

        private void OnDestroy() {
            if (AirConsole.instance) {
                AirConsole.instance.OnMaximumVolumeChanged -= HandleAudioVolumeChange;
            }
        }

        private void HandleOnReady(string code) {
            AirConsoleLogger.Log(() => $"OnReady for {code}");
            if (AirConsole.instance.MaximumAudioVolume > 0) {
                AudioListener.pause = false;
            } else {
                AudioListener.pause = true;
            }
        }

        private void HandleAudioVolumeChange(float volume) {
            AirConsoleLogger.Log(() => $"Setting volume to {volume}");
            if (volume > 0) {
                AudioListener.pause = false;
                AudioListener.volume = volume;
                AirConsoleLogger.Log(() => $"AudioListener.pause = false, new volume = {volume}");
            } else if (Mathf.Approximately(0, volume)) {
                AirConsoleLogger.Log(() => "AudioListener.pause = true, new volume = 0");
                AudioListener.pause = true;
            }
        }
    }
}
#endif
