"""Bake the native procedural oak colour onto the cube's shared export UV layout."""
import bpy,json,hashlib
from pathlib import Path
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
folder=R/'SourceArt/Demo02/Materials/WarmOak_v001';folder.mkdir(parents=True,exist_ok=True)
dest=folder/'WarmOak_Bake_v001.blend';texture=folder/'WarmOak_BaseColor.png'
if dest.exists() or texture.exists():raise RuntimeError('Refuse existing bake source replacement')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/CampusKit_v001.blend'))
scene=bpy.data.scenes.new('Demo02_OakBake_v001');bpy.context.window.scene=scene;scene.unit_settings.system='METRIC';scene.render.engine='CYCLES';scene.cycles.samples=1
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,0));cube=bpy.context.object;cube.name='Oak palette bake cube'
mat=bpy.data.materials['D02_WarmOak'];cube.data.materials.append(mat)
image=bpy.data.images.new('WarmOak_BaseColor',1024,1024,alpha=False);image.colorspace_settings.name='sRGB'
node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.name='Export bake destination';node.image=image;mat.node_tree.nodes.active=node
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True;scene.render.bake.margin=8
bpy.ops.object.bake(type='DIFFUSE');image.filepath_raw=str(texture);image.file_format='PNG';image.save();image.pack()
scene['purpose']='Editable procedural material and actual color bake. No full-scene raster UI.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
receipt={'material':'D02_WarmOak','source':'SourceArt/Demo02/Environment/CampusKit_v001.blend','bake_type':'Diffuse color only, lighting excluded','resolution':[1024,1024],'uv':'Native default cube UV layout; noncube oak parts require visual check','texture_sha256':hashlib.sha256(texture.read_bytes()).hexdigest(),'unity_import':'pending'}
(folder/'bake-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
