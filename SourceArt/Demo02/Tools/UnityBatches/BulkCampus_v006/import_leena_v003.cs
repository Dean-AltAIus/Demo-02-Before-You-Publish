if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new System.Exception("Preserve current scene");
string source="SourceArt/Demo02/Characters/Leena/Demo01_Mechanical_v003/",art="Assets/Demo02/Art/LeenaMechanical_v003/",mats="Assets/Demo02/Materials/LeenaMechanical_v003/",prefabs="Assets/Demo02/Prefabs/LeenaMechanical_v003/";
foreach(var folder in new[]{art,mats,prefabs})if(System.IO.Directory.Exists(folder)||System.IO.File.Exists(folder.TrimEnd('/')+".meta"))throw new System.Exception("Preserve existing Leena asset "+folder);
foreach(var file in new[]{"LEENA_MECHANICAL_v003.fbx","LEENA_Texture_0.png"})if(!System.IO.File.Exists(source+file))throw new System.Exception("Missing native export "+file);
foreach(var folder in new[]{art,mats,prefabs})System.IO.Directory.CreateDirectory(folder);
foreach(var file in new[]{"LEENA_MECHANICAL_v003.fbx","LEENA_Texture_0.png"})System.IO.File.Copy(source+file,art+file);
UnityEditor.AssetDatabase.Refresh();
var texture=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(art+"LEENA_Texture_0.png");texture.textureType=UnityEditor.TextureImporterType.Default;texture.sRGBTexture=true;texture.maxTextureSize=2048;texture.mipmapEnabled=true;texture.wrapMode=UnityEngine.TextureWrapMode.Clamp;texture.filterMode=UnityEngine.FilterMode.Trilinear;texture.SaveAndReimport();
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(art+"LEENA_MECHANICAL_v003.fbx");importer.importCameras=false;importer.importLights=false;importer.importAnimation=true;importer.materialImportMode=UnityEditor.ModelImporterMaterialImportMode.None;importer.animationType=UnityEditor.ModelImporterAnimationType.Generic;importer.avatarSetup=UnityEditor.ModelImporterAvatarSetup.CreateFromThisModel;importer.optimizeGameObjects=false;importer.SaveAndReimport();
var settings=importer.defaultClipAnimations;if(settings.Length!=3)throw new System.Exception("Expected three native attention takes, got "+settings.Length);
foreach(var clip in settings){clip.loopTime=true;clip.keepOriginalPositionY=true;}importer.clipAnimations=settings;importer.SaveAndReimport();
var material=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));material.name="Leena canonical shell";material.SetColor("_BaseColor",UnityEngine.Color.white);material.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(art+"LEENA_Texture_0.png"));material.SetFloat("_Smoothness",.22f);UnityEditor.AssetDatabase.CreateAsset(material,mats+"LeenaCanonicalShell.mat");
var controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(art+"LeenaAttention.controller");
var machine=controller.layers[0].stateMachine;var report=new System.Text.StringBuilder();int clipCount=0;
foreach(var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(art+"LEENA_MECHANICAL_v003.fbx")){
 var clip=asset as UnityEngine.AnimationClip;if(!clip||clip.name.StartsWith("__preview__"))continue;
 var state=machine.AddState(clip.name);state.motion=clip;if(clip.name.Contains("PresenceAttention"))machine.defaultState=state;clipCount++;report.AppendLine("Clip="+clip.name+" seconds="+clip.length.ToString("F3")+" fps="+clip.frameRate);
}
if(clipCount!=3)throw new System.Exception("Unexpected imported clips "+clipCount);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();UnityEngine.GameObject root=null;
try{
 root=new UnityEngine.GameObject("Leena mechanical | canonical editable source");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
 var model=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(art+"LEENA_MECHANICAL_v003.fbx"),root.transform);
 foreach(var rr in model.GetComponentsInChildren<UnityEngine.Renderer>()){var slots=rr.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=material;rr.sharedMaterials=slots;}
 var animator=model.GetComponentInChildren<UnityEngine.Animator>();if(!animator||!animator.avatar||!animator.avatar.isValid)throw new System.Exception("Invalid native generic rig");animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;
 var meshes=model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();if(meshes.Length==0)throw new System.Exception("Missing skinned Leena mesh");
 var bounds=meshes[0].bounds;foreach(var rr in meshes)bounds.Encapsulate(rr.bounds);
 if(bounds.size.y<1.2f||bounds.size.y>1.4f)throw new System.Exception("Unexpected Leena height "+bounds.size.y);
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,prefabs+"LeenaCanonical.prefab");report.AppendLine("Generic Avatar valid="+animator.avatar.isValid+" height="+bounds.size.y.ToString("F4")+" skinnedMeshes="+meshes.Length+" texture2048=source4096Preserved");
}finally{if(root)UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
UnityEditor.AssetDatabase.SaveAssets();report.AppendLine("Reusable asset imported. HUD stage placement, gesture refinement and actual playback acceptance remain pending.");System.IO.File.WriteAllText("LocalOnly/Validation/Production/leena-v003-import.txt",report.ToString());return report.ToString();
