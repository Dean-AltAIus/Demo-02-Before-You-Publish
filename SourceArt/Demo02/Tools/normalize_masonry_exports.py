"""Ground pivots and bake derivative transforms; native source stays editable."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish');out=R/'SourceArt/Demo02/Exports/MasonryKit_v003'
if out.exists():raise RuntimeError('Preserve existing normalized exports')
out.mkdir(parents=True);bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/MasonryKit_v002.blend'))
s=bpy.data.scenes['Demo02_Masonry_v002'];bpy.context.window.scene=s
manifest=json.loads((R/'SourceArt/Demo02/Exports/MasonryKit_v002/manifest.json').read_text());manifest['source']='SourceArt/Demo02/Environment/MasonryKit_v002.blend';manifest['export_revision']='v003: identity transforms and exact ground pivots';report={}
for item in manifest['assets']:
 name=item['name'];asset=bpy.data.objects[name];saved=asset.location.copy();asset.location=(0,0,0);bpy.context.view_layer.update()
 preview=bpy.data.scenes.new('Normalize '+name);bpy.context.window.scene=preview;copies=[]
 for original in asset.children_recursive:
  if original.type!='MESH':continue
  o=original.copy();o.data=original.data.copy();preview.collection.objects.link(o);o.parent=None;o.matrix_world=original.matrix_world.copy();copies.append(o)
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();joined=bpy.context.object;joined.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 bm=bmesh.new();bm.from_mesh(joined.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(joined.data);bm.free()
 points=[joined.matrix_world@v.co for v in joined.data.vertices];report[name]={'min':[min(v[i] for v in points) for i in range(3)],'max':[max(v[i] for v in points) for i in range(3)],'rotation':list(joined.rotation_euler),'scale':list(joined.scale)}
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='AUTO')
 bpy.data.scenes.remove(preview);bpy.context.window.scene=s;asset.location=saved;bpy.context.view_layer.update()
manifest['exact_export_bounds']=report;(out/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(report))
