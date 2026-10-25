"""Align contact candidates to actual editable folio geometry, before clip admission."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v005.blend'
if dest.exists(): raise RuntimeError('Preserve existing contact authoring')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactRig_v004.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001']; bpy.context.window.scene=s
a=bpy.data.objects['AcademicLearner_EditableRig']; body=bpy.data.objects['AcademicLearner_WeightedMesh']
folio=bpy.data.objects['Folio']; folio.location=(-.115,-.47,.815)
bpy.data.objects['FolioCoverHinge'].rotation_euler.y=math.pi
bpy.context.view_layer.update()
pen=bpy.data.objects['AnnotationPen']; right=a.pose.bones['RightHand']; rot=right.matrix.to_quaternion()
axis=(pen.matrix_world.to_quaternion()@Vector((0,0,1))).normalized()
right_target=bpy.data.objects['Right wrist contact']; desired_tip=Vector((-.065,-.435,.831))
for iteration in range(4):
 bpy.context.view_layer.update(); right.matrix=Matrix.Translation(right.head)@rot.to_matrix().to_4x4(); bpy.context.view_layer.update()
 grip=((a.matrix_world@a.pose.bones['RightThumb3'].tail)+(a.matrix_world@a.pose.bones['RightIndex2'].tail))/2
 pen.location=grip-axis*.065; bpy.context.view_layer.update(); tip=pen.matrix_world@Vector((0,0,-.012))
 if iteration<3: right_target.location+=desired_tip-tip
# A skin-surface support anchor is measured from the deformed distal finger.
left=a.pose.bones['LeftHand']; leftrot=left.matrix.to_quaternion(); left_target=bpy.data.objects['Left wrist contact']
group=body.vertex_groups['LeftMiddle3'].index
indices=[v.index for v in body.data.vertices if any(g.group==group and g.weight>.3 for g in v.groups)]
def surface_anchor():
 evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get()); mesh=evaluated.to_mesh()
 pts=sorted((evaluated.matrix_world@mesh.vertices[i].co for i in indices),key=lambda v:v.z)
 anchor=sum(pts[:5],Vector())/5; evaluated.to_mesh_clear(); return anchor
desired_support=Vector((.078,-.435,.832))
for iteration in range(4):
 bpy.context.view_layer.update(); left.matrix=Matrix.Translation(left.head)@leftrot.to_matrix().to_4x4(); bpy.context.view_layer.update()
 anchor=surface_anchor()
 if iteration<3:left_target.location+=desired_support-anchor
for name,position in [('Folio hinge contact',(-.115,-.47,.831)),('Pen tip lane',desired_tip)]:bpy.data.objects[name].location=position
s['contact_gate']='Static native folio contact candidate; exact geometry inspected before authoring motion.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
ev=R/'LocalOnly/Captures/Assets/Learner/Contacts_v005'; ev.mkdir(parents=True,exist_ok=True)
s.cycles.samples=12; s.render.threads_mode='FIXED'; s.render.threads=4; cam=s.camera
s.render.resolution_x=800; s.render.resolution_y=800
for view,pos,look,scale in [('hand_close',(.65,-1.25,1.45),(0,-.40,.91),.65),('overhead',(0,-.45,3),(0,-.45,.83),.65),('side',(-4,-.4,1.1),(0,-.2,.9),2.05),('threequarter',(2.8,-3,1.5),(0,-.25,.9),2.05)]:
 cam.data.ortho_scale=scale; cam.location=pos; cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler(); s.render.filepath=str(ev/(view+'.png')); bpy.ops.render.render(write_still=True)
report={'new_clips':0,'tip_world':list(tip),'paper_bounds_world':{'x':[-.109,.109],'y':[-.62,-.32],'top_z':.830},'tip_clearance_m':tip.z-.830,'support_surface_world':list(anchor),'support_clearance_m':anchor.z-.830,'acceptance':'Static multi-angle inspection required; no motion or Unity contact acceptance'}
(ev/'contact-audit.json').write_text(json.dumps(report,indent=2)); print(json.dumps(report))
