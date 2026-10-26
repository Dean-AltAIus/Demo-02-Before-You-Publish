"""Record owned native object matrices and exact contact tracks for editable Unity configuration."""
import bpy,json,hashlib,argparse,sys
from pathlib import Path
R=Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output-dir',type=Path,default=R/'SourceArt/Demo02/Exports/ContactPropTracks_v005')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=args.output_dir.resolve()
if out.exists():raise RuntimeError('Preserve existing prop export: '+str(out))
families=[('FolioOpen','FolioOpenSkin_v004','FolioCoverHinge'),('FolioClose','FolioCloseSkin_v004','FolioCoverHinge'),('Passage','SheetTransferSkin_v004','Selected passage sheet | editable candidate'),('PageTurn','PageTurnSkin_v002','Selected passage sheet | editable candidate'),('EditionSlide','EditionSlideSkin_v002','Selected passage sheet | editable candidate'),('PenPoint','PenPointSkin_v005','AnnotationPen')]
all=[]
for key,suffix,name in families:
 path=R/'SourceArt/Demo02/Characters/Learner'/('AcademicLearner_'+suffix+'.blend');bpy.ops.wm.open_mainfile(filepath=str(path));s=bpy.data.scenes['Demo02_LearnerRig_v001'];bpy.context.window.scene=s;o=bpy.data.objects.get(name)
 if o is None:raise RuntimeError('Missing prop '+name)
 records=[]
 for f in range(s.frame_start,s.frame_end+1):
  s.frame_set(f);bpy.context.view_layer.update();p,q,scale=o.matrix_world.decompose();records.append({'time':(f-s.frame_start)/30,'position':list(p),'rotation':[q.x,q.y,q.z,q.w],'scale':list(scale)})
 s.frame_set(s.frame_start);bpy.context.view_layer.update();rig=bpy.data.objects['AcademicLearner_EditableRig']
 all.append({'family':key,'source':str(path.relative_to(R)),'source_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'prop':name,'parent':o.parent.name if o.parent else '', 'local_position':list(o.location),'local_rotation':list(o.rotation_euler),'samples':records,'bones':{n:list(rig.matrix_world@rig.pose.bones[n].head) for n in ['RightHand','LeftHand']},'children':[{'name':c.name,'local_position':list(c.location),'mesh_bounds':[list(x) for x in c.bound_box]} for c in o.children if c.type=='MESH']})
out.mkdir(parents=True)
(out/'native-prop-tracks.json').write_text(json.dumps({'axis':'Native Blender Z up, learner faces -Y. Reconcile with imported Generic avatar and original prop geometry before admission.','tracks':all},indent=2));print('Exported6ownedproptracks',flush=True)




