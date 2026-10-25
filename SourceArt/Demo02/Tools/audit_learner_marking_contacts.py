"""Reopen the marking source and measure actual skin/prop surfaces per sample."""
import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/Learner/AcademicLearner_Motion_v004.blend'))
s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['AcademicLearner_EditableRig'];body=bpy.data.objects['AcademicLearner_WeightedMesh'];pen=bpy.data.objects['AnnotationPen']
gids={g.index for g in body.vertex_groups if any(term in g.name for term in ['Hand','Thumb','Index','Middle','Ring','Little'])}
indices=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in gids)>.20]
checks=[]
for name,end in [('TableReady',19),('PenApproach',8),('MarkPoint',28)]:
 act=bpy.data.actions['D02_Learner_'+name+'_v001'];a.animation_data.action=act;a.animation_data.action_slot=act.slots[0]
 for frame in range(1,end+1):
  s.frame_set(frame);bpy.context.view_layer.update();evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();points=[evaluated.matrix_world@mesh.vertices[i].co for i in indices]
  table=[p.z-.8125 for p in points if abs(p.x)<.80 and -1.145<p.y<-.395]
  paper=[p.z-.830 for p in points if -.109<p.x<.109 and -.620<p.y<-.320]
  floor=min((evaluated.matrix_world@v.co).z for v in mesh.vertices);evaluated.to_mesh_clear();tip=pen.matrix_world@Vector((0,0,-.012))
  checks.append({'clip':name,'frame':frame,'hand_table_min_clearance_m':min(table) if table else None,'hand_paper_min_clearance_m':min(paper) if paper else None,'body_min_height_m':floor,'pen_tip_world':list(tip)})
failures=[c for c in checks if (c['hand_table_min_clearance_m'] is not None and c['hand_table_min_clearance_m']<-.001) or (c['hand_paper_min_clearance_m'] is not None and c['hand_paper_min_clearance_m']<-.001) or c['body_min_height_m']<-.002]
report={'source':'AcademicLearner_Motion_v004.blend','sample_count':len(checks),'checks':checks,'intersection_samples':failures,'limits':'Skin/plane audit only. Does not establish normal-speed performance, loop/interrupt behavior or Unity station alignment.'}
out=R/'LocalOnly/Captures/Assets/Learner/Marking_v002/contact-surface-audit.json';out.write_text(json.dumps(report,indent=2));print(json.dumps({'samples':len(checks),'intersection_samples':failures}))
