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
    // Finite Play-only use of native review controls; changed-pair concurrency is explicitly synthetic reducer setup.
    public static class Task5ReviewQueueProbe
    {
        static PresentationView ui;static NpcInteractionDirector npc;static CampusContactDirector learner;
        static IEnumerator<Func<bool>> flow;static Func<bool> wait;static string folder;static double began;static int frame;
        static readonly List<string> log=new List<string>();static readonly List<ReviewPresentationQueue.Settlement> settlements=new List<ReviewPresentationQueue.Settlement>();
        public static bool Running {get;private set;}public static string Result {get;private set;}
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running||Directory.Exists(output))throw new InvalidOperationException("Explicit idle Play and new evidence directory required.");
            ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();learner=npc.Learner;
            folder=output;Directory.CreateDirectory(folder);log.Clear();began=EditorApplication.timeSinceStartup;frame=-1;wait=null;Result=null;Running=true;flow=Scenario().GetEnumerator();EditorApplication.update+=Tick;return "Started per-request native-control lifecycle proof.";
        }
        static IEnumerable<Func<bool>> Scenario()
        {
            foreach(var phase in new[]{"ReadPacket","PointCurrentPair","ReturnPacket"})
            {
                Fresh();yield return Frames(8);Track();var a=Request();yield return Until(()=>npc.LeadPhase==phase,"A reaches "+phase);int av=npc.PresentedRubricVersion;
                if(phase=="PointCurrentPair")
                {
                    var s=ui.Host.Session;var receipt=s.Submit(s.Stage(new EditRubric(s.Snapshot.Rubric.Version,s.Snapshot.Rubric.Criteria),"Synthetic concurrent edition change for lifecycle validation"));
                    Check(receipt.Status==ReceiptStatus.Accepted,"Synthetic changed-pair setup accepted without route exit");ui.Host.NotifyChanged();
                }
                var b=Request();yield return Frames(1);Check(npc.PresentedRequest==a&&npc.PresentedRubricVersion==av,"Active A identity/pair retained while B accepted in "+phase);
                if(phase=="ReadPacket")
                {
                    ui.Pause();var packet=npc.Packet.localToWorldMatrix;var active=npc.PresentedRequest;yield return Seconds(.5);
                    Check(npc.PresentedRequest==active&&MatrixError(packet,npc.Packet.localToWorldMatrix)<.000001f,"Pause freezes packet and active request");UI("Primary");
                }
                yield return Until(()=>settlements.Any(x=>x.RequestId==a),"A settlement");
                Log("B learner readiness when A settles: "+(npc.Requests.Next?.Ready.ToString()??"already active"));
                yield return Until(()=>settlements.Any(x=>x.RequestId==b),"B settlement");
                Check(settlements.Select(x=>x.RequestId).SequenceEqual(new[]{a,b})&&settlements.All(x=>x.Reason=="Presented"),"A then B each physically presented once");
                Check(ui.Host.Session.Snapshot.ReviewHistory.Count==2&&ui.Host.Session.Snapshot.ApprovalHistory.Count==0&&ui.Host.Session.Snapshot.Publication==PublicationStatus.Draft,"Two real requests, no approval/release authority");
                Check(npc.PacketOwner=="Desk"&&!ui.Gameplay.ContactOwnsInput,"Single packet Desk ownership and controls restored");Shot(phase+"-complete.png");yield return Frames(3);
            }
            Fresh();yield return Frames(8);Track();var batched=new[]{Request(),Request(),Request()};yield return Frames(1);
            Check(npc.Requests.PendingCount==3,"Three unseen native accepted audits retained independently");
            yield return Until(()=>settlements.Count==3,"Three queued presentations",35);Check(settlements.Select(x=>x.RequestId).SequenceEqual(batched),"Unseen batch exact receipt order");
            foreach(var mode in new[]{"cancel","route"})
            {
                Fresh();yield return Frames(8);Track();var a=Request();yield return Until(()=>npc.LeadPhase=="ReadPacket","active request before "+mode);
                var backlog=Enumerable.Range(0,11).Select(_=>Request()).ToArray();yield return Frames(1);
                Check(npc.Requests.PendingCount<=ReviewPresentationQueue.Capacity&&npc.Requests.DeferredCount(ui.Host.Session.Snapshot)>0,"Bounded queue retains deferred audit backlog");
                if(mode=="cancel"){ui.Gameplay.HandleEscape();if(ui.Lifecycle.Paused)UI("Primary");}else ui.Gameplay.DirectRoute("archive");
                yield return Until(()=>!npc.Busy&&!learner.Busy,"interruption handback");
                Check(settlements.Count==12&&settlements.Select(x=>x.RequestId).Distinct().Count()==12,"All loaded/deferred requests explicitly settled on "+mode);
                ui.Gameplay.DirectRoute("review");yield return Frames(8);Check(npc.Requests.PendingCount==0&&!npc.Busy,"Re-entry does not replay settled backlog");
                var fresh=Request();yield return Until(()=>settlements.Any(x=>x.RequestId==fresh),"new request after "+mode);
                Check(settlements.Last().Reason=="Presented"&&ui.Host.Session.Snapshot.ReviewHistory.Count==13,"New accepted request after handback presents normally");
            }
            Fresh();yield return Frames(8);Track();Request();yield return Until(()=>npc.LeadPhase=="ReadPacket","active before reduced motion");Request();ui.Applied.ReducedMotion=true;
            yield return Until(()=>settlements.Count==2,"reduced motion settlements");Check(npc.PacketOwner=="Desk"&&!npc.Busy&&!ui.Gameplay.ContactOwnsInput,"Reduced motion settles both real requests at desk");
            Check(settlements.All(x=>x.Reason.StartsWith("Reduced motion")),"Reduced motion reason is explicit per request");
            Fresh();yield return Frames(8);Track();Request();yield return Until(()=>npc.LeadPhase=="ReadPacket","active before reset");Request();yield return Frames(2);
            ui.Pause();UI("Pause.Reset");UI("Primary");yield return Frames(8);
            Check(settlements.Count==2&&settlements.All(x=>x.Reason.StartsWith("Attempt reset")),"Reset emits prior request settlements");
            Check(ui.Host.Session.Snapshot.ReviewHistory.Count==0&&!npc.Busy&&npc.Requests.PendingCount==0&&npc.PacketOwner=="Desk"&&!ui.Gameplay.ContactOwnsInput,"Reset leaves clean attempt, single desk packet and controls");
        }
        static void Fresh(){ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute("review");Log("Fresh synthetic Presenter attempt; review requests use native forms.");}
        static void Track(){settlements.Clear();npc.Requests.Settled+=x=>{settlements.Add(x);Log("SETTLED "+x.RequestId+" | "+x.Reason);};}
        static Guid Request(){ui.OpenReview();UI("Review.Request");Check(ui.SetFieldText("Review.Summary","Exact-pair lifecycle validation, no semantic grade."),"Summary field");Check(ui.SetFieldText("Review.Time","13:40"),"Time field");UI("Primary");UI("Primary");var r=ui.Host.Session.Snapshot.ReviewHistory.Last();Log("ACCEPTED "+r.Id+" pair "+r.Pair.TaskVersion+"/"+r.Pair.RubricVersion+" active "+npc.PresentedRequest+" phase "+npc.LeadPhase);return r.Id;}
        static void UI(string id){if(!ui.ActivateControl(id))throw new InvalidOperationException("Missing native control "+id+" on "+ui.SurfaceId);}
        static Func<bool> Frames(int n){int end=Time.frameCount+n;return ()=>Time.frameCount>=end;}
        static Func<bool> Seconds(double n){double end=EditorApplication.timeSinceStartup+n;return ()=>EditorApplication.timeSinceStartup>=end;}
        static Func<bool> Until(Func<bool> condition,string name,double timeout=20){double end=EditorApplication.timeSinceStartup+timeout;int earliest=Time.frameCount+1;return ()=>{if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(name+" | "+npc.LeadPhase+" | "+learner.VisiblePhase+" | "+learner.LastProblem);return Time.frameCount>=earliest&&condition();};}
        static float MatrixError(Matrix4x4 a,Matrix4x4 b){float e=0;for(int i=0;i<16;i++)e=Mathf.Max(e,Mathf.Abs(a[i]-b[i]));return e;}
        static void Check(bool pass,string text){if(!pass)throw new InvalidOperationException(text);Log("PASS "+text);}
        static void Shot(string file){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,file));}
        static void Log(string text)=>log.Add((EditorApplication.timeSinceStartup-began).ToString("F4")+" "+text);
        static void Tick(){try{if(!EditorApplication.isPlaying){Finish("Manual Play exit");return;}if(EditorApplication.timeSinceStartup-began>240)throw new TimeoutException("Whole probe");EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;if(wait!=null&&!wait())return;if(!flow.MoveNext()){Finish("PASS: per-request lifecycle cases complete");return;}wait=flow.Current;}catch(Exception ex){Finish("FAILED: "+ex.Message);}}
        static void Finish(string text){Running=false;EditorApplication.update-=Tick;Result=text;Log(text);File.WriteAllLines(Path.Combine(folder,"runtime.txt"),log);flow?.Dispose();}
    }
}
