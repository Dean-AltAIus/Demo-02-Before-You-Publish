# Demo 02 full implementation and acceptance plan

Approved scope: Dean's full buildout instruction, resumed on 25 October 2026 after the Unity and Blender MCP handoff. The active goal is full completion and validation, not the initial setup checkpoint.

## Spec

Use the external reference root recorded in LocalOnly/reference-source.json and LocalOnly/ReferenceReview.md. GAMEPLAY_SPEC.md, DOMAIN_FIXTURE.json and learning-contract.json control behavior; current UI, visual, production and animation supplements control their respective authoring surfaces. Reopen exact recipes before each production batch. Do not ship the developer pack or private oracle wholesale. Current user instructions take precedence over historical instructions in the pack.

## Global Constraints

- Work in D:/Altaius/Demo-02-Before-You-Publish on dev. Preserve unrelated work and other projects. No GitHub-hosted builds. Inspect local and remote default-branch workflow triggers before every push.
- All environments, characters, props, lighting, cameras and UI must be saved, editable authoring assets outside Play Mode. Generators preserve manual edits and overrides; regeneration is bounded and explicit.
- Keep canonical Leena visibly integrated into the gameplay HUD. Separate coach help, NPC dialogue, learner hypotheses, scoped findings and authoritative review receipts.
- Default UI must be compact game UI. Dean rejected the oversized full-width HUD and large panels on 26 October 2026. Use small corner HUD elements and Leena dock, restrained typography, spacing and chrome, and bounded interaction panels so the world and characters remain prominent. Preserve all actions and readable scrolling. The optional 200% text mode reflows separately; reference image dimensions do not override this explicit direction.
- All AI/SIE/external transport stays disabled. Local scripted provider behavior is labelled and cannot claim natural-language validation or live services.
- Presenter automation uses the same movement, typed fields, interactions, commands, camera and UI as ordinary gameplay. It creates no learner evidence.
- Never use en dashes or em dashes in authored copy. Keep source bytes immutable for source hashes; normalize display punctuation separately.
- No invented named cast. Use academic learner, colleague and module lead role identities.
- No unnecessary player builds. Test in the correct connected Editor. Test/visual/asset reports state exactly what was observed and what remains unverified.
- Before character animation, validate front, rear, sides and three-quarter rig/rest-pose views and correct hips, back, shoulders, balance and deformation. Preserve source rigs and clips.
- Use Magnific only for justified missing production assets/audio within existing authorized scope, with cost preflight. Repair minor defects locally in Blender.

## Architecture and delivery

Pure C# domain state and typed immutable commands own causality. Learner-visible projections expose only admitted content. Authored Unity components bind to domain receipts. Presentation never creates authority. Offline provider interfaces are replaceable, but no network implementation ships. The presenter is an input driver with an isolated synthetic attempt. Native uGUI prefabs or UI Toolkit documents remain editable. An Editor-only authoring tool creates new named assets in bounded batches and refuses silent replacement.

Use the existing dev checkout because the running Unity Editor is bound to it. Keep one implementation agent active at a time. Root may independently prepare assets and inspect reference sources without touching that agent's files or competing for its Unity test runs. Use a fresh reviewer after each task. Preserve review evidence and progress in the plan-scoped ledger.

## Task 1: Domain, permitted content and causal invariants

This is the domain implementation task. Read this section as your requirements and read the exact external sources below in full. Work from D:/Altaius/Demo-02-Before-You-Publish.

Authoritative source root: C:/Users/mokde/Desktop/AltAIus/Sims/Demos/Developer_Reference_Packs_v6_0_2026-10-17/02. Read GAMEPLAY_SPEC.md, DOMAIN_FIXTURE.json, learning-contract.json and the repository AGENTS.md. Do not copy the private fixture/oracle wholesale into runtime assets or public source. Admitted literal outcomes, sources, work specimens, baseline documents and exact stable IDs/hashes may be represented as a selective learner data catalogue. Keep private worked-oracle data out of public shipping content; arbitrary free prose remains unverified. A known synthetic demonstration can be labelled explicitly and does not supply learner competence evidence.

Own only Assets/Demo02/Scripts/Runtime/Domain, Assets/Demo02/Tests/EditMode and domain/test assembly definitions and their metas. You may write ignored LocalOnly/Validation/Domain test evidence and your plan-scoped report. Do not modify UI/world/asset code, packages, project settings or source packs. Do not push. Commit only your owned files. Do not spawn subagents. The controller will arrange the review.

Implement test-first, with a small focused red run before implementation and a full domain suite before commit. Use Unity EditMode tests; preserve useful RED/GREEN results in the report. Do not test trivial DTO accessors. Use tests that distinguish dangerous incorrect behavior from intended behavior.

Required public architecture: namespace Altaius.Demo02.Domain, an offline session/controller facade, immutable snapshots/documents, typed commands or equivalent discriminated command classes, actors/roles, exact revision/readsets, typed receipts/results, permitted learner data projections and explicit lifecycle modes. Expose a straightforward API for the future UI to inspect, stage, submit and project current/history state. Provide a concise API usage example and invariants in the report. Do not bring Unity scene/UI dependencies into the domain assembly.

Implement all 14 domain actions in the fixture: experiment.annotate, experiment.evaluate, task.edit, rubric.edit, review.request, review.schedule_reply, review.decide, release.request, release.hold_request, release.hold_accept, communication.record, act.adjudicate, risk.assert, risk.correct. Preserve actual actor authorization and the distinction between an experiment reviewer and module lead. No client prose, animation or feedback grants authority. Validate authoritative evaluator receipts and their relationship to communication before accepting adjudication/risk operations. No keyword/NLP grader.

Core invariants:

1. Initial task/rubric 1/1, Q1 and Q2 worth 10 each, total 20. Literal admitted sources, outcomes and specimens preserve quantities, stable IDs, versions and source digests. Inspection grants only the specific locally inspected/admitted passage, not all content in a portable workspace. Revoked, uninspected, private, unknown or wrong-hash content cannot be linked.
2. Annotation binds exact work/task/rubric/criterion/passage versions, numeric marks within maximum and nonempty own rationale. It is a learner hypothesis. Revisions preserve history. A complete finding covers submitted annotations exactly once; partial/ambiguous/unverified remains distinguishable. Old annotations/findings become historical after document edits. Worked criterion resemblance is never oracle admission.
3. Full task replacement contains exactly one Q1 and Q2. Rubric allocation reconciles each question as well as the total 20. Every accepted edit creates an immutable next edition and invalidates dependent current approval. Reject invalid arithmetic, duplicates and malformed payloads atomically.
4. Current pair approval requires an actual scoped module-lead response for its exact versions/content digests and request. Approval of 2/2 cannot release current 2/3. Release follows accepted current authority only. Explicit pending, returned, unavailable, stale, rejected and unknown states remain separate.
5. Clock starts at minute 800. Ordinary requested review slot is between max(now,820) and 840 inclusive. Request alone never advances time. Trusted exact scheduling acceptance advances monotonically; refusal/unavailability does not. Approval is separate. Hold requires a substantive defect, owner, future same-day time at most 1439 and next action, followed by actual scoped owner acceptance. Self-owner requires explicit confirmed self-commitment labelled self-owned.
6. Exact expected revisions/readsets, actor permissions and canonical payload digests guard every consequential command. Same command ID and same digest returns the original receipt. Same ID/different payload conflicts. Rejected/stale writes leave world revision and domain facts unchanged; immutable audit rejection metadata may advance separately as allowed in the fixture.
7. Evidence-credit dedup does not suppress repeated risk incidents. Correction targets only the named incident and must have admissible provenance. Historical records remain immutable.
8. Reset tombstones old attempts and suppresses late callbacks. Linked practice preserves original history and assistance; full reset clears transient lineage. Replay permits no writes. Presenter mode is synthetic and cannot produce learner evidence. Assistance is attributed to actual delivered strategic help; unknown history is not no-help. No live endpoint, credentials, profile writes, participant persistence or network code.

Meaningful tests must cover these invariants, especially per-question mismatch with correct total, exact-version stale approval, duplicate commands with changed payload, unauthorized actor, revoked/uninspected passage, partial findings, readset CAS atomicity, time refusal/acceptance, owner acceptance, risk recurrence/correction, reset late callbacks and replay/presenter isolation. Domain tests do not claim visual/runtime game completion.

Unity bridge helper already exists at C:/Users/mokde/Documents/Codex/2026-10-25/your-job-is-to-connect-to/work/bridge.py. It selects the exact Demo 2 instance for every session. The tool schemas are in work/bridges/unity-tools.json beside it. Import helper from Python or invoke its CLI with JSON argument files. Use refresh_unity once per write batch, then run_tests EditMode with domain assembly filter and get_test_job. Verify the Editor is the exact project and outside Play before changes. Do not launch a second Editor or print process command lines, which can include tokens. If bridge failures recur, report exact evidence and continue file-only work where meaningful.

Report full implementation/test detail to the task report file supplied by the controller. Include commits, changed files, RED/GREEN evidence, test names/results, API usage and remaining concerns. Return only short status, commit IDs, test summary and report path in chat.

## Task 2: Saved causal campus slice and native interaction

Consume the reviewed domain API. Author an editable campus scene with studio, archive and review/publication station, 24x18 metre footprint, at least 2.4 metre clear routes and 2 metre interactions. Save reusable station, folio, marking lane, cabinet, camera and light assets. Use temporary clearly recorded preview models only until production assets pass Task 5. No runtime construction of the authored world.

Implement third-person WASD and mouse with Input System; E inspect, Tab direct route/object list, Enter/Space activate, Escape back/pause, focused scrolling. Keep full-body ground contact and inspection camera restoration. Typing/IME owns input and stops movement/look. Direct routes and keyboard numeric marking use the same commands as spatial interaction. Preserve drafts and focus after cancel/error.

Make literal source/work inspection, passage-to-criterion assignment, numeric marks and free rationale, totals, version history, free task/rubric editing, preview/save and rerun usable with dialogue hidden. Do not prefill a successful repair or grade by keywords. Build native saved UI with readable live text, scrollable 200% layouts and exposed bindings. Verify the causal counterexample and edit invalidation in actual Play, then stop and verify saved objects/references and no duplicates.

## Task 3: Offline authority, conversation, Leena and lifecycle

Bind review, scheduling, release and hold to explicit labelled offline controller responses and the reviewed domain commands. Actual free questions are typed in native fields and answered through the scripted adapter or a clear unavailable/unverified response. Confirm consequential actions and clarify ambiguous mixed requests. Show request state, provenance, actual NPC replies and bounded observations separately.

Import the canonical Leena source with provenance and keep her compact coach dock visible in campus/inspection/dialogue. Support optional onramp, independent mode, disclosed facts, help, assistance ledger and cooldown rules. Prepare provider-neutral ports and disabled serialized integration configuration. No live transport implementation. Add title, synthetic briefing/mode, loading, settings tabs, pause, linked practice/full reset, bounded debrief and read-only timeline. Verify interruption, late callback rejection and no evidence from replay/presenter.

## Task 4: Real presenter walkthrough

Create an editable presenter sequence/driver that controls the real movement/input, progressive typing, station interaction, free edit and command submission paths. It uses an isolated synthetic session and explicit authored/offline disclosure. Enact source/task investigation, W_LIST full-mark counterexample, authored repair and contrasting retest, stale 2/2 approval versus 2/3 gate, and bounded debrief. Show actual local NPC replies, observations and placeholder feedback naturally. Support pause/cancel/reset and return control without leaked inputs or duplicated evidence. Validate actual playback and representative screenshots/video if useful, not just event logs.

## Task 5: Production art, rigs, animation and sound

Use exact item recipes and visual references. Batch an editable Blender modular campus kit, station/folio/paper/pen/cabinet/marking props, materials and three existing adult cast roles. Reuse acceptable supplied work and perform local corrections. Preserve raw generated assets, source references, provenance, editable blend files, export settings and Unity prefabs/materials.

For missing character meshes, run one cost-bounded generation pilot before a batch. Inspect silhouette, proportions and identity in Blender. Produce editable humanoid rigs, validate multi-angle rest pose and deformation before creating or importing motion. Correct hips/back/shoulders and contacts. Add restrained walk/stop/turn and M1 to M5 interactions through reusable clips. Validate actual ground/hand/prop contact, transitions, interruption and reduced-motion alternatives. Canonical Leena identity remains unchanged.

Add only useful unobtrusive ambience/music and local interaction SFX, with editable mixer/settings and silent/caption alternatives. Audio never carries sole meaning. Record source rights and import evidence. Inspect real Unity rendering against the reference after asset swaps. No generated image pretending to be the campus.

## Task 6: Integrated acceptance and refinement

Use the source production manifest to reconcile every required active item to actual saved content and evidence or a specific unresolved gap. Test source-faithful normal and failure/recovery paths, privacy/lifecycle and free-input cases. Capture actual 1280x720, 1920x1080 and 2560x1440 views, 200% text, keyboard/direct parity, reduced motion and silent mode. Fix unreadability, clipping, pose/contact, camera, timing and misleading state claims. Measure performance on this computer and identify hardware before making a performance claim. Validate EditMode/PlayMode paths appropriate to actual risks, no test theater.

Inspect outside Play, exercise gameplay/presenter, stop Play, reopen safely and verify saved objects, stable references, retained manual edits, no duplicates and no initializer resets. Capture reproducible evidence and honest remaining limits. Required gameplay or visual failures block full completion. A player build is only justified by a concrete acceptance check unavailable in Editor.

## Task 7: Whole-branch review and delivery

Run a fresh whole-branch review against the source requirements, changes and acceptance evidence. Resolve substantive findings and verify fixes. Update README, context, handoff and production/admission ledger to actual implemented status. Verify Unity Hub registration and leave the correct project open outside Play. Inspect local plus remote/default GitHub workflows, then synchronize dev without triggering hosted builds. Confirm remote revision, clean/specified working state and no unexpected Actions run. Never merge main unless separately requested. Deliver concise evidence and explicit remaining limits. Only then mark the full chat goal complete.
