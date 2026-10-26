using System;
using Altaius.Demo02.UI;
using Altaius.Demo02.Gameplay;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static class Task5LearningCueAuthoring
    {
        public static string AddOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Use the clean saved campus outside Play.");
            var ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();
            if(ui.GetComponent<LearningCueProjection>())throw new InvalidOperationException("Preserve existing authored cues.");
            var g=new GameObject("F1 | single reused inspected passage ribbon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(LayoutElement));
            g.transform.SetParent(ui.Workbench.Window.transform,false);g.GetComponent<LayoutElement>().ignoreLayout=true;
            var image=g.GetComponent<Image>();image.color=new Color(.22f,.40f,.46f,0);image.raycastTarget=false;image.enabled=false;
            var cue=ui.gameObject.AddComponent<LearningCueProjection>();cue.UI=ui;cue.Workbench=ui.Workbench;cue.PassageRibbon=image;cue.RibbonStorage=ui.Workbench.Window.transform;cue.WorldTotal=UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>().TotalLabel;
            EditorSceneManager.SaveScene(scene);return "Saved one reused F1 ribbon and F2 projection for one world plus one UI draft total; no particles or lights.";
        }
    }
}
