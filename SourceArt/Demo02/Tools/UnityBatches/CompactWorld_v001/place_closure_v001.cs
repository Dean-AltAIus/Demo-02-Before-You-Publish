if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new System.Exception("Preserve authoring");
if(UnityEngine.GameObject.Find("Compact courtyard closure | editable v001"))throw new System.Exception("Already placed; preserve edits");
var studio=UnityEngine.GameObject.Find("Studio_Entrance");var review=UnityEngine.GameObject.Find("Review_Entrance");var campus=UnityEngine.GameObject.Find("Campus | saved 24 x 18 metres");
if(!studio||!review||!campus)throw new System.Exception("Missing authored assembly");
string folder="Assets/Demo02/Prefabs/Environment/CourtyardClosure_v001/";
var assets=new System.Collections.Generic.Dictionary<string,UnityEngine.GameObject>();
foreach(var key in new string[]{"GardenWall_2m","GardenCornerPost","RoomRoof_6x4m","FrontWindowBay_1m","CourtyardGate_4m"}){var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+key+".prefab");if(!asset)throw new System.Exception("Missing "+key);assets.Add(key,asset);}
var previous=new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach(var t in campus.GetComponentsInChildren<UnityEngine.Transform>())if(t.name=="Campus perimeter collision"||t.name=="Studio_WindowLeft"||t.name=="Review_WindowLeft")previous.Add(t.gameObject);
if(previous.Count!=6)throw new System.Exception("Unexpected old boundary/window count "+previous.Count);
var parent=new UnityEngine.GameObject("Compact courtyard closure | editable v001");parent.transform.SetParent(campus.transform,false);int placed=0;
System.Action<string,string,UnityEngine.Vector3,UnityEngine.Quaternion,UnityEngine.Transform> place=(key,name,pos,rot,p)=>{var go=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(assets[key],scene);go.name=name;go.transform.SetParent(p,true);go.transform.SetPositionAndRotation(pos,rot);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);placed++;};
var baseRotation=UnityEngine.Quaternion.Euler(-90,0,0);
for(int x=-11;x<=11;x+=2){place("GardenWall_2m","North garden wall "+x,new UnityEngine.Vector3(x,0,-9.15f),baseRotation,parent.transform);if(x< -2||x>2)place("GardenWall_2m","South garden wall "+x,new UnityEngine.Vector3(x,0,9.15f),baseRotation,parent.transform);}
for(int z=-8;z<=8;z+=2)foreach(int sign in new int[]{-1,1})place("GardenWall_2m",(sign<0?"West":"East")+" garden wall "+z,new UnityEngine.Vector3(sign*12.15f,0,z),UnityEngine.Quaternion.Euler(-90,90,0),parent.transform);
foreach(int x in new int[]{-1,1})foreach(int z in new int[]{-1,1})place("GardenCornerPost","Garden corner "+x+" "+z,new UnityEngine.Vector3(x*12.15f,0,z*9.15f),baseRotation,parent.transform);
place("CourtyardGate_4m","Courtyard entrance gate",new UnityEngine.Vector3(0,0,9.15f),UnityEngine.Quaternion.Euler(-90,180,0),parent.transform);
foreach(var entry in new UnityEngine.GameObject[]{studio,review}){
 var center=entry.transform.TransformPoint(new UnityEngine.Vector3(0,2,0));
 place("RoomRoof_6x4m",entry.name+" | closed roof",center,entry.transform.rotation,entry.transform.parent);
 foreach(int side in new int[]{-1,1})place("FrontWindowBay_1m",entry.name+" | front window bay "+side,entry.transform.TransformPoint(new UnityEngine.Vector3(side*2.5f,0,0)),entry.transform.rotation,entry.transform.parent);
}
foreach(var old in previous){UnityEditor.Undo.RecordObject(old,"Preserve superseded architecture inactive");old.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(old);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEngine.Physics.SyncTransforms();
return "Saved "+placed+" reusable architectural pieces. Six superseded windows/boundaries preserved inactive. Functional anchors unchanged.";
