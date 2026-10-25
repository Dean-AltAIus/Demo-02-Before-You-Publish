"""Saved source assembly and engine placement manifest. Never constructs runtime content."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
src=ROOT/'SourceArt/Demo02/Environment/CampusKit_v001.blend'
dst=ROOT/'SourceArt/Demo02/Environment/CampusAssembly_v001.blend'
if dst.exists():raise RuntimeError('Preserve saved authoring. Version or edit explicitly.')
bpy.ops.wm.open_mainfile(filepath=str(src))
library=bpy.data.scenes['Demo02_AssetLibrary_v001']
roots={o.name:o for o in library.objects if o.type=='EMPTY' and 'production_ids' in o}
scene=bpy.data.scenes.new('Demo02_CampusAssembly_v001');bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
placements=[]
def place(asset,name,pos,deg=0):
    original=roots[asset];mapping={}
    for ob in [original]+list(original.children_recursive):
        c=ob.copy();scene.collection.objects.link(c);mapping[ob]=c
    for ob,c in mapping.items():
        if ob is original:c.parent=None;c.location=pos;c.rotation_euler=(0,0,math.radians(deg));c.name=name
        else:c.parent=mapping[ob.parent]
    placements.append({'asset':asset,'name':name,'blender_position':list(pos),'rotation_z_degrees':deg})
    return mapping[original]

for i in range(12):
    for j in range(9):place('CourtyardPavers_2m',f'Paving_{i:02}_{j:02}',(-11+i*2,-8+j*2,-.018))

def room(name,center,yaw,kind):
    transform=Matrix.Translation(Vector(center))@Matrix.Rotation(math.radians(yaw),4,'Z')
    def p(asset,suffix,local,rot=0):
        return place(asset,name+'_'+suffix,transform@Vector(local),yaw+rot)
    p('Portal_4m','Entrance',(0,-2,0))
    for x in [-2,0,2]:p('StoneWall_2m',f'Back{x}',(x,2,0))
    for x in [-3,3]:
        for y in [-1,1]:p('StoneWall_2m',f'Side{x}_{y}',(x,y,0),90)
    p('ArchedWindow_2m','WindowLeft',(-2.5,-2.03,0))
    # One tall opening each side remains visually distinct; no closing collision at the portal.
    p('Planter','PlanterLeft',(-2.8,-2.55,0))
    p('Planter','PlanterRight',(2.8,-2.55,0))
    if kind=='studio':
        p('ExperimentTable','Experiment',(0,0,0));p('AcademicChair','LearnerChair',(0,-.95,0))
        p('StudioDesk','RevisionDesk',(1.6,1.35,0));p('ArchiveBookcase','Shelves',(-1.8,1.7,0))
        p('Folio','Task',(-.6,0,.815));p('Folio','Rubric',(.15,0,.815))
        p('PassageSheet','WorkSample',(-.22,-.08,.816));p('CriterionRail','Lane',(.65,-.32,.815))
        p('AnnotationPen','Pen',(.8,-.13,.82),90)
    else:
        p('ReviewDesk','Desk',(0,0,0));p('AcademicChair','LeadChair',(0,.75,0),180)
        p('PublicationCabinet','Cabinet',(1.8,1.55,0));p('ReviewTray','Tray',(-.45,0,.815))
        p('Folio','ReviewPacket',(0,0,.815));p('ArchiveBookcase','Shelves',(-1.9,1.7,0))
    return transform

room('Studio',(-8,1.2,0),90,'studio')
room('Review',(8,1.2,0),-90,'review')
for x in [-2,0,2]:place('StoneWall_2m','Archive_Back_'+str(x),(x,8,0))
place('Awning_4m','Archive_Awning',(0,6.7,0))
place('StudioDesk','Archive_Surface',(0,6.2,0))
place('SourceStand','Archive_SourceA',(-.85,5.7,0))
place('SourceStand','Archive_SourceB',(.85,5.7,0))
place('ArchiveBookcase','Archive_Shelf',(-2.1,7.7,0))
for x in [-2.6,2.6]:place('Planter','Archive_Planter_'+str(x),(x,6.8,0))
for x,y in [(-4.8,-6.7),(4.8,-6.7),(-4.8,4.1),(4.8,4.1),(-10.7,7.2),(10.7,7.2)]:
    place('Planter',f'Courtyard_Planter_{x}_{y}',(x,y,0))
    place('CourtyardTree',f'Courtyard_Tree_{x}_{y}',(x,y,.5))
for x,y,rot in [(-7,-6,0),(7,-6,0),(-3,3.8,180),(3,3.8,180)]:place('CourtyardBench',f'Bench_{x}_{y}',(x,y,0),rot)
for x,y in [(-4.5,-4.4),(4.5,-4.4),(-4.5,6.3),(4.5,6.3)]:place('CampusLantern',f'Lantern_{x}_{y}',(x,y,0))

scene.world=bpy.data.worlds.new('Demo02_CampusSky');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.47,.64,.87,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6
sun=bpy.data.lights.new('Campus sun','SUN');sun.energy=2.5;sun.angle=.12
o=bpy.data.objects.new('Campus sun',sun);scene.collection.objects.link(o);o.rotation_euler=(math.radians(28),math.radians(-32),math.radians(-25))
area=bpy.data.lights.new('Courtyard fill','AREA');area.energy=1600;area.size=18
o=bpy.data.objects.new('Courtyard fill',area);scene.collection.objects.link(o);o.location=(0,0,13)
cam=bpy.data.objects.new('Campus source camera',bpy.data.cameras.new('Campus source camera'));scene.collection.objects.link(cam)
cam.location=(15,-20,13);target=Vector((0,1,1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.lens=37;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.filepath=str(ROOT/'LocalOnly/Captures/Assets/Campus/source_assembly_v001.png')
Path(scene.render.filepath).parent.mkdir(parents=True,exist_ok=True)
scene['status']='Editable reference-led source assembly. Unity gameplay and art acceptance pending.'
bpy.ops.wm.save_as_mainfile(filepath=str(dst))
(ROOT/'SourceArt/Demo02/Environment/campus-placements-v001.json').write_text(json.dumps({'footprint_m':[24,18],'blender_axes':'Z up, front -Y','placements':placements,'interaction_targets_blender':{'studio':[-5.9,1.2,0],'archive':[0,4.8,0],'review':[5.9,1.2,0]},'status':'Source assembly only, navigation and camera clearance require actual Unity checks.'},indent=2))
bpy.ops.render.render(write_still=True)
print(json.dumps({'saved':str(dst),'instances':len(placements),'render':scene.render.filepath}))
