import bpy,json
from pathlib import Path
from mathutils import Vector
r=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
d=r/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Mesh_v003.blend'
if d.exists():raise RuntimeError('Preserve source version.')
bpy.ops.wm.open_mainfile(filepath=str(r/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Mesh_v002.blend'))
scene=bpy.data.scenes['Demo02_LearnerMesh_v002'];scene.name='Demo02_LearnerMesh_v003';bpy.context.window.scene=scene
o=next(o for o in scene.objects if o.type=='MESH')
g=o.vertex_groups.new(name='Trouser surface repair')
for v in o.data.vertices:
    p=o.matrix_world@v.co
    if .12<p.z<.88:
        weight=min(1,(p.z-.12)/.05,(.88-p.z)/.05)
        g.add([v.index],max(0,weight),'REPLACE')
m=o.modifiers.new('Bounded trouser surface cleanup','SMOOTH');m.factor=.65;m.iterations=8;m.vertex_group=g.name
scene.camera.location=(2.8,-3,.95);scene.camera.rotation_euler=(Vector((0,0,.85))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(r/'LocalOnly/Captures/Assets/Learner/mesh_v003_threequarter.png')
bpy.ops.wm.save_as_mainfile(filepath=str(d));bpy.ops.render.render(write_still=True)
