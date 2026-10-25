"""Native conversation body clips on inspected rigs; no speech or authority implied."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
roles=['Colleague','ModuleLead']
for role in roles:
 if (R/f'SourceArt/Demo02/Characters/{role}/{role}_Motion_v003.blend').exists():raise RuntimeError('Preserve existing motion version')
for role in roles:
 bpy.ops.wm.open_mainfile(filepath=str(R/f'SourceArt/Demo02/Characters/{role}/{role}_Motion_v002.blend'))
 s=bpy.data.scenes['Demo02_'+role+'Rig_v001'];bpy.context.window.scene=s
 a=bpy.data.objects[role+'_EditableRig'];a.animation_data_create();s.render.fps=30
 for existing in bpy.data.actions:existing.use_fake_user=True
 def rotate(name,axis,degrees):
  p=a.pose.bones[name];q=p.bone.matrix_local.to_quaternion();p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(degrees))@q
 takes=['Listen','Speak'] if role=='Colleague' else ['Listen'];records=[]
 for take in takes:
  action=bpy.data.actions.new('D02_'+role+'_'+take+'_v001');action.use_fake_user=True;a.animation_data.action=action
  end=121 if take=='Listen' else 91;s.frame_start=1;s.frame_end=end
  for frame in range(1,end+1,5):
   t=(frame-1)/(end-1);phase=t*math.tau;gesture=math.sin(math.pi*t)**2
   for p in a.pose.bones:p.matrix_basis=Matrix.Identity(4)
   for n,axis,degrees in [('LeftArm',(0,1,0),78),('RightArm',(0,1,0),-78),('LeftForeArm',(0,0,1),-6),('RightForeArm',(0,0,1),6),('Spine01',(1,0,0),.35*math.sin(phase)),('Head',(0,0,1),1.5*math.sin(phase))]:rotate(n,axis,degrees)
   if take=='Listen':
    rotate('Head',(1,0,0),1.8*gesture);rotate('neck',(0,0,1),1.3*math.sin(phase))
   else:
    rotate('LeftArm',(0,1,0),78-15*gesture);rotate('LeftForeArm',(0,0,1),-6-48*gesture)
    rotate('LeftHand',(1,0,0),8*gesture);rotate('Spine01',(0,0,1),1.4*math.sin(phase));rotate('Head',(1,0,0),-1.5*gesture)
   for p in a.pose.bones:
    p.rotation_mode='QUATERNION';p.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert(data_path='location',frame=frame,group=p.name);p.keyframe_insert(data_path='scale',frame=frame,group=p.name)
  records.append({'action':action.name,'frames':[1,end],'fps':30,'loop':take=='Listen','source':'Native editable Blender keys','root_motion':'Fixed root, no authority or speech events'})
 s.frame_set(1);s['conversation_motion_gate']='Native restrained body candidates. Rig rest/deformation views passed before authoring. Normal-speed Unity validation pending; no lipsync or live voice.'
 bpy.ops.wm.save_as_mainfile(filepath=str(R/f'SourceArt/Demo02/Characters/{role}/{role}_Motion_v003.blend'))
 ev=R/f'LocalOnly/Captures/Assets/{role}/Conversation_v001';ev.mkdir(parents=True,exist_ok=True)
 s.cycles.samples=12;s.render.threads_mode='FIXED';s.render.threads=4;s.render.resolution_x=640;s.render.resolution_y=800
 for take in takes:
  act=bpy.data.actions['D02_'+role+'_'+take+'_v001'];a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];s.frame_set(61 if take=='Listen' else 46)
  for view,pos in [('front',(0,-4,.95)),('threequarter',(2.8,-3,1.1))]:
   s.camera.location=pos;s.camera.rotation_euler=(Vector((0,0,.9))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(take+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
 (ev/'source-receipt.json').write_text(json.dumps({'role':role,'actions':records,'validation':'Key-pose source review required; normal-speed and Unity playback pending'},indent=2))
