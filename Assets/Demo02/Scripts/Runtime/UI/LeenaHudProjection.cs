using System;
using Altaius.Demo02.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    /// <summary>Saved canonical model. Reads actual delivery; never supplies assistance or authority.</summary>
    [DisallowMultipleComponent]
    public sealed class LeenaHudProjection : MonoBehaviour
    {
        public PresentationView UI;
        public Animator Animator;
        public Camera PortraitCamera;
        public RawImage Portrait;
        public Image CanonicalFallback;
        public string AttentionState="Attention", HelpState="DeliveredHelp", DebriefState="DeliveredDebrief";
        public bool UsePngFallback;
        public string VisibleState => cue.State;
        public Guid ProjectedDelivery => cue.Delivery;
        readonly LeenaCueState cue=new LeenaCueState();
        uint renderedRevision=uint.MaxValue;
        bool staticPose;
        void OnEnable(){if(Animator)Animator.applyRootMotion=false;}
        void LateUpdate()
        {
            if(!UI||!UI.Host||UI.Host.Session==null||!Animator)return;
            var snapshot=UI.Host.Session.Snapshot;
            bool reduced=UI.Applied?.ReducedMotion==true;
            bool paused=UI.Lifecycle?.Paused==true;
            cue.Observe(snapshot);cue.Tick(Time.unscaledDeltaTime,paused);
            if(renderedRevision!=cue.Revision)
            {renderedRevision=cue.Revision;SetState(cue.State=="DeliveredHelp"?HelpState:cue.State=="DeliveredDebrief"?DebriefState:AttentionState,reduced||cue.State=="Attention");}
            // The canonical static pose is the reduced-motion equivalent. No fake listening loop.
            if(reduced&&!staticPose){Animator.Play(AttentionState,0,0);Animator.Update(0);staticPose=true;}
            if(!reduced&&staticPose)
            {
                var state=cue.State=="DeliveredHelp"?HelpState:cue.State=="DeliveredDebrief"?DebriefState:AttentionState;
                Animator.Play(state,0,cue.Remaining>0?Mathf.Clamp01(1-cue.Remaining/8f):0);Animator.Update(0);staticPose=false;
            }
            Animator.speed=paused||reduced||cue.Remaining<=0?0:1;
            bool available=!UsePngFallback&&PortraitCamera&&Portrait&&PortraitCamera.targetTexture;
            if(Portrait)Portrait.enabled=available;if(CanonicalFallback)CanonicalFallback.enabled=!available;
            if(PortraitCamera)PortraitCamera.enabled=available;
        }
        void SetState(string state,bool immediate)
        {if(immediate){Animator.Play(state,0,0);Animator.Update(0);}else Animator.CrossFadeInFixedTime(state,.18f);}
        public void ReportRendered(Guid reportAttempt,Guid display)
        {if(!UI||!UI.Host||UI.Host.Session==null||UI.Host.Session.Snapshot.AttemptId!=reportAttempt||UI.SurfaceId!="debrief")return;cue.Observe(UI.Host.Session.Snapshot);cue.ReportRendered(reportAttempt,display);}
        void OnDisable(){cue.Cancel();if(Animator){Animator.speed=0;if(Animator.isActiveAndEnabled)Animator.Play(AttentionState,0,0);}}
    }
}
