"""Batch native campus dressing aligned to supplied campus art. Preserve prior authoring."""
import bpy,math,random,json
from pathlib import Path
from mathutils import Vector,Euler
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Environment/DressingKit_v001.blend';exports=R/'SourceArt/Demo02/Exports/DressingKit_v001'
if dest.exists() or exports.exists():raise RuntimeError('Preserve existing dressing source and export')
exports.mkdir(parents=True)
s=bpy.data.scenes.new('Demo02_CampusDressing_v001');bpy.context.window.scene=s;s.unit_settings.system='METRIC';s.render.engine='CYCLES';s.cycles.samples=12;s.render.threads_mode='FIXED';s.render.threads=4
s.world=bpy.data.worlds.new('Campus dressing studio');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.55,.68,.86,1);s.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
def mat(name,color,rough=.65):
 m=bpy.data.materials.new('D02_'+name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;return m
leaves=[mat('LeafSunlit',(.42,.59,.055)),mat('LeafFresh',(.24,.42,.025)),mat('LeafShade',(.105,.25,.018)),mat('LeafGold',(.53,.58,.055))]
stone=mat('PaleCampusStone',(.78,.65,.45));brick=mat('AcademicTerracotta',(.51,.26,.16));slate=mat('CampusSlate',(.055,.14,.22));glass=mat('DistantWindow',(.06,.13,.18));cream=mat('NoticePaper',(.93,.89,.74));blue=mat('CampusBlue',(.014,.14,.53),.35);wood=mat('NoticeOak',(.48,.25,.08));soil=mat('GardenSoil',(.09,.055,.025));gold=mat('SunflowerPetal',(.95,.60,.035));purple=mat('LavenderFlower',(.30,.12,.42));lawn=mat('CampusLawn',(.24,.34,.10))
assets=[]
def begin(name,ids):
 c=bpy.data.collections.new('D02_'+name);s.collection.children.link(c);r=bpy.data.objects.new(name,None);c.objects.link(r);r['production_ids']=ids;r['authoring']='Native mesh source. Versioned generation refuses overwrite.';assets.append((r,c));return r,c
def adopt(obj,name,root,collection,material):
 for c in list(obj.users_collection):c.objects.unlink(obj)
 collection.objects.link(obj);obj.parent=root;obj.name=name;obj.data.materials.append(material);return obj
def box(name,pos,size,root,c,m,bevel=.025):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=adopt(bpy.context.object,name,root,c,m);o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:mod=o.modifiers.new('Editable softened edge','BEVEL');mod.width=bevel;mod.segments=2;o.modifiers.new('Corner normals','WEIGHTED_NORMAL')
 return o
def leaf_mesh(name,instances,root,c,materials=leaves):
 verts=[];faces=[];indices=[]
 # Seven-vertex bowed blade; shared mesh and material slots avoid one renderer per leaf.
 for i,(position,length,width,angles) in enumerate(instances):
  matrix=Euler(angles).to_matrix();base=len(verts)
  for v in [(0,0,0),(-width*.45,length*.25,.02),(-width*.5,length*.58,.025),(0,length,.008),(width*.5,length*.58,.025),(width*.45,length*.25,.02),(0,length*.44,.045)]:verts.append(tuple(Vector(position)+matrix@Vector(v)))
  for f in [(0,1,6),(1,2,6),(2,3,6),(3,4,6),(4,5,6),(5,0,6)]:faces.append(tuple(base+k for k in f));indices.append(i%len(materials))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);c.objects.link(o);o.parent=root
 for material in materials:mesh.materials.append(material)
 for p,index in zip(mesh.polygons,indices):p.material_index=index;p.use_smooth=True
 thick=o.modifiers.new('Editable leaf thickness','SOLIDIFY');thick.thickness=.003;return o
rng=random.Random(20261025)
r,c=begin('IvyColumn','D02.ASSET.ENV.COURTYARD');r['width_m']=.85;r['height_m']=3.4
items=[]
for i in range(420):
 z=rng.uniform(0,3.4);x=.15*math.sin(z*3)+rng.uniform(-.42,.42);y=rng.uniform(-.16,.08)
 items.append(((x,y,z),rng.uniform(.17,.30),rng.uniform(.13,.21),(rng.uniform(.9,1.9),rng.uniform(-.4,.4),rng.uniform(-math.pi,math.pi))))
leaf_mesh('Dense climbing ivy',items,r,c)
r,c=begin('IvyLintel','D02.ASSET.ENV.COURTYARD');items=[]
for i in range(390):
 x=rng.uniform(-2,2);z=rng.uniform(-.20,.32);y=rng.uniform(-.12,.10)
 items.append(((x,y,z),rng.uniform(.18,.32),rng.uniform(.12,.23),(rng.uniform(.6,1.8),rng.uniform(-.5,.5),rng.uniform(-math.pi,math.pi))))
leaf_mesh('Soft lintel foliage',items,r,c)
r,c=begin('TreeCanopy','D02.ASSET.ENV.COURTYARD');items=[]
for i in range(1350):
 theta=rng.random()*math.tau;radius=rng.random()**.5*1.18;z=2.35+rng.random()*1.25
 items.append(((math.cos(theta)*radius,math.sin(theta)*radius,z),rng.uniform(.25,.43),rng.uniform(.17,.27),(rng.uniform(-1.3,1.3),rng.uniform(-.3,.3),rng.random()*math.tau)))
leaf_mesh('Full sunlit canopy',items,r,c)
r,c=begin('PlanterCrown','D02.ASSET.ENV.COURTYARD');items=[]
for i in range(260):
 items.append(((rng.uniform(-.53,.53),rng.uniform(-.29,.29),rng.uniform(.57,.98)),rng.uniform(.17,.32),rng.uniform(.10,.18),(rng.uniform(-1,1),rng.uniform(-.3,.3),rng.random()*math.tau)))
leaf_mesh('Dense mixed planter leaves',items,r,c)
for k in range(8):
 x=-.45+k*.13;y=.11*math.sin(k*1.7);z=1.03+.13*math.sin(k*.9)
 petals=[]
 for p in range(9):
  ang=p*math.tau/9;petals.append(((x,y,z),.10,.045,(math.pi/2,0,ang)))
 leaf_mesh('Sunflower petals '+str(k),petals,r,c,[gold]);bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=6,radius=.033,location=(x,y-.015,z));adopt(bpy.context.object,'Sunflower seed head',r,c,wood)
r,c=begin('NoticeBoard','D02.ASSET.ENV.STUDIO')
box('Oak frame',(0,0,0),(1.3,.055,.9),r,c,wood);box('Cream notice backing',(0,-.035,0),(1.2,.025,.8),r,c,cream)
for k in range(6):
 x=(k%3-1)*.37;z=(.19 if k<3 else -.19)
 box('Editable blank notice '+str(k),(x,-.055,z),(.30,.012,.29),r,c,cream,.003);box('Notice header '+str(k),(x,-.063,z+.105),(.25,.007,.035),r,c,blue,.002)
r,c=begin('AcademicSkyline','D02.ASSET.ENV.COURTYARD')
for x,height in [(-8,6),(-3,9),(3,6.5),(8,8)]:
 box('Academic wing',(x,0,2.1),(4,3,4.2),r,c,brick,.06)
 box('Stone cornice',(x,0,4.2),(4.15,3.15,.22),r,c,stone,.04)
 box('Tower',(x,0,height*.5),(1.7,1.7,height),r,c,stone,.06)
 for row in range(2,int(height)-1,2):
  for side in [-.48,.48]:box('Tower window',(x+side,-.866,row),(.20,.02,.85),r,c,glass,.03)
 bpy.ops.mesh.primitive_cone_add(vertices=4,radius1=1.42,radius2=.04,depth=2.4,location=(x,0,height+1.2),rotation=(0,0,math.pi/4));adopt(bpy.context.object,'Slate tower roof',r,c,slate)
 for side in [-.73,.73]:
  box('Tower corner finial',(x+side,-.73,height+.28),(.15,.15,.65),r,c,stone,.02)
r,c=begin('CampusGround','D02.ASSET.ENV.COURTYARD');box('Surrounding lawn',(0,0,-.11),(100,100,.15),r,c,lawn,.01)
# Export derivative meshes by material, while original architectural parts remain editable in .blend.
manifest=[]
for root,c in assets:
 objects=[root]+list(root.children_recursive);bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=root;bpy.context.view_layer.update()
 bpy.ops.export_scene.fbx(filepath=str(exports/(root.name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
 pts=[o.matrix_world@Vector(v) for o in objects if o.type=='MESH' for v in o.bound_box]
 manifest.append({'name':root.name,'production_ids':root['production_ids'],'bounds_m':[max(p[j] for p in pts)-min(p[j] for p in pts) for j in range(3)],'mesh_objects':sum(o.type=='MESH' for o in objects)})
materials=[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value} for m in [*leaves,stone,brick,slate,glass,cream,blue,wood,soil,gold,purple,lawn]]
(exports/'manifest.json').write_text(json.dumps({'assets':manifest,'materials':materials,'source':'Native Blender authoring from supplied Demo 02 campus reference','validation':'Source preview and Unity integration pending'},indent=2))
# Contact-sheet style source arrangement. Ground stays on its own hidden display collection.
for i,(root,c) in enumerate(assets):
 if root.name=='CampusGround':c.hide_render=True
 else:root.location=((i%3)*6,(i//3)*6,0)
cam=bpy.data.objects.new('Dressing inspection camera',bpy.data.cameras.new('Dressing inspection camera'));s.collection.objects.link(cam);cam.data.type='ORTHO';cam.data.ortho_scale=29;cam.location=(22,-23,20);cam.rotation_euler=(Vector((6,3,2.5))-cam.location).to_track_quat('-Z','Y').to_euler();s.camera=cam
for name,loc,energy,size in [('Key',(1,-8,16),2400,12),('Fill',(14,5,12),1400,10)]:
 light=bpy.data.objects.new(name,bpy.data.lights.new(name,'AREA'));s.collection.objects.link(light);light.data.energy=energy;light.data.shape='DISK';light.data.size=size;light.location=loc;light.rotation_euler=(Vector((6,3,2))-light.location).to_track_quat('-Z','Y').to_euler()
s.render.resolution_x=1400;s.render.resolution_y=1000;s.render.filepath=str(R/'LocalOnly/Captures/Environment/DressingKit_v001.png');Path(s.render.filepath).parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(manifest))
