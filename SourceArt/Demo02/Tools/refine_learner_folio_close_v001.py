"""Correct only Close and CloseRecovery working-left-arm motion from the saved v019 master."""
import bpy,sys,json,hashlib,math
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
from npc_contact_math import NativeNpcRig
source=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v019.blend';dest=source.with_name('AcademicLearner_ContactLibrary_v020.blend');out=R/'LocalOnly/Validation/Production/FolioClose_v020'
if dest.exists() or out.exists():raise RuntimeError('Preserve saved source and evidence')
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));rows=[];chains=['LeftArm','LeftForeArm']
for p in ordered:
 for c in p.constraints:c.mute=True
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def lerp(x,y,t):
 p,q,z=x.decompose();p2,q2,z2=y.decompose();return Matrix.LocRotScale(p.lerp(p2,t),q.slerp(q2,t),z.lerp(z2,t))
def bezier(a,b,c,d,t):return a*(1-t)**3+b*(3*t*(1-t)**2)+c*(3*t*t*(1-t))+d*t**3
def sample(act,f):
 a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();return {'pose':{p.name:p.matrix_basis.copy() for p in ordered},'world':{p.name:p.matrix.copy() for p in ordered},'wrist':a.matrix_world@a.pose.bones['LeftHand'].matrix,'elbow':a.matrix_world@a.pose.bones['LeftForeArm'].head}
main=bpy.data.actions['D02_Learner_FolioClose_v013'];recovery=bpy.data.actions['D02_Learner_FolioCloseRecovery_v013'];initial=sample(main,1);grip=sample(main,8);mid=sample(recovery,10);end=Vector((.18,-.28,1.08));forward=Vector((.35,-.8,.8));low=Vector((.55,-.15,.75));new_end=None;digits=[p.name for p in ordered if any(p.name.startswith('Left'+d) for d in ['Thumb','Index','Middle','Ring','Little'])]
# A fixed initial anatomical roll reference removes inherited frame-specific twists.
a.animation_data.action=None
for p in ordered:p.matrix_basis=grip['pose'][p.name]
bpy.context.view_layer.update();rig=NativeNpcRig(a,body,{})
for n in chains:rig.base[n]=initial['world'][n]
rig.wrist('Left',grip['wrist'].translation,(a.matrix_world.inverted()@grip['wrist']).to_quaternion(),pole=forward);grip_reference={n:a.pose.bones[n].matrix.copy() for n in chains}
for phase,old in [('',main),('Recovery',recovery)]:
 name=old.name;count=int(old.frame_range[1]);cache={1+i/8:sample(old,1+i/8) for i in range((count-1)*8+1)};frames=[]
 for f,base in cache.items():
  a.animation_data.action=None
  for p in ordered:p.matrix_basis=base['pose'][p.name]
  bpy.context.view_layer.update();target=base['wrist'].translation;rotation=base['wrist'].to_quaternion();refs=None;u=0
  if not phase:
   if f<=8:
    u=smooth((f-1)/4);pole=base['elbow'].lerp(forward,u);refs=initial['world']
   else:
    t=(f-8)/8;u=smooth(t);pole=forward.lerp(low,u);target=bezier(grip['wrist'].translation,Vector((-.13,-.25,1.31)),Vector((.20,-.30,1.28)),end,t);rotation=grip['wrist'].to_quaternion().slerp(mid['wrist'].to_quaternion(),u);refs={n:grip_reference[n].to_quaternion().slerp(mid['world'][n].to_quaternion(),u).to_matrix().to_4x4() for n in chains}
    for n in digits:a.pose.bones[n].matrix_basis=lerp(grip['pose'][n],mid['pose'][n],smooth((f-8)/4))
  elif f<=10:
   t=(f-1)/9;u=smooth(t);pole=new_end['elbow'].lerp(mid['elbow'],u)+Vector((0,0,-.035*math.sin(math.pi*t)));target=bezier(end,end+Vector((0,0,.025)),mid['wrist'].translation+Vector((0,0,.025)),mid['wrist'].translation,t);rotation=mid['wrist'].to_quaternion();refs={n:new_end['world'][n].to_quaternion().slerp(mid['world'][n].to_quaternion(),u).to_matrix().to_4x4() for n in chains}
   for n in digits:a.pose.bones[n].matrix_basis=mid['pose'][n]
  bpy.context.view_layer.update();margin=None
  if refs is not None and (f>1 if not phase else f<10):
   rig=NativeNpcRig(a,body,{})
   for n in chains:rig.base[n]=refs[n]
   margin=rig.wrist('Left',target,a.matrix_world.inverted().to_quaternion()@rotation,pole=pole)
   if phase and f==1:
    for n in chains:a.pose.bones[n].matrix_basis=new_end['pose'][n]
    bpy.context.view_layer.update()
  frames.append((f,{p.name:p.matrix_basis.copy() for p in ordered}));rows.append({'clip':name,'frame':f,'elbow':list(a.matrix_world@a.pose.bones['LeftForeArm'].head),'wrist':list(a.matrix_world@a.pose.bones['LeftHand'].head),'wrist_error_m':((a.matrix_world@a.pose.bones['LeftHand'].head)-target).length,'reach_margin_m':margin})
  if not phase and f==count:new_end={'pose':{p.name:p.matrix_basis.copy() for p in ordered},'world':{p.name:p.matrix.copy() for p in ordered},'elbow':a.matrix_world@a.pose.bones['LeftForeArm'].head}
 old.name=name+'_before_left_clearance';act=bpy.data.actions.new(name);act.use_fake_user=True;a.animation_data.action=act;previous={}
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
s['folio_close_revision']='v020 candidate: working LEFT arm clears torso, exact cover grip through frame8, lifted own-left withdrawal and low recovery. Open and other27 current actions retained. Full native/Unity acceptance required.';a.animation_data.action=bpy.data.actions['D02_Learner_Idle_v013'];a.animation_data.action_slot=a.animation_data.action.slots[0];s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(dest));out.mkdir(parents=True);(out/'authoring.json').write_text(json.dumps({'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'candidate':str(dest.relative_to(R)),'candidate_sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'rows':rows},indent=2));print('Saved v020 close candidate',flush=True)
