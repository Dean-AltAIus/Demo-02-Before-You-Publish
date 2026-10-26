using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5ReviewerRevisionAuthoring
    {
        const string Folder="Assets/Demo02/Art/NPCInteractions_v001/";
        public static string ImportV023Once()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string path=Folder+"ModuleLead_Interactions_v023.fbx";
            if(EditorApplication.isPlaying||scene.isDirty||File.Exists(path))throw new InvalidOperationException("Clean saved campus and absent v023 import required.");
            File.Copy("SourceArt/Demo02/Exports/NPCInteractions_v001/ModuleLead_Interactions_v023.fbx",path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=32;importer.minBoneWeight=0;
            var definitions=importer.defaultClipAnimations;foreach(var c in definitions){c.loopTime=c.name.Contains("Idle_")||c.name.Contains("Listen_");c.loopPose=c.loopTime;c.lockRootRotation=c.lockRootPositionXZ=c.lockRootHeightY=true;}importer.clipAnimations=definitions;importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();var d=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();var library=d.LeadLibrary;var controller=(AnimatorController)d.Lead.runtimeAnimatorController;
            foreach(var take in library.Takes){var clip=clips.Single(c=>c.name.EndsWith("D02_ModuleLead_"+take.Id+"_v023",StringComparison.Ordinal));take.Clip=clip;controller.layers[0].stateMachine.states.Single(s=>s.state.name==take.Id).state.motion=clip;}
            library.SourceReceipt="SourceArt/Demo02/Exports/NPCInteractions_v001/ModuleLead_Interactions_v023.receipt.json";
            // Native rest, exact weights, packet tracks and endpoints are byte-identical. Keep the admitted mesh, avatar and hand anchors.
            EditorUtility.SetDirty(library);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();library.Find("SeatedIdle").Clip.SampleAnimation(d.Lead.gameObject,0);EditorSceneManager.SaveScene(scene);
            return "Bounded v023 clip replacement: ten graph states, saved exact skin/avatar/packet tracks retained. Surface and playback acceptance pending.";
        }
    }
}
