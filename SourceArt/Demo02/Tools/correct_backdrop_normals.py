"""Correct closed-surface winding exposed by Unity backface culling. Retain v002."""
import bpy,bmesh,json
from pathlib import Path
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Environment/AcademicBackdrop_v003.blend';exports=R/'SourceArt/Demo02/Exports/AcademicBackdrop_v003'
if dest.exists() or exports.exists():raise RuntimeError('Preserve corrected source outputs')
exports.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/AcademicBackdrop_v002.blend'))
s=bpy.data.scenes['Demo02_AcademicBackdrop_v002'];s.name='Demo02_AcademicBackdrop_v003';bpy.context.window.scene=s
root=s.objects['AcademicBackdrop'];parts=list(root.children)
fixed=0
for o in parts:
 bm=bmesh.new();bm.from_mesh(o.data)
 if bm.calc_volume(signed=True)<0:fixed+=1
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
deps=bpy.context.evaluated_depsgraph_get();copies=[]
for o in parts:
 data=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);copy=bpy.data.objects.new('Export '+o.name,data);s.collection.objects.link(copy);copy.matrix_world=o.matrix_world.copy();copies.append(copy)
bpy.ops.object.select_all(action='DESELECT')
for o in copies:o.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();derivative=bpy.context.object
# Export ground-space geometry rather than an arbitrary first-part join pivot.
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
derivative.name='AcademicBackdrop'
bpy.ops.export_scene.fbx(filepath=str(exports/'AcademicBackdrop.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
bpy.data.objects.remove(derivative,do_unlink=True)
manifest=json.loads((R/'SourceArt/Demo02/Exports/AcademicBackdrop_v002/manifest.json').read_text());manifest['correction']={'inverted_closed_source_meshes':fixed,'parts':len(parts),'export_pivot':'ground-space baked into derivative; native source part transforms preserved'};manifest['validation']='Unity backface-culling correction candidate; final placement capture pending'
(exports/'manifest.json').write_text(json.dumps(manifest,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(dest));print(json.dumps(manifest['correction']))
