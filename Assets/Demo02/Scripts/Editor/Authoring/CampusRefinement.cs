using System;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.Authoring
{
    public static class CampusRefinement
    {
        [MenuItem("Altaius/Demo 02/Apply bounded UI and composition refinements")]
        public static object Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before refining saved content.");
            var view=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();var canvas=view.transform;
            view.HudHeader=(RectTransform)canvas.Find("Campus header");view.HudTitle=view.HudHeader.Find("Title").GetComponent<Text>();
            view.PauseButton=view.HudHeader.Find("Pause").GetComponent<Button>();view.WorldHelp=(RectTransform)canvas.Find("World controls");
            view.LeenaDock=(RectTransform)canvas.Find("Leena persistent offline dock");view.LeenaIdentity=view.LeenaDock.Find("Identity").GetComponent<Text>();view.LeenaStatus=view.LeenaDock.Find("Offline status").GetComponent<Text>();
            if(!view.WorkspaceTextSizeButton)
            {
                var button=NativeUiAuthoring.Button("Accessible text size",canvas,"Text 200%");view.WorkspaceTextSizeButton=button;
                var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-18,24);r.sizeDelta=new Vector2(350,90);
                button.GetComponentInChildren<Text>().fontSize=40;button.gameObject.SetActive(false);
            }
            view.ApplyInterfaceScale();PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            // Explicit scene-only composition override: the converted source stations swap sides around their own courtyard axis.
            // This places Studio on screen-left and Review on screen-right in the opening view, without negative mesh scale.
            foreach(var name in new[]{"Studio authored assembly","Review authored assembly","Editable receipt-bound marking table"})
            {
                var g=GameObject.Find(name);
                if(g && Quaternion.Angle(g.transform.rotation,Quaternion.identity)<.01f && g.transform.position.sqrMagnitude<.01f)
                {g.transform.SetPositionAndRotation(new Vector3(0,0,-2.4f),Quaternion.Euler(0,180,0));PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);}
            }
            foreach(var sign in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name.EndsWith(" sign")))
            {sign.transform.rotation=Quaternion.Euler(0,180,0);PrefabUtility.RecordPrefabInstancePropertyModifications(sign.transform);}
            int statics=0;
            var campus=GameObject.Find("Campus | saved 24 x 18 metres");
            foreach(var renderer in campus.GetComponentsInChildren<Renderer>(true))
            {
                // Folio/cabinet hinges and all work-handling props stay movable for later authored animation.
                var ancestors=renderer.GetComponentsInParent<Transform>(true).Select(t=>t.name).ToArray();
                if(ancestors.Any(n=>n.Contains("Task") || n.Contains("Rubric") || n.Contains("WorkSample") || n.Contains("Pen") || n.Contains("Lane") || n.Contains("Cabinet") || n.Contains("Packet")))continue;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);statics++;
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            return new {environmentRenderers=campus.GetComponentsInChildren<Renderer>(true).Length,staticRendererObjects=statics,totalSceneRenderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length};
        }
    }
}
