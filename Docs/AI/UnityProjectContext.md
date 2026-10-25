# Unity project context

Analyzed 25 October 2026. Setup baseline, before Demo 2 implementation.

## Confirmed environment

- Project: D:/Altaius/Demo-02-Before-You-Publish.
- GitHub: https://github.com/Dean-AltAIus/Demo-02-Before-You-Publish.
- Working branch: dev, created from the initial main commit 72a20208714a4c413331e36a6fd767d4d1be8688.
- Unity: 6000.0.71f1, revision 907bc2d768b5.
- Installed Editor: C:/Program Files/Unity 6000.0.71f1/Editor/Unity.exe.
- Created from the installed 3D URP template, version 17.0.12. URP package 17.0.4.
- Input System package 1.19.0; activeInputHandler 1. Keyboard/mouse is the intended input surface.
- Template also includes AI Navigation 2.0.11, Timeline 1.8.10, uGUI 2.0.0, Test Framework 1.6.0 and Visual Scripting 1.9.10. These are available dependencies, not implemented Demo 2 systems. Built-in UI Toolkit is also available. Final native UI implementation remains a buildout choice.
- No multiplayer gameplay, AI transport or custom framework is implemented.

## Saved content and architecture

Assets/Scenes/SampleScene.unity is the standard saved template scene, with its camera, directional light and global volume. Assets/Settings contains editable URP profiles/assets. These are a project baseline, not the Demo 2 campus.

Assets/Demo02 is reserved for first-party scenes, prefabs, scripts, data, materials, UI, audio and animation. SourceArt/Demo02 is reserved for editable production sources. There are no first-party gameplay scripts or assembly definitions yet. Keep runtime and Editor code separate when authoring begins.

Planned dependency direction: domain state and typed commands, then permitted presentation projections, then input/UI and authored world bindings. Presenter automation and offline scripted response providers use the same commands/presentation. Actual authority, conversation text, rendering, and learning evidence remain separate concerns.

## Sources and continuity

The authoritative pack is external to the repository. LocalOnly/ReferenceReview.md records its exact location, review findings, precedence, validation evidence and gaps. LocalOnly/reference-review/inventory.json is the 841-file SHA-256 baseline. Do not import the developer pack wholesale into Assets or publish it to this public repository.

Current gameplay and domain fixture control mechanics; the learning contract controls interpretation/assistance. Later reference availability and Leena HUD clarifications supplement those sources. Historical UI examples and generated copy do not grant facts, authority or defaults.

## Validation and tooling

The initial Unity batch creation/import completed with exit code 0. No player build or Play Mode test was run. Test Framework is installed, but Demo 2 has no tests yet. Gameplay, art, accessibility, target performance, privacy/reset behavior and post-Play persistence require implementation and actual validation.

The requested MCP for Unity Git URL appeared in Packages/manifest.json during the Editor handoff. The agent did not install it. Its package import and server/bridge readiness remain unverified; Dean owns this setup. Live MCP access is unverified at this checkpoint. Verify the exact project instance before any later Editor operation.

Read ProductionRequirements.md and BuildoutPlan.md for acceptance and work order. Do not mistake folder scaffolding, JSON parsing or static reference checks for a completed game.
