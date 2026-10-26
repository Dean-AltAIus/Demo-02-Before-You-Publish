using System;
using System.Linq;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5CriterionLayoutAuthoring
    {
        public static string CreateOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var p=UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>();if(EditorApplication.isPlaying||scene.isDirty||p.ExpandedLaneRoot)throw new InvalidOperationException("Clean saved campus with no expanded layout required.");
            var frame=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="Studio contact coordinate frame");var area=new GameObject("Expanded current-criterion ledger | editable tabletop area");area.transform.SetParent(frame,false);area.transform.localPosition=new Vector3(.58f,.8125f,-.12f);p.ExpandedLaneRoot=area.transform;
            string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.AuthoredLanes[0].gameObject);var root=PrefabUtility.LoadPrefabContents(path);
            try{var lane=root.GetComponent<WorldMarkingLane>();lane.PassageProxy.transform.localPosition=new Vector3(0,.006f,0);lane.PassageProxy.transform.localScale=Vector3.one*.3f;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            foreach(var lane in p.AuthoredLanes){lane.PassageProxy.transform.localPosition=new Vector3(0,.006f,0);lane.PassageProxy.transform.localScale=Vector3.one*.3f;EditorUtility.SetDirty(lane.PassageProxy.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(lane.PassageProxy.transform);}
            EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved bounded tabletop area for four canonical current criteria, with scaled dense fallback. Static annotation miniatures fit between their own supported identity and mark strips. Baseline lane/contact anchors retained.";
        }
    }
}
