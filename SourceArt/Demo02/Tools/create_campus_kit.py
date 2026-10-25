"""Create a new editable Demo 02 asset library. Refuses to overwrite prior authoring."""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(r'D:\Altaius\Demo-02-Before-You-Publish\SourceArt\Demo02')
DEST = ROOT / 'Environment' / 'CampusKit_v001.blend'
EXPORT = ROOT / 'Exports' / 'CampusKit_v001'
if DEST.exists() or bpy.data.scenes.get('Demo02_AssetLibrary_v001'):
    raise RuntimeError('Asset library exists. Preserve manual edits; use an explicit new version or bounded edit.')
DEST.parent.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.new('Demo02_AssetLibrary_v001')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.render.resolution_x = 1600
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Demo02_StudioWorld')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.55,0.68,0.86,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .45

def material(name, color, rough=.5, metal=0):
    m=bpy.data.materials.new('D02_'+name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m

stone=material('WarmLimestone',(.68,.52,.34),.72)
stoneLight=material('LimestoneTrim',(.87,.72,.5),.6)
stoneVariants=[material('Paver'+str(i),(.63+i*.025,.49+i*.024,.34+i*.018),.78) for i in range(5)]
blue=material('CobaltEnamel',(.012,.12,.48),.3)
teal=material('TealCanvas',(.015,.36,.37),.65)
gold=material('Sunflower',(.95,.57,.035),.4)
metal=material('InkMetal',(.027,.055,.075),.3,.7)
brass=material('BrushedBrass',(.55,.33,.08),.26,.75)
wood=material('WarmOak',(.42,.19,.065),.43)
paper=material('CreamPaper',(.92,.88,.72),.78)
ink=material('Ink',(.014,.034,.06),.48)
soil=material('Soil',(.08,.045,.018),.94)
glass=material('BlueGreyGlass',(.22,.4,.49),.18,.1)
leafmats=[material('Foliage'+str(i),c,.65) for i,c in enumerate([(.17,.3,.025),(.29,.43,.045),(.4,.52,.075),(.11,.23,.025)])]
# Editable procedural source surface, export is explicitly recorded as requiring a later bake.
nodes=wood.node_tree.nodes;links=wood.node_tree.links
noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=3.5;noise.inputs['Detail'].default_value=2
tex=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeMapping');mapping.inputs['Scale'].default_value=(2,35,5)
ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.22,.075,.021,1);ramp.color_ramp.elements[1].color=(.53,.28,.1,1)
links.new(tex.outputs['Generated'],mapping.inputs['Vector']);links.new(mapping.outputs['Vector'],noise.inputs['Vector'])
links.new(noise.outputs['Fac'],ramp.inputs['Fac']);links.new(ramp.outputs['Color'],nodes['Principled BSDF'].inputs['Base Color'])

assets=[];active=None;coll=None
def begin(name, ids):
    global active,coll
    coll=bpy.data.collections.new('D02_'+name);scene.collection.children.link(coll)
    active=bpy.data.objects.new(name,None);coll.objects.link(active)
    active['production_ids']=','.join(ids);active['source_version']='v001';active['units']='metres'
    active['authoring_policy']='Editable source. Do not replace on regeneration.'
    assets.append(active);return active

def adopt(obj,name,mat=None):
    obj.name=name
    for c in list(obj.users_collection):c.objects.unlink(obj)
    coll.objects.link(obj);obj.parent=active
    if mat:obj.data.materials.append(mat)
    return obj

def cube(name,loc,size,mat,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=adopt(bpy.context.object,name,mat);o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        b=o.modifiers.new('Editable soft edges','BEVEL');b.width=bevel;b.segments=3
        n=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');n.keep_sharp=True
    return o

def cyl(name,loc,radius,depth,mat,vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc)
    o=adopt(bpy.context.object,name,mat)
    for p in o.data.polygons:p.use_smooth=True
    b=o.modifiers.new('Edge roll','BEVEL');b.width=min(radius*.2,.018);b.segments=3
    return o

def sphere(name,loc,scale,mat,segments=20,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc)
    o=adopt(bpy.context.object,name,mat);o.scale=scale
    for p in o.data.polygons:p.use_smooth=True
    return o

def rod(name,a,b,radius,mat):
    a,b=Vector(a),Vector(b);o=cyl(name,(a+b)/2,radius,(b-a).length,mat,16)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def mesh(name,verts,faces,mat):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);coll.objects.link(ob);ob.parent=active;me.materials.append(mat)
    return ob

def arch(name,outer,inner,spring,depth,mat):
    verts=[];faces=[];steps=32
    for y in [-depth/2,depth/2]:
        for r in [inner,outer]:
            for i in range(steps+1):
                a=math.pi*i/steps;verts.append((math.cos(a)*r,y,spring+math.sin(a)*r))
    n=steps+1
    for i in range(steps):
        faces.extend([(i,i+1,n+i+1,n+i),(2*n+i,3*n+i,3*n+i+1,2*n+i+1),
                      (i,2*n+i,2*n+i+1,i+1),(n+i,n+i+1,3*n+i+1,3*n+i)])
    faces.extend([(0,n,3*n,2*n),(n-1,2*n-1,4*n-1,3*n-1)])
    o=mesh(name,verts,faces,mat);b=o.modifiers.new('Stone edge','BEVEL');b.width=.012;b.segments=2
    return o

def legs(width,depth,height,mat=metal):
    for x in [-width/2+.08,width/2-.08]:
        for y in [-depth/2+.08,depth/2-.08]:cube('Leg',(x,y,height/2),(.065,.065,height),mat,.012)

def desk(name,width,depth,ids):
    begin(name,ids);cube('Oak worktop',(0,0,.78),(width,depth,.065),wood,.035);legs(width,depth,.75)
    for y in [-depth/2+.08,depth/2-.08]:cube('Apron',(0,y,.66),(width-.12,.06,.12),metal,.012)

random.seed(20261025)
begin('CourtyardPavers_2m',['D02.ASSET.ENV.COURTYARD'])
cube('Support',(0,0,-.075),(2,2,.15),stone,0)
for x in range(4):
    for y in range(4):cube('Individual stone',(x*.5-.75,y*.5-.75,-.012),(.493,.493,.06),stoneVariants[(x+2*y)%5],.012)
begin('StoneWall_2m',['D02.ASSET.ENV.STUDIO','D02.ASSET.ENV.ARCHIVE','D02.ASSET.ENV.REVIEW'])
cube('Wall core',(0,0,1.8),(2,.22,3.6),stone,.008)
for row in range(9):
    for col in range(4):cube('Limestone block',(col*.5-.75,-.126,row*.4+.2),(.491,.035,.388),stoneVariants[(row+col)%5],.008)
cube('Skirting',(0,-.035,.12),(2,.3,.24),stoneLight,.012)
cube('Cornice',(0,0,3.5),(2,.36,.18),stoneLight,.015)
begin('Portal_4m',['D02.ASSET.ENV.PORTALS'])
for x in [-1.82,1.82]:
    cube('Stone pier',(x,0,1.75),(.36,.45,3.5),stone,.02)
    cube('Cobalt jamb',(x-(-.23 if x<0 else .23),-.06,1.5),(.12,.25,3),blue,.014)
cube('Lintel',(0,0,3.3),(4,.45,.4),stoneLight,.02)
cube('Cobalt transom',(0,-.06,2.98),(3.4,.25,.15),blue,.012)
cube('Blank editable sign carrier',(0,-.25,3.28),(2.55,.065,.24),paper,.012)
for x in [-1.58,1.58]:
    rod('Brass pull',(x,-.22,.95),(x,-.22,1.45),.018,brass)
begin('ArchedWindow_2m',['D02.ASSET.ENV.WINDOWS'])
for x in [-.89,.89]:cube('Stone jamb',(x,0,1.15),(.2,.27,2.3),stoneLight,.015)
arch('Curved stone surround',.99,.79,2.3,.27,stoneLight)
cube('Sill',(0,-.03,.11),(2.05,.42,.22),stoneLight,.025)
cube('Glass panel',(0,.035,1.25),(1.55,.035,2.2),glass,.008)
# Curved glass infill follows the same arch instead of a square cap.
vs=[(0,.035,2.3)]+[(.78*math.cos(math.pi*i/32),.035,2.3+.78*math.sin(math.pi*i/32)) for i in range(33)]
mesh('Arched glass infill',vs,[(0,i,i+1) for i in range(1,33)],glass)
for x in [-.77,0,.77]:cube('Window mullion',(x,-.015,1.22),(.035,.07,2.3),metal,.006)
for z in [.95,1.65,2.3]:cube('Cross bar',(0,-.015,z),(1.58,.07,.035),metal,.006)
arch('Inner metal arch',.8,.765,2.3,.07,metal)
begin('Awning_4m',['D02.ASSET.ENV.PORTALS'])
o=cube('Canvas canopy',(0,-.5,2.95),(3.9,1.25,.08),teal,.018);o.rotation_euler.x=math.radians(12)
cube('Canvas valance',(0,-1.1,2.76),(3.9,.06,.23),teal,.012)
for x in [-1.82,1.82]:rod('Support',(x,-1.05,0),(x,-1.05,2.85),.038,metal)
desk('StudioDesk',1.7,.85,['D02.ASSET.ENV.SURFACES'])
desk('ExperimentTable',2.4,1.2,['D02.ASSET.PROP.EXPERIMENT_TABLE'])
desk('ReviewDesk',1.8,.9,['D02.ASSET.ENV.SURFACES'])
begin('AcademicChair',['D02.ASSET.ENV.SEATING'])
legs(.48,.48,.45,wood)
cube('Seat cushion',(0,0,.47),(.49,.5,.105),teal,.065)
for x in [-.2,.2]:cube('Back support',(x,.2,.67),(.05,.06,.64),wood,.012)
o=cube('Back cushion',(0,.2,.78),(.47,.1,.48),teal,.065);o.rotation_euler.x=math.radians(-6)
begin('ArchiveBookcase',['D02.ASSET.ENV.ARCHIVE'])
for x in [-.6,.6]:cube('Side',(x,0,1.05),(.07,.36,2.1),wood,.018)
cube('Back',(0,.16,1.05),(1.2,.035,2.1),wood,.012)
for z in [.06,.55,1.05,1.55,2.08]:cube('Shelf',(0,0,z),(1.26,.4,.05),wood,.015)
for row in range(4):
    for j in range(8):
        x=-.5+j*.13;h=.28+random.random()*.12
        cube('Reference volume',(x,-.01,.09+row*.5+h/2),(.07,.24,h),[blue,teal,paper,gold][(j+row)%4],.009)
        cube('Spine label',(x,-.136,.16+row*.5+h/2),(.045,.006,.05),paper,.002)
begin('PublicationCabinet',['D02.ASSET.PROP.CABINET'])
for x in [-.55,.55]:cube('Side',(x,0,.78),(.06,.48,1.5),blue,.018)
cube('Back',(0,.21,.78),(1.1,.06,1.5),blue,.012)
for z in [.06,.52,1,1.52]:cube('Shelf',(0,0,z),(1.16,.5,.045),metal,.01)
door=bpy.data.objects.new('DoorHinge',None);coll.objects.link(door);door.parent=active;door.location=(-.55,-.27,0)
o=cube('Separate cabinet door',(0,-.27,.78),(1.1,.055,1.46),blue,.025);o.parent=door;o.location=(.55,0,.78)
o=rod('Door handle',(.42,-.33,.68),(.42,-.33,.93),.014,brass);o.parent=door;o.location=(.97,-.06,.805)
o=cube('Publication label',(0,-.303,1.24),(.67,.01,.14),paper,.008);o.parent=door;o.location=(.55,-.033,1.24)
begin('Folio',['D02.ASSET.PROP.TASK_FOLIO','D02.ASSET.PROP.RUBRIC','D02.ASSET.PROP.PACKET'])
cube('Back cover',(.115,0,.004),(.23,.315,.006),blue,.004)
cube('Thin paper stack',(.115,0,.011),(.218,.3,.008),paper,.003)
hinge=bpy.data.objects.new('FolioCoverHinge',None);coll.objects.link(hinge);hinge.parent=active;hinge.location=(0,0,.016)
o=cube('Front cover',(.115,0,.018),(.23,.315,.004),blue,.004);o.parent=hinge;o.location=(.115,0,.002)
begin('PassageSheet',['D02.ASSET.PROP.PASSAGE_PROXY','D02.ASSET.PROP.WORK_LIST','D02.ASSET.PROP.WORK_COMPARE','D02.ASSET.PROP.WORK_PITCH'])
cube('Thin cream sheet',(0,0,.0007),(.21,.297,.0014),paper,.001)
begin('AnnotationPen',['D02.ASSET.PROP.PEN'])
cyl('Pen barrel',(0,0,.065),.005,.11,teal,24)
cyl('Metal grip',(0,0,.014),.0048,.025,brass,24)
bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=.0005,radius2=.0045,depth=.014,location=(0,0,-.005));adopt(bpy.context.object,'Nib',metal)
rod('Pocket clip',(.005,0,.095),(.005,0,.13),.001,brass)
begin('CriterionRail',['D02.ASSET.PROP.CRITERION_LANE','D02.ASSET.PROP.MARK_CONTROL'])
cube('Wood rail',(0,0,.012),(.46,.07,.024),wood,.008)
for n in range(11):cube('Scale tick',(-.2+n*.04,0,.025),(.002,.032 if n%5 else .05,.001),ink,0)
cube('Movable mark token',(-.2,0,.04),(.025,.055,.024),gold,.008)
begin('SourceStand',['D02.ASSET.PROP.SOURCE_A','D02.ASSET.PROP.SOURCE_B'])
cube('Foot',(0,0,.04),(.5,.4,.08),wood,.025)
cube('Support',(0,.1,.5),(.055,.08,.9),metal,.012)
o=cube('Paper backplate',(0,0,1.08),(.48,.035,.56),teal,.02);o.rotation_euler.x=math.radians(12)
begin('ReviewTray',['D02.ASSET.PROP.PACKET'])
cube('Base',(0,0,.014),(.3,.37,.028),wood,.008)
for x in [-.15,.15]:cube('Side',(x,0,.04),(.014,.37,.06),wood,.005)
cube('Back',(0,.18,.04),(.3,.014,.06),wood,.005)
begin('CourtyardBench',['D02.ASSET.ENV.SEATING'])
for x in [-.7,.7]:
    for y in [-.22,.22]:cube('Metal foot',(x,y,.25),(.055,.055,.5),metal,.012)
    rod('Back brace',(x,.2,.35),(x,.29,1.02),.027,metal)
for y in [-.2,-.06,.08,.22]:cube('Seat slat',(0,y,.49),(1.75,.125,.045),wood,.013)
for z in [.7,.85,1]:
    o=cube('Back slat',(0,.26,z),(1.75,.055,.12),wood,.015);o.rotation_euler.x=math.radians(-8)
begin('Planter',['D02.ASSET.ENV.COURTYARD'])
cube('Stone planter',(0,0,.28),(1.1,.6,.56),stoneLight,.045)
cube('Soil inset',(0,0,.565),(.96,.46,.035),soil,.035)

def leaf(name,center,length,width,angle,tilt,mat):
    # Curved double-sided blade, editable mesh and smooth normals.
    verts=[(0,0,0),(-width*.4,length*.3,.025),(-width*.5,length*.62,.04),(0,length,.015),
           (width*.5,length*.62,.04),(width*.4,length*.3,.025),(0,length*.45,.07)]
    o=mesh(name,verts,[(0,1,6),(1,2,6),(2,3,6),(3,4,6),(4,5,6),(5,0,6)],mat)
    o.location=center;o.rotation_euler=(tilt,0,angle)
    for p in o.data.polygons:p.use_smooth=True
    s=o.modifiers.new('Leaf thickness','SOLIDIFY');s.thickness=.001
    return o

for i in range(70):
    x=random.uniform(-.48,.48);y=random.uniform(-.22,.22);z=random.uniform(.58,.83)
    leaf('Leaf',(x,y,z),random.uniform(.12,.25),.1,random.random()*math.tau,random.uniform(-.6,.6),leafmats[i%4])
for k in range(4):
    x=-.36+k*.24;y=.08*math.sin(k);z=.95+random.uniform(0,.1)
    rod('Flower stem',(x,y,.58),(x,y,z),.006,leafmats[0])
    sphere('Flower centre',(x,y,z),(.04,.025,.04),wood,12,8)
    for i in range(10):
        a=i*math.tau/10
        o=sphere('Sunflower petal',(x+.055*math.cos(a),y-.004,z+.055*math.sin(a)),(.024,.012,.055),gold,12,6)
        o.rotation_euler.y=-a+math.pi/2
begin('CourtyardTree',['D02.ASSET.ENV.COURTYARD'])
rod('Trunk',(0,0,0),(.07,0,2.25),.1,wood)
for k in range(7):
    a=k*2.4;end=(math.cos(a)*.65,math.sin(a)*.65,2.4+random.random()*.7)
    rod('Branch',(.04,0,1.55+k*.1),end,.028,wood)
for i in range(170):
    a=random.random()*math.tau;r=random.random()**.5*.95;z=2.3+random.random()*1.05
    leaf('Canopy leaf',(math.cos(a)*r,math.sin(a)*r,z),random.uniform(.2,.38),.17,random.random()*math.tau,random.uniform(-1.2,1.2),leafmats[i%4])
begin('CampusLantern',['D02.ASSET.ENV.COURTYARD'])
cyl('Foot',(0,0,.09),.17,.18,metal)
cyl('Post',(0,0,1.35),.045,2.6,metal)
cyl('Lantern base',(0,0,2.65),.16,.07,metal)
cube('Warm glass',(0,0,2.88),(.21,.21,.4),paper,.02)
for x in [-.13,.13]:
    for y in [-.13,.13]:rod('Lantern frame',(x,y,2.66),(x,y,3.08),.014,metal)
bpy.ops.mesh.primitive_cone_add(vertices=4,radius1=.25,radius2=.03,depth=.2,location=(0,0,3.16),rotation=(0,0,math.pi/4));adopt(bpy.context.object,'Lantern roof',metal)
sphere('Finial',(0,0,3.29),(.035,.035,.055),metal,16,8)

# Export only explicit owned asset roots and descendants, each with origin at ground/contact.
manifest=[]
for idx,root in enumerate(assets):
    objects=[root]+list(root.children_recursive)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/(root.name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
        use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
    bounds=[o.matrix_world@Vector(c) for o in objects if o.type=='MESH' for c in o.bound_box]
    dims=[max(v[i] for v in bounds)-min(v[i] for v in bounds) for i in range(3)]
    manifest.append({'name':root.name,'ids':root['production_ids'].split(','),'mesh_objects':sum(o.type=='MESH' for o in objects),
        'source_bounds_m':dims,'fbx':str(EXPORT/(root.name+'.fbx')),'status':'authored source and export candidate, Unity validation pending'})
    root.location=((idx%5)*5,(idx//5)*5,0)

# Showroom cameras/lights are saved and editable; original live Scene is retained untouched.
presentation=bpy.data.collections.new('Demo02_LibraryPresentation');scene.collection.children.link(presentation)
def link(obj):presentation.objects.link(obj);return obj
cam=link(bpy.data.objects.new('Library camera',bpy.data.cameras.new('Library camera')))
cam.location=(29,-25,30);target=Vector((10,9,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=36;scene.camera=cam
for name,loc,power,size in [('Key',(2,-7,18),2800,12),('Fill',(20,6,14),1600,10)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=link(bpy.data.objects.new(name,d));o.location=loc
    o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
scene['authoring_status']='Editable modular source candidates. Requires material bake, Unity import and visual/contact validation.'
scene.render.filepath=str(ROOT/'Environment'/'CampusKit_v001_overview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
(EXPORT/'manifest.json').write_text(json.dumps({'version':'v001','units':'metres','axes':'Blender Z up, FBX -Z forward Y up',
    'assets':manifest,'known_limits':['Procedural oak requires export texture bake before final Unity art acceptance.','Unity import, scene assembly and runtime verification pending.','Tree foliage density and final lighting require gameplay-camera review.']},indent=2),encoding='utf-8')
print(json.dumps({'saved':str(DEST),'assets':len(assets),'mesh_objects':sum(x['mesh_objects'] for x in manifest),'export_directory':str(EXPORT)}))
