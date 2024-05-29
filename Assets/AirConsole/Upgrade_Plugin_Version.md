
# Upgrade the Unity Plugin to the current version

## Upgrading from v2.14- to v2.5.0

1. The content of airconsole-unity-plugin.js has changed.
   You need to copy the correct version from `WebGLTemplates/AirConsole` for Unity 2019 and below or `WebGLTemplates/AirConsole-2020` for Unity 2020+ to the WebGLTemplate folder you use.

2. The content of the index.html / screen.html has changed.
   Open your controller's html file as well as the index.html in your WebGL template and search for `<script src="translation.js"></script>` and replace it with `<script src="airconsole-settings.js"></script>`, otherwise neither Translations nor Player Silencing will work.

3. The version of the airconsole api has changed.
   Search for a script usage like `<script src="https://www.airconsole.com/api/airconsole-1.8.0.js"></script>` and replace it with `<script src="https://www.airconsole.com/api/airconsole-1.9.0.js"></script>`

4. Ensure that obsolete API devices, device_id and server_time_offset are updated:
    1. Replace AirConsole.instance.devices with AirConsole.instance.Devices
    2. Replace AirConsole.instance.device_id with AirConsole.instance.GetDeviceId()
    3. Replace AirConsole.instance.server_time_offset with AirConsole.instance.GetServerTime()

## Upgrading from v2.11 to v2.12+

The location of the Webview has changed in this release.

When upgrading from v2.11 and before, you need manually remove the old Webview plugin parts:

- `Assets/AirConsole/plugins/WebViewObject.cs`
- `Assets/AirConsole/plugins/Editor/UnityWebViewPostprocessBuild.cs`
- `Assets/AirConsole/plugins/WebView.bundle`
- `Assets/AirConsole/plugins/WebViewSeparated.bundle`
- `Assets/AirConsole/plugins/Anrdoid/WebViewPlugin.jar`
- `Assets/AirConsole/plugins/iOS/WebView.mm`
- `Assets/AirConsole/plugins/X86_64/WebView.bundle`

## Upgrading from older versions to v2.11+

With v2.11, the `AirConsole.instance.OnMute` event was removed and the AirConsole platform stopped invoking it for older projects in July 2023.
When upgrading you need to update your event handlers for `AirConsole.instance.OnAdShow` and `AirConsole.instance.OnAdComplete` to take care of muting the game.