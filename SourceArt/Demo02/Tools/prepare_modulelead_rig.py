import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=ROOT/'SourceArt/Demo02/Characters/ModuleLead/ModuleLead_Rig_v001.blend'
ev=ROOT/'LocalOnly/Captures/Assets/ModuleLead/RigRest'
if dest.exists():raise RuntimeError('Preserve saved rig edits, use a new bounded version.')
ev.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.new('Demo02_ModuleLeadRig_v001');bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
bpy.ops.import_scene.gltf(filepath=str(ROOT/'SourceArt/Demo02/Characters/ModuleLead/Original/ModuleLead_MeshyRig_v001.glb'))
arm=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH' and o.parent==arm)
arm.name='ModuleLead_EditableRig';body.name='ModuleLead_WeightedMesh'
for a in bpy.data.actions:a.use_fake_user=True
arm.animation_data_clear()
for p in arm.pose.bones:p.matrix_basis=Matrix.Identity(4)
helpers=[o for o in scene.objects if o.type=='MESH' and o!=body]
for o in helpers:
    o.hide_render=True;o.hide_set(True);o['purpose']='Provider debug shape, excluded from production export'
# Imported joint tails are 100 times their intended length. Shorten along the same
# axis, retaining every head, roll, bind orientation and source skin weight.
bpy.context.view_layer.objects.active=arm;arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
tail_repairs=[]
for b in arm.data.edit_bones:
    before=b.length;b.tail=b.head+(b.tail-b.head)*.01
    if b.name=='head_end':b.length=2.0
    tail_repairs.append({'bone':b.name,'before_local':before,'after_local':b.length})
bpy.ops.object.mode_set(mode='OBJECT')
root=bpy.data.objects.new('ModuleLead_Root',None);scene.collection.objects.link(root)
arm.parent=root;root.scale=(1.65/1.7,)*3
bpy.context.view_layer.update()
bpy.context.view_layer.objects.active=body;bpy.ops.object.select_all(action='DESELECT');body.select_set(True)
if body.data.has_custom_normals:
    try:bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception:pass
bm=bmesh.new();bm.from_mesh(body.data);before=len(bm.verts)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.001)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update();bm.to_mesh(body.data);bm.free()
for p in body.data.polygons:p.use_smooth=True
g=body.vertex_groups.new(name='Trouser surface repair')
for v in body.data.vertices:
    p=body.matrix_world@v.co
    if .12<p.z<.88:g.add([v.index],max(0,min(1,(p.z-.12)/.05,(.88-p.z)/.05)),'REPLACE')
m=body.modifiers.new('Bounded trouser surface cleanup','SMOOTH');m.factor=.35;m.iterations=3;m.vertex_group=g.name
bpy.ops.object.modifier_move_up(modifier=m.name)
for img in bpy.data.images:
    if img.users:
        try:img.pack()
        except Exception:pass
scene.world=bpy.data.worlds.new('Rig inspection neutral');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.28,.33,.4,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
for name,pos,power,size in [('Key',(2,-3,3.5),260,3),('Fill',(-2,-1,2),120,2),('Rim',(1,2,2.5),190,2)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos
    o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
cam=bpy.data.objects.new('Rig inspection camera',bpy.data.cameras.new('Rig inspection camera'));scene.collection.objects.link(cam)
cam.data.type='ORTHO';cam.data.ortho_scale=2.15;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene['animation_gate']='No animation authored or admitted. Multi-angle rest and deformation audit required.'
arm.show_in_front=True;arm.data.display_type='OCTAHEDRAL'
report={'source':'Meshy cast rig, 24 bones','tail_repairs':tail_repairs,'vertices_before':before,'vertices_after':len(body.data.vertices),
    'target_height_m':1.65,'actions_preserved_but_unbound':[a.name for a in bpy.data.actions],
    'excluded_helpers':[o.name for o in helpers],'limitations':['No finger bones in provider rig. Precise pen/folio grips require editable finger authoring or an appropriate alternate rig.','No animation acceptance yet.'],
    'joints':{b.name:list(arm.matrix_world@b.head_local) for b in arm.data.bones}}
(ev/'rig-rest-audit.json').write_text(json.dumps(report,indent=2))
cam.location=(0,-4,.9);cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
for name,pos in [('front',(0,-4,.9)),('left',(-4,0,.9)),('back',(0,4,.9)),('right',(4,0,.9)),('threequarter',(2.8,-3,.95)),('rear_threequarter',(-2.8,3,.95))]:
    cam.location=pos;cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(ev/(name+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps({'saved':str(dest),'bones':len(arm.data.bones),'report':str(ev/'rig-rest-audit.json')}))
