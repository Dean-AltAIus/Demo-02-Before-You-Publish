# Bounded Unity authoring batch

These are the actual editor-side import and placement recipes used for the bulk campus update. Run only in the assigned Demo 2 Editor, outside Play, through the existing execute-code bridge. The C# files are method bodies, not runtime components. The import JSON contains the execute-code arguments.

The recipes preflight output paths or exact existing authored objects. They intentionally refuse repeated application once assets are present. They preserve previous campus instances inactive and preserve current transforms. They are not general regeneration commands. Native Blender sources and export manifests remain in SourceArt/Demo02.

Order: import campus assets, place bounded replacements, place academic details, import canonical Leena, fit the archive sign/canopy and inward banners after actual visual inspection. Between scene batches, a dirty scene was saved to a local comparison copy and compared byte for byte with the existing saved scene; only identical content was cleared. Never discard nonidentical unsaved content.

Validation: actual campus capture v007, four clear arrival capsules, saved references and zero missing scripts. Leena imports as a reusable prefab with three existing attention clips; HUD staging and final motion acceptance are later work. No player build or external integration.
