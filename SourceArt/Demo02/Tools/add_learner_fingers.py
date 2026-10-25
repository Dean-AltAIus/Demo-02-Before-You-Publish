"""Bounded finger-rig addition. Preserves body weights and the prior source rig.
Joint placements come from inspected hand geometry. Static curl probes are not clips.
"""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Rig_v003.blend'
if dest.exists():raise RuntimeError('Refuse to replace an authored rig version')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Rig_v001.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh']
ev=R/'LocalOnly/Captures/Assets/Learner/Fingers_v003';ev.mkdir(parents=True,exist_ok=True)
xy={
 'Thumb':[(.655,-.018),(.680,-.039),(.699,-.056),(.711,-.067)],
 'Index':[(.713,-.013),(.746,-.014),(.773,-.016),(.792,-.017)],
 'Middle':[(.724,.012),(.754,.011),(.782,.011),(.803,.011)],
 'Ring':[(.722,.034),(.749,.033),(.776,.032),(.796,.031)],
 'Little':[(.704,.057),(.733,.055),(.758,.054),(.774,.053)]}
verts=[body.matrix_world@v.co for v in body.data.vertices]
chains={}
for side,sign in [('Left',1),('Right',-1)]:
 for digit,points in xy.items():
  chain=[]
  for x,y in points:
   x*=sign
   near=sorted((p for p in verts if abs(p.x)>.62),key=lambda p:(p.x-x)**2+(p.y-y)**2)[:12]
   z=sum(p.z for p in near)/len(near)
   chain.append(Vector((x,y,z)))
  chains[side+digit]=chain
bpy.ops.object.select_all(action='DESELECT');a.select_set(True);bpy.context.view_layer.objects.active=a
bpy.ops.object.mode_set(mode='EDIT');inverse=a.matrix_world.inverted()
for name,points in chains.items():
 previous=a.data.edit_bones['LeftHand' if name.startswith('Left') else 'RightHand']
 for i in range(3):
  bone=a.data.edit_bones.new(name+str(i+1));bone.head=inverse@points[i];bone.tail=inverse@points[i+1]
  bone.parent=previous;bone.use_connect=i>0;bone.use_deform=True;previous=bone
bpy.ops.object.mode_set(mode='OBJECT')
groups={name+str(i+1):body.vertex_groups.new(name=name+str(i+1)) for name in chains for i in range(3)}
def closest(point,chain):
 best=None;acc=0
 for i in range(3):
  delta=chain[i+1]-chain[i];length=delta.length;t=max(0,min(1,(point-chain[i]).dot(delta)/delta.length_squared));q=chain[i]+delta*t
  candidate=((point-q).length,acc+t*length,i,t)
  if best is None or candidate[0]<best[0]:best=candidate
  acc+=length
 return best
changed=0
for v,p in zip(body.data.vertices,verts):
 if abs(p.x)<.63:continue
 side='Left' if p.x>0 else 'Right';hand=body.vertex_groups.get(side+'Hand')
 if not hand:continue
 try:hand_weight=hand.weight(v.index)
 except RuntimeError:continue
 if hand_weight<.01:continue
 # Distal connected-component audit identified finger surface boundaries.
 # Avoid crossing weights between adjacent digits merely by Euclidean proximity.
 digit=None
 if abs(p.x)>.725:
  digit='Index' if p.y<-.002 else 'Middle' if p.y<.022 else 'Ring' if p.y<.044 else 'Little'
 candidates=[(closest(p,ch),name,ch) for name,ch in chains.items() if name.startswith(side) and (digit is None or name==side+digit)]
 hit,name,ch=min(candidates,key=lambda x:x[0][0]);distance,arc,seg,t=hit
 axis=(ch[1]-ch[0]).normalized();forward=(p-ch[0]).dot(axis)
 influence=max(0,min(1,(forward+.012)/.028))*max(0,min(1,(.035-distance)/.012))
 if influence<=0:continue
 # Linear skinning transitions around each anatomical joint, keeping palm support.
 weights=[0.,0.,0.]
 if t<.25 and seg>0:
  blend=(t+.25)/.5;weights[seg-1]=1-blend;weights[seg]=blend
 elif t>.75 and seg<2:
  blend=(t-.75)/.5;weights[seg]=1-blend;weights[seg+1]=blend
 else:weights[seg]=1
 hand.add([v.index],hand_weight*(1-influence),'REPLACE')
 for i,w in enumerate(weights):
  if w:groups[name+str(i+1)].add([v.index],hand_weight*influence*w,'REPLACE')
 changed+=1
a['finger_controls']='Ten editable three-joint chains; inspected geometry placement; body weights preserved'
s['animation_gate']='Body rest and static deformation inspected; new finger static deformation must be inspected before contact clips'
report={'bone_count':len(a.data.bones),'weighted_vertices_changed':changed,'joint_world_positions':{n:[list(p) for p in ch] for n,ch in chains.items()},'prior_source_preserved':True,'animation_clips_authored':0}
(ev/'finger-rig-audit.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
def curl(amount):
 for name,ch in chains.items():
  for i in range(3):
   p=a.pose.bones[name+str(i+1)];d=(ch[i+1]-ch[i]).normalized();axis=Vector((-d.y,d.x,0)).normalized();q=p.bone.matrix_local.to_quaternion()
   p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@Quaternion(axis,math.radians(amount[i]))@q
 bpy.context.view_layer.update()
cam=s.camera;cam.data.ortho_scale=.30;s.render.resolution_x=1024;s.render.resolution_y=1024
for pose,angles in [('soft_grip',(25,45,30))]:
 curl(angles)
 for side,sign in [('Left',1),('Right',-1)]:
  target=Vector((.71*sign,.005,1.28))
  for view,offset in [('top',(0,0,.5)),('palm',(0,0,-.5)),('oblique',(.25*sign,-.2,.35))]:
   cam.location=target+Vector(offset);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
   s.render.filepath=str(ev/(side+'_'+pose+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps(report))
