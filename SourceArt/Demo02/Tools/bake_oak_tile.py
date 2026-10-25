"""Full-UV oak color tile from editable native nodes, preserves prior cube bake."""
import bpy,json,hashlib
from pathlib import Path
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
folder=R/'SourceArt/Demo02/Materials/WarmOak_v002';folder.mkdir(parents=True,exist_ok=True)
dest=folder/'WarmOak_Bake_v002.blend';texture=folder/'WarmOak_BaseColor.png'
if dest.exists() or texture.exists():raise RuntimeError('Preserve existing bake version')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/CampusKit_v001.blend'))
s=bpy.data.scenes.new('Demo02_OakBake_v002');bpy.context.window.scene=s;s.render.engine='CYCLES';s.cycles.samples=1
bpy.ops.mesh.primitive_plane_add(size=1);plane=bpy.context.object;plane.name='Full UV oak bake surface'
mat=bpy.data.materials['D02_WarmOak'].copy();mat.name='D02_WarmOak_ExportSource';plane.data.materials.append(mat)
nodes=mat.node_tree.nodes;links=mat.node_tree.links
mapping=next(n for n in nodes if n.type=='MAPPING');coordinates=next(n for n in nodes if n.type=='TEX_COORD')
links.new(coordinates.outputs['UV'],mapping.inputs['Vector'])
image=bpy.data.images.new('WarmOak_FullUV_BaseColor',1024,1024,alpha=False);image.colorspace_settings.name='sRGB'
node=nodes.new('ShaderNodeTexImage');node.name='Export bake destination';node.image=image;nodes.active=node
s.render.bake.use_pass_direct=False;s.render.bake.use_pass_indirect=False;s.render.bake.use_pass_color=True;s.render.bake.margin=8
bpy.ops.object.bake(type='DIFFUSE');image.filepath_raw=str(texture);image.file_format='PNG';image.save();image.pack()
s['purpose']='Full-UV color-only bake avoids black unmapped regions on cylinder and box UVs.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
receipt={'material':'D02_WarmOak','source':'SourceArt/Demo02/Environment/CampusKit_v001.blend','bake_type':'Diffuse color only, lighting excluded','resolution':[1024,1024],'uv':'Entire 0 to 1 UV field, no empty atlas regions. Grain orientation follows each editable mesh UV.','texture_sha256':hashlib.sha256(texture.read_bytes()).hexdigest(),'unity_import':'pending'}
(folder/'bake-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
