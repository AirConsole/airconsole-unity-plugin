<!-- markdownlint-disable MD024 -->

# Releases

Release notes follow the [keep a changelog](https://keepachangelog.com/en/1.1.0/) format.

## [2.5.0]

### Added

- Added :gift_heart:: Support for Player Silencing in the AirConsole component. For more information visit the [AirConsole Player Silencing Guide](https://developers.airconsole.com/#!/guides/player_silencing).
- Added :gift_heart:: Support for EMSDK_PYTHON when building for WebGL in Unity 2019 which requires python2 that needs to be manually installed on OSX Ventura / Sonoma! If your python2 is not in `/usr/local/bin/python2` you can update the path in the AirConsole Settings window.
- Addition of version migration documentation for version migrations from 2.10 up to 2.5.0.

### Changed

- StorePersistentData's uid parameter is no longer optional.
- RequestPersistentData's uids parameter is no longer optional.
- Updated supported platforms list.
- Obsolete API devices, device_id and server_time_offset have now create errors so that they can be removed in the next minor version.

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
