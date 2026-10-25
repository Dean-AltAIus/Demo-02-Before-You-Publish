"""Bounded native shoulder surface and skin-weight correction, preserve originals."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
for role in ['Colleague','ModuleLead']:
 dest=R/f'SourceArt/Demo02/Characters/{role}/{role}_Motion_v002.blend'
 if dest.exists():raise RuntimeError('Preserve existing correction source')
 bpy.ops.wm.open_mainfile(filepath=str(R/f'SourceArt/Demo02/Characters/{role}/{role}_Motion_v001.blend'))
 s=bpy.data.scenes['Demo02_'+role+'Rig_v001'];bpy.context.window.scene=s
 a=bpy.data.objects[role+'_EditableRig'];body=bpy.data.objects[role+'_WeightedMesh'];s.frame_set(1)
 centers=[a.matrix_world@a.data.bones[side+'Arm'].head_local for side in ['Left','Right']]
 influence={}
 for v in body.data.vertices:
  w=body.matrix_world@v.co
  distance=min(Vector(((w.x-c.x)/.17,(w.y-c.y)/.18,(w.z-c.z)/.14)).length for c in centers)
  if distance<1.5:influence[v.index]=max(0,min(1,(1.5-distance)/.65))
 group=body.vertex_groups.new(name='Shoulder local surface repair')
 for i,w in influence.items():group.add([i],w,'REPLACE')
 modifier=body.modifiers.new('Editable shoulder seam relaxation','SMOOTH');modifier.factor=.6;modifier.iterations=9;modifier.vertex_group=group.name
 bpy.context.view_layer.objects.active=body
 while list(body.modifiers).index(modifier)>0:bpy.ops.object.modifier_move_up(modifier=modifier.name)
 neighbors={v.index:set() for v in body.data.vertices}
 for edge in body.data.edges:
  i,j=edge.vertices;neighbors[i].add(j);neighbors[j].add(i)
 ids={g.index for g in body.vertex_groups if g.name in a.data.bones}
 weights={v.index:{g.group:g.weight for g in v.groups if g.group in ids} for v in body.data.vertices}
 for iteration in range(8):
  updated={}
  for i,strength in influence.items():
   adj=neighbors[i]
   if not adj:continue
   average={}
   for j in adj:
    for g,w in weights[j].items():average[g]=average.get(g,0)+w/len(adj)
   alpha=.65*strength
   updated[i]={g:(1-alpha)*weights[i].get(g,0)+alpha*average.get(g,0) for g in set(weights[i])|set(average)}
  weights.update(updated)
 for i in influence:
  for g in ids:
   w=weights[i].get(g,0)
   if w>1e-7:body.vertex_groups[g].add([i],w,'REPLACE')
   else:body.vertex_groups[g].remove([i])
 s.frame_set(1);bpy.context.view_layer.update()
 s['shoulder_repair']='Bounded shoulder surface smoothing before armature, eight local weight-relaxation passes. Original source retained.'
 bpy.ops.wm.save_as_mainfile(filepath=str(dest))
 ev=R/f'LocalOnly/Captures/Assets/{role}/Shoulders_v002';ev.mkdir(parents=True,exist_ok=True)
 s.render.resolution_x=640;s.render.resolution_y=800;s.cycles.samples=12;s.render.threads_mode='FIXED';s.render.threads=4
 cam=s.camera;cam.data.ortho_scale=2.05
 for view,pos in [('front',(0,-4,.95)),('threequarter',(2.8,-3,.95)),('rear_threequarter',(-2.8,3,.95))]:
  cam.location=pos;cam.rotation_euler=(Vector((0,0,.9))-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(ev/(view+'.png'));bpy.ops.render.render(write_still=True)
 (ev/'repair-audit.json').write_text(json.dumps({'bounded_vertices':len(influence),'weight_passes':8,'surface_modifier':'Smooth factor .6, 9 iterations, local weighted region, before Armature','actions_preserved':True,'acceptance':'Visual inspection and Unity fidelity pending'},indent=2))
