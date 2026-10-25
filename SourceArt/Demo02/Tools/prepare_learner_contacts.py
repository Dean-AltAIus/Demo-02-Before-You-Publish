"""Editable two-arm contact rig pilot, static inspection before interaction clips."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v001.blend'
if dest.exists():raise RuntimeError('Preserve existing contact authoring')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Motion_v002.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;s.frame_set(1)
a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh']
for action in bpy.data.actions:action.use_fake_user=True
idle=bpy.data.actions['D02_Learner_Idle_v001'];a.animation_data.action=idle;a.animation_data.action_slot=idle.slots[0];s.frame_set(1);bpy.context.view_layer.update()
basis={p.name:p.matrix_basis.copy() for p in a.pose.bones};a.animation_data_clear()
for p in a.pose.bones:p.matrix_basis=basis[p.name]
def rotate(name,axis,degrees):
 p=a.pose.bones[name];q=p.bone.matrix_local.to_quaternion();p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(degrees))@q
for n,angle in [('Spine02',6),('Spine01',3),('Head',6)]:rotate(n,(1,0,0),angle)
controls=bpy.data.collections.new('Editable contact targets');s.collection.children.link(controls)
def target(name,position):
 o=bpy.data.objects.new(name,None);controls.objects.link(o);o.location=position;o.empty_display_type='SPHERE';o.empty_display_size=.025;return o
targets={}
for side,sign in [('Left',1),('Right',-1)]:
 wrist=target(side+' wrist contact',(.16*sign,-.29 if sign==1 else -.32,.96))
 pole=target(side+' elbow pole',(.48*sign,-.06,1.13))
 c=a.pose.bones[side+'ForeArm'].constraints.new('IK');c.name='Editable two-bone table reach';c.target=wrist;c.pole_target=pole;c.chain_count=2;c.iterations=64;c.use_tail=True;c.use_rotation=False
 targets[side]=wrist
bpy.context.view_layer.update()
for side,sign in [('Left',1),('Right',-1)]:
 p=a.pose.bones[side+'Hand'];world_rest=(a.matrix_world@p.bone.matrix_local).to_quaternion()
 wanted=Quaternion(Vector((1,0,0)),math.radians(35))@Quaternion(Vector((0,0,1)),math.radians(-90*sign))@world_rest
 wanted_local=a.matrix_world.to_quaternion().inverted()@wanted
 p.matrix=Matrix.Translation(p.head)@wanted_local.to_matrix().to_4x4()
bpy.context.view_layer.update()
# Keep the small relaxed finger curl that passed the earlier static hand gate.
with bpy.data.libraries.load(str(R/'SourceArt/Demo02/Environment/CampusKit_v001.blend'),link=False) as (source,data):
 data.collections=['D02_StudioDesk','D02_Folio','D02_AnnotationPen']
for collection in data.collections:s.collection.children.link(collection)
bpy.data.objects['StudioDesk'].location=(0,-.77,0)
bpy.data.objects['Folio'].location=(.02,-.47,.815)
pen=bpy.data.objects['AnnotationPen'];pen.location=(-.12,-.40,.835);pen.rotation_euler=(math.radians(32),math.radians(-18),0)
target('Folio hinge contact',(-.095,-.47,.832))['production_id']='D02.ASSET.CONTACT.FOLIO_HINGE_FINGERTIPS'
target('Pen tip lane',(-.12,-.40,.835))['production_id']='D02.ASSET.CONTACT.PEN_TIP_LANE'
s['contact_gate']='Static contact pose pilot only. Do not claim fingertip/pen contact or author interaction clips until views corrected and inspected.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
ev=R/'LocalOnly/Captures/Assets/Learner/Contacts_v001';ev.mkdir(parents=True,exist_ok=True)
s.render.resolution_x=640;s.render.resolution_y=800;s.render.threads_mode='FIXED';s.render.threads=4;s.cycles.samples=12;cam=s.camera
for view,pos,look,scale in [
 ('front',(0,-4,1.1),(0,-.2,.9),2.05),('side',(-4,-.4,1.1),(0,-.2,.9),2.05),
 ('threequarter',(2.8,-3,1.5),(0,-.25,.9),2.05),('hand_close',(.7,-1.3,1.6),(0,-.38,.91),.62)]:
 cam.data.ortho_scale=scale;cam.location=pos;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(view+'.png'));bpy.ops.render.render(write_still=True)
(ev/'contact-audit.json').write_text(json.dumps({'type':'Static contact candidate only','new_clips':0,'wrists':{side:{'target':list(w.location),'actual':list(a.matrix_world@a.pose.bones[side+'Hand'].head)} for side,w in targets.items()},'source_preserved':True},indent=2))
