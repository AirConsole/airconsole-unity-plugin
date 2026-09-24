# Info

The AirConsole-Unity-Plugin is a C# wrapper for the AirConsole Javascript API.
AirConsole provides a simple to use Javascript API for game developers to build their own local multiplayer realtime browser games.
You can find the Javascript API documentation here: <https://airconsole.github.io/airconsole-api>

IMPORTANT: The plugin comes with an embedded webserver / websocket-server for the communication between the AirConsole backend and the Unity-Editor.
You don't need to install any other webserver or services.

## Changelog

Please see [CHANGELOG.md](CHANGELOG.md) for the full changelog.

## Releasing (maintainers)

1. Update `VERSION` in `Assets/AirConsole/scripts/Runtime/Settings.cs`.
2. Run **Tools > AirConsole > Package Plugin**. It exports `Builds/airconsole-unity-plugin-v{VERSION}.unitypackage`, renames `## [Unreleased]` in `CHANGELOG.md` to `## [{VERSION}] - yyyy-MM-dd`, and stages both.
3. Check the release locally: `scripts/release.py --dry-run`. It creates nothing.
4. Commit as `Release v{VERSION}`, open the PR, and merge it after approval.
   The `Create Release` workflow then creates the GitHub release and the Release Log sheet row.
5. Manual steps:
   - Unity Asset Store: make a draft of the current package, add the release notes and version, and upload the `.unitypackage` with the Asset Store Tools. In the additional information, write: "We include unity webgl templates because webgl games deployed to our platform require a specific setup."
   - AppEngine: accept the new plugin version.
   - After Unity accepts the version, announce it in the Discord Unity channel.

## Upgrading your installation

The upgrade instructions can be found in <https://github.com/AirConsole/airconsole-unity-plugin/wiki/Upgrading-the-Unity-Plugin-to-a-newer-version>

## Documentation

All install instructions and examples are documented in the file "[Documentation_1.7.pdf](./Assets/AirConsole/Documentation_1.7.pdf)" inside this folder.
There are more examples on the website: <https://developers.airconsole.com/>

## Platforms

AirConsole supports WebGL and AndroidTV as targets. To make use of this plugin, you need to make sure to switch your target to either WebGL or Android.

### 1. Android instructions

#### 1.1 Debugging the Chromium webview in Android Builds

To be able to connect to the Chrome Webview, launch the Android based AirConsole application using ADB and add the intent extra `webview_debuggable`

Example: `adb shell am start -n <package_name>/com.unity3d.player.UnityPlayerActivity --ez webview_debuggable true`

#### 1.2 Debugging AirConsole Platform Messages in Android Builds

To have all received platform message payloads logged, launch the Android based AirConsole application using ADB and add the intent extra `log_platform_messages`

Example: `adb shell am start -n <package_name>/com.unity3d.player.UnityPlayerActivity --ez log_platform_messages true`

## Support

If you need support, please visit: <https://developers.airconsole.com/faq-help>
