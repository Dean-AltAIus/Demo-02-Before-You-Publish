if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new System.Exception("Preserve unsaved scene");
string treePath="Assets/Demo02/Prefabs/Environment/FoliageKit_v002/FullTree.prefab";
var tree=UnityEditor.PrefabUtility.LoadPrefabContents(treePath);
try{
 if(tree.transform.Find("Canopy camera envelope"))throw new System.Exception("Preserve existing envelope");
 var child=new UnityEngine.GameObject("Canopy camera envelope");child.transform.SetParent(tree.transform,false);var collider=child.AddComponent<UnityEngine.BoxCollider>();collider.center=new UnityEngine.Vector3(-.04f,.01f,3.31f);collider.size=new UnityEngine.Vector3(3.26f,3.34f,2.28f);
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(tree,treePath);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(tree);}
// The existing reverse wall surface is a mortar core. Give that exposed solid surface
// an intentional plaster finish, leaving limestone, cornice and geometry intact.
string wallPath="Assets/Demo02/Prefabs/Environment/MasonryKit_v003/StoneWall_2m.prefab";
var wall=UnityEditor.PrefabUtility.LoadPrefabContents(wallPath);int changed=0;
try{
 var plaster=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Demo02/Materials/CourtyardClosure_v001/D02_ClosurePlaster.mat");if(!plaster)throw new System.Exception("Missing plaster");
 foreach(var renderer in wall.GetComponentsInChildren<UnityEngine.Renderer>()){
  var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i].name=="D02_MasonryMortar"){mats[i]=plaster;changed++;}renderer.sharedMaterials=mats;
 }
 if(changed!=1)throw new System.Exception("Unexpected wall material mapping "+changed);
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(wall,wallPath);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(wall);}
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return "Saved camera-safe canopy envelope and intentional reverse-wall plaster finish.";
