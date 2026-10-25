"""Native editable restrained idle loops after six-view rig and deformation checks."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
for role in ['Colleague','ModuleLead']:
 dest=R/f'SourceArt/Demo02/Characters/{role}/{role}_Motion_v001.blend'
 if dest.exists():raise RuntimeError('Preserve existing motion source: '+str(dest))
 bpy.ops.wm.open_mainfile(filepath=str(R/f'SourceArt/Demo02/Characters/{role}/{role}_Rig_v001.blend'))
 s=bpy.data.scenes['Demo02_'+role+'Rig_v001'];bpy.context.window.scene=s;s.render.fps=30;s.frame_start=1;s.frame_end=121
 a=bpy.data.objects[role+'_EditableRig'];a.animation_data_create();action=bpy.data.actions.new('D02_'+role+'_Idle_v001');action.use_fake_user=True;a.animation_data.action=action
 def rotate(name,axis,degrees):
  p=a.pose.bones[name];q=p.bone.matrix_local.to_quaternion();p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(degrees))@q
 for f in [1,31,61,91,121]:
  for p in a.pose.bones:p.matrix_basis=Matrix.Identity(4)
  phase=(f-1)/120*math.tau
  for name,axis,degrees in [('LeftArm',(0,1,0),78),('RightArm',(0,1,0),-78),('LeftForeArm',(0,0,1),-6),('RightForeArm',(0,0,1),6),('Spine01',(1,0,0),.25*math.sin(phase)),('Head',(0,0,1),(1 if role=='Colleague' else .6)*math.sin(phase))]:rotate(name,axis,degrees)
  for p in a.pose.bones:
   p.rotation_mode='QUATERNION';p.keyframe_insert(data_path='rotation_quaternion',frame=f,group=p.name);p.keyframe_insert(data_path='location',frame=f,group=p.name);p.keyframe_insert(data_path='scale',frame=f,group=p.name)
 s.frame_set(1);bpy.context.view_layer.update();s['motion_gate']='Native restrained idle candidate. Six rest views and static deformation inspected before authoring. Unity playback pending.'
 bpy.ops.wm.save_as_mainfile(filepath=str(dest))
 ev=R/f'LocalOnly/Captures/Assets/{role}/Idle_v001';ev.mkdir(parents=True,exist_ok=True)
 s.render.resolution_x=768;s.render.resolution_y=1024;s.cycles.samples=16
 for view,pos in [('side',(-4,0,.95)),('threequarter',(2.8,-3,.95))]:
  s.camera.location=pos;s.camera.rotation_euler=(Vector((0,0,.85))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(view+'.png'));bpy.ops.render.render(write_still=True)
 (ev/'source-receipt.json').write_text(json.dumps({'source':'Native Blender keyframes','action':action.name,'fps':30,'frames':[1,121],'displacement':'Controller owns displacement; idle feet and root fixed','import_validation':'pending'},indent=2))
