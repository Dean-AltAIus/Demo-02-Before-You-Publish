using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Bounded asynchronous capture of the actual rendered viewport. Never focuses a window or changes simulation time.</summary>
    public static class Task5NativeCapture
    {
        static readonly object gate=new object();static readonly List<string> frames=new List<string>();
        static string folder,learnerPhase,npcPhase;static double began,next;static int index,pending;static float framesPerSecond=15;static bool recording;static GameObject pump;
        public static int Pending=>pending;
        public static string Error {get;private set;}
        public static void Begin(string output,float requestedFps=15)
        {
            if(recording||pending!=0||Directory.Exists(output))throw new InvalidOperationException("Preserve existing capture or pending writes.");
            folder=output;framesPerSecond=Mathf.Clamp(requestedFps,4,30);Directory.CreateDirectory(folder);frames.Clear();frames.Add("file,width,height,wall_seconds,engine_seconds,engine_frame,learner_phase,npc_phase");began=EditorApplication.timeSinceStartup;next=0;index=0;Error=null;recording=true;
            pump=new GameObject("Validation capture | transient end-of-frame reader"){hideFlags=HideFlags.HideAndDontSave};pump.AddComponent<Task5CapturePump>().StartCoroutine(EndFrames());
        }
        public static void Tick(string learner,string npc)
        {learnerPhase=learner;npcPhase=npc;}
        static IEnumerator EndFrames(){while(recording){yield return new WaitForEndOfFrame();if(recording)CaptureEndFrame();}}
        static void CaptureEndFrame()
        {
            if(!recording||pending>=2||EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+1.0/framesPerSecond;int width=Screen.width,height=Screen.height;string file="frame-"+(index++).ToString("D5")+".rgba",target=Path.Combine(folder,file);var texture=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32);texture.Create();
            string row=string.Join(",",file,width.ToString(),height.ToString(),(EditorApplication.timeSinceStartup-began).ToString("F6",CultureInfo.InvariantCulture),Time.unscaledTimeAsDouble.ToString("F6",CultureInfo.InvariantCulture),Time.frameCount.ToString(),learnerPhase,npcPhase);Interlocked.Increment(ref pending);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(texture);
            AsyncGPUReadback.Request(texture,0,TextureFormat.RGBA32,request=>
            {
                try
                {
                    if(request.hasError)throw new IOException("GPU readback failed for "+file);byte[] bytes=request.GetData<byte>().ToArray();
                    ThreadPool.QueueUserWorkItem(_=>{try{File.WriteAllBytes(target,bytes);lock(gate)frames.Add(row);}catch(Exception ex){Error=ex.Message;}finally{Interlocked.Decrement(ref pending);FlushIfDone();}});
                }
                catch(Exception ex){Error=ex.Message;Interlocked.Decrement(ref pending);FlushIfDone();}
                finally{texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
            });
        }
        public static void End(){recording=false;if(pump)UnityEngine.Object.DestroyImmediate(pump);pump=null;FlushIfDone();}
        static void FlushIfDone(){if(recording||pending!=0)return;lock(gate)File.WriteAllLines(Path.Combine(folder,"frames.csv"),frames);}
    }
    public sealed class Task5CapturePump:MonoBehaviour{}
}
