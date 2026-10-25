if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new System.Exception("Preserve scene");
string art="Assets/Demo02/Art/CourtyardClosure_v001", mats="Assets/Demo02/Materials/CourtyardClosure_v001", prefabs="Assets/Demo02/Prefabs/Environment/CourtyardClosure_v001";
foreach(var p in new string[]{art,mats,prefabs}){if(System.IO.Directory.Exists(p)||System.IO.File.Exists(p+".meta"))throw new System.Exception("Preserve existing "+p);}
foreach(var p in new string[]{art,mats,prefabs})System.IO.Directory.CreateDirectory(p);
foreach(var f in System.IO.Directory.GetFiles("SourceArt/Demo02/Exports/CourtyardClosure_v001","*.fbx"))System.IO.File.Copy(f,art+"/"+System.IO.Path.GetFileName(f));
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var shader=UnityEngine.Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new System.Exception("URP missing");
var materials=new System.Collections.Generic.Dictionary<string,UnityEngine.Material>();
materials.Add("D02_MasonryTrim",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Demo02/Materials/MasonryKit_v003/D02_MasonryTrim.mat"));
materials.Add("D02_LimestoneBaked",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Demo02/Materials/MasonryKit_v003/D02_LimestoneBaked.mat"));
materials.Add("D02_MasonryMortar",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Demo02/Materials/MasonryKit_v003/D02_MasonryMortar.mat"));
{var m=new UnityEngine.Material(shader);m.name="D02_ClosureSlate";m.SetColor("_BaseColor",new UnityEngine.Color(0.04500000f,0.11000000f,0.17000000f,1.00000000f).gamma);m.SetFloat("_Smoothness",0.31999999f);m.SetFloat("_Metallic",0.00000000f);UnityEditor.AssetDatabase.CreateAsset(m,mats+"/D02_ClosureSlate.mat");materials.Add("D02_ClosureSlate",m);}
{var m=new UnityEngine.Material(shader);m.name="D02_ClosurePlaster";m.SetColor("_BaseColor",new UnityEngine.Color(0.75999999f,0.68000001f,0.52999997f,1.00000000f).gamma);m.SetFloat("_Smoothness",0.14999998f);m.SetFloat("_Metallic",0.00000000f);UnityEditor.AssetDatabase.CreateAsset(m,mats+"/D02_ClosurePlaster.mat");materials.Add("D02_ClosurePlaster",m);}
{var m=new UnityEngine.Material(shader);m.name="D02_ClosureIron";m.SetColor("_BaseColor",new UnityEngine.Color(0.02400000f,0.04600000f,0.06000000f,1.00000000f).gamma);m.SetFloat("_Smoothness",0.55000001f);m.SetFloat("_Metallic",0.25000000f);UnityEditor.AssetDatabase.CreateAsset(m,mats+"/D02_ClosureIron.mat");materials.Add("D02_ClosureIron",m);}
{var m=new UnityEngine.Material(shader);m.name="D02_ClosureGlass";m.SetColor("_BaseColor",new UnityEngine.Color(0.12000000f,0.28000000f,0.36000001f,1.00000000f).gamma);m.SetFloat("_Smoothness",0.68000001f);m.SetFloat("_Metallic",0.00000000f);UnityEditor.AssetDatabase.CreateAsset(m,mats+"/D02_ClosureGlass.mat");materials.Add("D02_ClosureGlass",m);}
{var m=new UnityEngine.Material(shader);m.name="D02_ClosureGate";m.SetColor("_BaseColor",new UnityEngine.Color(0.01800000f,0.22000000f,0.23500000f,1.00000000f).gamma);m.SetFloat("_Smoothness",0.37000000f);m.SetFloat("_Metallic",0.00000000f);UnityEditor.AssetDatabase.CreateAsset(m,mats+"/D02_ClosureGate.mat");materials.Add("D02_ClosureGate",m);}
{var m=new UnityEngine.Material(shader);m.name="D02_ClosureBrass";m.SetColor("_BaseColor",new UnityEngine.Color(0.49000001f,0.30000001f,0.07000000f,1.00000000f).gamma);m.SetFloat("_Smoothness",0.59999999f);m.SetFloat("_Metallic",0.55000001f);UnityEditor.AssetDatabase.CreateAsset(m,mats+"/D02_ClosureBrass.mat");materials.Add("D02_ClosureBrass",m);}
var report=new System.Text.StringBuilder();
foreach(var file in System.IO.Directory.GetFiles(art,"*.fbx")){
 var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(file);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
 var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();UnityEngine.GameObject root=null;
 try{
 root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(file),preview);root.name=System.IO.Path.GetFileNameWithoutExtension(file);root.transform.position=UnityEngine.Vector3.zero;
 foreach(var renderer in root.GetComponentsInChildren<UnityEngine.Renderer>()){var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++){if(!materials.ContainsKey(slots[i].name)||!materials[slots[i].name])throw new System.Exception("Unmapped "+slots[i].name);slots[i]=materials[slots[i].name];}renderer.sharedMaterials=slots;}
 if(root.name=="RoomRoof_6x4m"){foreach(var mf in root.GetComponentsInChildren<UnityEngine.MeshFilter>())mf.gameObject.AddComponent<UnityEngine.MeshCollider>().sharedMesh=mf.sharedMesh;}
 else{var box=root.AddComponent<UnityEngine.BoxCollider>();
 if(root.name=="GardenWall_2m"){box.center=new UnityEngine.Vector3(0,0,1.1925f);box.size=new UnityEngine.Vector3(2,.36f,2.385f);}
 if(root.name=="GardenCornerPost"){box.center=new UnityEngine.Vector3(0,0,1.30f);box.size=new UnityEngine.Vector3(.5f,.5f,2.6f);}
 if(root.name=="FrontWindowBay_1m"){box.center=new UnityEngine.Vector3(0,0,1.8f);box.size=new UnityEngine.Vector3(1,.36f,3.6f);}
 if(root.name=="CourtyardGate_4m"){box.center=new UnityEngine.Vector3(0,0,1.68f);box.size=new UnityEngine.Vector3(4.44f,.5f,3.36f);}}
 var rs=root.GetComponentsInChildren<UnityEngine.Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);report.AppendLine(root.name+" size="+bounds.size.ToString("F4")+" rotation="+root.transform.eulerAngles+" renderers="+rs.Length);
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,prefabs+"/"+root.name+".prefab");
 }finally{if(root)UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
}
UnityEditor.AssetDatabase.SaveAssets();System.IO.File.WriteAllText("LocalOnly/Validation/Production/World360/closure-import.txt",report.ToString());return report.ToString();
