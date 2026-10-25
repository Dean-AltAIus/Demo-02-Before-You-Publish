"""New native architectural backdrop. Preserves existing campus and source versions."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(r'D:\Altaius\Demo-02-Before-You-Publish')
DEST = ROOT / 'SourceArt/Demo02/Environment/AcademicBackdrop_v002.blend'
EXPORT = ROOT / 'SourceArt/Demo02/Exports/AcademicBackdrop_v002'
if DEST.exists() or EXPORT.exists():
    raise RuntimeError('Versioned authoring output already exists; preserve it.')
EXPORT.mkdir(parents=True)
s = bpy.data.scenes.new('Demo02_AcademicBackdrop_v002')
bpy.context.window.scene = s
s.unit_settings.system = 'METRIC'
s.render.engine = 'CYCLES'
s.cycles.samples = 16
s.cycles.use_denoising = True
s.render.threads_mode = 'FIXED'
s.render.threads = 4
s.world = bpy.data.worlds.new('Academic backdrop daylight')
s.world.use_nodes = True
s.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.52,.68,.90,1)
s.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .7
root = bpy.data.objects.new('AcademicBackdrop', None)
s.collection.objects.link(root)
root['production_ids'] = 'D02.ASSET.ENV.COURTYARD'
root['purpose'] = 'Editable decorative campus depth, outside the playable perimeter'

def material(name, rgb, rough=.65):
    m = bpy.data.materials.new('D02_' + name)
    m.diffuse_color = (*rgb,1)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (*rgb,1)
    p.inputs['Roughness'].default_value = rough
    return m

stone = material('BackdropLimestone',(.69,.50,.30))
trim = material('BackdropCreamTrim',(.90,.76,.54))
brick = material('BackdropWarmBrick',(.55,.265,.14))
slate = material('BackdropBlueSlate',(.045,.12,.20),.42)
glass = material('BackdropWindow',(.025,.085,.13),.28)
inset = material('BackdropInset',(.21,.12,.065))
gold = material('BackdropBrass',(.59,.36,.06),.3)

def adopt(o,name,mat):
    o.name=name
    o.parent=root
    o.data.materials.append(mat)
    return o

def box(name,pos,size,mat,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    o=adopt(bpy.context.object,name,mat)
    o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        m=o.modifiers.new('Editable edge radius','BEVEL');m.width=bevel;m.segments=3
        o.modifiers.new('Surface corner normals','WEIGHTED_NORMAL')
    return o

def mesh(name,vertices,faces,mat):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    o=bpy.data.objects.new(name,data);s.collection.objects.link(o)
    return adopt(o,name,mat)

def arch(name,x,y,z,w,h,mat,border=False):
    # An extruded semicircular head and vertical jambs. Editable mesh, no image facade.
    radius=w/2;spring=h-radius
    contour=[(-radius,0),(radius,0),(radius,spring)]
    contour += [(math.cos(a)*radius,spring+math.sin(a)*radius) for a in [i*math.pi/16 for i in range(1,17)]]
    vertices=[(x+u,y+d,z+v) for d in [0,.07] for u,v in contour]
    n=len(contour);faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    o=mesh(name,vertices,faces,mat)
    if border:
        arch(name+' dark inset',x,y-.015,z+.11,w-.22,h-.20,glass)
        box(name+' central mullion',(x,y-.055,z+h*.44),(.055,.075,h*.78),trim,.009)
        box(name+' horizontal mullion',(x,y-.06,z+h*.40),(w-.2,.075,.055),trim,.009)
    return o

def gable(name,x,y,z,w,depth,rise,mat):
    verts=[(x-w/2,y-depth/2,z),(x+w/2,y-depth/2,z),(x,y-depth/2,z+rise),
           (x-w/2,y+depth/2,z),(x+w/2,y+depth/2,z),(x,y+depth/2,z+rise)]
    return mesh(name,verts,[(0,2,1),(3,4,5),(0,3,5,2),(2,5,4,1),(0,1,4,3)],mat)

def long_roof(name,x,y,z,w,depth,rise,mat):
    verts=[(x-w/2,y-depth/2,z),(x-w/2,y+depth/2,z),(x-w/2,y,z+rise),
           (x+w/2,y-depth/2,z),(x+w/2,y+depth/2,z),(x+w/2,y,z+rise)]
    return mesh(name,verts,[(0,2,1),(3,4,5),(0,3,5,2),(2,5,4,1),(0,1,4,3)],mat)

def spire(name,x,y,z,r,height):
    bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=r,radius2=.025,depth=height,location=(x,y,z+height/2))
    o=adopt(bpy.context.object,name,slate)
    m=o.modifiers.new('Soft roof edges','BEVEL');m.width=.015;m.segments=2
    box(name+' finial',(x,y,z+height+.16),(.055,.055,.34),gold,.012)

# A connected academic facade with floor divisions, inset bays and an asymmetric skyline.
box('Central lecture wing',(0,1.1,3.4),(12,4.2,6.8),brick,.06)
for z in [.35,3.45,6.75]:box('Continuous limestone course',(0,1.02,z),(12.3,4.38,.24),trim,.04)
long_roof('Central pitched slate roof',0,1.1,6.9,12.7,4.8,2.5,slate)
for x in [-5,-3,-1,1,3,5]:
    for z in [.8,3.9]:arch('Lecture wing arched window',x,-1.04,z,1.08,2.05,trim,True)
    box('Facade pilaster',(x+.85,-1.05,3.55),(.17,.22,6.3),stone,.025)
for x in [-4,0,4]:
    box('Roof dormer wall',(x,-.95,7.52),(1.5,1.1,1.25),stone)
    gable('Dormer slate roof',x,-.95,8.15,1.9,1.4,.85,slate)
    arch('Dormer window',x,-1.52,7.08,.66,.87,trim,True)

for index,(x,height,width) in enumerate([(-7.8,10.4,2.9),(7.7,13.3,2.55)]):
    box('Tower '+str(index),(x,.5,height/2),(width,3.05,height),stone,.055)
    for z in [.4,3.5,6.75,height-.55]:box('Tower cornice',(x,.5,z),(width+.3,3.35,.24),trim,.045)
    for side in [-1,1]:
        box('Tower corner buttress',(x+side*(width/2-.08),-1.05,height/2),(.28,.31,height),trim,.03)
    for z in [1,4.15,7.35]:
        for off in [-.6,.6]:arch('Tower lancet',x+off,-1.055,z,.65,1.72,trim,True)
    if height>12:
        for off in [-.58,.58]:arch('Belfry opening',x+off,-1.06,10.3,.65,1.7,trim,True)
    box('Tower roof collar',(x,.5,height),(width+.42,3.42,.28),trim,.04)
    spire('Academic spire '+str(index),x,.5,height+.14,width*.66,4.1 if index else 3.3)
    for dx in [-1,1]:
        for dy in [-1,1]:
            px=x+dx*(width/2+.04);py=.5+dy*1.55
            box('Corner pinnacle stem',(px,py,height+.52),(.25,.25,1.0),trim,.035)
            spire('Corner pinnacle',px,py,height+1.02,.22,.6)

for side in [-1,1]:
    x=side*12.2
    box('Library side wing',(x,2.7,2.4),(6.2,4.3,4.8),stone,.06)
    box('Library cornice',(x,2.7,4.8),(6.4,4.5,.24),trim,.04)
    long_roof('Library roof',x,2.7,4.9,6.6,4.7,1.6,slate)
    for off in [-2.1,-.7,.7,2.1]:arch('Library tall window',x+off,.52,.8,.8,2.9,trim,True)

# Export one evaluated derivative mesh with material submeshes. Native source parts stay editable.
source_parts=list(root.children)
deps=bpy.context.evaluated_depsgraph_get()
copies=[]
for obj in source_parts:
    data=bpy.data.meshes.new_from_object(obj.evaluated_get(deps),depsgraph=deps)
    copy=bpy.data.objects.new('Export derivative '+obj.name,data);s.collection.objects.link(copy)
    copy.matrix_world=obj.matrix_world.copy();copies.append(copy)
bpy.ops.object.select_all(action='DESELECT')
for obj in copies:obj.select_set(True)
bpy.context.view_layer.objects.active=copies[0]
bpy.ops.object.join()
derivative=bpy.context.object;derivative.name='AcademicBackdrop'
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'AcademicBackdrop.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
stats={'name':'AcademicBackdrop','source_parts':len(source_parts),'export_vertices':len(derivative.data.vertices),'export_polygons':len(derivative.data.polygons),'material_slots':len(derivative.data.materials)}
bpy.data.objects.remove(derivative,do_unlink=True)
materials=[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value} for m in [stone,trim,brick,slate,glass,inset,gold]]
(EXPORT/'manifest.json').write_text(json.dumps({'assets':[stats],'materials':materials,'source':'Native editable geometry based on supplied campus architecture, no third-party mesh','validation':'Source preview pending, Unity placement pending'},indent=2))
cam=bpy.data.objects.new('Backdrop inspection camera',bpy.data.cameras.new('Backdrop inspection camera'));s.collection.objects.link(cam)
cam.location=(23,-42,20);cam.rotation_euler=(Vector((0,1,7))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=36;s.camera=cam
sun=bpy.data.objects.new('Backdrop sun',bpy.data.lights.new('Backdrop sun','SUN'));s.collection.objects.link(sun);sun.data.energy=2.2;sun.data.angle=.15;sun.rotation_euler=(math.radians(24),math.radians(-22),math.radians(-30))
s.render.resolution_x=1500;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.render.filepath=str(ROOT/'LocalOnly/Captures/Environment/AcademicBackdrop_v002.png')
Path(s.render.filepath).parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(DEST));bpy.ops.render.render(write_still=True)
print(json.dumps(stats))
