"""Export full native learner surfaces and weights, without saving the source.

Run in isolated Blender with --factory-startup --python-exit-code 1, then this
script and -- --output-dir LocalOnly/Validation/NewLearnerSkinFixture.
The surface index preserves the existing Unity audit schema. Outputs are guarded.
"""
import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path('SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v013.blend')
POSES = [('Idle', 1), ('FolioOpen', 8), ('FolioClose', 9), ('Passage', 10),
         ('PenPoint', 17), ('PenPointApproach', 19), ('EditionSlide', 10)]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=SOURCE)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    source, out = (ROOT / args.source).resolve(), (ROOT / args.output_dir).resolve()
    if out.exists():
        raise RuntimeError('Preserve existing output directory: ' + str(out))
    before = digest(source)
    bpy.ops.wm.open_mainfile(filepath=str(source))
    scene = bpy.data.scenes['Demo02_LearnerRig_v001']
    bpy.context.window.scene = scene
    rig = bpy.data.objects['AcademicLearner_EditableRig']
    body = bpy.data.objects['AcademicLearner_WeightedMesh']
    if scene.render.fps != 30 or scene.render.fps_base != 1:
        raise RuntimeError('Changed source timing requires renewed fixture admission')
    mods = [m for m in body.modifiers if m.type == 'ARMATURE' and m.show_viewport]
    if len(mods) != 1:
        raise RuntimeError('Expected one active armature modifier')
    modifier = mods[0]
    if (modifier.object != rig or not modifier.use_vertex_groups or modifier.use_bone_envelopes
            or modifier.vertex_group or modifier.use_deform_preserve_volume):
        raise RuntimeError('Changed skinning mode requires renewed fixture admission')
    for take, frame in POSES:
        action = bpy.data.actions[f'D02_Learner_{take}_v013']
        if not action.frame_range[0] <= frame <= action.frame_range[1]:
            raise RuntimeError('Sample outside action range: ' + take)
    out.mkdir(parents=True)
    index = {'source': str(source), 'source_sha256': before,
             'coordinate': 'Blender native world metres, XYZ, little-endian float32',
             'vertex_order': 'Native evaluated order, unchanged by saved geometry modifiers',
             'fps': 30, 'source_saved': False, 'poses': []}

    def capture(name):
        bpy.context.view_layer.update()
        evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        try:
            if len(mesh.vertices) != len(body.data.vertices):
                raise RuntimeError('Evaluated vertex count changed; weight mapping is invalid')
            points = [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
            data = b''.join(struct.pack('<fff', *p) for p in points)
            path = out / (name + '.bin')
            path.write_bytes(data)
            faces = {i for face in mesh.polygons for i in face.vertices}
            record = {'path': str(path), 'file': path.name, 'vertices': len(points),
                      'sha256': hashlib.sha256(data).hexdigest(), 'byte_count': len(data)}
            return record, sorted(set(range(len(points))) - faces)
        finally:
            evaluated.to_mesh_clear()

    rig.data.pose_position = 'REST'
    scene.frame_set(1)
    index['rest'], wire = capture('rest')
    index['non_surface_vertices'] = wire
    rig.data.pose_position = 'POSE'
    for take, frame in POSES:
        action = bpy.data.actions[f'D02_Learner_{take}_v013']
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        scene.frame_set(frame)
        record, current_wire = capture(f'{take}-{frame}')
        if current_wire != wire:
            raise RuntimeError('Face topology changed across poses')
        record.update(take=take, frame=frame, seconds=(frame - 1) / 30)
        index['poses'].append(record)
    names = sorted(b.name for b in rig.data.bones if b.use_deform)
    name_ids = {name: i for i, name in enumerate(names)}
    groups = {g.index: name_ids[g.name] for g in body.vertex_groups if g.name in name_ids}
    data, maximum = bytearray(), 0
    for vertex in body.data.vertices:
        pairs = [(groups[g.group], g.weight) for g in vertex.groups if g.group in groups and g.weight > 0]
        total = sum(weight for _, weight in pairs)
        if not 0 < len(pairs) < 256 or total <= 0:
            raise RuntimeError('Unsupported vertex influence count or unweighted vertex')
        pairs = sorted(((bone, weight / total) for bone, weight in pairs), key=lambda p: -p[1])
        maximum = max(maximum, len(pairs))
        data.extend(struct.pack('<B', len(pairs)))
        for bone, weight in pairs:
            data.extend(struct.pack('<Hf', bone, weight))
    path = out / 'weights.bin'
    path.write_bytes(data)
    index['weights'] = {
        'path': str(path), 'file': path.name, 'vertices': len(body.data.vertices),
        'bone_names': names, 'maximum_influences': maximum,
        'sha256': hashlib.sha256(data).hexdigest(), 'byte_count': len(data),
        'format': 'Per vertex UInt8 count, then UInt16 bone index and float32 normalized weight; little endian, no padding',
        'normalization': 'All positive actual deform weights, normalized per vertex, descending order, no cutoff',
    }
    if digest(source) != before:
        raise RuntimeError('Source changed during capture')
    (out / 'index.json').write_text(json.dumps(index, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'output': str(out), 'vertices': len(body.data.vertices), 'poses': len(POSES),
                      'non_surface_vertices': wire, 'maximum_influences': maximum,
                      'source_unchanged': True}), flush=True)


if __name__ == '__main__':
    main()
