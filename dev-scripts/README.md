# Maintainer scripts

Scripts for plugin maintainers. They are not part of the published plugin.

## test-apks.sh

Runs a manual smoke test of a set of release test APKs on one Android device.
For each APK in a folder, the script uninstalls `com.airconsole.plugin.unity`,
installs the APK, launches it, and asks you for a Pass/Fail verdict.

### Requirements

- `adb` on `PATH`.
- Exactly one Android device connected (`adb devices`).
- All APKs use the application ID `com.airconsole.plugin.unity`.

### Usage

```sh
dev-scripts/test-apks.sh TestBuilds/release-test-2.6.2/ac-plugin
```

With no argument, the script uses the current directory.

For each APK, the script:

1. Shows the progress: `[##.....] 2/7 done  pass 1  fail 1`.
2. Shows the file name, the activity type, and the Unity version.
3. Uninstalls the app, installs the APK, and starts it with
   `--ez webview_debuggable true --ez development_logging true`.
4. Waits until you press `P` (pass) or `F` (fail). Other keys are ignored.

Press `Ctrl-C` to stop the run. Results that are already logged stay in the log.

### File name convention

The script reads the activity type and the Unity version from the APK file name:

| File name contains | Result |
| --- | --- |
| `gameactivity` | GameActivity, launched as `com.unity3d.player.UnityPlayerGameActivity` |
| anything else | Activity, launched as `com.unity3d.player.UnityPlayerActivity` |
| `NNNN.N`, e.g. `2022.3`, `6000.0` | Unity version |
| no version | Unity version `unknown` |

Example: `20260923-0245-com.airconsole.plugin.unity-unknown-prod-6000.0-activity.apk`
gives `Activity`, `6000.0`.

If the name has no `gameactivity` but the build uses GameActivity, the launch fails.
To show the Unity version for GameActivity builds, put the version in the file name.

### Results log

The script appends one line per APK to `results.log` in the APK folder:

```text
2026-09-23 14:12:03  PASS  20260923-0245-com.airconsole.plugin.unity-unknown-prod-6000.0-activity.apk  (Activity, 6000.0)
```

- If `adb install` fails, the APK is logged as `FAIL` and the run continues.
- The log is never cleared. Delete `results.log` before a new run if you want a clean log.
