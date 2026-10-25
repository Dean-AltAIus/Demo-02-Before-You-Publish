"""Correct the visible floating/conical flower candidate before Unity admission."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
R=Path(r'D:\Altaius\Demo-02-Before-You-Publish');dest=R/'SourceArt/Demo02/Environment/FullPlanter_v003.blend';out=R/'SourceArt/Demo02/Exports/FullPlanter_v003'
if dest.exists() or out.exists():raise RuntimeError('Preserve existing revision')
out.mkdir(parents=True);bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Demo02/Environment/FullPlanter_v002.blend'))
s=bpy.data.scenes['Demo02_FullPlanter_v002'];s.name='Demo02_FullPlanter_v003';bpy.context.window.scene=s;root=bpy.data.objects['FullPlanter']
for o in list(root.children):
 if o.name.startswith(('Sunflower petals','Sunflower seed head')):bpy.data.objects.remove(o,do_unlink=True)
leaves=next(o for o in root.children if o.name.startswith('Dense mixed planter'));leaves.location.z-=.10
gold=bpy.data.materials['D02_SunflowerPetal'];wood=bpy.data.materials['D02_NoticeOak'];green=bpy.data.materials['D02_LeafShade']
def sphere(name,p,scale,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=p);o=bpy.context.object;o.name=name;o.parent=root;o.scale=scale;o.data.materials.append(mat)
 for f in o.data.polygons:f.use_smooth=True
 return o
def rod(name,a,b,r,mat):
 a=Vector(a);b=Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.name=name;o.parent=root;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();o.data.materials.append(mat)
 for p in o.data.polygons:p.use_smooth=True
for k in range(8):
 center=Vector((-.45+k*.13,-.05+.06*math.sin(k*1.7),.96+.12*math.sin(k*.9)))
 rod('Visible flower stem '+str(k),(center.x,center.y,.56),center,.006,green)
 for i in range(11):
  angle=i*math.tau/11;radial=Vector((math.sin(angle),0,math.cos(angle)));o=sphere('Editable sunflower petal',center+radial*.072,(.027,.009,.060),gold);o.rotation_euler=radial.to_track_quat('Z','Y').to_euler()
 sphere('Smooth sunflower seed centre',center+Vector((0,-.015,0)),(.035,.018,.035),wood)
for k in range(5):
 x=-.35+k*.175;rod('Grounded shrub stem '+str(k),(x,0,.56),(x,.05,.83),.011,green)
parts=[o for o in root.children_recursive if o.type=='MESH'];bpy.context.view_layer.update();scratch=bpy.data.scenes.new('Full planter normalized export');bpy.context.window.scene=scratch
for src in parts:
 o=src.copy();o.data=src.data.copy();scratch.collection.objects.link(o);o.parent=None;o.matrix_world=src.matrix_world.copy();o.select_set(True)
bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');bpy.ops.object.join();joined=bpy.context.object;joined.name='FullPlanter';scratch.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(joined.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(joined.data);bm.free()
bpy.ops.export_scene.fbx(filepath=str(out/'FullPlanter.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='AUTO')
payload={'assets':[{'name':'FullPlanter','native_mesh_parts':len(parts),'export_vertices':len(joined.data.vertices),'materials':[m.name for m in joined.data.materials]}],'materials':[{'name':m.name,'linear_base_color':list(m.diffuse_color),'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value} for m in joined.data.materials],'status':'Grounded stems and outward radial flowers. Native preview and Unity acceptance required.'}
bpy.data.scenes.remove(scratch);bpy.context.window.scene=s;s.render.filepath=str(R/'LocalOnly/Captures/Environment/FullPlanter_v003.png');(out/'manifest.json').write_text(json.dumps(payload,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(dest));bpy.ops.render.render(write_still=True);print(json.dumps(payload))
