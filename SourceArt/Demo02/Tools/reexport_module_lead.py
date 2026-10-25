"""Validate or re-export the saved reviewer without local-only input fixtures.

Run in isolated Blender background mode. Default export paths are immutable;
use an explicit new output directory after intentional native edits. The native
blend is read only. All prop tracks and boundaries are sampled afresh.
"""
import argparse,bpy,hashlib,json,math,re,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from npc_contact_math import NativeNpcRig

R=Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser()
parser.add_argument('--version',default='v019',help='Explicit saved source/action version, for example v020')
parser.add_argument('--gesture-substeps',type=int,choices=[1,2,4,8],default=1,
                    help='Sample point and recovery between keys for contact and rotation continuity')
parser.add_argument('--validate-only',action='store_true')
parser.add_argument('--output-dir',type=Path)
parser.add_argument('--evidence-dir',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
version=args.version
if not re.fullmatch(r'v[0-9]{3}',version):raise RuntimeError('Expected a bounded source version such as v019')
source=R/'SourceArt/Demo02/Characters/ModuleLead'/('ModuleLead_Interactions_'+version+'.blend')
controls=json.loads(source.with_suffix('.controls.json').read_text())
out=(args.output_dir or R/'SourceArt/Demo02/Exports/NPCInteractions_v001').resolve()
evidence=args.evidence_dir.resolve();fbx=out/('ModuleLead_Interactions_'+version+'.fbx')
export_paths=[fbx,out/('ModuleLead_Interactions_'+version+'.packet-tracks.json'),out/('ModuleLead_Interactions_'+version+'.receipt.json')]
if (evidence.exists() and any(evidence.iterdir())) or (not args.validate_only and any(p.exists() for p in export_paths)):raise RuntimeError('Preserve existing source/export evidence; choose a new destination')
evidence.mkdir(parents=True,exist_ok=True)
before=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_ModuleLeadRig_v001'];bpy.context.window.scene=s
if s.render.fps!=controls['fps'] or s.render.fps_base!=1:raise RuntimeError('Review clip timing before changing the 30 fps contract')
a=bpy.data.objects['ModuleLead_EditableRig'];body=bpy.data.objects['ModuleLead_WeightedMesh'];folio=bpy.data.objects['Folio'];hinge=bpy.data.objects['FolioCoverHinge'];objects=[a,folio,hinge]
rig=NativeNpcRig(a,body,{})
props=[bpy.data.objects[n] for n in ['Front cover','Back cover','Thin paper stack']]
table=next(o for o in bpy.data.objects['ReviewDesk'].children_recursive if o.name.startswith('Oak worktop'))
all_ids=list(range(len(body.data.vertices)));foot_base=None
rows=[];bad=[];tracks=[];ends={};max_foot=0
def matrices():return {p.name:(a.matrix_world@p.matrix).copy() for p in a.pose.bones}|{'Packet':folio.matrix_world.copy(),'Cover':hinge.matrix_world.copy()}
def matrix_error(first,second):return max(abs(m[i][j]-second[n][i][j]) for n,m in first.items() for i in range(4) for j in range(4))
def xyzw(q):return [q.x,q.y,q.z,q.w]
for take in controls['takes']:
    for obj,suffix in zip(objects,['','_PacketRoot','_Cover']):
        action=bpy.data.actions['D02_ModuleLead_'+take+suffix+'_'+version];obj.animation_data.action=action;obj.animation_data.action_slot=action.slots[0]
    frame_range=tuple(a.animation_data.action.frame_range)
    if any(abs(x-round(x))>1e-5 for x in frame_range):raise RuntimeError('Review fractional-frame resampling explicitly: '+take)
    if any(tuple(obj.animation_data.action.frame_range)!=frame_range for obj in objects):raise RuntimeError('Actor and packet action ranges differ: '+take)
    first,last=map(int,frame_range)
    if first!=1 or last<2:raise RuntimeError('Clip needs an explicit frame1 start and a non-empty duration: '+take)
    take_frames=[];failures=0
    for frame in range(first,last+1):
        s.frame_set(frame);bpy.context.view_layer.update()
        if foot_base is None:foot_base={n:(a.matrix_world@a.pose.bones[n].matrix).translation.copy() for n in ['LeftFoot','RightFoot']}
        checks=rig.audit_objects(props)+rig.audit_objects([table],ids=all_ids,penetration_only=True)
        if any(r['inside_vertex_count'] or r['ray_disagreements'] for r in checks):bad.append({'take':take,'frame':frame,'checks':checks});failures+=1
        max_foot=max(max_foot,max(((a.matrix_world@a.pose.bones[n].matrix).translation-foot_base[n]).length for n in foot_base))
        take_frames.append({'frame':frame,'seconds':(frame-1)/controls['fps'],'packet_position':list(folio.location),'packet_rotation_xyzw':xyzw(folio.rotation_quaternion),'cover_rotation_xyzw':xyzw(hinge.rotation_quaternion)})
        if frame==first:ends[take]={'first':matrices()}
        if frame==last:ends[take]['last']=matrices()
    rows.append({'take':take,'frames':len(take_frames),'failed_frames':failures});tracks.append({'take':take,'fps':controls['fps'],'frames':take_frames})
    print('SAVED_SOURCE_CHECK='+json.dumps(rows[-1]),flush=True)
boundaries=[]
for start,end in zip(controls['cycle'],controls['cycle'][1:]):boundaries.append({'from':start,'to':end,'maximum_world_matrix_error':matrix_error(ends[start]['last'],ends[end]['first'])})
for take in ['SeatedIdle','SeatedListen','SeatedSpeak']:
    for endpoint in ['first','last']:boundaries.append({'from':take+'.'+endpoint,'to':'SeatedIdle.first','maximum_world_matrix_error':matrix_error(ends[take][endpoint],ends['SeatedIdle']['first'])})
interpolation={'substeps':args.gesture_substeps,'sample_rate_hz':controls['fps']*args.gesture_substeps,'samples':0,'failed_samples':0,'maximum_bone_rotation_step_degrees':0,'maximum_bone_rotation_speed_degrees_per_second':0,'failures':[]}
if args.gesture_substeps>1:
    for take in ['PointCurrentPair','RecoverPoint']:
        for obj,suffix in zip(objects,['','_PacketRoot','_Cover']):
            action=bpy.data.actions['D02_ModuleLead_'+take+suffix+'_'+version];obj.animation_data.action=action;obj.animation_data.action_slot=action.slots[0]
        first,last=map(int,a.animation_data.action.frame_range);previous=None
        for step in range((last-first)*args.gesture_substeps+1):
            frame=first+step/args.gesture_substeps;s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
            pose={p.name:p.matrix.to_quaternion().normalized() for p in a.pose.bones}
            if previous is not None:
                angle=max(math.degrees(2*math.acos(min(1,abs(q.dot(previous[n]))))) for n,q in pose.items())
                interpolation['maximum_bone_rotation_step_degrees']=max(interpolation['maximum_bone_rotation_step_degrees'],angle)
            previous=pose
            checks=rig.audit_objects(props)+rig.audit_objects([table],ids=all_ids,penetration_only=True)
            failures=[r for r in checks if r['inside_vertex_count'] or r['ray_disagreements']]
            interpolation['samples']+=1
            if failures:interpolation['failed_samples']+=1;interpolation['failures'].append({'take':take,'frame':frame,'checks':failures})
    interpolation['maximum_bone_rotation_speed_degrees_per_second']=interpolation['maximum_bone_rotation_step_degrees']*interpolation['sample_rate_hz']
    interpolation['speed_gate_note']='3000 degrees/second is an interpolation-spike diagnostic ceiling, not a visual or biomechanical quality verdict.'
validation={'source':source.relative_to(R).as_posix(),'source_sha256':before,'takes':rows,'failures':bad,'max_foot_drift_m':max_foot,'boundaries':boundaries,'gesture_interpolation':interpolation,'source_unchanged':hashlib.sha256(source.read_bytes()).hexdigest()==before,'input_scope':'Saved native source and tracked controls only; no LocalOnly fixture is read.','status':'Native sampled checks, not Unity gameplay or continuous collision acceptance.'}
(evidence/'saved-source-validation.json').write_text(json.dumps(validation,indent=2)+'\n',encoding='utf-8')
if bad or interpolation['failed_samples'] or interpolation['maximum_bone_rotation_speed_degrees_per_second']>3000 or max_foot>1e-5 or any(b['maximum_world_matrix_error']>.0001 for b in boundaries) or not validation['source_unchanged']:raise RuntimeError('Saved-source validation failed')
packet={'source':validation['source'],'source_sha256':before,'coordinates':'Blender native metres, Z up, actor faces negative Y. Positions relative to actor origin. Quaternion arrays XYZW.','point_target_native_m':controls['point_target_native_m'],'tracks':tracks,'ownership_notes':controls['ownership_notes'],'authority':'Presentation only. No animation callback supplies a domain decision, receipt, approval, evidence or request.'}
(evidence/'sampled-packet-tracks.json').write_text(json.dumps(packet,indent=2)+'\n',encoding='utf-8')
if args.validate_only:
    print('VALIDATE_ONLY_PASSED: '+str(sum(r['frames'] for r in rows))+' frames; source and exports untouched.',flush=True)
    sys.exit(0)
names=['D02_ModuleLead_'+take+'_'+version for take in controls['takes']]
for obj in [folio,hinge]:obj.animation_data.action=None
for action in list(bpy.data.actions):
    if action.name not in names:bpy.data.actions.remove(action)
action=bpy.data.actions['D02_ModuleLead_SeatedIdle_'+version];a.animation_data.action=action;a.animation_data.action_slot=action.slots[0];s.frame_set(1);bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');selected=[a,body];parent=a.parent
while parent:selected.append(parent);parent=parent.parent
for obj in selected:obj.select_set(True)
bpy.context.view_layer.objects.active=a;out.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'EMPTY','ARMATURE','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='RELATIVE',embed_textures=False)
(out/('ModuleLead_Interactions_'+version+'.packet-tracks.json')).write_text(json.dumps(packet,indent=2)+'\n',encoding='utf-8')
receipt={**validation,'fbx':str(fbx),'fbx_sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'bones':len(a.data.bones),'fps':controls['fps'],'clips':[{'name':n,'frames':list(bpy.data.actions[n].frame_range)} for n in names],'runtime_status':'Unity import, normal-speed visual playback, interruption, reset and persistence require new validation after native edits.'}
(out/('ModuleLead_Interactions_'+version+'.receipt.json')).write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print('MODULE_LEAD_EXPORTED='+str(fbx),flush=True)
