# Editing and validating the current learner source

The editable master is `SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v023.blend`. Its production contract retains 29 named takes on a 30 fps timeline, with dense 240 Hz keys for the six M1 phases, fixed object roots and motor-owned world displacement. The six family sources retain independently editable props and tracks. Direct edits can be exported without rebuilding the master. The portable tools retain their original v013 default for reproducibility; select the current source explicitly as shown below. The actual composite runtime profile is `SourceArt/Demo02/Exports/ContactLibrary_v023/integration-profile.json`.

Use isolated Blender background execution. Both tools derive the repository root from their own location, preserve source bytes and refuse any existing output directory. Choose a new directory for each changed-source check. The examples below assume the shell is in the repository and Blender is already available.

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --disable-autoexec --offline-mode --threads 2 --python-exit-code 1 --python SourceArt/Demo02/Tools/reexport_learner_contact_library_v001.py -- --source SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v023.blend --takes FolioClose FolioCloseRecovery --bake-step 0.125 --output-dir LocalOnly/Validation/LearnerExport_001

& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --disable-autoexec --offline-mode --threads 2 --python-exit-code 1 --python SourceArt/Demo02/Tools/export_learner_skin_fixture_v001.py -- --source SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v023.blend --poses FolioClose:1.875 FolioClose:7.9375 FolioClose:8.0625 FolioCloseRecovery:4.0625 --output-dir LocalOnly/Validation/LearnerSkin_001
```

Either tool accepts `--source path/to/edited.blend`. The source must retain the named learner rig, mesh and current take contract. A changed rig, timing contract or production take set requires explicit profile/tool updates and renewed runtime admission. The tools change only their private in-memory Blender scene and new output files.

`--takes` restricts a bounded revision to named logical keys. Omitting it exports all 29 current takes. `--bake-step` may not be coarser than the saved source's required step. A dense source is retimed in memory to integer high-rate frames before FBX export, so subframe corrections are retained by the importer. `--poses Take:frame` accepts fractional native frames for actual Unity half-key surface comparisons. The original v013 command remains reproducible by explicitly selecting the v013 source and omitting these optional selections. Unchanged imported clips are retained by the current graph; exporting a full master does not authorize replacing all of them without renewed checks.

## Current-master FBX reexport

`reexport_learner_contact_library_v001.py` reads the saved master and the tracked current production profile at `SourceArt/Demo02/Exports/ContactLibrary_v013/receipt.json`. It admits the exact 29 current take names, validates their recorded frame ranges/durations and 54-bone rig, and checks the root, armature and mesh object transforms across every source frame. It selects only the character objects and production actions for FBX. Historical actions retained inside the editable master are excluded from the exported takes in memory. The native source is never saved by the tool.

The new folder contains the FBX and a receipt recording source/profile/export hashes, the selected takes, timing and root checks. A changed source hash is explicitly recorded as different from the original production pin. Preserve the existing Unity import settings, Generic rig mapping and saved exact-weight mesh derivatives when admitting a changed export. The standard FBX import alone previously rounded native skin weights.

## Full-surface and weight fixture

`export_learner_skin_fixture_v001.py` retains all saved geometry modifiers when evaluating armature REST, then captures seven complete posed surfaces: Idle at frame 1, FolioOpen at 8, FolioClose at 9, Passage at 10, PenPoint at 17, PenPointApproach at 19 and EditionSlide at 10. Its index retains the current Unity audit fields `rest.path`, `poses[].path`, `take`, `frame` and `seconds`. Each surface is headerless little-endian float32 XYZ in native world metres and stable native vertex order.

The same index records exact positive deform-bone weights, normalized per vertex with no cutoff. `weights.bin` stores an unsigned 8-bit influence count for each native vertex, followed by unsigned 16-bit bone index and float32 weight pairs, little endian, descending weight. `weights.bone_names` supplies the index mapping. This is source comparison data; the tracked `export_native_skin_weights.py` package format and saved Unity mesh authorer remain the engine-admission path.

The current learner has 24,179 native vertices. Exactly ten are wire-only and used by no face: 4820,4970,18272,18273,18274,18275,20510,20511,20878,20879. They are preserved in the editable source and explicitly listed in the fixture. All 24,169 face-referenced native vertices remain mandatory for rendered-surface mapping. The fixture validates that face membership remains unchanged across its sampled poses.

## Validation of these tools

Executed against unchanged source SHA256 `a71f315a3da967ec93a3ec037de06f9b31856b150a14677a9406ada33cb523a1`:

- FBX reexport selected 29 takes and checked 619 source frames with zero object-root matrix drift.
- Independent Blender reimport of the recorded FBX and new export matched all 29 ranges, all 54 world bone matrices across 619 frames, all 24,179 mesh coordinates and 48,237 face index lists. Maximum matrix-element and mesh-coordinate differences were zero. This compares the two exports; it is separate from native-to-Unity skin acceptance.
- Evaluated rest plus all seven pose binaries matched the earlier native comparison fixtures byte for byte. Exact normalized weight bytes and bone ordering also matched the tracked current weight package. Maximum native influence count is 10.
- Both existing-output guards refused a retry before opening a native file. All twelve files across the two output folders retained their previous hashes.

The Blender inspection harness initially failed to find two long imported action names by their full suffix. Its imported datablocks use the existing abbreviated names `D02_Learner_EditionSlideApp_b3ecdf8` and `D02_Learner_EditionSlideRec_b409dd5`. The comparison then reconciled these aliases explicitly and verified all ranges/matrices. Current Unity metadata retains the full expected take names; this inspection issue is not a demonstrated Unity runtime defect.

Detailed local evidence is under `LocalOnly/Validation/Production/LearnerPortableTools_v001`: `fbx-roundtrip-comparison.json`, `fixture-comparison.json` and `preservation-guards.json`. Existing native masters, production FBX files, Unity assets and scenes were preserved. Admission after source changes still requires actual Unity deformation, contact, interruption and playback checks.
