using UnityEngine;
using UnityEngine.UI;
using NDream.AirConsole;

/// <summary>
/// Example script for handling SafeArea changes with UI cameras and canvas scaling.
/// This implementation adjusts both the camera and the canvas to ensure UI elements
/// stay within the safe area.
/// </summary>
[RequireComponent(typeof(Camera))]
public class UISafeAreaHandler : MonoBehaviour
{
    [Tooltip("Reference to the UI camera. Will use attached camera if not assigned.")]
    [SerializeField] private Camera uiCamera;
    
    [Tooltip("Reference to the canvas that should be adjusted. Will find in children if not assigned.")]
    [SerializeField] private Canvas targetCanvas;
    
    [Tooltip("Reference to the canvas scaler. Will find in children if not assigned.")]
    [SerializeField] private CanvasScaler canvasScaler;
    
    // Store the original reference resolution to restore it if needed
    private Vector2 originalReferenceResolution;
    
    private void Awake()
    {
        // Get references if not assigned
        if (uiCamera == null)
        {
            uiCamera = GetComponent<Camera>();
        }
        
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInChildren<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("UISafeAreaHandler: No Canvas found. Please assign one in the inspector.");
            }
        }
        
        if (canvasScaler == null && targetCanvas != null)
        {
            canvasScaler = targetCanvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                Debug.LogWarning("UISafeAreaHandler: No CanvasScaler found on the Canvas.");
            }
        }
        
        // Store the original reference resolution
        if (canvasScaler != null)
        {
            originalReferenceResolution = canvasScaler.referenceResolution;
        }
        
        // Ensure the canvas is set to use the UI camera
        if (targetCanvas != null && uiCamera != null)
        {
            targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            targetCanvas.worldCamera = uiCamera;
        }
    }
    
    private void Start()
    {
        // Subscribe to safe area changes when AirConsole is ready
        if (AirConsole.instance && AirConsole.instance.IsAirConsoleUnityPluginReady())
        {
            // Apply the current safe area if available
            if (AirConsole.instance.SafeArea != null && AirConsole.instance.SafeArea.width > 0)
            {
                HandleSafeAreaChanged(AirConsole.instance.SafeArea);
            }

            // Subscribe to future changes
            AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;
            
            Debug.Log("UISafeAreaHandler: Subscribed to OnSafeAreaChanged events");
        }
        else
        {
            Debug.LogWarning("AirConsole is not ready. Safe area handling won't work correctly.");
        }
    }

    /// <summary>
    /// Handles changes to the safe area by adjusting both the camera and canvas
    /// </summary>
    /// <param name="newSafeArea">The new safe area rectangle in pixel coordinates</param>
    private void HandleSafeAreaChanged(Rect newSafeArea)
    {
        if (uiCamera != null)
        {
            // Adjust the camera to the safe area
            uiCamera.pixelRect = newSafeArea;
            Debug.Log($"UI Camera adjusted to safe area: {newSafeArea}");
        }
        
        if (canvasScaler != null)
        {
            // Adjust the canvas scaler to maintain proper UI scaling
            AdjustCanvasScaler(newSafeArea);
        }
        
        // If we're using a RectTransform for the canvas, adjust its anchors to match the safe area
        if (targetCanvas != null)
        {
            RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
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
    private void AdjustCanvasScaler(Rect safeArea)
    {
        // Only adjust if we have a valid canvas scaler and safe area
        if (canvasScaler == null || safeArea.width <= 0 || safeArea.height <= 0)
            return;

        // Calculate the new aspect ratio based on the safe area
        float safeAreaAspect = safeArea.width / safeArea.height;
        
        // Update the reference resolution to match the new aspect ratio while maintaining the original height
        Vector2 newReferenceResolution = originalReferenceResolution;
        
        if (canvasScaler.matchWidthOrHeight == 1) // Height-based scaling
        {
            // Keep height fixed, adjust width to match new aspect ratio
            newReferenceResolution.x = originalReferenceResolution.y * safeAreaAspect;
        }
        else if (canvasScaler.matchWidthOrHeight == 0) // Width-based scaling
        {
            // Keep width fixed, adjust height to match new aspect ratio
            newReferenceResolution.y = originalReferenceResolution.x / safeAreaAspect;
        }
        else
        {
            // For mixed scaling, we'll use a balanced approach
            float matchValue = canvasScaler.matchWidthOrHeight;
            float widthInfluence = 1 - matchValue;
            float heightInfluence = matchValue;
            
            // Interpolate between width-based and height-based adjustments
            newReferenceResolution.x = (originalReferenceResolution.y * safeAreaAspect) * widthInfluence + 
                                      originalReferenceResolution.x * heightInfluence;
            newReferenceResolution.y = (originalReferenceResolution.x / safeAreaAspect) * heightInfluence + 
                                      originalReferenceResolution.y * widthInfluence;
        }
        
        // Apply the new reference resolution
        canvasScaler.referenceResolution = newReferenceResolution;
        
        Debug.Log($"Canvas scaler adjusted: Original resolution {originalReferenceResolution}, " +
                  $"New resolution {newReferenceResolution}, Safe area aspect ratio: {safeAreaAspect}");
    }
    
    /// <summary>
    /// Reset the canvas scaler when the component is destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe from the safe area event
        if (AirConsole.instance)
        {
            AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
        }
        
        // Restore the original reference resolution
        if (canvasScaler != null)
        {
            canvasScaler.referenceResolution = originalReferenceResolution;
        }
    }
}