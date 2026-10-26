using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Altaius.Demo02.Authoring
{
    public static partial class Task6RuntimeAcceptance
    {
        static bool batchCaptureActive;
        public static IEnumerator BatchingComparison(string output,bool candidate)
        {
            batchCaptureActive=false;
            if(candidate)
            {
                var baseline=Path.GetFullPath(Path.Combine(Application.dataPath,"../LocalOnly/Validation/Acceptance/BatchingComparison_v002/Baseline/comparison.json"));
                if(!File.Exists(baseline)||Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(baseline)).Value<bool>("allTargetsMet"))throw new InvalidOperationException("Candidate prohibited unless the completed original baseline misses the unchanged target.");
            }
            Bind(output,false);CampusDiagnostics.GameSize(1920,1080);yield return Frames(12);
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;ui.Applied.HighContrast=false;Apply();ui.CloseSurface(false);view.CloseWindow();
            var profile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var startPosition=ui.Gameplay.Motor.transform.position;var startFacing=ui.Gameplay.Motor.CharacterVisual.rotation;var startPitch=ui.Gameplay.CameraRig.Pitch;var originalMode=profile.gpuResidentDrawerMode;bool originalOcclusion=profile.gpuResidentDrawerEnableOcclusionCullingInCameras;float originalSmall=profile.smallMeshScreenPercentage;
            var flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;Type editorSettings=null;foreach(var a in AppDomain.CurrentDomain.GetAssemblies())editorSettings=editorSettings??a.GetType("UnityEditor.Rendering.EditorGraphicsSettings")??a.GetType("UnityEditor.EditorGraphicsSettings");
            var stripping=editorSettings.GetProperty("batchRendererGroupShaderStrippingMode",flags);var originalStripping=stripping.GetValue(null,null);if(candidate&&originalStripping.ToString()!="KeepAll")throw new InvalidOperationException("Native GraphicsSettings KeepAll required before candidate Play");var batchGet=typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",flags);var batchSet=typeof(PlayerSettings).GetMethod("SetBatchingForPlatform",flags);object[] batchArgs={BuildTarget.StandaloneWindows64,0,0};batchGet.Invoke(null,batchArgs);
            var views=SceneView.sceneViews.Cast<SceneView>().ToArray();var repaint=views.Select(v=>v.autoRepaintOnSceneChange).ToArray();var refresh=views.Select(v=>v.sceneViewState.alwaysRefresh).ToArray();var results=new List<object>();bool all=true;double preparationBegan=Time.realtimeSinceStartupAsDouble;
            var restore=new{mode=originalMode.ToString(),occlusion=originalOcclusion,smallMesh=originalSmall,stripping=originalStripping.ToString(),staticBatching=batchArgs[1],dynamicBatching=batchArgs[2],views=views.Select((v,i)=>new{id=v.GetInstanceID(),auto=repaint[i],always=refresh[i]}).ToArray()};File.WriteAllText(Path.Combine(folder,"entry-settings.json"),Newtonsoft.Json.JsonConvert.SerializeObject(restore,Newtonsoft.Json.Formatting.Indented));
            try
            {
                for(int i=0;i<views.Length;i++){views[i].autoRepaintOnSceneChange=false;views[i].sceneViewState.alwaysRefresh=false;}
                if(candidate)
                {
                    batchSet.Invoke(null,new object[]{BuildTarget.StandaloneWindows64,0,0});
                    profile.gpuResidentDrawerEnableOcclusionCullingInCameras=false;profile.smallMeshScreenPercentage=0;profile.gpuResidentDrawerMode=GPUResidentDrawerMode.InstancedDrawing;
                }
                yield return Seconds(3);var shaderProperty=typeof(ShaderUtil).GetProperty("anythingCompiling",flags);double shaderDeadline=Time.realtimeSinceStartupAsDouble+180;
                while(shaderProperty!=null&&(bool)shaderProperty.GetValue(null,null)){if(Time.realtimeSinceStartupAsDouble>shaderDeadline)throw new TimeoutException("Bounded shader preparation exceeded180seconds");yield return null;}
                File.WriteAllText(Path.Combine(folder,"preparation.txt"),"Seconds="+(Time.realtimeSinceStartupAsDouble-preparationBegan)+"; shader-status API="+(shaderProperty!=null)+"; candidate="+candidate+"; no rendering quality/content change.");
                foreach(var state in new[]{"courtyard","marking","review"})
                {
                    if(state=="marking"){yield return Route("studio");W("Inspect.D02.rubric.v1.naming");W("Primary");yield return Route("experiment");W("Work.W_LIST");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,30,"normal work opening before buffered marking");}
                    if(state=="review"){yield return Route("review");ui.OpenReview();U("Review.Request");Check(ui.SetFieldText("Review.Summary","Please review this exact pair and the stated limitations."),"Native buffered review summary");Check(ui.SetFieldText("Review.Time","13:40"),"Native buffered review time");U("Primary");}
                    yield return BatchImageGate(state+"-before");
                    if(candidate){var drawer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.Rendering.GPUResidentDrawer")).First(t=>t!=null);bool enabled=(bool)drawer.GetMethod("IsEnabled",flags).Invoke(null,null);Check(enabled,"GPU Resident Drawer is actually enabled before "+state);}
                    Action start=state=="marking"?(Action)(()=>W("Criterion.naming")):state=="review"?()=>U("Primary"):null;
                    yield return Task6BufferedPerformance.Measure(Path.Combine(folder,"performance"),state,contacts,npc,start);
                    bool passed=Task6BufferedPerformance.MeetsTarget;all&=passed;results.Add(new{state=state,meanMs=Task6BufferedPerformance.MeanMilliseconds,p95Ms=Task6BufferedPerformance.P95Milliseconds,passed=passed});Log("BUFFERED "+state+" mean="+Task6BufferedPerformance.MeanMilliseconds+" p95="+Task6BufferedPerformance.P95Milliseconds+" target="+passed);
                    if(state!="courtyard")yield return Shot(state+"-after");
                }
                Check(S.ReviewStatus==ReviewStatus.Pending&&S.CurrentApproval==null,"Buffered native request does not approve");
                File.WriteAllText(Path.Combine(folder,"comparison.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{candidate=candidate,allTargetsMet=all,states=results,mode=profile.gpuResidentDrawerMode.ToString(),visualAdoption="Requires inspected captures and real contact/sweep proof. GraphicsSettings byte restoration is separately verified after Play."},Newtonsoft.Json.Formatting.Indented));
                if(candidate)
                {
                    yield return BatchCandidateVisuals(startPosition,startFacing,startPitch);
                    File.WriteAllText(Path.Combine(folder,"visuals-complete.txt"),"Actual motion and sweep captured. Human inspection required before any adoption.");
                }
            }
            finally
            {
                if(batchCaptureActive){Task5NativeCapture.End();batchCaptureActive=false;}
                if(candidate){profile.gpuResidentDrawerMode=originalMode;profile.gpuResidentDrawerEnableOcclusionCullingInCameras=originalOcclusion;profile.smallMeshScreenPercentage=originalSmall;batchSet.Invoke(null,batchArgs);}
                for(int i=0;i<views.Length;i++)if(views[i]){views[i].autoRepaintOnSceneChange=repaint[i];views[i].sceneViewState.alwaysRefresh=refresh[i];}
                File.WriteAllText(Path.Combine(folder,"restored-settings.txt"),"Mode="+profile.gpuResidentDrawerMode+"; occlusion="+profile.gpuResidentDrawerEnableOcclusionCullingInCameras+"; small="+profile.smallMeshScreenPercentage+"; stripping currently="+stripping.GetValue(null,null)+" (GraphicsSettings bytes restored separately); original batching="+batchArgs[1]+"/"+batchArgs[2]+"; original view preferences restored. Saved bytes checked separately after Play.");
            }
            Result="PASS buffered "+(candidate?"candidate":"baseline")+" collection complete; target="+all;Log(Result);
        }
        static IEnumerator BatchImageGate(string name)
        {
            yield return Shot(name);File.WriteAllText(Path.Combine(folder,name+".ready"),"Inspect actual PNG for world, native UI, canonical Leena, source materials and no missing/pink geometry; approve only this image gate.");
            yield return Until(()=>File.Exists(Path.Combine(folder,name+".approved")),120,"Inspected image "+name);
        }
        static IEnumerator BatchCandidateVisuals(Vector3 startPosition,Quaternion startFacing,float startPitch)
        {
            Log("Candidate visual playback capture is outside every timing interval.");
            ui.CloseSurface(false);view.CloseWindow();ui.Host.BeginSession(SessionMode.Independent);ui.BindLifecycle();yield return Frames(4);
            yield return Route("studio");W("Inspect.D02.rubric.v1.naming");W("Primary");yield return Route("experiment");W("Work.W_LIST");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,30,"candidate work opened");
            Task5NativeCapture.Begin(Path.Combine(folder,"normal-marking-frames"),4);batchCaptureActive=true;W("Criterion.naming");double began=Time.realtimeSinceStartupAsDouble;
            do{Task5NativeCapture.Tick(contacts.VisiblePhase,npc.LeadPhase);if(Time.realtimeSinceStartupAsDouble-began>30)throw new TimeoutException("candidate marking contact");yield return null;}while(contacts.Busy||contacts.QueuedCount>0||Time.realtimeSinceStartupAsDouble-began<8);
            Task5NativeCapture.End();batchCaptureActive=false;yield return Until(()=>Task5NativeCapture.Pending==0,20,"candidate marking capture flush");Check(Task5NativeCapture.Error==null,"Actual marking frame capture");
            yield return Route("review");ui.OpenReview();U("Review.Request");ui.SetFieldText("Review.Summary","Please review this exact pair and the stated limitations.");ui.SetFieldText("Review.Time","13:40");U("Primary");Task5NativeCapture.Begin(Path.Combine(folder,"normal-review-frames"),4);batchCaptureActive=true;U("Primary");began=Time.realtimeSinceStartupAsDouble;
            do{Task5NativeCapture.Tick(contacts.VisiblePhase,npc.LeadPhase);if(Time.realtimeSinceStartupAsDouble-began>35)throw new TimeoutException("candidate review contact");yield return null;}while(contacts.Busy||npc.Busy||Time.realtimeSinceStartupAsDouble-began<18);
            Task5NativeCapture.End();batchCaptureActive=false;yield return Until(()=>Task5NativeCapture.Pending==0,20,"candidate review capture flush");Check(Task5NativeCapture.Error==null&&npc.PacketOwner=="Desk"&&!ui.Gameplay.ContactOwnsInput,"Actual single packet returns and controls restore");
            ui.CloseSurface(false);view.CloseWindow();ui.Gameplay.CloseInspection();var motor=ui.Gameplay.Motor;motor.GoTo(startPosition,startFacing);ui.Gameplay.CameraRig.Pitch=startPitch;
            for(int i=0;i<8;i++){ui.Gameplay.CameraRig.Yaw=i*45;ui.Gameplay.CameraRig.SnapFollow();yield return Shot("candidate-turn-"+(i*45));}
        }
    }
}
