using UnityEngine;
using NDream.AirConsole;

/// <summary>
/// Example script for handling SafeArea changes with a fullscreen camera.
/// This is the simplest implementation that adjusts the main camera to fit within the safe area.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FullscreenSafeAreaHandler : MonoBehaviour
{
    [Tooltip("Reference to the camera that should be adjusted to the safe area. Will use the attached camera if not assigned.")]
    [SerializeField] private Camera targetCamera;

    // Visual indicator for the safe area (optional, for debug purposes)
    [Tooltip("Optional visual representation of the safe area bounds")]
    [SerializeField] private bool showSafeAreaBounds = true;
    private GameObject safeAreaVisualizer;

    private void Awake()
    {
        // Get the camera from this GameObject if not specifically assigned
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        // Create visual indicator for safe area if enabled
        if (showSafeAreaBounds)
        {
            CreateSafeAreaVisualizer();
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

            Debug.Log("FullscreenSafeAreaHandler: Subscribed to OnSafeAreaChanged events");
        }
        else
        {
            Debug.LogWarning("AirConsole is not ready. Safe area handling won't work correctly.");
        }
    }

    /// <summary>
    /// Handles changes to the safe area by adjusting the camera's pixel rect.
    /// </summary>
    /// <param name="newSafeArea">The new safe area rectangle in pixel coordinates</param>
    private void HandleSafeAreaChanged(Rect newSafeArea)
    {
        if (targetCamera != null)
        {
            // Set the camera's pixel rect to match the safe area
            targetCamera.pixelRect = newSafeArea;
            
            Debug.Log($"Camera adjusted to safe area: {newSafeArea}");

            // Update the visual indicator if enabled
            if (showSafeAreaBounds && safeAreaVisualizer != null)
            {
                UpdateSafeAreaVisualizer(newSafeArea);
            }
        }
    }

    /// <summary>
    /// Creates a simple visual representation of the safe area bounds
    /// </summary>
    private void CreateSafeAreaVisualizer()
    {
        safeAreaVisualizer = new GameObject("SafeAreaBounds");
        safeAreaVisualizer.transform.SetParent(transform);
        
        // Create line renderer for visualizing the safe area
        LineRenderer lineRenderer = safeAreaVisualizer.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 5; // 5 points to create a rectangle (last point connects to first)
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.useWorldSpace = false;
        
        // Set a visible material
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
    }

    /// <summary>
    /// Updates the visual indicator to match the current safe area
    /// </summary>
    /// <param name="safeArea">The safe area rectangle</param>
    private void UpdateSafeAreaVisualizer(Rect safeArea)
    {
        LineRenderer lineRenderer = safeAreaVisualizer.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            // Convert safe area from screen space to world space
            float z = 10f; // Distance from camera
            Vector3 bottomLeft = targetCamera.ScreenToWorldPoint(new Vector3(safeArea.xMin, safeArea.yMin, z));
            Vector3 bottomRight = targetCamera.ScreenToWorldPoint(new Vector3(safeArea.xMax, safeArea.yMin, z));
            Vector3 topRight = targetCamera.ScreenToWorldPoint(new Vector3(safeArea.xMax, safeArea.yMax, z));
            Vector3 topLeft = targetCamera.ScreenToWorldPoint(new Vector3(safeArea.xMin, safeArea.yMax, z));

            lineRenderer.SetPosition(0, bottomLeft);
            lineRenderer.SetPosition(1, bottomRight);
            lineRenderer.SetPosition(2, topRight);
            lineRenderer.SetPosition(3, topLeft);
            lineRenderer.SetPosition(4, bottomLeft); // Close the rectangle
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        if (AirConsole.instance)
        {
            AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
            Debug.Log("FullscreenSafeAreaHandler: Unsubscribed from OnSafeAreaChanged events");
        }
    }
}