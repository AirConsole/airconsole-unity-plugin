# Safe Area Feature Guide

## Overview

The SafeArea feature is a critical component for AirConsole games, particularly those running on automotive platforms. It allows your game to dynamically adjust to different screen sizes and configurations by providing a safe area rectangle where all important game elements should be placed. This ensures optimal visibility across various devices and platforms.

## How It Works

When you enable the `Native Game Sizing` option on the AirConsole component, your game will receive notifications whenever the available safe area on the screen changes. This happens via the `OnSafeAreaChanged` event which provides a `Rect` parameter representing the new safe area in camera pixel coordinates.

## When to Use Safe Area

You should implement safe area handling in your game when:

- Your game runs on automotive platforms (required)
- You want to ensure UI elements are visible on all screen sizes and configurations
- Your game has multiple cameras or split screen functionality
- You're using custom screen layouts that need to adapt to device constraints

## Implementation Basics

### Prerequisites

- Enable the `Native Game Sizing` checkbox on the AirConsole component in your scene

### Basic Implementation

Here's how to implement safe area handling:

```csharp
using UnityEngine;
using NDream.AirConsole;

public class SafeAreaHandler : MonoBehaviour
{
    [SerializeField] 
    private Camera mainCamera;
    
    void Start()
    {
        // Register for safe area changes
        if (AirConsole.instance && AirConsole.instance.IsAirConsoleUnityPluginReady()) {
            // Apply the current safe area if available
            HandleSafeAreaChanged(AirConsole.instance.SafeArea);
            
            // Subscribe to future changes
            AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;
        }
    }
    
    private void HandleSafeAreaChanged(Rect newSafeArea)
    {
        // Adjust your camera's viewport or UI elements based on the safe area
        if (mainCamera != null) {
            mainCamera.pixelRect = newSafeArea;
        }
    }
    
    void OnDestroy()
    {
        // Always unsubscribe when done
        if (AirConsole.instance) {
            AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
        }
    }
}
```

## Understanding the SafeArea Property

The `AirConsole.instance.SafeArea` property returns a `Rect` structure with the following properties:

- `x`: The x-coordinate of the lower-left corner of the safe area
- `y`: The y-coordinate of the lower-left corner of the safe area
- `width`: The width of the safe area
- `height`: The height of the safe area

All values are provided in pixel coordinates, making it easy to apply to camera `pixelRect` properties.

## Example Use Cases

### Fullscreen Camera Adjustment

Adjust your main camera to match the safe area:

```csharp
mainCamera.pixelRect = AirConsole.instance.SafeArea;
```

### Canvas and UI Adjustment

For UI elements, you can create a Canvas with a RectTransform that matches the safe area:

```csharp
RectTransform rectTransform = GetComponent<RectTransform>();
Rect safeArea = AirConsole.instance.SafeArea;
Vector2 anchorMin = safeArea.position;
Vector2 anchorMax = safeArea.position + safeArea.size;

anchorMin.x /= Screen.width;
anchorMin.y /= Screen.height;
anchorMax.x /= Screen.width;
anchorMax.y /= Screen.height;
rectTransform.anchorMin = anchorMin;
rectTransform.anchorMax = anchorMax;
```

### Split Screen Configuration

For split screen games, you'll need to divide the safe area among multiple cameras:

```csharp
Rect safeArea = AirConsole.instance.SafeArea;
// For a horizontal 2-player split screen:
player1Camera.pixelRect = new Rect(safeArea.x, safeArea.y, safeArea.width / 2, safeArea.height);
player2Camera.pixelRect = new Rect(safeArea.x + safeArea.width / 2, safeArea.y, safeArea.width / 2, safeArea.height);
```

## Testing Safe Areas

During development, you can test different safe area configurations using the SafeArea Tester tool found in the Unity Editor under Window > AirConsole > SafeArea Tester.

## Important Notes

1. Always unsubscribe from the `OnSafeAreaChanged` event when your components are destroyed to prevent memory leaks
2. The safe area may change during gameplay, so your implementation should handle dynamic updates
3. When using `Native Game Sizing`, the `AndroidUIResizeMode.ResizeCameraAndReferenceResolution` mode is no longer supported

## Relation to Native Game Sizing Checkbox

The `Native Game Sizing` checkbox in the AirConsole component inspector enables the safe area feature. When enabled:

1. Your game receives safe area information through the `OnSafeAreaChanged` event
2. The game is responsible for handling this information appropriately
3. The old automatic camera resizing for Android (`AndroidUIResizeMode`) is disabled in favor of this more flexible approach

Enabling this option is required for automotive platforms and recommended for all new AirConsole projects.

## Further Examples

For more detailed examples, check out the example scenes included in the plugin under `Assets/AirConsole/examples/safe-area/`:

- Basic Example: Shows how to use safe area with a fullscreen camera
- UI Example: Demonstrates safe area handling with UI elements and canvas scaling
- Split Screen Example: Showcases complex safe area division for multiplayer games