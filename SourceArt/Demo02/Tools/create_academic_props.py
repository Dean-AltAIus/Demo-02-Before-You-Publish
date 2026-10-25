"""Editable campus furnishings with local native geometry and bounded exports."""
import bpy,bmesh,math,random,json
from mathutils import Vector
from pathlib import Path
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish');dest=R/'SourceArt/Demo02/Environment/AcademicProps_v001.blend';out=R/'SourceArt/Demo02/Exports/AcademicProps_v001'
if dest.exists() or out.exists():raise RuntimeError('Preserve existing academic props')
out.mkdir(parents=True);s=bpy.data.scenes.new('Demo02_AcademicProps_v001');bpy.context.window.scene=s;s.unit_settings.system='METRIC';s.render.engine='CYCLES';s.cycles.samples=20
s.world=bpy.data.worlds.new('Neutral preview');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45
def mat(name,c,rough=.6,metal=0):
 m=bpy.data.materials.new('D02_'+name);m.use_nodes=True;m.diffuse_color=(*c,1);p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;return m
cream=mat('PropsCeramic',(.81,.72,.55),.32);soil=mat('PropsSoil',(.055,.032,.018),.95);green=mat('PropsLeaf',(.035,.16,.045),.78);fresh=mat('PropsLeafFresh',(.105,.28,.055),.78)
navy=mat('PropsCobalt',(.014,.11,.43),.42);teal=mat('PropsTeal',(.015,.31,.33),.4);paper=mat('PropsPaper',(.84,.79,.63),.85);gold=mat('PropsGold',(.68,.42,.07),.45);brass=mat('PropsBrass',(.51,.31,.10),.32,.75);ink=mat('PropsInk',(.018,.028,.038),.42,.25)
assets=[];root=None;col=None
def begin(name):
 global root,col
 col=bpy.data.collections.new('D02_'+name);s.collection.children.link(col);root=bpy.data.objects.new(name,None);col.objects.link(root);root['authoring']='Native editable dressing, not learner evidence.';assets.append(root)
def adopt(o,name,material):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=root;o.data.materials.append(material);return o
def cube(name,loc,size,material,bevel=.006):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=adopt(bpy.context.object,name,material);o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  b=o.modifiers.new('Editable soft edge','BEVEL');b.width=bevel;b.segments=3;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 return o
def cyl(name,loc,radius,depth,material):
 bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=depth,location=loc);o=adopt(bpy.context.object,name,material)
 for p in o.data.polygons:p.use_smooth=True
 b=o.modifiers.new('Editable edge roll','BEVEL');b.width=min(.006,radius*.2);b.segments=3;return o
def rod(name,a,b,r,material):
 a=Vector(a);b=Vector(b);o=cyl(name,(a+b)/2,r,(b-a).length,material);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def mesh(name,verts,faces,material):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);col.objects.link(o);o.parent=root;me.materials.append(material);return o
begin('PottedPlant')
# Turned ceramic profile, native quad strips with a real upper rim.
profile=[(.09,0),(.105,.025),(.145,.27),(.148,.295),(.132,.30),(.125,.275),(.095,.025)]
verts=[(math.cos(a*math.tau/48)*r,math.sin(a*math.tau/48)*r,z) for r,z in profile for a in range(48)]
faces=[]
for ring in range(len(profile)-1):
 for a in range(48):faces.append((ring*48+a,ring*48+(a+1)%48,(ring+1)*48+(a+1)%48,(ring+1)*48+a))
pot=mesh('Turned editable ceramic',verts,faces,cream)
for p in pot.data.polygons:p.use_smooth=True
cyl('Soil',(0,0,.273),.126,.018,soil)
rng=random.Random(25)
for i in range(17):
 angle=i*2.4;length=rng.uniform(.35,.64);radius=rng.uniform(.19,.38);base=Vector((rng.uniform(-.035,.035),rng.uniform(-.035,.035),.28));tip=Vector((math.cos(angle)*radius,math.sin(angle)*radius,.36+length*.85));stemend=base.lerp(tip,.35);rod('Leaf stem '+str(i),base,stemend,.004,green)
 vertices=[];rows=10
 side=Vector((-math.sin(angle),math.cos(angle),0))
 for j in range(rows):
  t=j/(rows-1);middle=base.lerp(tip,t)+Vector((0,0,.11*math.sin(math.pi*t)));width=.095*math.sin(math.pi*t)**.8
  vertices.extend([tuple(middle-side*width),tuple(middle+Vector((0,0,.015*math.sin(math.pi*t)))),tuple(middle+side*width)])
 faces=[(j*3+k,j*3+k+1,(j+1)*3+k+1,(j+1)*3+k) for j in range(rows-1) for k in [0,1]]
 leaf=mesh('Broad smooth leaf '+str(i),vertices,faces,green if i%3 else fresh)
 for p in leaf.data.polygons:p.use_smooth=True
 sol=leaf.modifiers.new('Editable leaf thickness','SOLIDIFY');sol.thickness=.001
begin('DeskLamp')
cyl('Weighted base',(0,0,.018),.09,.036,brass);rod('Angled arm',(0,0,.03),(.025,.04,.31),.013,brass);rod('Shade support',(.025,.04,.31),(.015,-.035,.38),.011,brass)
verts=[];faces=[]
for y,r in [(-.055,.105),(.055,.105),(.055,.09),(-.055,.09)]:
 for i in range(25):
  a=math.pi*i/24;verts.append((math.cos(a)*r,y-.035,.375+math.sin(a)*r*.6))
for row in range(4):
 for i in range(24):faces.append((row*25+i,row*25+i+1,((row+1)%4)*25+i+1,((row+1)%4)*25+i))
shade=mesh('Teal curved shade',verts,faces,teal)
for p in shade.data.polygons:p.use_smooth=True
rod('Frosted lamp tube',(-.07,-.035,.385),(.07,-.035,.385),.012,paper)
begin('ReferenceBooks')
for i in range(4):
 z=.022+i*.043;w=.25-i*.012;d=.19+i*.009;color=[navy,teal,gold,navy][i]
 cube('Paper block '+str(i),(0,0,z),(w-.01,d-.01,.032),paper,.002)
 for side in [-1,1]:cube('Book cover '+str(i)+' '+str(side),(0,0,z+side*.018),(w,d,.004),color,.002)
 cube('Spine '+str(i),(-w/2,0,z),(.006,d,.038),color,.003)
begin('CampusBanner')
rod('Banner arm',(-.34,0,1.38),(.34,0,1.38),.018,ink)
verts=[];faces=[]
for row in range(17):
 for colno in range(9):
  x=(colno/8-.5)*.58;z=.52+row/16*.82;y=.028*math.sin(colno/8*math.pi*2+row*.14);verts.append((x,y,z))
for row in range(16):
 for c in range(8):a=row*9+c;faces.append((a,a+1,a+10,a+9))
cloth=mesh('Editable draped banner',verts,faces,navy)
for p in cloth.data.polygons:p.use_smooth=True
so=cloth.modifiers.new('Editable cloth thickness','SOLIDIFY');so.thickness=.003
# Open book symbol is native geometry, no baked text or brand substitution.
for sign in [-1,1]:
 points=[(0,-.045,.88),(sign*.19,-.045,.93),(sign*.19,-.045,1.16),(0,-.045,1.11),(0,-.045,.88)]
 for j in range(4):rod('Book symbol '+str(sign)+' '+str(j),points[j],points[j+1],.009,paper)
 for j in range(2):rod('Book page '+str(sign)+' '+str(j),(sign*.03,-.045,1.02+j*.045),(sign*.16,-.045,1.055+j*.045),.004,paper)
manifest=[]
for a in assets:
 scratch=bpy.data.scenes.new('Export '+a.name);bpy.context.window.scene=scratch;copies=[]
 for src in a.children_recursive:
  if src.type!='MESH':continue
  o=src.copy();o.data=src.data.copy();scratch.collection.objects.link(o);o.parent=None;o.matrix_world=src.matrix_world.copy();copies.append(o);o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();o=bpy.context.object;o.name=a.name;bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
 bpy.ops.export_scene.fbx(filepath=str(out/(a.name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='AUTO')
 manifest.append({'name':a.name,'source_mesh_parts':len(copies),'export_vertices':len(o.data.vertices),'materials':[m.name for m in o.data.materials]});bpy.data.scenes.remove(scratch);bpy.context.window.scene=s
for i,a in enumerate(assets):a.location=((i%2)*.9,(i//2)*1.1,0)
ld=bpy.data.lights.new('Soft source key','AREA');lo=bpy.data.objects.new('Soft source key',ld);s.collection.objects.link(lo);lo.location=(-2,-3,5);ld.energy=650;ld.size=3;lo.rotation_euler=(Vector((.4,.5,.5))-lo.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Source inspection');cam=bpy.data.objects.new('Source inspection',cd);s.collection.objects.link(cam);s.camera=cam;cam.location=(3,-4,3);cam.rotation_euler=(Vector((.45,.5,.65))-cam.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=3.3
s.render.resolution_x=1200;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.render.filepath=str(R/'LocalOnly/Captures/Environment/AcademicProps_v001.png')
used={m.name:m for a in assets for o in a.children_recursive if o.type=='MESH' for m in o.data.materials}
payload={'assets':manifest,'materials':[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value,'metallic':m.node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value} for m in used.values()],'status':'Native source dressing only. Unity visual acceptance pending.'}
(out/'manifest.json').write_text(json.dumps(payload,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(payload))
