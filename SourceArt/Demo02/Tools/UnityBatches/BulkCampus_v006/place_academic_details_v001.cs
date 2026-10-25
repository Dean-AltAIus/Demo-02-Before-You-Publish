if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new System.Exception("Preserve current scene");
foreach(var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))if(t.name.EndsWith(" | academic detail v001"))throw new System.Exception("Academic details already placed");
var anchors=new System.Collections.Generic.Dictionary<string,UnityEngine.Transform>();
foreach(string name in new[]{"Studio_Experiment","Studio_RevisionDesk","Review_Desk","Studio_Shelves","Review_Shelves","Archive_Shelf","Source archive cabinet | editable carrier","Lantern_-4.5_-4.4","Lantern_4.5_-4.4","Lantern_-4.5_6.3","Lantern_4.5_6.3"}){
 var go=UnityEngine.GameObject.Find(name);if(!go)throw new System.Exception("Missing anchor "+name);anchors.Add(name,go.transform);
}
var cabinet=anchors["Source archive cabinet | editable carrier"].Find("ArchiveCabinet");if(!cabinet)throw new System.Exception("Missing native archive cabinet basis");anchors.Add("ArchiveNative",cabinet);
var assets=new System.Collections.Generic.Dictionary<string,UnityEngine.GameObject>();
foreach(string name in new[]{"PottedPlant","DeskLamp","ReferenceBooks","CampusBanner"}){var go=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Demo02/Prefabs/Environment/AcademicProps_v001/"+name+".prefab");if(!go)throw new System.Exception("Missing prop "+name);assets.Add(name,go);}
var report=new System.Text.StringBuilder();int count=0;
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Reference academic details v001");
try{
 System.Action<string,string,UnityEngine.Vector3,float> place=(anchor,kind,local,scale)=>{
  var parent=anchors[anchor];var go=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(assets[kind],parent);
  UnityEditor.Undo.RegisterCreatedObjectUndo(go,"Place editable academic detail");go.name=anchor+" "+kind+" | academic detail v001";
  go.transform.localPosition=local;go.transform.localRotation=UnityEngine.Quaternion.identity;go.transform.localScale=UnityEngine.Vector3.one*scale;
  report.AppendLine(go.name+" world="+go.transform.position.ToString("F3")+" scale="+scale);count++;
 };
 place("Studio_RevisionDesk","PottedPlant",new UnityEngine.Vector3(-.55f,.18f,.813f),.65f);
 place("Studio_RevisionDesk","DeskLamp",new UnityEngine.Vector3(.58f,.18f,.813f),1f);
 place("Studio_RevisionDesk","ReferenceBooks",new UnityEngine.Vector3(.34f,-.18f,.813f),1f);
 place("Studio_Experiment","PottedPlant",new UnityEngine.Vector3(-.95f,.35f,.813f),.48f);
 place("Studio_Experiment","DeskLamp",new UnityEngine.Vector3(.95f,.35f,.813f),1f);
 place("Review_Desk","PottedPlant",new UnityEngine.Vector3(.70f,.23f,.813f),.45f);
 place("Review_Desk","DeskLamp",new UnityEngine.Vector3(-.70f,.23f,.813f),1f);
 place("Review_Desk","ReferenceBooks",new UnityEngine.Vector3(-.57f,-.18f,.813f),1f);
 place("ArchiveNative","PottedPlant",new UnityEngine.Vector3(-.70f,.13f,.878f),.6f);
 place("ArchiveNative","DeskLamp",new UnityEngine.Vector3(.70f,.13f,.878f),1f);
 place("ArchiveNative","ReferenceBooks",new UnityEngine.Vector3(.33f,.07f,.878f),1f);
 foreach(string shelf in new[]{"Studio_Shelves","Review_Shelves","Archive_Shelf"})place(shelf,"PottedPlant",new UnityEngine.Vector3(.35f,0,2.105f),.65f);
 foreach(string lantern in new[]{"Lantern_-4.5_-4.4","Lantern_4.5_-4.4","Lantern_-4.5_6.3","Lantern_4.5_6.3"})place(lantern,"CampusBanner",new UnityEngine.Vector3(.38f,0,.95f),1f);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.Undo.CollapseUndoOperations(group);
}catch{UnityEditor.Undo.RevertAllDownToGroup(group);throw;}
System.IO.File.WriteAllText("LocalOnly/Validation/Production/academic-details-v001-placement.txt","Saved instances="+count+"\n"+report);return "Saved editable academic details="+count;
