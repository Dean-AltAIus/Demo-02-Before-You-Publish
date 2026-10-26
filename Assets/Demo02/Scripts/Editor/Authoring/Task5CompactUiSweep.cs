using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Altaius.Demo02.UI;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presenter;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit finite synthetic visual confirmation. Uses native input bindings and real room states.</summary>
    public static class Task5CompactUiSweep
    {
        class Shot { public string Name;public Action Open; }
        static Queue<Shot> queue=new Queue<Shot>();static List<object> rows=new List<object>();static List<string> checks=new List<string>();
        static PresentationView ui;static WorkbenchView w;static Shot shot;static string folder;static double started,opened;static int frame;static bool captured;
        public static bool Running{get;private set;}public static string Result{get;private set;}
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running||Directory.Exists(output))throw new InvalidOperationException("Idle Play and fresh folder required.");
            folder=output;Directory.CreateDirectory(folder);ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();w=ui.Workbench;queue.Clear();rows.Clear();checks.Clear();shot=null;
            foreach(int width in new[]{1280,1920})
            {
                int widthCopy=width,height=width*9/16;string prefix=width+"-100-";
                Add(prefix+"world",()=>{Scale(1,widthCopy,height);Fresh();w.CloseWindow();});
                Add(prefix+"source",()=>{ui.Gameplay.DirectRoute("archive");w.ActivateControl("Inspect.S_A");});
                Add(prefix+"editor",()=>{Ready();w.OpenTask();Check(w.SetFieldText("Task.Q1.Text","Compact UI native draft.\\nSecond line retained."),"Native task input accepted");var field=w.Rows.Find("Task.Q1.Text").Input;Check(EventSystem.current.currentSelectedGameObject==field.gameObject,"Input focus is native");w.CloseWindow();w.OpenTask();Check(w.Rows.Find("Task.Q1.Text").Input.text=="Compact UI native draft.\\nSecond line retained.","Task draft survives close/reopen");w.Scroll.verticalNormalizedPosition=0;w.Focus.EnsureVisible(w.Rows.Find("Task.Q1.Text").Input.gameObject);});
                Add(prefix+"marking",()=>{ui.Gameplay.DirectRoute("experiment");w.ActivateControl("Work.W_LIST");w.ActivateControl("Criterion.naming");Check(w.ViewId=="mark","Native marking opens");Check(w.SetFieldText("Mark.Rationale","Exact source and criterion remain available. Compact check."),"Mark rationale input");w.ActivateControl("ReadContext");Check(w.Scroll.verticalNormalizedPosition>.99f,"Read context returns to literal source");});
                Add(prefix+"conversation",()=>{Fresh();ui.Gameplay.DirectRoute("studio");w.CloseWindow();ui.OpenConversation(ActorRole.Colleague);Check(ui.PhysicalConversationVisible,"Actual at-station colleague composition");var s=ui.Surfaces.Single(x=>x.SurfaceId=="conversation");Check(s.Composer.placeholder.gameObject.activeSelf,"Composer placeholder available");Check(ui.SetFieldText("Composer","What do these sources support?"),"Conversation native text/caret");Check(s.Composer.text=="What do these sources support?","Literal composer input retained");});
                Add(prefix+"review",()=>{ui.CloseSurface(false);Task5VisualFixtures.ShowReview("pending");ui.Gameplay.DirectRoute("review");ui.OpenReview();Check(ui.PhysicalReviewRoomVisible,"Actual review room composition");});
                Add(prefix+"receipt",()=>{ui.OpenReviewRequest();ui.SetFieldText("Review.Summary","Synthetic compact UI receipt check.");ui.SetFieldText("Review.Time","13:40");ui.ActivateControl("Primary");Check(ui.SurfaceId=="confirmation","Review preview through actual native actions");ui.ActivateControl("Primary");Check(ui.SurfaceId=="receipt","Receipt through actual native confirmation");});
                Add(prefix+"coach",ui.OpenCoach);Add(prefix+"settings",()=>ui.OpenSettings());Add(prefix+"pause",()=>{ui.Pause();Check(ui.Lifecycle.Paused,"Pause active");});
                Add(prefix+"title",()=>{ui.Resume();ui.OpenTitle();});
                Add(prefix+"presenter",()=>{Fresh();w.CloseWindow();var p=UnityEngine.Object.FindFirstObjectByType<PresenterRunner>();p.StartSequence(p.Sequence,true);p.Pause();Check(p.Active&&p.Paused,"Presenter native pause");});
                Add(prefix+"presenter-reset",()=>{var p=UnityEngine.Object.FindFirstObjectByType<PresenterRunner>();p.ResetRun();p.Pause();Check(p.Active&&p.Paused&&p.StepIndex==0,"Presenter reset and pause");p.Cancel();Check(!p.Active,"Presenter cancel restores controls");});
            }
            Add("2560-100-world",()=>{Scale(1,2560,1440);Fresh();w.CloseWindow();});
            Add("1024-200-editor",()=>{Scale(2,1024,768);Ready();w.OpenRubric();});
            Add("1024-200-conversation",()=>{Fresh();ui.Gameplay.DirectRoute("studio");w.CloseWindow();ui.OpenConversation(ActorRole.Colleague);});
            Add("1280-200-presenter",()=>{Scale(2,1280,720);Fresh();w.CloseWindow();var p=UnityEngine.Object.FindFirstObjectByType<PresenterRunner>();p.StartSequence(p.Sequence,true);p.Pause();});
            Add("1280-100-restored",()=>{UnityEngine.Object.FindFirstObjectByType<PresenterRunner>().Cancel();Scale(1,1280,720);Fresh();w.CloseWindow();});
            started=EditorApplication.timeSinceStartup;Running=true;Result=null;EditorApplication.update+=Tick;return "Started finite compact UI confirmation, actual100% and200% logged per capture.";
        }
        public static string RunFinalReadback(string output)
        {
            if(!EditorApplication.isPlaying||Running||Directory.Exists(output))throw new InvalidOperationException("Idle Play and fresh folder required.");
            folder=output;Directory.CreateDirectory(folder);ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();w=ui.Workbench;queue.Clear();rows.Clear();checks.Clear();shot=null;
            Add("1920-100-world",()=>{Scale(1,1920,1080);Fresh();w.CloseWindow();});
            Add("1920-100-conversation",()=>{ui.Gameplay.DirectRoute("studio");ui.OpenConversation(ActorRole.Colleague);ui.SetFieldText("Composer","What do these sources support?");Check(ui.PhysicalConversationVisible,"At-station conversation");});
            Add("1920-100-review",()=>{ui.CloseSurface(false);Task5VisualFixtures.ShowReview("pending");ui.Gameplay.DirectRoute("review");ui.OpenReview();Check(ui.PhysicalReviewRoomVisible,"At-station review");});
            Add("1280-100-title",()=>{Scale(1,1280,720);ui.OpenTitle();});
            Add("1024-200-conversation",()=>{Scale(2,1024,768);Fresh();ui.Gameplay.DirectRoute("studio");ui.OpenConversation(ActorRole.Colleague);});
            Add("1280-200-presenter-fixed",()=>{Scale(2,1280,720);Fresh();w.CloseWindow();var p=UnityEngine.Object.FindFirstObjectByType<PresenterRunner>();p.StartSequence(p.Sequence,true);p.Pause();});
            Add("1280-100-restored",()=>{UnityEngine.Object.FindFirstObjectByType<PresenterRunner>().Cancel();Scale(1,1280,720);Fresh();w.CloseWindow();});
            started=EditorApplication.timeSinceStartup;Running=true;Result=null;EditorApplication.update+=Tick;return "Started focused status and title readback.";
        }
        static void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new InvalidOperationException(label);}
        static void Scale(float scale,int width,int height){CampusDiagnostics.GameSize(width,height);ui.Applied.TextScale=scale;w.SetTextScale(scale);foreach(var s in ui.Surfaces)s.ApplyScale(scale);}
        static void Fresh(){ui.Lifecycle.Pause(false);ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=true;}
        static void Ready(){Fresh();foreach(var id in new[]{"D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})ui.Host.Session.Inspect(id,InspectionLocation.Studio);ui.Host.NotifyChanged();ui.Gameplay.DirectRoute("studio");}
        static void Add(string name,Action open){queue.Enqueue(new Shot{Name=name,Open=open});}
        static object Rect(RectTransform r){var corners=new Vector3[4];r.GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);return new{name=r.name,active=r.gameObject.activeInHierarchy,logicalWidth=r.rect.width,logicalHeight=r.rect.height,x=a.x,y=a.y,width=b.x-a.x,height=b.y-a.y,areaFraction=(b.x-a.x)*(b.y-a.y)/(Screen.width*Screen.height)};}
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying){Finish("Manual Play exit");return;}if(EditorApplication.timeSinceStartup-started>190){Finish("FAIL bounded timeout");return;}
                EditorApplication.QueuePlayerLoopUpdate();
                if(shot==null){if(queue.Count==0){Finish("Completed bounded confirmation");return;}shot=queue.Dequeue();shot.Open();Canvas.ForceUpdateCanvases();opened=EditorApplication.timeSinceStartup;frame=Time.frameCount;captured=false;return;}
                if(Time.frameCount<frame+4||EditorApplication.timeSinceStartup-opened<.8)return;
                if(!captured)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,shot.Name+".png"));
                    var s=ui.Surfaces.FirstOrDefault(x=>x.SurfaceId==ui.SurfaceId);rows.Add(new{name=shot.Name,width=Screen.width,height=Screen.height,applied=ui.Applied.TextScale,workbench=w.TextScale,preference=PlayerPrefs.GetFloat("D02.preference.text",1),canvasScale=w.GetComponentInParent<Canvas>().scaleFactor,header=Rect(w.HudHeader),leena=Rect(w.LeenaDock),hints=Rect(w.WorldHelp),workspace=Rect((RectTransform)w.Window.transform),panel=s!=null?Rect(s.Frame):null,physicalConversation=ui.PhysicalConversationVisible,physicalReview=ui.PhysicalReviewRoomVisible});captured=true;opened=EditorApplication.timeSinceStartup;return;
                }
                if(EditorApplication.timeSinceStartup-opened>.2)shot=null;
            }
            catch(Exception ex){Finish("FAIL "+ex);}
        }
        static void Finish(string result){Running=false;Result=result;EditorApplication.update-=Tick;checks.Add(result);File.WriteAllLines(Path.Combine(folder,"behavior.txt"),checks);File.WriteAllText(Path.Combine(folder,"measurements.json"),Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));}
    }
}
