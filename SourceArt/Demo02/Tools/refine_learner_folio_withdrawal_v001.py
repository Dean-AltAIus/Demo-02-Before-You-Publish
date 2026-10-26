"""Bounded native open-folio withdrawal revision. Keep the proven cover grip
through frame eight, clear above the cover, then return on the learner's right.
Only open/contact and open/recovery curves change from the retained dense base.
"""
import bpy,sys,json,hashlib,math
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
from npc_contact_math import NativeNpcRig
source=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v016.blend';dest=source.with_name('AcademicLearner_ContactLibrary_v017.blend');out=R/'LocalOnly/Validation/Production/FolioWithdrawal_v017'
if dest.exists() or out.exists():raise RuntimeError('Preserve saved candidate and evidence')
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def lerp(x,y,t):
 p,q,z=x.decompose();p2,q2,z2=y.decompose();return Matrix.LocRotScale(p.lerp(p2,t),q.slerp(q2,t),z.lerp(z2,t))
def bezier(a,b,c,d,t):return a*(1-t)**3+b*(3*t*(1-t)**2)+c*(3*t*t*(1-t))+d*t**3
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_FolioOpenSkin_v004.blend'));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;hinge=bpy.data.objects['FolioCoverHinge'];props={}
for f in range(1,17):
 s.frame_set(f);bpy.context.view_layer.update();props[f]={'hinge':hinge.matrix_world.copy(),'solids':{n:bpy.data.objects[n].matrix_world.copy() for n in ['Front cover','Back cover','Thin paper stack']}}
relative=props[1]['hinge'].inverted()@props[1]['solids']['Front cover']
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive))
for p in ordered:
 for c in p.constraints:c.mute=True
def sample(action,frame):
 a.animation_data.action=action;a.animation_data.action_slot=action.slots[0];s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update();return {'pose':{p.name:p.matrix_basis.copy() for p in ordered},'hand':a.matrix_world@a.pose.bones['RightHand'].matrix,'elbow':a.matrix_world@a.pose.bones['RightForeArm'].head}
old_open=bpy.data.actions['D02_Learner_FolioOpen_v013'];old_recovery=bpy.data.actions['D02_Learner_FolioOpenRecovery_v013'];grip=sample(old_open,8);mid=sample(old_recovery,10);end=Vector((-.18,-.28,1.08));q0=grip['hand'].to_quaternion();q1=mid['hand'].to_quaternion();rows=[]
right_digits=[p.name for p in ordered if any(p.name.startswith('Right'+digit) for digit in ['Thumb','Index','Middle','Ring','Little'])]
solids=[bpy.data.objects[n] for n in ['Front cover','Back cover','Thin paper stack']]
rig=NativeNpcRig(a,body,{});hand_ids=rig.hand_ids('Left',threshold=.3)+rig.hand_ids('Right',threshold=.3)
deform={b.name for b in a.data.bones if b.use_deform};weights=[]
for v in body.data.vertices:
 w={body.vertex_groups[g.group].name:g.weight for g in v.groups if body.vertex_groups[g.group].name in deform};total=sum(w.values());weights.append({n:x/total for n,x in w.items()})
torso={i for i,w in enumerate(weights) if sum(v for n,v in w.items() if n.startswith('Spine'))>.65 and sum(v for n,v in w.items() if 'Arm' in n)<.05};armids=[i for i,w in enumerate(weights) if w.get('RightForeArm',0)>.65]
def torso_check():
 ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();verts=[ev.matrix_world@v.co for v in mesh.vertices];faces=[list(p.vertices) for p in mesh.polygons if all(i in torso for i in p.vertices)];tree=BVHTree.FromPolygons(verts,faces);depths=[]
 for i in armids:
  v=verts[i];front,_,_,_=tree.ray_cast(Vector((v.x,-3,v.z)),Vector((0,1,0)),6);back,_,_,_=tree.ray_cast(Vector((v.x,3,v.z)),Vector((0,-1,0)),6)
  if front is not None and back is not None and back.y-front.y>.04 and front.y+.0001<v.y<back.y-.0001:depths.append(v.y-front.y)
 ev.to_mesh_clear();return {'inside_vertices':len(depths),'max_depth_y_m':max(depths,default=0)}
for phase,old in [('',old_open),('Recovery',old_recovery)]:
 name=old.name;count=int(old.frame_range[1]);cache={f/8+1:sample(old,f/8+1) for f in range((count-1)*8+1)};frames=[]
 for f,base in cache.items():
  a.animation_data.action=None
  for p in ordered:p.matrix_basis=base['pose'][p.name]
  bpy.context.view_layer.update();hand=base['hand'];weight=smooth((f-1)/4) if not phase else 1-smooth((f-1)/9);pole=base['elbow'].lerp(Vector((-.35,-.8,.8)),weight);target=hand.translation;rotation=(a.matrix_world.inverted()@hand).to_quaternion()
  if not phase and f>8:
   t=(f-8)/8;target=bezier(grip['hand'].translation,Vector((.13,-.25,1.31)),Vector((-.20,-.30,1.28)),end,t);rotation=a.matrix_world.inverted().to_quaternion()@q0.slerp(q1,smooth(t))
   for n in right_digits:a.pose.bones[n].matrix_basis=lerp(grip['pose'][n],mid['pose'][n],smooth((f-8)/4))
  if phase and f<=10:
   t=(f-1)/9;target=bezier(end,end+Vector((0,0,.025)),mid['hand'].translation+Vector((0,0,.025)),mid['hand'].translation,t);rotation=a.matrix_world.inverted().to_quaternion()@q1
   for n in right_digits:a.pose.bones[n].matrix_basis=mid['pose'][n]
  bpy.context.view_layer.update();rig=NativeNpcRig(a,body,{})
  if weight>0:
   margin=rig.wrist('Right',target,rotation,pole=pole)
  else:margin=None
  pf=16 if phase else f;left=int(pf);right=min(16,left+1);cover=lerp(props[left]['hinge'],props[right]['hinge'],pf-left)@relative;matrices=dict(props[left]['solids']);matrices['Front cover']=cover
  for obj in solids:obj.matrix_world=matrices[obj.name]
  bpy.context.view_layer.update();contact=rig.audit_objects(solids,ids=hand_ids,penetration_only=True);torso_result=torso_check();rows.append({'clip':name,'frame':f,'reach_margin_m':margin,'contact':contact,'torso':torso_result});frames.append((f,{p.name:p.matrix_basis.copy() for p in ordered}))
 old.name=name+'_before_withdrawal';act=bpy.data.actions.new(name);act.use_fake_user=True;a.animation_data.action=act;previous={}
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
s['folio_withdrawal_revision']='v017 candidate: exact grip through frame8, lifted clearance and own-right withdrawal, matching recovery. Contact/torso/native views/import/playback still required.';s['required_fbx_bake_step']=.125;a.animation_data.action=bpy.data.actions['D02_Learner_Idle_v013'];a.animation_data.action_slot=a.animation_data.action.slots[0];s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(dest));out.mkdir(parents=True)
(out/'authoring.json').write_text(json.dumps({'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'candidate':str(dest.relative_to(R)),'candidate_sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'samples':rows},indent=2));print(json.dumps({'contact_failed_samples':sum(any(c['inside_vertex_count'] or c['ray_disagreements'] for c in r['contact']) for r in rows),'torso_failed_samples':sum(r['torso']['inside_vertices']>0 for r in rows)}),flush=True)
