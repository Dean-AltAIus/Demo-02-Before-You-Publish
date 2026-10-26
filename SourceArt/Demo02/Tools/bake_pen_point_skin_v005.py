"""M3 source pointing take. Pen shares the actual fitted hand displacement; no generated writing."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[3];dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_PenPointSkin_v005.blend'
if dest.exists():raise RuntimeError('Preserve authored source')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_PenPointGrip_v006.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];pen=bpy.data.objects['AnnotationPen'];bpy.context.view_layer.update();ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive));base={p.name:p.matrix_basis.copy() for p in ordered};hq=a.pose.bones['RightHand'].matrix.to_quaternion().copy();wrist0=bpy.data.objects['Right wrist contact'].location.copy();pen0=pen.location.copy();frames=[];tracks=[]
def smooth(t):return t*t*(3-2*t)
direction=pen.matrix_world.to_quaternion()@Vector((0,0,1))
def arrival(u):
 rest=Vector((-.13,0,.035))
 if u<=1/3:return rest+direction*(.018*smooth(u*3))
 v=smooth((u-1/3)*1.5);return (rest+direction*.018)*(1-v)+Vector((0,0,.022*math.sin(math.pi*v)))
for frame in range(1,34):
 t=(frame-1)/30
 if frame<=8:delta=arrival((frame-1)/7)+Vector((0,0,.0004));phase='approach'
 elif frame<=26:delta=Vector((.002*math.sin(math.pi*(frame-8)/18),0,.0004));phase='point'
 else:delta=arrival(1-(frame-26)/7)+Vector((0,0,.0004));phase='rest'
 for p in ordered:p.matrix_basis=base[p.name]
 for side in ['Left','Right']:
  for c in a.pose.bones[side+'ForeArm'].constraints:c.mute=False
 bpy.data.objects['Right wrist contact'].location=wrist0+delta;bpy.context.view_layer.update();hand=a.pose.bones['RightHand'];hand.matrix=Matrix.Translation(hand.head)@hq.to_matrix().to_4x4();bpy.context.view_layer.update();ev=a.evaluated_get(bpy.context.evaluated_depsgraph_get());mats={p.name:p.matrix.copy() for p in ev.pose.bones};pose={}
 for p in ordered:
  kw={'parent_matrix':mats[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {};pose[p.name]=p.bone.convert_local_to_pose(mats[p.name],p.bone.matrix_local,invert=True,**kw)
 frames.append((frame,pose));tracks.append({'frame':frame,'phase':phase,'pen_position':list(pen0+delta),'wrist_error_m':((a.matrix_world@hand.head)-(wrist0+delta)).length})
 for p in ordered:
  for c in p.constraints:c.mute=True
# Editable angled nib socket with a real empty interior; its bottom supports the resting pen.
verts=[];faces=[]
for z,r in [(0,.004),(0,.009),(.008,.004),(.008,.009)]:
 for i in range(32):verts.append((r*math.cos(i*math.tau/32),r*math.sin(i*math.tau/32),z))
for i in range(32):
 j=(i+1)%32
 for a0,b0,c0,d0 in [(0,32,32,0),(64,96,96,64),(0,64,64,0),(32,96,96,32)]:faces.append((a0+i,b0+i,c0+j,d0+j))
mesh=bpy.data.meshes.new('Pen socket editable annulus');mesh.from_pydata(verts,[],faces);cup=bpy.data.objects.new('Pen rest angled socket | editable',mesh);s.collection.objects.link(cup);cup.location=Vector((-.239,-.353,.855412));cup.rotation_euler=pen.rotation_euler
bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.013,depth=.0409,location=(-.239,-.353,.83295));stem=bpy.context.object;stem.name='Pen rest brass support | editable'
act=bpy.data.actions.new('D02_Learner_PenPoint_skin_v005');act.use_fake_user=True;a.animation_data.action=act
for frame,pose in frames:
 for p in ordered:
  p.matrix_basis=pose[p.name];p.rotation_mode='QUATERNION'
  for prop in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=prop,frame=frame,group=p.name)
a.animation_data.action_slot=act.slots[0];pen.animation_data_create();prop=bpy.data.actions.new('D02_Pen_ApproachPointRest_skin_v005');prop.use_fake_user=True;pen.animation_data.action=prop
for tr in tracks:pen.location=tr['pen_position'];pen.keyframe_insert(data_path='location',frame=tr['frame'])
pen.animation_data.action_slot=prop.slots[0];s.frame_start=1;s.frame_end=33;s.render.fps=30;s.frame_set(1);bpy.context.view_layer.update();s['motion_status']='Unadmitted M3 restrained point:0.2s approach,0.6s contact,0.2s rest. Literal native text owns annotation, no artificial handwriting.';bpy.ops.wm.save_as_mainfile(filepath=str(dest));out=R/'LocalOnly/Validation/Production/PenPointSkin_v005';out.mkdir(parents=True,exist_ok=True);groups={g.index:g.name for g in body.vertex_groups};ids=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if any(term in groups[g.group] for term in ['Hand','Index','Thumb','Middle','Ring','Little']))>.3];solids=[o for o in pen.children_recursive if o.type=='MESH'];bounds={o.name:[[min(v[i] for v in o.bound_box),max(v[i] for v in o.bound_box)] for i in range(3)] for o in solids}
for tr in tracks:
 s.frame_set(tr['frame']);bpy.context.view_layer.update();tip=min((o.matrix_world@v.co for o in solids for v in o.data.vertices),key=lambda p:p.z);ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();points=[ev.matrix_world@me.vertices[i].co for i in ids];inside=[]
 for p in points:
  for o in solids:
   local=o.matrix_world.inverted()@p;ok,q,n,face=o.closest_point_on_mesh(local);b=bounds[o.name]
   if ok and all(b[j][0]<local[j]<b[j][1] for j in range(3)) and (local-q).dot(n)<-1e-6:inside.append((p-o.matrix_world@q).length)
 ev.to_mesh_clear();tr['tip_clearance_m']=tip.z-.8202;tr['hand_table_clearance_m']=min(p.z-.8125 for p in points if abs(p.x)<1.2 and -1.49<p.y<-.29);tr['pen_inside_vertices']=len(inside);tr['maximum_pen_penetration_m']=max(inside,default=0)
(out/'all-frame-skin-audit.json').write_text(json.dumps({'frames':tracks,'status':'Actual native skin and tip audit; Unity playback and ownership transitions pending'},indent=2));print('Finished31sampleaudit',flush=True)
s.render.resolution_x=s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=4;s.cycles.samples=10
for frame in [1,8,17,33]:
 s.frame_set(frame);look=Vector((-.109,-.32,.875));s.camera.location=look+Vector((.5,-.25,.15));s.camera.rotation_euler=(look-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=.46;s.render.filepath=str(out/f'contact-{frame:02}.png');bpy.ops.render.render(write_still=True)



