# Demo 02 asset production ledger

Updated 25 October 2026. All entries below are development sources, not final game acceptance.

## Campus kit

Source: SourceArt/Demo02/Environment/CampusKit_v001.blend. Native Blender authoring, based on the supplied campus, studio, archive, review, folio and marking references. Twenty-one reusable asset roots, 542 editable mesh parts. Separate FBX export candidates and measured source bounds are in SourceArt/Demo02/Exports/CampusKit_v001/manifest.json. Source units are metres. Export uses -Z forward and Y up. Destructive modifier application is confined to exported derivatives.

The kit includes paving, stone wall, portal, arched window, awning, three work surfaces, academic chair, bookcase, cabinet with separate hinge/door, folio with separate cover, thin paper, pen, marking rail/token, source stand, tray, bench, planter, tree and lantern. Shared geometry/materials and source modifiers remain editable. Asset roots carry stable production item IDs. Builder refuses to overwrite an existing library.

SourceArt/Demo02/Environment/CampusAssembly_v001.blend is a saved source assembly with 175 kit placements over a 24x18 metre courtyard. Studio and review face inward from opposite sides; the archive faces the courtyard from the rear. campus-placements-v001.json records editable assembly positions and provisional interaction targets. The initial live Blender scene was preserved when the new library was added.

Observed source preview: three stations and clear central courtyard are visible. Current limitations: wall/portal end joins show dark seams, foliage needs denser and more coherent grouping, room/furniture dressing is sparse, and procedural oak needs an export texture bake. These must be corrected and inspected in the real Unity scene. Navigation clearance, camera, runtime performance and post-Play persistence have not yet been proven.

## Academic learner pilot

Input: the supplied academic-learner-tpose-v2.png, with exact front/back panel extraction for multiview input. Original source hash and extraction coordinates are in Characters/Learner/Reference/reference-provenance.json. No visual identity was regenerated before model generation. Model created with Meshy 7.1 through Magnific, 30,000 target faces, actual provider report 30,807 triangles. Original GLB is preserved with SHA-256 receipt.

Native mesh versions preserve inspection and repair history. v001 is the scaled imported candidate at 1.68 metres. v002 welds coincident split vertices, retains UV loops and recalculates smooth normals. v003 retains an editable bounded trouser smoothing modifier after visual inspection exposed a local surface defect. The source still needs final runtime fidelity checks, including face/hand close views and material import. No corrective generation was used.

The Meshy pilot rig is preserved separately. Working rig v001 fixes imported bone tails that were 100 times the intended length, retaining head positions and bind directions. Body source repair is repeated on the weighted derivative. Provider helper geometry is excluded from production export. Any supplied source action is retained unbound, not played or admitted as animation. The rig has 24 bones and no finger chains. Precise hand/pen/folio contacts require finger controls or a suitable alternate rig before animation acceptance.

Multi-angle mesh renders, geometry audits, rig/rest views and local repair logs are in ignored LocalOnly/Captures/Assets/Learner. A render receipt is not a visual pass. Rest posture and deformation still require explicit reviewed results before motion authoring. Other cast members remain unproduced pending this pilot gate.

## Rights and provenance

The character references were supplied by Dean as the design pack for this project. They are used for this authorized build. The generated mesh and rig are AI-generated derivatives, not manually sculpted originals. Magnific's paid subscription was active at generation. Its current terms assign the provider's output rights to subscribers, subject to the input rights and incorporated provider terms. Sources: [Magnific AI terms](https://www.magnific.com/legal/ai-products), [Meshy terms](https://www.meshy.ai/terms-of-use). No unrelated stock or third-party university screenshot is being shipped as art. Preserve these provenance links and original files with derivatives.

## Target and acceptance

Observed computer: Intel Core i7-1255U with Intel Iris Xe integrated graphics. Rendering budget must be measured in the completed Unity slice. No frame-rate or memory acceptance is claimed. Keep URP and current installed packages. No player or GitHub builds have been run for these asset operations.
