import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
r=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
s=r/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Mesh_v001.blend'
d=r/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Mesh_v002.blend'
ev=r/'LocalOnly/Captures/Assets/Learner'
if d.exists():raise RuntimeError('Version exists, preserve edits.')
bpy.ops.wm.open_mainfile(filepath=str(s))
scene=bpy.data.scenes['Demo02_LearnerMesh_v001'];bpy.context.window.scene=scene
o=next(o for o in scene.objects if o.type=='MESH');bpy.context.view_layer.objects.active=o;o.select_set(True)
report={'operation':'Weld coincident split vertices at 0.00001 m, preserve UV loops, recalculate smooth source normals','before_vertices':len(o.data.vertices),'before_smooth_faces':sum(p.use_smooth for p in o.data.polygons)}
if o.data.has_custom_normals:
    try:bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception as e:report['custom_normals_clear_note']=str(e)
bm=bmesh.new();bm.from_mesh(o.data)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
report['after_vertices']=len(bm.verts);report['nonmanifold_edges']=sum(not e.is_manifold for e in bm.edges)
bm.to_mesh(o.data);bm.free()
for p in o.data.polygons:p.use_smooth=True
o.data.update();o['status']='Locally repaired unrigged candidate. Needs rig and runtime validation.'
scene.name='Demo02_LearnerMesh_v002'
scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.cycles.samples=24
cam=scene.camera;cam.data.ortho_scale=1.98
bpy.ops.wm.save_as_mainfile(filepath=str(d))
(ev/'mesh-refinement.json').write_text(json.dumps(report,indent=2))
for name,pos in [('front',(0,-4,.9)),('threequarter',(2.8,-3,.95))]:
    cam.location=pos;cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(ev/('mesh_v002_'+name+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps(report))
