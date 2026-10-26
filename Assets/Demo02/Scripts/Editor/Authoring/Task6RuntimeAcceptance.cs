using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Finite actual-frame acceptance through native controls. No OS input or frame stepping.</summary>
    public static partial class Task6RuntimeAcceptance
    {
        static PresentationView ui;static WorkbenchView view;static CampusContactDirector contacts;static NpcInteractionDirector npc;
        static string folder;static readonly List<string> log=new List<string>();static CampusProbeInput input;
        public static string Result {get;private set;}
        public static string State {get;private set;}
        public static void Bind(string output,bool simulateInput=true)
        {
            if(!Application.isPlaying||Directory.Exists(output))throw new InvalidOperationException("Explicit Play and new output required.");
            folder=output;Directory.CreateDirectory(folder);log.Clear();Result=null;
            ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();if(!ui)throw new InvalidOperationException("Saved UI absent in "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);view=ui.Workbench;contacts=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();
            if(simulateInput){input=ui.Gameplay.gameObject.AddComponent<CampusProbeInput>();input.Begin();}
            Log("Actual native InputSystem/uGUI test delivery. No OS/IME claim. No frame stepping, time-scale override, focus call or live service.");
        }
        public static IEnumerator Presenter(string output,bool direct)
        {
            Bind(output,false);CampusDiagnostics.GameSize(1920,1080);yield return Frames(15);
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;Apply();ui.CloseSurface(false);view.CloseWindow();
            var runner=UnityEngine.Object.FindFirstObjectByType<Altaius.Demo02.Presenter.PresenterRunner>();
            Log("Presenter route "+(direct?"direct; reduced travel":"spatial; normal motion")+" | saved pace "+runner.Sequence.Pace);
            yield return Shot("before-presenter-1920");yield return Seconds(5);
            yield return Task6PerformanceProbe.MeasureIsolated(Path.Combine(folder,"performance"),"warm-campus",20);
            runner.StartSequence(runner.Sequence,direct);int prior=-1;double began=Time.realtimeSinceStartupAsDouble;bool paused=false;
            while(runner.Active&&!runner.Completed)
            {
                if(Time.realtimeSinceStartupAsDouble-began>900)throw new TimeoutException("Actual presenter exceeded 900 seconds.");
                if(prior!=runner.StepIndex&&runner.CurrentStep!=null){prior=runner.StepIndex;Log("STEP "+prior+" | "+runner.CurrentStep.Kind+" | "+runner.CurrentStep.Label+" | "+S.Task.Version+"/"+S.Rubric.Version+" | "+S.ReviewStatus+" | "+S.Publication);}
                if(!paused&&runner.CurrentStep!=null&&runner.CurrentStep.Kind==Altaius.Demo02.Presenter.PresenterStepKind.TypeWorkbench)
                {runner.Pause();var selected=UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;int step=runner.StepIndex;yield return Seconds(.4f);Check(runner.StepIndex==step&&runner.Paused,"Presenter pause retains native typing step");runner.Resume();paused=true;}
                yield return null;
            }
            Check(runner.Completed&&runner.LastError==null,"Full actual presenter reaches completion");Log("Presenter measured seconds "+runner.PresentedSeconds+" | travelled metres "+runner.TravelledMetres+" | total wall "+(Time.realtimeSinceStartupAsDouble-began));
            Check(S.Task.Version==2&&S.Rubric.Version==3&&S.CurrentApproval==null&&S.Evidence.Count==0,"Current 2/3, historical approval and no learner evidence");
            yield return Shot("presenter-complete-1920");runner.Cancel();Check(!runner.Active&&S.Mode!=SessionMode.Presenter,"Presenter cancellation restores original learner session");
            Result="PASS actual presenter "+(direct?"direct":"spatial");Log(Result);
        }
        public static IEnumerator Learner(string output)
        {
            Bind(output);CampusDiagnostics.GameSize(1280,720);yield return Frames(12);
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;Apply();ui.OpenTitle();yield return Shot("title-1280");
            U("Primary");yield return Shot("brief-1280");U("Primary");yield return Until(()=>ui.SurfaceId=="onramp",10,"native loading");U("Secondary");yield return Frames(5);
            Check(!ui.IsOpen,"Optional controls return to campus with canonical Leena");Check(view.LeenaDock.gameObject.activeInHierarchy,"Leena persists");
            var start=ui.Gameplay.Motor.transform.position;input.Keys=new[]{Key.UpArrow};yield return Seconds(.7f);input.Keys=Array.Empty<Key>();yield return Frames(3);Check(Vector3.Distance(start,ui.Gameplay.Motor.transform.position)>.2f,"Native Up arrow moves actual CharacterController");
            yield return Route("studio");
            foreach(var source in new[]{"D02.outcome.O1","D02.outcome.O2","D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})
            {W("Inspect."+source);yield return Frames(3);if(source.EndsWith("Q1"))yield return Shot("literal-task-1280");W("Primary");}
            yield return Route("archive");foreach(var source in new[]{"S_A","S_B"}){W("Inspect."+source);yield return Frames(3);yield return Shot("source-"+source);W("Primary");}
            Check(S.Notebook.Count==8,"Only eight explicit source/outcome/folio passages admitted");
            yield return Route("experiment");W("Work.W_LIST");W("Criterion.naming");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,25,"passage placement");
            Check(view.Rows.Find("Mark.Marks").Input.text==""&&!view.Primary.interactable,"Empty numeric mark remains empty and cannot save");
            Check(((Text)view.Rows.Find("Mark.Marks").Input.placeholder).text.Contains("whole number"),"Numeric prompt is field-specific");
            yield return Type("Mark.Marks","11");yield return Type("Mark.Rationale","My own evidence explanation.");Check(!view.Primary.interactable&&view.Message.text.Contains("0 to 10"),"Out-of-range mark shows actual validation");
            yield return Type("Mark.Marks","10");
            const string words="Not certain \u2013 20 kg \u2014  not 2 kg.  {task_version} $1 <b>literal</b>";
            var actor=ui.Gameplay.Motor.transform.position;var camera=ui.Gameplay.CameraRig.transform.rotation;
            input.Keys=new[]{Key.W};input.MouseDelta=new Vector2(60,30);yield return Type("Mark.Rationale",words);yield return Frames(6);input.Keys=Array.Empty<Key>();
            Check(Vector3.Distance(actor,ui.Gameplay.Motor.transform.position)<.005f&&Quaternion.Angle(camera,ui.Gameplay.CameraRig.transform.rotation)<.15f,"Focused native text suppresses W and look leakage");
            W("Secondary");W("Criterion.naming");Check(view.Rows.Find("Mark.Rationale").Input.text==words,"Native reopen preserves literal punctuation, braces and whitespace");
            W("Primary");W("Primary");Check(S.AnnotationHistory.Count==1&&S.AnnotationHistory[0].Content.Rationale==words,"Repeated save reconciles one literal annotation");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,20,"actual pen marking");yield return Shot("mark-literal-1280");
            W("Secondary");W("Criterion.listing");yield return Type("Mark.Marks","10");yield return Type("Mark.Rationale","It lists features. This is my marking hypothesis.");W("Primary");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,25,"second criterion lane");W("Secondary");
            Check(S.DraftTotal("W_LIST")==20&&S.CurrentEvaluations.Count==0,"Actual 10 plus 10 remains an unverified hypothesis");yield return Shot("baseline-1280");
            yield return Route("studio");W("Editor.Task");yield return Type("Task.Q1.Text","Explain each source and its limitations.");yield return Type("Task.Q1.Marks","11");yield return Type("Task.Context",words);W("Task.Attach.S_A");W("Task.Attach.S_B");W("Primary");Check(view.ViewId=="task"&&view.Message.text.Contains("20"),"Incorrect arithmetic preserves task draft and explains the total");
            yield return Type("Task.Q1.Marks","10");W("Primary");yield return Shot("task-preview-1280");W("Secondary");Check(view.Rows.Find("Task.Context").Input.text==words,"Preview cancellation preserves own decision context");W("Primary");W("Primary");Check(S.Task.Version==2&&S.AnnotationHistory.Count==2&&S.CurrentAnnotations.Count==0,"New task edition preserves immutable baseline history");
            W("Secondary");foreach(var source in new[]{"D02.task.v2.Q1","D02.task.v2.Q2"}){W("Inspect."+source);W("Primary");}
            W("Editor.Rubric");yield return Type("Rubric.0.Text","Explain the nature and limits of the source.  Do not infer causation.");W("Primary");yield return Shot("rubric-preview-1280");W("Secondary");W("Primary");W("Primary");Check(S.Rubric.Version==2,"Free rubric prose creates a separate immutable edition");W("Secondary");
            foreach(var source in new[]{"D02.rubric.v2.naming","D02.rubric.v2.listing"}){W("Inspect."+source);W("Primary");}
            yield return Route("experiment");W("Work.W_COMPARE");W("Criterion.naming");yield return Type("Mark.Marks","7");yield return Type("Mark.Rationale","The source distinction supports a bounded comparison, not a causal claim.");W("Primary");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,25,"retest physical placement");yield return Shot("retest-1280");Check(S.CurrentAnnotations.Count==1&&S.AnnotationHistory.Count==3,"Retest creates current marking while earlier hypotheses remain historical");
            view.OpenHistory();yield return Shot("history-1280");
            yield return Route("review");U("Review.Request");yield return Type("Review.Summary",words,true);yield return Type("Review.Time","13:10",true);U("Primary");U("Primary");Check(ui.SurfaceId=="receipt"&&S.ReviewHistory.Count==0,"Past review time receives actual rejection without creating a review");yield return Shot("review-past-time-rejected");U("Primary");Check(ui.FindPresenterField("Review.Summary").text==words,"Rejected review returns literal preserved draft");yield return Type("Review.Time","13:40",true);U("Primary");U("Primary");yield return Shot("review-requested-1280");
            Check(S.ReviewStatus==ReviewStatus.Pending&&S.CurrentApproval==null,"Requested review grants no schedule or approval");U("Primary");U("Review.Schedule");yield return Frames(8);Check(S.ReviewStatus==ReviewStatus.Scheduled&&S.ClockMinute==820&&S.CurrentApproval==null,"Actual rendered scheduling response advances only the logical clock");yield return Shot("review-scheduled-1280");U("Primary");U("Review.Decision");yield return Frames(8);Check(S.ReviewStatus==ReviewStatus.Scheduled&&S.CurrentApproval==null,"Arbitrary own prose remains scheduled and unapproved when semantic review is unavailable");yield return Shot("review-unavailable-1280");
            U("Primary");U("Review.Release");U("Primary");Check(S.Publication==PublicationStatus.Draft,"Unapproved exact-pair release stays closed");yield return Shot("release-rejected-1280");U("Receipt.Details");yield return Shot("receipt-details-1280");
            ui.OpenReview();U("Review.Hold");yield return Type("Hold.Defect","A specific unresolved interpretation needs review.",true);yield return Type("Hold.Time","14:10",true);yield return Type("Hold.NextAction","I will verify the interpretation against the recorded sources.",true);U("Primary");U("Primary");U("Primary");U("Hold.Accept");U("Primary");Check(S.Publication==PublicationStatus.HoldCommittedUnverified&&S.HoldHistory.Last().SelfOwned,"Explicit learner hold is self-owned and unverified");yield return Shot("self-hold-1280");
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440)})
            {
                CampusDiagnostics.GameSize(size.x,size.y);yield return Frames(8);ui.OpenReview();yield return Shot("hold-"+size.x);ui.Applied.TextScale=2;ui.Applied.ReducedMotion=true;ui.Applied.Silent=true;ui.Applied.HighContrast=true;Apply();ui.OpenReview();yield return Shot("hold-"+size.x+"-text200");Check(Screen.width==size.x&&Screen.height==size.y,"Actual viewport "+size.x+"x"+size.y);Check(AudioListener.volume==0&&ui.Gameplay.CameraRig.ReducedMotion,"Silent and reduced-motion actual settings");ui.Applied.TextScale=1;Apply();
            }
            ui.OpenConversation(ActorRole.Colleague);yield return Type("Composer","What is your role?",true);ui.SendConversation();var request=ui.Lifecycle.Conversation.History.Last();ui.CancelConversation();yield return Seconds(.6f);Check(request.State==Altaius.Demo02.Integration.ReplyState.Cancelled&&ui.Lifecycle.Conversation.Draft(ActorRole.Colleague)=="What is your role?","Pending response cancellation retains words and suppresses delivery");
            ui.OpenCoach();ui.RequestHelp(Altaius.Demo02.Integration.HelpIntent.Controls);yield return Frames(8);Check(S.Assistance.Count>0,"Actual rendered optional control help is attributed");
            ui.Pause();var clock=S.ClockMinute;yield return Seconds(.3f);Check(S.ClockMinute==clock,"Pause does not advance logical time");ui.ConfirmLifecycle("linked");U("Primary");yield return Frames(8);Check(S.LinkedHistory.Count==1&&S.AnnotationHistory.Count==0,"Linked practice retains earlier history without carrying current marks");ui.EnterReplay();var audit=S.Audit.Count;yield return Shot("readonly-replay");Check(S.Mode==SessionMode.Replay&&S.Audit.Count==audit,"Replay is read-only");ui.ConfirmLifecycle("reset");U("Primary");yield return Frames(8);Check(S.LinkedHistory.Count==0&&S.AnnotationHistory.Count==0&&S.ReviewHistory.Count==0,"Full reset clears linked data and pending state");
            Result="PASS native learner flow";Log(Result);
        }
        static SessionSnapshot S=>ui.Host.Session.Snapshot;
        static void Apply()=>typeof(PresentationView).GetMethod("ApplyPreferences",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,new object[]{false});
        static IEnumerator Route(string station){ui.CloseSurface(false);view.OpenRoutes();W("Route."+station);yield return Frames(4);Check(ui.Gameplay.ActiveStation.StationId==station,"Native route "+station);}
        static void U(string id){Check(ui.ActivateControl(id),"Native presentation control "+id);}
        static void W(string id){Check(view.ActivateControl(id),"Native work control "+id);}
        static IEnumerator Type(string id,string text,bool presentation=false)
        {
            var field=presentation?ui.FindPresenterField(id):view.Rows.Find(id)?.Input;if(!field)throw new InvalidOperationException("Missing field "+id);
            field.Select();field.ActivateInputField();yield return Frames(2);field.text="";
            foreach(char c in text){field.ProcessEvent(new Event{type=EventType.KeyDown,character=c,keyCode=KeyCode.None});field.ForceLabelUpdate();}
            Check(field.text==text,"Native literal field "+id);yield return Frames(2);
        }
        public static IEnumerator Shot(string name){State=name;yield return Frames(4);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));Log("SHOT "+name+" | "+Screen.width+"x"+Screen.height+" | frame "+Time.frameCount);yield return Frames(4);}
        public static IEnumerator Frames(int n){for(int i=0;i<n;i++){if(!Application.isPlaying)throw new InvalidOperationException("Manual Play exit respected.");yield return null;}}
        static IEnumerator Seconds(float seconds){double until=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<until)yield return null;}
        static IEnumerator Until(Func<bool> condition,float seconds,string name){yield return Frames(2);double until=Time.realtimeSinceStartupAsDouble+seconds;while(!condition()){if(Time.realtimeSinceStartupAsDouble>until)throw new TimeoutException(name+" | "+contacts?.LastProblem);yield return null;}Log("SETTLED "+name);}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text+" | "+view?.Message?.text+" | "+ui?.SurfaceId);Log("PASS "+text);}
        static void Log(string text){log.Add(DateTime.UtcNow.ToString("O")+" "+text);File.WriteAllLines(Path.Combine(folder,"runtime.txt"),log);}
        public static void Cleanup(){if(input)UnityEngine.Object.DestroyImmediate(input);input=null;}
    }
}
