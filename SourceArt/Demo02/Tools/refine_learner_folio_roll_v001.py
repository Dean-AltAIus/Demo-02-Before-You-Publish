"""Keep v017 wrists/grip, use continuous arm roll and a low own-side return guide."""
import bpy,sys,json,hashlib,math
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
from npc_contact_math import NativeNpcRig
source=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v017.blend';dest=source.with_name('AcademicLearner_ContactLibrary_v019.blend');out=R/'LocalOnly/Validation/Production/FolioRoll_v019'
if dest.exists() or out.exists():raise RuntimeError('Preserve saved source and evidence')
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));rows=[];chains=['RightArm','RightForeArm']
for p in ordered:
 for c in p.constraints:c.mute=True
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def sample(act,f):
 a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();return {'pose':{p.name:p.matrix_basis.copy() for p in ordered},'world':{p.name:p.matrix.copy() for p in ordered},'wrist':a.matrix_world@a.pose.bones['RightHand'].matrix,'elbow':a.matrix_world@a.pose.bones['RightForeArm'].head}
main=bpy.data.actions['D02_Learner_FolioOpen_v013'];recovery=bpy.data.actions['D02_Learner_FolioOpenRecovery_v013'];grip=sample(main,8);mid=sample(recovery,10);lowpole=Vector((-.55,-.15,.75));new_end=None
for phase,old in [('',main),('Recovery',recovery)]:
 name=old.name;count=int(old.frame_range[1]);cache={1+i/8:sample(old,1+i/8) for i in range((count-1)*8+1)};frames=[]
 for f,base in cache.items():
  a.animation_data.action=None
  for p in ordered:p.matrix_basis=base['pose'][p.name]
  bpy.context.view_layer.update();active=f>8 if not phase else f<10
  if active:
   if not phase:
    u=smooth((f-8)/8);pole=Vector((-.35,-.8,.8)).lerp(lowpole,u);start=grip
   else:
    u=smooth((f-1)/9);pole=new_end['elbow'].lerp(mid['elbow'],u)+Vector((0,0,-.035*math.sin(math.pi*(f-1)/9)));start=new_end
   rig=NativeNpcRig(a,body,{})
   for n in chains:rig.base[n]=start['world'][n].to_quaternion().slerp(mid['world'][n].to_quaternion(),u).to_matrix().to_4x4()
   rig.wrist('Right',base['wrist'].translation,(a.matrix_world.inverted()@base['wrist']).to_quaternion(),pole=pole)
   if phase and f==1:
    for n in chains:a.pose.bones[n].matrix_basis=new_end['pose'][n]
    bpy.context.view_layer.update()
  frames.append((f,{p.name:p.matrix_basis.copy() for p in ordered}));rows.append({'clip':name,'frame':f,'elbow':list(a.matrix_world@a.pose.bones['RightForeArm'].head),'wrist_error_m':((a.matrix_world@a.pose.bones['RightHand'].head)-base['wrist'].translation).length})
  if not phase and f==count:new_end={'pose':{p.name:p.matrix_basis.copy() for p in ordered},'world':{p.name:p.matrix.copy() for p in ordered},'elbow':a.matrix_world@a.pose.bones['RightForeArm'].head}
 old.name=name+'_before_continuous_roll';act=bpy.data.actions.new(name);act.use_fake_user=True;a.animation_data.action=act;previous={}
 for f,pose in frames:
  for p in ordered:
   p.matrix_basis=pose[p.name];p.rotation_mode='QUATERNION';q=p.rotation_quaternion.copy()
   if p.name in previous and q.dot(previous[p.name])<0:q.negate();p.rotation_quaternion=q
   previous[p.name]=q.copy()
   for channel in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=channel,frame=f,group=p.name)
 a.animation_data.action_slot=act.slots[0]
 for layer in act.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:
     for key in curve.keyframe_points:key.interpolation='LINEAR'
 print('Authored '+name,flush=True)
s['folio_roll_revision']='v019 candidate: continuous anatomical arm roll and low own-side elbow return, v017 wrist/grip path and timings retained. Full acceptance required.';a.animation_data.action=bpy.data.actions['D02_Learner_Idle_v013'];a.animation_data.action_slot=a.animation_data.action.slots[0];s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(dest));out.mkdir(parents=True);(out/'authoring.json').write_text(json.dumps({'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'candidate':str(dest.relative_to(R)),'candidate_sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'rows':rows},indent=2));print('Saved v019 candidate',flush=True)
