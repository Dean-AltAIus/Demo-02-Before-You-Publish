using System;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;

namespace Altaius.Demo02.Authoring
{
    public static class CampusDiagnostics
    {
        public static void GameSize(int width,int height)
        {
            var assembly=typeof(Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
            var texts=(string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null);
            string label="Demo02 validation "+width+"x"+height;int index=Array.FindIndex(texts,s=>s.Contains(label));
            if(index<0)
            {
                var sizeType=assembly.GetType("UnityEditor.GameViewSize");var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,new[]{Enum.Parse(enumType,"FixedResolution"),(object)width,height,label});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
                texts=(string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null);index=Array.FindIndex(texts,s=>s.Contains(label));
            }
            var gameType=assembly.GetType("UnityEditor.GameView");var game=EditorWindow.GetWindow(gameType,false,null,false);
            gameType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(game,index);
            game.Repaint();
        }
        public static object SavedAudit()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots=scene.GetRootGameObjects();var all=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var facade=UnityEngine.Object.FindFirstObjectByType<CampusGameplayFacade>();var view=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();
            return new {scene=scene.path,play=EditorApplication.isPlaying,roots=roots.Length,objects=all.Length,
                stations=facade.Stations.Length,missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
                cameras=all.Count(t=>t.GetComponent<Camera>()),players=all.Count(t=>t.GetComponent<CampusMotor>()),
                movementCameraBound=facade.Motor.ViewCamera==facade.CameraRig.GetComponent<Camera>(),cameraFollowBound=facade.CameraRig.FollowTarget==facade.Motor.transform,
                nativeUi=all.Count(t=>t.GetComponent<UnityEngine.UI.Text>()),stationLinks=facade.Stations.All(s=>s.Arrival && s.InteractionPoint && s.InspectionCamera),
                prefabRoots=all.Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)),
                leena=view.transform.Find("Leena persistent offline dock/Canonical Leena unchanged").GetComponent<UnityEngine.UI.Image>().sprite!=null,
                groundTop=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Measured paving")).Select(c=>c.bounds.max.y).FirstOrDefault()};
        }
    }
}
