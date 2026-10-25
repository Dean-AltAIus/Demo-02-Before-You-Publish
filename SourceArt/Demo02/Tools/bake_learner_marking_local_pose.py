"""Bake one contact-proven native marking pilot; keep source IK rig and owned takes."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Motion_v004.blend'
if dest.exists():raise RuntimeError('Preserve existing motion authoring')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v008.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;s.render.fps=30
a=bpy.data.objects['AcademicLearner_EditableRig'];pen=bpy.data.objects['AnnotationPen'];right=a.pose.bones['RightHand'];wrist=bpy.data.objects['Right wrist contact']
for action in bpy.data.actions:action.use_fake_user=True
bpy.context.view_layer.update();base_target=wrist.location.copy();handrot=right.matrix.to_quaternion();base_tip=pen.matrix_world@Vector((0,0,-.012))
pen_hand=(a.matrix_world@right.matrix).inverted()@pen.matrix_world
base_basis={p.name:p.matrix_basis.copy() for p in a.pose.bones}
def ease(t):return t*t*(3-2*t)
def pose(offset):
 wrist.location=base_target+offset;bpy.context.view_layer.update()
 right.matrix=Matrix.Translation(right.head)@handrot.to_matrix().to_4x4();bpy.context.view_layer.update()
 evaluated=a.evaluated_get(bpy.context.evaluated_depsgraph_get())
 return {p.name:p.matrix.copy() for p in evaluated.pose.bones}
hover=pose(Vector((0,0,.08)));samples={}
for frame in range(1,9):samples.setdefault('PenApproach',[]).append((frame,pose(Vector((0,0,.08*(1-ease((frame-1)/7)))))))
for frame in range(1,29):
 t=(frame-1)/27
 if t<.70:offset=Vector((.007*math.sin(t/.70*math.pi),-.004*math.sin(t/.70*math.tau),0))
 else:offset=Vector((0,0,.08*ease((t-.70)/.30)))
 samples.setdefault('MarkPoint',[]).append((frame,pose(offset)))
# Keep the IK controls editable in the preceding source and in this scene, muted for baked clips.
for p in a.pose.bones:
 for constraint in p.constraints:constraint.mute=True
a.animation_data_create();a.animation_data.action=None
ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive))
def set_matrices(matrices):
 # Use the intended parent pose, never the previous evaluation's parent matrix.
 for p in ordered:
  kwargs={'parent_matrix':matrices[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
  p.matrix_basis=p.bone.convert_local_to_pose(matrices[p.name],p.bone.matrix_local,invert=True,**kwargs)
 bpy.context.view_layer.update()
idle=bpy.data.actions['D02_Learner_Idle_v001'];a.animation_data.action=idle;a.animation_data.action_slot=idle.slots[0];s.frame_set(1);bpy.context.view_layer.update();idle_matrices={p.name:p.matrix.copy() for p in a.pose.bones}
a.animation_data.action=None
# A separately editable arrival stance supplies the longer body preparation before the short tip approach.
samples['TableReady']=[]
def local_pose(matrices,p):
 kwargs={'parent_matrix':matrices[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
 return p.bone.convert_local_to_pose(matrices[p.name],p.bone.matrix_local,invert=True,**kwargs)
idle_local={p.name:local_pose(idle_matrices,p) for p in ordered};hover_local={p.name:local_pose(hover,p) for p in ordered}
for frame in range(1,20):
 t=ease((frame-1)/18);matrices={}
 for p in ordered:
  l0,q0,sc0=idle_local[p.name].decompose();l1,q1,sc1=hover_local[p.name].decompose()
  local=Matrix.LocRotScale(l0.lerp(l1,t),q0.slerp(q1,t),sc0.lerp(sc1,t))
  kwargs={'parent_matrix':matrices[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
  matrices[p.name]=p.bone.convert_local_to_pose(local,p.bone.matrix_local,**kwargs)
 samples['TableReady'].append((frame,matrices))
records=[];bake_checks=[]
for name,frames in samples.items():
 action=bpy.data.actions.new('D02_Learner_'+name+'_v001');action.use_fake_user=True;a.animation_data.action=action
 for frame,matrices in frames:
  set_matrices(matrices)
  for p in ordered:
   p.rotation_mode='QUATERNION'
   for channel in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=channel,frame=frame,group=p.name)
 a.animation_data.action_slot=action.slots[0]
 for frame,matrices in frames:
  s.frame_set(frame);bpy.context.view_layer.update()
  errors=sorted([(((a.matrix_world@a.pose.bones[bone_name].matrix).translation-(a.matrix_world@matrix).translation).length,bone_name) for bone_name,matrix in matrices.items()],reverse=True)
  error=errors[0][0]
  bake_checks.append({'clip':name,'frame':frame,'maximum_bone_position_error_metres':error})
  if error>.001:raise RuntimeError('Baked pose differs at '+name+' frame '+str(frame)+' : '+str(errors[:8])+' object_scale '+str(list(a.scale)))
 records.append({'name':action.name,'frames':[frames[0][0],frames[-1][0]],'fps':30,'source':'Native IK-solved keyframes, editable baked action','root_motion':'Fixed body root; movement owns world displacement'})
# Retain the prop as a visible native attachment in the pilot scene.
action=bpy.data.actions['D02_Learner_PenApproach_v001'];a.animation_data.action=action;a.animation_data.action_slot=action.slots[0];s.frame_start=1;s.frame_end=8;s.frame_set(8);bpy.context.view_layer.update()
pen.parent=a;pen.parent_type='BONE';pen.parent_bone='RightHand';pen.matrix_world=a.matrix_world@a.pose.bones['RightHand'].matrix@pen_hand;bpy.context.view_layer.update()
s['motion_gate']='Static v008 hand, side and oblique views inspected before this pilot. Contact samples and normal-speed playback still require validation. Literal marks remain UI text, no handwriting geometry.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
ev=R/'LocalOnly/Captures/Assets/Learner/Marking_v002';ev.mkdir(parents=True,exist_ok=True)
s.cycles.samples=12;s.render.threads_mode='FIXED';s.render.threads=4;s.render.resolution_x=640;s.render.resolution_y=800;cam=s.camera;checks=[]
for name,frame in [('TableReady',1),('TableReady',10),('TableReady',19),('PenApproach',1),('PenApproach',8),('MarkPoint',10),('MarkPoint',28)]:
 act=bpy.data.actions['D02_Learner_'+name+'_v001'];a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];s.frame_set(frame);bpy.context.view_layer.update()
 expected=a.matrix_world@a.pose.bones['RightHand'].matrix@pen_hand;tip=expected@Vector((0,0,-.012));actual=pen.matrix_world@Vector((0,0,-.012));checks.append({'clip':name,'frame':frame,'tip':list(tip),'attachment_tip_error_m':(tip-actual).length})
 cam.data.ortho_scale=2.05;cam.location=(2.8,-3,1.5);cam.rotation_euler=(Vector((0,-.25,.9))-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(name+'_'+str(frame)+'.png'));bpy.ops.render.render(write_still=True)
(ev/'source-receipt.json').write_text(json.dumps({'actions':records,'bake_checks':bake_checks,'checks':checks,'pen_hand_matrix':[list(row) for row in pen_hand],'validation':'Source sampled frames only. Source motion playback, Unity import, interruption and actual station calibration pending.'},indent=2));print(json.dumps(checks))
