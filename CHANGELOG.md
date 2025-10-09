<!-- markdownlint-disable MD024 -->

# Releases

Release notes follow the [keep a changelog](https://keepachangelog.com/en/1.1.0/) format.

## [Unreleased]

### Fixed

- Android: Platform overlay resizes correctly on Android TV
- Editor: No longer update index.html directly for API usage. The index.html should no longer be cleared out during Application Domain reloads.

### Changed

- Android Target SDK: Increased to 35 to meet Google Play requirements per Nov 1, 2025.
- Minimum Versions: The Unity minimum versions have been updated to match `CVE-2025-59489` fix versions.
- Android: After the last device disconnects, the webview is reset along the game state.

### Added

- **Webview Reset**: Added functionality to reset the webview, allowing users to clear its state and reload content as needed.

### Removed

- **Android**: The android library no longer manages AudioFocus or overriding the usage from USAGE_GAME.

## [2.6.1] - 2025-09-02

### Changed

- Increased minimum Unity versions to 2022.3.62f1 and 6000.0.43f1 to meet Google Play Store (16kb page file) and Automotive (WebRequest security) requirements.
- Web: Improve Memory Growth mode handling. It is now more consistent and provides clearer instructions to game developers.
- Android: Improved project configuration for correct fullscreen behavior on automotive.

## [2.6.0] - 2025-08-14

Version 2.6.0 is a major release, adding many features for game developers to create better performance experiences by default.
As part of this, we have removed support for Unity before 2022 LTS.

### Breaking Changes

- **Project Structure**: The plugin's internal folder structure has been significantly refactored. All runtime scripts are now located in `Assets/AirConsole/scripts/Runtime` and all editor scripts in `Assets/AirConsole/scripts/Editor`. The code has been organized into Assembly Definitions (`.asmdef` files), which may require updates to custom build scripts or direct script references.
- **Unity Version Support**: Unity versions older than Unity 2022 LTS are **no longer supported**.
- **Android Plugin Location**: The plugin no longer ships with a global `Assets/Plugins/Android` directory. The plugin updates the `AndroidManifest.xml` and Gradle files automatically during the build process.
- **Removed Obsolete APIs**: The obsoleted AirConsole properties `server_time_offset`, `device_id`, and `devices` have been removed. Please use `GetServerTime()`, `GetDeviceId()`, and `.Devices` instead.

### Removed

- Python SDK handling has been removed. This feature was only required for WebGL builds with Unity 2019 on MacOS 12+.

### Fixed

- When upgrading the plugin in Unity 2022 or Unity 6, the user should no longer be impacted by dependency differences on `com.unity.ugui` that previously caused package cache problems on Unity 2022 and Unity 6.

### Added

- `Translation Capabilities`: We now automatically unescape `&lt;` to `<` and `&gt;` to `>` as well as`\\n` to `\n` in translations to support TextMesh Pro RichText scenarios automatically.
- Validation that at least one of the required Unity Platform modules (WebGL or Android) is installed when projects are opened on other platforms without the DISABLE_AIRCONSOLE script predefine being set.
- Validation for platform project settings on WebGL and Android to ensure optimal performance and meeting requirements.
- Plugin upgrade capabilities: The plugin now attempts to auto update itself after installation. As part of that, the complete `Assets/AirConole/scripts` directory and `Assets/AirConsole/unity-webview` directories are replaced with new instances.
- Validation for the used AirConsole API version in controller and screen html to ensure that the required API version is configured, not outdated versions or `latest`.
- **Android:** Support for platform driven safe render areas: On platforms that the Safe Area, the new API provides games with a `Rect` based screen area in which the game is allowed to render. Areas outside of this are dedicated to platform specific information overlayed on top. Check `AirConsole.OnSafeAreaChanged` and the `NDream.AirConsole.OnSafeAreaChanged` delegate.
- **Safe Area Example**: Added a new example scene (`Assets/AirConsole/examples/safe-area`) to demonstrate the usage of the new Safe Area API on Android.

### Changed

- AirConsole now opens the socket server during playmode and closes it again at the end of the play session. This addresses cases where the Unity PlayMode would no longer work.
- **WebGL**: To simplify build automation including Unity Cloud Build, WebGL builds now copy `index.html` to `screen.html` instead of renaming it.

### Deprecated

- `GetActivePlayerDeviceIds` has been deprecated. Please use `ActivePlayerDeviceIds` instead.
- **WebGL**: The `rate-limiter.js` script is no longer included in the WebGL templates to simplify the build output.

## [2.5.7] - 2025-03-12

This release fixes an issue where AirConsole callbacks got delayed by at least one frame, incorrect Android Manifest outputs during builds
and improves the pong example.

### Fixed

- WebGL: Events sent from the browser to Unity are now processed as soon as possible.
- Fixed usage of all variants of Unity Activities in Android Manifest

### Changed

- Adjust the pong example to make use of the CustomDeviceState and bring it closer to the javascript pong example

## [2.5.6] - 2025-02-11

This release fixes an issue where the Unity loader is causing requests to non-existing files in Unity 6 web builds.

## [2.5.5] - 2025-01-07

This release patches a bug introduced in version 2.5.4

### Fixed

- We fixed the AirConsole settings to use the correct Unity WebGL template with Unity versions before Unity 6 and after Unity 2020.1.

## [2.5.4] - 2024-12-10

This release introduces the support for Unity 6 and extends the plugin with project configuration checks.

### Added

- Support for Unity 6 was added.
- Added project configuration checks to assist adjusting settings for AirConsole.
- Added minimal AndroidManifest.xml upgrade logic for Unity 6

## [2.5.3] - 2024-09-27

This release fixes a high impact bug for Android TV builds done with Unity Plugin 2.5.2.
If you use Unity 2019, you need to use 'Export Project' and build with Android Studio as Unity 2019s Gradle Version does support
`<queries>`.

The Unity Plugin versions of the 2.5.x series are the last versions to support Unity 2019 LTS, 2020 LTS.

Please upgrade your game to Unity 2022 LTS to benefit from the advanced WebGL memory configuration capabilities to improve automotive web
stability.

### Fixed

- Queries in AndroidManifest.xml are no longer dropped in merged manifests. Unity 2019 need to `Export Project` and build the project there
  as Unity did not backport Android SDK 30 support to Unity 2019.

## [2.5.2] - 2024-07-17

**IMPORTANT** The Unity Plugin versions of the 2.5.x series are the last versions to support Unity 2019 LTS and 2020 LTS.
Starting with version 2.6.0, the plugin will only support **Unity 2021 LTS and newer**.

### Added

- All example scenes are now setup to work on Unity Android with game id and game version set.

### Fixed

- We fixed an issue impacting Unity Android builds of 2.5.0 and 2.5.1 preventing the WebView from initializing correctly.
- We fixed an issue that prevented PreBuildProcessing from being included in packages

### Removed

- The custom gradle files `launcherTemplate.gradle` and `mainTemplate.gradle` have been removed as they are no longer required for Unity
  2019.4 LTS and newer.

## [2.5.1] - 2024-05-29

Adds game developer experience bugfixes as well as a pending fix that was lost in 2.5.0 for devGameId in Unity PlayMode.
For completeness as this is a 2.5.0 rerelease, the 2.5.0 release notes are repeated.

### Added

- Added :gift_heart:: Developers can now set the language to test with when running games in the editor. (Kudos to @bbeinder contributing
  #71)
- Added :gift_heart:: Partner specific highscore
  - New rank `partner` for `RequestHighScores` that will limit the response to highscores the player has achieve on the same partner. See
    the [partner specific high score section of the high score guide](https://developers.airconsole.com/#!/guides/highscore#partner)
- Added :gift_heart:: Multi-screen multiplayer
  API [see Multi-screen multiplayer guide](https://developers.airconsole.com/#!/guides/multiplayer)
  - provides information to enable online multiplayer matchmaking against screens in the same car as well screens in the same type of
    partner environment (e.g. car brand).
- Added :gift_heart:: New capability: Player
  Silencing [see Handling Players connecting guide](https://developers.airconsole.com/#!/guides/player_silencing)
  - Support for Player Silencing in the AirConsole component. For more information visit
    the [AirConsole Player Silencing Guide](https://developers.airconsole.com/#!/guides/player_silencing).
- Added :gift_heart:: Support for EMSDK_PYTHON when building for WebGL in Unity 2019 which requires python2 that needs to be manually
  installed on OSX Ventura / Sonoma. If your python2 is not in `/usr/local/bin/python2` you can update the path in the AirConsole Settings
  window.
- Addition of version migration documentation for version migrations from 2.10 up to 2.5.0
  in [Assets/AirConsole/Upgrade_Plugin_Version.md](./Assets/AirConsole/Upgrade_Plugin_Version.md).

### Changed

- StorePersistentData's uid parameter is no longer optional for screens.
- RequestPersistentData's uids parameter is no longer optional for screens.
- Updated supported platforms list.
- Obsolete API devices, device_id and server_time_offset will now create errors with instruction on their replacement. They will be removed
  in version 2.6.0.

### Fixed

- OnPause and OnResume are now called on the MainThread on all platform and the editor (Kudos to @bbeinder contributing #73)
- The devGameId is now correctly applied when using Unity PlayMode, removing the nagging language confirmation popups in the browser (Kudos
  to @bbeinder contributing #71)
- Using `Open Exported Port` no longer creates InvalidOperationException (Kudos to @bbeinder contributing #72)

## [2.5.0] - 2024-05-29

With version 2.5.0, AirConsole Unity Plugin adds a consistent system to handle situations where players can not join in the middle of
related capabilities. This is supported by the controller, informing new joining players that they can not join at the moment but can do so
after the current gameplay round has finished.
Gameplay rounds are controlled through AirConsole's setActivePlayers API.

### Added

- Added :gift_heart:: Partner specific highscore
  - New rank `partner` for `RequestHighScores` that will limit the response to highscores the player has achieve on the same partner. See
    the [partner specific high score section of the high score guide](https://developers.airconsole.com/#!/guides/highscore#partner)
- Added :gift_heart:: Multi-screen multiplayer
  API [see Multi-screen multiplayer guide](https://developers.airconsole.com/#!/guides/multiplayer)
  - provides information to enable online multiplayer matchmaking against screens in the same car as well screens in the same type of
    partner environment (e.g. car brand).
- Added :gift_heart:: New capability: Player
  Silencing [see Handling Players connecting guide](https://developers.airconsole.com/#!/guides/player_silencing)
  - Support for Player Silencing in the AirConsole component. For more information visit
    the [AirConsole Player Silencing Guide](https://developers.airconsole.com/#!/guides/player_silencing).
- Added :gift_heart:: Support for EMSDK_PYTHON when building for WebGL in Unity 2019 which requires python2 that needs to be manually
  installed on OSX Ventura / Sonoma. If your python2 is not in `/usr/local/bin/python2` you can update the path in the AirConsole Settings
  window.
- Addition of version migration documentation for version migrations from 2.10 up to 2.5.0
  in [Assets/AirConsole/Upgrade_Plugin_Version.md](./Assets/AirConsole/Upgrade_Plugin_Version.md).

### Changed

- StorePersistentData's uid parameter is no longer optional for screens.
- RequestPersistentData's uids parameter is no longer optional for screens.
- Updated supported platforms list.
- Obsolete API devices, device_id and server_time_offset will now create errors with instruction on their replacement. They will be removed
  in version 2.6.0.

## [2.14] - 2022-11-02

### Added

- Support for custom local IPs: AirConsole now provides a field to define the IP provided to the backend, enabling the AirConsole Controller
  to connect to running Unity Editor instances directly.

## [2.13] - 2022-10-23

### Fixed

- Update Unity Webview to v1.0.1 to address WebGL builds

## [2.12] - 2023-10-10

### Added

- AirConsole.Version: AirConsole now provides a static function to use for Remote Addressable of Android Builds.
- Updated Unity Webview: The updated webview supports all 64Bit Intel and ARM Macs now, including and in particular Unity 2021 LTS and
  newer.

### Changed

- New Dev Url for Play Mode Simulator launches: This addresses issues with http access to your running Unity instance arising from recent
  changes in Chrome Browser Security.
- AirConsole Unity Webview is integrated as a package dependency.
- Removed unnecessary Package Manager dependencies: Timeline, Unity Ads, Unity Analytics, Unity Collab
- Improve .gitignore and remove all Unity generated files.

### Deprecated

- Support for i386 OSX bundle
- The webview no longer supports rendering on OSX.
