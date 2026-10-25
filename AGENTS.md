# Demo 02 working instructions

## Scope and checkpoint

The user requested project/repository setup and a full reference review first. Stop with this project open for Dean to install MCP for Unity and start its server. Do not install MCP for him or continue the game buildout before that handoff.

Read Docs/AI/UnityProjectContext.md and the ignored LocalOnly/ReferenceReview.md on continuation. Revalidate the actual Git branch, editor process and reference hashes. Source summaries are navigation aids; reopen the exact source and item recipe before implementation.

## Dean's requirements

- Work on dev and preserve unrelated work. Never trigger a GitHub-hosted build. Before any push inspect applicable Actions triggers, including remote/default-branch workflows. Do not manually dispatch build workflows.
- Save editable scenes and reusable prefabs/native assets for environments, characters, lighting, cameras and UI. Make them visible outside Play Mode. Runtime construction cannot replace authored content.
- Batch related authoring work. Generators preserve manual edits, prefab overrides and references. Regeneration must be explicit and bounded.
- Validate saved content, exercise relevant gameplay, stop Play, then verify persistence, references, manual edits, no duplicates and no initializer resets.
- Keep canonical Leena creatively integrated into the HUD at all times during gameplay, distinct from NPC conversation. Preserve her white/navy/cyan identity.
- The presenter walkthrough must be automated gameplay using the same systems: real movement, typed input appearing progressively, actual interactions, visible placeholder feedback/observations and scripted NPC responses. Clearly disclose authored/offline presentation and keep it out of learner evidence.
- Prepare integration interfaces and endpoint/configuration slots during buildout, but leave AI/SIE and external transport disconnected. No credentials or live Control Centre writes.
- Before animation, inspect rigged bodies from front, rear, sides and three-quarter angles, then correct hips, spine/back, shoulders, rest pose, balance, deformation and contacts. Mixamo is an available route after compatibility/licence checks. Preserve editable rigs and clips.
- Magnific is available for needed music, sound effects and missing assets. Do not use generation for minor fixes. Accept decent supplied assets and repair locally in Blender when appropriate. Preserve sources and provenance.
- Functional completion alone is insufficient. Validate visuals, camera, animation, contact, UI readability and interaction against the references and expected experience.
- No unnecessary player builds. Do not alter Demo 1, Grand Bazaar, Insight or other projects.
- Never use en dashes or em dashes in authored copy. Preserve original source bytes separately when checking hashes; normalize display punctuation only without changing source meaning.
- Preserve existing cast roles. Before inventing any personal character name, read C:/Users/mokde/.codex/skills/simulation-character-registry/SKILL.md, refresh its registry, check variants and reserve the name.

For Unity authoring read C:/Users/mokde/.codex/skills/unity-editable-scene-authoring/SKILL.md. Apply Dean's current session instructions before historical pack guidance.
