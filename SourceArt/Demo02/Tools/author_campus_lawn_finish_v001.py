"""Author an editable tileable lawn shader and one colour bake, no new scenery.

Run only in isolated Blender. The original dressing kit and ground geometry are
read only. The separate material source includes a preview plane and controls.
"""
import bpy, hashlib, json, math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[3]
SOURCE=ROOT/'SourceArt/Demo02/Environment/DressingKit_v001.blend'
DEST=ROOT/'SourceArt/Demo02/Environment/CampusLawnFinish_v001.blend'
OUT=ROOT/'SourceArt/Demo02/Exports/CampusLawnFinish_v001'
EVIDENCE=ROOT/'LocalOnly/Validation/Production/CampusLawnFinish_v001'
EXPECTED='1a345a994be2af544363f16d0e7772c2de983ea9c3f5def05d9dfe014786e4a6'

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
if DEST.exists() or OUT.exists() or EVIDENCE.exists():raise RuntimeError('Preserve existing lawn source and bake')
if digest(SOURCE)!=EXPECTED:raise RuntimeError('Dressing kit was edited; preserve it and review derivation')
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
base=bpy.data.materials['D02_CampusLawn']
original=next(n for n in base.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
palette=list(original.inputs['Base Color'].default_value)
# This reset is confined to this isolated background process. The native kit is
# never saved, and no Unity mesh or user's live Blender session is touched.
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.name='Demo02_CampusLawnMaterial_v001'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene['derived_from']='DressingKit_v001.blend / D02_CampusLawn'
scene['original_sha256']=EXPECTED
scene['purpose']='Material preview only. Keep existing campus ground geometry.'
scene['tile_size_m']=4.0
scene.render.engine='CYCLES';scene.cycles.samples=8
scene.render.threads_mode='FIXED';scene.render.threads=2
scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
scene.view_settings.exposure=0;scene.view_settings.gamma=1
material=bpy.data.materials.new('D02_CampusLawnFinish_v001');material.use_nodes=True
material.diffuse_color=palette;material['tile_size_m']=4.0
material['description']='Restrained lawn colour variation and fine turf grain, one sRGB tile, no added geometry.'
nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
def node(kind,name,x,y):
 n=nodes.new(kind);n.name=name;n.label=name;n.location=(x,y);return n
tex=node('ShaderNodeTexCoord','Tile UV (one repeat = 4 metres)',-1100,100)
separate=node('ShaderNodeSeparateXYZ','UV components',-900,100);links.new(tex.outputs['UV'],separate.inputs[0])
periodic={}
for index,axis in enumerate(['X','Y']):
 angle=node('ShaderNodeMath',axis+' periodic angle',-720,300-index*400);angle.operation='MULTIPLY';angle.inputs[1].default_value=2*math.pi;links.new(separate.outputs[axis],angle.inputs[0])
 for j,operation in enumerate(['COSINE','SINE']):
  n=node('ShaderNodeMath',axis+' '+operation.lower(),-520,350-index*400-j*150);n.operation=operation;links.new(angle.outputs[0],n.inputs[0]);periodic[axis,operation]=n.outputs[0]
vector=node('ShaderNodeCombineXYZ','Seamless torus coordinates',-300,200)
for socket,key in zip(['X','Y','Z'],[('X','COSINE'),('X','SINE'),('Y','COSINE')]):links.new(periodic[key],vector.inputs[socket])
def noise(name,scale,detail,y):
 n=node('ShaderNodeTexNoise',name,-80,y);n.noise_dimensions='4D';n.inputs['Scale'].default_value=scale;n.inputs['Detail'].default_value=detail;n.inputs['Roughness'].default_value=.65
 links.new(vector.outputs[0],n.inputs['Vector']);links.new(periodic['Y','SINE'],n.inputs['W']);return n
broad=noise('Quiet broad lawn variation',1.35,2,350)
fine=noise('Fine turf grain',38,2,-30)
broad_ramp=node('ShaderNodeValToRGB','Lawn palette from admitted source',170,350)
links.new(broad.outputs['Fac'],broad_ramp.inputs['Fac'])
for element,position,factors in [(broad_ramp.color_ramp.elements[0],.20,(.87,.88,.85)),(broad_ramp.color_ramp.elements[1],.80,(1.06,1.055,1.035))]:
 element.position=position;element.color=tuple(palette[i]*factors[i] for i in range(3))+(1,)
grain_ramp=node('ShaderNodeValToRGB','Restrained grain contrast',170,-30)
links.new(fine.outputs['Fac'],grain_ramp.inputs['Fac'])
grain_ramp.color_ramp.elements[0].position=.18;grain_ramp.color_ramp.elements[0].color=(.88,.89,.88,1)
grain_ramp.color_ramp.elements[1].position=.82;grain_ramp.color_ramp.elements[1].color=(1.08,1.08,1.055,1)
colour=node('ShaderNodeMixRGB','Finished lawn colour',420,250);colour.blend_type='MULTIPLY';colour.inputs[0].default_value=1
links.new(broad_ramp.outputs['Color'],colour.inputs[1]);links.new(grain_ramp.outputs['Color'],colour.inputs[2])
shader=node('ShaderNodeBsdfPrincipled','Editable lawn surface',670,250);shader.inputs['Roughness'].default_value=.9;shader.inputs['Specular IOR Level'].default_value=.2
links.new(colour.outputs[0],shader.inputs['Base Color'])
output=node('ShaderNodeOutputMaterial','Surface output',940,250);links.new(shader.outputs['BSDF'],output.inputs['Surface'])
bpy.ops.mesh.primitive_plane_add(size=4);plane=bpy.context.object;plane.name='Lawn material preview, 4 metres per UV tile';plane.data.materials.append(material)
OUT.mkdir(parents=True);EVIDENCE.mkdir(parents=True)
image=bpy.data.images.new('D02_CampusLawnFinish_Albedo_v001',width=1024,height=1024,alpha=False,float_buffer=False)
image.colorspace_settings.name='sRGB';image.file_format='PNG';image.filepath_raw=str(OUT/'CampusLawn_Albedo_v001.png')
bake_node=node('ShaderNodeTexImage','Packed colour bake (export derivative)',430,-400);bake_node.image=image;nodes.active=bake_node
emission=node('ShaderNodeEmission','Colour-only bake helper',680,-180);links.new(colour.outputs[0],emission.inputs['Color']);links.new(emission.outputs[0],output.inputs['Surface'])
scene.render.bake.margin=16;bpy.context.view_layer.objects.active=plane;plane.select_set(True)
bpy.ops.object.bake(type='EMIT')
image.save();image.pack();links.new(shader.outputs['BSDF'],output.inputs['Surface']);emission.hide=True
# A 12 metre preview exposes a 3 by 3 repetition without changing the tile size.
plane.scale=(3,3,1)
for uv in plane.data.uv_layers.active.data:uv.uv*=3
world=bpy.data.worlds.new('Neutral material preview');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.72,.76,.82,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65;scene.world=world
sun_data=bpy.data.lights.new('Preview sun','SUN');sun_data.energy=1.4;sun_data.angle=math.radians(12)
sun=bpy.data.objects.new('Preview sun',sun_data);scene.collection.objects.link(sun);sun.rotation_euler=(math.radians(25),math.radians(-20),math.radians(20))
camera_data=bpy.data.cameras.new('Lawn material camera');camera=bpy.data.objects.new('Lawn material camera',camera_data);scene.collection.objects.link(camera);scene.camera=camera
camera.location=(0,-10,12);camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.type='ORTHO';camera_data.ortho_scale=13
scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.resolution_percentage=100
receipt={'schema':1,'original_source':SOURCE.relative_to(ROOT).as_posix(),'original_source_sha256':EXPECTED,'native_source':DEST.relative_to(ROOT).as_posix(),'original_palette_linear_rgba':palette,'tile_size_m':4,'resolution':[1024,1024],'texture':(OUT/'CampusLawn_Albedo_v001.png').relative_to(ROOT).as_posix(),'texture_sha256':digest(OUT/'CampusLawn_Albedo_v001.png'),'colour_space':'sRGB colour texture, bake from linear shader; use white Unity base tint','unity_import':'Repeat wrap, mipmaps on, anisotropic 4, preserve alpha disabled; one albedo only','source_nodes':'Editable periodic 4D noise and colour ramps, packed baked texture','node_count':len(nodes),'geometry_scope':'Original DressingKit and CampusGround geometry unchanged; separate preview plane is not a game export','runtime_status':'Native material candidate. Unity admission, scale, seam and final camera validation remain required.'}
bpy.ops.wm.save_as_mainfile(filepath=str(DEST));receipt['native_source_sha256']=digest(DEST)
(OUT/'CampusLawnFinish_v001.receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
scene.render.filepath=str(EVIDENCE/'repeat-preview.png');bpy.ops.render.render(write_still=True)
assert digest(SOURCE)==EXPECTED
print(json.dumps(receipt),flush=True)
