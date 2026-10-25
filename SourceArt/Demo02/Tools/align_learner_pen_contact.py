"""Align the static grip to the actual folio plane, preserving earlier candidates."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v004.blend'
if dest.exists():raise RuntimeError('Preserve existing contact source')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v003.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];hand=a.pose.bones['RightHand']
chains=json.loads((R/'LocalOnly/Captures/Assets/Learner/Fingers_v003/finger-rig-audit.json').read_text())['joint_world_positions']
for digit,angles in [('Index',(15,30,10)),('Middle',(25,40,20)),('Ring',(30,45,20)),('Little',(35,45,20))]:
 points=[Vector(v) for v in chains['Right'+digit]]
 for i,degrees in enumerate(angles):
  p=a.pose.bones['Right'+digit+str(i+1)];direction=(points[i+1]-points[i]).normalized();axis=Vector((-direction.y,direction.x,0)).normalized();q=p.bone.matrix_local.to_quaternion()
  p.rotation_quaternion=q.inverted()@Quaternion(axis,math.radians(degrees))@q
desired_rotation=hand.matrix.to_quaternion();wrist=bpy.data.objects['Right wrist contact'];pen=bpy.data.objects['AnnotationPen'];axis=Vector((-.1,.5,.86)).normalized();target=Vector((-.065,-.435,.834))
for iteration in range(4):
 bpy.context.view_layer.update();hand.matrix=Matrix.Translation(hand.head)@desired_rotation.to_matrix().to_4x4();bpy.context.view_layer.update()
 grip=((a.matrix_world@a.pose.bones['RightThumb3'].tail)+(a.matrix_world@a.pose.bones['RightIndex2'].tail))/2
 pen.rotation_euler=axis.to_track_quat('Z','Y').to_euler();pen.location=grip-axis*.065;bpy.context.view_layer.update();tip=pen.matrix_world@Vector((0,0,-.012))
 if iteration<3:wrist.location+=target-tip
s['contact_gate']='Static calibrated tip candidate. No animation clip admitted until the actual grip and full pose views pass.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
ev=R/'LocalOnly/Captures/Assets/Learner/Contacts_v004';ev.mkdir(parents=True,exist_ok=True);s.cycles.samples=12;s.render.threads_mode='FIXED';s.render.threads=4;cam=s.camera;s.render.resolution_x=800;s.render.resolution_y=800
for view,pos,look,scale in [('hand_close',(.65,-1.25,1.45),(0,-.38,.91),.65),('grip_front',(-.4,-1.5,1.1),grip,.30),('side',(-4,-.4,1.1),(0,-.2,.9),2.05)]:
 cam.data.ortho_scale=scale;cam.location=pos;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(view+'.png'));bpy.ops.render.render(write_still=True)
report={'new_clips':0,'tip_world':list(tip),'target_world':list(target),'tip_error_m':(target-tip).length,'wrist_target':list(wrist.location),'acceptance':'Static geometry inspection required; no runtime or motion proof'}
(ev/'contact-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
