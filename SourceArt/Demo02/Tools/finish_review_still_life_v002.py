"""Complete variants from the visually inspected native source, preserving its first renders."""
import bpy,math,json,shutil
from pathlib import Path
R=Path(__file__).resolve().parents[3];prior=R/'SourceArt/Demo02/UI/ReviewStillLife_v001';out=R/'SourceArt/Demo02/UI/ReviewStillLife_v002';out.mkdir(parents=True,exist_ok=True)
dest=out/'ReviewStillLife_v002.blend'
if dest.exists():raise RuntimeError('Preserve existing source')
bpy.ops.wm.open_mainfile(filepath=str(prior/'ReviewStillLife_v001.blend'));s=bpy.context.scene
def curve(name,points,radius,material,parent):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=radius;cu.bevel_resolution=4;sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
 for p,v in zip(sp.points,points):p.co=(*v,1)
 o=bpy.data.objects.new(name,cu);s.collection.objects.link(o);o.data.materials.append(material);o.parent=parent;return o
def marker(name):
 o=bpy.data.objects.new(name,None);s.collection.objects.link(o);o.location=(-.26,-.25,.51);return o
gold=bpy.data.materials['Brushed warm brass'];navy=bpy.data.materials['Deep blue enamel']
unknown=marker('Unknown review marker');curve('Unknown curved question',[(-.07,0,.055),(-.05,0,.105),(.03,0,.11),(.07,0,.07),(.06,0,.025),(0,0,-.025),(0,0,-.065)],.018,gold,unknown)
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.018,location=(0,0,-.115));dot=bpy.context.object;dot.name='Question dot';dot.data.materials.append(gold);dot.parent=unknown
unavailable=marker('Unavailable review marker');curve('Unavailable rim',[(.13*math.cos(i*math.tau/48),0,.13*math.sin(i*math.tau/48)) for i in range(49)],.018,navy,unavailable);curve('Unavailable bar',[(-.09,-.005,-.09),(.09,-.005,.09)],.018,gold,unavailable)
names=['Closed authority lock','Released open shackle','Actual requested review clock','Owned hold marker','Different current and historical papers','Returned document arrow','Actual accepted receipt seal','Unknown review marker','Unavailable review marker']
roots=[bpy.data.objects[n] for n in names]
variants={'folio':[],'locked':[0],'pending':[0,2],'returned':[0,5],'stale':[0,4],'approved':[0,6],'released':[1,6],'held':[3],'unverified-hold':[3,2],'unknown':[0,7],'unavailable':[0,8]}
def show(indices):
 for i,root in enumerate(roots):
  for o in [root]+list(root.children_recursive):o.hide_render=i not in indices
show(variants['locked']);s['variant_roots']=json.dumps({k:[names[i] for i in v] for k,v in variants.items()});bpy.ops.wm.save_as_mainfile(filepath=str(dest))
for name,indices in variants.items():
 if name in ['locked','stale']:shutil.copy2(prior/(name+'.png'),out/(name+'.png'));continue
 show(indices);s.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
(out/'recipe.json').write_text(json.dumps({'source':str(dest.relative_to(R)),'states':{k:[names[i] for i in v] for k,v in variants.items()},'text_baked':False,'rights':'Owned native procedural geometry and materials','locked_stale_pixels':'Preserved from inspected v001, identical geometry and camera','status':'Native render variants; Unity state and visual admission required'},indent=2))
