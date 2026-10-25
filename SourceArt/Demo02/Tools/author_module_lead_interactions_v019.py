"""Complete the editable seated reviewer packet cycle from validated native poses.

One packet, separate native prop actions, no gameplay authority. Existing sources
are preserved. Every newly authored frame is checked against evaluated volumes.
"""
import bpy,json,math,sys,hashlib
from pathlib import Path
from mathutils import Vector,Quaternion
sys.path.insert(0,str(Path(__file__).parent))
from npc_contact_math import NativeNpcRig
R=Path(r'D:/Altaius/Demo-02-Before-You-Publish');version='v019'
source=R/'SourceArt/Demo02/Characters/ModuleLead/ModuleLead_PickupRead_v012.blend'
dest=R/'SourceArt/Demo02/Characters/ModuleLead'/('ModuleLead_Interactions_'+version+'.blend')
out=R/'LocalOnly/Validation/Production/NPCInteractions_v001'/('LeadInteractions_'+version)
if dest.exists() or (out.exists() and any(out.iterdir())):raise RuntimeError('Preserve motion candidate')
out.mkdir(parents=True,exist_ok=True)
gate=json.loads((R/'LocalOnly/Validation/Production/NPCInteractions_v001/VolumeAudit_v001/volume-report.json').read_text())
if any(t['summary']['penetrating_frames'] or t['summary']['disagreement_count'] for e in gate['sources'] if not e['source'].endswith('v007.blend') for t in e['takes']):raise RuntimeError('Prior corrected volume gate failed')
point_fit=json.loads((R/'LocalOnly/Validation/Production/NPCInteractions_v001/LeadReturnProbe_v016/audit.json').read_text())
if any(o['inside_vertex_count'] for r in point_fit['audits'] for o in r['objects']):raise RuntimeError('Static point/placement gate failed')
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Characters/ModuleLead/ModuleLead_PickupContact_v008.blend'))
bpy.context.view_layer.update();solve_basis={p.name:p.matrix.copy() for p in bpy.data.objects['ModuleLead_EditableRig'].pose.bones}
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.data.scenes['Demo02_ModuleLeadRig_v001'];bpy.context.window.scene=s
a=bpy.data.objects['ModuleLead_EditableRig'];body=bpy.data.objects['ModuleLead_WeightedMesh'];folio=bpy.data.objects['Folio'];hinge=bpy.data.objects['FolioCoverHinge'];objects=[a,folio,hinge]
chains={n:[Vector(v) for v in pts] for n,pts in json.loads((R/'LocalOnly/Validation/Production/NPCInteractions_v001/LeadFingerControls/finger-rig-audit.json').read_text())['joint_world_positions'].items()}
props=[bpy.data.objects[n] for n in ['Front cover','Back cover','Thin paper stack']];contact={'Left':props[0],'Right':props[1]}
table=next(o for o in bpy.data.objects['ReviewDesk'].children_recursive if o.name.startswith('Oak worktop'))
def select(take,frame):
    for obj,suffix in zip(objects,['','_PacketRoot','_Cover']):
        act=bpy.data.actions['D02_ModuleLead_'+take+suffix+'_v012'];obj.animation_data.action=act;obj.animation_data.action_slot=act.slots[0]
    s.frame_set(frame);bpy.context.view_layer.update()
def capture():
    return {'bones':{p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in a.pose.bones},'packet':(folio.location.copy(),folio.rotation_quaternion.copy()),'cover':hinge.rotation_quaternion.copy()}
def apply(state):
    for p in a.pose.bones:p.location,p.rotation_quaternion,p.scale=state['bones'][p.name]
    folio.location,folio.rotation_quaternion=state['packet'];hinge.rotation_quaternion=state['cover'];bpy.context.view_layer.update()
cache={}
for take,end in [('SeatedIdle',121),('SeatedListen',121),('PickupPacket',34),('ReadPacket',37)]:
    cache[take]=[]
    for frame in range(1,end+1):select(take,frame);cache[take].append(capture())
select('PickupPacket',19);rig=NativeNpcRig(a,body,chains);rig.base=solve_basis
binding={side:o.matrix_world.inverted()@(a.matrix_world@a.pose.bones[side+'Hand'].matrix) for side,o in contact.items()}
for obj in objects:obj.animation_data.action=None
# Make pickup begin at the exact neutral seated head pose, then attend to the book.
neutral_q=cache['SeatedIdle'][0]['bones']['Head'][1].copy()
for i in range(7):
    loc,q,scale=cache['PickupPacket'][i]['bones']['Head'];u=i/6;u=u*u*(3-2*u)
    cache['PickupPacket'][i]['bones']['Head']=(loc,neutral_q.slerp(q,u),scale)
ready=cache['SeatedIdle'][0];held=cache['ReadPacket'][-1];closed=cache['PickupPacket'][18]
def ease(x):x=max(0,min(1,x));return x*x*(3-2*x)
def put(side,wrist,q,pole=None):return rig.wrist(side,wrist,q,pole=pole if pole is not None else ((.28,-.20,.83) if side=='Left' else (-.25,-.07,.80)))
def hand(side):return (a.matrix_world@a.pose.bones[side+'Hand'].matrix).copy()
def attach(left_release=0):
    margins={}
    for side,obj in contact.items():
        m=obj.matrix_world@binding[side];position=m.translation.copy()
        if side=='Left':position+=(obj.matrix_world.to_3x3()@Vector((0,-1,0))).normalized()*left_release
        margins[side]=put(side,position,a.matrix_world.inverted().to_quaternion()@m.to_quaternion())
    return margins
def blend_head(start,end,u):
    a.pose.bones['Head'].rotation_quaternion=start['bones']['Head'][1].slerp(end['bones']['Head'][1],u);bpy.context.view_layer.update()
cache['PlaceOpen']=[]
for frame in range(13):
    apply(held);u=ease(frame/12);folio.location=held['packet'][0].lerp(Vector((0,-.40,.815)),u);folio.rotation_quaternion=held['packet'][1].slerp(Quaternion(Vector((0,0,1)),math.pi),u)
    hinge.rotation_quaternion=Quaternion(Vector((0,1,0)),math.radians(-175-8*u));bpy.context.view_layer.update();attach(.045*ease((u-.70)/.30));blend_head(held,closed,u);cache['PlaceOpen'].append(capture())
placed=cache['PlaceOpen'][-1];apply(placed);placed_hands={side:hand(side) for side in contact}
for digit in ['Thumb','Index','Middle','Ring','Little']:
    rig.curl('Left',digit,(-5,0,0) if digit=='Thumb' else (4,8,3),-10 if digit=='Thumb' else 0)
    rig.curl('Right',digit,(2,4,2) if digit=='Index' else (35,45,10),0)
left_q=rig.hand_rotation('Left',(0,-1,0),(0,0,-1));put('Left',point_fit['left_support_fit']['wrist'],left_q)
put('Right',point_fit['point_wrist'],Quaternion(point_fit['point_rotation']));point=capture();point_hands={side:hand(side) for side in contact}
finger_names=[p.name for p in a.pose.bones if any(d in p.name for d in ['Thumb','Index','Middle','Ring','Little'])]
cache['PointCurrentPair']=[]
for frame in range(16):
    apply(placed);t=frame/30
    for side in contact:
        start=placed_hands[side];finish=point_hands[side];safe_start=start.translation.copy();safe_start.y=-.02;safe_end=finish.translation.copy();safe_end.y=-.02;safe_end.z+=.035
        if t<=.12:
            u=ease(t/.12);position=start.translation.lerp(safe_start,u);q=start.to_quaternion();finger_u=0
        elif t<=.32:
            u=ease((t-.12)/.20);position=safe_start.lerp(safe_end,u);q=start.to_quaternion().slerp(finish.to_quaternion(),u);finger_u=u
        else:
            u=ease((t-.32)/.18);position=safe_end.lerp(finish.translation,u);q=finish.to_quaternion();finger_u=1
        for name in finger_names:
            if name.startswith(side):a.pose.bones[name].rotation_quaternion=placed['bones'][name][1].slerp(point['bones'][name][1],finger_u)
        pole=Vector((.28,-.20,.83) if side=='Left' else (-.25,-.07,.80)).lerp(Vector((.23,.04,.75) if side=='Left' else (-.25,.02,.75)),ease(t/.20))
        put(side,position,a.matrix_world.inverted().to_quaternion()@q,pole)
    cache['PointCurrentPair'].append(capture())
cache['RecoverPoint']=list(reversed(cache['PointCurrentPair']))
cache['ReturnPacket']=[]
for frame in range(16):
    apply(placed);t=frame/15;u=ease(t)
    folio.location=Vector((.115*u,-.40,.815+.04*math.sin(math.pi*t)**2));hinge.rotation_quaternion=Quaternion(Vector((0,1,0)),math.radians(-183*(1-u)))
    bpy.context.view_layer.update();attach(.045*(1-ease(t/.28)));cache['ReturnPacket'].append(capture())
cache['RecoverToReady']=list(reversed(cache['PickupPacket'][:19]))
cache['SeatedSpeak']=[]
for frame in range(91):
    apply(ready);u=frame/90;amplitude=math.sin(math.pi*u)**2
    a.pose.bones['Head'].rotation_quaternion=neutral_q@Quaternion(Vector((1,0,0)),math.radians(1.5*math.sin(4*math.pi*u)*amplitude));bpy.context.view_layer.update();cache['SeatedSpeak'].append(capture())
report={'source':str(source.relative_to(R)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'fps':30,'takes':[],'boundaries':[],'status':'Native candidate. Actual Unity playback, contacts, state binding, cancellation and reset remain required.'}
all_ids=list(range(len(body.data.vertices)))
for take in ['PlaceOpen','PointCurrentPair','RecoverPoint','ReturnPacket','RecoverToReady','SeatedSpeak']:
    rows=[]
    for frame,state in enumerate(cache[take],1):
        apply(state);checks=rig.audit_objects(props)+rig.audit_objects([table],ids=all_ids,penetration_only=True)
        rows.append({'frame':frame,'objects':checks})
    row={'take':take,'frames':len(rows),'penetrating_frames':sum(any(o['inside_vertex_count'] for o in f['objects']) for f in rows),'max_depth_m':max(o['max_depth_m'] for f in rows for o in f['objects']),'rows':rows};report['takes'].append(row)
    print('AUDIT='+json.dumps({k:v for k,v in row.items() if k!='rows'}),flush=True)
(out/'contact-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
def pose_matrices(state):
    apply(state);return {p.name:(a.matrix_world@p.matrix).copy() for p in a.pose.bones}|{'Packet':folio.matrix_world.copy(),'Cover':hinge.matrix_world.copy()}
for start,end in [('SeatedIdle','PickupPacket'),('PickupPacket','ReadPacket'),('ReadPacket','PlaceOpen'),('PlaceOpen','PointCurrentPair'),('PointCurrentPair','RecoverPoint'),('RecoverPoint','ReturnPacket'),('ReturnPacket','RecoverToReady'),('RecoverToReady','SeatedIdle')]:
    before=pose_matrices(cache[start][-1]);after=pose_matrices(cache[end][0]);error=max(abs(m[i][j]-after[n][i][j]) for n,m in before.items() for i in range(4) for j in range(4))
    report['boundaries'].append({'from':start,'to':end,'maximum_world_matrix_error':error})
print('BOUNDARIES='+json.dumps(report['boundaries']),flush=True)
(out/'contact-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
def render(take,frame,suffix,offset=(-2.2,-3,.8),scale=1.65):
    apply(cache[take][frame-1]);look=Vector((0,-.23,.89));s.camera.location=look+Vector(offset);s.camera.rotation_euler=(look-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=scale;s.render.filepath=str(out/(take+'-'+str(frame)+'-'+suffix+'.png'));bpy.ops.render.render(write_still=True)
s.render.resolution_x=s.render.resolution_y=640;s.render.resolution_percentage=100;s.render.threads_mode='FIXED';s.render.threads=2;s.cycles.samples=6
if any(t['penetrating_frames'] for t in report['takes']) or any(b['maximum_world_matrix_error']>.0001 for b in report['boundaries']):
    for take in report['takes']:
        bad=next((f['frame'] for f in take['rows'] if any(o['inside_vertex_count'] for o in f['objects'])),None)
        if bad:render(take['take'],bad,'failure')
    raise RuntimeError('Correct recorded candidate failures before saving or exporting')
def collect(payload,obj,frame,paths,bone=None):
    target=bone or obj
    for path in paths:
        data_path=bone.path_from_id(path) if bone else path
        for index,value in enumerate(getattr(target,path)):payload.setdefault((data_path,index),[]).extend([float(frame),float(value)])
        if frame==1:target.keyframe_insert(data_path=path,frame=frame,group=bone.name if bone else obj.name)
def write(action,payload):
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in bag.fcurves:
                    coordinates=payload[(curve.data_path,curve.array_index)];curve.keyframe_points.add(len(coordinates)//2-len(curve.keyframe_points));curve.keyframe_points.foreach_set('co',coordinates)
                    for key in curve.keyframe_points:key.interpolation='LINEAR'
                    curve.update()
actions={};tracks=[]
for old in bpy.data.actions:old.use_fake_user=True
for take,states in cache.items():
    trio=[]
    for obj,suffix in zip(objects,['','_PacketRoot','_Cover']):
        action=bpy.data.actions.new('D02_ModuleLead_'+take+suffix+'_'+version);action.use_fake_user=True;obj.animation_data.action=action;trio.append(action)
    payloads=[{},{},{}];frames=[]
    for frame,state in enumerate(states,1):
        apply(state)
        for p in a.pose.bones:p.rotation_mode='QUATERNION';collect(payloads[0],a,frame,['location','rotation_quaternion','scale'],p)
        collect(payloads[1],folio,frame,['location','rotation_quaternion']);collect(payloads[2],hinge,frame,['rotation_quaternion'])
        frames.append({'frame':frame,'seconds':(frame-1)/30,'packet_position':list(folio.location),'packet_rotation_xyzw':[folio.rotation_quaternion.x,folio.rotation_quaternion.y,folio.rotation_quaternion.z,folio.rotation_quaternion.w],'cover_rotation_xyzw':[hinge.rotation_quaternion.x,hinge.rotation_quaternion.y,hinge.rotation_quaternion.z,hinge.rotation_quaternion.w]})
    for action,payload in zip(trio,payloads):write(action,payload)
    actions[take]=trio;tracks.append({'take':take,'fps':30,'frames':frames})
report['tracks']=tracks;report['saved_source']=str(dest.relative_to(R));report['point_target_native_m']=[-.10,-.32,.8305]
(out/'contact-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
for obj,action in zip(objects,actions['SeatedIdle']):obj.animation_data.action=action;obj.animation_data.action_slot=action.slots[0]
s.frame_start=1;s.frame_end=121;s.frame_set(1);s['motion_gate']='Native packet-cycle candidate. See corrected all-frame contact and boundary audit. Unity acceptance pending.'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
for obj in objects:obj.animation_data.action=None
for take,frame in [('PointCurrentPair',8),('PointCurrentPair',16)]:render(take,frame,'front')
for suffix,offset in [('left',(-3,0,.4)),('right',(3,0,.4)),('back',(0,3,.4))]:render('PointCurrentPair',16,suffix,offset)
print('NATIVE_CYCLE_SAVED='+str(dest),flush=True)
