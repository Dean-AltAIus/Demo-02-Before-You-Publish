"""Reuse inspected native pot and foliage in one editable full planter asset."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish');dest=R/'SourceArt/Demo02/Environment/FullPlanter_v002.blend';out=R/'SourceArt/Demo02/Exports/FullPlanter_v002'
if dest.exists() or out.exists():raise RuntimeError('Preserve existing planter source')
out.mkdir(parents=True);bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/FoliageKit_v002.blend'))
with bpy.data.libraries.load(str(R/'SourceArt/Demo02/Environment/CampusKit_v001.blend'),link=False) as (src,dst):dst.collections=['D02_Planter']
scene=bpy.data.scenes.new('Demo02_FullPlanter_v002');bpy.context.window.scene=scene;scene.collection.children.link(dst.collections[0])
old=next(o for o in dst.collections[0].all_objects if o.name=='Planter');old.location=(0,0,0)
crown=bpy.data.objects['PlanterCrown'];crown.location=(0,0,0)
root=bpy.data.objects.new('FullPlanter',None);scene.collection.objects.link(root);root['production_ids']='D02.ASSET.ENV.COURTYARD';root['source']='Original native planter base and inspected FoliageKit_v002 crown. Earlier sources retained.'
parts=[]
for original in [o for o in old.children if o.name.startswith(('Stone planter','Soil inset'))]+[o for o in crown.children_recursive if o.type=='MESH']:
 o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=root;o.matrix_local=original.matrix_local.copy();parts.append(o)
scene.collection.children.unlink(dst.collections[0]);bpy.context.view_layer.update()
scratch=bpy.data.scenes.new('Planter export');bpy.context.window.scene=scratch
for original in parts:
 o=original.copy();o.data=original.data.copy();scratch.collection.objects.link(o);o.parent=None;o.matrix_world=original.matrix_world.copy();o.select_set(True)
bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');bpy.ops.object.join();joined=bpy.context.object;joined.name='FullPlanter';scratch.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(joined.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(joined.data);bm.free()
bpy.ops.export_scene.fbx(filepath=str(out/'FullPlanter.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='AUTO')
payload={'assets':[{'name':'FullPlanter','native_mesh_parts':len(parts),'export_vertices':len(joined.data.vertices),'materials':[m.name for m in joined.data.materials]}],'materials':[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value} for m in joined.data.materials],'status':'Merged derivative preserves native source parts. Unity placement and visual acceptance pending.'}
bpy.data.scenes.remove(scratch);bpy.context.window.scene=scene
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.world=bpy.data.worlds.new('Planter neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
ld=bpy.data.lights.new('Soft source key','AREA');lo=bpy.data.objects.new('Soft source key',ld);scene.collection.objects.link(lo);lo.location=(-2,-3,5);ld.energy=650;ld.size=3;lo.rotation_euler=(Vector((0,0,.5))-lo.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Planter source camera');cam=bpy.data.objects.new('Planter source camera',cd);scene.collection.objects.link(cam);scene.camera=cam;cam.location=(2,-3,2);cam.rotation_euler=(Vector((0,0,.6))-cam.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=2
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.filepath=str(R/'LocalOnly/Captures/Environment/FullPlanter_v002.png')
(out/'manifest.json').write_text(json.dumps(payload,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(payload))
