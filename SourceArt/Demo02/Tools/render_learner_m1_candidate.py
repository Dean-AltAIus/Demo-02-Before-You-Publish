"""Inspect a named saved M1 master against its owned folio geometry, no saves."""
import bpy,sys,argparse
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--master',type=Path,required=True);p.add_argument('--output-dir',type=Path,required=True);p.add_argument('--withdrawal',action='store_true');p.add_argument('--recovery-only',action='store_true');p.add_argument('--family',choices=['FolioOpen','FolioClose'],default='FolioOpen');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=(R/args.output_dir).resolve()
if out.exists():raise RuntimeError('Preserve existing evidence')
out.mkdir(parents=True)
for family in ([args.family+'Recovery'] if args.recovery_only else [args.family,args.family+'Recovery'] if args.withdrawal else ['FolioOpen','FolioClose']):
 source_family=family.replace('Recovery','')
 bpy.ops.wm.open_mainfile(filepath=str(R/f'SourceArt/Demo02/Characters/Learner/AcademicLearner_{source_family}Skin_v004.blend'));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;a=bpy.data.objects['AcademicLearner_EditableRig'];old=bpy.data.objects['AcademicLearner_WeightedMesh'];name=f'D02_Learner_{family}_v013'
 if family.endswith('Recovery'):
  s.frame_set(16);bpy.context.view_layer.update();hinge=bpy.data.objects['FolioCoverHinge'];final_hinge=hinge.matrix_basis.copy();hinge.animation_data.action=None;hinge.matrix_basis=final_hinge
 with bpy.data.libraries.load(str((R/args.master).resolve()),link=False) as (src,dst):dst.actions=[name];dst.objects=['AcademicLearner_WeightedMesh']
 body=dst.objects[0];s.collection.objects.link(body);world=old.matrix_world.copy();body.parent=old.parent;body.matrix_world=world;old.hide_render=True
 for mod in body.modifiers:
  if mod.type=='ARMATURE':mod.object=a
 for bone in a.pose.bones:
  for c in bone.constraints:c.mute=True
 a.animation_data.action=dst.actions[0];a.animation_data.action_slot=dst.actions[0].slots[0]
 s.render.resolution_x=900;s.render.resolution_y=900;s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=4;s.cycles.samples=12
 side=1 if source_family=='FolioClose' else -1
 views=([(8,'front',(0,-1,.08)),(8,'side',(side,0,.1)),(9.5,'side',(side,0,.1)),(11,'front',(0,-1,.08)),(16,'threequarter',(side*.8,-1,.3))] if family==args.family else [(4,'front',(0,-1,.08)),(7,'side',(side,0,.1))]) if args.withdrawal else [(5,'front',(0,-1,.08)),(8,'threequarter',(side*.8,-1,.3)),(8,'side',(side,0,.1)),(13,'recovery',(.8,-1,.3))]
 for f,view,offset in views:
  s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
  look=Vector((0,-.24,1.13));s.camera.location=look+Vector(offset);s.camera.rotation_euler=(look-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=1.25;s.render.filepath=str(out/f'{family}-{f}-{view}.png');bpy.ops.render.render(write_still=True)
print('Rendered saved candidate, no source save',flush=True)
