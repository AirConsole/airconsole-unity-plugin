# Info

The AirConsole-Unity-Plugin is a C# wrapper for the AirConsole Javascript API.
AirConsole provides a simple to use Javascript API for game developers to build their own local multiplayer realtime browser games.
You can find the Javascript API documentation here: <https://airconsole.github.io/airconsole-api>

IMPORTANT: The plugin comes with an embedded webserver / websocket-server for the communication between the AirConsole backend and the Unity-Editor.
You don't need to install any other webserver or services.

## Requirements

The plugin requires Unity 2022.3 or newer. In an older Unity version, the plugin shows an error when scripts load and stops every build, unless the scripting define symbol `DISABLE_AIRCONSOLE` is set.

For security (CVE-2025-59489), the plugin shows an error (Android target) or a warning (WebGL target) when the Unity release is older than:

| Unity | Minimum release |
| --- | --- |
| 2022.3 | 2022.3.62f2 |
| 6000.0 | 6000.0.58f2 |
| 6000.1 | 6000.1.17f1 |
| 6000.2 | 6000.2.6f2 |
| 6000.3 and newer | 6000.3.0f1 |

## Changelog

Please see [CHANGELOG.md](CHANGELOG.md) for the full changelog.

## Releasing (maintainers)

On `master`, `VERSION` in `Assets/AirConsole/scripts/Runtime/Settings.cs` is the last released version. Feature PRs add their notes to the `## [Unreleased]` section of `CHANGELOG.md`. Do not change `VERSION` by hand: the release sets it.

1. Run the **Create Release** workflow (Actions tab) on `master` with `dry_run` on. Select the `bump`: `patch` for a fix release (2.6.3 → 2.6.4), `minor` for new features (2.6.3 → 2.7.0), `major` for changes that break games (2.6.3 → 3.0.0). The workflow sets `VERSION` to the next version, test-builds WebGL, exports the `.unitypackage`, and uploads the package as a workflow artifact. The run summary shows the release notes (`## [Unreleased]` dated as `## [{VERSION}] - yyyy-MM-dd`) and the release PR commands and body. It creates nothing.
2. Run it again with `dry_run` off and the same `bump`. It dates the CHANGELOG and opens the `Release v{VERSION}` PR from `release/v{VERSION}` with the new `VERSION`, the package and the dated CHANGELOG. Close and reopen the PR to start the required checks (a PR opened by the workflow token does not start them).
3. Merge the PR. The workflow then creates the `v{VERSION}` tag and the GitHub release with the package.

To preview the release without Unity, on `master`: `scripts/release.py --bump patch --open-pr --dry-run` prints the next version, the notes and the release PR. It changes nothing.

To run the same steps locally with your Unity editor (close the project in the Editor first, and run `mise install` once for the Python 3.12 that CI uses): `scripts/release_local.py --bump patch --dry-run` builds and validates, prints the branch, commit, push and PR commands without running them, and resets the files the build and the export changed. Without `--dry-run`, on `master`, it opens the release PR.
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
