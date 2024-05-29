<!-- markdownlint-disable MD024 -->

# Releases

Release notes follow the [keep a changelog](https://keepachangelog.com/en/1.1.0/) format.

## [2.5.0] - 2024-05-29

With version 2.5.0, AirConsole Unity Plugin adds a consistent system to handle situations where players can not join in the middle of
related capabilities. This is supported by the controller, informing new joining players that they can not join at the moment but can do so
after the current gameplay round has finished.
Gameplay rounds are controlled through AirConsole's setActivePlayers API.


### Added

- Added :gift_heart:: Developers can now set the language to test with when running games in the editor. (Kudos to @bbeinder contributing #71)
- Added :gift_heart:: Partner specific highscore
  - New rank `partner` for `RequestHighScores` that will limit the response to highscores the player has achieve on the same partner. See the [partner specific high score section of the high score guide](https://developers.airconsole.com/#!/guides/highscore#partner)
- Added :gift_heart:: Multi-screen multiplayer API [see Multi-screen multiplayer guide](https://developers.airconsole.com/#!/guides/multiplayer)
  - provides information to enable online multiplayer matchmaking against screens in the same car as well screens in the same type of partner environment (e.g. car brand).
- Added :gift_heart:: New capability: Player Silencing [see Handling Players connecting guide](https://developers.airconsole.com/#!/guides/player_silencing)
  - Support for Player Silencing in the AirConsole component. For more information visit the [AirConsole Player Silencing Guide](https://developers.airconsole.com/#!/guides/player_silencing).
- Added :gift_heart:: Support for EMSDK_PYTHON when building for WebGL in Unity 2019 which requires python2 that needs to be manually installed on OSX Ventura / Sonoma. If your python2 is not in `/usr/local/bin/python2` you can update the path in the AirConsole Settings window.
- Addition of version migration documentation for version migrations from 2.10 up to 2.5.0 in [Assets/AirConsole/Upgrade_Plugin_Version.md](./Assets/AirConsole/Upgrade_Plugin_Version.md).

### Changed

- StorePersistentData's uid parameter is no longer optional for screens.
- RequestPersistentData's uids parameter is no longer optional for screens.
- Updated supported platforms list.
- Obsolete API devices, device_id and server_time_offset will now create errors with instruction on their replacement. They will be removed in version 2.6.0.


### Fixed

- OnPause and OnResume are now called on the MainThread on all platform and the editor (Kudos to @bbeinder contributing #73)
- The devGameId is now correctly applied when using Unity PlayMode, removing the nagging language confirmation popups in the browser (Kudos to @bbeinder contributing #71)
- Using `Open Exported Port` no longer creates InvalidOperationException (Kudos to @bbeinder contributing #72)

## [2.14] - 2022-11-02

### Added

- Support for custom local IPs: AirConsole now provides a field to define the IP provided to the backend, enabling the AirConsole Controller to connect to running Unity Editor instances directly.

## [2.13] - 2022-10-23

### Fixed

- Update Unity Webview to v1.0.1 to address WebGL builds

## [2.12] - 2023-10-10

### Added

- AirConsole.Version: AirConsole now provides a static function to use for Remote Addressable of Android Builds.
- Updated Unity Webview: The updated webview supports all 64Bit Intel and ARM Macs now, including and in particular Unity 2021 LTS and newer.

### Changed

- New Dev Url for Play Mode Simulator launches: This addresses issues with http access to your running Unity instance arising from  recent changes in Chrome Browser Security.
- AirConsole Unity Webview is integrated as a package dependency.
- Removed unnecessary Package Manager dependencies: Timeline, Unity Ads, Unity Analytics, Unity Collab
- Improve .gitignore and remove all Unity generated files.

### Deprecated

- Support for i386 OSX bundle
- The webview no longer supports rendering on OSX.
