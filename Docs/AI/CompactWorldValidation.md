# Compact courtyard production checkpoint

Dean's final direction is a coherent, finished small world with purposeful open space. One restrained surrounding context layer is acceptable. There must be no stacked distant scenery or additions made merely to fill the view.

## Saved changes

- CourtyardClosure_v001 is an editable native Blender source with five reusable exported Unity prefabs. The saved campus uses 40 garden-wall modules, four corner posts, one entrance gate, two closed hipped roofs and four correctly sized front window bays. Six superseded boundary/window objects remain inactive and editable.
- The existing single academic backdrop and existing trees remain. No distant building or landscape layer was added. The playable footprint remains 24 by 18 metres and functional station anchors are unchanged.
- Exposed reverse wall surfaces now use the existing closure plaster material instead of a dark mortar core. Limestone, cornices and geometry remain editable.
- The hanging-banner prefab and tree canopy prefab now have simple camera collision envelopes. Actual camera renders had shown clipping through both surfaces before correction.
- World TextMesh labels use a depth-tested URP material and a dynamic-font-atlas binding. Twelve saved scene labels and two reusable marking-lane labels are covered. Native overlay UI is unchanged. Labels no longer show through walls or appear backwards through their opaque sign backs.
- A close-wall camera defect is corrected: the collision distance is no longer forced to a minimum half metre, which could place the camera beyond the wall it had just hit.

## Evidence

LocalOnly/Validation/Production/World360 contains baseline, intermediate and corrected actual Unity camera renders. The baseline exposed empty unframed edges, missing roofs, tree/banner clipping and text through walls. Reviewed corrected coverage includes an eight-direction centre orbit, station and archive-rear views, all four corners at minimum and maximum camera pitch, and all four authored inspection cameras. These captures use temporary offscreen cameras without moving the saved actor/camera or taking desktop focus. They are camera-only evidence and omit overlay UI.

The five native assets reopened successfully in an isolated Blender process. Their FBX bounds match evaluated editable source bounds within 0.000001 metre. The 51 placed pieces total 74,916 mesh triangles before subtraction of superseded windows; each has one renderer with shared materials. They reuse the existing limestone textures.

The saved-scene audit reports 10 roots, 9,407 total active/inactive objects, 827 active renderers, three active skinned cast renderers, 1,636,787 active mesh triangles, 1,267 material slots, zero missing scripts and twelve depth-bound labels. These are inventory figures, not frame-rate claims. All four arrival capsules are clear. Dense outward perimeter rays at three heights produce zero misses in 2,478 checks.

The final unfiltered EditMode job eb4d4661d7674180a083e48ed8d067fc passed 126 of 126 tests with no skips or failures. Two cases test actual camera clearance against near and ordinary-distance walls. The earlier class-name test_names filter matched zero tests and is not counted as a RED or passing run. The pre-fix native probe independently recorded a 0.10098-metre obstruction hit being overridden by the old 0.5-metre minimum distance.

The scene reopened cleanly with unchanged saved bytes, one player motor and one follow camera. A historical MCP websocket initialization warning occurred around script reload; no C# or shader compile error was reported. Unity's test-induced TimeManager serialization rewrite was restored to its unchanged pre-test value. No player build, push or hosted workflow was run.

## Remaining acceptance

This checkpoint closes broad environment gaps. It is not final art, animation, runtime or performance acceptance. Task 5 must refine physical table labels/props, reference UI art, persistent 3D Leena staging, character gestures and contacts, and inspect these assets during actual gameplay. In particular, current static table status text and rails still look suspended in space in inspection views. Retain their actual state bindings while giving them a deliberate physical/readable composition.

Task 6 must measure the real target hardware, optimize demonstrated costs, check close-boundary avatar/camera visibility during movement, run the full presenter and independent learner paths, and verify post-Play persistence. The 1.64-million-triangle/1,267-slot inventory is an explicit profiling input, not proof that this meets the user's lightweight requirement. No completion claim follows from static renders or unit tests alone.
