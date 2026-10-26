"""Preserve native evaluated rest positions and all deform influences for Unity.
Run in isolated background Blender. Versioned outputs are never overwritten.
No authored source, pose, mesh, rig, clip or texture is saved or changed.
"""
import bpy, hashlib, json, struct, argparse, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output-dir',type=Path,default=ROOT/'SourceArt/Demo02/Exports/ExactSkinWeights_v001')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
DEST=args.output_dir.resolve()
if DEST.exists():raise RuntimeError('Preserve existing exact-weight batch: '+str(DEST))
SOURCES = [
    ('Learner', 'Learner/AcademicLearner_ContactLibrary_v013.blend', 'Demo02_LearnerRig_v001', 'AcademicLearner_EditableRig', 'AcademicLearner_WeightedMesh'),
    ('ModuleLead', 'ModuleLead/ModuleLead_Interactions_v019.blend', 'Demo02_ModuleLeadRig_v001', 'ModuleLead_EditableRig', 'ModuleLead_WeightedMesh'),
    ('Colleague', 'Colleague/Colleague_Interactions_v001.blend', 'Demo02_ColleagueRig_v001', 'Colleague_EditableRig', 'Colleague_WeightedMesh'),
]
def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
for role, relative, scene, rig_name, mesh_name in SOURCES:
    output = DEST / role
    if output.exists(): raise RuntimeError('Preserve existing weight package: ' + str(output))
    source = ROOT / 'SourceArt/Demo02/Characters' / relative
    before = digest(source)
    bpy.ops.wm.open_mainfile(filepath=str(source))
    bpy.context.window.scene = bpy.data.scenes[scene]
    rig = bpy.data.objects[rig_name]; body = bpy.data.objects[mesh_name]
    rig.data.pose_position = 'REST'; bpy.context.view_layer.update()
    evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    if len(evaluated.data.vertices) != len(body.data.vertices): raise RuntimeError('Rest topology changed')
    rest = [evaluated.matrix_world @ v.co for v in evaluated.data.vertices]
    face_vertices = {i for face in evaluated.data.polygons for i in face.vertices}
    wire_only = sorted(set(range(len(rest))) - face_vertices)
    names = sorted(b.name for b in rig.data.bones if b.use_deform)
    indices = {name:i for i,name in enumerate(names)}
    weights = bytearray(); maximum = 0
    for vertex in body.data.vertices:
        pairs = [(indices[body.vertex_groups[g.group].name],g.weight) for g in vertex.groups if body.vertex_groups[g.group].name in indices and g.weight > 0]
        total = sum(weight for _,weight in pairs)
        if not pairs or total <= 0: raise RuntimeError('Unweighted native vertex')
        pairs = sorted(((index,weight/total) for index,weight in pairs),key=lambda p:-p[1])
        maximum = max(maximum,len(pairs)); weights.extend(struct.pack('<B',len(pairs)))
        for index,weight in pairs: weights.extend(struct.pack('<Hf',index,weight))
    if digest(source) != before: raise RuntimeError('Source changed')
    output.mkdir(parents=True)
    rest_path = output/'rest.f32'; rest_path.write_bytes(struct.pack('<'+'f'*(len(rest)*3),*[value for vertex in rest for value in vertex]))
    weight_path = output/'weights.bin'; weight_path.write_bytes(weights)
    (output/'receipt.json').write_text(json.dumps({'source':str(source.relative_to(ROOT)),'source_sha256':before,'bone_names':names,'vertices':len(rest),'maximum_influences':maximum,'rest':'rest.f32','rest_sha256':digest(rest_path),'weights':'weights.bin','weights_sha256':digest(weight_path),'rest_format':'Headerless little endian float32 XYZ, native world metres, evaluated armature REST, all geometry modifiers retained.','weight_format':'Per native vertex: UInt8 count, then UInt16 bone index and Float32 normalized weight, little endian, descending weights. No positive influence cutoff.','authority':'Source-to-engine skin fidelity only. No domain, contact or runtime acceptance is supplied by this package.'},indent=2),encoding='utf-8')
    if wire_only:
        receipt_path = output/'receipt.json'
        receipt = json.loads(receipt_path.read_text(encoding='utf-8'))
        receipt['non_surface_vertices'] = wire_only
        receipt_path.write_text(json.dumps(receipt,indent=2),encoding='utf-8')
    print(role + ': ' + str(len(rest)) + ' vertices, max ' + str(maximum) + ' influences',flush=True)
