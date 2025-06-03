namespace NDream.AirConsole.Examples {
    using UnityEngine;
    using NDream.AirConsole;

    /// <summary>
    /// Example script for handling SafeArea changes with a single fullscreen camera.
    /// This is the simplest implementation that adjusts the main camera to fit within the safe area.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FullscreenSafeAreaHandler : MonoBehaviour {
        [Tooltip("Reference to the camera that should be adjusted to the safe area. Will use the attached camera if not assigned.")]
        [SerializeField]
        private Camera targetCamera;

#if !DISABLE_AIRCONSOLE
        private void Awake() {
            if (!targetCamera) {
                targetCamera = GetComponent<Camera>();
            }

            HandleSafeAreaChanged(AirConsole.instance.SafeArea);
            AirConsole.instance.onReady += Setup;
        }

        private void Setup(string code) {
            if (AirConsole.instance && AirConsole.instance.IsAirConsoleUnityPluginReady()) {
                if (AirConsole.instance.SafeArea.width > 0) {
                    HandleSafeAreaChanged(AirConsole.instance.SafeArea);
                }

                AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;

                Debug.Log("FullscreenSafeAreaHandler: Subscribed to OnSafeAreaChanged events");
            } else {
                Debug.LogWarning("AirConsole is not ready. Safe area handling won't work correctly.");
            }
        }

        /// <summary>
        /// Handles changes to the safe area by adjusting the camera's pixel rect.
        /// </summary>
        /// <param name="newSafeArea">The new safe area rectangle in pixel coordinates</param>
        private void HandleSafeAreaChanged(Rect newSafeArea) {
            if (targetCamera) {
                targetCamera.pixelRect = newSafeArea;
                Debug.Log($"Camera adjusted to safe area: {newSafeArea}");
            }
        }

        private void OnEnable() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
                AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;
            }
        }

        private void OnDisable() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
            }
        }
#endif
    }
}