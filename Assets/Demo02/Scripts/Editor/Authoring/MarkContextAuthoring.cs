using System;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.Authoring
{
    /// <summary>Bounded addition of native pinned context, preserving existing scene and prefab overrides.</summary>
    public static class MarkContextAuthoring
    {
        public static void AddOnce()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before adding saved context controls.");
            var view=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();
            if(view.MarkContextPanel)throw new InvalidOperationException("Pinned context already exists. Edit the saved controls directly.");
            var panel=NativeUiAuthoring.Panel("Pinned inspected marking context",view.transform,new Color(.98f,.96f,.9f));
            var r=(RectTransform)panel.transform;r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,.5f);r.offsetMin=new Vector2(14,136);r.offsetMax=new Vector2(356,-90);
            var scrollObject=NativeUiAuthoring.Rect("Context scroll",panel.transform);NativeUiAuthoring.Stretch((RectTransform)scrollObject.transform,10,10,10,10);
            var scroll=scrollObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=36;
            var viewport=NativeUiAuthoring.Panel("Context viewport",scrollObject.transform,new Color(1,1,1,.02f));NativeUiAuthoring.Stretch((RectTransform)viewport.transform);viewport.AddComponent<RectMask2D>();scroll.viewport=(RectTransform)viewport.transform;
            var content=NativeUiAuthoring.Rect("Editable context rows",viewport.transform);var cr=(RectTransform)content.transform;cr.anchorMin=new Vector2(0,1);cr.anchorMax=Vector2.one;cr.pivot=new Vector2(.5f,1);cr.sizeDelta=Vector2.zero;
            var layout=content.AddComponent<VerticalLayoutGroup>();layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;layout.spacing=12;
            content.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=cr;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Prefabs/UI/TextRow.prefab");
            view.MarkContextWork=((GameObject)PrefabUtility.InstantiatePrefab(prefab,content.transform)).GetComponent<NativeRow>();view.MarkContextWork.name="Pinned literal work";
            view.MarkContextCriterion=((GameObject)PrefabUtility.InstantiatePrefab(prefab,content.transform)).GetComponent<NativeRow>();view.MarkContextCriterion.name="Pinned current criterion";
            view.MarkContextWork.BindText("Inspected work passage","Select work at the experiment table.",1);view.MarkContextCriterion.BindText("Inspected criterion","Select a current criterion to mark.",1);
            view.MarkContextPanel=panel;
            var button=NativeUiAuthoring.Button("Read marking context",view.transform,"Read\ncontext");view.MarkContextButton=button;
            var br=(RectTransform)button.transform;br.anchorMin=br.anchorMax=Vector2.zero;br.pivot=Vector2.zero;br.anchoredPosition=new Vector2(714,24);br.sizeDelta=new Vector2(180,90);button.GetComponentInChildren<Text>().fontSize=32;
            view.Focus.AdditionalFocusTargets=new Selectable[]{view.WorkspaceTextSizeButton,button};view.ApplyInterfaceScale();
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);PrefabUtility.RecordPrefabInstancePropertyModifications(view.Focus);
            PrefabUtility.ApplyPrefabInstance(view.gameObject,InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
    }
}
