"""Version the existing native foliage with softer blades and coherent crowns."""
import bpy,math,random,json
from pathlib import Path
from mathutils import Vector,Euler
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish');dest=R/'SourceArt/Demo02/Environment/FoliageKit_v002.blend';exports=R/'SourceArt/Demo02/Exports/FoliageKit_v002'
if dest.exists() or exports.exists():raise RuntimeError('Preserve existing foliage revision')
exports.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/DressingKit_v001.blend'))
s=bpy.data.scenes['Demo02_CampusDressing_v001'];s.name='Demo02_FoliageKit_v002';bpy.context.window.scene=s
rng=random.Random(251026)
materials=[bpy.data.materials['D02_'+n] for n in ['LeafSunlit','LeafFresh','LeafShade','LeafGold']]
for m in materials:m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.84
def mesh_from_leaves(name,instances):
 verts=[];faces=[];indices=[]
 rows=[0,.12,.30,.50,.70,.88,1]
 for leaf,(pos,length,width,angles) in enumerate(instances):
  matrix=Euler(angles).to_matrix();base=len(verts)
  for t in rows:
   half=width*.5*max(.018,math.sin(math.pi*t)**.65)
   for x,z in [(-half,.003*math.sin(math.pi*t)),(0,.018*math.sin(math.pi*t)),(half,.003*math.sin(math.pi*t))]:verts.append(tuple(Vector(pos)+matrix@Vector((x,length*t,z))))
  for i in range(len(rows)-1):
   for side in [0,1]:faces.append(tuple(base+k for k in [i*3+side,i*3+side+1,(i+1)*3+side+1,(i+1)*3+side]));indices.append(leaf%4)
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 for m in materials:mesh.materials.append(m)
 for p,index in zip(mesh.polygons,indices):p.material_index=index;p.use_smooth=True
 return mesh
def replace(rootname,meshname,instances):
 root=bpy.data.objects[rootname];o=next(o for o in root.children if o.name==meshname)
 o.data=mesh_from_leaves(meshname+' v002',instances)
 for modifier in list(o.modifiers):
  if modifier.name=='Editable leaf thickness':o.modifiers.remove(modifier)
 root['revision']='v002: smooth bowed foliage, new derivative only'
 return root
items=[]
for i in range(500):
 z=rng.uniform(0,3.4);x=.14*math.sin(z*2.5)+rng.uniform(-.36,.36)
 items.append(((x,rng.uniform(-.13,.08),z),rng.uniform(.15,.24),rng.uniform(.13,.21),(rng.uniform(1.15,1.8),rng.uniform(-.28,.28),rng.uniform(-math.pi,math.pi))))
roots=[replace('IvyColumn','Dense climbing ivy',items)]
items=[]
for i in range(460):items.append(((rng.uniform(-2,2),rng.uniform(-.1,.1),rng.uniform(-.15,.28)),rng.uniform(.16,.25),rng.uniform(.13,.22),(rng.uniform(1,1.8),rng.uniform(-.3,.3),rng.uniform(-math.pi,math.pi))))
roots.append(replace('IvyLintel','Soft lintel foliage',items))
items=[];clusters=[(-.53,0,2.95),(.52,.1,3.12),(0,-.52,3.22),(0,.5,3.2),(0,0,3.7)]
for i in range(1400):
 c=Vector(clusters[i%len(clusters)]);theta=rng.random()*math.tau;phi=math.acos(rng.uniform(-1,1));radius=rng.uniform(.6,.95)
 p=c+Vector((math.sin(phi)*math.cos(theta)*radius,math.sin(phi)*math.sin(theta)*radius,math.cos(phi)*radius*.72))
 items.append((tuple(p),rng.uniform(.20,.33),rng.uniform(.16,.25),(rng.uniform(-.65,.65),rng.uniform(-.45,.45),rng.random()*math.tau)))
roots.append(replace('TreeCanopy','Full sunlit canopy',items))
items=[]
for i in range(340):items.append(((rng.uniform(-.52,.52),rng.uniform(-.27,.27),rng.uniform(.57,.91)),rng.uniform(.17,.26),rng.uniform(.12,.20),(rng.uniform(-.5,.65),rng.uniform(-.25,.25),rng.random()*math.tau)))
roots.append(replace('PlanterCrown','Dense mixed planter leaves',items))
# Complete reusable tree: native bark parts plus one shared canopy mesh, rather than leaf objects.
collection=bpy.data.collections.new('D02_FullTree_v002');s.collection.children.link(collection)
tree=bpy.data.objects.new('FullTree',None);collection.objects.link(tree);tree['production_ids']='D02.ASSET.ENV.COURTYARD'
bark=bpy.data.materials['D02_NoticeOak']
def rod(name,a,b,radius):
 a=Vector(a);b=Vector(b);bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=radius,radius2=radius*.72,depth=(b-a).length,location=(a+b)/2)
 o=bpy.context.object
 for c in list(o.users_collection):c.objects.unlink(o)
 collection.objects.link(o);o.parent=tree;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();o.data.materials.append(bark)
 for p in o.data.polygons:p.use_smooth=True
rod('Editable tree trunk',(0,0,0),(.07,0,2.48),.10)
for i in range(7):
 a=i*2.4;rod('Editable tree branch '+str(i),(.04,0,1.6+i*.1),(math.cos(a)*.72,math.sin(a)*.72,2.6+(i%3)*.18),.035)
canopy=next(o for o in roots[2].children if o.type=='MESH');copy=bpy.data.objects.new('Shared smooth canopy',canopy.data);collection.objects.link(copy);copy.parent=tree
roots.append(tree)
manifest=[]
for root in roots:
 old=root.location.copy();root.location=(0,0,0);bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT')
 for o in [root]+list(root.children_recursive):o.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(exports/(root.name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
 manifest.append({'name':root.name,'mesh_parts':sum(o.type=='MESH' for o in root.children_recursive),'source_vertices':sum(len(o.data.vertices) for o in root.children_recursive if o.type=='MESH')});root.location=old
tree.location=(6,6,0)
for n in ['D02_NoticeBoard','D02_AcademicSkyline','D02_CampusGround']:bpy.data.collections[n].hide_render=True
s.camera.data.ortho_scale=22;s.camera.location=(17,-20,15);s.camera.rotation_euler=(Vector((6,2.5,1.8))-s.camera.location).to_track_quat('-Z','Y').to_euler()
s.render.resolution_x=1400;s.render.resolution_y=1000;s.cycles.samples=16;s.render.filepath=str(R/'LocalOnly/Captures/Environment/FoliageKit_v002.png')
used={m.name:m for root in roots for o in root.children_recursive if o.type=='MESH' for m in o.data.materials if m}
(exports/'manifest.json').write_text(json.dumps({'assets':manifest,'materials':[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value} for m in used.values()],'status':'Versioned native source. Unity visual/performance acceptance pending; original kit preserved.'},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(manifest))
