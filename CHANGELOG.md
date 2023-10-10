<!-- markdownlint-disable MD024 -->

# Releases

Release notes follow the [keep a changelog](https://keepachangelog.com/en/1.1.0/) format.

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
