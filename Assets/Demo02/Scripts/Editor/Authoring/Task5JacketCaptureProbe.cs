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
    /// <summary>Actual compact review controls and normal-time viewport capture. No pose override or transient material.</summary>
    public static class Task5JacketCaptureProbe
    {
        static PresentationView ui;static NpcInteractionDirector npc;static IEnumerator<Func<bool>> flow;static Func<bool> wait;static int frame;static double began;static string folder;
        static readonly List<string> log=new List<string>();public static bool Running {get;private set;}public static string Result {get;private set;}
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running||Directory.Exists(output))throw new InvalidOperationException("Idle explicit Play and new output required.");
            ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();folder=output;Directory.CreateDirectory(folder);log.Clear();began=EditorApplication.timeSinceStartup;frame=-1;wait=null;Result=null;flow=Scenario().GetEnumerator();Running=true;EditorApplication.update+=Tick;return "Started normal review idle/pointing and actual cadence capture.";
        }
        static IEnumerable<Func<bool>> Scenario()
        {
            ui.Lifecycle.Pause(false);ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute("review");ui.OpenReview();CampusDiagnostics.GameSize(1920,1080);yield return Frames(15);
            var renderer=npc.Lead.GetComponentInChildren<SkinnedMeshRenderer>();var material=renderer.sharedMaterial;var texture=(Texture2D)material.GetTexture("_BaseMap");
            Check(AssetDatabase.GetAssetPath(material)=="Assets/Demo02/Materials/Characters_v002/D02_ModuleLead_Body.mat"&&texture.wrapMode==TextureWrapMode.Clamp,"Saved original URP/Lit material and Clamp texture, no transient replacement");
            Check(Screen.width==1920&&Screen.height==1080,"Actual 1920 by 1080 viewport");Shot("idle-normal.png");yield return Frames(4);
            Task5NativeCapture.Begin(Path.Combine(folder,"frames"),15);UI("Review.Request");Check(ui.SetFieldText("Review.Summary","Please review this recorded current pair."),"Native summary");Check(ui.SetFieldText("Review.Time","13:40"),"Native time");UI("Primary");UI("Primary");
            var request=ui.Host.Session.Snapshot.ReviewHistory.Last().Id;Log("Real accepted request "+request);
            yield return Until(()=>npc.LeadPhase=="PointCurrentPair"&&npc.Lead.GetCurrentAnimatorStateInfo(0).normalizedTime>=.6f,"normal pointing");
            Check(npc.PresentedRequest==request,"Pointing belongs to accepted request");Shot("point-normal.png");yield return Frames(4);
            yield return Until(()=>npc.Requests.RecentSettlements.Any(x=>x.RequestId==request),"full return and settlement");Task5NativeCapture.End();yield return Until(()=>Task5NativeCapture.Pending==0,"capture flush");
            Check(npc.PacketOwner=="Desk"&&!npc.Busy&&!ui.Gameplay.ContactOwnsInput,"One packet returns to desk and controls restore");
            Check(ui.Host.Session.Snapshot.ApprovalHistory.Count==0&&ui.Host.Session.Snapshot.Publication==PublicationStatus.Draft,"No approval or release is created");
            Check(Task5NativeCapture.Error==null,"Timestamped actual viewport capture completed");Shot("returned-normal.png");yield return Frames(4);
            Log("Camera="+npc.ReviewCamera.position+"; material="+AssetDatabase.GetAssetPath(material)+"; texture="+AssetDatabase.GetAssetPath(texture)+"; format="+texture.format+"; mips="+texture.mipmapCount+"; wrap="+texture.wrapMode);
        }
        static void UI(string id){if(!ui.ActivateControl(id))throw new InvalidOperationException("Missing native control "+id);}
        static Func<bool> Frames(int count){int end=Time.frameCount+count;return ()=>Time.frameCount>=end;}
        static Func<bool> Until(Func<bool> test,string name){double end=EditorApplication.timeSinceStartup+20;return ()=>{if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(name);return test();};}
        static void Check(bool pass,string text){if(!pass)throw new InvalidOperationException(text);Log("PASS "+text);}
        static void Shot(string name){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name));Log("SHOT "+name+" queued at engine frame "+Time.frameCount+" phase "+npc.LeadPhase);}
        static void Log(string text)=>log.Add((EditorApplication.timeSinceStartup-began).ToString("F4")+" "+text);
        static void Tick(){try{if(!EditorApplication.isPlaying){Finish("Manual Play exit");return;}EditorApplication.QueuePlayerLoopUpdate();Task5NativeCapture.Tick(npc.Learner.VisiblePhase,npc.LeadPhase);if(frame==Time.frameCount)return;frame=Time.frameCount;if(wait!=null&&!wait())return;if(!flow.MoveNext()){Finish("PASS: normal review appearance and physical cycle complete");return;}wait=flow.Current;}catch(Exception e){Finish("FAILED: "+e.Message);}}
        static void Finish(string text){Running=false;EditorApplication.update-=Tick;Task5NativeCapture.End();Result=text;Log(text);File.WriteAllLines(Path.Combine(folder,"runtime.txt"),log);flow?.Dispose();}
    }
}
