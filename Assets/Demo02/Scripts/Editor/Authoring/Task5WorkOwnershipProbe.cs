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
    /// <summary>Finite ordinary-control proof for empty-folio closing and single-sheet ownership.</summary>
    public static class Task5WorkOwnershipProbe
    {
        static PresentationView ui;static WorkbenchView work;static CampusContactDirector d;static Transform sheet,cover;
        static IEnumerator<Func<bool>> flow;static Func<bool> waiting;static string folder;static double began;static int frame;static Vector3 lanePosition;static Quaternion closedRotation;
        static readonly List<string> log=new List<string>();static readonly List<string> completed=new List<string>();
        public static bool Running {get;private set;}public static string Result {get;private set;}
        public static string Run(string output)
        {
            if(!EditorApplication.isPlaying||Running||Task5InteractionProbe.Running||Task5GraphProbe.Running||Directory.Exists(output))throw new InvalidOperationException("Idle Play and new evidence folder required.");
            ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();work=ui.Workbench;d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();sheet=d.Bindings.Single(x=>x.Id=="PlacePassage").Prop;cover=d.Bindings.Single(x=>x.Id=="WorkOpen").Prop;
            folder=output;Directory.CreateDirectory(folder);began=EditorApplication.timeSinceStartup;frame=-1;log.Clear();completed.Clear();Result=null;waiting=null;flow=Scenario().GetEnumerator();d.PresentationCompleted+=OnComplete;Running=true;EditorApplication.update+=Tick;return "Started actual annotation/close ownership, repeat, criterion/specimen change, cancel, reduced and reset checks.";
        }
        static void OnComplete(string id){completed.Add(id);Log("Completed "+id);}
        static IEnumerable<Func<bool>> Scenario()
        {
            ui.CloseSurface(false);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute("studio");yield return Frames(8);closedRotation=cover.rotation;
            foreach(var id in new[]{"D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"}){Click("Inspect."+id);yield return Settled();Click("Primary");yield return Settled();}
            ui.Gameplay.DirectRoute("experiment");yield return Frames(8);
            Click("Work.W_LIST");yield return Settled();Click("Criterion.naming");yield return Settled();lanePosition=sheet.position;Check(sheet.gameObject.activeSelf,"Single selected sheet is on the lane");
            Save("First accepted annotation, cancel only its presentation.");yield return Until(()=>d.ActiveBinding=="WorkOpen"&&d.VisiblePhase=="Contact","reverse open closing");CheckSheet();int count=ui.Host.Session.Snapshot.AnnotationHistory.Count;ui.Gameplay.HandleEscape();yield return Settled();CheckSheet();Check(!ui.Gameplay.ContactOwnsInput&&ui.Host.Session.Snapshot.AnnotationHistory.Count==count,"Cancel during closing returns input without duplicating annotation");
            Click("Criterion.naming");yield return Settled();Save("Second annotation, cancel during the physical pen indication.");yield return Until(()=>d.ActiveBinding=="MarkPoint"&&d.VisiblePhase=="Contact","marking contact");CheckSheet();Check(Quaternion.Angle(cover.rotation,closedRotation)<.01f,"Work cover closed before pen contact");count=ui.Host.Session.Snapshot.AnnotationHistory.Count;ui.Gameplay.HandleEscape();yield return Settled();CheckSheet();Check(!ui.Gameplay.ContactOwnsInput&&ui.Host.Session.Snapshot.AnnotationHistory.Count==count,"Cancel during marking preserves one accepted receipt");
            Click("Criterion.naming");yield return Settled();completed.Clear();Save("Third annotation of the same selected criterion.");yield return Settled();CheckSheet();Check(!completed.Contains("WorkOpen")&&completed.SequenceEqual(new[]{"MarkPoint"}),"Same criterion annotation does not reopen the empty folio");count=ui.Host.Session.Snapshot.AnnotationHistory.Count;completed.Clear();Save("Repeated same criterion marking.");yield return Settled();Check(completed.SequenceEqual(new[]{"MarkPoint"})&&ui.Host.Session.Snapshot.AnnotationHistory.Count==count+1,"Repeated mark runs once with no redundant folio motion");CheckSheet();Shot("same-criterion.png");yield return Frames(3);
            Click("Secondary");completed.Clear();Click("Criterion.listing");yield return Until(()=>d.ActiveBinding=="PlacePassage"&&d.VisiblePhase=="Contact","changed criterion return contact");ui.Gameplay.HandleEscape();yield return Settled();CheckSheet();completed.Clear();Click("Criterion.listing");yield return Settled();Check(completed.Count(x=>x=="PlacePassage")==2,"Retry after cancelled return completes both sheet transfer directions");CheckSheet();var lane=UnityEngine.Object.FindFirstObjectByType<SelectedCriterionLane>();Check(lane.SelectedCriterionId=="listing"&&lane.SelectedWorkId=="W_LIST","Changed criterion destination retains exact live identity");Shot("changed-criterion.png");yield return Frames(3);
            ui.Gameplay.DirectRoute("experiment");yield return Frames(6);Click("Work.W_COMPARE");yield return Settled();completed.Clear();Click("Criterion.naming");yield return Until(()=>completed.Count(x=>x=="PlacePassage")==1&&d.ActiveBinding=="PlacePassage"&&d.VisiblePhase=="Contact","changed specimen forward placement");ui.Gameplay.HandleEscape();yield return Settled();completed.Clear();Click("Criterion.naming");yield return Settled();Check(completed.Count(x=>x=="PlacePassage")==1&&lane.SelectedWorkId=="W_COMPARE","Retry after cancelled placement completes the same sheet with current specimen identity");CheckSheet();
            ui.Applied.ReducedMotion=true;count=ui.Host.Session.Snapshot.AnnotationHistory.Count;Save("Reduced motion preserves this current numeric annotation.");yield return Settled();CheckSheet();Check(ui.Host.Session.Snapshot.AnnotationHistory.Count==count+1&&Quaternion.Angle(cover.rotation,closedRotation)<.01f&&!ui.Gameplay.ContactOwnsInput,"Reduced marking closes the empty folio, retains sheet and returns input");Check(ui.Host.Session.Snapshot.CurrentEvaluations.Count==0,"Presentation creates no evaluation authority");
            ui.Applied.ReducedMotion=false;ui.Gameplay.DirectRoute("studio");yield return Frames(6);Click("Editor.Rubric");yield return Settled();Check(work.SetFieldText("Rubric.0.Text",ui.Host.Session.Snapshot.Rubric.Criteria[0].Description+" Clarified wording for this synthetic ownership check."),"Free wording edit preserves criterion ID");Click("Primary");yield return Frames(8);Click("Primary");yield return Settled();Check(ui.Host.Session.Snapshot.Rubric.Version==2&&ui.Host.Session.Snapshot.Rubric.Criteria[0].Id=="naming","New rubric edition retains the same criterion ID");Click("Secondary");yield return Settled();
            foreach(var id in new[]{"D02.rubric.v2.naming","D02.rubric.v2.listing"}){Click("Inspect."+id);yield return Settled();Click("Primary");yield return Settled();}
            ui.Gameplay.DirectRoute("experiment");yield return Frames(6);Click("Work.W_COMPARE");yield return Settled();completed.Clear();Click("Criterion.naming");yield return Settled();Check(completed.Count(x=>x=="EditionSlide")==1&&completed.Count(x=>x=="PlacePassage")==2&&lane.PairIdentity=="Task 1 / Rubric 2","Same IDs in a new edition restore the edition slide and re-place the current sheet");CheckSheet();Check(d.LastProblem==null,"No revised-edition contact refusal");Shot("same-ids-new-edition.png");yield return Frames(3);
            ui.Pause();UI("Pause.Reset");UI("Primary");yield return Frames(8);Check(!d.Busy&&d.QueuedCount==0&&!ui.Gameplay.ContactOwnsInput&&!sheet.gameObject.activeSelf&&Quaternion.Angle(cover.rotation,closedRotation)<.01f,"Native reset restores single-sheet and closed-folio initial state");ui.Applied.ReducedMotion=false;
        }
        static void Save(string rationale){Check(work.SetFieldText("Mark.Marks","10")&&work.SetFieldText("Mark.Rationale",rationale),"Actual numeric mark and rationale fields");Click("Primary");}
        static void CheckSheet(){Check(sheet.gameObject.activeSelf&&Vector3.Distance(sheet.position,lanePosition)<.00001f,"Placed sheet remains visible and stationary on its selected lane");}
        static void Click(string id){if(!work.ActivateControl(id))throw new InvalidOperationException("Missing work control "+id+" on "+work.ViewId);}
        static void UI(string id){if(!ui.ActivateControl(id))throw new InvalidOperationException("Missing UI control "+id);}
        static Func<bool> Frames(int count){int end=Time.frameCount+count;return ()=>Time.frameCount>=end;}
        static Func<bool> Settled()=>Until(()=>!d.Busy&&d.QueuedCount==0,"settled physical sequence",8);
        static Func<bool> Until(Func<bool> condition,string name,int frames=1){double end=EditorApplication.timeSinceStartup+25;int earliest=Time.frameCount+frames;return ()=>{if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(name+" | "+d.ActiveBinding+" | "+d.VisiblePhase+" | "+d.LastProblem);return Time.frameCount>=earliest&&condition();};}
        static void Check(bool passed,string message){if(!passed)throw new InvalidOperationException(message);Log("PASS "+message);}
        static void Log(string message)=>log.Add((EditorApplication.timeSinceStartup-began).ToString("F4")+" "+message);
        static void Shot(string name){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name));}
        static void Tick(){try{if(!EditorApplication.isPlaying){Finish("Manual Play exit");return;}if(EditorApplication.timeSinceStartup-began>180)throw new TimeoutException("Whole ownership probe");EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;if(waiting!=null&&!waiting())return;if(!flow.MoveNext()){Finish("PASS: all single-sheet ownership checks completed");return;}waiting=flow.Current;}catch(Exception e){Finish("FAILED: "+e.Message);}}
        static void Finish(string result){d.PresentationCompleted-=OnComplete;Running=false;EditorApplication.update-=Tick;Result=result;Log(result);File.WriteAllLines(Path.Combine(folder,"runtime.txt"),log);flow?.Dispose();}
    }
}
