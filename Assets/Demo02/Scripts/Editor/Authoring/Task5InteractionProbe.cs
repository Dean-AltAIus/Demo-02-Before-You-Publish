using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using Altaius.Demo02.Presenter;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Finite background observation of real native controls. No OS input or domain substitute.</summary>
    public static class Task5InteractionProbe
    {
        sealed class Step{public string Name;public Action Run;public bool WaitContact;}
        static readonly Queue<Step> steps=new Queue<Step>();static readonly List<string> log=new List<string>();static readonly HashSet<string> captured=new HashSet<string>();
        static CampusContactDirector d;static NpcInteractionDirector npc;static PresentationView ui;static WorkbenchView work;static string folder;static double start,stepAt;static int lastFrame,stepFrame;static Step active;static bool reviewCapture;
        public static bool Running {get;private set;}
        public static string Result {get;private set;}
        public static string RunBaseline(string output)
        {
            Begin(output);
            Add("Fresh synthetic attempt",()=>{ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute("studio");CampusDiagnostics.GameSize(1280,720);},false);
            foreach(var id in new[]{"D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})
            {string source=id;Add("Inspect "+id,()=>Click("Inspect."+source));Add("Return from "+id,()=>Click("Primary"));}
            Add("Experiment route",()=>ui.Gameplay.DirectRoute("experiment"));Add("W_LIST current work",()=>Click("Work.W_LIST"));
            Add("Select naming lane",()=>Click("Criterion.naming"));
            Add("Enter actual numeric mark and rationale",()=>{Field("Mark.Marks","10");Field("Mark.Rationale","The work names the facilities count and supplier brochure.");});
            Add("Save naming",()=>Click("Primary"));
            Add("Check first saved annotation",()=>Check(ui.Host.Session.Snapshot.AnnotationHistory.Count==1,"One accepted annotation"));
            Add("Return to work",()=>Click("Secondary"));Add("Select listing lane",()=>Click("Criterion.listing"));
            Add("Enter second mark",()=>{Field("Mark.Marks","10");Field("Mark.Rationale","The work lists methods, dates and sample information.");});Add("Save listing",()=>Click("Primary"));
            Add("Baseline result",()=>{var s=ui.Host.Session.Snapshot;Check(s.DraftTotal("W_LIST")==20&&s.CurrentEvaluations.Count==0,"Actual 10/10 plus 10/10 remains a draft hypothesis, zero evaluations");Check(d.LastProblem==null,"No contact refusal");Shot("baseline-20.png");Log("notebook="+s.Notebook.Count+", audit="+s.Audit.Count+", evidence="+s.Evidence.Count+", completed="+d.Completed+", cancelled="+d.Cancelled);});
            Start();return "Started finite actual native source, lane and numeric-mark sequence.";
        }
        public static string RunReview(string output)
        {
            Begin(output);reviewCapture=true;Task5NativeCapture.Begin(Path.Combine(folder,"frames"));
            Add("Fresh synthetic attempt",()=>{ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute("review");CampusDiagnostics.GameSize(1280,720);},false);
            Add("Actual request form",()=>UIControl("Review.Request"));
            Add("Original request fields",()=>{UIField("Review.Summary","Inspect this current pair before publication. This is a synthetic motion check.");UIField("Review.Time","13:40");});
            Add("Preview original request",()=>UIControl("Primary"));
            Add("Confirm original request",()=>UIControl("Primary"));
            Add("Cycle and authority",()=>{Check(npc.CompletedCycles==1,"One complete physical review cycle");Check(npc.PacketOwner=="Desk"&&!npc.Busy,"Single packet returned to desk");var s=ui.Host.Session.Snapshot;Check(s.ReviewHistory.Count==1&&s.CurrentApproval==null&&s.Publication!=PublicationStatus.Released,"Handling did not grant review approval or release");Check(!ui.Gameplay.ContactOwnsInput,"Learner input ownership returned");Shot("review-completed.png");Log("Review requests="+s.ReviewHistory.Count+", cycles="+npc.CompletedCycles+", packet="+npc.PacketOwner);});
            Start();return "Started finite normal request form, learner presentation and actual single-packet reviewer sequence.";
        }
        public static string RunMotionExcerpt(string output,float captureFps=8)
        {
            Begin(output);reviewCapture=true;Task5NativeCapture.Begin(Path.Combine(folder,"frames"),captureFps);
            Add("Fresh synthetic attempt",()=>{ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute("studio");CampusDiagnostics.GameSize(1280,720);},false);
            foreach(var id in new[]{"D02.task.v1.Q1","D02.rubric.v1.naming"}){string source=id;Add("Inspect "+id,()=>Click("Inspect."+source));Add("Close "+id,()=>Click("Primary"));}
            Add("Experiment",()=>ui.Gameplay.DirectRoute("experiment"));Add("Work",()=>Click("Work.W_LIST"));Add("Selected lane",()=>Click("Criterion.naming"));
            Add("Numeric mark and rationale",()=>{Field("Mark.Marks","10");Field("Mark.Rationale","The work names the facilities count and supplier brochure.");});Add("Save",()=>Click("Primary"));
            Add("Verify",()=>{Check(ui.Host.Session.Snapshot.CurrentAnnotations.Count==1&&ui.Host.Session.Snapshot.CurrentEvaluations.Count==0,"One actual numeric annotation, no evaluation");Check(!ui.Gameplay.ContactOwnsInput&&d.LastProblem==null,"Contact returned ownership without refusal");});
            Start();return "Started timestamped actual M1/M2/M3 viewport capture with native controls, no per-phase PNG interruptions.";
        }
        public static string RunRevised(string output)
        {
            RunBaseline(output);
            Add("Archive route",()=>ui.Gameplay.DirectRoute("archive"));
            foreach(var id in new[]{"S_A","S_B"}){string source=id;Add("Inspect "+id,()=>Click("Inspect."+source));Add("Back from "+id,()=>Click("Primary"));}
            Add("Return to studio",()=>ui.Gameplay.DirectRoute("studio"));Add("Open actual task editor",()=>Click("Editor.Task"));
            var sequence=AssetDatabase.LoadAssetAtPath<PresenterSequence>("Assets/Demo02/Settings/Presenter/Walkthrough.asset");
            var fields=sequence.Steps.Where(s=>s.Kind==PresenterStepKind.TypeWorkbench).GroupBy(s=>s.Control).Select(g=>g.First()).ToArray();
            Add("Type existing authored task repair",()=>{foreach(var f in fields.Where(s=>s.Control.StartsWith("Task.")))Field(f.Control,f.Text);Click("Task.Attach.S_A");Click("Task.Attach.S_B");});
            Add("Preview actual task replacement",()=>Click("Primary"));Add("Commit task edition",()=>Click("Primary"));Add("Return from task receipt",()=>Click("Secondary"));
            Add("Open actual rubric editor",()=>Click("Editor.Rubric"));Add("Add third criterion",()=>Click("Rubric.Add"));Add("Add fourth criterion",()=>Click("Rubric.Add"));
            Add("Type existing authored four-criterion repair",()=>{foreach(var f in fields.Where(s=>s.Control.StartsWith("Rubric.")))Field(f.Control,f.Text);});Add("Preview actual rubric replacement",()=>Click("Primary"));Add("Commit rubric edition",()=>Click("Primary"));Add("Return from rubric receipt",()=>Click("Secondary"));
            var criteria=new[]{"explain_sources","compare_evidence","use_extracts","justify_limit"};
            foreach(var id in new[]{"D02.task.v2.Q1","D02.task.v2.Q2"}.Concat(criteria.Select(c=>"D02.rubric.v2."+c)))
            {string source=id;Add("Inspect revised "+id,()=>Click("Inspect."+source));Add("Back from revised "+id,()=>Click("Primary"));}
            Add("Retest route",()=>ui.Gameplay.DirectRoute("experiment"));Add("Inspect W_COMPARE",()=>Click("Work.W_COMPARE"));
            foreach(var id in criteria)
            {
                string criterion=id;Add("Select revised lane "+id,()=>Click("Criterion."+criterion));Add("Verify exact selected lane "+id,()=>{var lane=UnityEngine.Object.FindFirstObjectByType<SelectedCriterionLane>();Check(lane.CurrentSelection&&lane.SelectedCriterionId==criterion&&lane.SelectedWorkId=="W_COMPARE"&&lane.PairIdentity=="Task 2 / Rubric 2","Current physical lane binds "+criterion+" and W_COMPARE at2/2");Shot("revised-"+criterion+".png");Field("Mark.Marks","0");Field("Mark.Rationale","Synthetic motion and binding check. This provisional numeric mark is not an evaluation.");});Add("Save revised "+id,()=>Click("Primary"));Add("Back to revised work "+id,()=>Click("Secondary"));
            }
            Add("Revised state result",()=>{var s=ui.Host.Session.Snapshot;Check(s.Task.Version==2&&s.Rubric.Version==2&&s.Rubric.Criteria.Count==4&&s.CurrentAnnotations.Count==4&&s.CurrentEvaluations.Count==0,"Four revised selections and numeric annotations, no semantic evaluation");Check(d.LastProblem==null,"Revised page/edition/retest tracks finish without refusal");Shot("revised-four-lanes.png");});
            return "Started baseline then actual authored task/rubric editing, page/edition motion and all four current criterion placements.";
        }
        static void Begin(string output)
        {
            if(!EditorApplication.isPlaying||Running)throw new InvalidOperationException("One explicit Play session and no running probe required.");
            d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();ui=d.UI;work=d.Workbench;folder=output;Directory.CreateDirectory(folder);steps.Clear();log.Clear();captured.Clear();active=null;Result=null;lastFrame=-1;reviewCapture=false;
        }
        static void Add(string name,Action action,bool wait=true)=>steps.Enqueue(new Step{Name=name,Run=action,WaitContact=wait});
        static void Start(){Running=true;start=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying||!d){Finish("Manual Play exit, probe stopped");return;}if(EditorApplication.timeSinceStartup-start>240){Finish("FAILED: bounded probe timeout");return;}
                EditorApplication.QueuePlayerLoopUpdate();if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
                Log("frame="+Time.frameCount+", phase="+d.VisiblePhase+", binding="+d.ActiveBinding+", input="+ui.Gameplay.ContactOwnsInput+", queue="+d.QueuedCount+", problem="+d.LastProblem+", npc="+npc.LeadPhase+", packet="+npc.PacketOwner);
                if(reviewCapture)Task5NativeCapture.Tick(d.VisiblePhase,npc.LeadPhase);
                if(!reviewCapture&&npc.Busy&&npc.Lead.GetCurrentAnimatorStateInfo(0).normalizedTime>.4f&&captured.Add("npc-"+npc.LeadPhase))Shot("npc-"+npc.LeadPhase+".png");
                if(!reviewCapture&&d.VisiblePhase=="Contact"&&d.Learner.GetCurrentAnimatorStateInfo(0).normalizedTime>.40f){string key=d.ActiveBinding+"-"+d.Completed;if(captured.Add(key))Shot(key+".png");}
                if(active!=null)
                {
                    if(Time.frameCount-stepFrame<8||EditorApplication.timeSinceStartup-stepAt<.3)return;
                    if(active.WaitContact&&(d.Busy||d.QueuedCount>0||npc.Busy))return;
                    if(d.LastProblem!=null)throw new InvalidOperationException(d.LastProblem);
                    active=null;
                }
                if(steps.Count==0){Finish("PASS: all native-control steps completed");return;}
                active=steps.Dequeue();Log("STEP "+active.Name);active.Run();stepFrame=Time.frameCount;stepAt=EditorApplication.timeSinceStartup;
            }
            catch(Exception ex){Finish("FAILED: "+ex.Message);}
        }
        static void Click(string id){if(!work.ActivateControl(id))throw new InvalidOperationException("Unavailable native control "+id+" on "+work.ViewId);}
        static void Field(string id,string value){if(!work.SetFieldText(id,value))throw new InvalidOperationException("Unavailable native field "+id);}
        static void UIControl(string id){if(!ui.ActivateControl(id))throw new InvalidOperationException("Unavailable native control "+id+" on "+ui.SurfaceId);}
        static void UIField(string id,string value){if(!ui.SetFieldText(id,value))throw new InvalidOperationException("Unavailable native field "+id);}
        static void Check(bool condition,string detail){if(!condition)throw new InvalidOperationException(detail);Log("PASS "+detail);}
        static void Shot(string name){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name));}
        static void Log(string line){log.Add((EditorApplication.timeSinceStartup-start).ToString("F4")+" "+line);}
        static void Finish(string result){Result=result;Running=false;EditorApplication.update-=Tick;if(reviewCapture)Task5NativeCapture.End();Log(result);File.WriteAllLines(Path.Combine(folder,"runtime.txt"),log);}
    }
}
