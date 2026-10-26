using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Collections;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit contextual spacing correction, preserving contact-relative geometry and literal bindings.</summary>
    public static class Task5TabletopClearance
    {
        public static string AuditWordingOffset(string output,float depth,float lateral=0)
        {
            if(EditorApplication.isPlaying||File.Exists(output))throw new InvalidOperationException("Outside Play and new evidence path required.");
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var f=GameObject.Find("Studio contact coordinate frame").transform;var plate=GameObject.Find("Selected criterion wording plate").GetComponentInChildren<MeshFilter>();var inverse=plate.transform.worldToLocalMatrix*Matrix4x4.Translate(-f.TransformVector(new Vector3(lateral,0,depth)));var bounds=plate.sharedMesh.bounds;bounds.Expand(-.0002f);var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab"));actor.hideFlags=HideFlags.HideAndDontSave;var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();var rows=new List<string>{"binding,phase,seconds,vertices_inside_actual_box"};int samples=0,flags=0;
            try{foreach(var b in d.Bindings.Where(x=>x.Id!="PacketPresent")){actor.transform.SetPositionAndRotation(b.Stance.position,b.Stance.rotation);var motion=d.Library.Find(b.MotionId);foreach(var clip in new[]{motion.Approach,motion.Contact,motion.Recovery})for(int i=0;i<=Mathf.CeilToInt(clip.length*120);i++){float t=Mathf.Min(i/120f,clip.length);clip.SampleAnimation(actor,t);skin.BakeMesh(mesh,true);var matrix=inverse*skin.transform.localToWorldMatrix;int count=0;foreach(var v in mesh.vertices)if(bounds.Contains(matrix.MultiplyPoint3x4(v)))count++;samples++;if(count>0){flags++;rows.Add(b.Id+","+clip.name+","+t+","+count);}}}string result="Wording plate depth/lateral offset="+depth+"/"+lateral+" m; "+samples+" full-skin poses against actual rectangular solid, flags="+flags+". All studio contact bindings and both time directions covered by same poses.";rows.Add(result);File.WriteAllLines(output,rows);return result;}
            finally{UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(mesh);}
        }
        public static string AuditWordingPropSweeps(string output,float depth,float lateral)
        {
            if(EditorApplication.isPlaying||File.Exists(output))throw new InvalidOperationException("Outside Play and new evidence path required.");
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var f=GameObject.Find("Studio contact coordinate frame").transform;var plate=GameObject.Find("Selected criterion wording plate").GetComponentInChildren<MeshFilter>();var inverse=plate.transform.worldToLocalMatrix*Matrix4x4.Translate(-f.TransformVector(new Vector3(lateral,0,depth)));var bounds=plate.sharedMesh.bounds;var rows=new List<string>{"binding,seconds,mesh,overlap_local_xyz"};int samples=0,flags=0;
            foreach(var b in d.Bindings.Where(x=>x.Id!="PacketPresent")){var motion=d.Library.Find(b.MotionId);foreach(var mf in b.Prop.GetComponentsInChildren<MeshFilter>(true))for(int i=0;i<=Mathf.CeilToInt(motion.Duration*240);i++){float t=Mathf.Min(i/240f,motion.Duration);var box=BoundsFor(mf,inverse*PropMatrix(b,t)*b.Prop.worldToLocalMatrix*mf.transform.localToWorldMatrix);var overlap=Vector3.Min(box.max,bounds.max)-Vector3.Max(box.min,bounds.min);samples++;if(overlap.x>1e-5f&&overlap.y>1e-5f&&overlap.z>1e-5f){flags++;rows.Add(b.Id+","+t+","+mf.name+","+overlap.ToString("F6"));}}}
            string result="Wording plate depth/lateral offset="+depth+"/"+lateral+" m; "+samples+" actual transformed prop mesh-bound poses at240Hz, overlapping bounds="+flags+". Zero bounds overlap proves geometric separation; any positive pair requires exact review.";rows.Add(result);File.WriteAllLines(output,rows);return result;
        }
        public static string ApplyWorkGroupOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty)throw new InvalidOperationException("Clean saved campus required.");
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var f=GameObject.Find("Studio contact coordinate frame").transform;
            if(Vector3.Distance(f.InverseTransformPoint(GameObject.Find("Studio_WorkFolio").transform.position),new Vector3(.30f,.815f,.49f))>.001f||Mathf.Abs(f.InverseTransformPoint(GameObject.Find("Studio_Pen").transform.position).x+.19f)>.001f)throw new InvalidOperationException("Preserve edited layout; recorded work/pen baseline required.");
            var affected=d.Bindings.Where(x=>WorkBinding(x.Id)).ToArray();var station=UnityEngine.Object.FindObjectsByType<CampusStation>(FindObjectsSortMode.None).Single(x=>x.StationId=="experiment");var delta=f.TransformVector(new Vector3(.3f,0,0));var penDelta=f.TransformVector(new Vector3(-.07f,0,0));
            foreach(var b in affected.Where(x=>x.Id!="WorkClose"))if(station.DistanceTo(b.Stance.position+delta+(b.Id=="MarkPoint"?penDelta:Vector3.zero))>station.InteractionRange)throw new InvalidOperationException("Proposed stance exceeds saved interaction range: "+b.Id);
            var transforms=WorkRoots().Concat(affected.SelectMany(x=>new[]{x.Stance,x.CameraPose})).Where(x=>x).Distinct().ToArray();var pen=d.Bindings.Single(x=>x.Id=="MarkPoint");var extra=new[]{pen.Prop,GameObject.Find("Pen rest | angled socket and support").transform,pen.Stance,pen.CameraPose}.Distinct().ToArray();
            Undo.RecordObjects(transforms.Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[]{d}).ToArray(),"Clear contextual tabletop contacts");foreach(var t in transforms)t.position+=delta;foreach(var t in extra)t.position+=penDelta;foreach(var t in transforms)PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            d.Bindings=d.Bindings.Where(x=>x.Id!="WorkClose").ToArray();EditorUtility.SetDirty(d);PrefabUtility.RecordPrefabInstancePropertyModifications(d);
            string audit=Audit();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Saved work/selected-lane group +300 mm X, pen/rest/MarkPoint -70 mm within that group; removed unused WorkClose binding, preserved reusable close clips.\n"+audit;
        }
        public static string ApplyOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty)throw new InvalidOperationException("Clean saved campus required.");
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var frame=GameObject.Find("Studio contact coordinate frame").transform;var rubric=GameObject.Find("Studio_Rubric").transform;var wording=GameObject.Find("Selected criterion wording plate").transform;
            if(Vector3.Distance(frame.InverseTransformPoint(rubric.position),new Vector3(-.63f,.815f,.49f))>.001f||Vector3.Distance(frame.InverseTransformPoint(wording.position),new Vector3(-.36f,.8165f,.275f))>.001f)throw new InvalidOperationException("Preserve edited layout; this bounded correction expects the recorded baseline.");
            var moved=new List<Transform>{rubric};foreach(var b in d.Bindings.Where(b=>b.Id.StartsWith("Rubric"))){moved.Add(b.Stance);moved.Add(b.CameraPose);if(b.Id.EndsWith("Page"))moved.Add(b.Prop);}var unique=moved.Distinct().ToArray();var delta=frame.TransformVector(new Vector3(.18f,0,0));
            Undo.RecordObjects(unique.Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[]{wording}).ToArray(),"Separate authored folio swept envelopes");foreach(var t in unique){t.position+=delta;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}wording.position+=frame.TransformVector(new Vector3(0,0,-.065f));PrefabUtility.RecordPrefabInstancePropertyModifications(wording);
            var result=Audit();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Saved rubric folio, its stance/leaf/cameras +180 mm along table. Wording plate -65 mm in table depth. Task/work anchors and trajectories preserved.\n"+result;
        }
        public static string Audit()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Saved layout only.");var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var frame=GameObject.Find("Studio contact coordinate frame").transform;var blockers=new List<MeshFilter>();
            foreach(var name in new[]{"Studio_Task","Studio_Rubric","Studio_WorkFolio","Selected criterion wording plate","Selected criterion lane board","Pen rest | angled socket and support"})blockers.AddRange(GameObject.Find(name).GetComponentsInChildren<MeshFilter>(true));
            var lines=new List<string>();int samples=0,overlaps=0;
            foreach(var b in d.Bindings.Where(b=>b.Id.EndsWith("Open")||b.Id.EndsWith("Close")))
            {
                var motion=d.Library.Find(b.MotionId);var cover=b.Prop.GetComponentsInChildren<MeshFilter>().Single();var own=b.Prop.parent;
                for(int i=0;i<=240;i++)
                {
                    float time=motion.Duration*i/240;var key=motion.Sample(time);var matrix=Matrix4x4.TRS(b.Stance.TransformPoint(ContactMotionLibrary.NativePoint(key.Position)),b.Stance.rotation*ContactMotionLibrary.NativeRotation(key.Rotation)*Quaternion.Euler(-90,0,0),b.Prop.lossyScale);var sweep=BoundsFor(cover,frame.worldToLocalMatrix*matrix*b.Prop.worldToLocalMatrix*cover.transform.localToWorldMatrix);samples++;
                    foreach(var other in blockers.Where(x=>!x.transform.IsChildOf(own)))
                    {
                        var obstacle=BoundsFor(other,frame.worldToLocalMatrix*other.transform.localToWorldMatrix);var overlap=Vector3.Min(sweep.max,obstacle.max)-Vector3.Max(sweep.min,obstacle.min);if(overlap.x>.0001f&&overlap.y>.0001f&&overlap.z>.0001f){overlaps++;if(lines.Count<15)lines.Add(b.Id+" time="+time+" overlap "+other.name+" xyz="+overlap.ToString("F6"));}
                    }
                }
            }
            var station=UnityEngine.Object.FindObjectsByType<CampusStation>(FindObjectsSortMode.None).Single(s=>s.StationId=="studio");foreach(var b in d.Bindings.Where(b=>b.Id.StartsWith("Rubric")))if(station.DistanceTo(b.Stance.position)>station.InteractionRange)throw new InvalidOperationException("Shifted contact outside preserved station range.");
            lines.Add((samples/241)+" bound open/close sweeps, "+samples+" transformed mesh-bound samples, neighboring folio/paper/wording/criterion/pen overlaps="+overlaps+" at0.1mm diagnostic tolerance. Conservative AABB separation in common tabletop coordinates.");if(overlaps!=0)throw new InvalidOperationException(string.Join("\n",lines));return string.Join("\n",lines);
        }
        public static string DescribeMarkStudy(float groupOffset,float penOffset)
        {
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var f=GameObject.Find("Studio contact coordinate frame").transform;var o=d.Bindings.Single(x=>x.Id=="WorkOpen");var m=d.Library.Find(o.MotionId);var mf=o.Prop.GetComponentsInChildren<MeshFilter>().First();var mat=f.worldToLocalMatrix*PropMatrix(o,m.Duration)*o.Prop.worldToLocalMatrix*mf.transform.localToWorldMatrix;var bound=BoundsFor(mf,mat);bound.center+=new Vector3(groupOffset,0,0);var lines=new List<string>{"Work open min="+bound.min.ToString("F6")+" max="+bound.max.ToString("F6")+" imported="+o.ImportedNativeGeometry};
            var b=d.Bindings.Single(x=>x.Id=="MarkPoint");var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab"));actor.hideFlags=HideFlags.HideAndDontSave;actor.transform.SetPositionAndRotation(b.Stance.position+f.TransformVector(new Vector3(groupOffset+penOffset,0,0)),b.Stance.rotation);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();try{d.Library.Find(b.MotionId).Approach.SampleAnimation(actor,.45f);skin.BakeMesh(mesh,true);var vs=mesh.vertices.Select(x=>f.InverseTransformPoint(skin.transform.TransformPoint(x))).Where(x=>bound.Contains(x)).ToArray();lines.Add("skin count="+vs.Length+" bounds="+(vs.Length>0?new Vector3(vs.Min(x=>x.x),vs.Min(x=>x.y),vs.Min(x=>x.z)).ToString("F6")+" to "+new Vector3(vs.Max(x=>x.x),vs.Max(x=>x.y),vs.Max(x=>x.z)).ToString("F6"):"none"));}finally{UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(mesh);}return string.Join("\n",lines);
        }
        public static string AuditMovingPen(string output,float groupOffset,float penOffset)
        {
            if(EditorApplication.isPlaying||File.Exists(output))throw new InvalidOperationException("Outside Play and new evidence path required.");
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var f=GameObject.Find("Studio contact coordinate frame").transform;var binding=d.Bindings.Single(x=>x.Id=="MarkPoint");var motion=d.Library.Find(binding.MotionId);var roots=WorkRoots();var rest=GameObject.Find("Pen rest | angled socket and support").transform;var delta=f.TransformVector(new Vector3(groupOffset,0,0));var penDelta=f.TransformVector(new Vector3(penOffset,0,0));
            var obstacles=new List<MeshFilter>();foreach(var name in new[]{"Studio_Task","Studio_Rubric","Studio_WorkFolio","Selected criterion wording plate","Selected criterion lane board","Pen rest | angled socket and support","Studio_Experiment"})obstacles.AddRange(GameObject.Find(name).GetComponentsInChildren<MeshFilter>(true));foreach(var lane in UnityEngine.Object.FindObjectsByType<WorldMarkingLane>(FindObjectsSortMode.None))obstacles.AddRange(lane.GetComponentsInChildren<MeshFilter>(true));
            var sheet=d.Bindings.Single(x=>x.Id=="PlacePassage");obstacles.AddRange(sheet.Prop.GetComponentsInChildren<MeshFilter>(true));obstacles=obstacles.Distinct().ToList();var obstacleMatrices=obstacles.ToDictionary(x=>x,x=>{var world=x.transform.localToWorldMatrix;if(x.transform.IsChildOf(sheet.Prop))world=PropMatrix(sheet,d.Library.Find(sheet.MotionId).Duration)*sheet.Prop.worldToLocalMatrix*world;var shift=(roots.Any(r=>x.transform.IsChildOf(r))?delta:Vector3.zero)+(x.transform.IsChildOf(rest)?penDelta:Vector3.zero);return Matrix4x4.Translate(shift)*world;});var vertices=obstacles.ToDictionary(x=>x,x=>Vertices(x.sharedMesh));var triangles=obstacles.ToDictionary(x=>x,x=>Triangles(x.sharedMesh));var moving=binding.Prop.GetComponentsInChildren<MeshFilter>(true);var lines=new List<string>{"seconds,pen_mesh,obstacle,inside,disagree"};int flags=0,samples=0;
            for(int i=0;i<=Mathf.CeilToInt(motion.Duration*120);i++)
            {
                float t=Mathf.Min(i/120f,motion.Duration);var root=Matrix4x4.Translate(delta+penDelta)*PropMatrix(binding,t);samples++;
                foreach(var mesh in moving){var world=root*binding.Prop.worldToLocalMatrix*mesh.transform.localToWorldMatrix;foreach(var o in obstacles){var matrix=obstacleMatrices[o].inverse*world;var bounds=o.sharedMesh.bounds;bounds.Expand(-.0002f);int inside=0,disagree=0;foreach(var v in Vertices(mesh.sharedMesh)){var p=matrix.MultiplyPoint3x4(v);if(!bounds.Contains(p))continue;int votes=InsideVotes(p,vertices[o],triangles[o]);if(votes==3)inside++;else if(votes>0)disagree++;}if(inside>0||disagree>0){flags++;lines.Add(t+","+mesh.name+","+Hierarchy(o.transform)+","+inside+","+disagree);}}}
            }
            string result="Moving pen: "+samples+" complete prop poses against closed folios, actual placed sheet, board, rest, tabletop/supports and rails; exact volume flags="+flags+" at0.1mm bounds tolerance; group/pen offsets="+groupOffset+"/"+penOffset+" m.";lines.Add(result);Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllLines(output,lines);return result;
        }
        static Vector3[] Vertices(Mesh mesh)
        {using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh)){if(data[0].vertexCount==0)throw new InvalidOperationException("Empty obstacle mesh: "+mesh.name);using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp)){data[0].GetVertices(vertices);return vertices.ToArray();}}}
        static int[] Triangles(Mesh mesh)
        {using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh)){var result=new List<int>();for(int i=0;i<data[0].subMeshCount;i++){if(data[0].GetSubMesh(i).topology!=MeshTopology.Triangles)throw new InvalidOperationException("Non-triangle obstacle: "+mesh.name);using(var indices=new NativeArray<int>(data[0].GetSubMesh(i).indexCount,Allocator.Temp)){data[0].GetIndices(indices,i,true);result.AddRange(indices.ToArray());}}if(result.Count==0)throw new InvalidOperationException("Empty obstacle triangles: "+mesh.name);return result.ToArray();}}
        static Bounds BoundsFor(MeshFilter filter,Matrix4x4 matrix)
        {var vertices=Vertices(filter.sharedMesh);var bounds=new Bounds(matrix.MultiplyPoint3x4(vertices[0]),Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(matrix.MultiplyPoint3x4(v));return bounds;}
        static bool WorkBinding(string id)=>id.StartsWith("Work")||id=="PlacePassage"||id=="EditionSlide"||id=="MarkPoint";
        static Transform[] WorkRoots()=>new[]{"Studio_WorkFolio","Selected criterion wording plate","Selected criterion lane board","Pen rest | angled socket and support","Studio_Pen"}.Select(n=>GameObject.Find(n).transform).Concat(new[]{UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>().Bindings.Single(x=>x.Id=="PlacePassage").Prop}).Distinct().ToArray();
        static Matrix4x4 PropMatrix(ContactBinding binding,float time){var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var key=d.Library.Find(binding.MotionId).Sample(time);return Matrix4x4.TRS(binding.Stance.TransformPoint(ContactMotionLibrary.NativePoint(key.Position)),binding.Stance.rotation*ContactMotionLibrary.NativeRotation(key.Rotation)*(binding.ImportedNativeGeometry?Quaternion.Euler(-90,0,0):Quaternion.identity),binding.Prop.lossyScale);}
        static string Hierarchy(Transform t){return t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;}
        static int InsideVotes(Vector3 point,Vector3[] vertices,int[] triangles)
        {
            int votes=0;foreach(var direction in new[]{new Vector3(1,.371f,.193f).normalized,new Vector3(-.227f,1,.419f).normalized,new Vector3(.313f,-.197f,1).normalized})
            {
                var hits=new List<float>();for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var e1=vertices[triangles[i+1]]-a;var e2=vertices[triangles[i+2]]-a;var h=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-10f)continue;float inv=1/det;var s=point-a;float u=inv*Vector3.Dot(s,h);if(u<0||u>1)continue;var q=Vector3.Cross(s,e1);float v=inv*Vector3.Dot(direction,q);if(v<0||u+v>1)continue;float distance=inv*Vector3.Dot(e2,q);if(distance>1e-7f&&!hits.Any(x=>Mathf.Abs(x-distance)<1e-6f))hits.Add(distance);}if(hits.Count%2==1)votes++;
            }
            return votes;
        }
        public static string AuditNeighborSkin(string bindingId,string output,float tableDepthOffset=0,float tableLateralOffset=0,float workGroupOffset=0,bool exactVolumes=false,float penOffset=0,bool closeWorkBeforeMark=false,bool sheetPlaced=false)
        {
            if(EditorApplication.isPlaying||File.Exists(output))throw new InvalidOperationException("Outside Play and new evidence path required.");
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var binding=d.Bindings.Single(b=>b.Id==bindingId);var motion=d.Library.Find(binding.MotionId);var blockers=new List<MeshFilter>();
            foreach(var name in new[]{"Studio_Task","Studio_Rubric","Studio_WorkFolio","Selected criterion wording plate","Selected criterion lane board","Pen rest | angled socket and support","Studio_Pen"})blockers.AddRange(GameObject.Find(name).GetComponentsInChildren<MeshFilter>(true));
            foreach(var lane in UnityEngine.Object.FindObjectsByType<WorldMarkingLane>(FindObjectsSortMode.None))blockers.AddRange(lane.GetComponentsInChildren<MeshFilter>(true));
            var own=binding.Id.EndsWith("Open")||binding.Id.EndsWith("Close")?binding.Prop.parent:binding.Prop;blockers.RemoveAll(m=>m.transform.IsChildOf(own));var group=WorkRoots();var frame=GameObject.Find("Studio contact coordinate frame").transform;var groupDelta=frame.TransformVector(new Vector3(workGroupOffset,0,0));
            var penRoots=new[]{GameObject.Find("Studio_Pen").transform,GameObject.Find("Pen rest | angled socket and support").transform};var penDelta=frame.TransformVector(new Vector3(penOffset,0,0));
            var settled=new Dictionary<Transform,Matrix4x4>();
            if(bindingId=="MarkPoint"||bindingId=="PlacePassage"||bindingId=="EditionSlide"||sheetPlaced){var open=d.Bindings.Single(x=>x.Id=="WorkOpen");if(bindingId!="WorkOpen"&&!(bindingId=="MarkPoint"&&closeWorkBeforeMark))settled[open.Prop]=PropMatrix(open,d.Library.Find(open.MotionId).Duration);if(bindingId=="MarkPoint"||sheetPlaced){var sheet=d.Bindings.Single(x=>x.Id=="PlacePassage");settled[sheet.Prop]=PropMatrix(sheet,d.Library.Find(sheet.MotionId).Duration);blockers.AddRange(sheet.Prop.GetComponentsInChildren<MeshFilter>(true));}}
            var obstacleVertices=blockers.ToDictionary(b=>b,b=>Vertices(b.sharedMesh));var obstacleTriangles=blockers.ToDictionary(b=>b,b=>Triangles(b.sharedMesh));
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab"));actor.hideFlags=HideFlags.HideAndDontSave;actor.transform.SetPositionAndRotation(binding.Stance.position+frame.TransformVector(new Vector3(tableLateralOffset,0,tableDepthOffset))+(WorkBinding(bindingId)?groupDelta:Vector3.zero)+(bindingId=="MarkPoint"?penDelta:Vector3.zero),binding.Stance.rotation);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();var lines=new List<string>{"phase,seconds,obstacle,inside_aabb_vertices,inside_exact_vertices,ray_disagreements"};int samples=0,flags=0;
            try
            {
                foreach(var clip in new[]{motion.Approach,motion.Contact,motion.Recovery})for(int step=0;step<=Mathf.CeilToInt(clip.length*120);step++)
                {
                    float t=Mathf.Min(step/120f,clip.length);clip.SampleAnimation(actor,t);skin.BakeMesh(baked,true);var vertices=baked.vertices;samples++;
                    foreach(var obstacle in blockers){var bounds=obstacle.sharedMesh.bounds;bounds.Expand(-.0002f);var shift=(group.Any(r=>obstacle.transform.IsChildOf(r))?groupDelta:Vector3.zero)+(penRoots.Any(r=>obstacle.transform.IsChildOf(r))?penDelta:Vector3.zero);var obstacleMatrix=obstacle.transform.localToWorldMatrix;foreach(var pair in settled)if(obstacle.transform.IsChildOf(pair.Key))obstacleMatrix=pair.Value*pair.Key.worldToLocalMatrix*obstacle.transform.localToWorldMatrix;var matrix=obstacleMatrix.inverse*Matrix4x4.Translate(-shift)*skin.transform.localToWorldMatrix;int count=0,inside=0,disagree=0;foreach(var v in vertices){var p=matrix.MultiplyPoint3x4(v);if(!bounds.Contains(p))continue;count++;if(exactVolumes){int votes=InsideVotes(p,obstacleVertices[obstacle],obstacleTriangles[obstacle]);if(votes==3)inside++;else if(votes>0)disagree++;}}if(count>0){if(!exactVolumes||inside>0||disagree>0)flags++;lines.Add(clip.name+","+t+","+Hierarchy(obstacle.transform)+","+count+","+inside+","+disagree);}}
                }
                string result=bindingId+": "+samples+" actual skinned poses against neighboring static props; table depth/lateral/work-group study offset="+tableDepthOffset+"/"+tableLateralOffset+"/"+workGroupOffset+" m; pen offset="+penOffset+" m; Work closed before mark="+closeWorkBeforeMark+", flagged pose/obstacle pairs="+flags+" at0.1mm inward bounds tolerance. Exact three-ray triangle volumes="+exactVolumes+". Positive boxes without volume checks require exact-geometry review.";lines.Add(result);Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllLines(output,lines);return result;
            }
            finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(actor);}
        }
    }
}
