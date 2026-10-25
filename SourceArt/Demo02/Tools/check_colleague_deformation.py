"""Static deformation probes only. Does not author or admit animation clips."""
import bpy,math,json
from pathlib import Path
from mathutils import Matrix,Vector,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Colleague/Colleague_Rig_v001.blend'))
s=bpy.data.scenes['Demo02_ColleagueRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['Colleague_EditableRig'];cam=s.camera
ev=R/'LocalOnly/Captures/Assets/Colleague/Deformation';ev.mkdir(parents=True,exist_ok=True)
def reset():
    for p in a.pose.bones:p.matrix_basis=Matrix.Identity(4)
def rotate(name,axis,degrees):
    p=a.pose.bones[name];q=p.bone.matrix_local.to_quaternion();p.rotation_mode='QUATERNION'
    p.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(degrees))@q
poses={'relaxed_shoulders':[('LeftArm',(0,1,0),72),('RightArm',(0,1,0),-72),('LeftForeArm',(0,0,1),-8),('RightForeArm',(0,0,1),8)],
       'reach_and_knee':[('LeftArm',(0,1,0),40),('RightArm',(0,1,0),-70),('LeftForeArm',(0,0,1),-70),('LeftUpLeg',(1,0,0),-25),('LeftLeg',(1,0,0),45),('Spine01',(1,0,0),5)]}
for name,items in poses.items():
    reset()
    for item in items:rotate(*item)
    bpy.context.view_layer.update()
    for view,pos in [('front',(0,-4,.9)),('side',(-4,0,.9)),('threequarter',(2.8,-3,.95))]:
        cam.location=pos;cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
        s.render.filepath=str(ev/(name+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
