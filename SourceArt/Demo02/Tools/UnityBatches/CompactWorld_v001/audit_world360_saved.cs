if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new System.Exception("Preserve scene");
var report=new System.Text.StringBuilder("Saved compact courtyard audit. Static and collision evidence only, not frame-rate or live gameplay acceptance.\n");
int objects=0,renderers=0,skinned=0,missing=0,labels=0;long triangles=0;int slots=0;
foreach(var root in scene.GetRootGameObjects()){
 foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>(true)){objects++;missing+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);}
 foreach(var r in root.GetComponentsInChildren<UnityEngine.Renderer>()){renderers++;slots+=r.sharedMaterials.Length;var skin=r as UnityEngine.SkinnedMeshRenderer;var filter=r.GetComponent<UnityEngine.MeshFilter>();var mesh=skin?skin.sharedMesh:(filter?filter.sharedMesh:null);if(skin)skinned++;if(mesh)for(int i=0;i<mesh.subMeshCount;i++)triangles+=(long)mesh.GetIndexCount(i)/3;}
 foreach(var label in root.GetComponentsInChildren<UnityEngine.TextMesh>()){var renderer=label.GetComponent<UnityEngine.Renderer>();if(renderer.sharedMaterial.shader.name!="Altaius/Demo02/World Text Depth"||!label.GetComponent<Altaius.Demo02.Gameplay.WorldTextAtlasBinding>())throw new System.Exception("Unbound label "+label.name);labels++;}
}
report.AppendLine("roots="+scene.rootCount+" objects="+objects+" activeRenderers="+renderers+" activeSkinned="+skinned+" activeMeshTriangles="+triangles+" materialSlots="+slots+" missingScripts="+missing+" depthLabels="+labels);
UnityEngine.Physics.SyncTransforms();int blocked=0;
foreach(var st in UnityEngine.Object.FindObjectsByType<Altaius.Demo02.Gameplay.CampusStation>(UnityEngine.FindObjectsSortMode.None)){var p=st.Arrival.position;var hits=UnityEngine.Physics.OverlapCapsule(p+UnityEngine.Vector3.up*.30f,p+UnityEngine.Vector3.up*1.38f,.27f,1,UnityEngine.QueryTriggerInteraction.Ignore);blocked+=hits.Length;report.AppendLine(st.StationId+" arrivalBlockers="+hits.Length+" horizontalDistance="+st.DistanceTo(p).ToString("F3"));}
// Dense outward rays prove the saved visible boundary has continuous solid coverage.
int rays=0,misses=0;foreach(float height in new float[]{.3f,1.1f,2f}){
 for(float x=-11.8f;x<=11.8f;x+=.1f)foreach(int side in new int[]{-1,1}){rays++;if(!UnityEngine.Physics.Raycast(new UnityEngine.Vector3(x,height,side*8.7f),UnityEngine.Vector3.forward*side,1.1f,1,UnityEngine.QueryTriggerInteraction.Ignore))misses++;}
 for(float z=-8.8f;z<=8.8f;z+=.1f)foreach(int side in new int[]{-1,1}){rays++;if(!UnityEngine.Physics.Raycast(new UnityEngine.Vector3(side*11.7f,height,z),UnityEngine.Vector3.right*side,1.1f,1,UnityEngine.QueryTriggerInteraction.Ignore))misses++;}
}
report.AppendLine("perimeterRays="+rays+" misses="+misses+" arrivalBlockers="+blocked);
if(missing!=0||misses!=0||blocked!=0)throw new System.Exception(report.ToString());
System.IO.File.WriteAllText("LocalOnly/Validation/Production/World360/saved-audit.txt",report.ToString());return report.ToString();
