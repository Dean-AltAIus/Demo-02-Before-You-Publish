using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.Presenter;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static class PresenterAuthoring
    {
        const string Settings="Assets/Demo02/Settings/Presenter/",Prefabs="Assets/Demo02/Prefabs/Presenter/";
        static Font Font=>AssetDatabase.LoadAssetAtPath<Font>("Assets/Demo02/Art/UI/Fonts/SourceSerif4-Regular.ttf");
        [MenuItem("Altaius/Demo02/Add editable presenter walkthrough (once)")]
        public static void AddSavedPresenter()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before presenter authoring.");
            var ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();if(!ui)throw new InvalidOperationException("Open the saved Demo 02 campus.");
            if(ui.gameObject.scene.isDirty||UnityEngine.Object.FindFirstObjectByType<PresenterRunner>()||File.Exists(Settings+"Walkthrough.asset"))throw new InvalidOperationException("Existing or unsaved authoring is preserved. This is an additive, one-time operation.");
            Directory.CreateDirectory(Settings);Directory.CreateDirectory(Prefabs);var sequence=PresenterSequenceAuthoring.CreateDefault();AssetDatabase.CreateAsset(sequence,Settings+"Walkthrough.asset");
            var root=new GameObject("Saved editable presenter walkthrough",typeof(RectTransform));root.transform.SetParent(ui.transform,false);var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var runner=root.AddComponent<PresenterRunner>();runner.UI=ui;runner.Sequence=sequence;
            var dock=new GameObject("Presenter controls | synthetic disclosure",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup));dock.transform.SetParent(root.transform,false);Place((RectTransform)dock.transform,16,88,350,198);dock.GetComponent<Image>().color=new Color(.025f,.09f,.18f,.96f);dock.GetComponent<CanvasGroup>().ignoreParentGroups=true;runner.Dock=dock;
            runner.Status=Text(dock.transform,"Visible synthetic status","SYNTHETIC / AUTHORED / OFFLINE\nEditable saved walkthrough\nNo learner evidence",16,12,10,326,80);
            runner.PauseButton=Button(dock.transform,"Pause",12,98,100);runner.CancelButton=Button(dock.transform,"Return",122,98,100);runner.ResetButton=Button(dock.transform,"Restart",232,98,106);runner.ExitButton=Button(dock.transform,"Exit",12,148,100);runner.CommentaryButton=Button(dock.transform,"Buyer captions",122,148,216);
            runner.Commentary=Text(root.transform,"Separate optional buyer commentary","Buyer commentary",17,16,296,350,132);runner.Commentary.color=NativeUiPalette.Ink;AddCommentaryPaper(runner.Commentary);runner.Commentary.gameObject.SetActive(false);
            var routeRoot=new GameObject("Saved presenter collision route anchors");var positions=new[]{new Vector3(0,0,4.5f),new Vector3(0,0,2),new Vector3(6,0,2),new Vector3(-6,0,2),new Vector3(0,0,-2),new Vector3(0,0,-4.2f)};
            var anchors=positions.Select((p,i)=>{var anchor=new GameObject("Editable route waypoint "+i).transform;anchor.SetParent(routeRoot.transform,false);anchor.position=p;return anchor;}).ToArray();
            runner.RouteNodes=anchors;PrefabUtility.SaveAsPrefabAssetAndConnect(routeRoot,Prefabs+"PresenterRoutes.prefab",InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,Prefabs+"PresenterWalkthrough.prefab",InteractionMode.AutomatedAction);runner=root.GetComponent<PresenterRunner>();runner.UI=ui;runner.RouteNodes=routeRoot.GetComponentsInChildren<Transform>().Where(t=>t!=routeRoot.transform).ToArray();
            ui.PresenterBehaviour=runner;PrefabUtility.RecordPrefabInstancePropertyModifications(runner);PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
            var motor=ui.Gameplay.Motor;var observer=motor.gameObject.AddComponent<CharacterLocomotion>();observer.Motor=motor;observer.Animator=motor.GetComponentInChildren<Animator>(true);observer.Animator.applyRootMotion=false;PrefabUtility.RecordPrefabInstancePropertyModifications(observer.Animator);EditorUtility.SetDirty(observer);EditorUtility.SetDirty(ui);
            dock.SetActive(false);EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveScene(ui.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        public static void AddCommentaryPaper(Text text)
        {
            var panel=new GameObject("Editable cream buyer-caption backing",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));panel.transform.SetParent(text.transform,false);panel.transform.SetAsFirstSibling();var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(-8,-8);rect.offsetMax=new Vector2(8,8);panel.GetComponent<Image>().color=new Color(1,.965f,.87f,.96f);panel.GetComponent<Image>().raycastTarget=false;
            // Backing is an independent sibling so it cannot draw over the parent's live text.
            panel.transform.SetParent(text.transform.parent,true);panel.transform.SetSiblingIndex(text.transform.GetSiblingIndex());var backing=text.gameObject.AddComponent<PresenterCaptionBacking>();backing.Backing=panel;backing.Fit();
        }
        static Text Text(Transform parent,string name,string text,int size,float x,float y,float width,float height)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));g.transform.SetParent(parent,false);Place((RectTransform)g.transform,x,y,width,height);var t=g.AddComponent<Text>();t.font=Font;t.fontSize=size;t.text=text;t.supportRichText=false;t.color=new Color(1,.97f,.87f);t.raycastTarget=false;return t;
        }
        static Button Button(Transform parent,string label,float x,float y,float width)
        {
            var g=new GameObject(label,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(parent,false);Place((RectTransform)g.transform,x,y,width,40);g.GetComponent<Image>().color=new Color(.97f,.91f,.76f);var b=g.AddComponent<Button>();b.targetGraphic=g.GetComponent<Image>();var t=Text(g.transform,"Native button label",label,16,0,0,width,40);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=new Vector2(8,0);t.rectTransform.offsetMax=new Vector2(-8,0);t.alignment=TextAnchor.MiddleCenter;t.color=NativeUiPalette.Ink;return b;
        }
    }
}
