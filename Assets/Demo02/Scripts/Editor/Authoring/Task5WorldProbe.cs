using System;
using System.Collections.Generic;
using System.IO;
using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Finite native-motor walk and camera sweep. Input injection uses the existing presenter lease only.</summary>
    public static class Task5WorldProbe
    {
        sealed class Step{public string Name;public Action Begin;public float Seconds;public bool Capture;}
        static readonly Queue<Step> steps=new Queue<Step>();static readonly List<string> log=new List<string>();
        static PresentationView ui;static CampusGameplayFacade game;static Step active;static string folder;static double started,at;static bool captured;static int frame,lastFrame;
        public static bool Running{get;private set;}public static string Result{get;private set;}
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running||Directory.Exists(output))throw new InvalidOperationException("Idle Play and new evidence folder required.");
            folder=output;Directory.CreateDirectory(folder);ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();game=ui.Gameplay;steps.Clear();log.Clear();active=null;Result=null;
            Add("start",()=>{ui.CloseSurface(false);ui.Workbench.CloseWindow();ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;game.PresenterOwnsInput=true;CampusDiagnostics.GameSize(1280,720);game.CameraRig.Yaw=0;game.CameraRig.Pitch=20;},.8f,false);
            foreach(var station in new[]{"studio","archive","review","experiment"}){string id=station;Add(id+"-inspection",()=>game.DirectRoute(id),.8f,true);Add(id+"-world",()=>{ui.CloseSurface(false);ui.Workbench.CloseWindow();game.CloseInspection();},.8f,true);}
            Add("centre-return",()=>{game.Motor.GoTo(new Vector3(0,.03f,3),Quaternion.identity);game.CameraRig.SnapFollow();},.8f,false);
            for(int i=0;i<8;i++){int index=i;Add("centre-"+i,()=>{game.CameraRig.Yaw=index*45;game.CameraRig.Pitch=index%2==0?8:35;},.5f,true);}
            Add("walk-to-gate",()=>{game.CameraRig.Yaw=0;game.CameraRig.Pitch=20;game.DriveMovement(new Vector2(0,1));},10,true);
            Add("gate-hold",()=>game.DriveMovement(Vector2.zero),.5f,true);
            for(int i=0;i<4;i++){int index=i;Add("gate-camera-"+i,()=>{game.CameraRig.Yaw=index*90;game.CameraRig.Pitch=index%2==0?8:55;},.5f,true);}
            Add("walk-east-boundary",()=>{game.CameraRig.Yaw=0;game.CameraRig.Pitch=20;game.DriveMovement(Vector2.right);},11,true);
            Add("east-hold",()=>game.DriveMovement(Vector2.zero),.5f,true);
            for(int i=0;i<4;i++){int index=i;Add("east-corner-"+i,()=>{game.CameraRig.Yaw=index*90;game.CameraRig.Pitch=index%2==0?8:55;},.5f,true);}
            Add("walk-west-boundary",()=>{game.CameraRig.Yaw=0;game.CameraRig.Pitch=20;game.DriveMovement(Vector2.left);},18,true);
            Add("west-hold",()=>game.DriveMovement(Vector2.zero),.5f,true);
            for(int i=0;i<4;i++){int index=i;Add("west-corner-"+i,()=>{game.CameraRig.Yaw=index*90;game.CameraRig.Pitch=index%2==0?8:55;},.5f,true);}
            Add("near-wall-hidden",()=>{game.CameraRig.Yaw=90;game.CameraRig.Pitch=20;},.8f,true);
            Add("step-away",()=>game.DriveMovement(Vector2.up),1.4f,false);
            Add("restored-after-step",()=>game.DriveMovement(Vector2.zero),.6f,true);
            Add("assert-restored",()=>{if(game.CameraRig.BodyOccluded)throw new InvalidOperationException("Local body did not restore after step away.");},.1f,false);
            Add("arrival-return",()=>game.DirectRoute("studio"),.8f,true);
            Add("assert-inspection",()=>{if(game.CameraRig.BodyOccluded)throw new InvalidOperationException("Inspection hid local body.");ui.Pause();ui.ConfirmLifecycle("reset");ui.ActivateControl("Primary");},.8f,true);
            Add("assert-reset",()=>{if(game.CameraRig.BodyOccluded)throw new InvalidOperationException("Reset retained local body suppression.");},.1f,false);
            started=EditorApplication.timeSinceStartup;Running=true;lastFrame=-1;EditorApplication.update+=Tick;return "Started finite actual motor walk, station inspections and 360 camera sweep.";
        }
        static void Add(string name,Action begin,float seconds,bool capture){steps.Enqueue(new Step{Name=name,Begin=begin,Seconds=seconds,Capture=capture});}
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying){Finish("Manual Play exit, stopped");return;}if(EditorApplication.timeSinceStartup-started>140){Finish("FAILED timeout");return;}
                EditorApplication.QueuePlayerLoopUpdate();if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
                var p=game.Motor.transform.position;if(Mathf.Abs(p.x)>12.2||Mathf.Abs(p.z)>9.2||p.y<-.12)throw new InvalidOperationException("Escaped saved compact boundary: "+p);
                if(active==null){if(steps.Count==0){Finish("Completed movement/camera captures without boundary escape. Visual review required.");return;}active=steps.Dequeue();active.Begin();at=EditorApplication.timeSinceStartup;frame=Time.frameCount;captured=false;return;}
                if(EditorApplication.timeSinceStartup-at<active.Seconds||Time.frameCount<frame+4)return;
                if(!captured){log.Add(active.Name+" | player="+p.ToString("F4")+" | camera="+game.CameraRig.transform.position.ToString("F4")+" | speed="+game.Motor.CurrentSpeed+" | bodyOccluded="+game.CameraRig.BodyOccluded+" | distance="+Vector3.Distance(game.CameraRig.transform.position,p+Vector3.up*game.CameraRig.Height));if(active.Capture)ScreenCapture.CaptureScreenshot(Path.Combine(folder,active.Name+".png"));captured=true;at=EditorApplication.timeSinceStartup-active.Seconds;return;}
                if(EditorApplication.timeSinceStartup-at>active.Seconds+.25f)active=null;
            }
            catch(Exception error){Finish("FAILED "+error);}
        }
        static void Finish(string result){Result=result;Running=false;EditorApplication.update-=Tick;if(game)game.ReleasePresenter();log.Add(result);File.WriteAllLines(Path.Combine(folder,"runtime.txt"),log);}
    }
}
