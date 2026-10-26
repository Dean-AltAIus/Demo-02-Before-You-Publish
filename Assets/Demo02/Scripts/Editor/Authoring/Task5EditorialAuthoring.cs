using System;
using Altaius.Demo02.UI;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static class Task5EditorialAuthoring
    {
        public static string ApplyOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty)throw new InvalidOperationException("Clean saved campus required.");
            var view=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();
            if(view.Rows.GetComponent<NativeEditorialLayout>())throw new InvalidOperationException("Preserve existing editorial layout.");
            string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(view.gameObject);var root=PrefabUtility.LoadPrefabContents(path);
            try{Apply(root.GetComponentInChildren<WorkbenchView>(true));PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            if(!view.Rows.GetComponent<NativeEditorialLayout>())Apply(view);
            var projection=UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>();
            string lanePath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(projection.AuthoredLanes[0].gameObject);root=PrefabUtility.LoadPrefabContents(lanePath);
            try{root.GetComponent<WorldMarkingLane>().FitLabels();PrefabUtility.SaveAsPrefabAsset(root,lanePath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            foreach(var lane in projection.AuthoredLanes){lane.FitLabels();foreach(var label in new[]{lane.CriterionLabel,lane.MarkLabel}){EditorUtility.SetDirty(label);PrefabUtility.RecordPrefabInstancePropertyModifications(label);}}
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved paired metadata fields and fitted persistent criterion-strip text. Native control order and large-text vertical layout retained.";
        }
        static void Apply(WorkbenchView view)
        {
            var previous=view.Rows.GetComponent<VerticalLayoutGroup>();if(!previous)throw new InvalidOperationException("Expected authored vertical layout.");
            var padding=previous.padding;float spacing=previous.spacing;UnityEngine.Object.DestroyImmediate(previous);
            var layout=view.Rows.gameObject.AddComponent<NativeEditorialLayout>();layout.padding=padding;layout.Spacing=spacing;
        }
    }
}
