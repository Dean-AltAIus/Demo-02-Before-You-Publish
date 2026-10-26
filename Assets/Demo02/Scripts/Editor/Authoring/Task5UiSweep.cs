using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Disposable native UI art fixture. Does not persist preferences, authored objects or domain state.</summary>
    public static class Task5UiSweep
    {
        sealed class Shot {public string Name;public Action Open;}
        static readonly Queue<Shot> shots=new Queue<Shot>();static readonly List<string> log=new List<string>();
        static PresentationView ui;static WorkbenchView work;static Shot shot;static string folder;static double start,opened;static int frame;static bool captured;
        public static bool Running{get;private set;}public static string Result{get;private set;}
        public static string RunFinalDelta(string output)
        {
            if(!EditorApplication.isPlaying||Running||Directory.Exists(output))throw new InvalidOperationException("Fresh evidence folder and idle Play required.");
            folder=output;Directory.CreateDirectory(folder);ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();work=ui.Workbench;shots.Clear();log.Clear();shot=null;Result=null;
            foreach(var state in new[]{"locked","pending","stale","released","hold"}){string id=state;Add("review-"+id,()=>{Scale(1,1280,720);work.CloseWindow();Task5VisualFixtures.ShowReview(id);ui.Gameplay.DirectRoute("review");work.CloseWindow();ui.OpenReview();});}
            Add("review-request",ui.OpenReviewRequest);Add("hold-form",ui.OpenHold);
            Add("valid-task-preview",()=>{Scale(1,1280,720);Ready();foreach(var id in new[]{"S_A","S_B"})ui.Host.Session.Inspect(id,InspectionLocation.Archive);work.OpenTask();var sequence=AssetDatabase.LoadAssetAtPath<Altaius.Demo02.Presenter.PresenterSequence>("Assets/Demo02/Settings/Presenter/Walkthrough.asset");foreach(var f in sequence.Steps.Where(x=>x.Kind==Altaius.Demo02.Presenter.PresenterStepKind.TypeWorkbench&&x.Control.StartsWith("Task.")).GroupBy(x=>x.Control).Select(x=>x.First()))if(!work.SetFieldText(f.Control,f.Text))throw new InvalidOperationException("Missing task field "+f.Control);work.ActivateControl("Task.Attach.S_A");work.ActivateControl("Task.Attach.S_B");work.ActivateControl("Primary");if(work.ViewId!="preview")throw new InvalidOperationException("Task preview did not open: "+work.ViewId);});
            Add("rubric-200",()=>{Scale(2,1280,720);work.OpenRubric();});
            Add("task-1024",()=>{Scale(1,1024,768);Ready();work.OpenTask();});
            Add("colleague-1024",()=>ui.OpenConversation(ActorRole.Colleague));
            Add("rubric-1024-200",()=>{Scale(2,1024,768);ui.CloseSurface(false);work.OpenRubric();});
            Add("review-1024-200",ui.OpenReview);
            Add("source-studio",()=>{Scale(1,1280,720);Ready();work.ActivateControl("Inspect.D02.task.v1.Q1");});
            Add("presenter-1024",()=>{Scale(1,1024,768);Fresh();work.CloseWindow();var p=UnityEngine.Object.FindFirstObjectByType<Altaius.Demo02.Presenter.PresenterRunner>();p.StartSequence(p.Sequence,true);p.Pause();});
            Add("presenter-1024-200",()=>{Scale(2,1024,768);});
            Add("world-restored",()=>{UnityEngine.Object.FindFirstObjectByType<Altaius.Demo02.Presenter.PresenterRunner>().Cancel();Scale(1,1280,720);ui.CloseSurface(false);work.CloseWindow();});
            start=EditorApplication.timeSinceStartup;Running=true;EditorApplication.update+=Tick;return "Started bounded final native UI delta captures.";
        }
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running||Directory.Exists(output))throw new InvalidOperationException("Fresh evidence folder and idle Play required.");
            folder=output;Directory.CreateDirectory(folder);ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();work=ui.Workbench;shots.Clear();log.Clear();shot=null;Result=null;
            Add("00-world",()=>{Scale(1,1280,720);ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=true;work.CloseWindow();});
            Add("01-title",ui.OpenTitle);Add("02-brief",ui.OpenBrief);Add("03-controls",ui.OpenOnramp);Add("04-coach",ui.OpenCoach);
            Add("05-conversation",()=>ui.OpenConversation(ActorRole.Colleague));Add("06-settings-audio",()=>ui.OpenSettings());
            Add("07-settings-accessibility",()=>ui.Surfaces.Single(s=>s.SurfaceId=="settings").TabButtons[3].onClick.Invoke());
            Add("08-pause",ui.Pause);Add("09-reset-confirmation",()=>ui.ConfirmLifecycle("reset"));
            Add("10-timeline",ui.OpenTimeline);Add("11-practice-record",ui.OpenDebrief);Add("12-replay",ui.EnterReplay);
            Add("13-source-passage",()=>{Fresh();ui.Gameplay.DirectRoute("archive");work.ActivateControl("Inspect.S_A");});
            Add("14-source-notebook",work.OpenNotebook);
            Add("15-task-editor",()=>{Ready();work.OpenTask();});
            Add("16-task-preview",()=>{work.ActivateControl("Primary");});
            Add("17-rubric-editor",()=>{work.OpenRubric();});
            Add("18-marking-work",()=>{ui.Gameplay.DirectRoute("experiment");work.ActivateControl("Work.W_LIST");});
            Add("19-marking-context",()=>work.ActivateControl("Criterion.naming"));
            Add("20-review-pending",()=>Task5VisualFixtures.ShowReview("pending"));
            Add("21-review-stale",()=>Task5VisualFixtures.ShowReview("stale"));
            Add("22-review-release",()=>Task5VisualFixtures.ShowReview("released"));
            Add("23-owned-hold",()=>Task5VisualFixtures.ShowReview("hold"));
            Add("24-request",ui.OpenReviewRequest);Add("25-hold-form",ui.OpenHold);
            Add("26-task-200",()=>{Scale(2,1280,720);Ready();work.OpenTask();});
            Add("27-rubric-200",work.OpenRubric);Add("28-history-200",work.OpenHistory);
            Add("29-review-200",ui.OpenReview);Add("30-conversation-200",()=>ui.OpenConversation(ActorRole.ModuleLead));
            Add("31-settings-200",()=>{ui.OpenSettings();ui.Surfaces.Single(s=>s.SurfaceId=="settings").TabButtons[3].onClick.Invoke();});
            Add("32-title-200",ui.OpenTitle);
            Add("33-task-1024",()=>{Scale(1,1024,768);Ready();work.OpenTask();});
            Add("34-review-1024",ui.OpenReview);Add("35-conversation-1024",()=>ui.OpenConversation(ActorRole.Colleague));
            Add("36-review-1024-200",()=>{Scale(2,1024,768);ui.OpenReview();});
            Add("37-editor-1024-200",()=>{ui.CloseSurface(false);work.OpenRubric();});
            Add("38-pause-1920",()=>{Scale(1,1920,1080);ui.Pause();});
            start=EditorApplication.timeSinceStartup;Running=true;EditorApplication.update+=Tick;return "Started finite synthetic native art sweep, including 200% and alternate aspect ratios.";
        }
        static void Fresh(){ui.Lifecycle.Pause(false);ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=true;}
        static void Ready(){Fresh();foreach(var id in new[]{"D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})ui.Host.Session.Inspect(id,InspectionLocation.Studio);ui.Host.NotifyChanged();ui.Gameplay.DirectRoute("studio");}
        static void Scale(float scale,int w,int h){CampusDiagnostics.GameSize(w,h);ui.Applied.TextScale=scale;work.SetTextScale(scale);foreach(var surface in ui.Surfaces)surface.ApplyScale(scale);}
        static void Add(string name,Action open){shots.Enqueue(new Shot{Name=name,Open=open});}
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying){Finish("Manual Play exit, stopped");return;}if(EditorApplication.timeSinceStartup-start>160){Finish("FAILED bounded sweep timeout");return;}
                EditorApplication.QueuePlayerLoopUpdate();
                if(shot==null){if(shots.Count==0){Finish("Completed native art captures; visual inspection remains required.");return;}shot=shots.Dequeue();shot.Open();Canvas.ForceUpdateCanvases();opened=EditorApplication.timeSinceStartup;frame=Time.frameCount;captured=false;return;}
                if(Time.frameCount<frame+4||EditorApplication.timeSinceStartup-opened<1.2)return;
                if(!captured){ScreenCapture.CaptureScreenshot(Path.Combine(folder,shot.Name+".png"));log.Add(shot.Name+" | "+Screen.width+"x"+Screen.height+" | scale="+work.TextScale+" | "+ui.SurfaceId+"/"+work.ViewId);captured=true;opened=EditorApplication.timeSinceStartup;return;}
                if(EditorApplication.timeSinceStartup-opened>.35)shot=null;
            }
            catch(Exception ex){Finish("FAILED "+ex);}
        }
        static void Finish(string result){Result=result;Running=false;EditorApplication.update-=Tick;log.Add(result);File.WriteAllLines(Path.Combine(folder,"capture-status.txt"),log);}
    }
}
