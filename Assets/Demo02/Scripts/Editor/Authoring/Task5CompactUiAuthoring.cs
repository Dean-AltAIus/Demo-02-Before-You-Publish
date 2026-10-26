using System;
using System.Linq;
using Altaius.Demo02.UI;
using Altaius.Demo02.Presenter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit, bounded layout update. Never creates or replaces an authored hierarchy.</summary>
    public static class Task5CompactUiAuthoring
    {
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Clean saved campus outside Play required.");
            var style=AssetDatabase.LoadAssetAtPath<NativeUiStyle>("Assets/Demo02/Settings/Presentation/CompactGameUi.asset");if(!style){style=ScriptableObject.CreateInstance<NativeUiStyle>();AssetDatabase.CreateAsset(style,"Assets/Demo02/Settings/Presentation/CompactGameUi.asset");}
            int count=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Demo02/Prefabs/UI"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
                try{ApplyRoot(root,style);PrefabUtility.SaveAsPrefabAsset(root,path);count++;}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var work=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();
            var presenter=UnityEngine.Object.FindFirstObjectByType<PresenterRunner>();
            var roots=new[]{work.gameObject,presenter.Dock}.Where(g=>g).Distinct().ToArray();
            foreach(var root in roots)Undo.RegisterFullObjectHierarchyUndo(root,"Apply compact game UI layout");
            foreach(var root in roots)ApplyRoot(root,style);
            typeof(PresenterRunner).GetMethod("LayoutDock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(presenter,null);
            foreach(var component in roots.SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Distinct())
            {
                if(component is RectTransform||component is Text||component is LayoutGroup||component is LayoutElement||component is NativeFrame||component is WorkbenchView||component is PresentationSurface||component is NativeRow||component is NativeSettingRow)
                {EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Saved bounded compact geometry and typography in "+count+" UI prefabs and campus overrides.";
        }
        static void ApplyRoot(GameObject root,NativeUiStyle style)
        {
            foreach(var frame in root.GetComponentsInChildren<NativeFrame>(true)){frame.Radius=style.FrameRadius;frame.GoldCorners=false;}
            foreach(var layout in root.GetComponentsInChildren<VerticalLayoutGroup>(true))
            {layout.spacing=style.RowSpacing-2;layout.padding=new RectOffset(Mathf.Min(style.RowPadding,layout.padding.left),Mathf.Min(style.RowPadding,layout.padding.right),Mathf.Min(Mathf.Max(0,style.RowPadding-2),layout.padding.top),Mathf.Min(Mathf.Max(0,style.RowPadding-2),layout.padding.bottom));}
            foreach(var layout in root.GetComponentsInChildren<NativeEditorialLayout>(true)){layout.Spacing=style.RowSpacing;layout.ColumnGap=12;layout.padding=new RectOffset(0,0,2,2);}
            foreach(var row in root.GetComponentsInChildren<NativeRow>(true))
            {row.Style=style;row.ApplyScale(1);if(row.InputLayout)row.InputLayout.minHeight=0;if(row.ActionLayout)row.ActionLayout.preferredHeight=-1;}
            foreach(var w in root.GetComponentsInChildren<WorkbenchView>(true)){w.Style=style;w.ApplyInterfaceScale();}
            foreach(var s in root.GetComponentsInChildren<PresentationSurface>(true)){s.Style=style;s.ApplyScale(1);}
            foreach(var setting in root.GetComponentsInChildren<NativeSettingRow>(true)){setting.Style=style;setting.ApplyScale(1);}
            foreach(var ui in root.GetComponentsInChildren<PresentationView>(true))
            {
                var r=(RectTransform)ui.TalkLauncher.transform;r.anchorMin=r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-14,12);r.sizeDelta=new Vector2(130,32);
                var text=ui.TalkLauncher.GetComponentInChildren<Text>();text.fontSize=13;text.rectTransform.offsetMin=new Vector2(28,0);text.rectTransform.offsetMax=new Vector2(-6,0);
                var icon=ui.TalkLauncher.GetComponentInChildren<NativeIcon>();if(icon){icon.rectTransform.sizeDelta=new Vector2(16,16);icon.rectTransform.anchoredPosition=new Vector2(8,0);}
            }
            foreach(var presenter in root.GetComponentsInChildren<PresenterRunner>(true))if(presenter.UI&&presenter.UI.Workbench)
            {typeof(PresenterRunner).GetMethod("LayoutDock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(presenter,null);}
        }
    }
}
