"""Owned native 3D UI still life. No text baked into the transparent renders."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[3];out=R/'SourceArt/Demo02/UI/ReviewStillLife_v001';out.mkdir(parents=True,exist_ok=True)
dest=out/'ReviewStillLife_v001.blend'
if dest.exists():raise RuntimeError('Preserve authored UI source')
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.name='Editable review paper and authority objects'
def mat(name,color,rough=.5,metal=0,paper=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
 if paper:
  n=m.node_tree.nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=250;n.inputs['Detail'].default_value=2;b=m.node_tree.nodes.new('ShaderNodeBump');b.inputs['Strength'].default_value=.14;b.inputs['Distance'].default_value=.0012;m.node_tree.links.new(n.outputs['Fac'],b.inputs['Height']);m.node_tree.links.new(b.outputs['Normal'],p.inputs['Normal'])
 return m
navy=mat('Bound navy cloth',(.025,.105,.19),.75,paper=True);paper=mat('Warm unprinted paper',(.86,.81,.69),.86,paper=True);fresh=mat('Current unprinted paper',(.96,.925,.82),.86,paper=True);gold=mat('Brushed warm brass',(.65,.37,.085),.31,.7);ink=mat('Deep blue enamel',(.014,.04,.075),.32);leather=mat('Folio leather',(.10,.18,.22),.65,paper=True)
def cube(name,loc,size,material,bevel=.015,parent=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 if bevel:mod=o.modifiers.new('Editable softened edge','BEVEL');mod.width=bevel;mod.segments=3;o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
 if parent:o.parent=parent
 return o
def empty(name,loc=(0,0,0)):
 o=bpy.data.objects.new(name,None);s.collection.objects.link(o);o.location=loc;return o
base=empty('Reusable paper and folio assembly')
for i,(w,d) in enumerate([(.82,.50),(.73,.54),(.80,.48)]):
 z=.055+i*.095
 for dz in [-.032,.032]:cube('Cloth cover '+str(i),(0,0,z+dz),(w,d,.017),navy,.012,base)
 cube('Visible paper edges '+str(i),(0,-.007,z),(w-.045,d-.025,.054),paper,.006,base)
 cube('Fine brass spine rule '+str(i),(0,-d/2-.002,z),(w-.035,.007,.008),gold,.002,base)
board=empty('Editable clipboard',(-.06,.12,.67));board.rotation_euler=(math.radians(70),0,math.radians(-5))
cube('Folio board',(0,0,0),(.58,.76,.035),leather,.035,board)
for i in range(4):cube('Unprinted layered page '+str(i),(i*.003,-i*.003,.023+i*.0025),(.51,.69,.002),fresh if i==3 else paper,.004,board)
cube('Brass binder clip',(0,.33,.055),(.23,.065,.038),gold,.012,board)
cube('Binder inner shadow',(0,.315,.077),(.15,.025,.008),ink,.004,board)
for i in range(4):cube('Raised page rule '+str(i),(-.025,.19-i*.075,.036),(.33 if i<3 else .21,.004,.0007),paper,.0005,board)
def tube(name,points,radius,material,parent=None):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=radius;cu.bevel_resolution=4;sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
 for p,v in zip(sp.points,points):p.co=(*v,1)
 o=bpy.data.objects.new(name,cu);s.collection.objects.link(o);o.data.materials.append(material)
 if parent:o.parent=parent
 return o
lock=empty('Closed authority lock',(.28,-.25,.42));lock.rotation_euler.z=math.radians(-8)
cube('Lock body',(0,0,0),(.30,.105,.27),gold,.035,lock);cube('Lock inset face',(0,-.056,0),(.25,.012,.215),gold,.024,lock)
cube('Keyway',(0,-.066,-.01),(.022,.004,.055),ink,.009,lock)
closed=[]
for i in range(25):
 t=math.pi*i/24;closed.append((.102*math.cos(t),0,.14+.102*math.sin(t)))
tube('Closed curved shackle',[(.102,0,.10)]+closed+[(-.102,0,.10)],.018,gold,lock)
openlock=empty('Released open shackle',lock.location);openlock.rotation_euler=lock.rotation_euler
cube('Released lock body',(0,0,0),(.30,.105,.27),gold,.035,openlock);cube('Released keyway',(0,-.056,-.01),(.022,.004,.055),ink,.009,openlock)
tube('Open curved shackle',[(-.10,0,.10),(-.10,0,.20)]+[(-.10+.102*(1-math.cos(math.pi*i/24)),0,.20+.102*math.sin(math.pi*i/24)) for i in range(25)]+[(.104,0,.175)],.018,gold,openlock)
def disc(name,parent,loc,r,material):
 bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=r,depth=.018,location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object;o.name=name;o.data.materials.append(material);o.parent=parent;mod=o.modifiers.new('Soft rim','BEVEL');mod.width=.008;mod.segments=3;return o
clock=empty('Actual requested review clock',(-.27,-.24,.48));disc('Clock brass rim',clock,(0,0,0),.155,gold);disc('Clock unnumbered face',clock,(0,-.013,0),.135,fresh);tube('Clock hands',[(0,-.028,.09),(0,-.028,0),(.07,-.028,-.04)],.009,ink,clock)
hold=empty('Owned hold marker',(.26,-.24,.45));disc('Hold enamel badge',hold,(0,0,0),.17,ink);disc('Hold brass inset',hold,(0,-.014,0),.15,gold)
for x in [-.043,.043]:cube('Hold pause bar',(x,-.032,0),(.035,.012,.16),ink,.005,hold)
stale=empty('Different current and historical papers',(-.27,-.25,.55));stale.rotation_euler=(math.radians(73),0,math.radians(10))
cube('Historical sheet',(-.045,-.04,0),(.26,.34,.015),paper,.01,stale);cube('Current sheet',(.04,.035,.026),(.26,.34,.015),fresh,.01,stale)
returned=empty('Returned document arrow',(-.24,-.23,.50));tube('Return arrow',[(.12,0,.01),(-.11,0,.01),(-.035,0,.085),(-.11,0,.01),(-.035,0,-.065)],.018,gold,returned)
seal=empty('Actual accepted receipt seal',(-.25,-.23,.49));disc('Receipt enamel',seal,(0,0,0),.14,ink);tube('Receipt check',[(-.075,-.024,.0),(-.018,-.024,-.055),(.09,-.024,.075)],.014,gold,seal)
variants={'locked':[lock],'pending':[lock,clock],'returned':[lock,returned],'stale':[lock,stale],'approved':[lock,seal],'released':[openlock,seal],'held':[hold],'unverified-hold':[hold,clock]}
allroots=[lock,openlock,clock,hold,stale,returned,seal]
def visibility(root,show):
 for o in [root]+list(root.children_recursive):o.hide_render=not show
for root in allroots:visibility(root,root in variants['locked'])
world=bpy.data.worlds.new('Soft neutral studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.52,.60,.69,1);world.node_tree.nodes['Background'].inputs[1].default_value=.3;s.world=world
for name,loc,power,size in [('Key',(-3,-4,5),600,4),('Fill',(4,-1,3),350,3),('Rim',(1,3,4),450,3)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);s.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Editable icon orthographic camera');cam=bpy.data.objects.new('Editable icon orthographic camera',data);s.collection.objects.link(cam);s.camera=cam;cam.location=(1.6,-5,2.5);cam.rotation_euler=(Vector((0,0,.57))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=1.35
s.render.engine='CYCLES';s.cycles.samples=24;s.render.threads_mode='FIXED';s.render.threads=4;s.render.resolution_x=720;s.render.resolution_y=840;s.render.resolution_percentage=100;s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.view_settings.view_transform='AgX'
s['status']='Editable native semantic UI props. No baked lettering. Runtime sprite choice must read actual state.';bpy.ops.wm.save_as_mainfile(filepath=str(dest))
for name in ['locked','stale']:
 for root in allroots:visibility(root,root in variants[name])
 s.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
(out/'recipe.json').write_text(json.dumps({'source':str(dest.relative_to(R)),'states':list(variants),'text_baked':False,'rights':'Owned native procedural geometry and materials','status':'Representative renders awaiting visual review before remaining variants'},indent=2))
