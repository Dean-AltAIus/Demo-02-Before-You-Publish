"""Correct copied base transforms found by the close source inspection."""
import bpy,bmesh,json
from pathlib import Path
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish');dest=R/'SourceArt/Demo02/Environment/FullPlanter_v004.blend';out=R/'SourceArt/Demo02/Exports/FullPlanter_v004'
if dest.exists() or out.exists():raise RuntimeError('Preserve existing corrected planter')
out.mkdir(parents=True);bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/FullPlanter_v003.blend'))
s=bpy.data.scenes['Demo02_FullPlanter_v003'];s.name='Demo02_FullPlanter_v004';bpy.context.window.scene=s;root=bpy.data.objects['FullPlanter']
for o in root.children:
 if o.name.startswith('Stone planter'):
  o.location=(0,0,.28)
  for p in o.data.polygons:p.use_smooth=True
 if o.name.startswith('Soil inset'):o.location=(0,0,.565)
bpy.context.view_layer.update();parts=[o for o in root.children_recursive if o.type=='MESH'];scratch=bpy.data.scenes.new('Grounded planter export');bpy.context.window.scene=scratch
for src in parts:
 o=src.copy();o.data=src.data.copy();scratch.collection.objects.link(o);o.parent=None;o.matrix_world=src.matrix_world.copy();o.select_set(True)
bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');bpy.ops.object.join();joined=bpy.context.object;joined.name='FullPlanter';scratch.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(joined.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(joined.data);bm.free()
bpy.ops.export_scene.fbx(filepath=str(out/'FullPlanter.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='AUTO')
payload=json.loads((R/'SourceArt/Demo02/Exports/FullPlanter_v003/manifest.json').read_text());payload['status']='Corrected pot and soil transforms, grounded native source. Unity acceptance pending.'
payload['export_bounds']={'min':[min(v.co[i] for v in joined.data.vertices) for i in range(3)],'max':[max(v.co[i] for v in joined.data.vertices) for i in range(3)]};bpy.data.scenes.remove(scratch);bpy.context.window.scene=s;s.render.filepath=str(R/'LocalOnly/Captures/Environment/FullPlanter_v004.png');(out/'manifest.json').write_text(json.dumps(payload,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(payload['export_bounds']))
