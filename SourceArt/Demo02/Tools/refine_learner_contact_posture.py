"""Minimize wrist torsion in the static two-arm reach pilot before contact clips."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v002.blend'
if dest.exists():raise RuntimeError('Preserve existing contact source')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v001.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig']
report=[]
for side,sign in [('Left',1),('Right',-1)]:
 wrist=bpy.data.objects[side+' wrist contact'];wrist.location=(.16*sign,-.24 if sign==1 else -.27,1.02)
 p=a.pose.bones[side+'Hand'];c=a.pose.bones[side+'ForeArm'].constraints['Editable two-bone table reach']
 world_rest=(a.matrix_world@p.bone.matrix_local).to_quaternion()
 wanted=Quaternion(Vector((1,0,0)),math.radians(35))@Quaternion(Vector((0,0,1)),math.radians(-90*sign))@world_rest
 wanted_local=a.matrix_world.to_quaternion().inverted()@wanted
 best=None
 for degrees in range(-180,180,5):
  c.pole_angle=math.radians(degrees);bpy.context.view_layer.update()
  p.matrix=Matrix.Translation(p.head)@wanted_local.to_matrix().to_4x4();bpy.context.view_layer.update()
  q=p.rotation_quaternion.normalized();angle=min(q.angle,math.tau-q.angle)
  elbow=a.matrix_world@a.pose.bones[side+'ForeArm'].head
  # Avoid an elbow crossing the torso just to reduce wrist roll.
  penalty=max(0,.18-sign*elbow.x)*10+max(0,-.25-elbow.y)*10
  score=angle+penalty
  if best is None or score<best[0]:best=(score,degrees,angle,list(elbow))
 c.pole_angle=math.radians(best[1]);bpy.context.view_layer.update();p.matrix=Matrix.Translation(p.head)@wanted_local.to_matrix().to_4x4();bpy.context.view_layer.update()
 report.append({'side':side,'pole_angle_degrees':best[1],'wrist_local_rotation_degrees':math.degrees(best[2]),'elbow_world':best[3]})
s['contact_gate']='Static wrist-roll correction candidate, no interaction clips. Pen grip and exact fingertip contacts remain unaccepted.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
ev=R/'LocalOnly/Captures/Assets/Learner/Contacts_v002';ev.mkdir(parents=True,exist_ok=True)
s.render.resolution_x=640;s.render.resolution_y=800;s.render.threads_mode='FIXED';s.render.threads=4;s.cycles.samples=12;cam=s.camera
for view,pos,look,scale in [('side',(-4,-.4,1.1),(0,-.2,.9),2.05),('threequarter',(2.8,-3,1.5),(0,-.25,.9),2.05),('hand_close',(.7,-1.3,1.6),(0,-.35,.97),.62)]:
 cam.data.ortho_scale=scale;cam.location=pos;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(view+'.png'));bpy.ops.render.render(write_still=True)
(ev/'contact-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
