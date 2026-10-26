"""Consolidate owned contact takes, editable approach/recovery and Generic export candidates."""
import bpy,math,json,hashlib,sys
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
R=Path(__file__).resolve().parents[3];dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v013.blend';out=R/'SourceArt/Demo02/Exports/ContactLibrary_v013'
if dest.exists() or out.exists():raise RuntimeError('Preserve existing source/export batch')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_FolioCloseSkin_v002.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];root=bpy.data.objects['AcademicLearner_Root'];ordered=sorted(a.pose.bones,key=lambda p:len(p.parent_recursive))
for p in ordered:
 for c in p.constraints:c.mute=True
sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
from npc_contact_math import NativeNpcRig
sources={};clips={};poses={}
def offset_hand_pose(pose,side,offset):
 a.animation_data.action=None
 for p in ordered:p.matrix_basis=pose[p.name]
 bpy.context.view_layer.update();rig=NativeNpcRig(a,body,{});h=a.pose.bones[side+'Hand'];q=h.matrix.to_quaternion().copy();target=a.matrix_world@h.head+offset;rig.wrist(side,target,q,pole=a.matrix_world@a.pose.bones[side+'ForeArm'].head);bpy.context.view_layer.update();return {p.name:p.matrix_basis.copy() for p in ordered}
def load(file,name):
 path=R/'SourceArt/Demo02/Characters/Learner'/file
 if path.resolve()==Path(bpy.data.filepath).resolve():
  act=bpy.data.actions[name];sources[act.name]={'file':str(path.relative_to(R)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'action':name};return act
 with bpy.data.libraries.load(str(path),link=False) as (src,dst):
  if name not in src.actions:raise RuntimeError('Missing exact source action '+name+' in '+file)
  dst.actions=[name]
 act=dst.actions[0];sources[act.name]={'file':str(path.relative_to(R)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'action':name};return act
def sample(act,start,end):
 a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];result=[]
 for frame in range(start,end+1):s.frame_set(frame);bpy.context.view_layer.update();result.append({p.name:p.matrix_basis.copy() for p in ordered})
 return result
def bake(key,frames,origin):
 act=bpy.data.actions.new('D02_Learner_'+key+'_v013');act.use_fake_user=True;a.animation_data.action=act
 for frame,pose in enumerate(frames,1):
  for p in ordered:
   p.matrix_basis=pose[p.name];p.rotation_mode='QUATERNION'
   for channel in ['location','rotation_quaternion','scale']:p.keyframe_insert(data_path=channel,frame=frame,group=p.name)
 a.animation_data.action_slot=act.slots[0];clips[key]=act;poses[key]=frames;return act
def lerp(x,y,t):
 l0,q0,z0=x.decompose();l1,q1,z1=y.decompose();return Matrix.LocRotScale(l0.lerp(l1,t),q0.slerp(q1,t),z0.lerp(z1,t))
def smooth(t):return t*t*(3-2*t)
def transition(x,y,count):return [{p.name:lerp(x[p.name],y[p.name],smooth(i/(count-1))) for p in ordered} for i in range(count)]
original_idle=bpy.data.actions['D02_Learner_Idle_v001'];original_walk=bpy.data.actions['D02_Learner_Walk_v002'];idle_frames=sample(original_idle,int(original_idle.frame_range[0]),int(original_idle.frame_range[1]));walk_frames=sample(original_walk,int(original_walk.frame_range[0]),int(original_walk.frame_range[1]));idle=idle_frames[0];mid=sample(bpy.data.actions['D02_Learner_TableReady_v003'],15,15)[0]
bake('Idle',idle_frames,'owned idle');bake('Walk',walk_frames,'owned walk')
families=[('FolioOpen','AcademicLearner_FolioOpenSkin_v004.blend','D02_Learner_FolioOpen_skin_v004',1,16),('FolioClose','AcademicLearner_FolioCloseSkin_v004.blend','D02_Learner_FolioClose_skin_v004',1,16),('Passage','AcademicLearner_SheetTransferSkin_v004.blend','D02_Learner_SheetTransfer_skin_v004',1,19),('PageTurn','AcademicLearner_PageTurnSkin_v002.blend','D02_Learner_PageTurn_skin_v002',1,13),('EditionSlide','AcademicLearner_EditionSlideSkin_v002.blend','D02_Learner_EditionSlide_skin_v002',1,19),('PenPoint','AcademicLearner_PenPointSkin_v005.blend','D02_Learner_PenPoint_skin_v005',1,33)]
for key,file,name,start,end in families:
 act=load(file,name);frames=sample(act,start,end);bake(key,frames,file)
 # Arms first clear the near edge, then lower into the specific contact pose.
 approach=transition(idle,mid,9)+transition(mid,frames[0],12)[1:];recover=transition(frames[-1],mid,10)+transition(mid,idle,10)[1:]
 if key in ['PenPoint','EditionSlide']:
  delta=Vector((0,.776,.632)).normalized()*.12 if key=='PenPoint' else Vector((0,.10,0));edge=offset_hand_pose(frames[0],'Right',delta)
  approach=transition(idle,mid,9)+transition(mid,edge,7)[1:]+transition(edge,frames[0],6)[1:]
  if key=='PenPoint':
   entry=[offset_hand_pose(frames[0],'Right',delta*(1-smooth(i/5))) for i in range(6)];approach=transition(idle,mid,9)+transition(mid,edge,7)[1:]+entry[1:]
   exit=offset_hand_pose(frames[-1],'Right',delta);withdraw=[offset_hand_pose(frames[-1],'Right',delta*smooth(i/5)) for i in range(6)];recover=withdraw+transition(exit,mid,6)[1:]+transition(mid,idle,9)[1:]
 bake(key+'Approach',approach,file);bake(key+'Recovery',recover,file)
bake('PassageLift',poses['Passage'][:7],'Passage frames1to7');bake('PassageTransfer',poses['Passage'][6:13],'Passage frames7to13');bake('PassageRelease',poses['Passage'][12:19],'Passage frames13to19')
bake('PenApproach',poses['PenPoint'][:8],'PenPoint frames1to7');bake('MarkPoint',poses['PenPoint'][7:26],'PenPoint frames7to25');bake('PenRest',poses['PenPoint'][25:33],'PenPoint frames25to31')
bake('Stop',transition(walk_frames[0],idle,8),'Fixed-root stop settle; motor owns deceleration')
turn=[]
for frame in range(1,10):
 a.animation_data.action=None
 for p in ordered:p.matrix_basis=idle[p.name]
 u=math.sin(math.pi*(frame-1)/8);bpy.context.view_layer.update()
 for name,degrees in [('Spine02',7),('Head',9)]:
  p=a.pose.bones[name];p.matrix=Matrix.Translation(p.head)@(Quaternion(Vector((0,0,1)),math.radians(degrees)*u)@p.matrix.to_quaternion()).to_matrix().to_4x4();bpy.context.view_layer.update()
 turn.append({p.name:p.matrix_basis.copy() for p in ordered})
bake('Turn',turn,'Restrained facing anticipation; actual heading remains motor owned')
present=poses['FolioOpenApproach']+([poses['FolioOpen'][0]]*7)+transition(poses['FolioOpen'][0],mid,10)[1:]+transition(mid,idle,10)[1:]
bake('PacketPresent',present,'Present the desk-owned closed packet; no handoff or authority event')
for o in bpy.data.objects:
 if o.name in ['Folio','AnnotationPen','Selected passage sheet | editable candidate']:
  for child in [o]+list(o.children_recursive):child.hide_render=True
a.animation_data.action=clips['Idle'];a.animation_data.action_slot=clips['Idle'].slots[0];s.frame_set(1);s.frame_start=1;s.frame_end=len(idle_frames);s.render.fps=30;s['contact_library_status']='Generic source/export candidate. Original per-family contact scenes retain props and controls. No Unity or normal-speed acceptance inferred.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest));out.mkdir(parents=True);proof=R/'LocalOnly/Validation/Production/ContactLibrary_v013';proof.mkdir(parents=True,exist_ok=True);groups={g.index:g.name for g in body.vertex_groups};ids=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if any(t in groups[g.group] for t in ['Hand','Index','Thumb','Middle','Ring','Little']))>.3];audits=[]
for key,act in clips.items():
 if key in ['Idle','Walk','Stop','Turn']:continue
 a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];samples=[]
 for frame in range(1,len(poses[key])+1):
  s.frame_set(frame);bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();points=[ev.matrix_world@me.vertices[i].co for i in ids];clearance=min((p.z-.8125 for p in points if abs(p.x)<1.2 and -1.49<p.y<-.29),default=None);floor=min((ev.matrix_world@v.co).z for v in me.vertices);ev.to_mesh_clear();samples.append({'frame':frame,'hand_table_clearance_m':clearance,'floor_m':floor})
 audits.append({'clip':key,'samples':samples,'failures':[x for x in samples if x['hand_table_clearance_m'] is not None and x['hand_table_clearance_m']<-.0005 or x['floor_m']<-.002]})
(proof/'all-frame-table-audit.json').write_text(json.dumps(audits,indent=2));print('AUDIT '+json.dumps([{'clip':v['clip'],'failures':len(v['failures'])} for v in audits]),flush=True)
# Export only this owned character and the explicitly consolidated takes.
for act in list(bpy.data.actions):
 if act not in clips.values():bpy.data.actions.remove(act)
bpy.ops.object.select_all(action='DESELECT')
for o in [root,a,body]:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=a;a.animation_data.action=clips['Idle'];a.animation_data.action_slot=clips['Idle'].slots[0];s.frame_set(1);bpy.context.view_layer.update();fbx=out/'AcademicLearner_ContactLibrary_v013.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'EMPTY','ARMATURE','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='RELATIVE',embed_textures=False)
receipt={'source':str(dest.relative_to(R)),'source_sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'fbx_sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'bone_count':len(a.data.bones),'root_motion':False,'source_takes':sources,'clips':[{'key':key,'name':act.name,'frames':len(poses[key]),'duration':(len(poses[key])-1)/30} for key,act in clips.items()],'status':'Candidate export. Table audit and motion/contact visual defects block admission; Unity mapping and playback pending.'};(out/'receipt.json').write_text(json.dumps(receipt,indent=2));print('EXPORTED '+str(fbx),flush=True)






