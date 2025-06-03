# Safe Area Examples

This directory contains example implementations for the AirConsole SafeArea feature. The SafeArea feature allows your game to adapt to different screen configurations by providing a safe rectangle where content should be displayed.

## Overview

The examples demonstrate how to handle the `OnSafeAreaChanged` event and use the `SafeArea` property from the AirConsole instance to properly adjust cameras and UI elements for optimal display across different platforms.

## Prerequisites

To use these examples:

1. Enable the `Native Game Sizing` checkbox in the AirConsole component inspector
2. Make sure your game is using AirConsole version 2.6.0 or newer
3. For proper testing in the editor, use the SafeArea Tester tool (Window > AirConsole > SafeArea Tester)

## Example Scenes

### 1. Fullscreen Example

The `FullscreenSafeAreaHandler.cs` script demonstrates how to adapt a single camera to fit within the safe area. This is the simplest implementation and works well for most single-player games.

Key features:

- Automatic adjustment of the main camera's pixel rect
- Optional visual debug representation of the safe area bounds
- Proper event subscription and cleanup

### 2. UI Example

The `UISafeAreaHandler.cs` script shows how to handle safe area changes with UI cameras and canvas scaling. This approach ensures UI elements stay within visible bounds and are properly scaled.

Key features:

- Adjusts both the camera and canvas scaler
- Maintains proper UI scaling regardless of safe area changes
- Provides options for different scaling approaches (width, height, or mixed priority)

### 3. Split Screen Example

The `SplitScreenSafeAreaHandler.cs` script demonstrates how to handle complex split-screen setups with multiple players. It supports several configurations:

Key features:

- Two players horizontal layout (side by side)
- Two players vertical layout (top and bottom)
- Three players layout (top left, top right, bottom left) with the option to use a 4th camera for an overview camera
- Four players layout (2x2 grid)
- Customizable border width between screens

## How to Use

1. Add one of the example scripts to a GameObject in your scene (usually the one with your camera)
2. Configure the script's properties in the inspector
3. Ensure the AirConsole object in your scene has `Native Game Sizing` enabled
4. Run your game and test with different safe area sizes using the SafeArea Tester

## Additional Information

For more comprehensive documentation on the SafeArea feature, refer to:

- [Safe Area Documentation](../../../docs/safe-area.md)
- [AirConsole Developer Documentation](https://developers.airconsole.com/)

## Important Notes

- Always unsubscribe from the `OnSafeAreaChanged` event when your component is destroyed
- The safe area can change at runtime, so your implementation should be prepared to handle changes dynamically
- When using `Native Game Sizing`, the legacy `AndroidUIResizeMode.ResizeCameraAndReferenceResolution` is no longer supported

