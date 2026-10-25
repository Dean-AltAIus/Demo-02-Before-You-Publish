"""Static reduced distal curl study with editable finger rotations and preserved source."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v008.blend'
if dest.exists():raise RuntimeError('Preserve contact candidate')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v005.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig']
chains=json.loads((R/'LocalOnly/Captures/Assets/Learner/Fingers_v003/finger-rig-audit.json').read_text())['joint_world_positions']
def curl(digit,angles,opposition=0):
 points=[Vector(v) for v in chains['Right'+digit]]
 for i,degrees in enumerate(angles):
  p=a.pose.bones['Right'+digit+str(i+1)];d=(points[i+1]-points[i]).normalized();axis=Vector((-d.y,d.x,0)).normalized();q=p.bone.matrix_local.to_quaternion()
  rot=Quaternion(axis,math.radians(degrees))
  if i==0:rot=Quaternion(Vector((0,0,1)),math.radians(opposition))@rot
  p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@rot@q
for digit,angles in [('Index',(30,50,0)),('Middle',(35,50,5)),('Ring',(40,50,5)),('Little',(40,50,5))]:curl(digit,angles)
bpy.context.view_layer.update();index=a.matrix_world@a.pose.bones['RightIndex3'].head
best=None
for opposition in range(-90,91,10):
 for bend in range(-30,61,10):
  curl('Thumb',(bend,25,10),opposition);bpy.context.view_layer.update();thumb=a.matrix_world@a.pose.bones['RightThumb3'].tail
  distance=(thumb-index).length;score=abs(distance-.019)+abs(opposition)*.000008+abs(bend-20)*.00001
  if best is None or score<best[0]:best=(score,opposition,bend,distance)
curl('Thumb',(best[2],25,10),best[1]);bpy.context.view_layer.update()
right=a.pose.bones['RightHand'];rotation=right.matrix.to_quaternion();wrist=bpy.data.objects['Right wrist contact'];pen=bpy.data.objects['AnnotationPen']
axis=Vector((-.18,.65,.74)).normalized();target=Vector((-.055,-.435,.831))
for iteration in range(4):
 bpy.context.view_layer.update();right.matrix=Matrix.Translation(right.head)@rotation.to_matrix().to_4x4();bpy.context.view_layer.update()
 grip=((a.matrix_world@a.pose.bones['RightThumb3'].tail)+(a.matrix_world@a.pose.bones['RightIndex3'].head))/2
 pen.rotation_euler=axis.to_track_quat('Z','Y').to_euler();pen.location=grip-axis*.043;bpy.context.view_layer.update();tip=pen.matrix_world@Vector((0,0,-.012))
 if iteration<3:wrist.location+=target-tip
s['contact_gate']='Static tripod study. Inspect finger silhouette, pen intersection and complete body before clips.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
ev=R/'LocalOnly/Captures/Assets/Learner/Contacts_v008';ev.mkdir(parents=True,exist_ok=True)
s.cycles.samples=12;s.render.threads_mode='FIXED';s.render.threads=4;s.render.resolution_x=800;s.render.resolution_y=800;cam=s.camera
for view,pos,look,scale in [('hand_close',(.65,-1.25,1.45),(0,-.40,.91),.65),('grip_front',(-.45,-1.5,1.15),grip,.32),('grip_side',(.4,-.5,1.25),grip,.32),('side',(-4,-.4,1.1),(0,-.2,.9),2.05)]:
 cam.data.ortho_scale=scale;cam.location=pos;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(view+'.png'));bpy.ops.render.render(write_still=True)
report={'new_clips':0,'finger_joint_gap_m':best[3],'thumb_opposition_degrees':best[1],'thumb_base_bend_degrees':best[2],'tip_world':list(tip),'acceptance':'Static visual review pending'}
(ev/'contact-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
