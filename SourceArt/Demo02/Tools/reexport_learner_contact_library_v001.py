"""Reexport an explicitly selected learner master without regenerating or saving it.

Run in isolated Blender with --factory-startup --python-exit-code 1, then this
script and -- --output-dir LocalOnly/Validation/NewLearnerExport.
Only the 29 current production takes are admitted. Existing outputs are refused.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path('SourceArt/Demo02/Characters/Learner/AcademicLearner_ContactLibrary_v013.blend')
PROFILE = ROOT / 'SourceArt/Demo02/Exports/ContactLibrary_v013/receipt.json'
TAKES = {'Idle', 'Walk', 'FolioOpen', 'FolioOpenApproach', 'FolioOpenRecovery',
         'FolioClose', 'FolioCloseApproach', 'FolioCloseRecovery', 'Passage',
         'PassageApproach', 'PassageRecovery', 'PageTurn', 'PageTurnApproach',
         'PageTurnRecovery', 'EditionSlide', 'EditionSlideApproach', 'EditionSlideRecovery',
         'PenPoint', 'PenPointApproach', 'PenPointRecovery', 'PassageLift',
         'PassageTransfer', 'PassageRelease', 'PenApproach', 'MarkPoint', 'PenRest',
         'Stop', 'Turn', 'PacketPresent'}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=SOURCE)
    parser.add_argument('--output-dir', type=Path, required=True)
    parser.add_argument('--takes', nargs='+', help='Optional logical take keys for a bounded clip revision')
    parser.add_argument('--bake-step', type=float, help='Native frame step; may not omit required fractional source keys')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    source = (ROOT / args.source).resolve()
    out = (ROOT / args.output_dir).resolve()
    if out.exists():
        raise RuntimeError('Preserve existing output directory: ' + str(out))
    profile = json.loads(PROFILE.read_text(encoding='utf-8'))
    clips = profile['clips']
    names = {clip['name'] for clip in clips}
    if (len(clips) != 29 or names != {f'D02_Learner_{key}_v013' for key in TAKES}
            or {clip['key'] for clip in clips} != TAKES or profile['root_motion'] is not False):
        raise RuntimeError('Changed production contract requires an explicitly revised exporter/profile')
    before = digest(source)
    bpy.ops.wm.open_mainfile(filepath=str(source))
    scene = bpy.data.scenes['Demo02_LearnerRig_v001']
    bpy.context.window.scene = scene
    required_step = float(scene.get('required_fbx_bake_step', 1))
    bake_step = args.bake_step if args.bake_step is not None else required_step
    if not 0 < bake_step <= min(1, required_step):
        raise RuntimeError('Bake step would omit required source keys or is invalid')
    selected = set(args.takes) if args.takes else TAKES
    if not selected or not selected.issubset(TAKES):
        raise RuntimeError('Unknown logical take selection')
    exported_clips = [clip for clip in clips if clip['key'] in selected]
    exported_names = {clip['name'] for clip in exported_clips}
    if scene.render.fps != 30 or scene.render.fps_base != 1:
        raise RuntimeError('Preserve the current 30fps timing contract')
    rig = bpy.data.objects['AcademicLearner_EditableRig']
    body = bpy.data.objects['AcademicLearner_WeightedMesh']
    root = bpy.data.objects['AcademicLearner_Root']
    objects = [root, rig, body]
    if len(rig.data.bones) != profile['bone_count']:
        raise RuntimeError('Changed rig requires renewed import admission')
    rig.data.pose_position = 'POSE'
    scene.frame_set(1)
    bpy.context.view_layer.update()
    baseline = {obj.name: obj.matrix_world.copy() for obj in objects}
    samples, max_root_delta = 0, 0.0
    for clip in clips:
        action = bpy.data.actions.get(clip['name'])
        if action is None:
            raise RuntimeError('Missing current take: ' + clip['name'])
        start, end = action.frame_range
        if abs(start - 1) > 1e-5 or abs(end - clip['frames']) > 1e-5:
            raise RuntimeError('Changed clip range requires renewed timing admission: ' + clip['name'])
        if abs((clip['frames'] - 1) / 30 - clip['duration']) > 1e-7:
            raise RuntimeError('Inconsistent recorded frame/duration profile')
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        for frame in range(1, clip['frames'] + 1):
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            samples += 1
            for obj in objects:
                delta = max(abs(obj.matrix_world[i][j] - baseline[obj.name][i][j])
                            for i in range(4) for j in range(4))
                max_root_delta = max(max_root_delta, delta)
    if max_root_delta > 1e-6:
        raise RuntimeError('Object roots moved; preserve motor-owned world translation')
    # Remove historical takes only in this isolated in-memory copy.
    for action in list(bpy.data.actions):
        if action.name not in exported_names:
            bpy.data.actions.remove(action)
    # Retiming this private export copy to integer high-rate frames avoids an
    # importer silently resampling dense fractional keys back to a 30 Hz file.
    frame_scale = round(1 / bake_step)
    if abs(frame_scale * bake_step - 1) > 1e-7:
        raise RuntimeError('Bake step must be a reciprocal integer frame rate')
    if frame_scale != 1:
        for action in bpy.data.actions:
            for layer in action.layers:
                for strip in layer.strips:
                    for bag in strip.channelbags:
                        for curve in bag.fcurves:
                            for key in curve.keyframe_points:
                                key.co.x = (key.co.x - 1) * frame_scale + 1
                                key.handle_left.x = (key.handle_left.x - 1) * frame_scale + 1
                                key.handle_right.x = (key.handle_right.x - 1) * frame_scale + 1
        scene.render.fps = 30 * frame_scale
    idle = bpy.data.actions[exported_clips[0]['name']]
    rig.animation_data.action = idle
    rig.animation_data.action_slot = idle.slots[0]
    scene.frame_set(1)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    out.mkdir(parents=True)
    target = out / (source.stem + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(target), use_selection=True, object_types={'EMPTY', 'ARMATURE', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
        add_leaf_bones=False, use_armature_deform_only=True, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', bake_anim=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True, bake_anim_step=1, bake_anim_simplify_factor=0,
        path_mode='RELATIVE', embed_textures=False,
    )
    if digest(source) != before:
        raise RuntimeError('Source changed during export')
    receipt = {
        'source': str(source), 'source_sha256': before,
        'profile': PROFILE.relative_to(ROOT).as_posix(), 'profile_sha256': digest(PROFILE),
        'matches_original_source_pin': before == profile['source_sha256'],
        'export': target.name, 'export_sha256': digest(target), 'clips': exported_clips,
        'profile_clip_count': len(clips), 'bake_frame_step': bake_step,
        'samples_per_second': 30 / bake_step,
        'export_fps': scene.render.fps, 'export_frame_scale': frame_scale,
        'export_ranges': [{'key': clip['key'], 'frames': (clip['frames'] - 1) * frame_scale + 1,
                           'duration': clip['duration']} for clip in exported_clips],
        'bone_count': len(rig.data.bones), 'fps': 30, 'root_motion': False,
        'root_policy': 'Fixed object roots; runtime motor owns world displacement',
        'source_frames_checked': samples, 'maximum_object_root_matrix_delta': max_root_delta,
        'source_saved': False, 'historical_actions_exported': False,
        'status': 'Native reexport only. Renew actual Unity surface, contact and playback acceptance after edits.',
    }
    (out / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'export': str(target), 'clips': len(exported_clips), 'frames': samples,
                      'root_delta': max_root_delta, 'source_unchanged': True}), flush=True)


if __name__ == '__main__':
    main()
