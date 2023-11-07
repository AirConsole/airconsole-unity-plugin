<!-- markdownlint-disable MD024 -->

# Releases

Release notes follow the [keep a changelog](https://keepachangelog.com/en/1.1.0/) format.

## [Unreleased]

### Improvement

- Addition of version migration documentation for version migrations from 2.10 and before.

## [2.14] - 2022-11-02

### New Features

- Support for custom local IPs: AirConsole now provides a field to define the IP provided to the backend, enabling the AirConsole Controller to connect to running Unity Editor instances directly.

## [2.13] - 2022-10-23

### Fixed

- Update Unity Webview to v1.0.1 to address WebGL builds


## [2.12] - 2023-10-10

### New Features

- AirConsole.Version: AirConsole now provides a static function to use for Remote Addressable of Android Builds.
- Updated Unity Webview: The updated webview supports all 64Bit Intel and ARM Macs now, including and in particular Unity 2021 LTS and newer.

### Improvement

- New Dev Url for Play Mode Simulator launches: This addresses issues with http access to your running Unity instance arising from  recent changes in Chrome Browser Security.
- AirConsole Unity Webview is integrated as a package dependency.
- Removed unnecessary Package Manager dependencies: Timeline, Unity Ads, Unity Analytics, Unity Collab
- Improve .gitignore and remove all Unity generated files.

### Fixed

-

### Deprecated

- Support for i386 OSX bundle
- The webview no longer supports rendering on OSX.
