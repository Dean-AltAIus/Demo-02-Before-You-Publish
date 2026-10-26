using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.UI;
using Altaius.Demo02.Presenter;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
namespace Altaius.Demo02.Authoring
{
    public static class Task5ArtAuthoring
    {
        const string Folder="Assets/Demo02/Art/FinalPresentation/";
        public static string AddOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Use clean saved campus outside Play.");
            if(Directory.Exists(Folder)||UnityEngine.Object.FindFirstObjectByType<LeenaHudProjection>())throw new InvalidOperationException("Preserve existing presentation authoring.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();var w=ui.Workbench;
            var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"LeenaDelivery.controller");
            var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Demo02/Art/LeenaMechanical_v003/LEENA_MECHANICAL_v003.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            foreach(var pair in new[]{new[]{"PresenceAttention","Attention"},new[]{"PanelAttentionLeft","DeliveredHelp"},new[]{"PanelAttentionRight","DeliveredDebrief"}})
            {var clip=UnityEngine.Object.Instantiate(clips.Single(c=>c.name.Contains(pair[0])));clip.name=pair[1];var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,Folder+pair[1]+".anim");var state=controller.layers[0].stateMachine.AddState(pair[1]);state.motion=clip;if(pair[1]=="Attention")controller.layers[0].stateMachine.defaultState=state;}
            var stage=new GameObject("Saved canonical Leena HUD stage");stage.transform.position=new Vector3(1000,0,1000);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Prefabs/LeenaMechanical_v003/LeenaCanonical.prefab"),stage.transform);
            model.transform.localRotation=Quaternion.Euler(0,180,0);
            var animator=model.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var rt=new RenderTexture(280,320,24,RenderTextureFormat.ARGB32){name="Leena portrait | transparent live 3D",antiAliasing=2};AssetDatabase.CreateAsset(rt,Folder+"LeenaPortrait.renderTexture");
            var cameraObject=new GameObject("Editable Leena portrait camera");cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,.70f,-3);cameraObject.transform.localRotation=Quaternion.identity;
            var cam=cameraObject.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.76f;cam.nearClipPlane=.1f;cam.farClipPlane=5;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.targetTexture=rt;cam.cullingMask=1<<31;cam.depth=-10;
            var data=cameraObject.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.renderShadows=false;data.antialiasing=AntialiasingMode.None;
            foreach(var item in new[]{new Vector4(28,-35,0,1.3f),new Vector4(15,145,0,.55f)})
            {var lightObject=new GameObject("Editable portrait soft light");lightObject.transform.SetParent(stage.transform,false);lightObject.transform.localEulerAngles=new Vector3(item.x,item.y,item.z);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=item.w;light.shadows=LightShadows.None;light.cullingMask=1<<31;}
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            Camera.main.cullingMask&=~(1<<31);
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(!light.transform.IsChildOf(stage.transform))light.cullingMask&=~(1<<31);
            var fallback=w.LeenaDock.GetComponentsInChildren<Image>(true).Single(i=>i.name=="Canonical Leena unchanged");
            var portraitObject=new GameObject("Canonical Leena | live 3D portrait",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));portraitObject.transform.SetParent(w.LeenaDock,false);var rect=(RectTransform)portraitObject.transform;EditorUtility.CopySerialized(fallback.rectTransform,rect);rect.SetParent(w.LeenaDock,false);rect.SetSiblingIndex(fallback.transform.GetSiblingIndex()+1);var raw=portraitObject.GetComponent<RawImage>();raw.texture=rt;raw.raycastTarget=false;
            var projection=w.LeenaDock.gameObject.AddComponent<LeenaHudProjection>();projection.UI=ui;projection.Animator=animator;projection.PortraitCamera=cam;projection.Portrait=raw;projection.CanonicalFallback=fallback;fallback.enabled=false;
            PrefabUtility.SaveAsPrefabAsset(stage,Folder+"LeenaHudStage.prefab");
            foreach(var surface in ui.Surfaces.Where(s=>s.Sidebar))
            {
                var emblemObject=new GameObject("Editable paper and folio still life",typeof(RectTransform),typeof(CanvasRenderer));emblemObject.transform.SetParent(surface.Sidebar,false);var er=(RectTransform)emblemObject.transform;er.anchorMin=new Vector2(.08f,.43f);er.anchorMax=new Vector2(.92f,.98f);er.offsetMin=er.offsetMax=Vector2.zero;var emblem=emblemObject.AddComponent<NativePaperEmblem>();emblem.color=Color.white;emblem.raycastTarget=false;emblem.Kind=PaperEmblemKind.Folio;
                if(surface.SideText){surface.SideText.rectTransform.anchorMin=new Vector2(0,0);surface.SideText.rectTransform.anchorMax=new Vector2(1,.43f);surface.SideText.rectTransform.offsetMin=new Vector2(10,8);surface.SideText.rectTransform.offsetMax=new Vector2(-10,-6);surface.SideText.alignment=TextAnchor.UpperCenter;surface.SideText.fontSize=18;}
            }
            var presenter=UnityEngine.Object.FindFirstObjectByType<PresenterRunner>();FrameBehind(presenter.Dock,NativeFrameKind.Navy);
            foreach(var button in new[]{presenter.PauseButton,presenter.CancelButton,presenter.ResetButton,presenter.ExitButton,presenter.CommentaryButton})
            {FrameBehind(button.gameObject,button==presenter.PauseButton?NativeFrameKind.Button:NativeFrameKind.QuietButton);button.GetComponentInChildren<Text>().color=button==presenter.PauseButton?new Color(1,.97f,.87f):NativeUiPalette.Ink;}
            if(presenter.Commentary.GetComponent<PresenterCaptionBacking>()?.Backing is GameObject backing)FrameBehind(backing,NativeFrameKind.Paper);
            int duplicates=0;
            foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {var bindings=text.GetComponents<WorldTextAtlasBinding>();foreach(var extra in bindings.Where(b=>PrefabUtility.IsAddedComponentOverride(b)).Take(Mathf.Max(0,bindings.Length-1)).ToArray()){UnityEngine.Object.DestroyImmediate(extra);duplicates++;}if(text.GetComponents<WorldTextAtlasBinding>().Length!=1)throw new InvalidOperationException("Expected exactly one text material binding: "+text.name);}
            BindReviewArt(ui);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved 3D canonical Leena, native paper still lifes, styled presenter controls. Removed redundant text bindings: "+duplicates;
        }
        public static string CompleteReviewArtOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Use clean saved campus outside Play.");
            var ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();
            if(ui.GetComponent<ReviewArtProjection>())throw new InvalidOperationException("Preserve existing review projection.");
            BindReviewArt(ui);EditorSceneManager.SaveScene(scene);return "Saved review art projection on actual current state";
        }
        static void BindReviewArt(PresentationView ui)
        {
            var projection=ui.gameObject.AddComponent<ReviewArtProjection>();projection.UI=ui;
            projection.ReviewHeaderIcon=ui.Surfaces.Single(s=>s.SurfaceId=="review").Icon;
            projection.Emblems=ui.Surfaces.Where(s=>new[]{"review","reviewRequest","hold","receipt","confirmation"}.Contains(s.SurfaceId)).SelectMany(s=>s.GetComponentsInChildren<NativePaperEmblem>(true)).ToArray();
            foreach(var emblem in projection.Emblems)emblem.BindReview(ReviewArtState.Locked);
        }
        static void FrameBehind(GameObject target,NativeFrameKind kind)
        {
            if(target.GetComponent<Image>() is Image image)image.color=Color.clear;
            var g=new GameObject("Editable final native frame",typeof(RectTransform),typeof(CanvasRenderer));g.transform.SetParent(target.transform,false);g.transform.SetAsFirstSibling();var r=(RectTransform)g.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var frame=g.AddComponent<NativeFrame>();frame.Kind=kind;frame.GoldCorners=kind==NativeFrameKind.Navy||kind==NativeFrameKind.Paper;frame.raycastTarget=false;
        }
    }
}
