using System;
using System.Collections.Generic;
using Altaius.Demo02.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Altaius.Demo02.Presenter
{
    /// <summary>Blocks laptop keyboard/navigation and background UI clicks while retaining native programmatic fields.</summary>
    public sealed class PresenterInputLease:IDisposable
    {
        readonly PresentationView ui;readonly EventSystem events;readonly bool navigation;readonly CanvasGroup canvas;
        readonly bool raycasts,added;readonly Dictionary<InputField,bool> fields=new Dictionary<InputField,bool>();
        readonly bool contextVisible,textSizeVisible;
        bool disposed;
        public PresenterInputLease(PresentationView ui)
        {
            this.ui=ui;events=EventSystem.current;if(events){navigation=events.sendNavigationEvents;events.sendNavigationEvents=false;}
            contextVisible=ui.Workbench.MarkContextButton&&ui.Workbench.MarkContextButton.gameObject.activeSelf;textSizeVisible=ui.Workbench.WorkspaceTextSizeButton&&ui.Workbench.WorkspaceTextSizeButton.gameObject.activeSelf;
            var root=ui.Workbench.GetComponentInParent<Canvas>();canvas=root.GetComponent<CanvasGroup>();if(!canvas){canvas=root.gameObject.AddComponent<CanvasGroup>();added=true;}
            raycasts=canvas.blocksRaycasts;canvas.blocksRaycasts=false;Apply();
        }
        public void Apply()
        {
            if(disposed)return;if(events)events.sendNavigationEvents=false;canvas.blocksRaycasts=false;
            if(ui.Workbench.TextScale>1){if(ui.Workbench.MarkContextButton)ui.Workbench.MarkContextButton.gameObject.SetActive(false);if(ui.Workbench.WorkspaceTextSizeButton)ui.Workbench.WorkspaceTextSizeButton.gameObject.SetActive(false);}
            foreach(var field in canvas.GetComponentsInChildren<InputField>(true))
            {if(!fields.ContainsKey(field))fields.Add(field,field.readOnly);field.readOnly=true;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;foreach(var pair in fields)if(pair.Key)pair.Key.readOnly=pair.Value;
            if(events)events.sendNavigationEvents=navigation;if(canvas){canvas.blocksRaycasts=raycasts;if(added)UnityEngine.Object.DestroyImmediate(canvas);}
            if(ui.Workbench.MarkContextButton)ui.Workbench.MarkContextButton.gameObject.SetActive(contextVisible);if(ui.Workbench.WorkspaceTextSizeButton)ui.Workbench.WorkspaceTextSizeButton.gameObject.SetActive(textSizeVisible);
        }
    }
}
