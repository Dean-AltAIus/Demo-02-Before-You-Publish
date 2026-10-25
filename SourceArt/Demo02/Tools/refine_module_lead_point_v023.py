"""Bounded reviewer gesture revision, preserving v019 and all other takes.

Run in isolated Blender with --factory-startup --python-exit-code 1. The saved
v019 source and v008 solve basis are pinned by the tracked v023 controls. Only
PointCurrentPair and its reversed RecoverPoint actor curves are reauthored.
Existing files are never replaced. This is source authoring, not gameplay proof.
"""
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).parent))
from npc_contact_math import NativeNpcRig

ROOT = Path(__file__).resolve().parents[3]
FOLDER = ROOT / 'SourceArt/Demo02/Characters/ModuleLead'
DEST = FOLDER / 'ModuleLead_Interactions_v023.blend'
CONTROLS = FOLDER / 'ModuleLead_Interactions_v023.controls.json'
OUT = ROOT / 'LocalOnly/Validation/Production/NPCInteractions_v001/LeadPointRefinement_v023/SavedCandidate'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def ease(value):
    value = max(0, min(1, value))
    return value * value * (3 - 2 * value)


def main():
    if DEST.exists() or OUT.exists():
        raise RuntimeError('Preserve the existing native candidate and evidence')
    controls = json.loads(CONTROLS.read_text(encoding='utf-8'))
    for name, expected in controls['input_sha256'].items():
        if digest(FOLDER / name) != expected:
            raise RuntimeError('Input was edited; preserve it and review regeneration: ' + name)
    source = FOLDER / 'ModuleLead_Interactions_v019.blend'
    source_hash = digest(source)
    bpy.ops.wm.open_mainfile(filepath=str(FOLDER / 'ModuleLead_PickupContact_v008.blend'))
    bpy.context.view_layer.update()
    basis = {p.name: p.matrix.copy() for p in bpy.data.objects['ModuleLead_EditableRig'].pose.bones}
    bpy.ops.wm.open_mainfile(filepath=str(source))
    scene = bpy.data.scenes['Demo02_ModuleLeadRig_v001']
    bpy.context.window.scene = scene
    arm = bpy.data.objects['ModuleLead_EditableRig']
    body = bpy.data.objects['ModuleLead_WeightedMesh']
    folio = bpy.data.objects['Folio']
    hinge = bpy.data.objects['FolioCoverHinge']
    objects = [arm, folio, hinge]
    suffixes = ['', '_PacketRoot', '_Cover']
    props = [bpy.data.objects[n] for n in ['Front cover', 'Back cover', 'Thin paper stack']]
    table = next(o for o in bpy.data.objects['ReviewDesk'].children_recursive if o.name.startswith('Oak worktop'))

    def select(frame):
        for obj, suffix in zip(objects, suffixes):
            action = bpy.data.actions['D02_ModuleLead_PointCurrentPair' + suffix + '_v019']
            obj.animation_data.action = action
            obj.animation_data.action_slot = action.slots[0]
        scene.frame_set(frame)
        bpy.context.view_layer.update()

    def capture():
        return {p.name: (p.location.copy(), p.rotation_quaternion.copy(), p.scale.copy()) for p in arm.pose.bones}

    def apply(pose):
        for p in arm.pose.bones:
            p.location, p.rotation_quaternion, p.scale = pose[p.name]
        bpy.context.view_layer.update()

    select(1)
    first = capture()
    start_hands = {side: (arm.matrix_world @ arm.pose.bones[side + 'Hand'].matrix).copy() for side in ['Left', 'Right']}
    start_elbows = {side: arm.matrix_world @ arm.pose.bones[side + 'ForeArm'].head for side in start_hands}
    select(16)
    last = capture()
    end_hands = {side: (arm.matrix_world @ arm.pose.bones[side + 'Hand'].matrix).copy() for side in start_hands}
    end_elbows = {side: arm.matrix_world @ arm.pose.bones[side + 'ForeArm'].head for side in start_hands}
    for obj in objects:
        obj.animation_data.action = None
    rig = NativeNpcRig(arm, body, {})
    rig.base = basis
    fingers = [p.name for p in arm.pose.bones if any(d in p.name for d in ['Thumb', 'Index', 'Middle', 'Ring', 'Little'])]
    parameters = controls['point_refinement']
    states, rows = [], []
    twist_angles = {}
    for frame in range(16):
        apply(first)
        t = frame / controls['fps']
        if frame == 15:
            apply(last)
        elif frame:
            for side in start_hands:
                start, finish = start_hands[side], end_hands[side]
                safe_start = start.translation.copy()
                safe_start.y = parameters['withdrawal_y_m']
                safe_end = Vector(parameters['turn_exit_wrist_m'][side])
                if t <= .12:
                    u = ease(t / .12)
                    position = start.translation.lerp(safe_start, u)
                    q, finger_u = start.to_quaternion(), 0
                elif t <= .32:
                    u = ease((t - .12) / .20)
                    position = safe_start.lerp(safe_end, u)
                    position.x += (1 if side == 'Left' else -1) * parameters['lateral_arc_m'] * math.sin(math.pi * u)
                    q = start.to_quaternion().slerp(finish.to_quaternion(), u)
                    finger_u = u
                else:
                    u = ease((t - .32) / .18)
                    position = safe_end.lerp(finish.translation, u)
                    q, finger_u = finish.to_quaternion(), 1
                for name in fingers:
                    if name.startswith(side):
                        arm.pose.bones[name].rotation_quaternion = first[name][1].slerp(last[name][1], finger_u)
                preferred = Vector(parameters['elbow_poles_m'][side])
                pole = start_elbows[side].lerp(end_elbows[side], ease(frame / 15)).lerp(preferred, math.sin(math.pi * frame / 15))
                rig.wrist(side, position, arm.matrix_world.inverted().to_quaternion() @ q, pole=pole)
                # Carry the palm roll through the forearm, instead of twisting
                # the wrist against a fixed forearm. Head/tail positions remain.
                lower, hand = arm.pose.bones[side + 'ForeArm'], arm.pose.bones[side + 'Hand']
                hand_matrix = hand.matrix.copy()
                relative = lower.bone.matrix_local.to_quaternion().inverted() @ hand.bone.matrix_local.to_quaternion()
                ideal = hand_matrix.to_quaternion() @ relative.inverted()
                direction = (lower.tail - lower.head).normalized()
                ideal = (ideal @ Vector((0, 1, 0))).rotation_difference(direction) @ ideal
                roll_blend = ease((t - .12) / .20) * ease((.5 - t) / .12)
                base_rotation = lower.matrix.to_quaternion()
                difference = base_rotation.inverted() @ ideal
                twist = 2 * math.atan2(difference.y, difference.w)
                twist = (twist + math.pi) % (2 * math.pi) - math.pi
                previous_twist = twist_angles.get(side, twist)
                while twist - previous_twist > math.pi:
                    twist -= 2 * math.pi
                while twist - previous_twist < -math.pi:
                    twist += 2 * math.pi
                twist_angles[side] = twist
                limit = math.radians(parameters['max_forearm_roll_degrees'])
                bounded_twist = max(-limit, min(limit, twist))
                rotation = base_rotation @ Quaternion(Vector((0, 1, 0)), bounded_twist * roll_blend)
                lower.matrix = Matrix.Translation(lower.head) @ rotation.to_matrix().to_4x4()
                bpy.context.view_layer.update()
                hand.matrix = hand_matrix
                bpy.context.view_layer.update()
        states.append(capture())
        checks = rig.audit_objects(props) + rig.audit_objects([table], ids=list(range(len(body.data.vertices))), penetration_only=True)
        rows.append({'frame': frame + 1, 'checks': checks})
    OUT.mkdir(parents=True)
    report = {'source_sha256': source_hash, 'rows': rows,
              'failed_frames': sum(any(c['inside_vertex_count'] or c['ray_disagreements'] for c in row['checks']) for row in rows),
              'modified_actor_takes': ['PointCurrentPair', 'RecoverPoint'],
              'endpoints': 'Exact v019 point endpoints retained',
              'source_unchanged': digest(source) == source_hash}
    (OUT / 'authoring-audit.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    if report['failed_frames'] or not report['source_unchanged']:
        raise RuntimeError('Gesture failed sampled contact or source-preservation checks')

    actions = {}
    for take in controls['takes']:
        for obj, suffix in zip(objects, suffixes):
            original = bpy.data.actions['D02_ModuleLead_' + take + suffix + '_v019']
            action = original.copy()
            action.name = 'D02_ModuleLead_' + take + suffix + '_v023'
            action.use_fake_user = True
            actions[(take, suffix)] = action
    for take, poses in [('PointCurrentPair', states), ('RecoverPoint', list(reversed(states)))]:
        action = actions[(take, '')]
        payload = {}
        previous = {}
        for frame, pose in enumerate(poses, 1):
            for bone in arm.pose.bones:
                location, rotation, scale = pose[bone.name]
                rotation = rotation.normalized()
                if bone.name in previous and previous[bone.name].dot(rotation) < 0:
                    rotation.negate()
                previous[bone.name] = rotation.copy()
                for path, values in zip(['location', 'rotation_quaternion', 'scale'], [location, rotation, scale]):
                    for index, value in enumerate(values):
                        payload.setdefault((bone.path_from_id(path), index), []).extend([float(frame), float(value)])
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        values = payload.pop((curve.data_path, curve.array_index))
                        if len(curve.keyframe_points) != len(poses):
                            raise RuntimeError('Unexpected point curve sample count')
                        curve.keyframe_points.foreach_set('co', values)
                        for key in curve.keyframe_points:
                            key.interpolation = 'LINEAR'
                        curve.update()
        if payload:
            raise RuntimeError('Missing authored pose channels')
    for obj, suffix in zip(objects, suffixes):
        action = actions[('SeatedIdle', suffix)]
        obj.animation_data.action = action
        obj.animation_data.action_slot = action.slots[0]
    scene.frame_set(1)
    scene['motion_gate'] = 'v023 reviewer gesture candidate. Native and actual Unity playback acceptance are separate.'
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
    if digest(source) != source_hash:
        raise RuntimeError('Original source changed')
    print(json.dumps({'saved': str(DEST), 'sha256': digest(DEST), 'failed_frames': 0, 'original_unchanged': True}), flush=True)


main()
