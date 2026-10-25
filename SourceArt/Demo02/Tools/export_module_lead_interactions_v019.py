"""Reopen and verify the complete saved reviewer cycle, then export the actor.
The single packet's editable source actions are retained in the blend and copied
as explicit native-coordinate tracks for engine-side authored clip conversion.
"""
import bpy,json,hashlib,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from npc_contact_math import NativeNpcRig
R=Path(r'D:/Altaius/Demo-02-Before-You-Publish');version='v019'
source=R/'SourceArt/Demo02/Characters/ModuleLead'/('ModuleLead_Interactions_'+version+'.blend')
out=R/'SourceArt/Demo02/Exports/NPCInteractions_v001';fbx=out/('ModuleLead_Interactions_'+version+'.fbx')
evidence=R/'LocalOnly/Validation/Production/NPCInteractions_v001'/('LeadInteractions_'+version)
if fbx.exists():raise RuntimeError('Preserve existing export')
authored=json.loads((evidence/'contact-audit.json').read_text())
if any(t['penetrating_frames'] for t in authored['takes']) or any(b['maximum_world_matrix_error']>.0001 for b in authored['boundaries']):raise RuntimeError('Authoring gate failed')
before=hashlib.sha256(source.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_ModuleLeadRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['ModuleLead_EditableRig'];body=bpy.data.objects['ModuleLead_WeightedMesh'];folio=bpy.data.objects['Folio'];hinge=bpy.data.objects['FolioCoverHinge'];objects=[a,folio,hinge]
chains={n:[Vector(v) for v in pts] for n,pts in json.loads((R/'LocalOnly/Validation/Production/NPCInteractions_v001/LeadFingerControls/finger-rig-audit.json').read_text())['joint_world_positions'].items()}
rig=NativeNpcRig(a,body,chains);props=[bpy.data.objects[n] for n in ['Front cover','Back cover','Thin paper stack']];table=next(o for o in bpy.data.objects['ReviewDesk'].children_recursive if o.name.startswith('Oak worktop'))
foot_base={name:(a.matrix_world@a.pose.bones[name].matrix).translation.copy() for name in ['LeftFoot','RightFoot']}
rows=[];bad=[];max_foot=0;max_track_error=0
for take in authored['tracks']:
    for obj,suffix in zip(objects,['','_PacketRoot','_Cover']):
        act=bpy.data.actions['D02_ModuleLead_'+take['take']+suffix+'_'+version];obj.animation_data.action=act;obj.animation_data.action_slot=act.slots[0]
    sample=[];failures=0
    for frame in take['frames']:
        s.frame_set(frame['frame']);bpy.context.view_layer.update()
        checks=rig.audit_objects(props)+rig.audit_objects([table],ids=list(range(len(body.data.vertices))),penetration_only=True)
        if any(r['inside_vertex_count'] or r['ray_disagreements'] for r in checks):bad.append({'take':take['take'],'frame':frame['frame'],'checks':checks});failures+=1
        max_foot=max(max_foot,max(((a.matrix_world@a.pose.bones[n].matrix).translation-foot_base[n]).length for n in foot_base))
        max_track_error=max(max_track_error,(folio.location-Vector(frame['packet_position'])).length)
        sample.append({'frame':frame['frame'],'bones':{n:list((a.matrix_world@a.pose.bones[n].matrix).translation) for n in ['LeftHand','RightHand','LeftFoot','RightFoot','Head']}})
    rows.append({'take':take['take'],'frames':len(sample),'failed_frames':failures,'samples':sample})
    print('SAVED_SOURCE_CHECK='+json.dumps({k:v for k,v in rows[-1].items() if k!='samples'}),flush=True)
validation={'source_sha256':before,'takes':rows,'failures':bad,'max_foot_drift_m':max_foot,'maximum_packet_track_error_m':max_track_error,'source_unchanged':hashlib.sha256(source.read_bytes()).hexdigest()==before}
(evidence/'saved-source-reopen.json').write_text(json.dumps(validation,indent=2),encoding='utf-8')
if bad or max_foot>.00001 or max_track_error>.00001 or not validation['source_unchanged']:raise RuntimeError('Saved-source validation failed')
names=['D02_ModuleLead_'+t['take']+'_'+version for t in authored['tracks']]
for obj in [folio,hinge]:obj.animation_data.action=None
for act in list(bpy.data.actions):
    if act.name not in names:bpy.data.actions.remove(act)
act=bpy.data.actions['D02_ModuleLead_SeatedIdle_'+version];a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];s.frame_set(1);bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');selected=[a,body];parent=a.parent
while parent:selected.append(parent);parent=parent.parent
for obj in selected:obj.select_set(True)
bpy.context.view_layer.objects.active=a;out.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'EMPTY','ARMATURE','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='RELATIVE',embed_textures=False)
tracks={'source':str(source.relative_to(R)),'source_sha256':before,'coordinates':'Blender native metres, Z up, actor faces negative Y. Positions relative to actor native origin. Quaternion arrays are XYZW. Convert the coordinate basis and target the single authored runtime packet.','point_target_native_m':authored['point_target_native_m'],'tracks':authored['tracks'],'ownership_notes':{'PickupPacket':'Desk through frame18, NPC contact from frame19.','ReadPacket':'NPC holds the single packet.','PlaceOpen':'NPC placement, desk owns at settled frame13.','PointCurrentPair':'Desk owns, point refers to live exact pair.','RecoverPoint':'Desk owns, hand recovery only.','ReturnPacket':'NPC lifts/closes/centres, desk owns at settled frame16.','RecoverToReady':'Desk owns closed packet.','SeatedIdle':'Desk owns closed packet.','SeatedListen':'Desk owns closed packet.','SeatedSpeak':'Desk owns closed packet.'},'authority':'Presentation only. No animation callback supplies a domain decision, receipt, approval, evidence or request.'}
(out/('ModuleLead_Interactions_'+version+'.packet-tracks.json')).write_text(json.dumps(tracks,indent=2),encoding='utf-8')
receipt={'source':str(source.relative_to(R)),'source_sha256':before,'fbx':str(fbx.relative_to(R)),'fbx_sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'bones':len(a.data.bones),'fps':30,'clips':[{'name':n,'frames':list(bpy.data.actions[n].frame_range)} for n in names],'contact_scope':'All 484 saved 30 fps samples, actual hand skin against closed prop volumes and whole-body vertices against tabletop. 0.1 mm tolerance. This is not continuous triangle collision proof.','max_foot_drift_m':max_foot,'maximum_packet_track_error_m':max_track_error,'boundaries':authored['boundaries'],'runtime_status':'Unity import, normal-speed visual playback, interruption, reset and persistence not yet accepted.'}
(out/('ModuleLead_Interactions_'+version+'.receipt.json')).write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print('MODULE_LEAD_EXPORTED='+str(fbx),flush=True)
