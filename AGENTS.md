# airconsole-unity-plugin
## OVERVIEW
AirConsole SDK for Unity.
C# wrapper around the AirConsole browser API.
Android native bridge for Android builds.
WebGL JavaScript generation for browser builds.
Main targets: Android and WebGL.
Bridge languages: C#, Java or Kotlin, JavaScript.
Two message patterns exist.
Old pattern: polling through `GetMessage()`.
New pattern: queue based event delivery.
Prefer queue based delivery for new Android bridge behavior.
## ENTRY POINT
Main SDK entry: `Assets/AirConsole/scripts/Runtime/AirConsole.cs`.
`AirConsole.cs` owns the singleton.
The singleton is the main public API surface.
Unity scenes call this API for connection state, messages, device data, and platform actions.
Android bridge entry uses `AndroidJavaProxy`.
Native callbacks enter Unity through proxy handlers.
WebGL entry uses generated JavaScript.
Generated JS exposes the runtime browser interface for Unity WebGL builds.
## ARCHITECTURE
Old Android message path:
1. Native side stores incoming messages.
2. Unity polls with `GetMessage()`.
3. `UnitySendMessageDispatcher` forwards payloads into Unity.
4. C# parses payloads and raises SDK callbacks.
New Android message path:
1. Native side calls an `AndroidJavaProxy` callback.
2. Proxy receives payloads off the Unity main thread.
3. Proxy writes work items into a `ConcurrentQueue`.
4. Unity main thread drains the queue.
5. SDK events fire during main thread processing.
WebGL message path:
1. Build postprocess generates JS from C# reflection.
2. Runtime JS binds Unity calls to AirConsole browser API calls.
3. Browser callbacks route back into Unity.
## ANDROID BRIDGE
Android bridge code lives under `Assets/AirConsole/plugins/Android/`.
`PluginManager` initializes native bridge state.
`PluginManager` connects Unity runtime and Android plugin code.
`AndroidJavaProxy` handles messages from native code.
Proxy handlers must stay small.
Proxy handlers must not call Unity APIs directly from background threads.
Use the main thread queue for message delivery.
Use `ConcurrentQueue` for thread safe handoff.
Drain queued actions or messages from Unity main thread update flow.
Preserve payload shape when moving between native and C#.
## WEBGL EXPORT
Build postprocess generates `airconsole-unity-plugin.js`.
Generation uses C# reflection.
Generated JS mirrors the callable SDK surface needed by WebGL builds.
Runtime JS interface talks to the AirConsole browser API.
Unity WebGL calls route through generated JS functions.
Browser events route back into Unity objects.
Change the generator or C# source instead of hand editing generated JS.
## STRUCTURE
`Assets/AirConsole/` contains the main SDK.
`Assets/AirConsole/scripts/Runtime/AirConsole.cs` contains the singleton and main public API.
`Assets/AirConsole/plugins/Android/` contains native bridge code and Android plugin assets.
`Assets/AirConsole/scripts/Editor/` contains editor tooling and build postprocessing.
`Assets/AirConsole/scripts/Editor/BuildAutomation/` is the build generation area.
Sample scenes live under the AirConsole assets tree.
`.compound-engineering/solutions/` contains documented solutions to past problems, organized by category with YAML frontmatter (module, tags, problem_type).
## WHERE TO LOOK
Main SDK: `Assets/AirConsole/scripts/Runtime/AirConsole.cs`.
Android bridge: `Assets/AirConsole/plugins/Android/`.
Build generation: `Assets/AirConsole/scripts/Editor/BuildAutomation/`.
Generated WebGL interface and sample integration: `airconsole-unity-plugin.js`, sample scenes.
## BUILD POSTPROCESSING
Unity build postprocess auto runs after supported builds.
It generates Android manifests when needed.
It handles AndroidX setup.
It copies Android AAR files into the build output.
It links iOS frameworks for supported exports.
It generates WebGL JavaScript interface files.
It can export `.unitypackage` distribution packages.
## TESTING
Automated tests are limited.
Use Unity Test Runner when relevant.
Most confidence comes from integration testing.
Run sample scenes for message flow checks.
Test Android bridge behavior on Android builds.
Test WebGL behavior in exported WebGL builds.
Verify old polling behavior when touching legacy paths.
Verify queue based behavior when touching new bridge paths.
## EXPORTS
Distribution output is a `.unitypackage`.
The package includes AirConsole `Assets` and all required `Plugins` content.
The package must include Android bridge assets and WebGL runtime generation support.
## COMMANDS
Unity build: run from Unity Editor or configured Unity batchmode build.
PostprocessBuild auto runs during Unity build.
Export package: use Unity export package workflow for `.unitypackage`.
Run tests: use Unity Test Runner for EditMode and PlayMode suites.
## FORBIDDEN
NEVER hardcode device IDs.
NO platform specific code outside `plugins/`.
Do not bypass the main thread queue for Android callbacks.
Do not call Unity APIs directly from Android background callbacks.
Do not edit generated WebGL JS when generator changes are required.
