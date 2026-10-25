"""Rebake the saved editable lawn material into a fresh export directory.

The saved source may be edited directly. This tool samples its current base
colour graph, preserves every native file, and never overwrites an export.
"""
import argparse,bpy,hashlib,json,sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source',type=Path,default=Path('SourceArt/Demo02/Environment/CampusLawnFinish_v001.blend'))
parser.add_argument('--output-dir',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
source=args.source if args.source.is_absolute() else ROOT/args.source
out=args.output_dir if args.output_dir.is_absolute() else ROOT/args.output_dir
if out.exists():raise RuntimeError('Preserve existing lawn export directory')
before=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
material=bpy.data.materials['D02_CampusLawnFinish_v001']
nodes=material.node_tree.nodes;links=material.node_tree.links
output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL' and n.is_active_output)
shader=output.inputs['Surface'].links[0].from_node
if shader.type!='BSDF_PRINCIPLED':raise RuntimeError('Review the changed surface shader before baking its base colour')
base=shader.inputs['Base Color']
dependencies={'linked_libraries':len(bpy.data.libraries),'file_images':[{'name':image.name,'packed':bool(image.packed_file) or bool(image.packed_files)} for image in bpy.data.images if image.source=='FILE'],'nodes':len(nodes),'preview_objects':len(scene.objects)}
if dependencies['linked_libraries'] or any(not image['packed'] for image in dependencies['file_images']):raise RuntimeError('Resolve source dependencies before export')
tile_size=float(scene['tile_size_m'])
if not 0<tile_size<=100:raise RuntimeError('Invalid physical tile size')
bpy.ops.object.select_all(action='DESELECT');bpy.ops.mesh.primitive_plane_add(size=tile_size)
plane=bpy.context.object;plane.name='Temporary colour bake plane';plane.data.materials.append(material)
out.mkdir(parents=True)
image=bpy.data.images.new('Temporary lawn colour bake',width=1024,height=1024,alpha=False,float_buffer=False)
image.colorspace_settings.name='sRGB';image.file_format='PNG';image.filepath_raw=str(out/'CampusLawn_Albedo_v001.png')
target=nodes.new('ShaderNodeTexImage');target.image=image;nodes.active=target
emission=nodes.new('ShaderNodeEmission')
if base.is_linked:links.new(base.links[0].from_socket,emission.inputs['Color'])
else:emission.inputs['Color'].default_value=base.default_value
links.new(emission.outputs[0],output.inputs['Surface'])
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.bake.margin=16
bpy.context.view_layer.objects.active=plane;bpy.ops.object.bake(type='EMIT');image.save()
after=hashlib.sha256(source.read_bytes()).hexdigest()
if before!=after:raise RuntimeError('Native source changed during read-only export')
receipt={'source':str(source.relative_to(ROOT)) if source.is_relative_to(ROOT) else str(source),'source_sha256':before,'source_unchanged':True,'texture_sha256':hashlib.sha256(Path(image.filepath_raw).read_bytes()).hexdigest(),'resolution':[1024,1024],'tile_size_m':tile_size,'colour_space':'sRGB, use white Unity base tint','dependencies':dependencies,'runtime_status':'Native rebake only; Unity material and camera acceptance remains separate.'}
(out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt),flush=True)
