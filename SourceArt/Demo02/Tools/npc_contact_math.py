"""Native NPC contact utilities. No scene creation, file writes or runtime authority."""
import bpy,math
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree

_VOLUME_RAYS=tuple(Vector(v).normalized() for v in [(1,.371,.193),(-.227,1,.419),(.313,-.197,1)])

def volume_votes(tree,point,extent):
    """Orientation-independent containment for a closed evaluated world mesh.

    Three non-axis rays avoid treating the nearest face's normal as an inside
    test. That shortcut is invalid beyond a thin plate edge. A caller must first
    reject points outside the object's local bounds and near-surface tolerance.
    """
    # Blender's BVH uses float coordinates. Sub-ULP offsets can hit the same
    # triangle forever near metre-scale translations. Two micrometres remains
    # far below the audit's 0.1 mm surface tolerance and the thinnest cover.
    epsilon=max(2e-6,extent*2e-6);votes=[]
    for direction in _VOLUME_RAYS:
        origin=point.copy();hits=0
        for _ in range(256):
            hit,normal,index,distance=tree.ray_cast(origin,direction)
            if hit is None:break
            hits+=1;origin=hit+direction*epsilon
        else:raise RuntimeError('Containment ray did not exit evaluated mesh')
        votes.append(bool(hits%2))
    return votes

class NativeNpcRig:
    def __init__(self,arm,body,chains):
        self.arm=arm;self.body=body;self.chains=chains
        self.base={p.name:p.matrix.copy() for p in arm.pose.bones}
        self.inverse=arm.matrix_world.inverted();self.scale=arm.matrix_world.to_scale().x
        self._hand_id_cache={}

    def solve(self,upper,lower,target,pole,min_margin=.008):
        a=self.arm;p=a.pose.bones[upper];l=a.pose.bones[lower]
        start=a.matrix_world@p.head;target=Vector(target);pole=Vector(pole)
        n=(target-start).normalized();d=(target-start).length;l1=p.length*self.scale;l2=l.length*self.scale
        if not abs(l1-l2)+.001<d<l1+l2-min_margin:raise RuntimeError('Unreachable '+upper+': '+str({'distance':d,'reach':l1+l2,'target':list(target)}))
        x=(l1*l1-l2*l2+d*d)/(2*d);bend=pole-start-n*(pole-start).dot(n);bend.normalize()
        joint=start+n*x+bend*math.sqrt(max(0,l1*l1-x*x))
        for name,head,direction in [(upper,start,joint-start),(lower,joint,target-joint)]:
            p=a.pose.bones[name];q=self.base[name].to_quaternion();q=(q@Vector((0,1,0))).rotation_difference((self.inverse.to_3x3()@direction).normalized())@q
            p.matrix=Matrix.Translation(self.inverse@head)@q.to_matrix().to_4x4();bpy.context.view_layer.update()
        return l1+l2-d

    def hand_rotation(self,side,direction,palmnormal):
        def basis(d,n):
            d=Vector(d).normalized();n=Vector(n);n=(n-d*n.dot(d)).normalized();return Matrix((d.cross(n).normalized(),d,n)).transposed()
        sign=1 if side=='Left' else -1;p=self.arm.pose.bones[side+'Hand']
        rotation=basis(direction,palmnormal)@basis((sign,0,0),(0,0,-1)).transposed()
        return rotation.to_quaternion()@p.bone.matrix_local.to_quaternion()

    def wrist(self,side,target,rotation,pole=None):
        sign=1 if side=='Left' else -1
        margin=self.solve(side+'Arm',side+'ForeArm',target,pole or (sign*.22,0,.75))
        p=self.arm.pose.bones[side+'Hand'];p.matrix=Matrix.Translation(p.head)@rotation.to_matrix().to_4x4();bpy.context.view_layer.update()
        return margin

    def curl(self,side,digit,angles,opposition=0):
        chain=self.chains[side+digit]
        for i,degrees in enumerate(angles):
            p=self.arm.pose.bones[side+digit+str(i+1)];d=(chain[i+1]-chain[i]).normalized();axis=Vector((-d.y,d.x,0)).normalized();q=p.bone.matrix_local.to_quaternion();rot=Quaternion(axis,math.radians(degrees))
            if i==0:rot=Quaternion(Vector((0,0,1)),math.radians(opposition))@rot
            p.rotation_mode='QUATERNION';p.rotation_quaternion=q.inverted()@rot@q

    def skin(self):
        bpy.context.view_layer.update();ev=self.body.evaluated_get(bpy.context.evaluated_depsgraph_get())
        return [ev.matrix_world@v.co for v in ev.data.vertices]

    def hand_ids(self,side,thumb=True,threshold=.65):
        key=(side,thumb,threshold)
        if key in self._hand_id_cache:return self._hand_id_cache[key]
        digits=['Index','Middle','Ring','Little']+(['Thumb'] if thumb else [])
        prefixes=tuple(side+d for d in digits);result=[]
        for v in self.body.data.vertices:
            total=sum(g.weight for g in v.groups if self.body.vertex_groups[g.group].name==side+'Hand' or self.body.vertex_groups[g.group].name.startswith(prefixes))
            if total>threshold:result.append(v.index)
        self._hand_id_cache[key]=result;return result

    def fit_plane(self,side,wrist,rotation,cover,normal_sign,half_thickness,max_shift=.06,half_extents=(.114,.156)):
        wrist=Vector(wrist);normal=(cover.matrix_world.to_3x3()@Vector((0,0,normal_sign))).normalized()
        plane=cover.matrix_world@Vector((0,0,normal_sign*half_thickness));inverse=cover.matrix_world.inverted();ids=self.hand_ids(side,thumb=False)
        total_shift=0
        for iteration in range(5):
            margin=self.wrist(side,wrist,rotation);points=self.skin()
            selected=[i for i in ids if abs((inverse@points[i]).x)<half_extents[0] and abs((inverse@points[i]).y)<half_extents[1]]
            if not selected:raise RuntimeError('No skin overlaps the intended '+side+' cover contact')
            gap=min(normal.dot(points[i]-plane) for i in selected);shift=.0005-gap
            if abs(shift)<.00005:
                return {'side':side,'wrist':list(wrist),'clearance_m':gap,'selected_vertices':len(selected),'shift_m':total_shift,'reach_margin_m':margin}
            if abs(total_shift+shift)>max_shift:raise RuntimeError('Bounded correction exceeded for '+side)
            wrist+=normal*shift;total_shift+=shift
        raise RuntimeError('Support skin did not converge for '+side)

    def audit_objects(self,objects,ids=None,penetration_only=False):
        points=self.skin();ids=self.hand_ids('Left')+self.hand_ids('Right') if ids is None else ids;rows=[]
        for obj in objects:
            ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh()
            vertices=[ev.matrix_world@v.co for v in mesh.vertices]
            polygons=[list(p.vertices) for p in mesh.polygons]
            tree=BVHTree.FromPolygons(vertices,polygons,all_triangles=False)
            inverse=ev.matrix_world.inverted()
            local_lo=[min(v.co[k] for v in mesh.vertices) for k in range(3)]
            local_hi=[max(v.co[k] for v in mesh.vertices) for k in range(3)]
            world_lo=[min(v[k] for v in vertices) for k in range(3)]
            world_hi=[max(v[k] for v in vertices) for k in range(3)]
            extent=(Vector(world_hi)-Vector(world_lo)).length
            edges={}
            for polygon in polygons:
                for j,v in enumerate(polygon):
                    edge=tuple(sorted((v,polygon[(j+1)%len(polygon)])));edges[edge]=edges.get(edge,0)+1
            open_edges=sum(count!=2 for count in edges.values())
            if open_edges:raise RuntimeError('Volume audit requires closed mesh: '+obj.name+' has '+str(open_edges)+' nonmanifold edges')
            if penetration_only:
                lo=[min(v[k] for v in vertices)-.0001 for k in range(3)];hi=[max(v[k] for v in vertices)+.0001 for k in range(3)]
                candidates=[i for i in ids if all(lo[k]<=points[i][k]<=hi[k] for k in range(3))]
            else:candidates=ids
            inside=[];distances=[];disagreements=[]
            for i in candidates:
                hit,normal,index,distance=tree.find_nearest(points[i])
                local=inverse@points[i]
                if distance>.0001 and all(local_lo[k]<=local[k]<=local_hi[k] for k in range(3)):
                    votes=volume_votes(tree,points[i],extent)
                    if len(set(votes))!=1:disagreements.append({'vertex':i,'votes':votes})
                    if sum(votes)>=2:inside.append({'vertex':i,'depth_m':distance,'distance_m':distance,'inside_votes':votes})
                distances.append(distance)
            rows.append({'object':obj.name,'inside_vertex_count':len(inside),'max_depth_m':max((i['depth_m'] for i in inside),default=0),'closest_skin_distance_m':min(distances) if distances else None,'distance_scope':'AABB candidates only' if penetration_only else 'all supplied vertices','vertices_considered':len(ids),'narrow_phase_vertices':len(candidates),'inside':inside,'containment_method':'local AABB plus three-ray parity, 0.1 mm surface tolerance','nonmanifold_edges':open_edges,'ray_disagreements':disagreements});ev.to_mesh_clear()
        return rows
