using System;
using System.Linq;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit, bounded title update. No campus builder, model change or renderer migration.</summary>
    public static class TitleMenuAuthoring
    {
        const string TitlePath="Assets/Demo02/Prefabs/UI/Presentation/title.prefab";
        public static object Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop before saved authoring.");
            var ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();
            if(!ui||ui.gameObject.scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Exact Demo2 scene required.");
            var existingStage=ui.GetComponent<TitleScenePresentation>();
            if((!existingStage||!existingStage.Shot)&&ui.Gameplay.transform.Cast<Transform>().Count(t=>t.name=="Title workroom shot | saved editable composition")>1)
                throw new InvalidOperationException("Ambiguous saved title shot. Restore its explicit reference before authoring.");
            var table=ui.Workbench.English;
            Add(table,"title.walkthrough","Watch walkthrough");Add(table,"title.subtitle","Explore, question and refine\nbefore you publish.");Add(table,"title.disclosure","Fictional practice. No real student data.");Add(table,"title.leena.identity","Leena | AI coach");Add(table,"title.leena.status","Offline | Help and controls");
            table.Invalidate();EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
            var prefab=PrefabUtility.LoadPrefabContents(TitlePath);
            try
            {
                var s=prefab.GetComponent<PresentationSurface>();var layout=s.GetComponent<TitleMenuLayout>();
                if(!layout)layout=s.gameObject.AddComponent<TitleMenuLayout>();s.TitleLayout=layout;layout.Surface=s;layout.Catalogue=table;
                var frame=s.Frame.GetComponent<NativeFrame>();if(frame)frame.enabled=false;
                layout.ViewBorder=Ornament("Editable campus view border",s.Frame,TitleOrnamentKind.ViewBorder);
                layout.MenuShade=Rect("Title navigation shade",s.Frame);var shade=Get<Image>(layout.MenuShade.gameObject);shade.color=new Color(.012f,.055f,.115f,.88f);shade.raycastTarget=false;
                layout.Plaque=Ornament("Cream academic title cartouche",s.Frame,TitleOrnamentKind.Cartouche);
                layout.Crest=Ornament("Book and laurel crest",s.Frame,TitleOrnamentKind.BookLaurel);
                layout.Subtitle=Label("Title purpose",s.Frame,s.Heading.font,new Color(1,.97f,.88f));
                layout.PresenterRow=s.Rows.AuthoredRows.First(r=>r.Kind==NativeRowKind.Action);layout.PresenterRow.transform.SetParent(s.Frame,false);
                foreach(var group in layout.PresenterRow.GetComponents<LayoutGroup>())group.enabled=false;
                foreach(var fitter in layout.PresenterRow.GetComponents<ContentSizeFitter>())fitter.enabled=false;
                layout.PresenterRow.BindAction(table.Get("title.walkthrough"),null,1);layout.PresenterRow.ControlId="Title.Presenter";layout.PresenterRow.gameObject.SetActive(true);
                layout.Companion=Rect("Title Leena help",s.Frame);var backing=Get<NativeFrame>(layout.Companion.gameObject);backing.Kind=NativeFrameKind.Navy;
                layout.CompanionHelp=Get<Button>(layout.Companion.gameObject);layout.CompanionHelp.targetGraphic=backing;var colors=layout.CompanionHelp.colors;colors.highlightedColor=new Color(.85f,.94f,1);layout.CompanionHelp.colors=colors;
                Get<Outline>(layout.Companion.gameObject).effectColor=new Color(0,0,0,.2f);
                layout.CompanionPortrait=Get<Image>(Rect("Canonical Leena",layout.Companion).gameObject);layout.CompanionPortrait.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Demo02/Art/UI/Leena_canonical_transparent.png");layout.CompanionPortrait.preserveAspect=true;layout.CompanionPortrait.raycastTarget=false;
                layout.CompanionIdentity=Label("Leena identity",layout.Companion,s.Heading.font,new Color(1,.97f,.88f));layout.CompanionStatus=Label("Leena availability",layout.Companion,s.Status.font,new Color(.65f,.85f,.9f));layout.RefreshLabels();
                s.Scroll.gameObject.SetActive(false);s.Paper.gameObject.SetActive(false);s.Icon.gameObject.SetActive(false);s.Close.gameObject.SetActive(false);
                s.Status.text=table.Get("title.disclosure");s.Heading.text=table.Get("presentationview.opentitle.opentitle.heading.before.you.publish");
                s.Focus.FocusRoot=s.Frame;s.Focus.Scroll=null;s.Focus.AdditionalFocusTargets=Array.Empty<Selectable>();
                layout.ViewBorder.SetAsFirstSibling();layout.MenuShade.SetSiblingIndex(1);layout.Plaque.SetSiblingIndex(2);layout.Crest.SetSiblingIndex(3);s.Heading.transform.SetAsLastSibling();layout.Subtitle.transform.SetAsLastSibling();s.Status.transform.SetAsLastSibling();
                s.ApplyScale(1);
                PrefabUtility.SaveAsPrefabAsset(prefab,TitlePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var title=ui.Surfaces.First(s=>s.SurfaceId=="title");
            title.ApplyScale(1);title.TitleLayout.RefreshLabels();title.Status.text=table.Get("title.disclosure");
            var stage=ui.GetComponent<TitleScenePresentation>();if(!stage){stage=ui.gameObject.AddComponent<TitleScenePresentation>();stage.FieldOfView=58;}
            stage.Rig=ui.Gameplay.CameraRig;stage.HiddenLearnerRenderers=stage.Rig.LocalBodyRenderers;
            EnsureShot(stage,ui.Gameplay.transform);
            ui.TitleStage=stage;
            foreach(var c in new UnityEngine.Object[]{ui,stage,title,title.TitleLayout}){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            foreach(var rect in title.GetComponentsInChildren<RectTransform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveScene(ui.gameObject.scene);AssetDatabase.SaveAssets();
            return new{saved=ui.gameObject.scene.path,title=TitlePath,shot=stage.Shot.position.ToString()};
        }
        public static void EnsureShot(TitleScenePresentation stage,Transform parent)
        {
            if(stage.Shot)return;
            const string name="Title workroom shot | saved editable composition";
            var matches=parent.Cast<Transform>().Where(t=>t.name==name).ToArray();
            if(matches.Length>1)throw new InvalidOperationException("Ambiguous saved title shot. Restore its explicit reference before authoring.");
            if(matches.Length==1){stage.Shot=matches[0];return;}
            var shotObject=new GameObject(name);stage.Shot=shotObject.transform;stage.Shot.SetParent(parent,false);
            stage.Shot.position=new Vector3(-4.9f,1.72f,.85f);stage.Shot.rotation=Quaternion.LookRotation(new Vector3(-6.15f,1.1f,-2.35f)-stage.Shot.position);
        }
        static T Get<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
        static RectTransform Rect(string name,Transform parent){var found=parent.Find(name);if(found)return (RectTransform)found;var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        static RectTransform Ornament(string name,Transform parent,TitleOrnamentKind kind){var r=Rect(name,parent);var g=Get<NativeTitleOrnament>(r.gameObject);g.Kind=kind;g.raycastTarget=false;return r;}
        static Text Label(string name,Transform parent,Font font,Color color){var t=Get<Text>(Rect(name,parent).gameObject);t.font=font;t.color=color;t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        static void Add(NativeEnglishStringTable table,string key,string value){if(table.Entries.Any(e=>e.Key==key))return;table.Entries=table.Entries.Concat(new[]{new NativeEnglishStringTable.Entry{Key=key,English=value}}).ToArray();}
    }
}
