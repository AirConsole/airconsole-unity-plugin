# Info

The AirConsole-Unity-Plugin is a C# wrapper for the AirConsole Javascript API.
AirConsole provides a simple to use Javascript API for game developers to build their own local multiplayer realtime browser games.
You can find the Javascript API documentation here: <https://airconsole.github.io/airconsole-api>

IMPORTANT: The plugin comes with an embedded webserver / websocket-server for the communication between the AirConsole backend and the Unity-Editor.
You don't need to install any other webserver or services.

## Changelog

Please see [CHANGELOG.md](CHANGELOG.md) for the full changelog.

## Releasing (maintainers)

`VERSION` in `Assets/AirConsole/scripts/Runtime/Settings.cs` already holds the next version (it is updated during development) and `CHANGELOG.md` has its `## [Unreleased]` section.

1. Run the **Create Release** workflow (Actions tab) on `master` with `dry_run` on. It test-builds WebGL, exports the `.unitypackage`, dates `## [Unreleased]` as `## [{VERSION}] - yyyy-MM-dd`, validates the release notes, and uploads the package as a workflow artifact. It creates nothing.
2. Run it again with `dry_run` off. It opens the `Release v{VERSION}` PR from `release/v{VERSION}` with the package and the dated CHANGELOG. Close and reopen the PR to start the required checks (a PR opened by the workflow token does not start them).
3. Merge the PR. The workflow then creates the `v{VERSION}` tag, the GitHub release with the package, and the Release Log sheet row.

To run the same steps locally with your Unity editor (close the project in the Editor first): `scripts/release_local.py --dry-run` builds and validates, prints the branch, commit, push, PR, tag and release commands without running them, and resets the files the export changed. Without `--dry-run`, on `master`, it opens the release PR.
4. Manual steps:
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
