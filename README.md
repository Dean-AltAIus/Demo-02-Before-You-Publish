# Demo-02-Before-You-Publish

A playable offline campus simulation about university marking and assessment design, authored in Unity 6000.0.71f1 with Universal Render Pipeline 17.0.4.

Inspect fictional source material and student work, record marks and your own reasons, revise the task and rubric, retest, and request review of the exact versions. Review, publication and an accountable hold are separate decisions. Authored replies support the local demonstration; they do not interpret arbitrary prose or provide live AI evaluation.

## Open and play

Open this project from Unity Hub and load `Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity`. Enter Play Mode for the title and practice options. Choose **Watch walkthrough** to open the briefing for the isolated synthetic demonstration. Its controls support pause, restart and return to the preceding practice session.

- Arrow keys move; hold the right mouse button to look; E inspects a nearby station.
- Tab opens direct routes in the world and moves focus within interfaces. Shift+Tab reverses focus. Enter or Space activates a focused action.
- Escape returns or pauses. Native text fields retain their drafts; use the displayed submit action to send multiline text.
- Settings include text scaling, reduced motion, silent mode and high contrast. Direct routes reach the same station commands without travel precision.

The title uses an editable book-and-laurel plaque, a vertical menu and a saved view of the reviewer workroom. The default gameplay HUD uses compact corner controls, a persistent Leena dock and bounded panels. Its editable style is `Assets/Demo02/Settings/Presentation/CompactGameUi.asset`; enlarged text uses a separate reflow. The campus, characters, station props, cameras, lights and native UI are saved and editable outside Play Mode.

## Edit and validate

First-party scenes, prefabs, settings and scripts are under `Assets/Demo02`. Editable Blender sources and export tools are under `SourceArt/Demo02`. Keep manual edits and prefab overrides; authoring tools use explicit bounded operations. Historical construction scripts are not a safe way to regenerate the entire accepted scene.

Read [integrated acceptance](Docs/AI/IntegratedAcceptance.md), [project context](Docs/AI/UnityProjectContext.md), [presenter operation](Docs/AI/PresenterWalkthrough.md), [production sources](Docs/AI/Task5EditableProductionSources.md), [NPC sources](Docs/AI/NPCInteractionSources.md) and [integration preparation](Docs/AI/IntegrationPreparation.md). The acceptance record identifies tested conditions and limits; source inspection and test counts alone do not establish visual or performance acceptance.

Development uses `dev`; `main` is preserved. Do not trigger GitHub-hosted builds or dispatch build workflows. Dean has directed that no further local player builds are needed for this buildout; remaining validation uses the Editor.

The private developer reference pack, reviewer oracles, machine-specific validation receipts, credentials and participant data are excluded from public source. AI/SIE transport is disconnected, with blank endpoint and identity slots. The installed Unity MCP package is an Editor authoring tool, not the simulation's AI integration.
