"""Export the reviewed native colleague gesture without replacing any source.
The exported FBX remains a Unity validation candidate until actual playback checks.
"""
import bpy,json,hashlib
from pathlib import Path
R=Path(__file__).resolve().parents[3]
source=R/'SourceArt/Demo02/Characters/Colleague/Colleague_Interactions_v001.blend'
out=R/'SourceArt/Demo02/Exports/NPCInteractions_v001'
fbx=out/'Colleague_Interactions_v001.fbx'
if fbx.exists():raise RuntimeError('Preserve existing export')
out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_ColleagueRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['Colleague_EditableRig'];body=bpy.data.objects['Colleague_WeightedMesh']
def select_action(name,frame):
    act=bpy.data.actions[name];a.animation_data.action=act;a.animation_data.action_slot=act.slots[0];s.frame_set(frame);bpy.context.view_layer.update()
    return {p.name:a.matrix_world@p.matrix for p in a.pose.bones}
idle=select_action('D02_Colleague_Idle_v001',1)
first=select_action('D02_Colleague_SourcePoint_v001',1);last=select_action('D02_Colleague_SourcePoint_v001',61)
end_error=max(abs(m[i][j]-idle[n][i][j]) for frame in [first,last] for n,m in frame.items() for i in range(4) for j in range(4))
if end_error>.0001:raise RuntimeError('Return to idle mismatch: '+str(end_error))
max_foot_drift=0;max_wrist_step=0;previous=None;frames=[]
for frame in range(1,62):
    pose=select_action('D02_Colleague_SourcePoint_v001',frame)
    wrist=pose['LeftHand'].translation
    if previous is not None:max_wrist_step=max(max_wrist_step,(wrist-previous).length)
    previous=wrist.copy()
    for name in ['LeftFoot','RightFoot']:max_foot_drift=max(max_foot_drift,(pose[name].translation-idle[name].translation).length)
    frames.append({'frame':frame,'left_wrist_m':list(wrist)})
if max_foot_drift>1e-5:raise RuntimeError('Gesture cannot slide the feet')
actions=[act for act in bpy.data.actions if act.name.startswith('D02_Colleague_')]
clips=[{'name':act.name,'frames':list(act.frame_range),'fps':30} for act in actions]
for act in list(bpy.data.actions):
    if act not in actions:bpy.data.actions.remove(act)
select_action('D02_Colleague_SourcePoint_v001',1)
bpy.ops.object.select_all(action='DESELECT')
selected=[a,body];parent=a.parent
while parent:
    selected.append(parent);parent=parent.parent
for obj in selected:obj.select_set(True)
bpy.context.view_layer.objects.active=a
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'EMPTY','ARMATURE','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='RELATIVE',embed_textures=False)
receipt={'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'export':str(fbx.relative_to(R)),'export_sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'bones':len(a.data.bones),'clips':clips,'source_point_item':'D02.ASSET.CLIP.COLLEAGUE.SOURCE_POINT','semantic':'Open-palm gesture toward the actual permitted source context; no approval authority','native_multiangle_static_review':'front, back, both sides inspected; no obvious shoulder, wrist or body penetration','endpoint_matrix_error':end_error,'max_foot_drift_m':max_foot_drift,'max_wrist_step_m':max_wrist_step,'frames':frames,'runtime_status':'Not yet imported or accepted. Actual Unity source-state binding, interruption, reset and normal-speed playback required.'}
(out/'Colleague_Interactions_v001.receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print('COLLEAGUE_EXPORT='+json.dumps({k:v for k,v in receipt.items() if k!='frames'}),flush=True)
