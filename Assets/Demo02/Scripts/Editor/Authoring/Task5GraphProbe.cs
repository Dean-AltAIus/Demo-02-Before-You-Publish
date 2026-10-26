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
    /// <summary>Finite Play-only interruption/state checks through existing native controls. Temporary obstruction is removed on every exit.</summary>
    public static class Task5GraphProbe
    {
        static PresentationView ui;static WorkbenchView work;static CampusContactDirector d;static NpcInteractionDirector npc;static PublicationCabinetProjection cabinet;
        static IEnumerator<Func<bool>> flow;static Func<bool> waiting;static string folder;static readonly List<string> log=new List<string>();static double began;static int frame;static GameObject obstacle;
        public static bool Running {get;private set;}public static string Result {get;private set;}
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running||Task5InteractionProbe.Running)throw new InvalidOperationException("Idle explicit Play session required.");
            ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();work=ui.Workbench;d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();cabinet=UnityEngine.Object.FindFirstObjectByType<PublicationCabinetProjection>();folder=output;Directory.CreateDirectory(folder);log.Clear();Result=null;waiting=null;frame=-1;began=EditorApplication.timeSinceStartup;flow=Scenario().GetEnumerator();Running=true;EditorApplication.update+=Tick;return "Started bounded pause/cancel/repeat/obstruction/reset/reduced/cabinet state checks.";
        }
        static IEnumerable<Func<bool>> Scenario()
        {
            Fresh("studio");yield return Frames(8);Click("Inspect.D02.task.v1.Q1");yield return Until(()=>d.VisiblePhase=="Contact","folio contact");
            var cover=d.Bindings.Single(b=>b.Id=="TaskOpen").Prop;var pose=Pose(d.Learner);var point=cover.position;var rotation=cover.rotation;int notebook=ui.Host.Session.Snapshot.Notebook.Count,audit=ui.Host.Session.Snapshot.Audit.Count;
            work.PauseButton.onClick.Invoke();yield return Seconds(.6);Check(PoseError(pose,d.Learner)<.000001f&&Vector3.Distance(point,cover.position)<.000001f&&Quaternion.Angle(rotation,cover.rotation)<.001f,"Learner and cover freeze while paused");UI("Primary");yield return Frames(2);
            ui.Gameplay.HandleEscape();yield return Until(()=>!d.Busy&&d.QueuedCount==0,"folio cancellation recovery");Check(!ui.Gameplay.ContactOwnsInput&&ui.Host.Session.Snapshot.Notebook.Count==notebook&&ui.Host.Session.Snapshot.Audit.Count==audit,"Cancel restores input and preserves actual inspection without new authority");
            Click("Inspect.D02.task.v1.Q1");yield return Until(()=>!d.Busy&&d.QueuedCount==0,"repeat folio open",true);Check(d.LastProblem==null,"Repeated folio open has a valid prop entry");
            Click("Primary");yield return Until(()=>d.VisiblePhase=="Contact","folio close contact");ui.Gameplay.DirectRoute("archive");yield return Frames(5);Check(!d.Busy&&!ui.Gameplay.ContactOwnsInput&&d.QueuedCount==0,"Direct route interrupts and restores input");
            ui.Pause();UI("Pause.Reset");UI("Primary");yield return Frames(8);Check(ui.Host.Session.Snapshot.Notebook.Count==0&&!d.Busy&&!npc.Busy&&npc.PacketOwner=="Desk","Native reset clears attempt and returns single props");
            ui.CloseSurface(false);ui.Applied.ReducedMotion=true;ui.Gameplay.DirectRoute("studio");yield return Frames(4);var motor=ui.Gameplay.Motor.transform.position;Click("Inspect.D02.task.v1.Q1");yield return Until(()=>!d.Busy&&d.QueuedCount==0,"reduced folio state",true);Check(Vector3.Distance(motor,ui.Gameplay.Motor.transform.position)<.002f&&!ui.Gameplay.ContactOwnsInput&&ui.Host.Session.Snapshot.Notebook.Any(p=>p.Reference.SourceId=="D02.task.v1.Q1"),"Reduced motion keeps actual inspection and skips contact travel");
            Fresh("studio");yield return Frames(6);var stance=d.Bindings.Single(b=>b.Id=="TaskOpen").Stance.position;var arrival=ui.Gameplay.Motor.transform.position;obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="Validation obstruction | transient";obstacle.hideFlags=HideFlags.HideAndDontSave;obstacle.transform.position=Vector3.Lerp(arrival,stance,.55f)+Vector3.up*.8f;obstacle.transform.localScale=new Vector3(.7f,1.6f,.7f);Physics.SyncTransforms();Click("Inspect.D02.task.v1.Q1");yield return Until(()=>!d.Busy&&d.LastProblem!=null,"collision-aware contact refusal",true);Check(!ui.Gameplay.ContactOwnsInput&&d.LastProblem.Contains("obstructed"),"Obstruction refuses physical approach without trapping input");UnityEngine.Object.DestroyImmediate(obstacle);obstacle=null;
            Fresh("review");yield return Frames(6);RequestReview();yield return Until(()=>npc.LeadPhase=="ReadPacket","reviewer held read");var leadPose=Pose(npc.Lead);point=npc.Packet.position;rotation=npc.Packet.rotation;int reviews=ui.Host.Session.Snapshot.ReviewHistory.Count;ui.Pause();yield return Seconds(.6);Check(PoseError(leadPose,npc.Lead)<.000001f&&Vector3.Distance(point,npc.Packet.position)<.000001f&&Quaternion.Angle(rotation,npc.Packet.rotation)<.001f,"Reviewer and owned packet freeze while paused");UI("Primary");yield return Frames(2);ui.Gameplay.HandleEscape();if(ui.Lifecycle.Paused)UI("Primary");yield return Until(()=>!npc.Busy&&!d.Busy,"reviewer cancel recovery");Check(npc.PacketOwner=="Desk"&&ui.Host.Session.Snapshot.ReviewHistory.Count==reviews&&ui.Host.Session.Snapshot.CurrentApproval==null,"Reviewer cancellation returns packet without undoing request or granting approval");
            Fresh("review");yield return Frames(6);ui.Applied.ReducedMotion=true;RequestReview();yield return Frames(12);Check(!d.Busy&&!npc.Busy&&npc.PacketOwner=="Desk"&&ui.Host.Session.Snapshot.ReviewHistory.Count==1,"Reduced review preserves original request and stable desk packet");
            foreach(var state in new[]{"locked","pending","approved","stale","released","hold"})
            {
                ui.Applied.ReducedMotion=false;Task5VisualFixtures.ShowReview(state);yield return Seconds(.8);bool released=state=="released";Check(cabinet.Released==released&&Mathf.Abs(cabinet.OpenAmount-(released?1:0))<.001f,"Cabinet exact receipt gate: "+state);Shot("cabinet-"+state+".png");
            }
            Task5VisualFixtures.ShowReview("released");yield return Frames(1);ui.Lifecycle.Pause(true);float amount=cabinet.OpenAmount;yield return Seconds(.5);Check(Mathf.Abs(cabinet.OpenAmount-amount)<.000001f,"Cabinet travel pauses");ui.Lifecycle.Pause(false);yield return Seconds(.7);Check(cabinet.OpenAmount==1,"Cabinet resumes to accepted release");Task5VisualFixtures.ShowReview("locked");ui.Applied.ReducedMotion=true;yield return Frames(5);Check(cabinet.OpenAmount==0&&!cabinet.Released,"Reset/reduced cabinet is closed for an unapproved pair");ui.Applied.ReducedMotion=false;
        }
        static void Fresh(string station){ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute(station);Log("Fresh synthetic "+station+" attempt");}
        static void RequestReview(){UI("Review.Request");Check(ui.SetFieldText("Review.Summary","Synthetic interruption check of this exact current pair."),"Review summary field");Check(ui.SetFieldText("Review.Time","13:40"),"Review time field");UI("Primary");UI("Primary");}
        static void Click(string id){if(!work.ActivateControl(id))throw new InvalidOperationException("Missing work control "+id+" on "+work.ViewId);}
        static void UI(string id){if(!ui.ActivateControl(id))throw new InvalidOperationException("Missing presentation control "+id+" on "+ui.SurfaceId);}
        static Func<bool> Frames(int count){int end=Time.frameCount+count;return ()=>Time.frameCount>=end;}
        static Func<bool> Seconds(double seconds){double end=EditorApplication.timeSinceStartup+seconds;return ()=>EditorApplication.timeSinceStartup>=end;}
        static Func<bool> Until(Func<bool> condition,string name,bool settle=false){double end=EditorApplication.timeSinceStartup+18;int earliest=Time.frameCount+(settle?8:1);return ()=>{if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(name+" | "+d.VisiblePhase+" | "+d.LastProblem+" | "+npc.LeadPhase);return Time.frameCount>=earliest&&condition();};}
        static Matrix4x4[] Pose(Animator actor)=>actor.GetComponentsInChildren<Transform>().Select(t=>t.localToWorldMatrix).ToArray();
        static float PoseError(Matrix4x4[] expected,Animator actor){var actual=Pose(actor);float error=0;for(int i=0;i<actual.Length;i++)for(int j=0;j<16;j++)error=Mathf.Max(error,Mathf.Abs(expected[i][j]-actual[i][j]));return error;}
        static void Check(bool pass,string message){if(!pass)throw new InvalidOperationException(message);Log("PASS "+message);}
        static void Shot(string name){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name));}
        static void Log(string message)=>log.Add((EditorApplication.timeSinceStartup-began).ToString("F4")+" "+message);
        static void Tick(){try{if(!EditorApplication.isPlaying){Finish("Manual Play exit");return;}if(EditorApplication.timeSinceStartup-began>180)throw new TimeoutException("Whole graph probe");EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;if(waiting!=null&&!waiting())return;if(!flow.MoveNext()){Finish("PASS: all graph checks completed");return;}waiting=flow.Current;}catch(Exception ex){Finish("FAILED: "+ex.Message);}}
        static void Finish(string result){if(obstacle)UnityEngine.Object.DestroyImmediate(obstacle);obstacle=null;Running=false;EditorApplication.update-=Tick;Result=result;Log(result);File.WriteAllLines(Path.Combine(folder,"graph-runtime.txt"),log);flow?.Dispose();}
    }
}
