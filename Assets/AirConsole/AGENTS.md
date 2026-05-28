# Assets/AirConsole

This subtree contains the shipped Unity runtime, editor tooling, examples, and native wrappers.

## Local Focus Areas

- Runtime API and bridges: `scripts/Runtime/`
- Editor tooling and build processors: `scripts/Editor/`
- Automated tests: `scripts/Tests/`
- Examples and manual smoke scenes: `examples/`

## Local Invariants

- Extend platform-specific behavior through the existing configurators and Android wrapper classes instead of scattering `#if UNITY_*` checks.
- Keep asmdef boundaries intact (`AirConsole.Runtime`, `AirConsole.Editor`, `AirConsole.SupportCheck`, examples/tests).
- Preserve safe-area and main-thread callback behavior when touching Android or WebView flows.
- Treat `unity-webview/` as vendored unless the task explicitly belongs there.

## Common Checks

- Add matching runtime/editor tests where feasible.
- For manifest or Gradle output changes, use the existing build processors in `scripts/Editor/BuildAutomation/`.

## Learnings

- _None yet._
