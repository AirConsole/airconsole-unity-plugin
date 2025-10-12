#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Examples {
    using NDream.AirConsole;
    using UnityEngine;

    /// <summary>
    /// Example Audio Player that starts playing when the game is ready and stops when the game ends.
    /// It also listens to volume changes and adjusts the audio accordingly.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioPlayer : MonoBehaviour {
        private AudioSource _audioSource;

        private void Awake() {
            _audioSource = GetComponent<AudioSource>();
            if (!_audioSource) {
                AirConsoleLogger.LogError(() => "AudioPlayer requires an AudioSource component.", this);
                enabled = false;
                return;
            }

            SetupAudioSource();
            AirConsole.instance.onReady += HandleOnReady;
            AirConsole.instance.onGameEnd += HandleOnGameEnd;

            // Until OnReady is called, we don't want any audio from Unity playing as the Player Lobby overlay will be shown.
            AudioListener.pause = true;
        }

        private void SetupAudioSource() {
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.volume = 1.0f;
            if (!_audioSource.clip) {
                _audioSource.clip = Resources.Load<AudioClip>("Audio/Music/Happy_1");
            }
        }

        private void HandleOnGameEnd() {
            // After OnGameEnd is called, we must not play any audio until OnReady is called again. During this time the Player Lobby
            //  overlay will be shown.
            AudioListener.pause = true;
        }

        private void HandleOnReady(string code) {
            AirConsoleLogger.Log(() => $"OnReady for {code}");
            AudioListener.pause = false;
            _audioSource.Play();
        }

        private void HandleAudioVolumeChange(float volume) {
            AirConsoleLogger.Log(() => $"Setting volume to {volume}");
            if (volume > 0) {
                AudioListener.pause = false;
                AudioListener.volume = volume;
                _audioSource.Play();
            } else if (Mathf.Approximately(0, volume)) {
                AudioListener.pause = true;
            }
        }
    }
}
#endif
