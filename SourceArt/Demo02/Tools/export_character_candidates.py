"""Versioned, selected-only Unity export derivatives from preserved native sources."""
import bpy,json,hashlib,sys,argparse
from pathlib import Path
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
parser=argparse.ArgumentParser();parser.add_argument('--edition',default='v001');parser.add_argument('--npc-motion',default='v001')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
for role,stem,version in [('Learner','AcademicLearner','v002'),('Colleague','Colleague',args.npc_motion),('ModuleLead','ModuleLead',args.npc_motion)]:
 source=R/f'SourceArt/Demo02/Characters/{role}/{stem}_Motion_{version}.blend'
 dest=R/f'SourceArt/Demo02/Exports/Characters_{args.edition}/{role}';dest.mkdir(parents=True,exist_ok=True)
 target=dest/(stem+'_'+args.edition+'.fbx')
 if target.exists():raise RuntimeError('Refuse existing export replacement')
 bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_'+role+'Rig_v001'];bpy.context.window.scene=s
 a=bpy.data.objects[stem+'_EditableRig'];body=bpy.data.objects[stem+'_WeightedMesh'];root=bpy.data.objects[stem+'_Root']
 keep={'D02_Learner_Idle_v001','D02_Learner_Walk_v002'} if role=='Learner' else {'D02_'+role+'_Idle_v001'}
 for act in list(bpy.data.actions):
  if act.name not in keep:bpy.data.actions.remove(act)
 idle=bpy.data.actions[next(n for n in keep if 'Idle' in n)];a.animation_data.action=idle;a.animation_data.action_slot=idle.slots[0];s.frame_set(1);bpy.context.view_layer.update()
 texture_receipts=[]
 for mat in body.data.materials:
  mat.name='D02_'+role+'_Body'
  for node in mat.node_tree.nodes:
   if node.type=='TEX_IMAGE' and node.image:
    img=node.image;path=dest/(role+'_Albedo.png');img.filepath_raw=str(path);img.file_format='PNG';img.save()
    texture_receipts.append({'file':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
 bpy.ops.object.select_all(action='DESELECT')
 for o in [root,a,body]:o.hide_set(False);o.select_set(True)
 bpy.context.view_layer.objects.active=a
 bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'EMPTY','ARMATURE','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='RELATIVE',embed_textures=False)
 receipt={'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'export':target.name,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'units':'metres via FBX unit scaling','axis_forward':'-Z','axis_up':'Y','bones':len(a.data.bones),'mesh_vertices':len(body.data.vertices),'actions':sorted(keep),'textures':texture_receipts,'status':'Export candidate, Unity avatar/render/play validation pending'}
 (dest/'export-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
