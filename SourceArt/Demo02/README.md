# Editable production sources

This directory contains the saved native production sources and guarded export tools for Demo 2. Unity runtime assets are under `Assets/Demo02`; these native sources remain editable independently of Play Mode.

- `Characters/Learner`, `Characters/Colleague` and `Characters/ModuleLead` contain versioned meshes, rigs and motion sources. Current bindings and retained compatibility dependencies are documented in [learner production sources](../../Docs/AI/Task5EditableProductionSources.md) and [NPC sources](../../Docs/AI/NPCInteractionSources.md).
- `Characters/Leena/Demo01_Mechanical_v003` preserves the canonical Leena source.
- `Environment` contains editable campus, backdrop, masonry, foliage, props and lawn sources.
- `UI/ReviewStillLife_v001` and `UI/ReviewStillLife_v002` contain editable review artwork. Gameplay labels remain native Unity text.
- `Materials`, `Exports` and `Tools` retain material sources, export manifests and reproducible authoring/validation utilities.

Use the documented current masters and exact runtime bindings. A higher version number alone does not make every older dependency obsolete. Do not rerun historical construction recipes over manually edited sources or the saved campus. Export to a new versioned destination, preserve earlier evidence, and validate import fidelity, appearance and relevant interactions before rebinding.

Provenance and admission decisions are recorded in [AssetProductionLedger.md](../../Docs/AI/AssetProductionLedger.md). Private source packs and local production studies are not part of this public source directory.
