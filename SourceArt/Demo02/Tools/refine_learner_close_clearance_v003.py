"""Millimetre-scale edge clearance on v020 Close. Other 28 actions stay unchanged."""
import bpy,sys,json,hashlib,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
from npc_contact_math import NativeNpcRig
source=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v020.blend';dest=source.with_name('AcademicLearner_ContactLibrary_v023.blend');out=R/'LocalOnly/Validation/Production/FolioClose_v023'
if dest.exists() or out.exists():raise RuntimeError('Preserve saved source and evidence')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_FolioCloseSkin_v004.blend'));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;cover=bpy.data.objects['Front cover'];axes={};normals={}
for i in range(121):
 f=1+i/8;s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();axes[f]=(cover.matrix_world.to_3x3()@Vector((1,0,0))).normalized();normals[f]=(cover.matrix_world.to_3x3()@Vector((0,0,1))).normalized()
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));rows=[]
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
old=bpy.data.actions['D02_Learner_FolioClose_v013'];name=old.name;frames=[]
for i in range(121):
 f=1+i/8;a.animation_data.action=old;a.animation_data.action_slot=old.slots[0];s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();weight=smooth((f-3)/3)*(1-smooth((f-8)/3));wrist=a.matrix_world@a.pose.bones['LeftHand'].head;elbow=a.matrix_world@a.pose.bones['LeftForeArm'].head;rotation=a.pose.bones['LeftHand'].matrix.to_quaternion()
 a.animation_data.action=None
 if weight>0:
  target=wrist+(axes[min(f,8)]*.0065-normals[min(f,8)]*.003+Vector((0,-.01,0)))*weight;rig=NativeNpcRig(a,body,{});margin=rig.wrist('Left',target,rotation,pole=elbow+Vector((0,-.08*weight,0)))
 else:target=wrist;margin=None
 rows.append({'frame':f,'offset_weight':weight,'wrist_error_m':((a.matrix_world@a.pose.bones['LeftHand'].head)-target).length,'reach_margin_m':margin});frames.append((f,{p.name:p.matrix_basis.copy() for p in ordered}))
old.name=name+'_before_edge_clearance';act=bpy.data.actions.new(name);act.use_fake_user=True;a.animation_data.action=act;previous={}
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
s['folio_close_clearance_revision']='v023: up to6.5mm edge-axis and3mm outward-surface wrist clearance plus10mm forward clearance,80mm forward elbow guide, tapered over frames3to11. Same contact timing/endpoints;28other actions preserved.'
a.animation_data.action=bpy.data.actions['D02_Learner_Idle_v013'];a.animation_data.action_slot=a.animation_data.action.slots[0];s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(dest));out.mkdir(parents=True);(out/'authoring.json').write_text(json.dumps({'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'candidate':str(dest.relative_to(R)),'candidate_sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'rows':rows},indent=2));print('Saved v023 clearance candidate',flush=True)
