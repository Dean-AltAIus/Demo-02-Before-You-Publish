"""Local posture/grounding corrections and an authored breathing idle.
The controller owns displacement. Original imported motion stays preserved.
"""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Motion_v002.blend'
if dest.exists():raise RuntimeError('Preserve existing motion source')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Motion_v001.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh']
walk=a.animation_data.action.copy();walk.name='D02_Learner_Walk_v002';walk.use_fake_user=True;a.animation_data.action=walk;a.animation_data.action_slot=walk.slots[0]
def rotation(name,axis,angle,replace=False):
 p=a.pose.bones[name];q=p.bone.matrix_local.to_quaternion();r=q.inverted()@Quaternion(Vector(axis),math.radians(angle))@q;p.rotation_mode='QUATERNION'
 p.rotation_quaternion=r if replace else r@p.rotation_quaternion
def frame(f):s.frame_set(f);bpy.context.view_layer.update()
corrections=[]
for f in range(1,31):
 frame(f)
 for name,angle in [('Spine02',2),('Spine01',1),('Head',3)]:
  rotation(name,(1,0,0),angle);a.pose.bones[name].keyframe_insert(data_path='rotation_quaternion',frame=f,group=name)
 bpy.context.view_layer.update();evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
 z=min((evaluated.matrix_world@v.co).z for v in mesh.vertices);evaluated.to_mesh_clear()
 hip=a.pose.bones['Hips'];delta=hip.bone.matrix_local.to_quaternion().inverted()@(a.matrix_world.inverted().to_3x3()@Vector((0,0,z-.002)))
 hip.location-=delta;hip.keyframe_insert(data_path='location',frame=f,group='Hips');corrections.append({'frame':f,'lowered_m':z-.002})
# Editable low-amplitude idle is locally authored, with a closed 4s loop.
idle=bpy.data.actions.new('D02_Learner_Idle_v001');idle.use_fake_user=True;a.animation_data.action=idle
chains={n:[Vector(p) for p in pts] for n,pts in json.loads((R/'LocalOnly/Captures/Assets/Learner/Fingers_v003/finger-rig-audit.json').read_text())['joint_world_positions'].items()}
for f in [1,31,61,91,121]:
 for p in a.pose.bones:p.matrix_basis=Matrix.Identity(4)
 phase=(f-1)/120*math.tau
 for name,axis,angle in [('LeftArm',(0,1,0),80),('RightArm',(0,1,0),-80),('LeftForeArm',(0,0,1),-8),('RightForeArm',(0,0,1),8),('Spine01',(1,0,0),.35*math.sin(phase)),('Head',(0,0,1),.7*math.sin(phase))]:rotation(name,axis,angle,True)
 for name,ch in chains.items():
  for i,angle in enumerate([8,12,8]):
   d=(ch[i+1]-ch[i]).normalized();rotation(name+str(i+1),(-d.y,d.x,0),angle,True)
 for p in a.pose.bones:
  p.rotation_mode='QUATERNION';p.keyframe_insert(data_path='rotation_quaternion',frame=f,group=p.name);p.keyframe_insert(data_path='location',frame=f,group=p.name);p.keyframe_insert(data_path='scale',frame=f,group=p.name)
a.animation_data.action=walk;a.animation_data.action_slot=walk.slots[0];s.frame_start=1;s.frame_end=30;frame(1)
s['motion_status']='Locomotion candidate v002: grounded support foot, reduced backwards posture; Unity and full-speed contact checks pending'
ev=R/'LocalOnly/Captures/Assets/Learner/Walk_v002';ev.mkdir(parents=True,exist_ok=True)
report={'walk':walk.name,'idle':idle.name,'fps':30,'controller_reference_speed_m_s':1.35,'grounding_corrections':corrections,'posture_pitch_degrees':{'Spine02':2,'Spine01':1,'Head':3},'acceptance':'Candidate, actual source and Unity playback still required'}
(ev/'corrections.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(dest))
bpy.ops.mesh.primitive_plane_add(size=10,location=(0,0,0));floor=bpy.context.object;floor.name='Temporary inspection floor'
mat=bpy.data.materials.new('Inspection floor');mat.diffuse_color=(.23,.27,.3,1);floor.data.materials.append(mat)
cam=s.camera;cam.data.ortho_scale=2.05;s.render.resolution_x=768;s.render.resolution_y=1024;s.cycles.samples=16
for clip,frames in [(walk,[1,8,16,23]),(idle,[1])]:
 a.animation_data.action=clip;a.animation_data.action_slot=clip.slots[0]
 for f in frames:
  frame(f)
  for view,pos in [('side',(-4,0,.95)),('threequarter',(2.8,-3,.95))]:
   cam.location=pos;cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(('idle' if clip==idle else 'walk')+'_'+str(f).zfill(3)+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps(report))
