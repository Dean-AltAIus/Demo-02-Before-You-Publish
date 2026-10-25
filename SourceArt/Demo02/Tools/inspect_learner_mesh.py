import bpy, json, bmesh
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
src=ROOT/'SourceArt/Demo02/Characters/Learner/Original/AcademicLearner_Meshy71_v001.glb'
dest=ROOT/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Mesh_v001.blend'
evidence=ROOT/'LocalOnly/Captures/Assets/Learner'
evidence.mkdir(parents=True,exist_ok=True)
if dest.exists():raise RuntimeError('Source exists. Use bounded edits and versioning.')
scene=bpy.data.scenes.new('Demo02_LearnerMesh_v001');bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
bpy.ops.import_scene.gltf(filepath=str(src))
meshes=[o for o in scene.objects if o.type=='MESH']
points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
height=hi.z-lo.z;scale=1.68/height
center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
normalise=Matrix.Scale(scale,4)@Matrix.Translation(-center)
for o in meshes:
    o.matrix_world=normalise@o.matrix_world
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.name='AcademicLearner_EditableMesh'
    o['source_creation']='rgcvVHWxtc';o['target_height_m']=1.68;o['status']='Unrigged mesh inspection candidate'
for img in bpy.data.images:
    if img.users and img.source=='FILE':
        try:img.pack()
        except Exception:pass
scene.world=bpy.data.worlds.new('Learner neutral world');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.28,.33,.4,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=768;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
def light(name,loc,power,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc
    o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
light('Key',(2,-3,3.5),260,3);light('Fill',(-2,-1,2),120,2);light('Rim',(1,2,2.5),190,2)
cam=bpy.data.objects.new('Inspection camera',bpy.data.cameras.new('Inspection camera'));scene.collection.objects.link(cam)
cam.data.type='ORTHO';cam.data.ortho_scale=1.98;scene.camera=cam
cam.location=(0,-4,.9);cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
report={'source_height_before':height,'normalisation_scale':scale,'height_m':1.68,'meshes':[],'rig':'not yet authored','animations':'none'}
for o in meshes:
    bm=bmesh.new();bm.from_mesh(o.data)
    report['meshes'].append({'name':o.name,'vertices':len(bm.verts),'faces':len(bm.faces),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'loose_vertices':sum(not v.link_edges for v in bm.verts),'uv_layers':list(o.data.uv_layers.keys()),'materials':[m.name for m in o.data.materials]});bm.free()
(evidence/'mesh-audit.json').write_text(json.dumps(report,indent=2))
bpy.data.libraries.write(str(dest),{scene},path_remap='RELATIVE_ALL',fake_user=True,compress=True)
for name,pos in [('front',(0,-4,.9)),('left',(-4,0,.9)),('back',(0,4,.9)),('right',(4,0,.9)),('threequarter',(2.8,-3,.95)),('rear_threequarter',(-2.8,3,.95))]:
    cam.location=pos;cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(evidence/('mesh_'+name+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps({'saved':str(dest),'audit':report,'renders':str(evidence)}))
