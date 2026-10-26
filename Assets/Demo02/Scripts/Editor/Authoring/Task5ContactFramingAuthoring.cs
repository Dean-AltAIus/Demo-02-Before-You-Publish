using System;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit bounded final contact-framing pass. Does not move functional arrivals, lanes, props or contact stances.</summary>
    public static class Task5ContactFramingAuthoring
    {
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty)throw new InvalidOperationException("Clean saved campus required.");
            var projection=UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>();
            string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(projection.AuthoredLanes[0].gameObject);var root=PrefabUtility.LoadPrefabContents(path);
            try{var lane=root.GetComponent<WorldMarkingLane>();foreach(var label in new[]{lane.CriterionLabel,lane.MarkLabel})label.transform.localRotation=Quaternion.Euler(90,0,0);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            foreach(var lane in projection.AuthoredLanes)foreach(var label in new[]{lane.CriterionLabel,lane.MarkLabel}){label.transform.localRotation=Quaternion.Euler(90,0,0);EditorUtility.SetDirty(label.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(label.transform);}
            var npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();npc.ReviewFieldOfView=55;
            npc.ReviewCamera.position=npc.LeadOrigin.TransformPoint(new Vector3(1.1f,1.55f,1.6f));npc.ReviewCamera.LookAt(npc.LeadOrigin.TransformPoint(new Vector3(0,.93f,.3f)));EditorUtility.SetDirty(npc);EditorUtility.SetDirty(npc.ReviewCamera);
            var camera=npc.UI.Gameplay.CameraRig.GetComponent<Camera>().GetUniversalAdditionalCameraData();camera.renderPostProcessing=true;camera.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;camera.antialiasingQuality=AntialiasingQuality.High;EditorUtility.SetDirty(camera);PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved four readable lane-sign rotations, shared prefab direction, tighter real reviewer view and camera SMAA High. Functional anchors and native weights unchanged.";
        }
    }
}
