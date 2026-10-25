if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new System.Exception("Preserve current scene");
string oldKit="Assets/Demo02/Prefabs/Environment/Kit_v001/",masonry="Assets/Demo02/Prefabs/Environment/MasonryKit_v003/";
var expected=new System.Collections.Generic.Dictionary<string,int>{{"CourtyardPavers_2m",108},{"StoneWall_2m",17},{"Planter",12},{"Awning_4m",1}};
var actual=new System.Collections.Generic.Dictionary<string,int>();
var swaps=new System.Collections.Generic.List<UnityEngine.Transform>();
var crowns=new System.Collections.Generic.List<UnityEngine.GameObject>();
var report=new System.Text.StringBuilder();
int renderersBefore=0;
foreach(var root in scene.GetRootGameObjects()){
 renderersBefore+=root.GetComponentsInChildren<UnityEngine.Renderer>().Length;
 foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>(true)){
  if(!t.gameObject.activeInHierarchy||!UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))continue;
  string path=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
  if(path.StartsWith(masonry)||path.StartsWith("Assets/Demo02/Prefabs/Environment/FullPlanter_v004/"))throw new System.Exception("Batch already placed, preserve edits");
  if(path=="Assets/Demo02/Prefabs/Environment/FoliageKit_v002/PlanterCrown.prefab"){crowns.Add(t.gameObject);continue;}
  if(!path.StartsWith(oldKit))continue;
  string kind=System.IO.Path.GetFileNameWithoutExtension(path);if(!expected.ContainsKey(kind))continue;
  if(t.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true).Length!=0)throw new System.Exception("Preserve new behavior on "+t.name);
  foreach(var child in t.GetComponentsInChildren<UnityEngine.Transform>(true)){
   if(child==t)continue;
   var source=UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(child);
   if(!source||UnityEngine.Vector3.Distance(source.localPosition,child.localPosition)>.0001f||UnityEngine.Quaternion.Angle(source.localRotation,child.localRotation)>.01f||UnityEngine.Vector3.Distance(source.localScale,child.localScale)>.0001f)throw new System.Exception("Preserve custom child geometry "+t.name+"/"+child.name);
  }
  foreach(var rr in t.GetComponentsInChildren<UnityEngine.Renderer>())foreach(var mat in rr.sharedMaterials){
   string mp=UnityEditor.AssetDatabase.GetAssetPath(mat);
   if(!mp.StartsWith("Assets/Demo02/Materials/CampusKit_v001/")&&!mp.StartsWith("Assets/Demo02/Materials/CampusPalette_v002/"))throw new System.Exception("Preserve custom material "+t.name+" "+mp);
  }
  foreach(var mf in t.GetComponentsInChildren<UnityEngine.MeshFilter>()){
   var source=UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(mf);
   if(!source||source.sharedMesh!=mf.sharedMesh)throw new System.Exception("Preserve custom mesh "+t.name);
  }
  actual[kind]=actual.ContainsKey(kind)?actual[kind]+1:1;swaps.Add(t);
 }
}
foreach(var pair in expected)if(!actual.ContainsKey(pair.Key)||actual[pair.Key]!=pair.Value)throw new System.Exception("Unexpected count "+pair.Key);
if(crowns.Count!=12)throw new System.Exception("Unexpected additive planter crowns "+crowns.Count);
foreach(var name in new[]{"PavingA_2m","PavingB_2m","PavingC_2m","StoneWall_2m","Awning_4m"})if(!UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(masonry+name+".prefab"))throw new System.Exception("Missing "+name);
string planter="Assets/Demo02/Prefabs/Environment/FullPlanter_v004/FullPlanter.prefab";
if(!UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(planter))throw new System.Exception("Missing planter");
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Reference campus bulk v006");
try{
 foreach(var old in swaps){
  string kind=System.IO.Path.GetFileNameWithoutExtension(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old.gameObject));
  string path=kind=="Planter"?planter:masonry+kind+".prefab";
  if(kind=="CourtyardPavers_2m"){
   var parts=old.name.Split('_');int x=int.Parse(parts[1]),z=int.Parse(parts[2]);
   path=masonry+new[]{"PavingA_2m","PavingB_2m","PavingC_2m"}[(x+2*z)%3]+".prefab";
  }
  var fresh=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path),scene);
  UnityEditor.Undo.RegisterCreatedObjectUndo(fresh,"Place revised campus asset");
  fresh.name=old.name+" | reference bulk v006";fresh.transform.SetParent(old.parent,false);fresh.transform.localPosition=old.localPosition;fresh.transform.localRotation=old.localRotation;fresh.transform.localScale=old.localScale;
  UnityEditor.Undo.RecordObject(old.gameObject,"Preserve earlier campus asset inactive");old.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
  report.AppendLine(old.name+" -> "+path+" position="+fresh.transform.position.ToString("F3"));
 }
 foreach(var crown in crowns){UnityEditor.Undo.RecordObject(crown,"Preserve earlier additive crown inactive");crown.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(crown);}
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.Undo.CollapseUndoOperations(group);
}catch{UnityEditor.Undo.RevertAllDownToGroup(group);throw;}
int renderersAfter=0;foreach(var root in scene.GetRootGameObjects())renderersAfter+=root.GetComponentsInChildren<UnityEngine.Renderer>().Length;
string summary="Replaced="+swaps.Count+" preservedInactiveCrowns="+crowns.Count+" activeRenderersBefore="+renderersBefore+" after="+renderersAfter;
System.IO.File.WriteAllText("LocalOnly/Validation/Production/campus-bulk-v006-placement.txt",summary+"\n"+report);
return summary;
