"""Versioned, editable local courtyard closure. No distant scenery or live-session edits."""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Environment/CourtyardClosure_v001.blend'
out=R/'SourceArt/Demo02/Exports/CourtyardClosure_v001'
if dest.exists() or out.exists(): raise RuntimeError('Preserve existing closure authoring')
out.mkdir(parents=True)
# Reuse the approved native stone surfaces. No duplicate texture generation.
with bpy.data.libraries.load(str(R/'SourceArt/Demo02/Environment/MasonryKit_v002.blend'),link=False) as (src,dst):
 dst.materials=['D02_LimestoneBaked','D02_MasonryTrim','D02_MasonryMortar']
stone,trim,mortar=[bpy.data.materials[n] for n in ['D02_LimestoneBaked','D02_MasonryTrim','D02_MasonryMortar']]
s=bpy.data.scenes.new('Demo02_CourtyardClosure_v001');bpy.context.window.scene=s
s.unit_settings.system='METRIC';s.unit_settings.scale_length=1
def mat(name,c,rough=.7,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*c,1)
 p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;return m
slate=mat('D02_ClosureSlate',(.045,.11,.17),.68)
plaster=mat('D02_ClosurePlaster',(.76,.68,.53),.85)
teal=mat('D02_ClosureGate',(.018,.22,.235),.63)
glass=mat('D02_ClosureGlass',(.12,.28,.36),.32)
ink=mat('D02_ClosureIron',(.024,.046,.06),.45,.25)
brass=mat('D02_ClosureBrass',(.49,.30,.07),.4,.55)
assets=[];root=None;collection=None
def begin(name):
 global root,collection
 collection=bpy.data.collections.new(name+' editable parts');s.collection.children.link(collection)
 root=bpy.data.objects.new(name,None);collection.objects.link(root);assets.append(root)
 root['purpose']='Finish the existing local courtyard footprint; no additional scenery layer.'
def adopt(o,name,material):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 collection.objects.link(o);o.parent=root;o.data.materials.append(material);return o
def cube(name,loc,size,material,bevel=.008):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=adopt(bpy.context.object,name,material);o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 uv=o.data.uv_layers.active
 for p in o.data.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=[i for i in range(3) if i!=axis]
  for index in p.loop_indices:
   v=o.data.vertices[o.data.loops[index].vertex_index].co;uv.data[index].uv=(v[axes[0]]+loc[axes[0]],v[axes[1]]+loc[axes[1]])
 if bevel:
  b=o.modifiers.new('Editable edge softness','BEVEL');b.width=bevel;b.segments=2
  o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL')
 return o
def mesh(name,verts,faces,material):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);collection.objects.link(o);o.parent=root;me.materials.append(material);return o
def rod(name,a,b,width,material):
 a,b=Vector(a),Vector(b);o=cube(name,(a+b)/2,(width,width,(b-a).length),material,.004);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
begin('GardenWall_2m')
cube('Solid continuous wall',(0,0,1.13),(2,.31,2.26),mortar,0)
for row in range(4):
 cuts=[-1,0,1] if row%2==0 else [-1,-.5,.5,1]
 for j,(a,b) in enumerate(zip(cuts,cuts[1:])):
  cube('Limestone course %s block %s'%(row,j),((a+b)/2,0,.33+row*.51),(b-a-.008,.36,.502),stone)
cube('Foot course',(0,0,.095),(2,.42,.19),trim,.012)
cube('Upper cornice',(0,0,2.20),(2,.40,.16),trim,.012)
cube('Weathered coping',(0,0,2.32),(2,.46,.13),trim,.018)
begin('GardenCornerPost')
cube('Solid stone pier',(0,0,1.21),(.5,.5,2.42),stone,.015)
cube('Pier footing',(0,0,.105),(.58,.58,.21),trim,.018)
cube('Pier neck',(0,0,2.39),(.55,.55,.16),trim,.012)
cube('Pier cap',(0,0,2.53),(.66,.66,.14),trim,.02)
begin('RoomRoof_6x4m')
cube('Plaster ceiling',(0,0,3.54),(6.30,4.30,.12),plaster,.008)
cube('Cream eave',(0,0,3.65),(6.48,4.48,.14),trim,.015)
# Closed hip roof, two ridge endpoints and four eave corners, outward normals audited below.
v=[(-3.3,-2.3,3.72),(3.3,-2.3,3.72),(3.3,2.3,3.72),(-3.3,2.3,3.72),(-1.2,0,4.52),(1.2,0,4.52)]
o=mesh('Closed slate hip roof',v,[(0,1,5,4),(1,2,5),(2,3,4,5),(3,0,4),(3,2,1,0)],slate)
b=o.modifiers.new('Editable slate edge','BEVEL');b.width=.015;b.segments=2;o.modifiers.new('Weighted roof normals','WEIGHTED_NORMAL')
rod('Ridge cap',(-1.23,0,4.535),(1.23,0,4.535),.075,slate)
begin('FrontWindowBay_1m')
cube('Solid facade bay',(0,0,1.8),(1,.36,3.6),stone,.009)
cube('Facade plinth',(0,0,.09),(1,.43,.18),trim,.012)
cube('Facade crown',(0,0,3.51),(1,.43,.18),trim,.012)
# An opaque arched inset and real surrounding frame, with no hole through the room.
profile=[(-.33,1.05),(.33,1.05),(.33,2.6)]+[(math.cos(a)*.33,2.6+math.sin(a)*.33) for a in [math.pi*i/16 for i in range(1,17)]]
o=mesh('Blue arched inset',[(x,-.194,z) for x,z in profile],[tuple(range(len(profile)))],glass)
for i,(x,z) in enumerate(profile):
 nx,nz=profile[(i+1)%len(profile)];rod('Arched frame %02d'%i,(x,-.217,z),(nx,-.217,nz),.045,ink)
rod('Centre mullion',(0,-.224,1.06),(0,-.224,2.9),.035,ink)
rod('Cross mullion',(-.32,-.224,1.96),(.32,-.224,1.96),.035,ink)
cube('Window sill',(0,-.205,1.015),(.80,.25,.09),trim,.014)
begin('CourtyardGate_4m')
for side in [-1,1]:
 x=side*2.0
 cube('Entry pier '+str(side),(x,0,1.55),(.44,.50,3.1),stone,.012)
 cube('Entry pier foot '+str(side),(x,0,.12),(.56,.62,.24),trim,.015)
 cube('Entry pier capital '+str(side),(x,0,3.03),(.56,.62,.20),trim,.015)
cube('Entry lintel',(0,0,3.16),(4.5,.54,.22),trim,.018)
cube('Entry cap',(0,0,3.30),(4.65,.68,.12),slate,.018)
for side in [-1,1]:
 x=side*.895
 cube('Closed gate backing '+str(side),(x,0,1.40),(1.775,.12,2.72),teal,.012)
 for i in range(6):
  xx=x+(i-2.5)*.287
  cube('Gate board '+str(side)+' '+str(i),(xx,-.072,1.4),(.279,.025,2.64),teal,.004)
 for z in [.36,2.43]:cube('Gate rail '+str(side)+' '+str(z),(x,-.103,z),(1.71,.055,.10),ink,.008)
 rod('Gate brace '+str(side),(x-side*.75,-.106,.42),(x+side*.75,-.106,2.37),.055,ink)
 cube('Pull plate '+str(side),(side*.16,-.12,1.35),(.075,.055,.28),brass,.012)
# Small native open-book emblem in the lintel, repeated on both sides, no invented institution name.
for y in [-.29,.29]:
 for side in [-1,1]:
  pts=[(0,y,3.12),(side*.22,y,3.17),(side*.22,y,3.28),(0,y,3.23),(0,y,3.12)]
  for i in range(4):rod('Entry book emblem',pts[i],pts[i+1],.015,brass)
manifest=[]
for a in assets:
 scratch=bpy.data.scenes.new('Export '+a.name);bpy.context.window.scene=scratch;copies=[]
 for src in a.children_recursive:
  if src.type!='MESH':continue
  o=src.copy();o.data=src.data.copy();scratch.collection.objects.link(o);o.parent=None;o.matrix_world=src.matrix_world.copy();o.select_set(True);copies.append(o)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();o=bpy.context.object;o.name=a.name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
 o.data.calc_loop_triangles();coords=[v.co for v in o.data.vertices]
 row={'name':a.name,'source_parts':len(copies),'vertices':len(coords),'triangles':len(o.data.loop_triangles),'materials':[m.name for m in o.data.materials],'min':[min(v[i] for v in coords) for i in range(3)],'max':[max(v[i] for v in coords) for i in range(3)]}
 bpy.ops.export_scene.fbx(filepath=str(out/(a.name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='AUTO')
 manifest.append(row);bpy.data.scenes.remove(scratch);bpy.context.window.scene=s
used={m.name:m for a in assets for o in a.children_recursive if o.type=='MESH' for m in o.data.materials}
payload={'assets':manifest,'materials':[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value,'metallic':m.node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value} for m in used.values()],'status':'Native source and export candidates. Actual Unity placement and multi-angle review required.'}
(out/'manifest.json').write_text(json.dumps(payload,indent=2))
for i,a in enumerate(assets):a.location=((i%3)*8,(i//3)*8,0)
s.world=bpy.data.worlds.new('Closure neutral inspection');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45
ld=bpy.data.lights.new('Source key','AREA');lo=bpy.data.objects.new('Source key',ld);s.collection.objects.link(lo);lo.location=(0,-5,15);ld.energy=2800;ld.size=10
cd=bpy.data.cameras.new('Closure source camera');cam=bpy.data.objects.new('Closure source camera',cd);s.collection.objects.link(cam);cam.location=(22,-25,20);cam.rotation_euler=(Vector((7,3,1.5))-cam.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=25;s.camera=cam
s.render.engine='CYCLES';s.cycles.samples=16;s.render.resolution_x=1280;s.render.resolution_y=900;s.render.resolution_percentage=100
s.render.filepath=str(R/'LocalOnly/Captures/Environment/CourtyardClosure_v001.png');Path(s.render.filepath).parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True)
print(json.dumps({'saved':str(dest),'assets':manifest}))
