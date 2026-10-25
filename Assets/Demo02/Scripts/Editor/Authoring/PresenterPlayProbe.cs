using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
using Altaius.Demo02.Presenter;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Actual native saved playback, with explicit stepping and no foreground or OS input.</summary>
    public static class PresenterPlayProbe
    {
        const string Folder="LocalOnly/Validation/Presenter/";
        static PresenterRunner runner;static int phase,previousStep,pausedFrames;static Vector3 pausedPosition;static string pausedText,normalPair;
        static Guid originalAttempt,resetAttempt;static float movementAtPause;static HashSet<string> shots;static readonly List<string> log=new List<string>();
        static readonly Dictionary<string,float> captureSince=new Dictionary<string,float>();static float directPace;static PresenterSequence directCopy;
        public static bool Running {get;private set;}
        public static string State=>"phase "+phase+" step "+(runner?runner.StepIndex:-1)+" "+(runner?runner.CurrentStep?.Label:"")+" | "+(runner?runner.LastError:"");
        public static void Run(float directValidationPace=3)
        {
            if(!EditorApplication.isPlaying||Running)throw new InvalidOperationException("Use the assigned existing Editor in one Play session.");
            runner=UnityEngine.Object.FindFirstObjectByType<PresenterRunner>();if(!runner)throw new InvalidOperationException("Author the saved presenter first.");
            Directory.CreateDirectory(Folder);log.Clear();shots=new HashSet<string>();captureSince.Clear();directPace=Mathf.Max(.1f,directValidationPace);phase=0;previousStep=-1;pausedFrames=0;originalAttempt=runner.UI.Host.Session.Snapshot.AttemptId;
            CampusDiagnostics.GameSize(1280,720);runner.StartSequence(runner.Sequence,false);Running=true;EditorApplication.update+=Tick;
            Log("Explicit stepped native playback. No OS input, foreground, live transport, player build or normal-speed/performance claim. Saved pace "+runner.Sequence.Pace+".");
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying){Stop("Play exited intentionally or externally. No restart was attempted.");return;}
            try
            {
                EditorApplication.Step();if(!runner.Active)throw new InvalidOperationException("Presenter stopped: "+runner.LastError);
                var ui=runner.UI;var s=ui.Host.Session.Snapshot;
                if(previousStep!=runner.StepIndex){previousStep=runner.StepIndex;Log(State+" | seconds "+runner.PresentedSeconds.ToString("F2")+" | world "+s.WorldRevision+" | pair "+s.Task.Version+"/"+s.Rubric.Version+" | annotations "+s.AnnotationHistory.Count+" | evaluations "+s.EvaluationHistory.Count);}
                if(phase==0)
                {
                    if(pausedFrames==0&&runner.CurrentStep?.Kind==PresenterStepKind.MoveStation&&runner.TravelledMetres>.3f){runner.Pause();pausedPosition=ui.Gameplay.Motor.transform.position;movementAtPause=runner.TravelledMetres;pausedFrames=1;}
                    else if(pausedFrames>0)
                    {pausedFrames++;if(pausedFrames==12){Check(Vector3.Distance(pausedPosition,ui.Gameplay.Motor.transform.position)<.005f,"Movement pause clears driver input and actual displacement");Check(ui.Gameplay.Motor.CurrentSpeed<.01f,"Pause reports idle speed");runner.Resume();}if(pausedFrames==18){runner.Cancel();Check(ui.Host.Session.Snapshot.AttemptId==originalAttempt,"Movement cancellation restores the original attempt");runner.StartSequence(runner.Sequence,true);phase=1;pausedFrames=0;previousStep=-1;}}
                    return;
                }
                if(phase==1)
                {
                    var field=ui.FindPresenterField("Composer");
                    if(pausedFrames==0&&runner.CurrentStep?.Kind==PresenterStepKind.TypePresentation&&field&&field.text.Length>=8){pausedText=field.text;runner.Pause();pausedFrames=1;}
                    else if(pausedFrames>0)
                    {pausedFrames++;if(pausedFrames==12){Check(field.text==pausedText,"Typing pause preserves exact native progressive content");runner.Resume();resetAttempt=s.AttemptId;runner.ResetRun();Check(ui.Host.Session.Snapshot.AttemptId!=resetAttempt,"Reset creates a fresh Presenter attempt");}if(pausedFrames==20){runner.Cancel();runner.StartSequence(runner.Sequence,false);phase=2;previousStep=-1;shots.Clear();}}
                    return;
                }
                Capture(ui,s);
                if(!runner.Completed)return;
                Check(s.Task.Version==2&&s.Rubric.Version==3,"Current pair is 2/3");Check(s.ApprovalHistory.Count==1&&s.ApprovalHistory[0].Pair.TaskVersion==2&&s.ApprovalHistory[0].Pair.RubricVersion==2&&s.CurrentApproval==null,"Actual displayed 2/2 approval is historical");
                Check(s.Audit.Last(a=>a.Receipt.Action=="release.request").Receipt.ReasonCode=="ExactCurrentApprovalRequired","Ordinary release confirmation reaches the exact closed gate");
                Check(s.Publication!=PublicationStatus.Released&&s.Publication!=PublicationStatus.Held&&s.HoldHistory.Count==0,"No release or responsible hold is invented");Check(s.Evidence.Count==0&&!s.HasLearnerEvidence,"Presenter creates no learner evidence");
                Check(s.AnnotationHistory.Count==10&&s.EvaluationHistory.Count==2,"One baseline and two selective contrasting retests, no duplicate artifacts");
                Check(s.Audit.Select(a=>a.Receipt.RequestId).Distinct().Count()==s.Audit.Count,"Each domain request submitted once");
                SaveRun(s,phase==2?"normal":"direct");
                if(phase==2){normalPair=s.Task.Digest+"/"+s.Rubric.Digest;Log("Normal route actual distance "+runner.TravelledMetres.ToString("F2")+" metres, actual stepped sequence "+runner.PresentedSeconds.ToString("F2")+" seconds.");Check(runner.TravelledMetres>10,"Normal route actually moved the actor");runner.Cancel();directCopy=UnityEngine.Object.Instantiate(runner.Sequence);directCopy.Pace=directPace;directCopy.hideFlags=HideFlags.HideAndDontSave;runner.StartSequence(directCopy,true);phase=3;previousStep=-1;shots.Clear();captureSince.Clear();}
                else{Check(normalPair==s.Task.Digest+"/"+s.Rubric.Digest,"Direct route has the exact same document outcome");runner.Cancel();Check(ui.Host.Session.Snapshot.AttemptId==originalAttempt&&!ui.Gameplay.PresenterOwnsInput,"Final handback restores session and ordinary input");Stop(null);}
            }
            catch(Exception error){Stop(error.ToString());}
        }
        static void Capture(PresentationView ui,SessionSnapshot s)
        {
            if(runner.StepIndex==0)Shot("campus");
            if(ui.SurfaceId=="conversation"&&ui.Lifecycle.Conversation.History.Any(r=>r.State==ReplyState.Shown))Shot("npc-reply");
            if(ui.Workbench.ViewId=="mark"&&ui.Host.Workbench.Mark?.Rationale.Length>25)Shot("progressive-mark");
            if(ui.Workbench.ViewId=="task"&&ui.Host.Workbench.Task?.Context.Length>30&&ui.Host.Workbench.Task.Context.Length<120)Shot("progressive-task-edit");
            if(ui.Workbench.ViewId=="preview"&&ui.Host.Workbench.Pending?.Command is EditTask)Shot("actual-task-preview");
            if(ui.SurfaceId=="receipt"&&ui.Surfaces.Single(x=>x.SurfaceId=="receipt").Rows.Find("Presenter.Observation"))Shot("baseline-counterexample");
            if(ui.SurfaceId=="receipt"&&runner.CurrentStep?.Control=="Presenter.Finding.2")
            {var row=ui.Surfaces.Single(x=>x.SurfaceId=="receipt").Rows.Find("Presenter.Finding.0");if(row)SettledShot(row.Caption.text.StartsWith("W_PITCH",StringComparison.Ordinal)?"pitch-findings":"compare-findings");}
            if(ui.SurfaceId=="receipt"&&runner.CurrentStep?.Kind==PresenterStepKind.Await&&ui.LastAuthorityReply?.Displayed==true&&s.ReviewStatus==ReviewStatus.Scheduled)SettledShot("scheduling-accepted");
            if(ui.SurfaceId=="receipt"&&runner.CurrentStep?.Kind==PresenterStepKind.Await&&s.CurrentApproval!=null)SettledShot("exact-2-2-approval-accepted");
            if(s.Task.Version==2&&s.Rubric.Version==3&&s.Audit.Any(a=>a.Receipt.Action=="release.request"))Shot("current-2-3-release-blocked");
            if(ui.SurfaceId=="debrief"&&runner.CurrentStep?.Control=="Debrief.Presenter")SettledShot("bounded-debrief");
        }
        static void SettledShot(string name)
        {if(!captureSince.ContainsKey(name)){captureSince[name]=runner.PresentedSeconds;return;}if(runner.PresentedSeconds-captureSince[name]>.25f)Shot(name);}
        static void Shot(string name)
        {if(!shots.Add(name))return;Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Folder+(phase==2?"normal-":"direct-")+name+".png");}
        static void SaveRun(SessionSnapshot s,string prefix)
        {
            var audit=s.Audit.Select(a=>new {sequence=a.Sequence,id=a.Receipt.RequestId,action=a.Receipt.Action,status=a.Receipt.Status.ToString(),reason=a.Receipt.ReasonCode,synthetic=a.Receipt.Synthetic,before=a.Receipt.BeforeRevision,after=a.Receipt.AfterRevision}).ToArray();
            File.WriteAllText(Folder+prefix+"-receipts.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {attempt=s.AttemptId,task=s.Task.Version,rubric=s.Rubric.Version,taskDigest=s.Task.Digest,rubricDigest=s.Rubric.Digest,seconds=runner.PresentedSeconds,metres=runner.TravelledMetres,evidence=s.Evidence.Count,publication=s.Publication.ToString(),audit},Newtonsoft.Json.Formatting.Indented));
        }
        static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException("FAILED: "+message);Log("PASS: "+message);}
        static void Log(string message){log.Add(DateTime.UtcNow.ToString("O")+" "+message);Directory.CreateDirectory(Folder);File.WriteAllLines(Folder+"native-playback.txt",log);}
        public static void Stop(string reason)
        {Running=false;EditorApplication.update-=Tick;if(reason!=null){Log(reason);if(runner&&runner.Active)runner.Cancel();}else Log("COMPLETE. Stop Play and verify saved scene separately. Realtime playback and performance remain unverified.");if(directCopy)UnityEngine.Object.DestroyImmediate(directCopy);directCopy=null;}
    }
}
