namespace NDream.AirConsole.Examples {
    using UnityEngine;
    using UnityEngine.UI;
    using NDream.AirConsole;

    /// <summary>
    /// Example script for handling SafeArea changes with UI cameras and canvas scaling.
    /// This implementation adjusts both the camera and the canvas to ensure UI elements
    /// stay within the safe area.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UISafeAreaHandler : MonoBehaviour {
        [Tooltip("Reference to the UI camera. Will use attached camera if not assigned.")]
        [SerializeField]
        private Camera uiCamera;

        [Tooltip("Reference to the canvas that should be adjusted. Will find in children if not assigned.")]
        [SerializeField]
        private Canvas targetCanvas;

        [Tooltip("Reference to the canvas scaler. Will find in children if not assigned.")]
        [SerializeField]
        private CanvasScaler canvasScaler;

        private Vector2 originalReferenceResolution;

#if !DISABLE_AIRCONSOLE
        private void Awake() {
            // Get references if not assigned
            if (!uiCamera) {
                uiCamera = GetComponent<Camera>();
            }

            if (!targetCanvas) {
                targetCanvas = GetComponentInChildren<Canvas>();
                if (!targetCanvas) {
                    Debug.LogError("UISafeAreaHandler: No Canvas found. Please assign one in the inspector.");
                }
            }

            if (!canvasScaler && targetCanvas) {
                canvasScaler = targetCanvas.GetComponent<CanvasScaler>();
                if (!canvasScaler) {
                    Debug.LogWarning("UISafeAreaHandler: No CanvasScaler found on the Canvas.");
                }
            }

            if (canvasScaler) {
                originalReferenceResolution = canvasScaler.referenceResolution;
            }

            if (targetCanvas && uiCamera) {
                targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                targetCanvas.worldCamera = uiCamera;
            }

            AirConsole.instance.onReady += Setup;
        }

        private void Setup(string code) {
            // Subscribe to safe area changes when AirConsole is ready
            if (AirConsole.instance && AirConsole.instance.IsAirConsoleUnityPluginReady()) {
                // Apply the current safe area if available
                if (AirConsole.instance.SafeArea.width > 0) {
                    HandleSafeAreaChanged(AirConsole.instance.SafeArea);
                }

                // Subscribe to future changes
                AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;

                Debug.Log("UISafeAreaHandler: Subscribed to OnSafeAreaChanged events");
            } else {
                Debug.LogWarning("AirConsole is not ready. Safe area handling won't work correctly.");
            }
        }

        /// <summary>
        /// Handles changes to the safe area by adjusting both the camera and canvas
        /// </summary>
        /// <param name="newSafeArea">The new safe area rectangle in pixel coordinates</param>
        private void HandleSafeAreaChanged(Rect newSafeArea) {
            if (uiCamera) {
                uiCamera.pixelRect = newSafeArea;
                Debug.Log($"UI Camera adjusted to safe area: {newSafeArea}");
            }

            if (canvasScaler) {
                // Adjust the canvas scaler to maintain proper UI scaling
                AdjustCanvasScaler(newSafeArea);
            }

            // If we're using a RectTransform for the canvas, adjust its anchors to match the safe area
            if (targetCanvas) {
                RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
                if (canvasRect) {
                    // Ensure the canvas adapts to the safe area
                    canvasRect.anchoredPosition = Vector2.zero;
                    canvasRect.sizeDelta = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// Adjusts the canvas scaler to work correctly with the new safe area dimensions
        /// </summary>
        /// <param name="safeArea">The safe area rectangle</param>
        private void AdjustCanvasScaler(Rect safeArea) {
            if (!canvasScaler || safeArea.width <= 0 || safeArea.height <= 0) {
                return;
            }

            float safeAreaAspect = safeArea.width / safeArea.height;
            Vector2 newReferenceResolution = originalReferenceResolution;

            if (Mathf.Approximately(canvasScaler.matchWidthOrHeight, 1)) {
                newReferenceResolution.x = originalReferenceResolution.y * safeAreaAspect;
            } else if (canvasScaler.matchWidthOrHeight == 0) // Width-based scaling
            {
                newReferenceResolution.y = originalReferenceResolution.x / safeAreaAspect;
            } else {
                float matchValue = canvasScaler.matchWidthOrHeight;
                float widthInfluence = 1 - matchValue;
                float heightInfluence = matchValue;

                newReferenceResolution.x = originalReferenceResolution.y * safeAreaAspect * widthInfluence
                                           + originalReferenceResolution.x * heightInfluence;
                newReferenceResolution.y = originalReferenceResolution.x / safeAreaAspect * heightInfluence
                                           + originalReferenceResolution.y * widthInfluence;
            }

            canvasScaler.referenceResolution = newReferenceResolution;
            Debug.Log($"Canvas scaler adjusted: Original resolution {originalReferenceResolution}, "
                      + $"New resolution {newReferenceResolution}, Safe area aspect ratio: {safeAreaAspect}");
        }

        private void OnEnable() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;
            }
        }

        private void OnDisable() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
            }

            if (canvasScaler) {
                canvasScaler.referenceResolution = originalReferenceResolution;
            }
        }
#endif
    }
}