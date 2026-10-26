# Unity project context

Demo 2 is an implemented local, offline campus simulation. This document describes its source architecture; [integrated acceptance](IntegratedAcceptance.md) identifies executed checks and remaining limits.

## Environment

The project is `D:/Altaius/Demo-02-Before-You-Publish`, linked to `Dean-AltAIus/Demo-02-Before-You-Publish` on `dev`. The initial `main` commit is `72a20208714a4c413331e36a6fd767d4d1be8688`. Preserve `main` and do not trigger GitHub-hosted builds.

`ProjectVersion.txt` pins Unity 6000.0.71f1, revision 907bc2d768b5. The package manifest includes URP 17.0.4, Input System 1.19.0, uGUI 2.0.0 and Test Framework 1.6.0. The native interface uses uGUI. Template dependencies such as AI Navigation, Timeline and Visual Scripting being installed do not imply those systems implement the demo.

The Unity MCP package is installed from the requested CoplayDev URL and has been used for exact-instance Editor authoring and validation. It is separate from the disconnected simulation AI/SIE boundary. Verify the actual open project before every new Editor session; another project's Editor is never an acceptable target.

## Saved content

`Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity` is the authored gameplay scene. It contains the compact courtyard, four stations, characters, physical marking/review props, lights, camera rig, Leena portrait camera and native interface. Reusable source assets live in the corresponding `Prefabs`, `Art` and `Settings` directories. `Assets/Scenes/SampleScene.unity` is the retained template, not the gameplay start point.

`SourceArt/Demo02` retains editable Blender environments, characters, rigs, motions, materials and review artwork. See `Task5EditableProductionSources.md`, `NPCInteractionSources.md` and `LearnerSourceValidationTools.md` for current master files, exact skin derivatives and safe export paths. Older compatibility assets remain where current serialized references require them.

Authoring tools are explicit and bounded. Preserve manual scene edits, prefab overrides and existing catalogue entries. Runtime code operates on saved assets and data-driven state; it does not replace the campus with a runtime-only generated scene.

`Assets/Demo02/Settings/Presentation/CompactGameUi.asset` exposes the compact HUD, panel, typography and spacing settings. `Task5CompactUiAuthoring.Apply()` is an explicit layout update across the existing UI prefabs and campus overrides; it requires the correct clean saved scene outside Play. Use it only when deliberately updating that layout scope, since its purpose is to apply the style to those authored properties.

`Assets/Demo02/Prefabs/UI/Presentation/title.prefab` contains the dedicated native title composition. Its saved workroom shot reuses the gameplay camera while the title owns presentation, then releases it on return to gameplay. Title layout, ornament and scene-presentation components remain editable separately from the gameplay HUD style.

## Runtime boundaries

- **Domain:** a pure C# assembly owns typed commands, immutable document versions, source permissions, logical time, scoped review/release/hold rules, receipts, audit and reset isolation.
- **Integration and presentation:** pure C# contracts and coordinators manage authored offline replies, cancellation, history, help, assistance attribution and state projections. Reply text is distinct from authority.
- **Gameplay and UI:** Unity assemblies bind the saved campus, collision movement, station interactions, physical contacts, cameras and native fields to the same domain operations. Direct routes provide an alternative to spatial travel.
- **Presenter:** an editable sequence and route configuration drive real movement, progressive native field entry and the ordinary interaction surfaces in an isolated synthetic attempt. Returning restores the prior practice session. See `PresenterWalkthrough.md` for operation and measured timing.
- **Authoring and tests:** Editor code and test assemblies are separate from runtime. Acceptance tools retain measurements and captures locally; a passing source or rig audit does not substitute for rendered gameplay inspection.

`Assets/Demo02/strings/STR_EN.asset` is the saved native English string table. Runtime semantic keys and saved label bindings use it through `EnglishCatalogueBinding`. Static authored copy is separate from literal learner text, admitted sources, identifiers and historical payloads. Named substitutions insert those values once without changing their punctuation or interpreting rich text. The explicit label refresh and missing-key seeding operations have different purposes; do not use broad regeneration to overwrite authored labels.

## Source and delivery boundaries

The authoritative reference pack remains outside the repository. Ignored `LocalOnly/ReferenceReview.md` and the reference inventory record its location, precedence and review. Gameplay contracts control mechanics; the learning contract controls interpretation and assistance. The user's later compact-UI and restrained-world instructions control presentation sizing and scope.

Only admitted learner content belongs in runtime assets. Do not publish the private developer pack, reviewer oracle, credentials or participant records. Endpoint and identity configuration is blank and transport disabled, as described in `IntegrationPreparation.md`.

Final handoff evidence must distinguish native input simulation from unobserved OS/IME delivery, Editor rendering from a Player build, and functional/visual acceptance from production service integration. Verify the current Unity Hub registration, stopped Editor state and remote `dev` revision at delivery rather than inferring them from this architecture document.
