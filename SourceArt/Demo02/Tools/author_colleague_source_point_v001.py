"""Editable native open-palm source gesture, following motion-colleague Panel 4.
No index-finger claim, contact receipt, voice or review authority is generated.
"""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
R=Path(r'D:/Altaius/Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Colleague/Colleague_Interactions_v001.blend'
out=R/'LocalOnly/Validation/Production/NPCInteractions_v001/ColleagueSourcePoint'
if dest.exists() or (out.exists() and any(out.iterdir())):raise RuntimeError('Preserve authored version')
out.mkdir(parents=True,exist_ok=True)
source=R/'SourceArt/Demo02/Characters/Colleague/Colleague_Motion_v003.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
s=bpy.data.scenes['Demo02_ColleagueRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['Colleague_EditableRig'];body=bpy.data.objects['Colleague_WeightedMesh']
idle=bpy.data.actions['D02_Colleague_Idle_v001'];a.animation_data.action=idle;a.animation_data.action_slot=idle.slots[0]
s.frame_set(1);bpy.context.view_layer.update()
base={p.name:p.matrix.copy() for p in a.pose.bones};a.animation_data.action=None
for p in sorted(a.pose.bones,key=lambda p:len(p.parent_recursive)):
    kw={'parent_matrix':base[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
    p.matrix_basis=p.bone.convert_local_to_pose(base[p.name],p.bone.matrix_local,invert=True,**kw)
base_q={p.name:p.rotation_quaternion.copy() for p in a.pose.bones}
inv=a.matrix_world.inverted();scale=a.matrix_world.to_scale().x
upper=a.pose.bones['LeftArm'];lower=a.pose.bones['LeftForeArm']
start=a.matrix_world@upper.head;target=Vector((.25,-.24,1.10));pole=Vector((.34,0,1.02))
n=(target-start).normalized();d=(target-start).length;l1=upper.length*scale;l2=lower.length*scale
if d>l1+l2-.008:raise RuntimeError('Gesture must remain inside natural reach')
x=(l1*l1-l2*l2+d*d)/(2*d);bend=pole-start-n*(pole-start).dot(n);bend.normalize()
elbow=start+n*x+bend*math.sqrt(l1*l1-x*x)
for name,head,direction in [('LeftArm',start,elbow-start),('LeftForeArm',elbow,target-elbow)]:
    p=a.pose.bones[name];q=base[name].to_quaternion();q=(q@Vector((0,1,0))).rotation_difference((inv.to_3x3()@direction).normalized())@q
    p.matrix=Matrix.Translation(inv@head)@q.to_matrix().to_4x4();bpy.context.view_layer.update()
def basis(d,n):
    d=Vector(d).normalized();n=Vector(n);n=(n-d*n.dot(d)).normalized();x=d.cross(n).normalized()
    return Matrix((x,d,n)).transposed()
hand=a.pose.bones['LeftHand']
rotation=basis((.15,-.85,-.5),(0,-.5,.85))@basis((1,0,0),(0,0,-1)).transposed()
hand.matrix=Matrix.Translation(hand.head)@(rotation.to_quaternion()@hand.bone.matrix_local.to_quaternion()).to_matrix().to_4x4()
bpy.context.view_layer.update()
for name,axis,degrees in [('Head',(0,0,1),-8),('neck',(1,0,0),5)]:
    p=a.pose.bones[name];q=p.matrix.to_quaternion();p.matrix=Matrix.Translation(p.head)@(Quaternion(Vector(axis),math.radians(degrees))@q).to_matrix().to_4x4();bpy.context.view_layer.update()
gesture_q={p.name:p.rotation_quaternion.copy() for p in a.pose.bones}
actual=a.matrix_world@a.pose.bones['LeftHand'].head
action=bpy.data.actions.new('D02_Colleague_SourcePoint_v001');action.use_fake_user=True
for existing in bpy.data.actions:existing.use_fake_user=True
a.animation_data.action=action;s.render.fps=30;s.frame_start=1;s.frame_end=61
def ease(t):return t*t*(3-2*t)
for frame in range(1,62):
    t=(frame-1)/60
    amount=ease(t/.25) if t<.25 else 1 if t<.65 else 1-ease((t-.65)/.35)
    for p in a.pose.bones:
        p.rotation_mode='QUATERNION';p.rotation_quaternion=base_q[p.name].slerp(gesture_q[p.name],amount)
        p.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=p.name)
for layer in action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for curve in bag.fcurves:
                for key in curve.keyframe_points:key.interpolation='LINEAR'
target_helper=bpy.data.objects.new('Source gesture target (presentation only)',None);s.collection.objects.link(target_helper)
target_helper.location=(.29,-.45,.98);target_helper.empty_display_type='ARROWS';target_helper.empty_display_size=.08
s['source_point_gate']='Multi-angle native pose and sequence evidence required; Unity runtime and interruption acceptance pending.'
s.frame_set(28);bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(dest))
(out/'native-receipt.json').write_text(json.dumps({'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'action':action.name,'frames':[1,61],'fps':30,'entry_seconds':.5,'hold_seconds':.8,'recovery_seconds':.7,'root_motion':False,'authority':False,'wrist_target':list(target),'wrist_actual':list(actual),'wrist_error_m':(actual-target).length,'reach_margin_m':l1+l2-d,'status':'Native candidate, no Unity acceptance'},indent=2),encoding='utf-8')
s.render.resolution_x=640;s.render.resolution_y=760;s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=2;s.cycles.samples=8
cam=s.camera;cam.data.type='ORTHO';cam.data.ortho_scale=2.05
for name,offset in [('front',(0,-4,0)),('left',(4,0,0)),('back',(0,4,0)),('right',(-4,0,0))]:
    look=Vector((0,-.08,.91));cam.location=look+Vector(offset);cam.rotation_euler=(look-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
print('SOURCE_POINT_NATIVE_READY',flush=True)
