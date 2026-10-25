if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new System.Exception("Preserve current authoring");
string materialPath="Assets/Demo02/Materials/WorldTextDepth.mat";
if(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath))throw new System.Exception("Already bound; preserve material edits");
var shader=UnityEngine.Shader.Find("Altaius/Demo02/World Text Depth");
if(!shader||UnityEditor.ShaderUtil.ShaderHasError(shader))throw new System.Exception("World label shader is not ready");
var material=new UnityEngine.Material(shader);material.name="WorldTextDepth";UnityEditor.AssetDatabase.CreateAsset(material,materialPath);
int labels=0;
foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<UnityEngine.TextMesh>(true)){
 var renderer=text.GetComponent<UnityEngine.MeshRenderer>();if(!renderer)throw new System.Exception("Missing text renderer");
 UnityEditor.Undo.RecordObject(renderer,"Depth-tested authored world label");renderer.sharedMaterial=material;
 var binding=text.GetComponent<Altaius.Demo02.Gameplay.WorldTextAtlasBinding>();if(!binding)binding=UnityEditor.Undo.AddComponent<Altaius.Demo02.Gameplay.WorldTextAtlasBinding>(text.gameObject);binding.RefreshAtlas();
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);labels++;
}
string lanePath="Assets/Demo02/Prefabs/Gameplay/MarkingLane.prefab";var lane=UnityEditor.PrefabUtility.LoadPrefabContents(lanePath);int reusable=0;
try{foreach(var text in lane.GetComponentsInChildren<UnityEngine.TextMesh>(true)){text.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial=material;var binding=text.GetComponent<Altaius.Demo02.Gameplay.WorldTextAtlasBinding>();if(!binding)binding=text.gameObject.AddComponent<Altaius.Demo02.Gameplay.WorldTextAtlasBinding>();binding.RefreshAtlas();reusable++;}UnityEditor.PrefabUtility.SaveAsPrefabAsset(lane,lanePath);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(lane);}
string bannerPath="Assets/Demo02/Prefabs/Environment/AcademicProps_v001/CampusBanner.prefab";var banner=UnityEditor.PrefabUtility.LoadPrefabContents(bannerPath);
try{if(banner.GetComponent<UnityEngine.Collider>())throw new System.Exception("Preserve banner collider");var box=banner.AddComponent<UnityEngine.BoxCollider>();box.center=new UnityEngine.Vector3(0,-.0115f,.9589f);box.size=new UnityEngine.Vector3(.68f,.085f,.8782f);UnityEditor.PrefabUtility.SaveAsPrefabAsset(banner,bannerPath);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(banner);}
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Saved depth-tested world labels="+labels+", reusable lane labels="+reusable+", banner camera collider. Native UI untouched.";
