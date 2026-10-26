using System;
using System.Linq;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static class Task5PaperRefinement
    {
        public static string ApplyOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty)throw new InvalidOperationException("Clean saved campus outside Play required.");
            if(UnityEngine.Object.FindFirstObjectByType<NativeRowPaperCard>(FindObjectsInactive.Include))throw new InvalidOperationException("Preserve existing paper-card authoring.");
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Demo02/Prefabs/UI"}).Select(AssetDatabase.GUIDToAssetPath))
            {var root=PrefabUtility.LoadPrefabContents(path);try{Refine(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
            foreach(var root in scene.GetRootGameObjects())Refine(root);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved paper cards and restrained native frames; focus defaults cleared without removing focus components.";
        }
        static void Refine(GameObject root)
        {
            foreach(var frame in root.GetComponentsInChildren<NativeFrame>(true)){frame.Radius=frame.Kind==NativeFrameKind.Navy?9:6;frame.Gold=new Color(.69f,.48f,.20f);frame.SetVerticesDirty();}
            foreach(var selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                var art=selectable.GetComponentsInChildren<NativeFrame>(true).FirstOrDefault();
                if(art&&selectable is Button&&selectable.GetComponent<Image>() is Image original){original.color=Color.clear;selectable.targetGraphic=art;}
                if(selectable.GetComponent<Outline>() is Outline outline){outline.effectColor=Color.clear;outline.effectDistance=Vector2.zero;outline.useGraphicAlpha=false;}
            }
            foreach(var row in root.GetComponentsInChildren<NativeRow>(true).Where(r=>r.Kind==NativeRowKind.Text))
            {
                if(row.GetComponent<NativeRowPaperCard>())continue;
                var layer=new GameObject("Editable live-text paper card",typeof(RectTransform),typeof(CanvasRenderer),typeof(LayoutElement));layer.transform.SetParent(row.transform,false);layer.transform.SetAsFirstSibling();layer.GetComponent<LayoutElement>().ignoreLayout=true;
                var rect=(RectTransform)layer.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var frame=layer.AddComponent<NativeFrame>();frame.Kind=NativeFrameKind.Card;frame.GoldCorners=false;frame.raycastTarget=false;
                var card=row.gameObject.AddComponent<NativeRowPaperCard>();card.Row=row;card.Card=frame;card.Layout=row.GetComponent<VerticalLayoutGroup>();
                if(card.Layout){var p=card.Layout.padding;card.PlainPadding=new Vector4(p.left,p.right,p.top,p.bottom);}
                layer.SetActive(false);
            }
        }
    }
}
