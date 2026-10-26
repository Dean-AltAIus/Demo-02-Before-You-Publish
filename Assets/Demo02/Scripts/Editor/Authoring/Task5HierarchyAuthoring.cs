using System;
using System.Linq;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static class Task5HierarchyAuthoring
    {
        static readonly string[] Cards={"Review.Pair","Review.Earlier","Review.Gate","Review.Original","Hold.Current","Receipt.Status","Receipt.Outcome","Debrief.","Onramp.","Coach.","Conversation.Own.","Conversation.Reply.","Conversation.Detail.","Timeline.","Replay.Scope","Reset.Explanation","Brief.Fiction","ReviewRequest.Clock","Passage.Body","Passage.Identity","Work.Literal","Work.Total","Mark.Link","Mark.Criterion","Mark.Scope","Task.Base","Rubric.Base","Rubric.Allocation","Preview.Before.","Preview.After.","History.Current","History.Total."};
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Use the clean saved campus outside Play.");
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Demo02/Prefabs/UI"}).Select(AssetDatabase.GUIDToAssetPath))
            {var root=PrefabUtility.LoadPrefabContents(path);try{Refine(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
            foreach(var root in scene.GetRootGameObjects())Refine(root);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved secondary-button hierarchy and live paper sections on existing authored surfaces.";
        }
        static void Refine(GameObject root)
        {
            foreach(var card in root.GetComponentsInChildren<NativeRowPaperCard>(true))card.ControlPrefixes=Cards.ToArray();
            foreach(var surface in root.GetComponentsInChildren<PresentationSurface>(true))
            foreach(var button in new[]{surface.Secondary,surface.Tertiary,surface.Close,surface.CancelRequest})
            {
                if(!button)continue;
                var frame=button.GetComponentInChildren<NativeFrame>(true);if(frame){frame.Kind=NativeFrameKind.QuietButton;frame.SetVerticesDirty();}
                foreach(var text in button.GetComponentsInChildren<Text>(true))text.color=NativeUiPalette.Ink;
                foreach(var icon in button.GetComponentsInChildren<NativeIcon>(true))icon.color=NativeUiPalette.Ink;
            }
            foreach(var work in root.GetComponentsInChildren<WorkbenchView>(true))
            foreach(var button in new[]{work.Secondary,work.Close})
            {if(!button)continue;var frame=button.GetComponentInChildren<NativeFrame>(true);if(frame){frame.Kind=NativeFrameKind.QuietButton;frame.SetVerticesDirty();}foreach(var text in button.GetComponentsInChildren<Text>(true))text.color=NativeUiPalette.Ink;}
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                var frame=button.targetGraphic as NativeFrame;if(!frame)continue;
                var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.07f,1.07f,1.07f);colors.selectedColor=Color.white;colors.pressedColor=new Color(.86f,.86f,.86f);colors.disabledColor=new Color(.70f,.70f,.70f);button.colors=colors;
                if(frame.Kind==NativeFrameKind.QuietButton)foreach(var text in button.GetComponentsInChildren<Text>(true))text.color=NativeUiPalette.Ink;
            }
            // Explicitly record scene overrides; source-prefab changes alone cannot replace
            // an earlier colour override after Play restores the saved scene.
            foreach(var component in root.GetComponentsInChildren<Component>(true).Where(c=>c is Graphic||c is Selectable||c is NativeRowPaperCard))
            {EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
        }
    }
}
