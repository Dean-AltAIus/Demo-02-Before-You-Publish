"""Export native NPC surfaces and exact skin weights without saving the blend.

In isolated Blender, pass --factory-startup --python-exit-code 1 before this
script, then -- --role lead --output-dir LocalOnly/Validation/NewLeadFixture.
Use a new output directory. Existing source and derivative files are preserved.
"""
import argparse
import collections
import hashlib
import json
import struct
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[3]
PROFILES = {
    'lead': {
        'role': 'ModuleLead', 'version': 'v019',
        'poses': [('SeatedIdle', 1), ('PickupPacket', 34), ('ReadPacket', 19),
                  ('ReadPacket', 37), ('PointCurrentPair', 16), ('ReturnPacket', 8)],
    },
    'colleague': {
        'role': 'Colleague', 'version': 'v001',
        'poses': [('Idle', 1), ('Listen', 61), ('Speak', 46),
                  ('SourcePoint', 16), ('SourcePoint', 40), ('SourcePoint', 61)],
    },
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--role', choices=PROFILES, required=True)
    parser.add_argument('--source', type=Path, help='Optional edited native source, retaining the named rig/actions')
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    profile = PROFILES[args.role]
    role, version = profile['role'], profile['version']
    source = args.source or Path(f'SourceArt/Demo02/Characters/{role}/{role}_Interactions_{version}.blend')
    if not source.is_absolute():
        source = ROOT / source
    out = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
    if out.exists():
        raise RuntimeError('Preserve existing output directory: ' + str(out))
    source_hash = digest(source)
    bpy.ops.wm.open_mainfile(filepath=str(source))
    scene = bpy.data.scenes[f'Demo02_{role}Rig_v001']
    bpy.context.window.scene = scene
    rig = bpy.data.objects[f'{role}_EditableRig']
    body = bpy.data.objects[f'{role}_WeightedMesh']
    modifiers = [m for m in body.modifiers if m.type == 'ARMATURE' and m.show_viewport]
    if len(modifiers) != 1:
        raise RuntimeError('Expected one active armature modifier')
    modifier = modifiers[0]
    if (modifier.object != rig or not modifier.use_vertex_groups or modifier.use_bone_envelopes
            or modifier.vertex_group or modifier.use_deform_preserve_volume):
        raise RuntimeError('Unsupported skinning mode; verify the changed native contract before exporting')
    for take, frame in profile['poses']:
        action = bpy.data.actions[f'D02_{role}_{take}_{version}']
        if not action.frame_range[0] <= frame <= action.frame_range[1]:
            raise RuntimeError(f'Sample outside action range: {take}:{frame}')
    out.mkdir(parents=True)
    try:
        source_label = source.relative_to(ROOT).as_posix()
    except ValueError:
        source_label = str(source)
    index = {
        'source': source_label, 'source_sha256': source_hash,
        'body': body.name, 'rig': rig.name, 'raw_vertex_count': len(body.data.vertices),
        'format': 'Headerless little-endian float32 XYZ, interleaved, native evaluated vertex order',
        'space': 'Blender native world metres, Z up, character faces -Y',
        'unity_observed_basis': '(-x,z,-y), apply actor placement separately',
        'rest_method': 'Armature REST; retain all saved geometry modifiers',
        'modifiers': [{'name': m.name, 'type': m.type, 'viewport': m.show_viewport, 'render': m.show_render} for m in body.modifiers],
        'samples': [], 'source_saved': False,
    }

    def capture(label, take=None, frame=None):
        bpy.context.view_layer.update()
        evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        try:
            if len(mesh.vertices) != len(body.data.vertices):
                raise RuntimeError('Vertex count changed; source-order weight mapping is invalid')
            positions = [evaluated.matrix_world @ v.co for v in mesh.vertices]
            data = b''.join(struct.pack('<fff', *p) for p in positions)
            filename = label + '.f32'
            (out / filename).write_bytes(data)
            sample = {'label': label, 'file': filename, 'take': take, 'frame': frame,
                      'vertices': len(positions), 'byte_count': len(data),
                      'sha256': hashlib.sha256(data).hexdigest(),
                      'bounds_min': [min(p[k] for p in positions) for k in range(3)],
                      'bounds_max': [max(p[k] for p in positions) for k in range(3)]}
            if take is None:
                sample['unique_positions_at_1um'] = len({tuple(round(float(v), 6) for v in p) for p in positions})
            index['samples'].append(sample)
        finally:
            evaluated.to_mesh_clear()

    rig.data.pose_position = 'REST'
    scene.frame_set(1)
    capture('Rest')
    rig.data.pose_position = 'POSE'
    for take, frame in profile['poses']:
        action = bpy.data.actions[f'D02_{role}_{take}_{version}']
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        scene.frame_set(frame)
        capture(f'{take}_{frame}', take, frame)

    names = [b.name for b in rig.data.bones if b.use_deform]
    bone_ids = {name: i for i, name in enumerate(names)}
    group_ids = {g.index: bone_ids[g.name] for g in body.vertex_groups if g.name in bone_ids}
    data, counts, sums = bytearray(), collections.Counter(), []
    for vertex in body.data.vertices:
        weights = sorted(((group_ids[g.group], g.weight) for g in vertex.groups
                          if g.group in group_ids and g.weight > 0), key=lambda p: (-p[1], p[0]))
        total = sum(weight for _, weight in weights)
        if not 0 < len(weights) < 256 or total <= 0:
            raise RuntimeError('Unsupported unweighted vertex or influence count')
        counts[len(weights)] += 1
        sums.append(total)
        data.extend(struct.pack('<B', len(weights)))
        for bone, weight in weights:
            data.extend(struct.pack('<Hf', bone, weight / total))
    (out / 'weights.bin').write_bytes(data)
    index['weights'] = {
        'file': 'weights.bin',
        'schema': 'Per native vertex: UInt8 count, then count pairs of UInt16 bone index and Float32 normalized weight. Little endian, no padding or header.',
        'bone_names': names, 'vertices': len(body.data.vertices),
        'byte_count': len(data), 'sha256': hashlib.sha256(data).hexdigest(),
        'sort': 'Descending weight, then bone index',
        'normalization': 'Positive deform weights divided by per-vertex sum. No minimum cutoff.',
        'raw_weight_sum_range': [min(sums), max(sums)],
        'influence_counts': dict(sorted(counts.items())),
        'armature_envelopes': False, 'preserve_volume': False, 'modifier_mask_group': '',
    }
    index['source_sha256_after'] = digest(source)
    if index['source_sha256_after'] != source_hash:
        raise RuntimeError('Source changed during export')
    (out / 'index.json').write_text(json.dumps(index, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'output': str(out), 'surfaces': len(index['samples']),
                      'vertices': len(body.data.vertices), 'max_influences': max(counts),
                      'source_unchanged': True}), flush=True)


if __name__ == '__main__':
    main()
