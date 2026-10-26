"""Audit the consolidated learner skin through approach, actual contact and withdrawal.
Uses the reconciled local-volume validator without changing its NPC-owned source.
"""
import bpy,json,sys,hashlib,argparse,math
from mathutils import Matrix,Vector
from mathutils.bvhtree import BVHTree
from pathlib import Path
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'SourceArt/Demo02/Tools'))
parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output-dir',type=Path,default=R/'LocalOnly/Validation/Production/ContactLibrary_v013')
parser.add_argument('--master',type=Path,default=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v013.blend');parser.add_argument('--families',nargs='*');parser.add_argument('--substeps',type=int,choices=[1,2,4,8,16],default=1);parser.add_argument('--prop-interpolation',choices=['source','runtime'],default='source');parser.add_argument('--torso',action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []);out=args.output_dir.resolve()
if out.exists():raise RuntimeError('Preserve earlier contact audit: '+str(out))
from npc_contact_math import NativeNpcRig
master=(R/args.master).resolve();reports=[]
families=[('FolioOpen','FolioOpenSkin_v004'),('FolioClose','FolioCloseSkin_v004'),('Passage','SheetTransferSkin_v004'),('PageTurn','PageTurnSkin_v002'),('EditionSlide','EditionSlideSkin_v002'),('PenPoint','PenPointSkin_v005')]
if args.families:
 if not set(args.families).issubset({x[0] for x in families}):raise RuntimeError('Unknown contact family')
 families=[x for x in families if x[0] in args.families]
def frame_set(frame):s.frame_set(int(frame),subframe=frame-int(frame))
for family,suffix in families:
 path=R/'SourceArt/Demo02/Characters/Learner'/('AcademicLearner_'+suffix+'.blend');bpy.ops.wm.open_mainfile(filepath=str(path));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];old=bpy.data.objects['AcademicLearner_WeightedMesh'];main_frames=s.frame_end
 solids=[o for o in s.objects if o.type=='MESH' and not o.hide_render and (o.name in ['Front cover','Back cover','Thin paper stack','Selected passage sheet | editable candidate','Selected criterion lane board | editable'] or o.name.startswith('Pen rest') or o.parent and o.parent.name=='AnnotationPen')]
 # Read props before any audit-time world-matrix overrides. Runtime uses position
 # Lerp and quaternion Slerp of the independent 30 Hz prop keys.
 prop_root=bpy.data.objects.get('FolioCoverHinge') if family.startswith('Folio') else None
 prop_cache={};root_keys={};root_relatives={}
 if args.prop_interpolation=='runtime' and prop_root is None:raise RuntimeError('Runtime interpolation currently scoped to folio families')
 for i in range((main_frames-1)*args.substeps+1):
  frame=1+i/args.substeps;frame_set(frame);bpy.context.view_layer.update();prop_cache[frame]={o:o.matrix_world.copy() for o in solids}
  if prop_root and frame==int(frame):root_keys[int(frame)]=prop_root.matrix_world.copy()
 if prop_root:
  frame_set(1);bpy.context.view_layer.update();root_relatives={o:prop_root.matrix_world.inverted()@o.matrix_world for o in solids if o in prop_root.children_recursive}
 if args.prop_interpolation=='runtime':
  for frame,matrices in prop_cache.items():
   left=int(frame);right=min(main_frames,left+1);t=frame-left;p,q,scale=root_keys[left].decompose();p2,q2,scale2=root_keys[right].decompose();root_matrix=Matrix.LocRotScale(p.lerp(p2,t),q.slerp(q2,t),scale.lerp(scale2,t))
   for o,relative in root_relatives.items():matrices[o]=root_matrix@relative
 names=['D02_Learner_'+family+end+'_v013' for end in ['Approach','','Recovery']]
 with bpy.data.libraries.load(str(master),link=False) as (src,dst):dst.actions=names;dst.objects=['AcademicLearner_WeightedMesh']
 body=dst.objects[0];s.collection.objects.link(body);world=old.matrix_world.copy();body.parent=old.parent;body.matrix_world=world;old.hide_render=True
 for mod in body.modifiers:
  if mod.type=='ARMATURE':mod.object=a
 for p in a.pose.bones:
  for c in p.constraints:c.mute=True
 rig=NativeNpcRig(a,body,{});ids=rig.hand_ids('Left',threshold=.3)+rig.hand_ids('Right',threshold=.3);rows=[]
 deform={b.name for b in a.data.bones if b.use_deform};weights=[]
 for v in body.data.vertices:
  w={body.vertex_groups[g.group].name:g.weight for g in v.groups if body.vertex_groups[g.group].name in deform};total=sum(w.values());weights.append({n:x/total for n,x in w.items()})
 torso_side='Left' if family=='FolioClose' else 'Right';torso_ids={i for i,w in enumerate(weights) if sum(v for n,v in w.items() if n.startswith('Spine'))>.65 and sum(v for n,v in w.items() if 'Arm' in n)<.05};arm_ids=[i for i,w in enumerate(weights) if w.get(torso_side+'ForeArm',0)>.65]
 def extra_skin_audit():
  ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();verts=[ev.matrix_world@v.co for v in mesh.vertices];depths=[]
  if args.torso:
   faces=[list(p.vertices) for p in mesh.polygons if all(i in torso_ids for i in p.vertices)];tree=BVHTree.FromPolygons(verts,faces)
   for i in arm_ids:
    v=verts[i];front,_,_,_=tree.ray_cast(Vector((v.x,-3,v.z)),Vector((0,1,0)),6);back,_,_,_=tree.ray_cast(Vector((v.x,3,v.z)),Vector((0,-1,0)),6)
    if front is not None and back is not None and back.y-front.y>.04 and front.y+.0001<v.y<back.y-.0001:depths.append(v.y-front.y)
  clearance=min((verts[i].z-.8125 for i in ids if abs(verts[i].x)<1.2 and -1.49<verts[i].y<-.29),default=None);ev.to_mesh_clear();return {'working_forearm':torso_side,'scoped_forearm_torso_inside':len(depths),'maximum_depth_y_m':max(depths,default=0),'table_min_clearance_m':clearance}
 # Cache native prop matrices before replacing the actor action; their animation remains independently editable.
 prop_actions={o:o.animation_data.action for o in s.objects if o.animation_data and o.animation_data.action and o!=a}
 static_locals={o:o.matrix_basis.copy() for o in solids if not (o.animation_data and o.animation_data.action)}
 for phase,act in zip(['Approach','Contact','Recovery'],dst.actions):
  for i in range(int((act.frame_range[1]-act.frame_range[0])*args.substeps)+1):
   frame=act.frame_range[0]+i/args.substeps
   for o,m in static_locals.items():o.matrix_basis=m
   prop_frame=1 if phase=='Approach' else main_frames if phase=='Recovery' else frame
   prop_matrices=prop_cache[prop_frame]
   a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];frame_set(frame);bpy.context.view_layer.update()
   # Freeze each physical solid at the matching source contact boundary during entry/withdrawal.
   for o,m in prop_matrices.items():o.matrix_world=m
   bpy.context.view_layer.update();audit=rig.audit_objects(solids,ids=ids,penetration_only=True)
   failed=[v for v in audit if v['inside_vertex_count'] or v['ray_disagreements']]
   rows.append({'phase':phase,'frame':frame,'inside_vertices':sum(v['inside_vertex_count'] for v in audit),'failures':failed,'skin_extra':extra_skin_audit()})
   for o,m in static_locals.items():o.matrix_basis=m
  print(family+' '+phase+' complete',flush=True)
 reports.append({'family':family,'source':str(path.relative_to(R)),'samples':rows})
out.mkdir(parents=True)
(out/'transition-volume-audit-v002.json').write_text(json.dumps({'master':str(master.relative_to(R)),'master_sha256':hashlib.sha256(master.read_bytes()).hexdigest(),'prop_interpolation':args.prop_interpolation,'substeps':args.substeps,'validator_sha256':hashlib.sha256((R/'SourceArt/Demo02/Tools/npc_contact_math.py').read_bytes()).hexdigest(),'reports':reports},indent=2));print(json.dumps([{'family':r['family'],'samples':len(r['samples']),'failed_samples':sum(bool(x['failures']) for x in r['samples']),'inside_vertices':sum(x['inside_vertices'] for x in r['samples'])} for r in reports]),flush=True)




