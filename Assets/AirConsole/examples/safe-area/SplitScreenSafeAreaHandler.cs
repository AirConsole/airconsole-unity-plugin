using System.Collections.Generic;
using UnityEngine;
using NDream.AirConsole;

/// <summary>
/// Example script for handling SafeArea changes with split screen camera setup.
/// Supports multiple screen configurations including:
/// - 2 players (horizontal or vertical)
/// - 3 players (3 cameras + optional UI/birds-eye view)
/// - 4 players (2x2 grid)
/// </summary>
public class SplitScreenSafeAreaHandler : MonoBehaviour
{
    // Split screen configuration
    public enum SplitMode
    {
        TwoPlayersHorizontal,
        TwoPlayersVertical,
        ThreePlayers,
        FourPlayers
    }

    [Tooltip("The current split screen configuration")]
    [SerializeField] private SplitMode splitMode = SplitMode.TwoPlayersHorizontal;

    [Tooltip("Array of player cameras to be arranged in split-screen")]
    [SerializeField] private Camera[] playerCameras;

    [Tooltip("Optional UI/overview camera (used in 3-player mode)")]
    [SerializeField] private Camera overviewCamera;

    [Tooltip("Optional border between split screens (in pixels)")]
    [SerializeField] private float borderWidth = 2f;

    // Visual indicator for the safe area
    [Tooltip("Show debug visualizers for the safe areas")]
    [SerializeField] private bool showSafeAreaDebug = true;
    private List<GameObject> safeAreaVisualizers = new List<GameObject>();

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

            Debug.Log("SplitScreenSafeAreaHandler: Subscribed to OnSafeAreaChanged events");
        }
        else
        {
            Debug.LogWarning("AirConsole is not ready. Safe area handling won't work correctly.");
        }
    }

    /// <summary>
    /// Handles changes to the safe area by adjusting all cameras according to the split mode
    /// </summary>
    /// <param name="newSafeArea">The new safe area rectangle in pixel coordinates</param>
    private void HandleSafeAreaChanged(Rect newSafeArea)
    {
        // Clear old visualizers
        ClearSafeAreaVisualizers();

        switch (splitMode)
        {
            case SplitMode.TwoPlayersHorizontal:
                SetupTwoPlayersHorizontal(newSafeArea);
                break;
            case SplitMode.TwoPlayersVertical:
                SetupTwoPlayersVertical(newSafeArea);
                break;
            case SplitMode.ThreePlayers:
                SetupThreePlayers(newSafeArea);
                break;
            case SplitMode.FourPlayers:
                SetupFourPlayers(newSafeArea);
                break;
        }

        Debug.Log($"Split screen cameras adjusted to safe area: {newSafeArea}");
    }

    #region Split Screen Configuration Methods

    /// <summary>
    /// Sets up two player cameras in a horizontal split (side by side)
    /// </summary>
    private void SetupTwoPlayersHorizontal(Rect safeArea)
    {
        if (playerCameras.Length < 2)
        {
            Debug.LogError("Not enough cameras for two player mode. Need at least 2 cameras.");
            return;
        }

        // Calculate dimensions with border
        float halfWidth = (safeArea.width - borderWidth) / 2;
        
        // Left player
        Rect leftRect = new Rect(
            safeArea.x,
            safeArea.y,
            halfWidth,
            safeArea.height
        );
        playerCameras[0].pixelRect = leftRect;
        
        // Right player
        Rect rightRect = new Rect(
            safeArea.x + halfWidth + borderWidth,
            safeArea.y,
            halfWidth,
            safeArea.height
        );
        playerCameras[1].pixelRect = rightRect;

        // Create debug visualizers if enabled
        if (showSafeAreaDebug)
        {
            CreateSafeAreaVisualizer(playerCameras[0], leftRect, Color.red);
            CreateSafeAreaVisualizer(playerCameras[1], rightRect, Color.blue);
        }
    }

    /// <summary>
    /// Sets up two player cameras in a vertical split (top and bottom)
    /// </summary>
    private void SetupTwoPlayersVertical(Rect safeArea)
    {
        if (playerCameras.Length < 2)
        {
            Debug.LogError("Not enough cameras for two player mode. Need at least 2 cameras.");
            return;
        }

        // Calculate dimensions with border
        float halfHeight = (safeArea.height - borderWidth) / 2;
        
        // Top player
        Rect topRect = new Rect(
            safeArea.x,
            safeArea.y + halfHeight + borderWidth,
            safeArea.width,
            halfHeight
        );
        playerCameras[0].pixelRect = topRect;
        
        // Bottom player
        Rect bottomRect = new Rect(
            safeArea.x,
            safeArea.y,
            safeArea.width,
            halfHeight
        );
        playerCameras[1].pixelRect = bottomRect;

        // Create debug visualizers if enabled
        if (showSafeAreaDebug)
        {
            CreateSafeAreaVisualizer(playerCameras[0], topRect, Color.red);
            CreateSafeAreaVisualizer(playerCameras[1], bottomRect, Color.blue);
        }
    }

    /// <summary>
    /// Sets up three player cameras plus an optional overview/UI camera
    /// Layout is top left, top right, and bottom left, with bottom right being the overview
    /// </summary>
    private void SetupThreePlayers(Rect safeArea)
    {
        if (playerCameras.Length < 3)
        {
            Debug.LogError("Not enough cameras for three player mode. Need at least 3 cameras.");
            return;
        }

        // Calculate dimensions with borders
        float halfWidth = (safeArea.width - borderWidth) / 2;
        float halfHeight = (safeArea.height - borderWidth) / 2;
        
        // Player 1 (top left)
        Rect topLeftRect = new Rect(
            safeArea.x,
            safeArea.y + halfHeight + borderWidth,
            halfWidth,
            halfHeight
        );
        playerCameras[0].pixelRect = topLeftRect;
        
        // Player 2 (top right)
        Rect topRightRect = new Rect(
            safeArea.x + halfWidth + borderWidth,
            safeArea.y + halfHeight + borderWidth,
            halfWidth,
            halfHeight
        );
        playerCameras[1].pixelRect = topRightRect;
        
        // Player 3 (bottom left)
        Rect bottomLeftRect = new Rect(
            safeArea.x,
            safeArea.y,
            halfWidth,
            halfHeight
        );
        playerCameras[2].pixelRect = bottomLeftRect;
        
        // Overview camera (bottom right)
        if (overviewCamera != null)
        {
            Rect bottomRightRect = new Rect(
                safeArea.x + halfWidth + borderWidth,
                safeArea.y,
                halfWidth,
                halfHeight
            );
            overviewCamera.pixelRect = bottomRightRect;
            
            // Create debug visualizer for overview camera
            if (showSafeAreaDebug)
            {
                CreateSafeAreaVisualizer(overviewCamera, bottomRightRect, Color.yellow);
            }
        }

        // Create debug visualizers for player cameras if enabled
        if (showSafeAreaDebug)
        {
            CreateSafeAreaVisualizer(playerCameras[0], topLeftRect, Color.red);
            CreateSafeAreaVisualizer(playerCameras[1], topRightRect, Color.blue);
            CreateSafeAreaVisualizer(playerCameras[2], bottomLeftRect, Color.green);
        }
    }

    /// <summary>
    /// Sets up four player cameras in a 2x2 grid
    /// </summary>
    private void SetupFourPlayers(Rect safeArea)
    {
        if (playerCameras.Length < 4)
        {
            Debug.LogError("Not enough cameras for four player mode. Need at least 4 cameras.");
            return;
        }

        // Calculate dimensions with borders
        float halfWidth = (safeArea.width - borderWidth) / 2;
        float halfHeight = (safeArea.height - borderWidth) / 2;
        
        // Player 1 (top left)
        Rect topLeftRect = new Rect(
            safeArea.x,
            safeArea.y + halfHeight + borderWidth,
            halfWidth,
            halfHeight
        );
        playerCameras[0].pixelRect = topLeftRect;
        
        // Player 2 (top right)
        Rect topRightRect = new Rect(
            safeArea.x + halfWidth + borderWidth,
            safeArea.y + halfHeight + borderWidth,
            halfWidth,
            halfHeight
        );
        playerCameras[1].pixelRect = topRightRect;
        
        // Player 3 (bottom left)
        Rect bottomLeftRect = new Rect(
            safeArea.x,
            safeArea.y,
            halfWidth,
            halfHeight
        );
        playerCameras[2].pixelRect = bottomLeftRect;
        
        // Player 4 (bottom right)
        Rect bottomRightRect = new Rect(
            safeArea.x + halfWidth + borderWidth,
            safeArea.y,
            halfWidth,
            halfHeight
        );
        playerCameras[3].pixelRect = bottomRightRect;

        // Create debug visualizers if enabled
        if (showSafeAreaDebug)
        {
            CreateSafeAreaVisualizer(playerCameras[0], topLeftRect, Color.red);
            CreateSafeAreaVisualizer(playerCameras[1], topRightRect, Color.blue);
            CreateSafeAreaVisualizer(playerCameras[2], bottomLeftRect, Color.green);
            CreateSafeAreaVisualizer(playerCameras[3], bottomRightRect, Color.yellow);
        }
    }

    #endregion

    #region Debug Visualization Methods

    /// <summary>
    /// Creates a visual representation of a camera's safe area
    /// </summary>
    private void CreateSafeAreaVisualizer(Camera camera, Rect safeArea, Color color)
    {
        GameObject visualizer = new GameObject($"SafeAreaVisualizer_{camera.name}");
        safeAreaVisualizers.Add(visualizer);
        
        // Create line renderer for visualizing the safe area
        LineRenderer lineRenderer = visualizer.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 5; // 5 points to create a rectangle (last point connects to first)
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.useWorldSpace = false;
        
        // Set a visible material
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        
        // Convert safe area from screen space to world space
        float z = 10f; // Distance from camera
        Vector3 bottomLeft = camera.ScreenToWorldPoint(new Vector3(safeArea.xMin, safeArea.yMin, z));
        Vector3 bottomRight = camera.ScreenToWorldPoint(new Vector3(safeArea.xMax, safeArea.yMin, z));
        Vector3 topRight = camera.ScreenToWorldPoint(new Vector3(safeArea.xMax, safeArea.yMax, z));
        Vector3 topLeft = camera.ScreenToWorldPoint(new Vector3(safeArea.xMin, safeArea.yMax, z));

        // Set the positions in the line renderer
        lineRenderer.SetPosition(0, bottomLeft);
        lineRenderer.SetPosition(1, bottomRight);
        lineRenderer.SetPosition(2, topRight);
        lineRenderer.SetPosition(3, topLeft);
        lineRenderer.SetPosition(4, bottomLeft); // Close the rectangle
    }

    /// <summary>
    /// Clears all debug visualizers
    /// </summary>
    private void ClearSafeAreaVisualizers()
    {
        foreach (GameObject visualizer in safeAreaVisualizers)
        {
            if (visualizer != null)
            {
                Destroy(visualizer);
            }
        }
        safeAreaVisualizers.Clear();
    }

    #endregion

    /// <summary>
    /// Public method to change the split mode at runtime
    /// </summary>
    public void ChangeSplitMode(SplitMode newMode)
    {
        splitMode = newMode;
        
        // Apply the change immediately if we have a valid safe area
        if (AirConsole.instance && AirConsole.instance.IsAirConsoleUnityPluginReady())
        {
            Rect safeArea = AirConsole.instance.SafeArea;
            if (safeArea.width > 0)
            {
                HandleSafeAreaChanged(safeArea);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up visualizers
        ClearSafeAreaVisualizers();
        
        // Unsubscribe from the safe area event
        if (AirConsole.instance)
        {
            AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
        }
    }
}