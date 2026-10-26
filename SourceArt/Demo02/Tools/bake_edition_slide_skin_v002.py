"""M4 owned source candidate: supported historical edition slide and hand release."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(__file__).resolve().parents[3];dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_EditionSlideSkin_v002.blend'
if dest.exists():raise RuntimeError('Preserve existing authored take')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_SheetTransferSkin_v004.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];sheet=bpy.data.objects['Selected passage sheet | editable candidate']
s.frame_set(13);bpy.context.view_layer.update();mats={p.name:p.matrix.copy() for p in a.pose.bones};a.animation_data.action=None;sheet.animation_data.action=None
for p in sorted(a.pose.bones,key=lambda p:len(p.parent_recursive)):
 for c in p.constraints:c.mute=True
 kw={'parent_matrix':mats[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {};p.matrix_basis=p.bone.convert_local_to_pose(mats[p.name],p.bone.matrix_local,invert=True,**kw)
for side in ['Left','Right']:bpy.data.objects[side+' wrist contact'].location=a.matrix_world@a.pose.bones[side+'Hand'].head
lh=a.pose.bones['LeftHand'];lq=lh.matrix.to_quaternion().copy();bpy.context.view_layer.update()
ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));base={p.name:p.matrix_basis.copy() for p in ordered};hq=a.pose.bones['RightHand'].matrix.to_quaternion().copy();wrist0=a.matrix_world@a.pose.bones['RightHand'].head;sheet0=sheet.location.copy();reach=(a.pose.bones['RightArm'].length+a.pose.bones['RightForeArm'].length)*a.matrix_world.to_scale().x
left0=bpy.data.objects['Left wrist contact'].location.copy();frames=[];tracks=[]
def smooth(t):return t*t*(3-2*t)
for frame in range(1,20):
 t=(frame-1)/12;transfer=smooth(min(t,1));release=smooth(max(0,min(1,(frame-13)/6)));delta=Vector((-.05*transfer,.015*transfer,0));withdraw=Vector((0,.06*release,.05*release))
 for p in ordered:p.matrix_basis=base[p.name]
 for side in ['Left','Right']:
  for c in a.pose.bones[side+'ForeArm'].constraints:c.mute=False
 bpy.context.view_layer.update();spine=a.pose.bones['Spine02'];spine.matrix=Matrix.Translation(spine.head)@(Quaternion(Vector((0,1,0)),0)@spine.matrix.to_quaternion()).to_matrix().to_4x4();bpy.context.view_layer.update()
 bpy.data.objects['Left wrist contact'].location=left0;target=wrist0+delta+withdraw;bpy.data.objects['Right wrist contact'].location=target;bpy.context.view_layer.update()
 for name,q in [('RightHand',hq),('LeftHand',lq)]:
  h=a.pose.bones[name];h.matrix=Matrix.Translation(h.head)@q.to_matrix().to_4x4()
 bpy.context.view_layer.update();shoulder=a.matrix_world@a.pose.bones['RightArm'].head;actual=a.matrix_world@a.pose.bones['RightHand'].head
 ev=a.evaluated_get(bpy.context.evaluated_depsgraph_get());mats={p.name:p.matrix.copy() for p in ev.pose.bones};pose={}
 for p in ordered:
  kw={'parent_matrix':mats[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {};pose[p.name]=p.bone.convert_local_to_pose(mats[p.name],p.bone.matrix_local,invert=True,**kw)
 frames.append((frame,pose));tracks.append({'frame':frame,'phase':'edition slide' if frame<=13 else 'settle','sheet_position':list(sheet0+delta),'reach_margin_m':reach-(target-shoulder).length,'wrist_error_m':(actual-target).length})
 for p in ordered:
  for c in p.constraints:c.mute=True
act=bpy.data.actions.new('D02_Learner_EditionSlide_skin_v002');act.use_fake_user=True;a.animation_data.action=act
for frame,pose in frames:
 for p in ordered:
  p.matrix_basis=pose[p.name];p.rotation_mode='QUATERNION'
  for prop in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=prop,frame=frame,group=p.name)
a.animation_data.action_slot=act.slots[0];sheet.animation_data_create();propact=bpy.data.actions.new('D02_Edition_Slide_skin_v002');propact.use_fake_user=True;sheet.animation_data.action=propact
for tr in tracks:sheet.location=tr['sheet_position'];sheet.keyframe_insert(data_path='location',frame=tr['frame'])
sheet.animation_data.action_slot=propact.slots[0];s.frame_start=1;s.frame_end=19;s.render.fps=30;s.frame_set(1);bpy.context.view_layer.update();s['motion_status']='Unadmitted M4 0.4s edition slide and0.2s settle. Actual skin audit required.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest));out=R/'LocalOnly/Validation/Production/EditionSlideSkin_v002';out.mkdir(parents=True,exist_ok=True)
groups={g.index:g.name for g in body.vertex_groups};gripids={kind:[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if groups[g.group].startswith('Right'+kind))>.3] for kind in ['Index','Thumb']};handids=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if any(part in groups[g.group] for part in ['Hand','Index','Thumb','Middle','Ring','Little']))>.3];bounds=[[min(v[i] for v in sheet.bound_box),max(v[i] for v in sheet.bound_box)] for i in range(3)]
for tr in tracks:
 s.frame_set(tr['frame']);bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
 for kind,ids in gripids.items():
  distances=[];inside=[]
  for i in ids:
   p=ev.matrix_world@me.vertices[i].co;local=sheet.matrix_world.inverted()@p;ok,point,n,face=sheet.closest_point_on_mesh(local)
   if not ok:continue
   d=(p-sheet.matrix_world@point).length;distances.append(d)
   if all(bounds[j][0]<local[j]<bounds[j][1] for j in range(3)) and (local-point).dot(n)<-1e-6:inside.append(d)
  tr[kind.lower()]={'nearest_m':min(distances),'inside':len(inside),'penetration_m':max(inside,default=0)}
 points=[ev.matrix_world@me.vertices[i].co for i in handids];tr['table_min_clearance_m']=min((p.z-.8125 for p in points if abs(p.x)<1.2 and -1.49<p.y<-.29),default=None);ev.to_mesh_clear()
(out/'all-frame-skin-audit.json').write_text(json.dumps({'duration':.6,'frames':tracks,'status':'Actual sampled skin audit, not Unity playback acceptance'},indent=2));print(json.dumps(tracks),flush=True)
s.render.resolution_x=s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=4;s.cycles.samples=10
for frame in [1,13,19]:
 s.frame_set(frame);look=Vector((-.12,-.34,.99));s.camera.location=look+Vector((-.6,-.6,.35));s.camera.rotation_euler=(look-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=1.15;s.render.filepath=str(out/f'contact-{frame:02}.png');bpy.ops.render.render(write_still=True)






