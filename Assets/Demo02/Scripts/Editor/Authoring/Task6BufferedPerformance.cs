using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Altaius.Demo02.UI;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Altaius.Demo02.Authoring
{
    /// <summary>One numeric-buffered interval. String formatting and file writes occur afterward.</summary>
    public static class Task6BufferedPerformance
    {
        const int Capacity=8192;
        struct Frame {public int Engine;public double Wall,Milliseconds;public string Contact,Lead;}
        struct CameraFrame {public int Engine,Id,Width,Height;public long Ticks;}
        static readonly string[] Names={"Main Thread","GPU Frame Time","PlayerLoop","Animator.ProcessAnimations","Draw Calls Count","Batches Count","SetPass Calls Count","Triangles Count","Total Used Memory","Total Reserved Memory","GC Allocated In Frame","EditorLoop","Gfx.WaitForRenderThread","Gfx.WaitForGfxCommandsFromMainThread"};
        static readonly string[] Categories={"Internal","Render","PlayerLoop","Animation","Render","Render","Render","Render","Memory","Memory","Memory","Internal","Render","Internal"};
        public static double MeanMilliseconds {get;private set;}
        public static double P95Milliseconds {get;private set;}
        public static bool MeetsTarget=>MeanMilliseconds<=1000.0/30&&P95Milliseconds<=33.33;
        public static IEnumerator Measure(string output,string state,CampusContactDirector contact,NpcInteractionDirector npc,Action action)
        {
            MeanMilliseconds=P95Milliseconds=double.PositiveInfinity;
            var frames=new Frame[Capacity];var values=new long[Capacity,Names.Length];var cameras=new CameraFrame[Capacity*3];int count=0,cameraCount=0;bool overflow=false,recording=false;
            var known=new Dictionary<int,Camera>();var starts=new Dictionary<int,long>();foreach(var camera in Resources.FindObjectsOfTypeAll<Camera>()){known[camera.GetInstanceID()]=camera;starts[camera.GetInstanceID()]=0;}
            var records=new ProfilerRecorder[Names.Length];var restores=new List<Action>();var metadata=new List<string>();var endFrame=new WaitForEndOfFrame();
            Action<ScriptableRenderContext,Camera> begin=(context,camera)=>{if(recording&&starts.ContainsKey(camera.GetInstanceID()))starts[camera.GetInstanceID()]=System.Diagnostics.Stopwatch.GetTimestamp();};
            Action<ScriptableRenderContext,Camera> end=(context,camera)=>
            {
                if(!recording)return;long started;int id=camera.GetInstanceID();if(!starts.TryGetValue(id,out started)||cameraCount>=cameras.Length){overflow=true;return;}
                cameras[cameraCount++]=new CameraFrame{Engine=Time.frameCount,Id=id,Width=camera.pixelWidth,Height=camera.pixelHeight,Ticks=System.Diagnostics.Stopwatch.GetTimestamp()-started};
            };
            metadata.Add(DateTime.UtcNow.ToString("O")+" | "+state+" | Buffered actual end-of-frame, five seconds warmup, one uncaptured20second interval.");
            metadata.Add("No per-frame CSV/LINQ formatting, environment argument reads, native desktop checks or file writes. Phase strings are existing reference values. Unity/product allocations remain part of the sample.");
            metadata.Add("Resolution="+Screen.width+"x"+Screen.height+" focused="+UnityEditorInternal.InternalEditorUtility.isApplicationActive+" background="+Application.runInBackground+" timeScale="+Time.timeScale+" vSync="+QualitySettings.vSyncCount+" target="+Application.targetFrameRate+" GPU="+SystemInfo.graphicsDeviceName+" API="+SystemInfo.graphicsDeviceType);
            metadata.Add("Entry idle="+EditorPrefs.GetInt("ApplicationIdleTime")+" interaction="+EditorPrefs.GetInt("InteractionMode")+"; preceding-frame counters, zeros/unavailable retained without zero-cost claims.");
            try
            {
                var urp=typeof(UnityEngine.Rendering.Universal.UniversalRenderPipeline).Assembly.GetType("UnityEngine.Rendering.Universal.URPProfileId");
                foreach(var name in new[]{"DrawTransparentObjects","DrawScreenSpaceUI","DrawOpaqueObjects","MainLightShadow"})
                {
                    var sampler=(ProfilingSampler)typeof(ProfilingSampler).GetMethod("Get").MakeGenericMethod(urp).Invoke(null,new[]{Enum.Parse(urp,name)});
                    foreach(var field in new[]{"m_Recorder","m_InlineRecorder"}){var recorder=typeof(ProfilingSampler).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(sampler) as UnityEngine.Profiling.Recorder;if(recorder!=null){bool prior=recorder.enabled;restores.Add(()=>recorder.enabled=prior);metadata.Add(name+" "+field+" entry="+prior);}}
                    sampler.enableRecording=false;
                }
                for(int i=0;i<Names.Length;i++){records[i]=ProfilerRecorder.StartNew(new ProfilerCategory(Categories[i]),Names[i],1);metadata.Add(Names[i]+" valid="+records[i].Valid+" unit="+(records[i].Valid?records[i].UnitType.ToString():"unavailable"));}
                RenderPipelineManager.beginCameraRendering+=begin;RenderPipelineManager.endCameraRendering+=end;
                recording=true;bool warmDraw=false;double warm=Time.realtimeSinceStartupAsDouble+5;
                while(Time.realtimeSinceStartupAsDouble<warm){if(records[4].Valid&&records[4].Count>0&&records[4].LastValue>0)warmDraw=true;yield return null;}
                if(Screen.width!=1920||Screen.height!=1080)throw new InvalidOperationException("Exact1920x1080 required");
                int warmCampus=0,warmPortrait=0,warmScene=0;for(int n=0;n<cameraCount;n++){var camera=known[cameras[n].Id];if(camera.name=="Campus camera rig")warmCampus++;if(camera.name=="Editable Leena portrait camera")warmPortrait++;if(camera.cameraType==CameraType.SceneView)warmScene++;}
                if(!warmDraw||warmCampus<10||warmPortrait<10||warmScene!=0||overflow)throw new InvalidOperationException("Positive real graphics and isolated camera warmup required");
                metadata.Add("Warm camera gate campus="+warmCampus+" portrait="+warmPortrait+" SceneView="+warmScene+" positiveDraw="+warmDraw);cameraCount=0;
                // Native control activation can queue one SceneView repaint even when its persistent refresh is off.
                // Drain that queued render outside the measured interval while normal contact motion begins.
                recording=false;action?.Invoke();
                for(int i=0;i<3;i++)yield return endFrame;
                cameraCount=0;recording=true;double began=Time.realtimeSinceStartupAsDouble,previous=0;
                while(Time.realtimeSinceStartupAsDouble-began<20)
                {
                    yield return endFrame;double now=Time.realtimeSinceStartupAsDouble;
                    if(previous>0)
                    {
                        if(count>=Capacity){overflow=true;break;}
                        frames[count]=new Frame{Engine=Time.frameCount,Wall=now-began,Milliseconds=(now-previous)*1000,Contact=contact.VisiblePhase,Lead=npc.LeadPhase};
                        for(int i=0;i<Names.Length;i++)values[count,i]=records[i].Valid&&records[i].Count>0?records[i].LastValue:long.MinValue;
                        count++;
                    }
                    previous=now;
                }
            }
            finally{recording=false;RenderPipelineManager.beginCameraRendering-=begin;RenderPipelineManager.endCameraRendering-=end;foreach(var recorder in records)recorder.Dispose();foreach(var restore in restores)restore();}
            Directory.CreateDirectory(output);var csv=new StringBuilder("engine_frame,wall_seconds,frame_ms,contact,lead");foreach(var name in Names)csv.Append(',').Append(name.Replace(' ','_'));csv.AppendLine();var intervals=new double[count];double sum=0;int positiveDraw=0;
            for(int n=0;n<count;n++){var frame=frames[n];intervals[n]=frame.Milliseconds;sum+=frame.Milliseconds;csv.Append(frame.Engine).Append(',').Append(F(frame.Wall)).Append(',').Append(F(frame.Milliseconds)).Append(',').Append(frame.Contact).Append(',').Append(frame.Lead);for(int i=0;i<Names.Length;i++)csv.Append(',').Append(values[n,i]==long.MinValue?"unavailable":values[n,i].ToString(CultureInfo.InvariantCulture));csv.AppendLine();if(values[n,4]>0)positiveDraw++;}
            File.WriteAllText(Path.Combine(output,state+"-raw.csv"),csv.ToString());csv.Clear().AppendLine("engine_frame,camera,type,width,height,cpu_submission_ms");int campus=0,portrait=0,scene=0;
            for(int n=0;n<cameraCount;n++){var frame=cameras[n];var camera=known[frame.Id];if(camera.name=="Campus camera rig")campus++;if(camera.name=="Editable Leena portrait camera")portrait++;if(camera.cameraType==CameraType.SceneView)scene++;csv.Append(frame.Engine).Append(',').Append(camera.name).Append(',').Append(camera.cameraType).Append(',').Append(frame.Width).Append(',').Append(frame.Height).Append(',').Append(F(frame.Ticks*1000.0/System.Diagnostics.Stopwatch.Frequency)).AppendLine();}
            File.WriteAllText(Path.Combine(output,state+"-cameras.csv"),csv.ToString());
            if(overflow||count<100||campus<100||portrait<100||scene!=0||positiveDraw<100)throw new InvalidOperationException("Buffered graphics gate failed: overflow="+overflow+" frames="+count+" campus="+campus+" portrait="+portrait+" scene="+scene+" draws="+positiveDraw);
            Array.Sort(intervals);MeanMilliseconds=sum/count;P95Milliseconds=intervals[(int)Math.Ceiling(count*.95)-1];metadata.Add("Frames="+count+" campus="+campus+" portrait="+portrait+" SceneView="+scene+" positiveDraw="+positiveDraw);metadata.Add("FPS="+F(1000/MeanMilliseconds)+" mean_ms="+F(MeanMilliseconds)+" p95_ms="+F(P95Milliseconds)+" target_pass="+MeetsTarget);File.WriteAllLines(Path.Combine(output,state+"-conditions.txt"),metadata);
        }
        static string F(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
    }
}
