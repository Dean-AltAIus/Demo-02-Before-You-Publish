"""Read-only FBX derivative roundtrip audit in an isolated Blender process."""
import bpy,json,sys,argparse
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish');reports=[]
parser=argparse.ArgumentParser();parser.add_argument('--edition',default='v001')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
for role,stem,expected in [('Learner','AcademicLearner',1.68),('Colleague','Colleague',1.8),('ModuleLead','ModuleLead',1.65)]:
 scene=bpy.data.scenes.new(role+' FBX roundtrip');scene.render.fps=30;bpy.context.window.scene=scene
 before=set(bpy.data.actions);bpy.ops.import_scene.fbx(filepath=str(R/f'SourceArt/Demo02/Exports/Characters_{args.edition}/{role}/{stem}_{args.edition}.fbx'))
 actions=[a for a in bpy.data.actions if a not in before];arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data_clear()
 for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update();meshes=[o for o in scene.objects if o.type=='MESH'];points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
 height=max(p.z for p in points)-min(p.z for p in points)
 report={'role':role,'height_m':height,'expected_height_m':expected,'height_error_m':height-expected,'bones':len(arm.data.bones),'actions':[{'name':a.name,'frame_range':list(a.frame_range)} for a in actions],'mesh_count':len(meshes),'material_names':[m.name for o in meshes for m in o.data.materials],'unity_validation':'Not performed by this source audit'}
 if abs(height-expected)>.005:raise RuntimeError('Unexpected roundtrip scale: '+str(report))
 reports.append(report)
out=R/f'LocalOnly/AssetJobs/character-fbx-roundtrip-{args.edition}.json';out.write_text(json.dumps(reports,indent=2));print(json.dumps(reports))
