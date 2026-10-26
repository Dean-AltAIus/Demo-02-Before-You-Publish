# Integrated acceptance

Native functional, visual and saved-authoring checks passed within the scopes below. The required rendered-performance gate remains failed in measured Editor conditions. The first local Player diagnostic could not render. A separately authorized isolated-desktop run later rendered successfully but still missed the frame-time target in courtyard and marking. This is not an all-gates acceptance claim. The production matrix maps all 155 items to saved content, evidence and explicit limits.

The saved native English catalogue has 697 entries and 54 static label bindings. Its literal-data bindings preserve learner prose, source text, identifiers, units, whitespace and review reasons. Primary review/receipt copy explains the original requested pair and current publication state. Exact provenance remains available in read-only details. The saved compact interface, canonical Leena, scene geometry, native motion masters and exact skin weights are retained.

Actual learner input verification used Unity's in-process InputSystem and native uGUI fields. It covered source inspection, 10 plus 10 baseline marking, invalid arithmetic, own task/rubric text, preview cancellation, immutable history, retest, scheduling, unavailable semantic review, rejected release, self-owned unverified hold, delivered help attribution, pending-response cancellation, pause, replay and reset. This does not claim physical keyboard, OS input or IME delivery.

The saved 183-step spatial presenter completed at normal 1x pacing and normal motion in 462.0512 presented seconds, with 104.635 metres of collision-driven travel and the final current Task2/Rubric3 gate closed and no learner evidence. The original learner session was restored on Return. An explicit active-presenter Play exit also passed without unexpected teardown errors. Earlier failed test apparatus runs remain retained and separately labelled.

Required native views cover 1280x720, 1920x1080 and 2560x1440 at 100% and optional 200% text. The review, settings, help, notebook and timeline inspection includes actual Tab focus, silent mode, reduced motion and non-colour state labels. Existing full-turn, room, rear, corner, perimeter and inspection-camera evidence was inspected against unchanged geometry; older UI in those world captures is not final interface proof. Current review screens show the real seated actor and single packet beside the right panel.

The accepted Task 5 full-surface import checks remain current: learner closing, 21 poses, maximum 4.162122 micrometres; learner opening, 27 poses, maximum 4.854601 micrometres; colleague, six poses, maximum 12.20241 micrometres; module reviewer, ten poses, maximum 13.86631 micrometres. Saved and effective skin quality stays Unlimited. These are surface-fidelity results, separate from rendered performance. Task5's retained normal contact playback, queue interruption and reset checks remain evidence for unchanged physical content.

The final movement controls use arrow keys. A focused native check passed all four directions, verified that W/A/S/D no longer move the learner, and retained Tab routes/focus, UI navigation isolation and native text-caret ownership. Authored HUD, saved catalogue, controls help and authoring defaults agree. Stable control identities and catalogue keys did not change. The check passed one test with no failures or skips; summary duration was 42.2741513 seconds and test-case duration was 82.6947016 seconds. The earlier zero-displacement UpArrow failure is retained as the meaningful RED result.

All 110 review/accessibility captures were inspected in labelled sheets, with full-resolution checks of critical states. That overall run later failed an incorrect comparison of runtime rows with saved rows. A separate focused check closed the clipped 1280 primary caption, verified expanded provenance and passed six reset/replay cycles. Runtime rows and objects settled at 226 and 9772, distinct from the saved counts of 210 and 9695.

Additive catalogue authoring preserved a manually edited saved translation, native label, font, colour and prefab override through authoring, save, reopen, Play, exit and saved reopen. Runtime preferences still apply their normal live style. The manual saved style remained intact after Play. Temporary probe edits were restored byte for byte. Final arrow-check reopen retained 14 roots, 9695 transforms, 210 rows, 54 bindings, 697 entries and no missing scripts. Input event processing, Editor preferences and SceneView repaint settings were restored to the observed Task 6 entry values.

The corrected full EditMode regression job completed 159 of 159 with no reported failures. Its detailed result was lost across the runner's automatic domain reload, so no skipped count or duration is inferred. The earlier detailed run had 150 passes, one test-fixture failure and eight explicit skips. That fixture was corrected without changing domain rules. The focused copy run retains ten passes, no failures or skips, in 4.7773019 seconds. Functional test success is separate from frame-time acceptance.

## Rendered performance

Hardware is the verified Intel Core i7-1255U / Iris Xe laptop, driver 32.0.101.5763, Windows 11. The engineering target is sustained 30 rendered FPS at 1920x1080 with warm p95 frame duration at or below 33.33 ms. Test durations, manually stepped time, CPU camera submission intervals and capture throughput do not establish this target.

| Observed condition | Mean FPS | p95 frame duration | Verdict |
| --- | ---: | ---: | --- |
| Early Editor with SceneView rendering | 22.211 | 51.265 ms | Failed |
| Isolated Editor with GPU pass instrumentation | 29.073 | 46.999 ms | Failed |
| Warm uncaptured courtyard, pass recorders disabled | 25.462 | 65.494 ms | Failed |
| Warm uncaptured normal marking | 32.668 | 47.788 ms | Failed |
| Warm uncaptured normal review packet | 36.490 | 37.783 ms | Failed |
| Final Editor/Gfx attribution sample | 30.43 | 41.1333 ms | Failed |

Supported camera callbacks verified the absence of SceneView rendering during isolated samples. Temporary repaint suppression was restored afterward. A matched original/two-shadow-cascade/half-resolution-SSAO comparison used three fixed views, each with three seconds of warmup and 12 uncaptured seconds. Neither alternative met the declared aggregate improvement threshold. Two cascades also softened courtyard shadow detail. No profile change was adopted; original profile bytes and exact skin fidelity remain intact. Still images do not establish temporal shimmer behavior.

The final supported attribution sample recorded GPU mean 31.23698 ms and p95 35.9065 ms, PlayerLoop mean 26.7271 ms and p95 33.0957 ms. EditorLoop and several wait recorders were zero-only and remain unverified, rather than evidence of zero cost. The remaining whole-frame versus PlayerLoop difference left standalone cost unresolved.

One authorized local Development Player was built from `e22dc15005b4f8e45ba6f4a6512bc0d9fe4ba03d` plus a recorded Unity-generated settings/resource serialization delta. It is a pre-arrow diagnostic. The build succeeded with zero errors and one retained warning. It was launched once in hidden batch mode with D3D11, without `-nographics`. The Player reported Iris Xe, 1920x1080 and PC quality, but supplied zero render-camera callbacks; framebuffer readback failed before any PNG or timing interval. It exited safely. Device initialization alone did not pass the rendering guard. No Player FPS is reported. Recorded build-created settings were restored, and generated performance metadata was preserved privately from the packed output. Any further validation method requires a separate concrete decision.

A separately authorized amendment made one scripts-only update of that same artifact, from `91f2f1d19eff2015a7da2cadb6974d11a7bb29b8`, followed by one normal-rendering launch on a newly created inactive Windows desktop. The update succeeded in 47.8209466 seconds with zero errors or warnings. All 15 packed scene/catalogue/resource data files were unchanged; the ten changed managed DLL/PDB files and original recoverable assemblies are recorded. Startup metadata changed; the original hash and complete current contents/hash are retained, but the exact original startup metadata diff is unavailable. The artifact retains pre-arrow saved catalogue data and later runtime code, so it is rendering evidence only.

All five original 1920x1080 captures were inspected: courtyard, marking before/after and review before/after. They show the actual saved world/actors, native UI and canonical Leena. The three planned warm 20-second intervals, with no capture during timing, measured:

| Isolated Player state | Mean FPS | p95 frame duration | GPU mean / p95, positive samples | Verdict |
| --- | ---: | ---: | ---: | --- |
| Courtyard | 32.5467 | 37.5936 ms | 28.8753 / 32.6816 ms | Failed |
| Normal marking | 37.6711 | 36.1625 ms | 23.8953 / 27.1233 ms | Failed |
| Normal review packet | 38.5970 | 32.4875 ms | 22.9788 / 26.7080 ms | Passed stated mean/p95 gate |

Both actual Game cameras rendered throughout: campus at 1920x1080 and Leena at 280x320. Typical draw/batch counts were 3227/2866, 1707/1681 and 936/843. Counter startup zeros and all raw values remain retained; zeros do not establish free rendering. Per-frame desktop-identity checks and measurement formatting allocate and contribute to measured CPU time. Aggregate allocation counters do not isolate product allocations or establish a GC cause. No further run or rendering change followed these results.

The child exited normally. Input desktop `Default` stayed unchanged through 418 launcher checks, and all owned handles closed. The owned desktop was subsequently confirmed absent. Current D3D11 rendering used ClientWorkerJobs after the requested SplitJobs mode was rejected. Inactive-desktop rendering does not establish physical display/compositor or keyboard/IME behavior. The required overall performance gate remains failed.

A read-only saved-state audit found 847 active MeshRenderers, 376 with batching flags, and substantial repeated mesh/material use. GPU Resident Drawer remains disabled; its project validator requires a shader-stripping setting change before it is supported. These are candidates for a later bounded diagnostic, not implemented optimizations. The original rendering profile, geometry, material sources and Unlimited skin remain unchanged.

## Scope and provenance

Rights and provenance remain those recorded in `AssetProductionLedger.md` and the existing source receipts. Dean supplied character references for this authorized build. Generated meshes/rigs and local derivatives retain provider and input provenance. The acquired Meshy walking source incorporates the action into the generated learner output and is distinct from a standalone motion library; its corrected runtime derivative retains a separate record. Raw third-party motion redistribution is not inferred from game-use permission; no Mixamo original was copied into this repository. Task6 makes no new acquisition or redistribution-rights determination.

The four SOURCE manifest entries are verified preparation documents, not runtime data copies or a connected service. The saved integration configuration stays disabled. Arabic, voice, live AI, SIE transport and Control Centre linking remain unavailable. Optional ambience is omitted. A working offline demonstration does not establish live semantic grading or learning transfer.

Detailed local evidence is retained under `LocalOnly/Validation/Acceptance`. It is intentionally not a portable Git artifact. Public source delivery, build and hosted execution remain separate from this local acceptance record.
