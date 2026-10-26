"""Refine only the six M1 takes at 240 Hz, preserving timings and all other takes.
Wrist motion follows the actual Lerp/Slerp cover track. Native contact fit and
export/import/playback validation are required before admission.
"""
import bpy,json,sys,math,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
from npc_contact_math import NativeNpcRig
source=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v013.blend'
dest=source.with_name('AcademicLearner_ContactLibrary_v016.blend');out=R/'LocalOnly/Validation/Production/FolioClearance_v016'
if dest.exists() or out.exists():raise RuntimeError('Preserve existing candidate/evidence')
def lerp(x,y,t):
 p,q,z=x.decompose();p2,q2,z2=y.decompose();return Matrix.LocRotScale(p.lerp(p2,t),q.slerp(q2,t),z.lerp(z2,t))
def smooth(x):x=max(0,min(1,x));return x*x*(3-2*x)
props={}
for family in ['FolioOpen','FolioClose']:
 bpy.ops.wm.open_mainfile(filepath=str(R/f'SourceArt/Demo02/Characters/Learner/AcademicLearner_{family}Skin_v004.blend'));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s
 hinge=bpy.data.objects['FolioCoverHinge'];cover=bpy.data.objects['Front cover'];data={'hinge':{},'solids':{}}
 for f in range(1,17):
  s.frame_set(f);bpy.context.view_layer.update();data['hinge'][f]=hinge.matrix_world.copy();data['solids'][f]={n:bpy.data.objects[n].matrix_world.copy() for n in ['Front cover','Back cover','Thin paper stack']}
 data['relative']=data['hinge'][1].inverted()@data['solids'][1]['Front cover'];props[family]=data
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));rows=[]
for p in ordered:
 for c in p.constraints:c.mute=True
rig=NativeNpcRig(a,body,{});hand_ids={side:rig.hand_ids(side,threshold=.3) for side in ['Left','Right']}
bounds={n:(np.min([v.co[:] for v in bpy.data.objects[n].data.vertices],axis=0),np.max([v.co[:] for v in bpy.data.objects[n].data.vertices],axis=0)) for n in ['Front cover','Back cover','Thin paper stack']}
def wrist(side,target,rotation,pole):
 if solve_chain:
  margin=rig.solve(side+'Arm',side+'ForeArm',target,pole,min_margin=.001);head=a.pose.bones[side+'Hand'].head
 else:
  margin=(a.pose.bones[side+'Arm'].length+a.pose.bones[side+'ForeArm'].length)*rig.scale-(target-a.matrix_world@a.pose.bones[side+'Arm'].head).length;head=a.matrix_world.inverted()@target
 hand=a.pose.bones[side+'Hand'];hand.matrix=Matrix.Translation(head)@rotation.to_matrix().to_4x4();bpy.context.view_layer.update();return margin
def box_failures(side,solids):
 points=np.array([v[:] for v in rig.skin()]);points=points[hand_ids[side]];hom=np.column_stack((points,np.ones(len(points))));count=0
 for n,m in solids.items():
  local=hom@np.asarray(m.inverted()).T;lo,hi=bounds[n];inside=np.all((local[:,:3]>lo-.0005)&(local[:,:3]<hi+.0005),axis=1);count+=int(np.count_nonzero(inside))
 return count
for family,grip in [('FolioOpen','Right'),('FolioClose','Left')]:
 prop=props[family]
 for phase in ['Approach','','Recovery']:
  name='D02_Learner_'+family+phase+'_v013';old=bpy.data.actions[name];end=int(old.frame_range[1]);cache={};frames=[]
  for f in range(1,end+1):
   a.animation_data.action=old;a.animation_data.action_slot=old.slots[0];s.frame_set(f);bpy.context.view_layer.update();pf=1 if phase=='Approach' else 16 if phase=='Recovery' else f;cover=prop['solids'][pf]['Front cover']
   cache[f]={'pose':{p.name:p.matrix_basis.copy() for p in ordered},'hand':{side:a.matrix_world@a.pose.bones[side+'Hand'].matrix for side in hand_ids},'relative':cover.inverted()@(a.matrix_world@a.pose.bones[grip+'Hand'].matrix),'elbow':{side:a.matrix_world@a.pose.bones[side+'ForeArm'].head for side in hand_ids}}
  a.animation_data.action=None
  for i in range((end-1)*8+1):
   f=1+i/8;left=int(f);right=min(end,left+1);t=f-left;before=cache[left];after=cache[right]
   for p in ordered:p.matrix_basis=lerp(before['pose'][p.name],after['pose'][p.name],t)
   bpy.context.view_layer.update();pf=1 if phase=='Approach' else 16 if phase=='Recovery' else f;pl=int(pf);pr=min(16,pl+1);u=pf-pl;hinge=lerp(prop['hinge'][pl],prop['hinge'][pr],u);cover=hinge@prop['relative'];solids=dict(prop['solids'][pl]);solids['Front cover']=cover
   for side in hand_ids:
    original_hand=a.matrix_world@a.pose.bones[side+'Hand'].matrix
    hand=cover@lerp(before['relative'],after['relative'],t) if side==grip and not phase else original_hand.copy()
    target=hand.translation;rotation=(a.matrix_world.inverted()@hand).to_quaternion();pole=before['elbow'][side].lerp(after['elbow'][side],t)
    weight=0
    if family=='FolioOpen' and side==grip:
     weight=smooth((f-1)/4) if not phase else 1-smooth((f-1)/9) if phase=='Recovery' else 0
     pole=pole.lerp(Vector((-.35,-.35,.85)),weight)
    solve_chain=family=='FolioOpen' and side==grip and weight>0
    rig=NativeNpcRig(a,body,{});margin=(a.pose.bones[side+'Arm'].length+a.pose.bones[side+'ForeArm'].length)*rig.scale-(target-a.matrix_world@a.pose.bones[side+'Arm'].head).length
    # The unchanged hanging rest arm is nearly straight. Preserve its authored
    # interpolation rather than imposing a new eight-millimetre bend margin.
    if margin>.001:margin=wrist(side,target,rotation,pole)
    elif weight>0 or not phase:raise RuntimeError(f'Insufficient active-contact reach margin {name} frame {f} side {side}: {margin}')
    original_target=target.copy();failed=box_failures(side,solids);offset=Vector((0,0,0))
    if failed:
     best=None
     # Bounded local wrist translation only, never accumulated beyond 6 mm.
     for distance in [.0005,.001,.002,.003,.004,.006]:
      for axis in range(3):
       for sign in [-1,1]:
        shift=cover.to_3x3().col[axis].normalized()*distance*sign
        try:wrist(side,original_target+shift,rotation,pole)
        except RuntimeError:continue
        failures=box_failures(side,solids)
        if failures==0:best=shift;break
       if best is not None:break
      if best is not None:break
     if best is None:raise RuntimeError(f'No bounded skin clearance: {name} {f} {side}, {failed} vertices')
     offset=best;target=original_target+offset;margin=wrist(side,target,rotation,pole)
    rows.append({'clip':name,'frame':f,'side':side,'reach_margin_m':margin,'wrist_shift_m':offset.length,'wrist_shift_xyz':list(offset),'interpolated_hand_delta_m':(hand.translation-original_hand.translation).length,'initial_box_inside':failed,'final_box_inside':box_failures(side,solids),'pole_blend':weight})
   frames.append((f,{p.name:p.matrix_basis.copy() for p in ordered}))
  old.name=name+'_before_clearance';act=bpy.data.actions.new(name);act.use_fake_user=True;a.animation_data.action=act;previous={}
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
s['folio_clearance_revision']='v016 candidate: six M1 takes sampled 240 Hz, runtime cover interpolation, bounded right elbow and <=6mm wrist skin corrections. Native/export/runtime acceptance pending.'
s['required_fbx_bake_step']=.125
a.animation_data.action=bpy.data.actions['D02_Learner_Idle_v013'];a.animation_data.action_slot=a.animation_data.action.slots[0];s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(dest));out.mkdir(parents=True)
(out/'authoring.json').write_text(json.dumps({'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'candidate':str(dest.relative_to(R)),'candidate_sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'required_fbx_bake_step':.125,'samples':rows},indent=2));print('Saved candidate '+str(dest),flush=True)

