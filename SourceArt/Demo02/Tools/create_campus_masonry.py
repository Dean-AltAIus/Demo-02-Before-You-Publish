"""New editable masonry revision, with combined export derivatives only."""
import bpy,bmesh,math,random,json,hashlib
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
out=R/'SourceArt/Demo02/Exports/MasonryKit_v002'
dest=R/'SourceArt/Demo02/Environment/MasonryKit_v002.blend'
tex=R/'SourceArt/Demo02/Materials/Limestone_v001'
if out.exists() or dest.exists() or tex.exists():raise RuntimeError('Preserve existing masonry authoring')
out.mkdir(parents=True);tex.mkdir(parents=True)
s=bpy.data.scenes.new('Demo02_Masonry_v002');bpy.context.window.scene=s
s.unit_settings.system='METRIC';s.render.engine='CYCLES';s.cycles.samples=16
s.world=bpy.data.worlds.new('Neutral source inspection');s.world.use_nodes=True
s.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.65,.74,.85,1)
s.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45
rng=random.Random(251026)
def material(name,color,rough=.7):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
 p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough
 return m
stone=material('D02_LimestoneBaked',(.68,.57,.42),.8)
nodes=stone.node_tree.nodes;links=stone.node_tree.links
uv=nodes.new('ShaderNodeTexCoord');noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=4;noise.inputs['Detail'].default_value=3.5;noise.inputs['Roughness'].default_value=.7
ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.15;ramp.color_ramp.elements[0].color=(.51,.41,.29,1);ramp.color_ramp.elements[1].position=.85;ramp.color_ramp.elements[1].color=(.77,.66,.49,1)
links.new(uv.outputs['UV'],noise.inputs['Vector']);links.new(noise.outputs['Fac'],ramp.inputs['Fac']);links.new(ramp.outputs['Color'],nodes['Principled BSDF'].inputs['Base Color'])
grain=nodes.new('ShaderNodeTexNoise');grain.inputs['Scale'].default_value=85;grain.inputs['Detail'].default_value=2
bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.008
links.new(uv.outputs['UV'],grain.inputs['Vector']);links.new(grain.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],nodes['Principled BSDF'].inputs['Normal'])
trim=material('D02_MasonryTrim',(.82,.70,.51),.76)
mortar=material('D02_MasonryMortar',(.39,.34,.26),.92)
teal=material('D02_CanopyCanvas',(.012,.31,.34),.82)
metal=material('D02_CanopyIron',(.025,.048,.061),.48)
assets=[];root=None;col=None
def begin(name,ids):
 global root,col
 col=bpy.data.collections.new('D02_'+name);s.collection.children.link(col)
 root=bpy.data.objects.new(name,None);col.objects.link(root);root['production_ids']=','.join(ids);root['revision']='v002';root['authoring']='Native editable parts; export is combined derivative only.';assets.append(root)
def cube(name,loc,size,mat,bevel=.008):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=root;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
 # Explicit planar UVs, physical metre scale, no unmapped atlas regions.
 uv=o.data.uv_layers.active
 for p in o.data.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=[i for i in range(3) if i!=axis]
  for index in p.loop_indices:
   v=o.data.vertices[o.data.loops[index].vertex_index].co
   uv.data[index].uv=(v[axes[0]]+loc[axes[0]],v[axes[1]]+loc[axes[1]])
 if bevel:
  b=o.modifiers.new('Editable stone edge','BEVEL');b.width=bevel;b.segments=3
  n=o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL');n.keep_sharp=True
 return o
for variant in range(3):
 begin('Paving'+chr(65+variant)+'_2m',['D02.ASSET.ENV.COURTYARD'])
 cube('Continuous support',(0,0,-.075),(2,2,.15),mortar,0)
 for row in range(4):
  cuts=[0,.5,1.5,2] if (row+variant)%2 else [0,1,2]
  if variant==2 and row in [1,2]:cuts=[0,.75,1.25,2]
  for j,(a,b) in enumerate(zip(cuts,cuts[1:])):
   o=cube('Editable limestone slab '+str(row)+' '+str(j),((a+b)/2-1,row*.5-.75,-.012),(b-a-.005,.495,.06),stone,.006)
   # UV phase varies the natural stone, without moving physical seams.
   for d in o.data.uv_layers.active.data:d.uv.x+=variant*.173+row*.071
begin('StoneWall_2m',['D02.ASSET.ENV.STUDIO','D02.ASSET.ENV.ARCHIVE','D02.ASSET.ENV.REVIEW'])
cube('Wall core',(0,0,1.8),(2,.22,3.6),mortar,.008)
for row in range(9):
 cuts=[0,.25,.75,1.25,1.75,2] if row%2 else [0,.5,1,1.5,2]
 for j,(a,b) in enumerate(zip(cuts,cuts[1:])):cube('Editable limestone block '+str(row)+' '+str(j),((a+b)/2-1,-.126,row*.4+.2),(b-a-.005,.035,.395),stone,.006)
cube('Skirting',(0,-.035,.12),(2,.3,.24),trim,.012)
cube('Cornice',(0,0,3.5),(2,.36,.18),trim,.015)
begin('Awning_4m',['D02.ASSET.ENV.PORTALS'])
verts=[(-1.95,.10,3.34),(1.95,.10,3.34),(-1.95,-1.15,2.93),(1.95,-1.15,2.93),(-1.95,-1.15,2.76),(1.95,-1.15,2.76),(-1.95,.10,3.17),(1.95,.10,3.17)]
mesh=bpy.data.meshes.new('Editable canopy envelope');mesh.from_pydata(verts,[],[(0,2,3,1),(2,4,5,3),(0,6,4,2),(1,3,5,7),(0,1,7,6),(4,6,7,5)]);mesh.update()
o=bpy.data.objects.new('Teal canvas roof',mesh);col.objects.link(o);o.parent=root;mesh.materials.append(teal)
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
bevel=o.modifiers.new('Editable stitched edge','BEVEL');bevel.width=.012;bevel.segments=3;o.modifiers.new('Weighted canvas normals','WEIGHTED_NORMAL')
for x in [-1.82,1.82]:cube('Slim canopy support',(x,-1.05,1.42),(.065,.065,2.84),metal,.012)
for x in [-1.9,1.9]:cube('Seam piping',(x,-.525,3.135),(.018,1.314,.018),teal,.008).rotation_euler.x=math.radians(18.16)
# Dedicated full-UV stone bake; lighting excluded.
bake=bpy.data.scenes.new('Limestone native bake');bpy.context.window.scene=bake;bake.render.engine='CYCLES';bake.cycles.samples=1
bpy.ops.mesh.primitive_plane_add(size=1);plane=bpy.context.object;plane.name='Full UV stone surface';plane.data.materials.append(stone)
for kind in ['BaseColor','Normal']:
 image=bpy.data.images.new('Limestone_'+kind,1024,1024,alpha=False);image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
 node=nodes.new('ShaderNodeTexImage');node.name='Export '+kind;node.image=image;nodes.active=node
 bake.render.bake.margin=8;bake.render.bake.use_pass_direct=False;bake.render.bake.use_pass_indirect=False;bake.render.bake.use_pass_color=True
 bpy.ops.object.bake(type='DIFFUSE' if kind=='BaseColor' else 'NORMAL')
 image.filepath_raw=str(tex/('Limestone_'+kind+'.png'));image.file_format='PNG';image.save();image.pack()
bpy.context.window.scene=s
manifest=[]
for asset in assets:
 preview=bpy.data.scenes.new('Temporary export '+asset.name);bpy.context.window.scene=preview
 copies=[]
 for original in asset.children_recursive:
  if original.type!='MESH':continue
  o=original.copy();o.data=original.data.copy();preview.collection.objects.link(o);o.parent=None;o.matrix_world=original.matrix_world.copy();copies.append(o)
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();joined=bpy.context.object
 joined.name=asset.name;bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bm=bmesh.new();bm.from_mesh(joined.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(joined.data);bm.free()
 bpy.ops.export_scene.fbx(filepath=str(out/(asset.name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='AUTO')
 manifest.append({'name':asset.name,'native_mesh_parts':sum(o.type=='MESH' for o in asset.children_recursive),'export_vertices':len(joined.data.vertices),'export_polygons':len(joined.data.polygons),'materials':[m.name for m in joined.data.materials]})
 bpy.data.scenes.remove(preview);bpy.context.window.scene=s
# Neutral source assembly, always preserved separately from exported pivots.
for i,a in enumerate(assets):a.location=((i%3)*2.3,(i//3)*3,0)
ld=bpy.data.lights.new('Warm afternoon key','AREA');light=bpy.data.objects.new('Warm afternoon key',ld);s.collection.objects.link(light);light.location=(-3,-5,9);ld.energy=1500;ld.size=5;light.rotation_euler=(Vector((2,1,0))-light.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Source camera');camera=bpy.data.objects.new('Source camera',cd);s.collection.objects.link(camera);s.camera=camera;camera.location=(9,-11,9);camera.rotation_euler=(Vector((2,1.5,1))-camera.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=11
s.render.resolution_x=1400;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.render.filepath=str(R/'LocalOnly/Captures/Environment/MasonryKit_v002.png')
payload={'assets':manifest,'materials':[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value} for m in [stone,trim,mortar,teal,metal]],'textures':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in tex.glob('*.png')},'status':'Native sources and derivative textures. Unity acceptance pending.'}
(out/'manifest.json').write_text(json.dumps(payload,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(payload))
