# AGENTS GUIDE — Assets/AirConsole Focus

The `airconsole-unity-plugin` project targets Unity 2022.3.x and ships the full AirConsole runtime, editor tooling, Android TV helpers, and WebGL controller templates. This guide is tailored to the contents under `Assets/AirConsole`.

## What Lives in Assets/AirConsole

- `scripts/Runtime`: Core API surface (`AirConsole.cs`), websocket/webview bridges (`WebsocketListener.cs`, `WebViewManager.cs`), runtime settings (`Settings.cs`, `AirconsoleRuntimeSettings.cs`, `NativeGameSizingSettings.cs`), logging (`AirConsoleLogger.cs`, `DebugLevel.cs`), platform runtime configurators, and Android plugin wrappers under `Runtime/Plugin/Android`.
- `scripts/Editor`: Custom inspectors/windows (`Inspector.cs`, `SettingWindow.cs`, `PlayMode.cs`, `SafeAreaTester.cs`, `WebListener.cs`), build automation (`BuildAutomation/*`), project maintenance checks (`ProjectMaintenance/*`), and plugin developer helpers (`PluginDevelopment/*`).
- `scripts/SupportCheck`: Unity version/platform validator ASMDEF used in-editor.
- `scripts/Tests`: ASMDEF-scoped EditMode, PlayMode, and Editor tests.
- `examples`: Sample scenes (`basic`, `pong`, `platformer`, `swipe`, `game-states`, `safe-area`, `translations`) for manual validation.
- `extras`: Controller template HTML and `HighScoreHelper.cs`.
- `plugins`: Managed dependencies (`Newtonsoft.Json.dll`, `websocket-sharp.dll`) plus Android native plugin payload under `plugins/Android`.
- `unity-webview`: Embedded WebView plugin with runtime/editor asmdefs and native binaries under `Plugins`.
- `unity-webview` is an imported external dependency. You are not allowed to alter it.
- Docs and misc: `Documentation_1.7.pdf`, `Documentation for AndroidTV.pdf`, `Upgrade_Plugin_Version.md`, `README.txt`, `airconsole.prefs`, and the distributable `airconsole-code.unitypackage`.

## Runtime Essentials

- **API + bridge**: `AirConsole.cs` exposes the public API; `WebsocketListener` routes JSON actions/events; `WebViewManager` coordinates the embedded webview. Add new events in both the listener and the API surface.
- **Config assets**: `AirconsoleRuntimeSettings.asset` and `NativeGameSizingSettings.asset` (in `scripts/Runtime/Resources`) hold defaults for ports, safe-area sizing, etc. Adjust via the editor UI instead of hardcoding.
- **Platform setup**: Implementations of `IRuntimeConfigurator` (`AndroidRuntimeConfigurator`, `WebGLRuntimeConfigurator`, `EditorRuntimeConfigurator`) handle platform-specific initialization. Extend these rather than branching deep in the API.
- **Android plugin wrappers**: `PluginManager`, `AndroidUnityUtils`, and callback helpers (`UnityPluginExecutionCallback`, `UnityPluginStringCallback`, `GenericUnityPluginCallback`, `UnityAndroidObjectProvider`) encapsulate Java bridge calls. Keep new native hooks consistent with these wrappers.
- **Logging & diagnostics**: Use `AirConsoleLogger` with `DebugLevel` enums to respect runtime debug settings.

## Editor & Build Tooling

- **UI/inspectors**: `SettingWindow`, `Inspector`, `PlayMode`, `SafeAreaTester`, and `WebListener` provide editor UI and simulator hooks. Prefer extending these instead of adding ad-hoc windows.
- **Build automation** (`scripts/Editor/BuildAutomation`):
  - `PreBuildProcessing` and `PostBuildProcess` wrap pre/post hooks; post steps validate WebGL template AirConsole JS versions.
  - `AndroidManifestProcessor`, `AndroidGradleProcessor`, `AndroidProjectProcessor`, `AndroidManifest`, and `AndroidXMLDocument` mutate generated Android projects/manifests. Use these helpers instead of manual IO.
- **Project maintenance** (`ProjectMaintenance`): `ProjectDependencyCheck`, `SemVerCheck`, and `ProjectConfigurationCheck` enforce Unity version and project settings; `ProjectPreferenceManager/ProjectPreferences` manage stored preferences; `ProjectUpgradeEditor` supports upgrade flows.
- **Plugin development helpers** (`PluginDevelopment`): `BuildHelper` hosts build/packaging entry points (Web/Android) and can auto-zip or auto-commit; `DevelopmentTools` includes additional packager utilities.

## Tests, Samples, Validation

- Tests live under `scripts/Tests` (EditMode/PlayMode/Editor asmdefs). Run via Unity Test Runner; some PlayMode tests expect Android as the active build target.
- Manual checks: leverage sample scenes in `examples` to verify controller flows, safe-area behavior, translations, and Android TV input.
- WebGL validation: keep `PostBuildProcess` version checks aligned with the AirConsole JS used in `Assets/WebGLTemplates/AirConsole-*`.

## Dependencies & Third-Party

- Managed DLLs: `Newtonsoft.Json.dll` and `websocket-sharp.dll` are under `plugins/` and referenced by runtime asmdefs.
- Embedded webview: `unity-webview` includes runtime/editor code plus native libs under `unity-webview/Plugins`.
- Android native pieces are under `plugins/Android`; ensure gradle/manifest processors remain compatible when updating them.

## Common Workflows

- **Add/extend an API event**: Update `WebsocketListener` to parse/route the action, expose it in `AirConsole`, and wire through logging; add tests where possible.
- **Adjust runtime defaults**: Modify `AirconsoleRuntimeSettings.asset` or `NativeGameSizingSettings.asset` (via editor windows). Avoid hardcoded constants in code.
- **Android manifest/Gradle edits**: Implement changes in `AndroidManifestProcessor`/`AndroidGradleProcessor`/`AndroidProjectProcessor`; ensure `useCustomMainManifest` stays enabled.
- **WebGL template/JS version bumps**: Update constants/regex in `PostBuildProcess`, refresh `Settings.RequiredMinimumVersion` if needed, and sync HTML in `Assets/WebGLTemplates/AirConsole-*`.
- **Packaging/exports**: Use `BuildHelper.BuildWeb()`, `BuildHelper.BuildAndroid()`, or `BuildHelper.BuildAndroidInternal()`; they run configuration checks and can zip outputs.

## Guardrails & Gotchas

- Conditional compilation is heavily used: `#if !DISABLE_AIRCONSOLE`, `UNITY_ANDROID`, `UNITY_EDITOR`, `UNITY_WEBGL`. Keep new code behind appropriate defines and asmdefs.
- Respect asmdef boundaries (`AirConsole.Runtime`, `AirConsole.Editor`, `AirConsole.SupportCheck`, `AirConsole.Examples`, `unity-webview`); place new files in matching folders.
- Embedded websocket/webview server underpins editor play; avoid breaking `WebsocketListener`/`WebViewManager` interactions (ports come from runtime settings).
- Safe-area handling is central for Android TV/Automotive—maintain `OnSafeAreaChanged` flows and avoid state changes before safe-area readiness.
- Build helper may auto-commit with `build: TIMESTAMP` when uncommitted changes exist; be mindful when running locally.

## Event Queue Architecture (Thread-Safe Callbacks)

The WebView plugin uses a thread-safe event queue to handle callbacks from native code (Android only at this point):

### Event Processing Lifecycle

Events are processed at multiple points to ensure timely delivery:

1. **`Update()`**: Primary drain point (every frame)
2. **`LateUpdate()`**: Secondary drain (end of frame)
3. **`FixedUpdate()`**: Tertiary drain (physics frame / fixed timestep / before rendering)
4. **`OnApplicationPause(false)`**: Flush on resume
5. **`OnDisable()`**: Flush before component teardown
6. **`OnApplicationQuit()`**: Final flush before app exit

## Additional References

- `Documentation_1.7.pdf` (setup/examples), `Documentation for AndroidTV.pdf`, and `Upgrade_Plugin_Version.md` for upgrade flows.
- Controller HTML template: `extras/controller-template.html`.
- Quick intro: `Assets/AirConsole/README.txt`; preferences file `airconsole.prefs`.
