"""Admit one source locomotion candidate onto the inspected editable rig.
Preserve the source take, remove only net horizontal travel for controller ownership,
and add a restrained relaxed finger pose. Rendering remains an inspection gate.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Motion_v001.blend'
if dest.exists():raise RuntimeError('Refuse existing motion source replacement')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Rig_v004.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;s.render.fps=30
a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh']
source_scene=bpy.data.scenes.new('Walking source preserved');source_scene.render.fps=30;bpy.context.window.scene=source_scene
bpy.ops.import_scene.gltf(filepath=str(R/'SourceArt/Demo02/Motion/Original/LearnerWalkingWoman_Meshy_v001.glb'))
source_arm=next(o for o in source_scene.objects if o.type=='ARMATURE');source_action=source_arm.animation_data.action;source_action.use_fake_user=True
differences={b.name:max(abs(b.matrix_local[i][j]-a.data.bones[b.name].matrix_local[i][j]) for i in range(4) for j in range(4)) for b in source_arm.data.bones}
if max(differences.values())>.001:raise RuntimeError('Source rest skeleton differs, explicit retargeting required: '+str(differences))
bpy.context.window.scene=s;a.animation_data_create();action=source_action.copy();action.name='D02_Learner_Walk_InPlace_v001';action.use_fake_user=True
a.animation_data.action=action;a.animation_data.action_slot=action.slots[0]
start,end=action.frame_range;s.frame_start=math.ceil(start);s.frame_end=round(end)
def frame(value):s.frame_set(int(value),subframe=value-int(value));bpy.context.view_layer.update()
frame(start);begin=a.matrix_world@a.pose.bones['Hips'].head
frame(end);finish=a.matrix_world@a.pose.bones['Hips'].head
travel=Vector((finish.x-begin.x,finish.y-begin.y,0))
chains={n:[Vector(p) for p in pts] for n,pts in json.loads((R/'LocalOnly/Captures/Assets/Learner/Fingers_v003/finger-rig-audit.json').read_text())['joint_world_positions'].items()}
for f in range(s.frame_start,s.frame_end+1):
 frame(f);t=(f-start)/(end-start);hip=a.pose.bones['Hips']
 delta=hip.bone.matrix_local.to_quaternion().inverted()@(a.matrix_world.inverted().to_3x3()@(travel*t))
 hip.location-=delta;hip.keyframe_insert(data_path='location',frame=f,group='Hips')
 for name,ch in chains.items():
  for i,angle in enumerate([8,12,8]):
   p=a.pose.bones[name+str(i+1)];direction=(ch[i+1]-ch[i]).normalized();axis=Vector((-direction.y,direction.x,0)).normalized();q=p.bone.matrix_local.to_quaternion()
   p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@Quaternion(axis,math.radians(angle))@q
   p.keyframe_insert(data_path='rotation_quaternion',frame=f,group=name)
# Grounding evidence uses the evaluated feet, without changing their animation.
foot_vertices={side:[v.index for v in body.data.vertices if (body.matrix_world@v.co).z<.16 and ((body.matrix_world@v.co).x>0)==(side=='Left')] for side in ['Left','Right']}
samples=[]
for f in range(s.frame_start,s.frame_end+1):
 frame(f);evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
 samples.append({'frame':f,'feet':{side:{'min_z':min((evaluated.matrix_world@mesh.vertices[i].co).z for i in inds),'ankle':list(a.matrix_world@a.pose.bones[side+'Foot'].head)} for side,inds in foot_vertices.items()}});evaluated.to_mesh_clear()
ev=R/'LocalOnly/Captures/Assets/Learner/Walk_v001';ev.mkdir(parents=True,exist_ok=True)
report={'source_action':source_action.name,'target_action':action.name,'frames':[start,end],'fps':30,'net_horizontal_travel_removed_m':list(travel),'rest_matrix_max_error':max(differences.values()),'samples':samples,'acceptance':'Inspection candidate, no Unity acceptance yet'}
(ev/'walk-audit.json').write_text(json.dumps(report,indent=2))
frame(s.frame_start)
s['motion_status']='Walking source candidate transferred to inspected rig, ground/contact inspection pending'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
# Inspection-only floor in the temporary process, excluded from source export.
bpy.ops.mesh.primitive_plane_add(size=10,location=(0,0,-.01));floor=bpy.context.object;floor.name='Inspection floor'
mat=bpy.data.materials.new('Inspection floor neutral');mat.diffuse_color=(.23,.27,.3,1);floor.data.materials.append(mat)
cam=s.camera;cam.data.ortho_scale=2.05;s.render.resolution_x=768;s.render.resolution_y=1024;s.cycles.samples=16
for f in [s.frame_start,8,16,23]:
 frame(f)
 for view,position in [('side',(-4,0,.95)),('threequarter',(2.8,-3,.95))]:
  cam.location=position;cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
  s.render.filepath=str(ev/(str(f).zfill(3)+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps({'saved':str(dest),'action':action.name,'report':str(ev/'walk-audit.json')}))
