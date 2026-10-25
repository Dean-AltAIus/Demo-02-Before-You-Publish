"""Local topology and weight-continuity correction, no regenerated asset or clips."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Quaternion
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
dest=R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Rig_v004.blend'
if dest.exists():raise RuntimeError('Preserve existing source version')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Rig_v003.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh']
bm=bmesh.new();bm.from_mesh(body.data);before=len(bm.verts)
hand=lambda v:abs((body.matrix_world@v.co).x)>.63
overlaps={min(e.link_faces,key=lambda f:f.calc_area()) for e in bm.edges if len(e.link_faces)>2 and all(hand(v) for v in e.verts)}
if overlaps:bmesh.ops.delete(bm,geom=list(overlaps),context='FACES_ONLY')
boundary=[e for e in bm.edges if e.is_boundary and all(abs((body.matrix_world@v.co).x)>.64 for v in e.verts)]
filled=bmesh.ops.holes_fill(bm,edges=boundary,sides=8)['faces'] if boundary else []
bmesh.ops.subdivide_edges(bm,edges=[e for e in bm.edges if all(hand(v) for v in e.verts)],cuts=2,use_grid_fill=True)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(body.data);bm.free()
for f in body.data.polygons:f.use_smooth=True
neighbors={v.index:set() for v in body.data.vertices}
for e in body.data.edges:
 i,j=e.vertices;neighbors[i].add(j);neighbors[j].add(i)
hand_vertices={v.index for v in body.data.vertices if abs((body.matrix_world@v.co).x)>.64}
group_ids={g.index for g in body.vertex_groups if g.name in a.data.bones}
weights={v.index:{g.group:g.weight for g in v.groups if g.group in group_ids} for v in body.data.vertices}
for iteration in range(4):
 updated={}
 for i in hand_vertices:
  average={};adj=neighbors[i]
  if not adj:continue
  for j in adj:
   for g,w in weights[j].items():average[g]=average.get(g,0)+w/len(adj)
  updated[i]={g:.55*weights[i].get(g,0)+.45*average.get(g,0) for g in set(weights[i])|set(average)}
 weights.update(updated)
for i in hand_vertices:
 for g in group_ids:
  w=weights[i].get(g,0)
  if w>1e-7:body.vertex_groups[g].add([i],w,'REPLACE')
  else:body.vertex_groups[g].remove([i])
ev=R/'LocalOnly/Captures/Assets/Learner/Fingers_v004';ev.mkdir(parents=True,exist_ok=True)
report={'vertices_before':before,'vertices_after':len(body.data.vertices),'overlap_faces_removed':len(overlaps),'boundary_faces_filled':len(filled),'weight_relaxation_passes':4,'scope':'hands only, body weights preserved','animation_clips':0}
(ev/'repair-audit.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
chains={n:[Vector(p) for p in pts] for n,pts in json.loads((R/'LocalOnly/Captures/Assets/Learner/Fingers_v003/finger-rig-audit.json').read_text())['joint_world_positions'].items()}
for name,ch in chains.items():
 for i,angle in enumerate([25,45,30]):
  p=a.pose.bones[name+str(i+1)];d=(ch[i+1]-ch[i]).normalized();axis=Vector((-d.y,d.x,0)).normalized();q=p.bone.matrix_local.to_quaternion()
  p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@Quaternion(axis,math.radians(angle))@q
bpy.context.view_layer.update();cam=s.camera;cam.data.ortho_scale=.30
for side,sign in [('Left',1),('Right',-1)]:
 target=Vector((.71*sign,.005,1.28))
 for view,offset in [('oblique',(.25*sign,-.2,.35)),('top',(0,0,.5)),('palm',(0,0,-.5))]:
  cam.location=target+Vector(offset);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
  s.render.filepath=str(ev/(side+'_soft_grip_'+view+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps(report))
