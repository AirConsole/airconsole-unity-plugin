# Project Overview
- Purpose: Unity plugin providing a C# wrapper around the AirConsole JavaScript API so Unity developers can build local multiplayer games that communicate with the AirConsole backend through the embedded web/websocket server.
- Tech stack: Unity/C# scripts plus accompanying assets located primarily under `Assets/`. Targets AirConsole-supported platforms (WebGL and Android TV).
- Repo structure highlights: Unity project root with standard folders (`Assets`, `Packages`, `ProjectSettings`, etc.), csproj files for various Unity modules/tests, documentation PDF in `Assets/AirConsole/Documentation_1.7.pdf`, and changelog/README at root.
- Entry points: open the Unity project (`airconsole-unity-plugin.sln` or via Unity Hub) and use provided AirConsole scenes/examples under `Assets/AirConsole` as the starting point.
- Special notes: includes embedded webserver/websocket server for editor communication, so no extra server dependency is required.