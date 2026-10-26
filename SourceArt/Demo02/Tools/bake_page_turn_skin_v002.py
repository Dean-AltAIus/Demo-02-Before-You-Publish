"""Native M4 page turn from the fitted skin contact. Presentation curves have no domain callbacks."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(__file__).resolve().parents[3];dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_PageTurnSkin_v002.blend'
if dest.exists():raise RuntimeError('Preserve existing authored take')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_SheetGrip_v001.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];sheet=bpy.data.objects['Selected passage sheet | editable candidate'];cover=bpy.data.objects.new('Page turn spine | editable hinge',None);s.collection.objects.link(cover);cover.location=(.1,-.4215,.831);bpy.context.view_layer.update();old=sheet.matrix_world.copy();sheet.parent=cover;sheet.matrix_world=old;bpy.context.view_layer.update()
left_hand=a.pose.bones['LeftHand'];left_q=left_hand.matrix.to_quaternion().copy();left_target=bpy.data.objects['Left wrist contact'];left_target.location.y+=.04;bpy.context.view_layer.update();left_hand.matrix=Matrix.Translation(left_hand.head)@left_q.to_matrix().to_4x4();bpy.context.view_layer.update()
ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));base={p.name:p.matrix_basis.copy() for p in ordered};hand=a.pose.bones['RightHand'];wrist0=a.matrix_world@hand.head;rotation0=(a.matrix_world@hand.matrix).to_quaternion();hinge=cover.matrix_world.translation.copy();shoulder=a.matrix_world@a.pose.bones['RightArm'].head;reach=(a.pose.bones['RightArm'].length+a.pose.bones['RightForeArm'].length)*a.matrix_world.to_scale().x
safe=0
for angle in range(1,101):
 target=hinge+Quaternion(Vector((0,1,0)),math.radians(angle))@(wrist0-hinge)
 if reach-(target-shoulder).length<.008:break
 safe=angle
if safe<85:raise RuntimeError('Opening requires a new reachable stance, maximum supported angle '+str(safe))
safe=80;frames=[];tracks=[]
def smooth(t):return t*t*(3-2*t)
for frame in range(1,14):
 t=(frame-1)/12;angle=180*smooth(t);held=angle<=safe;q=Quaternion(Vector((0,1,0)),math.radians(min(angle,100)));target=hinge+q@(wrist0-hinge)
 release=max(0,(angle-100)/80);target+=Vector((0,.07*smooth(max(0,min(1,(angle-80)/20))),0))+Vector((-.10,.01,.08))*smooth(release)
 for p in ordered:p.matrix_basis=base[p.name]
 for side in ['Left','Right']:
  for c in a.pose.bones[side+'ForeArm'].constraints:c.mute=False
 bpy.data.objects['Right wrist contact'].location=target;bpy.context.view_layer.update();hand=a.pose.bones['RightHand'];hq=a.matrix_world.to_quaternion().inverted()@q@rotation0;hand.matrix=Matrix.Translation(hand.head)@hq.to_matrix().to_4x4();bpy.context.view_layer.update()
 if release>0:
  # Open the fingers only after the cover has left their contact phase.
  for name in ['RightIndex1','RightIndex2','RightIndex3','RightThumb1','RightThumb2','RightThumb3']:
   p=a.pose.bones[name];p.rotation_mode='QUATERNION';p.rotation_quaternion=p.rotation_quaternion.slerp(Quaternion(),smooth(release)*.6)
  bpy.context.view_layer.update()
 ev=a.evaluated_get(bpy.context.evaluated_depsgraph_get());mats={p.name:p.matrix.copy() for p in ev.pose.bones};pose={}
 for p in ordered:
  kw={'parent_matrix':mats[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {};pose[p.name]=p.bone.convert_local_to_pose(mats[p.name],p.bone.matrix_local,invert=True,**kw)
 frames.append((frame,pose));tracks.append({'frame':frame,'cover_degrees':angle,'grip':held,'reach_margin_m':reach-(target-shoulder).length,'wrist_error_m':((a.matrix_world@a.pose.bones['RightHand'].head)-target).length})
 for p in ordered:
  for c in p.constraints:c.mute=True
if min(tr['reach_margin_m'] for tr in tracks)<.005:raise RuntimeError('Baked opening exceeds chain reach')
act=bpy.data.actions.new('D02_Learner_PageTurn_skin_v002');act.use_fake_user=True;a.animation_data.action=act
for frame,pose in frames:
 for p in ordered:
  p.matrix_basis=pose[p.name];p.rotation_mode='QUATERNION'
  for prop in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=prop,frame=frame,group=p.name)
a.animation_data.action_slot=act.slots[0];cover.animation_data_create();propact=bpy.data.actions.new('D02_Page_Turn_skin_v002');propact.use_fake_user=True;cover.animation_data.action=propact
for tr in tracks:
 cover.rotation_euler.y=math.radians(tr['cover_degrees']);cover.keyframe_insert(data_path='rotation_euler',frame=tr['frame'])
 cover.location.z=.831+.0012*smooth(max(0,(tr['frame']-8)/5));cover.keyframe_insert(data_path='location',frame=tr['frame'])
cover.animation_data.action_slot=propact.slots[0];s.frame_start=1;s.frame_end=13;s.render.fps=30;s.frame_set(1);bpy.context.view_layer.update()
s['motion_status']='Unadmitted M4 page turn, 0.4 seconds at 30fps. Fitted edge grip follows hinge until measured reach limit, then releases. Authority never follows animation.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest));out=R/'LocalOnly/Validation/Production/PageTurnSkin_v002';out.mkdir(parents=True,exist_ok=True)
groups={g.index:g.name for g in body.vertex_groups};gripids={kind:[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if groups[g.group].startswith('Right'+kind))>.3] for kind in ['Index','Thumb']};handids=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if any(part in groups[g.group] for part in ['Hand','Index','Thumb','Middle','Ring','Little']))>.3];solid=next(o for o in cover.children_recursive if o.type=='MESH');bounds=[[min(v[i] for v in solid.bound_box),max(v[i] for v in solid.bound_box)] for i in range(3)]
for tr in tracks:
 s.frame_set(tr['frame']);bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
 for kind,ids in gripids.items():
  distances=[];inside=[]
  for i in ids:
   p=ev.matrix_world@me.vertices[i].co;local=solid.matrix_world.inverted()@p;ok,point,n,face=solid.closest_point_on_mesh(local)
   if not ok:continue
   d=(p-solid.matrix_world@point).length;distances.append(d)
   if all(bounds[j][0]<local[j]<bounds[j][1] for j in range(3)) and (local-point).dot(n)<-1e-6:inside.append(d)
  tr[kind.lower()]={'nearest_m':min(distances),'inside':len(inside),'penetration_m':max(inside,default=0)}
 points=[ev.matrix_world@me.vertices[i].co for i in handids];tr['table_min_clearance_m']=min((p.z-.8125 for p in points if abs(p.x)<1.2 and -1.49<p.y<-.29),default=None);ev.to_mesh_clear()
(out/'all-frame-skin-audit.json').write_text(json.dumps({'safe_grip_degrees':safe,'duration':.4,'frames':tracks,'status':'Actual sampled skin audit, not Unity playback acceptance'},indent=2));print(json.dumps(tracks),flush=True)
s.render.resolution_x=s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=4;s.cycles.samples=10
for frame in [1,6,8,13]:
 s.frame_set(frame);look=Vector((.04,-.34,.99));s.camera.location=look+Vector((-.6,-.6,.35));s.camera.rotation_euler=(look-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=.85;s.render.filepath=str(out/f'contact-{frame:02}.png');bpy.ops.render.render(write_still=True)




