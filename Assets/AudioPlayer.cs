using NDream.AirConsole;
using UnityEngine;

public class AudioPlayer : MonoBehaviour {
    private AudioSource _audioSource;

    private void Awake() {
        _audioSource = GetComponent<AudioSource>();

        // AirConsole.instance.onPause += () => _audioSource.Pause();
        // AirConsole.instance.onResume += () => _audioSource.UnPause();
        AirConsole.instance.onReady += HandleOnReady;

        AirConsole.instance.OnMaximumVolumeChanged += HandleAudioVolume;
    }

    private void OnDestroy() {
        if (AirConsole.instance) {
            AirConsole.instance.OnMaximumVolumeChanged -= HandleAudioVolume;
        }
    }

    private void HandleOnReady(string code) {
        AirConsoleLogger.Log(() => $"OnReady for {code}");
        _audioSource.Play();
    }

    private void HandleAudioVolume(float volume) {
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
