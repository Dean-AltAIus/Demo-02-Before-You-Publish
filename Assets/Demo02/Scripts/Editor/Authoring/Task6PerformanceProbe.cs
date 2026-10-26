using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Bounded actual end-of-frame measurement, distinct from screenshot capture.</summary>
    public static class Task6PerformanceProbe
    {
        public static double LastMeanMilliseconds {get;private set;}
        public static double LastP95Milliseconds {get;private set;}
        static readonly string[] Names={"Main Thread","GPU Frame Time","Animator.ProcessAnimations","Gfx.WaitForPresentOnGfxThread","Draw Calls Count","Batches Count","SetPass Calls Count","Triangles Count","Total Used Memory","Total Reserved Memory","GC Used Memory","GC Allocated In Frame","Texture Memory","PlayerLoop","Canvas.BuildBatch","Canvas.RenderOverlays","EditorLoop","Gfx.WaitForRenderThread","Gfx.WaitForGfxCommandsFromMainThread"};
        static readonly string[] Categories={"Internal","Render","Animation","Render","Render","Render","Render","Render","Memory","Memory","Memory","Memory","Memory","PlayerLoop","UI Render","UI Render","Internal","Render","Internal"};
        public static IEnumerator MeasureIsolated(string folder,string state,float seconds,bool profilePasses=false,Action beforeSample=null,Func<string> activity=null)
        {
            Directory.CreateDirectory(folder);var views=SceneView.sceneViews.Cast<SceneView>().ToArray();var originals=views.Select(v=>v.autoRepaintOnSceneChange).ToArray();var refresh=views.Select(v=>v.sceneViewState.alwaysRefresh).ToArray();var settings=new List<string>();
            try
            {
                for(int i=0;i<views.Length;i++){settings.Add("Before "+views[i].GetInstanceID()+" autoRepaint="+originals[i]+" alwaysRefresh="+refresh[i]);views[i].autoRepaintOnSceneChange=false;views[i].sceneViewState.alwaysRefresh=false;}
                double warm=Time.realtimeSinceStartupAsDouble+3;while(Time.realtimeSinceStartupAsDouble<warm)yield return null;
                beforeSample?.Invoke();yield return Measure(folder,state,seconds,profilePasses,activity);
            }
            finally
            {
                for(int i=0;i<views.Length;i++)if(views[i]){views[i].autoRepaintOnSceneChange=originals[i];views[i].sceneViewState.alwaysRefresh=refresh[i];settings.Add("Restored "+views[i].GetInstanceID()+" autoRepaint="+views[i].autoRepaintOnSceneChange+" alwaysRefresh="+views[i].sceneViewState.alwaysRefresh);}
                File.WriteAllLines(Path.Combine(folder,state+"-editor-restoration.txt"),settings);
            }
        }
        public static IEnumerator Measure(string folder,string state,float seconds,bool profilePasses=false,Func<string> activity=null)
        {
            Directory.CreateDirectory(folder);var recorders=new ProfilerRecorder[Names.Length];var rows=new List<string>();var intervals=new List<double>();
            var cameraRows=new List<string>{"frame,camera,type,width,height,cpu_submission_ms"};var cameraStart=new Dictionary<int,long>();
            Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> begin=(context,camera)=>cameraStart[camera.GetInstanceID()]=System.Diagnostics.Stopwatch.GetTimestamp();
            Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> end=(context,camera)=>{long start;if(cameraStart.TryGetValue(camera.GetInstanceID(),out start))cameraRows.Add(Time.frameCount+","+camera.name+","+camera.cameraType+","+camera.pixelWidth+","+camera.pixelHeight+","+((System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency).ToString("F6",CultureInfo.InvariantCulture));};
            var allSamplerNames=new[]{"DrawTransparentObjects","DrawScreenSpaceUI","DrawOpaqueObjects","MainLightShadow"};var samplerNames=profilePasses?allSamplerNames:Array.Empty<string>();var samplers=new List<UnityEngine.Rendering.ProfilingSampler>();var restore=new List<Action>();
            var metadata=new List<string>{"UTC "+DateTime.UtcNow.ToString("O"),"State "+state,"Method actual WaitForEndOfFrame, no stepping, no screenshot/readback during sample", "Editor focused "+UnityEditorInternal.InternalEditorUtility.isApplicationActive,"Run in background "+Application.runInBackground,"Resolution "+Screen.width+"x"+Screen.height,"GPU "+SystemInfo.graphicsDeviceName+" | "+SystemInfo.graphicsDeviceVersion,"CPU "+SystemInfo.processorType,"Memory MB "+SystemInfo.systemMemorySize,"Quality "+QualitySettings.names[QualitySettings.GetQualityLevel()],"Time scale "+Time.timeScale,"vSync "+QualitySettings.vSyncCount+" | targetFrameRate "+Application.targetFrameRate,"EditorPrefs ApplicationIdleTime "+EditorPrefs.GetInt("ApplicationIdleTime",-1)+" | InteractionMode "+EditorPrefs.GetInt("InteractionMode",-1),"Recorder values are preceding-frame counters; absent or zero-only metrics are unverified."};
            try
            {
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering+=begin;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=end;
                var id=typeof(UnityEngine.Rendering.Universal.UniversalRenderPipeline).Assembly.GetType("UnityEngine.Rendering.Universal.URPProfileId");
                metadata.Add("GPU pass recording "+profilePasses+"; these samples are separate from screenshot/readback capture.");
                foreach(var name in allSamplerNames)
                {
                    var sampler=(UnityEngine.Rendering.ProfilingSampler)typeof(UnityEngine.Rendering.ProfilingSampler).GetMethod("Get").MakeGenericMethod(id).Invoke(null,new[]{Enum.Parse(id,name)});if(profilePasses)samplers.Add(sampler);
                    foreach(var fieldName in new[]{"m_Recorder","m_InlineRecorder"}){var field=typeof(UnityEngine.Rendering.ProfilingSampler).GetField(fieldName,BindingFlags.Instance|BindingFlags.NonPublic);if(field==null)continue;var recorder=field.GetValue(sampler) as UnityEngine.Profiling.Recorder;if(recorder==null)continue;bool wasEnabled=recorder.enabled;metadata.Add(name+" "+fieldName+" entry="+wasEnabled);restore.Add(()=>{recorder.enabled=wasEnabled;metadata.Add(name+" "+fieldName+" restored="+recorder.enabled);});}
                    sampler.enableRecording=profilePasses;
                }
                for(int i=0;i<Names.Length;i++){try{recorders[i]=ProfilerRecorder.StartNew(new ProfilerCategory(Categories[i]),Names[i],1);}catch(Exception e){metadata.Add(Names[i]+" unavailable "+e.Message);}metadata.Add(Names[i]+" valid "+recorders[i].Valid+" unit "+(recorders[i].Valid?recorders[i].UnitType.ToString():"unavailable"));}
                rows.Add("activity,engine_frame,wall_seconds,render_interval_ms,"+string.Join(",",Names.Select(n=>n.Replace(' ','_')))+","+string.Join(",",samplerNames.Select(n=>n+"_gpu_ms,"+n+"_gpu_count")));
                double began=Time.realtimeSinceStartupAsDouble,previous=0;
                while(Time.realtimeSinceStartupAsDouble-began<seconds)
                {
                    yield return new WaitForEndOfFrame();double now=Time.realtimeSinceStartupAsDouble;
                    if(previous>0){double ms=(now-previous)*1000;intervals.Add(ms);rows.Add((activity==null?state:activity()).Replace(',',';')+","+Time.frameCount+","+(now-began).ToString("R",CultureInfo.InvariantCulture)+","+ms.ToString("R",CultureInfo.InvariantCulture)+","+string.Join(",",recorders.Select(r=>r.Valid&&r.Count>0?r.LastValue.ToString(CultureInfo.InvariantCulture):"unavailable"))+","+string.Join(",",samplers.Select(s=>s.gpuElapsedTime.ToString("R",CultureInfo.InvariantCulture)+","+s.gpuSampleCount)));}previous=now;
                }
            }
            finally{for(int i=0;i<recorders.Length;i++)recorders[i].Dispose();foreach(var action in restore)action();UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=begin;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=end;}
            File.WriteAllLines(Path.Combine(folder,state+"-raw.csv"),rows);
            File.WriteAllLines(Path.Combine(folder,state+"-cameras.csv"),cameraRows);
            var sorted=intervals.OrderBy(v=>v).ToArray();if(sorted.Length>0){LastMeanMilliseconds=intervals.Average();LastP95Milliseconds=sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*.95)-1)];metadata.Add("Samples "+sorted.Length);metadata.Add("Rendered FPS "+(1000/intervals.Average()).ToString("F3",CultureInfo.InvariantCulture));metadata.Add("P95 ms "+sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*.95)-1)].ToString("F3",CultureInfo.InvariantCulture));metadata.Add("P99 ms "+sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*.99)-1)].ToString("F3",CultureInfo.InvariantCulture));metadata.Add("Max ms "+sorted.Last().ToString("F3",CultureInfo.InvariantCulture));}
            File.WriteAllLines(Path.Combine(folder,state+"-conditions.txt"),metadata);
        }
    }
}
