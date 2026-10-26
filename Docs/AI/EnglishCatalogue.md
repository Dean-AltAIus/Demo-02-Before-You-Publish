# Editable English catalogue

`Assets/Demo02/strings/STR_EN.asset` is the native, Inspector-editable English string table. The saved campus contains one `EnglishCatalogueBinding`, 54 static label references and the Workbench catalogue reference. Static interface copy resolves through stable keys. Native controls, source IDs and presenter commands keep their own stable identities.

Edit an entry's English value in the Inspector. Keep named slots such as `{task}`, `{rubric}` and `{reason}` intact. Slots insert literal values once. Learner text, admitted passages, versions, units, reasons and receipt identifiers are not normalized or interpreted as markup. Rich text is disabled on payload-bearing native fields and text. Numeric, time, outcome and prose fields have separate empty-state prompts.

The catalogue covers interface and offline presentation copy. It does not replace the saved editable presenter sequence, import the private reference pack, change domain decisions or add a translation service. English is the admitted interface language; Arabic and voice remain unavailable.

For saved label previews, select the binding and explicitly choose **Refresh bound English labels**. This updates bound text, without regenerating layout. Play installs the catalogue and resolves bound labels. Runtime text changes do not overwrite saved scene labels. Geometry, styles and prefab overrides remain ordinary authored Unity content.

`EnglishCatalogueAuthoring.ApplyMissing(path)` accepts an explicit JSON file with an `Entries` array of `Key` and `English` values. It requires the clean saved campus outside Play. It adds absent keys and missing binding references only, preserves existing translations and labels, and saves the table and scene. It does not refresh existing label text or replace the scene. Deliberate copy revisions therefore edit the existing catalogue entry explicitly. Missing or duplicate keys fail visibly instead of silently substituting different text.

Review and command receipts retain the original typed request and document pair. Primary copy explains requested, current, historical, returned, approved, released or held state. Exact IDs and digests remain available in expandable read-only details. Scheduling acceptance does not grant publication approval. Authored presenter decisions disclose their exact synthetic scope; ordinary local decisions retain their requested-pair scope.

Validation and scope limits are recorded in `IntegratedAcceptance.md` and its production matrix. The catalogue is editable source content, not evidence of live semantic assessment.
