using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5NpcAuthoring
    {
        const string Folder="Assets/Demo02/Art/NPCInteractions_v001/";
        [Serializable]sealed class Tracks{public Track[] tracks;}
        [Serializable]sealed class Track{public string take;public Frame[] frames;}
        [Serializable]sealed class Frame{public float seconds;public float[] packet_position,packet_rotation_xyzw,cover_rotation_xyzw;}
        public static string ApplyOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>())throw new InvalidOperationException("Clean saved campus, absent NPC authoring required.");
            Directory.CreateDirectory(Folder);var gameplay=UnityEngine.Object.FindFirstObjectByType<CampusGameplayFacade>();var studio=gameplay.Stations.Single(s=>s.Kind==StationKind.Studio);var review=gameplay.Stations.Single(s=>s.Kind==StationKind.Review);
            var colleague=Actor("Colleague","v001",studio.NpcVisualSlot);var lead=Actor("ModuleLead","v019",review.NpcVisualSlot);
            var root=new GameObject("NPC interactions | editable packet graph");var director=root.AddComponent<NpcInteractionDirector>();director.UI=UnityEngine.Object.FindFirstObjectByType<PresentationView>();director.Learner=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();director.Colleague=colleague;director.Lead=lead;
            director.ColleagueLibrary=AssetDatabase.LoadAssetAtPath<NpcMotionLibrary>(Folder+"ColleagueMotions.asset");director.LeadLibrary=AssetDatabase.LoadAssetAtPath<NpcMotionLibrary>(Folder+"ModuleLeadMotions.asset");
            var desk=Find("Review_Desk");var yaw=Quaternion.Euler(0,15,0);var origin=new GameObject("Review native contact origin | metres");origin.transform.SetParent(root.transform,false);origin.transform.SetPositionAndRotation(desk.position-yaw*new Vector3(0,0,.78f),yaw);director.LeadOrigin=origin.transform;
            review.NpcVisualSlot.SetPositionAndRotation(origin.transform.position,yaw);lead.transform.localPosition=Vector3.zero;lead.transform.localRotation=Quaternion.identity;
            var chair=Find("Review_LeadChair");chair.SetPositionAndRotation(origin.transform.TransformPoint(ContactMotionLibrary.NativePoint(new Vector3(0,.07f,0))),Quaternion.Euler(-90,15,0));
            var studioFrame=Find("Studio contact coordinate frame");studio.NpcVisualSlot.SetPositionAndRotation(studioFrame.TransformPoint(new Vector3(-1.55f,0,.1f)),studioFrame.rotation*Quaternion.Euler(0,90,0));colleague.transform.localPosition=Vector3.zero;colleague.transform.localRotation=Quaternion.identity;
            director.Packet=Find("Review_ReviewPacket");director.PacketCover=director.Packet.GetComponentsInChildren<Transform>().Single(t=>t.name=="FolioCoverHinge");
            var pose=director.LeadLibrary.Find("SeatedIdle").Sample(0);director.Packet.SetPositionAndRotation(origin.transform.TransformPoint(ContactMotionLibrary.NativePoint(pose.Position)),yaw*ContactMotionLibrary.NativeRotation(pose.Rotation)*Quaternion.Euler(-90,0,0));director.PacketCover.localRotation=pose.Cover;
            var template=UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>().AuthoredLanes[0].CriterionLabel;var label=UnityEngine.Object.Instantiate(template.gameObject,director.Packet);label.name="Live current pair | reviewer point target";label.transform.localPosition=new Vector3(-.20f,-.075f,.0155f);label.transform.localRotation=Quaternion.Euler(180,0,0);label.transform.localScale=Vector3.one;director.PairLabel=label.GetComponent<TextMesh>();director.PairLabel.text="Task 1 / Rubric 1";director.PairLabel.anchor=TextAnchor.UpperLeft;director.PairLabel.alignment=TextAlignment.Left;director.PairLabel.characterSize=.0028f;
            AddAnchor(lead,"LeftHand","Left palm packet contact");AddAnchor(lead,"RightHand","Right palm packet contact");
            director.LeadLibrary.Find("SeatedIdle").Clip.SampleAnimation(lead.gameObject,0);director.ColleagueLibrary.Find("Idle").Clip.SampleAnimation(colleague.gameObject,0);
            EditorUtility.SetDirty(director);PrefabUtility.RecordPrefabInstancePropertyModifications(review.NpcVisualSlot);PrefabUtility.RecordPrefabInstancePropertyModifications(studio.NpcVisualSlot);PrefabUtility.RecordPrefabInstancePropertyModifications(chair);PrefabUtility.RecordPrefabInstancePropertyModifications(director.Packet);PrefabUtility.RecordPrefabInstancePropertyModifications(director.PacketCover);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved 14 owned NPC takes, seated desk/packet alignment, live pair label, palm anchors and single packet director. Runtime acceptance pending.";
        }
        static Animator Actor(string role,string version,Transform slot)
        {
            string name=role+"_Interactions_"+version,path=Folder+name+".fbx";if(!File.Exists(path))File.Copy("SourceArt/Demo02/Exports/NPCInteractions_v001/"+name+".fbx",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=32;importer.minBoneWeight=0;
            var definitions=importer.defaultClipAnimations;foreach(var c in definitions){c.loopTime=c.name.Contains("Idle_")||c.name.Contains("Listen_");c.loopPose=c.loopTime;c.lockRootRotation=c.lockRootPositionXZ=c.lockRootHeightY=true;}importer.clipAnimations=definitions;importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+role+".controller");var machine=controller.layers[0].stateMachine;
            var library=ScriptableObject.CreateInstance<NpcMotionLibrary>();library.SourceReceipt="SourceArt/Demo02/Exports/NPCInteractions_v001/"+name+".receipt.json";
            var trackPath="SourceArt/Demo02/Exports/NPCInteractions_v001/"+name+".packet-tracks.json";var tracks=File.Exists(trackPath)?JsonUtility.FromJson<Tracks>(File.ReadAllText(trackPath)):null;
            library.Takes=clips.Select(c=>{string key=c.name.Substring(c.name.IndexOf("D02_"+role+"_",StringComparison.Ordinal)+role.Length+5);key=key.Substring(0,key.LastIndexOf("_v",StringComparison.Ordinal));var state=machine.AddState(key);state.motion=c;state.writeDefaultValues=true;var track=tracks?.tracks.SingleOrDefault(t=>t.take==key);return new NpcTake{Id=key,Clip=c,Packet=track?.frames.Select(f=>new PacketPoseKey{Time=f.seconds,Position=new Vector3(f.packet_position[0],f.packet_position[1],f.packet_position[2]),Rotation=Q(f.packet_rotation_xyzw),Cover=Q(f.cover_rotation_xyzw)}).ToArray()};}).ToArray();machine.defaultState=machine.states.Single(s=>s.state.name==(role=="ModuleLead"?"SeatedIdle":"Idle")).state;AssetDatabase.CreateAsset(library,Folder+role+"Motions.asset");
            var old=slot.GetComponentInChildren<Animator>();var material=old.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial;
            var actor=new GameObject(role);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);var embedded=model.GetComponent<Animator>();if(embedded)UnityEngine.Object.DestroyImmediate(embedded);foreach(Transform child in model.transform.Cast<Transform>().ToArray())child.SetParent(actor.transform,false);UnityEngine.Object.DestroyImmediate(model);
            var animator=actor.AddComponent<Animator>();animator.avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Single();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){renderer.sharedMaterial=material;renderer.updateWhenOffscreen=true;}
            library.Find(role=="ModuleLead"?"SeatedIdle":"Idle").Clip.SampleAnimation(actor,0);var prefab=PrefabUtility.SaveAsPrefabAsset(actor,Folder+role+".prefab");UnityEngine.Object.DestroyImmediate(actor);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,slot);UnityEngine.Object.DestroyImmediate(old.gameObject);return instance.GetComponent<Animator>();
        }
        static Quaternion Q(float[] q)=>new Quaternion(q[0],q[1],q[2],q[3]);
        static Transform Find(string name)=>UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.gameObject.scene.IsValid()&&t.name==name);
        static void AddAnchor(Animator actor,string bone,string name){var parent=actor.GetComponentsInChildren<Transform>().Single(t=>t.name==bone);var anchor=new GameObject(name);anchor.transform.SetParent(parent,false);}
    }
}
