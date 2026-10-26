"""Withdraw the non-gripping hand before the opening/closing cover reaches its thumb."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
from npc_contact_math import NativeNpcRig
for family,oldversion,newversion,side in [('FolioOpen','003','004','Left')]:
 source=R/'SourceArt/Demo02/Characters/Learner'/('AcademicLearner_'+family+'Skin_v'+oldversion+'.blend');dest=source.with_name('AcademicLearner_'+family+'Skin_v'+newversion+'.blend')
 if dest.exists():raise RuntimeError('Preserve authored source')
 bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];old=bpy.data.actions['D02_Learner_'+family+'_skin_v'+oldversion];ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));frames=[];margins=[]
 for p in ordered:
  for c in p.constraints:c.mute=True
 for f in range(1,17):
  a.animation_data.action=old;a.animation_data.action_slot=old.slots[0];s.frame_set(f);bpy.context.view_layer.update();rig=NativeNpcRig(a,body,{})
  t=max(0,min(1,(f-8)/5));u=t*t*(3-2*t);h=a.pose.bones[side+'Hand'];target=a.matrix_world@h.head+Vector((0,.08,.025))*u;rotation=h.matrix.to_quaternion().copy();margin=rig.wrist(side,target,rotation,pole=a.matrix_world@a.pose.bones[side+'ForeArm'].head) if u>0 else (a.pose.bones[side+'Arm'].length+a.pose.bones[side+'ForeArm'].length)*rig.scale-(target-a.matrix_world@a.pose.bones[side+'Arm'].head).length;margins.append(margin)
  mats={p.name:p.matrix.copy() for p in ordered};pose={}
  for p in ordered:
   kw={'parent_matrix':mats[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {};pose[p.name]=p.bone.convert_local_to_pose(mats[p.name],p.bone.matrix_local,invert=True,**kw)
  frames.append(pose)
 act=bpy.data.actions.new('D02_Learner_'+family+'_skin_v'+newversion);act.use_fake_user=True;a.animation_data.action=act
 for f,pose in enumerate(frames,1):
  for p in ordered:
   p.matrix_basis=pose[p.name];p.rotation_mode='QUATERNION'
   for channel in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=channel,frame=f,group=p.name)
 a.animation_data.action_slot=act.slots[0];s.frame_set(1);s['support_correction']='Non-gripping hand withdraws 80mm toward body and25mm up before late cover contact; grip hand/source hinge unchanged.';bpy.ops.wm.save_as_mainfile(filepath=str(dest));print(family+' minmargin '+str(min(margins)),flush=True)
 out=R/'LocalOnly/Validation/Production'/('FolioSupportWithdrawal_'+family);out.mkdir(parents=True,exist_ok=True);(out/'reach.json').write_text(json.dumps({'side':side,'frames':16,'reach_margins_m':margins},indent=2));s.frame_set(13);s.render.resolution_x=s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=4;s.cycles.samples=10;look=Vector((0,-.34,1.0));s.camera.location=look+Vector((-.6,-.6,.35));s.camera.rotation_euler=(look-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=1.1;s.render.filepath=str(out/'late-cover.png');bpy.ops.render.render(write_still=True)

