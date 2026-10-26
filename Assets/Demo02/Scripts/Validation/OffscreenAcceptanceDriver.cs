#if UNITY_EDITOR || DEMO02_VALIDATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Altaius.Demo02.Validation
{
    /// <summary>Opt-in validation build only. No normal Player bootstrap or saved scene changes.</summary>
    public sealed class OffscreenAcceptanceDriver:MonoBehaviour
    {
        public string Output;
        PresentationView ui;WorkbenchView view;CampusContactDirector contact;NpcInteractionDirector npc;
        LocalPresentationPreferences preferences;bool finished,stopping,ownsOutput;double began,lastProgress;
        readonly List<string> log=new List<string>();
        readonly List<string> cameraRows=new List<string>{"frame,camera,type,width,height,cpu_submission_ms"};
        readonly Dictionary<int,long> cameraStarts=new Dictionary<int,long>();
        string sample="setup";int sampleCampusFrames;bool hasGraphics;
        static readonly string[] Names={"Main Thread","GPU Frame Time","PlayerLoop","Animator.ProcessAnimations","Draw Calls Count","Batches Count","SetPass Calls Count","Triangles Count","Total Used Memory","Total Reserved Memory","GC Allocated In Frame"};
        static readonly string[] Categories={"Internal","Render","PlayerLoop","Animation","Render","Render","Render","Render","Memory","Memory","Memory"};

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
#if DEMO02_VALIDATION && !UNITY_EDITOR
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-demo2Validation");
            if(index<0)return;
            if(index+1>=args.Length){Application.Quit(2);return;}
            if(!Application.isBatchMode)
            {
                try{ValidateDesktopArguments(args);}catch(Exception e){Debug.LogError(e.Message);Application.Quit(2);return;}
            }
            var driver=new GameObject("Explicit offscreen acceptance driver").AddComponent<OffscreenAcceptanceDriver>();driver.Output=Path.GetFullPath(args[index+1]);
#endif
        }
        IEnumerator Start()
        {
            began=lastProgress=Time.realtimeSinceStartupAsDouble;
            if(string.IsNullOrEmpty(Output)||Directory.Exists(Output)){Finish("FAIL new explicit output directory required",2);yield break;}
            Directory.CreateDirectory(Output);ownsOutput=true;Application.logMessageReceived+=OnLog;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0&&!stopping)
            {
                object next=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}
                catch(Exception ex){failure=ex;}
                if(failure!=null){Finish("FAIL "+failure,2);yield break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(next is IEnumerator nested){stack.Push(nested);continue;}
                yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            if(!stopping)Finish("COMPLETE offscreen rendered samples; gate verdict requires retained metrics and inspected captures",0);
        }
        void Update()
        {
            if(finished)return;
            if(!Application.isEditor&&!Application.isBatchMode)
            {try{ValidateDesktopArguments(Environment.GetCommandLineArgs());}catch(Exception e){Finish("FAIL "+e.Message,2);return;}}
            if(Time.realtimeSinceStartupAsDouble-began>600||Time.realtimeSinceStartupAsDouble-lastProgress>150)Finish("FAIL bounded validation timeout, no visible fallback",2);
        }
        IEnumerator Run()
        {
            Application.runInBackground=true;Application.targetFrameRate=-1;
            for(int i=0;i<15;i++)yield return null;
            ui=FindFirstObjectByType<PresentationView>();Require(ui,"Saved presentation missing");view=ui.Workbench;contact=FindFirstObjectByType<CampusContactDirector>();npc=FindFirstObjectByType<NpcInteractionDirector>();
            preferences=ui.Applied.Copy();ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;ui.Applied.HighContrast=false;Apply();ui.CloseSurface(false);view.CloseWindow();
            Require(Screen.width==1920&&Screen.height==1080,"Exact1920x1080 backbuffer required, actual "+Screen.width+"x"+Screen.height);
            hasGraphics=SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D11&&SystemInfo.graphicsDeviceName.Contains("Iris");Require(hasGraphics,"Actual Iris Xe D3D11 graphics device required");
            RenderPipelineManager.beginCameraRendering+=BeginCamera;RenderPipelineManager.endCameraRendering+=EndCamera;
            File.WriteAllLines(Path.Combine(Output,"conditions.txt"),new[]{DateTime.UtcNow.ToString("O"),"Offscreen Player, not on-screen/compositor/input acceptance",Application.unityVersion,"Development "+Debug.isDebugBuild,"Batch "+Application.isBatchMode,"GPU "+SystemInfo.graphicsDeviceName+" | "+SystemInfo.graphicsDeviceVersion,"CPU "+SystemInfo.processorType,"Quality "+QualitySettings.names[QualitySettings.GetQualityLevel()],"vSync "+QualitySettings.vSyncCount+" target "+Application.targetFrameRate,"Resolution "+Screen.width+"x"+Screen.height,"Skin "+QualitySettings.skinWeights,"Direct routes; normal physical motion; 100percent text; original saved render profile","No capture/readback during timing. GPU timing may be unavailable and is never treated as zero cost.",Application.isBatchMode?"Original batch diagnostic":ValidateDesktopArguments(Environment.GetCommandLineArgs()),"Artifact may contain updated runtime scripts with retained e22dc15 packed scene/catalogue data."});
            yield return PrepareCapture("courtyard");yield return Measure("courtyard",null);
            yield return Route("studio");W("Inspect.D02.rubric.v1.naming");W("Primary");yield return Route("experiment");W("Work.W_LIST");yield return Until(()=>!contact.Busy&&contact.QueuedCount==0,30,"work opening");
            yield return PrepareCapture("marking");yield return Measure("marking",()=>W("Criterion.naming"));yield return Until(()=>!contact.Busy&&contact.QueuedCount==0,30,"criterion placement");yield return Capture("marking-after");
            yield return Route("review");ui.OpenReview();U("Review.Request");Require(ui.SetFieldText("Review.Summary","Please review this exact pair and the stated limitations."),"Summary native field");Require(ui.SetFieldText("Review.Time","13:40"),"Time native field");U("Primary");
            yield return PrepareCapture("review");yield return Measure("review",()=>U("Primary"));yield return Capture("review-after");
            Require(ui.Host.Session.Snapshot.ReviewStatus==ReviewStatus.Pending&&ui.Host.Session.Snapshot.CurrentApproval==null,"Request sample must not grant approval");
            File.WriteAllLines(Path.Combine(Output,"all-camera-callbacks.csv"),cameraRows);Log("Native pending review, no approval; completed actual normal marking/review frames.");
        }
        IEnumerator PrepareCapture(string state)
        {
            sample=state+"-capture";yield return Seconds(3);yield return Capture(state);
            Log("READY "+state+". Awaiting local inspected-capture acknowledgement.");File.WriteAllText(Path.Combine(Output,state+".ready"),"Inspect actual decoded "+state+".png; create "+state+".approved only if real world, native UI and canonical Leena are visible.");
            yield return Until(()=>File.Exists(Path.Combine(Output,state+".approved")),120,"Inspected capture "+state);yield return Seconds(3);
        }
        IEnumerator Capture(string state)
        {
            yield return new WaitForEndOfFrame();var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try{image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();var pixels=image.GetRawTextureData<byte>();byte min=255,max=0;for(int i=0;i<pixels.Length;i+=97){min=Math.Min(min,pixels[i]);max=Math.Max(max,pixels[i]);}Require(max-min>30,"Capture is blank or unrendered");Require(cameraRows.Count>4,"Actual SRP camera callbacks required");File.WriteAllBytes(Path.Combine(Output,state+".png"),image.EncodeToPNG());}
            finally{Destroy(image);}Log("Captured "+state+" "+Screen.width+"x"+Screen.height);
        }
        IEnumerator Measure(string state,Action startAction)
        {
            sample=state;sampleCampusFrames=0;int cameraOffset=cameraRows.Count;var records=new ProfilerRecorder[Names.Length];var metadata=new List<string>();var rows=new List<string>{"engine_frame,wall_seconds,frame_ms,activity,"+string.Join(",",Names.Select(n=>n.Replace(' ','_')))};var durations=new List<double>();
            var restore=new List<Action>();bool positiveDraw=false;
            try
            {
                // Match the Editor baseline: exclude optional detailed GPU-pass recording.
                var urp=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.Rendering.Universal.URPProfileId")).FirstOrDefault(t=>t!=null);
                Require(urp!=null,"Pinned URP profiling IDs required");
                foreach(var name in new[]{"DrawTransparentObjects","DrawScreenSpaceUI","DrawOpaqueObjects","MainLightShadow"})
                {
                    var sampler=(ProfilingSampler)typeof(ProfilingSampler).GetMethod("Get").MakeGenericMethod(urp).Invoke(null,new[]{Enum.Parse(urp,name)});
                    foreach(var field in new[]{"m_Recorder","m_InlineRecorder"}){var recorder=typeof(ProfilingSampler).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(sampler) as UnityEngine.Profiling.Recorder;if(recorder!=null){bool prior=recorder.enabled;restore.Add(()=>recorder.enabled=prior);}}
                    sampler.enableRecording=false;
                }
                for(int i=0;i<Names.Length;i++){records[i]=ProfilerRecorder.StartNew(new ProfilerCategory(Categories[i]),Names[i],1);metadata.Add(Names[i]+" valid="+records[i].Valid+" unit="+(records[i].Valid?records[i].UnitType.ToString():"unavailable"));}
                startAction?.Invoke();double t=Time.realtimeSinceStartupAsDouble,priorTime=0;
                while(Time.realtimeSinceStartupAsDouble-t<20)
                {
                    yield return new WaitForEndOfFrame();double now=Time.realtimeSinceStartupAsDouble;lastProgress=now;
                    if(priorTime>0){double ms=(now-priorTime)*1000;durations.Add(ms);rows.Add(Time.frameCount+","+F(now-t)+","+F(ms)+","+contact.VisiblePhase+" | "+npc.LeadPhase+","+string.Join(",",records.Select(r=>r.Valid&&r.Count>0?r.LastValue.ToString(CultureInfo.InvariantCulture):"unavailable")));}
                    if(records[4].Valid&&records[4].Count>0&&records[4].LastValue>0)positiveDraw=true;priorTime=now;
                }
            }
            finally{foreach(var r in records)r.Dispose();foreach(var action in restore)action();}
            Require(hasGraphics&&positiveDraw&&sampleCampusFrames>30&&durations.Count>30,"Actual positive graphics/draw/campus-frame evidence required");
            File.WriteAllLines(Path.Combine(Output,state+"-raw.csv"),rows);File.WriteAllLines(Path.Combine(Output,state+"-cameras.csv"),new[]{cameraRows[0]}.Concat(cameraRows.Skip(cameraOffset)));
            var sorted=durations.OrderBy(v=>v).ToArray();metadata.Add("Frames "+durations.Count+" campus callbacks "+sampleCampusFrames);metadata.Add("FPS "+F(1000/durations.Average()));metadata.Add("P95ms "+F(sorted[(int)Math.Ceiling(sorted.Length*.95)-1]));metadata.Add("P99ms "+F(sorted[(int)Math.Ceiling(sorted.Length*.99)-1]));metadata.Add("Maxms "+F(sorted.Last()));File.WriteAllLines(Path.Combine(Output,state+"-summary.txt"),metadata);Log("Measured "+state);
        }
        void BeginCamera(ScriptableRenderContext context,Camera camera){cameraStarts[camera.GetInstanceID()]=System.Diagnostics.Stopwatch.GetTimestamp();}
        void EndCamera(ScriptableRenderContext context,Camera camera)
        {if(camera.name=="Campus camera rig")sampleCampusFrames++;long start;if(cameraStarts.TryGetValue(camera.GetInstanceID(),out start))cameraRows.Add(Time.frameCount+","+camera.name+","+camera.cameraType+","+camera.pixelWidth+","+camera.pixelHeight+","+F((System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency));}
        IEnumerator Route(string station){ui.CloseSurface(false);view.OpenRoutes();W("Route."+station);for(int i=0;i<5;i++)yield return null;Require(ui.Gameplay.ActiveStation.StationId==station,"Actual native route "+station);}
        IEnumerator Seconds(float seconds){double t=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<t)yield return null;lastProgress=Time.realtimeSinceStartupAsDouble;}
        IEnumerator Until(Func<bool> predicate,float seconds,string label){double t=Time.realtimeSinceStartupAsDouble+seconds;while(!predicate()){Require(Time.realtimeSinceStartupAsDouble<t,label+" timed out");yield return null;}lastProgress=Time.realtimeSinceStartupAsDouble;}
        void W(string id)=>Require(view.ActivateControl(id),"Native work control "+id);
        void U(string id)=>Require(ui.ActivateControl(id),"Native presentation control "+id);
        void Apply()=>typeof(PresentationView).GetMethod("ApplyPreferences",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,new object[]{false});
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static string F(double number)=>number.ToString("R",CultureInfo.InvariantCulture);
        void Log(string message){lastProgress=Time.realtimeSinceStartupAsDouble;log.Add(DateTime.UtcNow.ToString("O")+" "+message);File.WriteAllLines(Path.Combine(Output,"runtime.txt"),log);}
        void OnLog(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)Finish("FAIL runtime error "+message+"\n"+stack,2);}
        void Restore(){RenderPipelineManager.beginCameraRendering-=BeginCamera;RenderPipelineManager.endCameraRendering-=EndCamera;Application.logMessageReceived-=OnLog;if(ui&&preferences!=null){ui.Applied.Master=preferences.Master;ui.Applied.Silent=preferences.Silent;ui.Applied.TextScale=preferences.TextScale;ui.Applied.ReducedMotion=preferences.ReducedMotion;ui.Applied.HighContrast=preferences.HighContrast;Apply();preferences=null;}}
        void Finish(string result,int code)
        {
            if(finished)return;finished=stopping=true;Restore();
            if(ownsOutput){File.WriteAllText(Path.Combine(Output,"result.txt"),result);File.WriteAllLines(Path.Combine(Output,"all-camera-callbacks.csv"),cameraRows);}
#if !UNITY_EDITOR
            Application.Quit(code);
#endif
        }
        void OnApplicationQuit(){Restore();}
        void OnDestroy(){Restore();}
        static string ValidateDesktopArguments(string[] args)
        {
            int desktop=Array.IndexOf(args,"-demo2IsolatedDesktop"),input=Array.IndexOf(args,"-demo2InputDesktop");
            if(desktop<0||desktop+1>=args.Length||input<0||input+1>=args.Length)throw new InvalidOperationException("Explicit isolated-desktop arguments required for non-batch validation.");
            return ValidationDesktopIsolation.Validate(args[desktop+1],args[input+1]);
        }
    }
}
#endif
