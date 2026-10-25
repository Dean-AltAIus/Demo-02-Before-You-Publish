"""Editable static grip candidate, no contact clip admission until inspected."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v003.blend'
if dest.exists():raise RuntimeError('Preserve previous source edits')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v002.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig']
def body_rotation(name,axis,angle):
 p=a.pose.bones[name];q=p.bone.matrix_local.to_quaternion();p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(angle))@q
body_rotation('Spine02',(1,0,0),12);body_rotation('Spine01',(1,0,0),5)
bpy.data.objects['Right wrist contact'].location=(-.15,-.22,.965);bpy.data.objects['Left wrist contact'].location=(.16,-.25,.945)
for side,sign in [('Left',1),('Right',-1)]:
 p=a.pose.bones[side+'Hand'];c=a.pose.bones[side+'ForeArm'].constraints['Editable two-bone table reach']
 desired=a.matrix_world.to_quaternion().inverted()@Quaternion(Vector((1,0,0)),math.radians(35))@Quaternion(Vector((0,0,1)),math.radians(-90*sign))@(a.matrix_world@p.bone.matrix_local).to_quaternion()
 best=None
 for degrees in range(-180,180,5):
  c.pole_angle=math.radians(degrees);bpy.context.view_layer.update();p.matrix=Matrix.Translation(p.head)@desired.to_matrix().to_4x4();bpy.context.view_layer.update()
  angle=min(p.rotation_quaternion.angle,math.tau-p.rotation_quaternion.angle);elbow=a.matrix_world@a.pose.bones[side+'ForeArm'].head
  score=angle+max(0,.18-sign*elbow.x)*10
  if best is None or score<best[0]:best=(score,degrees)
 c.pole_angle=math.radians(best[1]);bpy.context.view_layer.update();p.matrix=Matrix.Translation(p.head)@desired.to_matrix().to_4x4()
chains=json.loads((R/'LocalOnly/Captures/Assets/Learner/Fingers_v003/finger-rig-audit.json').read_text())['joint_world_positions']
def curl(name,angles,opposition=0):
 ch=[Vector(v) for v in chains[name]]
 for i,degrees in enumerate(angles):
  p=a.pose.bones[name+str(i+1)];d=(ch[i+1]-ch[i]).normalized();axis=Vector((-d.y,d.x,0)).normalized();q=p.bone.matrix_local.to_quaternion()
  rotation=Quaternion(axis,math.radians(degrees))
  if i==0:rotation=Quaternion(Vector((0,0,1)),math.radians(opposition))@rotation
  p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@rotation@q
for digit,angles in [('Index',(25,45,20)),('Middle',(35,50,30)),('Ring',(40,55,35)),('Little',(40,60,35))]:curl('Right'+digit,angles)
best=None
for opposition in range(-70,-9,5):
 for bend in range(10,61,10):
  curl('RightThumb',(bend,25,15),opposition);bpy.context.view_layer.update()
  thumb=a.matrix_world@a.pose.bones['RightThumb3'].tail;index=a.matrix_world@a.pose.bones['RightIndex2'].tail
  distance=(thumb-index).length;score=abs(distance-.024)+abs(bend-30)*.00002
  if best is None or score<best[0]:best=(score,opposition,bend,distance)
curl('RightThumb',(best[2],25,15),best[1]);bpy.context.view_layer.update()
thumb=a.matrix_world@a.pose.bones['RightThumb3'].tail;index=a.matrix_world@a.pose.bones['RightIndex2'].tail;grip=(thumb+index)/2
axis=Vector((-.1,.5,.86)).normalized();pen=bpy.data.objects['AnnotationPen'];pen.rotation_euler=axis.to_track_quat('Z','Y').to_euler();pen.location=grip-axis*.065
tip=pen.matrix_world@Vector((0,0,-.012));bpy.context.view_layer.update();tip=pen.matrix_world@Vector((0,0,-.012))
s['contact_gate']='Static right-hand pen grip candidate; inspect finger geometry, actual tip and folio before admitting motion.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
ev=R/'LocalOnly/Captures/Assets/Learner/Contacts_v003';ev.mkdir(parents=True,exist_ok=True);s.cycles.samples=12;s.render.threads_mode='FIXED';s.render.threads=4
cam=s.camera;s.render.resolution_x=800;s.render.resolution_y=800
for view,pos,look,scale in [('hand_close',(.65,-1.25,1.45),(0,-.35,.91),.65),('grip_front',(-.4,-1.5,1.1),grip,.30),('side',(-4,-.4,1.1),(0,-.2,.9),2.05)]:
 cam.data.ortho_scale=scale;cam.location=pos;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(view+'.png'));bpy.ops.render.render(write_still=True)
report={'new_clips':0,'thumb_opposition_degrees':best[1],'thumb_base_bend_degrees':best[2],'thumb_to_index_joint_distance_m':best[3],'grip_world':list(grip),'pen_tip_world':list(tip),'tip_vs_folio_height_m':tip.z-.834,'acceptance':'Candidate only, full geometry inspection required'}
(ev/'contact-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
