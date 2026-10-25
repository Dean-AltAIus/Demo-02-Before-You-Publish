# Demo 02 asset production ledger

Updated 25 October 2026. All entries below are development sources, not final game acceptance.

## Campus kit

Source: SourceArt/Demo02/Environment/CampusKit_v001.blend. Native Blender authoring, based on the supplied campus, studio, archive, review, folio and marking references. Twenty-one reusable asset roots, 542 editable mesh parts. Separate FBX export candidates and measured source bounds are in SourceArt/Demo02/Exports/CampusKit_v001/manifest.json. Source units are metres. Export uses -Z forward and Y up. Destructive modifier application is confined to exported derivatives.

The kit includes paving, stone wall, portal, arched window, awning, three work surfaces, academic chair, bookcase, cabinet with separate hinge/door, folio with separate cover, thin paper, pen, marking rail/token, source stand, tray, bench, planter, tree and lantern. Shared geometry/materials and source modifiers remain editable. Asset roots carry stable production item IDs. Builder refuses to overwrite an existing library.

SourceArt/Demo02/Environment/CampusAssembly_v001.blend is a saved source assembly with 175 kit placements over a 24x18 metre courtyard. Studio and review face inward from opposite sides; the archive faces the courtyard from the rear. campus-placements-v001.json records editable assembly positions and provisional interaction targets. The initial live Blender scene was preserved when the new library was added.

Observed source preview: three stations and clear central courtyard are visible. Current limitations: wall/portal end joins show dark seams, foliage needs denser and more coherent grouping, room/furniture dressing is sparse, and procedural oak needs an export texture bake. These must be corrected and inspected in the real Unity scene. Navigation clearance, camera, runtime performance and post-Play persistence have not yet been proven.

Unity import: 21 FBX assets, 21 URP palette materials and 21 saved reusable prefabs now exist. Measured imported bounds match metre-scale source intent, including the asymmetric folio, door cabinet and 4m portal. Material remapping was explicit. Prefab creation preserves existing assets. Full scene rendering and collision clearance still need the gameplay assembly.

Material bake: WarmOak_v001 preserves the first cube-atlas experiment. Its empty UV regions are unsuitable for all kit shapes, so WarmOak_v002 is a full-UV color-only bake from the editable source nodes, with no lighting baked in. The full texture was inspected. Unity import remains pending. Actual campus Game-view capture now shows the authored layout, corrected facing signs and transparent Leena dock. Stone palette/lighting, sign contrast, backdrop, temporary learner and final dressing remain visual refinement items. The current scene has roughly 5,500 renderers; a frozen-frame statistics snapshot cannot establish runtime performance.

## Academic learner pilot

Input: the supplied academic-learner-tpose-v2.png, with exact front/back panel extraction for multiview input. Original source hash and extraction coordinates are in Characters/Learner/Reference/reference-provenance.json. No visual identity was regenerated before model generation. Model created with Meshy 7.1 through Magnific, 30,000 target faces, actual provider report 30,807 triangles. Original GLB is preserved with SHA-256 receipt.

Native mesh versions preserve inspection and repair history. v001 is the scaled imported candidate at 1.68 metres. v002 welds coincident split vertices, retains UV loops and recalculates smooth normals. v003 retains an editable bounded trouser smoothing modifier after visual inspection exposed a local surface defect. The source still needs final runtime fidelity checks, including face/hand close views and material import. No corrective generation was used.

The Meshy pilot rig is preserved separately. Working rig v001 fixes imported bone tails that were 100 times the intended length, retaining head positions and bind directions. Body source repair is repeated on the weighted derivative. Provider helper geometry is excluded from production export. Any supplied source action is retained unbound, not played or admitted as animation. The rig has 24 bones and no finger chains. Precise hand/pen/folio contacts require finger controls or a suitable alternate rig before animation acceptance.

Multi-angle mesh renders, geometry audits, rig/rest views and local repair logs are in ignored LocalOnly/Captures/Assets/Learner. Six rest views and front/side/three-quarter shoulder, reach and knee probes were explicitly inspected. Body posture and deformation are usable for the next authoring step, with no exploding skin or displaced hips/back. Rig v002 adds 30 editable finger bones. Static grip inspection exposed crossed finger influences and overlapping surface faces. v003 aligns chains with the actual separated digits; v004 removes two overlapping faces, locally subdivides the hands and relaxes weight discontinuities. The relaxed hand pose is usable for the locomotion candidate. Precise pen/folio contact remains unaccepted until its actual close views and runtime checks.

Locomotion: one compatible Meshy catalogue Walking_Woman source was acquired for 120 credits after the rest/deformation gate. The original source is preserved. Native Motion_v002 includes the locally corrected walk and a locally authored four-second breathing idle. Inspected walking frames 1, 8, 16 and 23 from side/three-quarter exposed and then corrected a 21 to 30mm support-foot float and slightly backward upper-body posture. The derivative grounds the support foot at 2mm and applies small spine/head corrections. Reference controller speed is 1.35m/s. Source view sampling is not normal-speed Unity playback acceptance. Library provenance: [Meshy animation catalogue](https://docs.meshy.ai/en/api/animation-library).

## Colleague and module lead

Both missing meshes were generated from the supplied exact front/back reference crops with Meshy 7.1, 1160 credits each, without retries. Original GLBs, hashes, input crop provenance and native metre-scale Blender sources are preserved. Both rigs cost 120 credits each and received local bone-tail, split-normal and bounded trouser-surface repairs. Six rest views and front/side/three-quarter static shoulder/reach/knee probes were inspected for each before authoring restrained native idle loops. Current idle shoulder folds are a visual refinement item, and neither rig yet has finger controls for precise prop contacts. These sources do not establish finished game art or animation quality.

Motion_v002 for both roles retains the native idle and adds a bounded editable pre-armature shoulder surface modifier plus eight local weight-continuity passes. Front, three-quarter and rear-three-quarter views were inspected: the original hard ridges are softer and body balance remains intact. Residual jacket shoulder seams still need actual Unity-framing review; this is a local repair, with no additional generation charge.

## Character export candidates

SourceArt/Demo02/Exports/Characters_v001 contains three FBX derivatives, three albedo maps and hashed source/export receipts. The learner contains 54 bones and separate Idle/Walk clips; the other cast roles contain 24 bones and a native Idle clip each. An isolated Blender FBX roundtrip verified expected 1.68m, 1.80m and 1.65m heights to within 0.000002m, material names and the intended clip counts/ranges. No source clips or helpers were silently substituted into the export. Unity Avatar/material/animation, post-Play persistence and final contact checks remain pending.

Characters_v002 repeats the learner derivative and exports the two locally refined NPC sources. Its independent FBX roundtrip again confirms the three expected heights, bone counts, one mesh per role and intended Idle/Walk ranges. Versioned sources and previous exports are preserved. This is the current candidate for a Unity import pilot.

Unity pilot: all three candidates now have URP materials, reusable saved prefabs, valid Generic Avatars and editable Animator controllers under Characters_v002. Generic animation retains the exact owned bone/finger curves without retargeting; navigation owns root displacement. The learner has separate 4-second Idle and 0.9667-second Walk clips; each NPC has its 4-second native Idle. CPU-skinned Unity samples confirm 1.68m, 1.80m and 1.65m idle heights, and the sampled learner walk keeps minimum foot clearance about 1.3 to 2mm. Front/side/back isolated Unity previews exist locally. These are import/pose checks, not normal-speed gameplay or contact acceptance. The first measurement mistakenly applied scale twice through BakeMesh; the corrected useScale=true audit is in UnityPilot_v002. In-scene lighting/material fidelity and final motion still need inspection.

Static contact development: native learner ContactRig_v001 to v004 preserve editable two-bone reach controls and finger poses. Inspection exposed right-wrist torsion; bounded pole-angle correction reduced the required wrist rotation to about 18 degrees and removed the visible skin twisting. A later tip alignment reached the provisional plane numerically, but visual inspection found the folio's origin is its hinge rather than its centre. Final prop-position/grip correction remains open. No interaction animation clips have been admitted from these candidates.

Existing local Mixamo gesture sources were considered read-only for reuse. No original Mixamo file was copied into this repository. The GitHub repository is public, so raw third-party motion redistribution must not be inferred from game-use permission. Current native idle loops use locally authored keyframes.

## Rights and provenance

The character references were supplied by Dean as the design pack for this project. They are used for this authorized build. The generated mesh and rig are AI-generated derivatives, not manually sculpted originals. Magnific's paid subscription was active at generation. Its current terms assign the provider's output rights to subscribers, subject to the input rights and incorporated provider terms. Sources: [Magnific AI terms](https://www.magnific.com/legal/ai-products), [Meshy terms](https://www.meshy.ai/terms-of-use). No unrelated stock or third-party university screenshot is being shipped as art. Preserve these provenance links and original files with derivatives.

## Target and acceptance

Observed computer: Intel Core i7-1255U with Intel Iris Xe integrated graphics. Rendering budget must be measured in the completed Unity slice. No frame-rate or memory acceptance is claimed. Keep URP and current installed packages. No player or GitHub builds have been run for these asset operations.
