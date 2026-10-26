using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Altaius.Demo02.Authoring
{
    public static partial class Task6RuntimeAcceptance
    {
        public static IEnumerator LoopAttribution(string output)
        {
            Bind(output);CampusDiagnostics.GameSize(1920,1080);yield return Frames(12);
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;ui.Applied.HighContrast=false;Apply();ui.CloseSurface(false);view.CloseWindow();
            yield return Task6PerformanceProbe.MeasureIsolated(Path.Combine(folder,"performance"),"editor-loop-attribution",20,false);
            Result="PASS loop attribution completed; diagnostic only";Log(Result);
        }
        public static IEnumerator RenderComparison(string output)
        {
            Bind(output);CampusDiagnostics.GameSize(1920,1080);yield return Frames(12);
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;ui.Applied.HighContrast=false;Apply();ui.CloseSurface(false);view.CloseWindow();
            var start=ui.Gameplay.Motor.transform.position;var facing=ui.Gameplay.Motor.CharacterVisual.rotation;float yaw=ui.Gameplay.CameraRig.Yaw,pitch=ui.Gameplay.CameraRig.Pitch;
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var ao=renderer.rendererFeatures.Single(f=>f.GetType().Name=="ScreenSpaceAmbientOcclusion");var settings=ao.GetType().GetField("m_Settings",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ao);var downsample=settings.GetType().GetField("Downsample",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
            int originalCascades=pipeline.shadowCascadeCount;bool originalDownsample=(bool)downsample.GetValue(settings);var results=new List<string>{"variant,camera,mean_ms,p95_ms,cascades,ssao_downsample"};double baseline=0,shadow=0,half=0;
            Log("Matched render A/B,1920x1080,normal motion,100percent text. Same geometry, skin, materials, camera and UI. Temporary in-memory profile values only.");
            try
            {
                yield return Route("studio");W("Inspect.D02.rubric.v1.naming");W("Primary");yield return Route("experiment");W("Work.W_LIST");W("Criterion.naming");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,30,"prepare fixed marking view");
                for(int variant=0;variant<4;variant++)
                {
                    if(variant==3&&Math.Min(shadow,half)>=baseline*.95){Log("Combined omitted: neither individual mean improved at least5percent across the three fixed views.");break;}
                    string label=new[]{"original","two-cascades","half-ssao","combined"}[variant];pipeline.shadowCascadeCount=variant==1||variant==3?2:originalCascades;downsample.SetValue(settings,variant==2||variant==3?true:originalDownsample);
                    double sum=0;
                    foreach(string camera in new[]{"courtyard","marking","review"})
                    {
                        if(camera=="courtyard"){ui.CloseSurface(false);view.CloseWindow();ui.Gameplay.CloseInspection();ui.Gameplay.Motor.GoTo(start,facing);ui.Gameplay.CameraRig.Yaw=yaw;ui.Gameplay.CameraRig.Pitch=pitch;ui.Gameplay.CameraRig.SnapFollow();}
                        else if(camera=="marking"){yield return Route("experiment");W("Work.W_LIST");W("Criterion.naming");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,30,"fixed marking view");}
                        else{yield return Route("review");ui.OpenReview();}
                        yield return Seconds(2);yield return Shot(label+"-"+camera);yield return Task6PerformanceProbe.MeasureIsolated(Path.Combine(folder,"performance"),label+"-"+camera,12,false);
                        sum+=Task6PerformanceProbe.LastMeanMilliseconds;results.Add(label+","+camera+","+Task6PerformanceProbe.LastMeanMilliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+Task6PerformanceProbe.LastP95Milliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+pipeline.shadowCascadeCount+","+downsample.GetValue(settings));File.WriteAllLines(Path.Combine(folder,"comparison.csv"),results);
                    }
                    if(variant==0)baseline=sum;else if(variant==1)shadow=sum;else if(variant==2)half=sum;
                }
            }
            finally{pipeline.shadowCascadeCount=originalCascades;downsample.SetValue(settings,originalDownsample);File.WriteAllText(Path.Combine(folder,"profile-restoration.txt"),"Original and restored cascades="+originalCascades+" / "+pipeline.shadowCascadeCount+"; SSAO downsample="+originalDownsample+" / "+downsample.GetValue(settings)+". No AssetDatabase save or dirty call was used.");}
            Result="PASS bounded render comparison completed; timing verdict requires actual results";Log(Result);
        }
    }
}
