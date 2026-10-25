using System;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    public interface IPresenterControls
    {
        bool Active {get;}
        void ShowBrief();void Pause();void Resume();void Cancel();
    }
    public sealed class PresenterWorkspaceBookmark
    {
        internal AttemptLifecycle Lifecycle;internal string Surface,View,Work,Criterion,Focus,Passage;
        internal CampusStation Station;internal ActorRole Role;internal string Summary,Time,Defect,HoldTime,Next,Help,Kind;
        internal ActorRole Owner;internal HoldDefectBasis Basis;internal CommandEnvelope Pending;internal AuthorityReply Reply;
        internal bool RetainedWorkbench;internal LocalPresentationPreferences Settings;internal int SettingsTab;internal string SettingsCaller;
    }
    public sealed partial class PresentationView
    {
        public MonoBehaviour PresenterBehaviour;
        public IPresenterControls PresenterControls=>PresenterBehaviour as IPresenterControls;
        public ulong SurfaceGeneration=>current?current.BindingGeneration:0;
        public string NativeSurfaceIdentity=>(Overlay&&Overlay.activeSelf?"presentation:"+SurfaceId+":"+SurfaceGeneration:"workbench:"+Workbench.ViewId+":"+Workbench.BindingGeneration);
        public void ShowPresenterBrief(Action normal,Action direct,Action back)
        {
            Begin("brief","Authored gameplay walkthrough");
            Text("Presenter.Disclosure","Synthetic, authored and offline","This run operates the same movement, native fields, previews and commands as manual practice. Its repair, marks, findings and role decisions are authored demonstration content, never your work or learner evidence. No AI or live service is connected.");
            Text("Presenter.Pacing","Full visible causal sequence","The source presentation target is 90 seconds. Full progressive typing, readable replies and campus travel take longer. Pace and dwell are editable in the saved Walkthrough asset. Pause, return to your practice, restart and exit remain available throughout.");
            Text("Presenter.Handback","Your current local work is preserved","The walkthrough uses a fresh isolated Presenter session. Returning restores your own session, drafts and prior workspace. Direct routes preserve the same decisions and receipts.");
            Button(current.Primary,"Start walkthrough",normal);Button(current.Secondary,"Direct / reduced motion",direct);Button(current.Tertiary,"Back",back);Finish("Authored demonstration. No learner evidence or measured learning claim.");
        }
        public PresenterWorkspaceBookmark CapturePresenterWorkspace()
        {
            var selected=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;var row=selected?selected.GetComponentInParent<NativeRow>():null;
            return new PresenterWorkspaceBookmark{Lifecycle=Lifecycle,Surface=SurfaceId,View=Workbench.ViewId,Work=Workbench.SelectedWork,Criterion=Host.Workbench.Mark?.CriterionId,Focus=current&&selected==current.Composer?.gameObject?"Composer":row?row.ControlId:null,Passage=Workbench.Heading.text,Station=Gameplay.ActiveStation,Role=ConversationRole,Summary=ReviewSummary,Time=ReviewTime,Defect=HoldDefect,HoldTime=HoldTime,Next=HoldNextAction,Help=HelpDraft,Owner=HoldOwner,Basis=HoldBasis,Pending=Pending,Reply=LastAuthorityReply,Kind=confirmationKind,RetainedWorkbench=suspendedWorkbench||Workbench.IsOpen,Settings=Staged?.Copy(),SettingsTab=settingsTab,SettingsCaller=settingsCaller};
        }
        public void EnterPresenterSession()
        {
            deliveries.Clear();suspendedWorkbench=false;suspendedFocus=null;Workbench.CloseWindow();CloseSurface(false);
            Host.BeginSession(SessionMode.Presenter);BindLifecycle();ClearDrafts();
        }
        public void InvalidatePresenterDeliveries(){deliveries.Clear();Lifecycle?.Dispatch.Clear();}
        public void RestorePresenterWorkspace(PresenterWorkspaceBookmark b)
        {
            if(b==null)return;deliveries.Clear();suspendedWorkbench=false;suspendedFocus=null;Workbench.CloseWindow();CloseSurface(false);Lifecycle=b.Lifecycle;
            ReviewSummary=b.Summary;ReviewTime=b.Time;HoldDefect=b.Defect;HoldTime=b.HoldTime;HoldNextAction=b.Next;HelpDraft=b.Help;HoldOwner=b.Owner;HoldBasis=b.Basis;Pending=b.Pending;LastAuthorityReply=b.Reply;confirmationKind=b.Kind;
            if(b.Station)Gameplay.Inspect(b.Station);
            if(b.Surface!="world"&&b.RetainedWorkbench)RestoreWorkbenchView(b);
            switch(b.Surface)
            {
                case "title":OpenTitle();break;case "brief":OpenBrief();break;case "conversation":OpenConversation(b.Role);break;case "coach":OpenCoach();break;
                case "review":OpenReview();break;case "reviewRequest":OpenReviewRequest();break;case "hold":OpenHold();break;
                case "confirmation":if(b.Pending!=null)PreviewDomain(b.Pending,b.Kind);else Pause();break;
                case "receipt":if(b.Reply!=null)ShowAuthority(b.Reply);else OpenReview();break;
                case "pause":Pause();break;case "settings":Staged=b.Settings??Applied.Copy();settingsCaller=b.SettingsCaller;ShowSettingsTab(b.SettingsTab);break;case "debrief":OpenDebrief();break;case "timeline":OpenTimeline();break;
                default:
                    RestoreWorkbenchView(b);
                    break;
            }
            if(!string.IsNullOrEmpty(b.Focus))
            {var field=Overlay.activeSelf?FindPresenterField(b.Focus):Workbench.Rows.Find(b.Focus)?.Input;if(field){field.Select();field.ActivateInputField();field.ForceLabelUpdate();}}
        }
        void RestoreWorkbenchView(PresenterWorkspaceBookmark b)
        {
            switch(b.View)
            {
                case "task":Workbench.OpenTask();break;case "rubric":Workbench.OpenRubric();break;case "preview":Workbench.RestorePendingPreview();break;
                case "work":Workbench.OpenWork(b.Work);break;case "mark":Workbench.OpenMark(b.Work,b.Criterion);break;
                case "notebook":Workbench.OpenNotebook();break;case "history":Workbench.OpenHistory();break;case "routes":Workbench.OpenRoutes();break;
                case "passage":Workbench.OpenPassage(b.Passage,b.Station?"sources":"notebook");break;
                case "sources":if(b.Station)Workbench.OpenSourceList();break;case "works":if(b.Station)Workbench.OpenWorkList();break;
                default:Workbench.CloseWindow();CloseSurface(false);break;
            }
        }
        string DisplayDisclosure(CommandReceipt receipt)=>S.Mode==SessionMode.Presenter?"Authored synthetic enactment through the "+receipt.Authority+" command route. No learner action or evidence.":receipt.AuthorityDisclosure;
        public void ShowPresenterStopNotice(string text)
        {if(Overlay.activeSelf&&current)current.Status.text=text;else if(Workbench.IsOpen)Workbench.Message.text=text;else Workbench.HudStatus.text=text;}
        public InputField FindPresenterField(string id)=>id=="Composer"?current?.Composer:current?.Rows.Find(id)?.Input;
        public bool ReadPresenterControl(string id)
        {
            var row=current?.Rows.Find(id);if(!row)return false;EventSystem.current?.SetSelectedGameObject(null);current.Focus.EnsureVisible(row.gameObject);return true;
        }
        public void ShowPresenterObservation(string heading,string actualText)
        {
            Begin("receipt",heading);Text("Presenter.Observation","Authored demonstration | actual local records",actualText);Button(current.Primary,"Review and publication",OpenReview);Finish("Synthetic presenter content. No learner evidence. Inspect the exact recorded IDs and versions.");
        }
        public void ShowPresenterFinding(string work,string[] actualText,Guid delivery,Action displayed)
        {
            Begin("receipt","Experiment reviewer | authored finding");
            NativeRow row=null;for(int i=0;i<actualText.Length;i++)row=Text("Presenter.Finding."+i,work+" | exact authored demonstration",actualText[i]);
            Button(current.Primary,"Review and publication",OpenReview);Finish("Prepared synthetic finding. Actual display is acknowledged separately. This is not publication approval.");
            AfterRendered(row.Body,delivery,displayed);
        }
    }
}
