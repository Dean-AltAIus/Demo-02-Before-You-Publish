"""Editable two-source archive carrier, based on the supplied campus reference."""
import bpy, json
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Environment/ArchiveCabinet_v001.blend'
exports=R/'SourceArt/Demo02/Exports/ArchiveCabinet_v001'
if dest.exists() or exports.exists():raise RuntimeError('Preserve existing native authoring')
exports.mkdir(parents=True)
s=bpy.data.scenes.new('Demo02_ArchiveCabinet_v001');bpy.context.window.scene=s;s.unit_settings.system='METRIC'
s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=True;s.render.threads_mode='FIXED';s.render.threads=4
s.world=bpy.data.worlds.new('Archive neutral studio');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.55,.62,.72,1);s.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6
root=bpy.data.objects.new('ArchiveCabinet',None);s.collection.objects.link(root)
root['production_ids']='D02.ASSET.PROP.SOURCE_A,D02.ASSET.PROP.SOURCE_B'
def mat(name,c,rough=.6,metal=0):
 m=bpy.data.materials.new('D02_'+name);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;return m
steel=mat('ArchiveEnamel',(.31,.35,.36),.48,.25);shadow=mat('ArchiveShadow',(.07,.10,.12));wood=mat('ArchiveOak',(.42,.19,.065),.5);paper=mat('ArchiveLabel',(.92,.88,.72));metal=mat('ArchiveHandle',(.025,.045,.06),.3,.55)
def box(name,pos,size,material,parent=root,bevel=.012):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.parent=parent;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 if bevel:m=o.modifiers.new('Editable edge radius','BEVEL');m.width=bevel;m.segments=3;o.modifiers.new('Corner normals','WEIGHTED_NORMAL')
 return o
box('Cabinet body',(0,0,.43),(1.83,.68,.76),steel)
box('Oak counter',(0,0,.84),(1.94,.78,.075),wood,bevel=.022)
box('Dark recessed plinth',(0,.025,.05),(1.70,.57,.10),shadow)
for i,x in enumerate([-.445,.445]):
 source='S_A' if i==0 else 'S_B'
 box(source+' drawer reveal',(x,-.349,.44),(.844,.012,.65),shadow,bevel=.006)
 drawer=bpy.data.objects.new(source+' editable drawer',None);s.collection.objects.link(drawer);drawer.parent=root;drawer['source_id']=source;drawer['motion_policy']='Optional drawer travel is presentation only; source inspection uses actual permitted passage.'
 box(source+' drawer face',(x,-.365,.44),(.812,.036,.61),steel,drawer,.015)
 box(source+' label bezel',(x,-.395,.40),(.38,.025,.18),metal,drawer,.014)
 box(source+' blank live-text label plate',(x,-.411,.40),(.346,.012,.146),paper,drawer,.008)
 box(source+' pull left',(x-.15,-.407,.64),(.025,.075,.04),metal,drawer,.009)
 box(source+' pull right',(x+.15,-.407,.64),(.025,.075,.04),metal,drawer,.009)
 box(source+' pull bar',(x,-.44,.64),(.32,.028,.04),metal,drawer,.012)
root['scale_m']='1.94 wide, 0.88 high; source text is live Unity typography'
bpy.ops.object.select_all(action='DESELECT')
for o in [root]+list(root.children_recursive):o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(exports/'ArchiveCabinet.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
manifest={'name':'ArchiveCabinet','mesh_parts':sum(o.type=='MESH' for o in root.children_recursive),'source_ids':['S_A','S_B'],'materials':[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value,'metallic':m.node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value} for m in [steel,shadow,wood,paper,metal]],'status':'Native source; Unity import and live labels pending'}
(exports/'manifest.json').write_text(json.dumps(manifest,indent=2))
cam=bpy.data.objects.new('Archive carrier inspection',bpy.data.cameras.new('Archive carrier inspection'));s.collection.objects.link(cam);cam.location=(2,-3.3,1.8);cam.rotation_euler=(Vector((0,0,.45))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.8;s.camera=cam
light=bpy.data.objects.new('Softbox',bpy.data.lights.new('Softbox','AREA'));s.collection.objects.link(light);light.location=(1,-2,4);light.data.energy=500;light.data.size=4;light.rotation_euler=(Vector((0,0,.4))-light.location).to_track_quat('-Z','Y').to_euler()
s.render.resolution_x=1100;s.render.resolution_y=800;s.render.filepath=str(R/'LocalOnly/Captures/Environment/ArchiveCabinet_v001.png')
bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(manifest))
