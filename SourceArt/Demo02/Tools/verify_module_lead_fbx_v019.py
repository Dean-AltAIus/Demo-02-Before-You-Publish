"""Independent read-only FBX roundtrip for all reviewer bone samples and skin."""
import bpy,json,math,hashlib,sys
from pathlib import Path
from mathutils import Vector,Quaternion
from mathutils.kdtree import KDTree
sys.path.insert(0,str(Path(__file__).parent))
from npc_contact_math import NativeNpcRig
R=Path(__file__).resolve().parents[3];version='v019'
folder=R/'SourceArt/Demo02/Exports/NPCInteractions_v001'
source=R/'SourceArt/Demo02/Characters/ModuleLead'/('ModuleLead_Interactions_'+version+'.blend');fbx=folder/('ModuleLead_Interactions_'+version+'.fbx')
out=R/'LocalOnly/Validation/Production/NPCInteractions_v001'/('LeadInteractions_'+version)/'fbx-roundtrip.json'
if out.exists():raise RuntimeError('Preserve existing roundtrip evidence')
out.parent.mkdir(parents=True,exist_ok=True)
tracks=json.loads((folder/('ModuleLead_Interactions_'+version+'.packet-tracks.json')).read_text())
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_ModuleLeadRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['ModuleLead_EditableRig'];body=bpy.data.objects['ModuleLead_WeightedMesh'];folio=bpy.data.objects['Folio'];hinge=bpy.data.objects['FolioCoverHinge'];rig=NativeNpcRig(a,body,{})
bone_names=[p.name for p in a.pose.bones];hand_ids=rig.hand_ids('Left')+rig.hand_ids('Right');native={};skin={};max_position_error=0;max_rotation_error=0
for take in tracks['tracks']:
    name=take['take'];native[name]=[];skin[name]={}
    for obj,suffix in [(a,''),(folio,'_PacketRoot'),(hinge,'_Cover')]:
        act=bpy.data.actions['D02_ModuleLead_'+name+suffix+'_'+version];obj.animation_data.action=act;obj.animation_data.action_slot=act.slots[0]
    frames=take['frames'];selected={1,len(frames),1+(len(frames)-1)//2}
    for row in frames:
        s.frame_set(row['frame']);bpy.context.view_layer.update();native[name].append({n:(a.matrix_world@a.pose.bones[n].head).copy() for n in bone_names})
        max_position_error=max(max_position_error,(folio.location-Vector(row['packet_position'])).length)
        for obj,key in [(folio,'packet_rotation_xyzw'),(hinge,'cover_rotation_xyzw')]:
            q=row[key];expected=[q[3],q[0],q[1],q[2]];actual=list(obj.rotation_quaternion)
            dot=abs(sum(x*y for x,y in zip(expected,actual)))/math.sqrt(sum(x*x for x in expected)*sum(x*x for x in actual));max_rotation_error=max(max_rotation_error,math.degrees(2*math.acos(min(1,dot))))
        if row['frame'] in selected:
            points=rig.skin();skin[name][row['frame']]=[points[i].copy() for i in hand_ids]
# A new empty native file ensures the FBX is the sole actor used for comparison.
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(fbx),use_anim=True,automatic_bone_orientation=False)
s=bpy.context.scene;a=next(o for o in s.objects if o.type=='ARMATURE');body=next(o for o in s.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers))
if set(bone_names)!=set(p.name for p in a.pose.bones):raise RuntimeError('Bone-name set changed on roundtrip')
if len(bpy.data.actions)!=len(native):raise RuntimeError('Unexpected exported clip count')
report={'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'fbx_sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'bones':len(bone_names),'clips':[],'maximum_packet_position_error_m':max_position_error,'maximum_packet_quaternion_error_degrees':max_rotation_error,'maximum_bone_head_error_m':0,'maximum_sampled_hand_skin_error_m':0,'status':'Native FBX reimport proof only. Actual Unity gameplay acceptance remains separate.'}
for name,frames in native.items():
    act=next(x for x in bpy.data.actions if 'D02_ModuleLead_'+name+'_'+version in x.name);a.animation_data.action=act;a.animation_data.action_slot=act.slots[0]
    maximum=0;skin_max=0
    for index,pose in enumerate(frames,1):
        s.frame_set(index);bpy.context.view_layer.update()
        maximum=max(maximum,max(((a.matrix_world@a.pose.bones[n].head)-point).length for n,point in pose.items()))
        if index in skin[name]:
            ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());points=[ev.matrix_world@v.co for v in ev.data.vertices];tree=KDTree(len(points))
            for i,p in enumerate(points):tree.insert(p,i)
            tree.balance();skin_max=max(skin_max,max(tree.find(p)[2] for p in skin[name][index]))
    report['clips'].append({'take':name,'action':act.name,'frames':len(frames),'bone_samples':len(frames)*len(bone_names),'max_bone_head_error_m':maximum,'skin_sample_frames':len(skin[name]),'max_hand_skin_error_m':skin_max})
    report['maximum_bone_head_error_m']=max(report['maximum_bone_head_error_m'],maximum);report['maximum_sampled_hand_skin_error_m']=max(report['maximum_sampled_hand_skin_error_m'],skin_max)
out.write_text(json.dumps(report,indent=2),encoding='utf-8')
if max_position_error>1e-5 or max_rotation_error>.0001 or report['maximum_bone_head_error_m']>1e-5 or report['maximum_sampled_hand_skin_error_m']>.0001:raise RuntimeError('Independent roundtrip mismatch: '+json.dumps(report))
print('INDEPENDENT_ROUNDTRIP='+json.dumps(report),flush=True)
