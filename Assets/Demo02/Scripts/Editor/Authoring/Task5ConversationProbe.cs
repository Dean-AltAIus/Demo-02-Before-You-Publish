using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5ConversationProbe
    {
        static PresentationView ui;static NpcInteractionDirector npc;static CampusContactDirector learner;static IEnumerator<Func<bool>> flow;static Func<bool> waiting;static string folder;static double began;static int frame;static readonly List<string> log=new List<string>();
        public static bool Running{get;private set;}public static string Result{get;private set;}
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running)throw new InvalidOperationException("Idle explicit Play session required.");ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();learner=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();folder=output;Directory.CreateDirectory(folder);log.Clear();began=EditorApplication.timeSinceStartup;frame=-1;waiting=null;Result=null;flow=Scenario().GetEnumerator();Task5NativeCapture.Begin(Path.Combine(folder,"frames"),8);Running=true;EditorApplication.update+=Tick;return "Started actual source, both station roles, Leena delivery and camera handback capture.";
        }
        static IEnumerable<Func<bool>> Scenario()
        {
            ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Applied.TextScale=1;ui.Workbench.SetTextScale(1);CampusDiagnostics.GameSize(1280,720);ui.Gameplay.DirectRoute("studio");yield return Seconds(.5);
            ui.Workbench.ActivateControl("Inspect.D02.task.v1.Q1");yield return Seconds(2.8);Check(npc.ColleaguePhase=="Idle"||npc.ColleaguePhase=="SourcePoint","Actual source gesture observed in captured frames");yield return Until(()=>!learner.Busy);
            ui.Workbench.CloseWindow();ui.OpenConversation(ActorRole.Colleague);yield return Seconds(1);Check(ui.PhysicalConversationVisible&&ui.Gameplay.CameraRig.ContactView==npc.ColleagueConversationCamera,"Nearby colleague visible in normal conversation");
            ui.SetFieldText("Composer","What is your role?");ui.SendConversation();yield return Seconds(3.8);Check(ui.Lifecycle.Conversation.History.Any(r=>r.Request.Recipient==ActorRole.Colleague&&r.DisplayId!=null&&r.Reply.Available),"Actual colleague reply rendered and acknowledged");
            ui.CloseSurface();yield return Seconds(.4);Check(!ui.Gameplay.CameraRig.Inspecting&&!ui.Gameplay.CameraRig.ContactFraming,"Colleague close restores normal camera");
            ui.Gameplay.DirectRoute("review");ui.OpenConversation(ActorRole.ModuleLead);yield return Seconds(1);Check(ui.PhysicalConversationVisible&&ui.Gameplay.CameraRig.ContactView==npc.ReviewCamera,"Nearby seated reviewer visible in normal conversation");ui.SetFieldText("Composer","What is your role?");ui.SendConversation();yield return Seconds(3.8);Check(ui.Lifecycle.Conversation.History.Any(r=>r.Request.Recipient==ActorRole.ModuleLead&&r.DisplayId!=null&&r.Reply.Available),"Actual module reviewer reply rendered and acknowledged");
            ui.Applied.TextScale=2;ui.Workbench.SetTextScale(2);ui.OpenConversation(ActorRole.ModuleLead);yield return Seconds(1);Check(!ui.PhysicalConversationVisible&&ui.Surfaces.Single(s=>s.SurfaceId=="conversation").Frame.anchorMin.x<.1f,"200 percent conversation reflows to scrollable full width");
            ui.Applied.TextScale=1;ui.Workbench.SetTextScale(1);ui.CloseSurface();ui.Gameplay.DirectRoute("archive");ui.Workbench.CloseWindow();ui.OpenConversation(ActorRole.Colleague);yield return Seconds(.4);Check(!ui.PhysicalConversationVisible,"Distant role has no invented local camera");ui.CloseSurface();
            ui.OpenCoach();ui.RequestHelp(HelpIntent.Controls);yield return Seconds(8.5);Check(ui.Host.Session.Snapshot.Assistance.Count>0,"Actual help delivery retained");ui.OpenDebrief();yield return Seconds(8.5);ui.CloseSurface();
            ui.Pause();ui.ActivateControl("Pause.Reset");ui.ActivateControl("Primary");yield return Seconds(.5);Check(!ui.Gameplay.CameraRig.ContactFraming&&!ui.Gameplay.ContactOwnsInput&&npc.PacketOwner=="Desk","Reset restores camera, input and packet");
        }
        static Func<bool> Seconds(double seconds){double end=EditorApplication.timeSinceStartup+seconds;return ()=>EditorApplication.timeSinceStartup>=end;}
        static Func<bool> Until(Func<bool> condition){double end=EditorApplication.timeSinceStartup+12;return ()=>{if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Contact did not settle");return condition();};}
        static void Check(bool pass,string message){if(!pass)throw new InvalidOperationException(message);log.Add("PASS "+message);}
        static void Tick(){try{if(!EditorApplication.isPlaying){Finish("Manual Play exit");return;}if(EditorApplication.timeSinceStartup-began>90)throw new TimeoutException("Conversation probe");EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;Task5NativeCapture.Tick(learner.ActiveBinding+":"+learner.VisiblePhase,npc.ColleaguePhase+"/"+npc.LeadPhase+"/"+ui.Workbench.LeenaDock.GetComponent<LeenaHudProjection>().VisibleState);if(waiting!=null&&!waiting())return;if(!flow.MoveNext()){Finish("PASS: normal role visibility, actual delivery, camera restoration and Leena capture completed");return;}waiting=flow.Current;}catch(Exception ex){Finish("FAILED "+ex.Message);}}
        static void Finish(string result){Running=false;EditorApplication.update-=Tick;Result=result;Task5NativeCapture.End();log.Add(result);File.WriteAllLines(Path.Combine(folder,"runtime.txt"),log);flow?.Dispose();}
    }
}
