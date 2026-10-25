using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.Integration;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Editor-only input validation. Explicit frame stepping, never OS input or foreground activation.</summary>
    public static class PresentationPlayProbe
    {
        sealed class Step{public Action Run;public float Delay;}
        static readonly Queue<Step> steps=new Queue<Step>();static readonly List<string> log=new List<string>();
        static PresentationView ui;static CampusGameplayFacade game;static float next;static Guid cancelledRequest,firstAttempt;static int evidenceBeforeReplay;
        public static bool Running {get;private set;}
        static HashSet<Guid> oldHelpIds;
        static CampusProbeInput isolatedInput;
        public static void Run()
        {
            if(!EditorApplication.isPlaying||Running)throw new InvalidOperationException("Use one existing Play session.");
            ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();if(!ui)throw new InvalidOperationException("Author saved presentation first.");game=ui.Gameplay;steps.Clear();log.Clear();Running=true;isolatedInput=game.gameObject.AddComponent<CampusProbeInput>();isolatedInput.Begin();
            Add(()=>{CampusDiagnostics.GameSize(1280,720);ui.OpenSettings("title");ui.Staged.TextScale=1;ui.ActivateControl("Primary");Log("Explicitly stepped background functional and layout evidence. Native test input is isolated in-process. No OS input, focus, live service, player build or normal-speed performance claim.");},.4f);
            Add(()=>{Check(ui.SurfaceId=="title","Saved title is the initial live surface");Shot("title-1280.png");});
            Click("Primary");Add(()=>Shot("brief-1280.png"));Click("Primary",.8f);
            Add(()=>{Check(ui.SurfaceId=="onramp","Truthful local loading enters optional controls");Shot("onramp-1280.png");});Click("Secondary");
            Add(()=>{Check(!ui.IsOpen,"Skip returns to actual campus");Check(ui.Workbench.LeenaDock.gameObject.activeInHierarchy,"Canonical Leena remains visible on campus");Shot("campus-leena-1280.png");Submit(ui.TalkLauncher);});
            Type("Composer","What is your role?");Click("Send",.8f);
            Add(()=>{var record=ui.Lifecycle.Conversation.History.Last();Check(record.State==ReplyState.Shown,"Ordinary free question receives an actual rendered source-bounded role reply");Check(ui.Host.Session.Snapshot.CurrentApproval==null,"Role reply grants no publication approval");Shot("conversation-colleague-1280.png");});
            Add(()=>ui.OpenConversation(ActorRole.ModuleLead));Type("Composer","I have not received approval for the revised rubric. Can it cover Rubric 3?");Click("Send",.8f);
            Add(()=>{Check(ui.Lifecycle.Conversation.History.Last().State==ReplyState.Unavailable,"Unsupported own question is unavailable, not approved or failed");Check(ui.Workbench.LeenaDock.gameObject.activeInHierarchy,"Leena remains separate and visible in NPC conversation");Shot("conversation-unavailable-1280.png");});
            Type("Composer","Please preserve this unsent follow-up while I cancel the local request.");Click("Send",.01f);Add(()=>{cancelledRequest=ui.Lifecycle.Conversation.History.Last().Request.Id;ui.CancelConversation();});
            Add(()=>{var r=ui.Lifecycle.Conversation.History.Last();Check(r.Request.Id==cancelledRequest&&r.State==ReplyState.Cancelled,"Cancel retains original request identity");Check(ui.Lifecycle.Conversation.Draft(ActorRole.ModuleLead).Contains("unsent follow-up"),"Cancelled own draft is preserved");Shot("conversation-cancelled-1280.png");});
            Add(()=>{ui.CloseSurface(false);game.DirectRoute("review");});Click("Review.Request");Type("Review.Summary","Please review the exact current draft. I have not inferred approval.");Type("Review.Time","13:40");Click("Primary");
            Add(()=>{Check(ui.Host.Session.Snapshot.ClockMinute==800,"Preview alone does not advance logical time");Shot("review-proposal-1280.png");});Click("Primary");
            Add(()=>Check(ui.Host.Session.Snapshot.ClockMinute==800,"Accepted learner review proposal still does not advance time"));Click("Primary");Click("Review.Schedule",.8f);
            Add(()=>{Check(ui.Host.Session.Snapshot.ClockMinute==820&&ui.Host.Session.Snapshot.ReviewStatus==ReviewStatus.Scheduled,"Rendered actual local scheduling acceptance advances to 13:40 only");Check(ui.Host.Session.Snapshot.CurrentApproval==null,"Scheduling is separate from approval");Shot("schedule-receipt-1280.png");});Click("Primary");Click("Review.Decision",.8f);
            Add(()=>{Check(ui.Host.Session.Snapshot.ReviewStatus==ReviewStatus.Returned,"Structural baseline receives a separate returned review with retained reason");Shot("review-returned-1280.png");});
            Add(()=>{ui.CloseSurface(false);game.DirectRoute("studio");ui.Workbench.ActivateControl("Inspect.D02.task.v1.Q1");ui.Workbench.ActivateControl("Primary");ui.CloseSurface(false);game.DirectRoute("review");});Click("Review.Hold");
            Click("Primary");Click("Primary");Add(()=>{Check(ui.Host.Session.Snapshot.HoldHistory.Count==0,"Missing hold defect/time/action rejects atomically");Shot("hold-missing-fields-1280.png");});
            Add(()=>ui.OpenHold());Type("Hold.Defect","The current task has no attached source passages.");Type("Hold.Time","14:30");Type("Hold.NextAction","Attach and inspect both source passages before the next review.");
            Click("Hold.Owner");Click("Hold.Basis");Click("Primary");Click("Primary");Click("Primary");Click("Hold.Accept",.8f);
            Add(()=>{Check(ui.Host.Session.Snapshot.Publication==PublicationStatus.Held,"Actual scoped owner accepts the structurally anchored hold");Check(ui.Host.Session.Snapshot.HoldHistory.Last().DefectVerified,"Hold defect uses actual inspected current anchors");Shot("hold-accepted-1280.png");});
            Add(()=>{ui.CloseSurface(false);ui.OpenCoach();});
            Add(()=>{var before=ui.Host.Session.Snapshot.Assistance.Count;ui.RequestHelp(HelpIntent.RecordedAttempt);Check(ui.Host.Session.Snapshot.Assistance.Count==before,"Requested help is not attributed before rendering");},.8f);
            Add(()=>{Check(ui.Host.Session.Snapshot.Assistance.Count>0,"Rendered requested help appears in actual ledger");Shot("leena-recorded-help-1280.png");ui.OpenSettings("pause");});
            Add(()=>Shot("settings-audio-1280.png"));
            for(int i=1;i<5;i++){int tab=i;Add(()=>{var screen=ui.Surfaces.Single(s=>s.SurfaceId=="settings");Submit(screen.TabButtons[tab]);Shot("settings-"+tab+"-1280.png");});}
            Add(()=>{var s=ui.Surfaces.Single(x=>x.SurfaceId=="settings");Submit(s.TabButtons[3]);s.TextSizeDropdown.value=3;Submit(s.Primary);});
            Add(()=>{Check(ui.Workbench.TextScale==2,"Applied 200% text reaches existing and new native UI");ui.Resume();ui.OpenConversation(ActorRole.Colleague);},.5f);Add(()=>Shot("conversation-text200-1280.png"));
            Add(()=>{CampusDiagnostics.GameSize(1920,1080);},.4f);Add(()=>Shot("conversation-text200-1920.png"));
            Add(()=>{CampusDiagnostics.GameSize(2560,1440);},.4f);Add(()=>Shot("conversation-text200-2560.png"));
            Add(()=>{CampusDiagnostics.GameSize(1280,720);ui.OpenDebrief();},.4f);Add(()=>Shot("debrief-text200-1280.png"));Click("Primary");Add(()=>Shot("timeline-text200-1280.png"));
            Add(()=>{firstAttempt=ui.Host.Session.Snapshot.AttemptId;evidenceBeforeReplay=ui.Host.Session.Snapshot.Evidence.Count;ui.EnterReplay();Check(ui.Host.Session.Snapshot.Evidence.Count==evidenceBeforeReplay,"Replay adds no learner evidence");});
            Add(()=>{ui.ConfirmLifecycle("linked");});Click("Primary");
            Add(()=>{var s=ui.Host.Session.Snapshot;Check(s.AttemptId!=firstAttempt&&s.LinkedHistory.Count==1&&s.Assistance.Count>0,"Linked practice creates new attempt and preserves original history/help");oldHelpIds=new HashSet<Guid>(s.Assistance.Select(h=>h.Id));ui.ConfirmLifecycle("reset");});Click("Primary");
            Add(()=>{var s=ui.Host.Session.Snapshot;Check(s.LinkedHistory.Count==0&&s.Assistance.All(h=>!oldHelpIds.Contains(h.Id))&&s.AnnotationHistory.Count==0&&ui.Lifecycle.Conversation.History.Count==0,"Full reset clears old lineage, evidence, assistance and personal text; only newly rendered optional controls may be recorded");Check(ui.Workbench.LeenaDock.gameObject.activeInHierarchy,"Saved canonical Leena survives reset");Shot("reset-onramp-text200-1280.png");Log("COMPLETE. Stop Play separately and verify saved authoring. Normal-speed timing, animation and hardware performance remain unverified.");});
            next=Time.unscaledTime;EditorApplication.update+=Tick;
        }
        static void Type(string control,string value)
        {Add(()=>{Check(ui.SetFieldText(control,""),"Native field exists: "+control);});foreach(char character in value){char c=character;Add(()=>{var surface=ui.Surfaces.Single(s=>s.SurfaceId==ui.SurfaceId);var field=control=="Composer"?surface.Composer:surface.Rows.Find(control).Input;field.ProcessEvent(new Event{type=EventType.KeyDown,character=c,keyCode=KeyCode.None});field.ForceLabelUpdate();},.005f);}Add(()=>{var surface=ui.Surfaces.Single(s=>s.SurfaceId==ui.SurfaceId);var field=control=="Composer"?surface.Composer:surface.Rows.Find(control).Input;Check(field.text==value,"Native own typing retained: "+control);});}
        static void Click(string id,float delay=.14f)=>Add(()=>{Check(ui.ActivateControl(id),"Live native action available: "+id);},delay);
        static void Submit(Button button){EventSystem.current.SetSelectedGameObject(button.gameObject);ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);}
        static void Add(Action action,float delay=.14f)=>steps.Enqueue(new Step{Run=action,Delay=delay});
        static void Shot(string filename){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("LocalOnly/Validation/Presentation/"+filename);Log("Capture "+filename+" | "+Screen.width+"x"+Screen.height+" | "+ui.SurfaceId);}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("FAILED: "+text+" | "+ui.SurfaceId);Log("PASS: "+text);}
        static void Log(string text){log.Add(DateTime.UtcNow.ToString("O")+" "+text);Directory.CreateDirectory("LocalOnly/Validation/Presentation");File.WriteAllLines("LocalOnly/Validation/Presentation/native-ui-probe.txt",log);}
        static void Tick()
        {if(!EditorApplication.isPlaying){Stop("Play stopped intentionally or externally.");return;}EditorApplication.Step();if(Time.unscaledTime<next)return;if(steps.Count==0){Stop(null);return;}var step=steps.Dequeue();try{step.Run();next=Time.unscaledTime+step.Delay;}catch(Exception e){Stop(e.ToString());}}
        public static void Stop(string reason){Running=false;steps.Clear();EditorApplication.update-=Tick;if(isolatedInput)UnityEngine.Object.DestroyImmediate(isolatedInput);new LocalPresentationPreferences().Save();if(reason!=null)Log(reason);}
    }
}
