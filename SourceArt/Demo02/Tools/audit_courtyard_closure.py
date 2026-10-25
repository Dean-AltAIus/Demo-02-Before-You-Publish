"""Read-only source reopen and FBX bound audit in an isolated Blender worker."""
import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
data=json.loads((R/'SourceArt/Demo02/Exports/CourtyardClosure_v001/manifest.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/CourtyardClosure_v001.blend'))
s=bpy.data.scenes['Demo02_CourtyardClosure_v001'];bpy.context.window.scene=s;rows=[]
for item in data['assets']:
 root=s.objects[item['name']];root.location=(0,0,0);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();points=[];mods=0
 for ob in root.children_recursive:
  if ob.type!='MESH':continue
  mods+=len(ob.modifiers);evaluated=ob.evaluated_get(dg);me=evaluated.to_mesh();points.extend(evaluated.matrix_world@v.co for v in me.vertices);evaluated.to_mesh_clear()
 source=[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]]
 preview=bpy.data.scenes.new('Audit '+item['name']);bpy.context.window.scene=preview
 bpy.ops.import_scene.fbx(filepath=str(R/'SourceArt/Demo02/Exports/CourtyardClosure_v001'/(item['name']+'.fbx')))
 exported=[ob.matrix_world@v.co for ob in preview.objects if ob.type=='MESH' for v in ob.data.vertices]
 actual=[[min(p[i] for p in exported) for i in range(3)],[max(p[i] for p in exported) for i in range(3)]]
 error=max(abs(source[k][i]-actual[k][i]) for k in range(2) for i in range(3))
 row={'name':item['name'],'editable_parts':len(root.children_recursive),'editable_modifiers':mods,'bound_error_m':error,'source_bounds':source,'export_bounds':actual,'pass':error<.00001};rows.append(row)
 if not row['pass']:raise RuntimeError(str(row))
 bpy.data.scenes.remove(preview);bpy.context.window.scene=s
result={'status':'Native source reopened; five FBX exports match evaluated native bounds. This does not replace Unity visual or runtime acceptance.','assets':rows}
(R/'LocalOnly/Validation/Production/World360/native-closure-audit.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
