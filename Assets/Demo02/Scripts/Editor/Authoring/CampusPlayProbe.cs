using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Altaius.Demo02.Authoring
{
    /// <summary>Editor-only native control validation. No domain mutation, successful repair preset or shipping presenter logic.</summary>
    public static class CampusPlayProbe
    {
        sealed class Step {public Action Run;public double Delay;}
        static readonly Queue<Step> steps=new Queue<Step>();
        static readonly List<string> log=new List<string>();
        static double next;
        static WorkbenchView view;
        static CampusGameplayFacade game;
        static Vector3 initial,typingPosition;
        static Quaternion typingCamera;
        static CampusProbeInput probeInput;
        static string path="LocalOnly/Validation/Gameplay/native-ui-probe.txt";
        public static bool Running {get;private set;}
        public static void Run(bool keyboardEdges=false)
        {
            if(!EditorApplication.isPlaying || Running)throw new InvalidOperationException("Start one Play session before this probe.");
            view=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();game=view.Gameplay;steps.Clear();log.Clear();Running=true;
            probeInput=game.gameObject.AddComponent<CampusProbeInput>();probeInput.Begin();
            Add(()=> {CampusDiagnostics.GameSize(1280,720);view.CloseWindow();initial=game.Motor.transform.position;Log("Start: explicitly stepped Editor playback. Native InputSystem keyboard and uGUI Submit/ProcessEvent. No OS input or direct domain writes. This does not establish realtime motion or performance.");},.5);
            Add(()=>QueueKeys(Key.UpArrow),.7);Add(()=>QueueKeys(),.2);
            Add(()=>Check(Vector3.Distance(initial,game.Motor.transform.position)>.5f,"Up arrow moves the learner through CharacterController"));
            Add(()=>Shot("play-campus-1280.png"),.3);
            Add(()=>{if(keyboardEdges)QueueKeys(Key.Tab);else Submit(view.RoutesButton);},.2);Add(()=>QueueKeys(),.2);
            Add(()=>Check(view.ViewId=="routes",keyboardEdges?"Tab opens direct routes":"Native uGUI Submit opens direct routes. Tab edge remains unverified in stepped Editor."));
            Add(()=>{if(keyboardEdges)QueueKeys(Key.Enter);else Row("Route.studio");},.2);Add(()=>QueueKeys(),.4);
            Add(()=>Check(game.ActiveStation && game.ActiveStation.StationId=="studio",keyboardEdges?"Enter activates focused studio route":"Native uGUI Submit activates the studio route. OS keyboard not exercised."));
            foreach(var id in new[]{"D02.outcome.O1","D02.outcome.O2","D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})
            {var source=id;Add(()=>Row("Inspect."+source));Add(()=>Submit(view.Primary));}
            Add(()=>Check(game.SessionHost.Session.Snapshot.Notebook.Count==6,"Six deliberate studio inspections admit only six passages"));
            Add(()=>Shot("play-studio-1280.png"));
            Route("archive");
            foreach(var id in new[]{"S_A","S_B"}){var source=id;Add(()=>Row("Inspect."+source));Add(()=>Submit(view.Primary));}
            Add(()=>Check(game.SessionHost.Session.Snapshot.Notebook.Count==8,"Archive sources admitted separately"));
            Route("experiment");Add(()=>Row("Work.W_LIST"));Add(()=>Row("Criterion.naming"));
            Add(()=>Check(!view.Primary.interactable,"Empty mark and rationale disable Save"));
            Type("Mark.Marks","10");Type("Mark.Rationale","The work names the facilities count and supplier brochure.",true);
            Add(()=>Shot("play-marking-1280.png"));Add(()=>Submit(view.Primary));Add(()=>Submit(view.Primary));
            Add(()=>Check(game.SessionHost.Session.Snapshot.AnnotationHistory.Count==1,"Repeated save reconciles the same annotation receipt"));
            Add(()=>Submit(view.Secondary));Add(()=>Row("Criterion.listing"));
            Type("Mark.Marks","10");Type("Mark.Rationale","The work lists method, dates and a relevant sample.");Add(()=>Submit(view.Primary));
            Add(()=>Submit(view.Secondary));
            Add(()=>{var s=game.SessionHost.Session.Snapshot;Check(s.DraftTotal("W_LIST")==20 && s.CurrentEvaluations.Count==0,"Actual UI W_LIST total 20 remains a hypothesis with zero grounded evaluations");Shot("play-baseline-total-1280.png");});
            Route("studio");Add(()=>Row("Editor.Task"));
            Type("Task.Q1.Text","Explain two information sources in this case.");Type("Task.Context","Decide which source could inform an entrance review.");
            Add(()=>Row("Task.Attach.S_A"));Add(()=>Row("Task.Attach.S_B"));
            Add(()=>Submit(view.Primary));Add(()=>Submit(view.Secondary));
            Add(()=>Check(view.Rows.Find("Task.Q1.Text").Input.text=="Explain two information sources in this case.","Cancel preview retains free task wording"));
            Add(()=>Submit(view.Primary));Add(()=>Shot("play-task-preview-1280.png"));Add(()=>Submit(view.Primary));
            Add(()=>{var s=game.SessionHost.Session.Snapshot;Check(s.Task.Version==2 && s.CurrentAnnotations.Count==0 && s.AnnotationHistory.Count==2,"Free task edit creates Task 2 and makes both baseline annotations historical");});
            Add(()=>Submit(view.HistoryButton));Add(()=>Shot("play-history-1280.png"));
            Add(()=>CampusDiagnostics.GameSize(1920,1080),.5);Add(()=>Shot("play-history-1920.png"));
            Add(()=>Submit(view.TextSizeButton));Add(()=>Shot("play-history-1920-text200.png"));
            Add(()=>CampusDiagnostics.GameSize(1280,720),.5);Add(()=>Shot("play-history-1280-text200.png"));
            Add(()=>Submit(view.WorkspaceTextSizeButton));
            Add(()=>{view.CloseWindow();Check(!game.CameraRig.Inspecting,"Close restores ordinary camera ownership");Log("COMPLETE. Scene still in Play; stop separately and verify saved persistence.");});
            next=Time.unscaledTime;EditorApplication.update+=Tick;
        }
        static void Route(string id){Add(()=>Submit(view.RoutesButton));Add(()=>Row("Route."+id),.4);}
        static void Type(string id,string value,bool testMovement=false)
        {
            Add(()=> {var input=view.Rows.Find(id).Input;input.Select();input.ActivateInputField();input.text="";view.Focus.EnsureVisible(input.gameObject);},.15);
            if(testMovement)Add(()=>{typingPosition=game.Motor.transform.position;typingCamera=game.CameraRig.transform.rotation;QueueKeys(Key.W);probeInput.MouseDelta=new Vector2(60,30);});
            foreach(var character in value){char c=character;Add(()=>{var input=view.Rows.Find(id).Input;input.ProcessEvent(new Event{type=EventType.KeyDown,character=c,keyCode=KeyCode.None});input.ForceLabelUpdate();},.018);}
            Add(()=>Check(view.Rows.Find(id).Input.text==value,"Native text entry retained in "+id));
            if(testMovement)Add(()=> {QueueKeys();Check(Vector3.Distance(typingPosition,game.Motor.transform.position)<.005f && Quaternion.Angle(typingCamera,game.CameraRig.transform.rotation)<.1f,"Typing with W held and mouse delta does not move player or inspection camera");});
        }
        static void Add(Action action,double delay=.15)=>steps.Enqueue(new Step{Run=action,Delay=delay});
        static void QueueKeys(params Key[] keys){if(probeInput)probeInput.Keys=keys;}
        static void Row(string id){var row=view.Rows.Find(id);if(!row || !row.Action)throw new InvalidOperationException("Missing native control "+id);Submit(row.Action);}
        static void Submit(Button button)
        {
            if(!button || !button.gameObject.activeInHierarchy || !button.interactable)throw new InvalidOperationException("Native button unavailable");
            EventSystem.current.SetSelectedGameObject(button.gameObject);view.Focus.EnsureVisible(button.gameObject);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        static void Shot(string filename){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("LocalOnly/Validation/Gameplay/"+filename);Log("Capture "+filename+" | "+Screen.width+"x"+Screen.height+" | view "+view.ViewId);}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("FAILED: "+text+" | view="+view.ViewId+" | "+view.Message.text);Log("PASS: "+text);}
        static void Log(string text){log.Add(DateTime.UtcNow.ToString("O")+" "+text);File.WriteAllLines(path,log);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying){Stop("Stopped because Play ended.");return;}
            // Explicit bounded frame stepping is necessary on this host while its Game view is hidden.
            // InputSystem, MonoBehaviour Update/LateUpdate and native uGUI all still execute normally per frame.
            EditorApplication.Step();
            if(Time.unscaledTime<next)return;
            if(steps.Count==0){Stop(null);return;}
            var step=steps.Dequeue();try{step.Run();next=Time.unscaledTime+step.Delay;}
            catch(Exception e){Stop(e.ToString());}
        }
        public static void Stop(string reason)
        {QueueKeys();if(probeInput)UnityEngine.Object.DestroyImmediate(probeInput);Running=false;steps.Clear();EditorApplication.update-=Tick;if(reason!=null)Log(reason);}
    }

    /// <summary>Test-only InputSystem pump before the real gameplay Update. Never saved into the scene.</summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class CampusProbeInput : MonoBehaviour
    {
        public Key[] Keys=Array.Empty<Key>();
        public Vector2 MouseDelta;
        InputSettings.UpdateMode original;
        bool started;
        public void Begin(){original=InputSystem.settings.updateMode;InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;started=true;}
        void Update()
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Keys));
            if(MouseDelta!=Vector2.zero){InputSystem.QueueDeltaStateEvent(Mouse.current.delta,MouseDelta);MouseDelta=Vector2.zero;}
            InputSystem.Update();
        }
        void OnDestroy()
        {
            if(!started)return;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();
            InputSystem.settings.updateMode=original;
        }
    }
}
