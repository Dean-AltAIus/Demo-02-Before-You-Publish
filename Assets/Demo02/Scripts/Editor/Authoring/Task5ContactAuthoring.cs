using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using Altaius.Demo02.Presenter;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit versioned authoring, never a startup initializer or whole-scene generator.</summary>
    public static class Task5ContactAuthoring
    {
        const string Folder="Assets/Demo02/Art/FinalContacts_v009/";
        const string Model=Folder+"AcademicLearner_ContactLibrary_v009.fbx";
        [Serializable] sealed class JsonRoot{public JsonTrack[] tracks;}
        [Serializable] sealed class JsonTrack{public string family;public JsonKey[] samples;}
        [Serializable] sealed class JsonKey{public float time;public float[] position,rotation;}
        public static string ImportOnce()
        {
            if(EditorApplication.isPlaying||Directory.Exists(Folder))throw new InvalidOperationException("Absent candidate import and edit mode required.");
            Directory.CreateDirectory(Folder);File.Copy("SourceArt/Demo02/Exports/ContactLibrary_v009/AcademicLearner_ContactLibrary_v009.fbx",Model);
            AssetDatabase.ImportAsset(Model,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(Model);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;importer.importBlendShapes=false;importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=32;importer.minBoneWeight=0;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;var clips=importer.defaultClipAnimations;
            foreach(var c in clips){c.loopTime=c.name.Contains("_Idle_")||c.name.Contains("_Walk_");c.loopPose=c.loopTime;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;}
            importer.clipAnimations=clips;importer.SaveAndReimport();
            return "Imported owned 54-bone contact candidate, compression off. Scene and old sources unchanged.";
        }
        public static string AuthorOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity"||AssetDatabase.LoadAssetAtPath<ContactMotionLibrary>(Folder+"ContactLibrary.asset"))throw new InvalidOperationException("Clean saved campus and absent authoring batch required.");
            var gameplay=UnityEngine.Object.FindFirstObjectByType<CampusGameplayFacade>();var work=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();var ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();var projection=UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>();
            var clips=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
            Func<string,AnimationClip> clip=id=>clips.Single(c=>c.name.EndsWith("D02_Learner_"+id+"_v009",StringComparison.Ordinal));
            string controllerPath=Folder+"LearnerContacts.controller";AssetDatabase.CopyAsset("Assets/Demo02/Animations/Characters_v002/Learner.controller",controllerPath);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);var machine=controller.layers[0].stateMachine;
            foreach(var state in machine.states){if(state.state.name=="Idle")state.state.motion=clip("Idle");else if(state.state.name=="Walk")state.state.motion=clip("Walk");}
            foreach(var c in clips){var key=c.name.Substring(c.name.IndexOf("D02_Learner_",StringComparison.Ordinal));if(machine.states.Any(s=>s.state.name==key))continue;var state=machine.AddState(key);state.motion=c;state.writeDefaultValues=true;}
            var neutral=new AnimationClip{name="ContactRest"};
            foreach(var binding in AnimationUtility.GetCurveBindings(clip("Idle"))){var value=AnimationUtility.GetEditorCurve(clip("Idle"),binding).Evaluate(0);AnimationUtility.SetEditorCurve(neutral,binding,new AnimationCurve(new Keyframe(0,value),new Keyframe(1f/30f,value)));}
            AssetDatabase.CreateAsset(neutral,Folder+"ContactRest.anim");machine.AddState("ContactRest").motion=neutral;
            var library=ScriptableObject.CreateInstance<ContactMotionLibrary>();library.SourceReceipt="SourceArt/Demo02/Exports/ContactLibrary_v009/receipt.json";
            var tracks=JsonUtility.FromJson<JsonRoot>(File.ReadAllText("SourceArt/Demo02/Exports/ContactPropTracks_v002/native-prop-tracks.json"));
            library.Motions=tracks.tracks.Select(t=>new ContactMotion{Id=t.family,Approach=clip(t.family+"Approach"),Contact=clip(t.family),Recovery=clip(t.family+"Recovery"),ApproachState="D02_Learner_"+t.family+"Approach_v009",ContactState="D02_Learner_"+t.family+"_v009",RecoveryState="D02_Learner_"+t.family+"Recovery_v009",PropKeys=t.samples.Select(k=>new NativePropKey{Time=k.time,Position=new Vector3(k.position[0],k.position[1],k.position[2]),Rotation=new Quaternion(k.rotation[0],k.rotation[1],k.rotation[2],k.rotation[3])}).ToArray()}).Concat(new[]{new ContactMotion{Id="PacketPresent",Approach=neutral,Contact=clip("PacketPresent"),Recovery=neutral,ApproachState="ContactRest",ContactState="D02_Learner_PacketPresent_v009",RecoveryState="ContactRest"}}).ToArray();
            AssetDatabase.CreateAsset(library,Folder+"ContactLibrary.asset");
            var old=gameplay.Motor.CharacterVisual.GetComponentInChildren<Animator>();var bodyMaterial=old.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial;
            var actor=new GameObject("AcademicLearner");var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(actor.transform,false);var embedded=model.GetComponent<Animator>();if(embedded)UnityEngine.Object.DestroyImmediate(embedded);
            // Use the same hierarchy root convention as the reviewed character prefab.
            foreach(Transform child in model.transform.Cast<Transform>().ToArray())child.SetParent(actor.transform,false);UnityEngine.Object.DestroyImmediate(model);
            var animator=actor.AddComponent<Animator>();animator.avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){renderer.sharedMaterial=bodyMaterial;renderer.updateWhenOffscreen=true;}
            var prefab=PrefabUtility.SaveAsPrefabAsset(actor,Folder+"AcademicLearner.prefab");UnityEngine.Object.DestroyImmediate(actor);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,gameplay.Motor.CharacterVisual);instance.transform.localPosition=old.transform.localPosition;instance.transform.localRotation=old.transform.localRotation;instance.transform.localScale=old.transform.localScale;
            var locomotion=UnityEngine.Object.FindFirstObjectByType<CharacterLocomotion>();locomotion.Animator=instance.GetComponent<Animator>();UnityEngine.Object.DestroyImmediate(old.gameObject);gameplay.Motor.CharacterVisual.name="CharacterVisual | owned editable learner";
            var root=new GameObject("Physical learning contacts | editable stances and props");var table=new GameObject("Studio contact coordinate frame");table.transform.SetParent(root.transform,false);table.transform.SetPositionAndRotation(new Vector3(6.7f,0,-1.2f),Quaternion.Euler(0,345,0));
            var task=Find("Studio_Task");var rubric=Find("Studio_Rubric");PlaceFolio(task,table.transform,-.95f);PlaceFolio(rubric,table.transform,-.63f);
            var workFolio=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Prefabs/Environment/Kit_v001/Folio.prefab"),root.transform);workFolio.name="Studio_WorkFolio";PlaceFolio(workFolio.transform,table.transform,.30f);
            var bindings=new List<ContactBinding>();
            AddFolio(bindings,root.transform,table.transform,"Task",task,-1.07f);AddFolio(bindings,root.transform,table.transform,"Rubric",rubric,-.75f);AddFolio(bindings,root.transform,table.transform,"Work",workFolio.transform,.18f);
            var passageStance=Stance(root.transform,table.transform,"Passage placement stance",.18f);var pointStance=Stance(root.transform,table.transform,"Literal marking stance",.049f);
            var sheet=Part(root.transform,"Single selected passage sheet",new Vector3(.21f,.0014f,.297f),Material("D02_CreamPaper"));SetNative(sheet,passageStance,library.Find("Passage").Sample(0),false);
            bindings.Add(new ContactBinding{Id="PlacePassage",MotionId="Passage",Stance=passageStance,Prop=sheet});bindings.Add(new ContactBinding{Id="EditionSlide",MotionId="EditionSlide",Stance=passageStance,Prop=sheet});
            var board=Part(root.transform,"Selected criterion lane board",new Vector3(.19f,.006f,.25f),Material("D02_WarmOak"));board.SetPositionAndRotation(table.transform.TransformPoint(new Vector3(-.06f,.8155f,.5385f)),table.transform.rotation);
            var labelBoard=Part(root.transform,"Selected criterion wording plate",new Vector3(.48f,.008f,.21f),Material("D02_CreamPaper"));labelBoard.SetPositionAndRotation(table.transform.TransformPoint(new Vector3(-.36f,.8165f,.275f)),table.transform.rotation);
            var selected=root.AddComponent<SelectedCriterionLane>();selected.Workbench=work;var labelTemplate=projection.AuthoredLanes[0].CriterionLabel;
            selected.Identity=Label(labelTemplate,labelBoard,"Exact selected work and criterion",new Vector3(-.225f,.005f,-.08f),.008f);selected.LiteralCriterion=Label(labelTemplate,labelBoard,"Literal current criterion",new Vector3(-.225f,.005f,-.025f),.008f);selected.Mark=Label(labelTemplate,labelBoard,"Actual saved mark",new Vector3(-.225f,.005f,.075f),.008f);selected.Refresh();
            var pen=Find("Studio_Pen");SetNative(pen,pointStance,library.Find("PenPoint").Sample(0),true);bindings.Add(new ContactBinding{Id="MarkPoint",MotionId="PenPoint",Stance=pointStance,Prop=pen,ImportedNativeGeometry=true});
            // Versioned editable rest source includes a socket, not an unsupported tilted pen.
            var rest=Part(root.transform,"Pen rest support",new Vector3(.026f,.0409f,.026f),Material("D02_BrushedBrass"));rest.SetPositionAndRotation(pointStance.TransformPoint(ContactMotionLibrary.NativePoint(new Vector3(-.029f,-.353f,.83295f))),table.transform.rotation);
            var reviewStance=new GameObject("Review desk presentation stance");reviewStance.transform.SetParent(root.transform,false);reviewStance.transform.SetPositionAndRotation(new Vector3(-6.7f,0,-1.2f)+Quaternion.Euler(0,15,0)*new Vector3(0,0,.9f),Quaternion.Euler(0,195,0));bindings.Add(new ContactBinding{Id="PacketPresent",MotionId="PacketPresent",Stance=reviewStance.transform});
            var director=root.AddComponent<CampusContactDirector>();director.Gameplay=gameplay;director.Workbench=work;director.UI=ui;director.Library=library;director.Learner=locomotion.Animator;director.Bindings=bindings.ToArray();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved owned learner/controller/contact library, separate physical stances, literal selected criterion lane, single passage sheet and supported pen. Runtime contact acceptance remains pending.";
        }
        static Transform Find(string name)=>UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name==name&&t.gameObject.scene.IsValid());
        static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo02/Materials/CampusPalette_v002/"+name+".mat");
        static Transform Part(Transform parent,string name,Vector3 size,Material material){var root=new GameObject(name);root.transform.SetParent(parent,false);var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Editable solid";g.transform.SetParent(root.transform,false);g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return root.transform;}
        static Transform Stance(Transform parent,Transform table,string name,float x){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.SetPositionAndRotation(table.TransformPoint(new Vector3(x,0,.92f)),table.rotation*Quaternion.Euler(0,180,0));return g.transform;}
        static void PlaceFolio(Transform folio,Transform table,float x){folio.SetPositionAndRotation(table.TransformPoint(new Vector3(x,.815f,.49f)),table.rotation*Quaternion.Euler(-90,0,0));}
        static void AddFolio(List<ContactBinding> result,Transform parent,Transform table,string name,Transform folio,float x)
        {var stance=Stance(parent,table,name+" folio opening stance",x);var close=Stance(parent,table,name+" folio closing stance",x+.24f);var hinge=folio.GetComponentsInChildren<Transform>().Single(t=>t.name=="FolioCoverHinge");result.Add(new ContactBinding{Id=name+"Open",MotionId="FolioOpen",Stance=stance,Prop=hinge,ImportedNativeGeometry=true});result.Add(new ContactBinding{Id=name+"Close",MotionId="FolioClose",Stance=close,Prop=hinge,ImportedNativeGeometry=true});if(name=="Work")return;var page=Part(parent,name+" editable turning leaf",new Vector3(.21f,.0014f,.297f),Material("D02_CreamPaper"));page.SetPositionAndRotation(stance.TransformPoint(ContactMotionLibrary.NativePoint(new Vector3(-.005f,-.4215f,.831f))),table.rotation);result.Add(new ContactBinding{Id=name+"Page",MotionId="PageTurn",Stance=stance,Prop=page});}
        static void SetNative(Transform prop,Transform stance,NativePropKey key,bool imported){prop.SetPositionAndRotation(stance.TransformPoint(ContactMotionLibrary.NativePoint(key.Position)),stance.rotation*ContactMotionLibrary.NativeRotation(key.Rotation)*(imported?Quaternion.Euler(-90,0,0):Quaternion.identity));}
        static TextMesh Label(TextMesh template,Transform parent,string name,Vector3 position,float size)
        {var copy=UnityEngine.Object.Instantiate(template.gameObject,parent);copy.name=name;copy.transform.localPosition=position;copy.transform.localRotation=template.transform.localRotation;copy.transform.localScale=Vector3.one;var text=copy.GetComponent<TextMesh>();text.anchor=TextAnchor.UpperLeft;text.alignment=TextAlignment.Left;text.characterSize=size;text.text="";return text;}
    }
}

