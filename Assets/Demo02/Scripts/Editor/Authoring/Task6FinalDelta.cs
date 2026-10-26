using System;
using System.Collections;
using System.IO;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presentation;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static partial class Task6RuntimeAcceptance
    {
        public static IEnumerator FinalDelta(string output)
        {
            Bind(output);CampusDiagnostics.GameSize(1920,1080);yield return Frames(12);
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;ui.Applied.HighContrast=false;Apply();ui.CloseSurface(false);view.CloseWindow();
            Log("Direct routes; normal physical motion; 100percent text; silent off. GPU pass recorders disabled for throughput samples. No screenshot readbacks within samples.");
            yield return Shot("baseline-1920");yield return Task6PerformanceProbe.MeasureIsolated(Path.Combine(folder,"performance"),"baseline-no-pass-recording",20);
            yield return Route("studio");W("Inspect.D02.rubric.v1.naming");W("Primary");yield return Route("experiment");W("Work.W_LIST");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,25,"work open before action sample");
            yield return Shot("experiment-before-action");yield return Task6PerformanceProbe.MeasureIsolated(Path.Combine(folder,"performance"),"experiment-normal-contact",20,false,()=>W("Criterion.naming"),()=>contacts.VisiblePhase+" | "+npc.LeadPhase);
            yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,25,"criterion placement after sample");yield return Shot("experiment-after-action");
            yield return Route("review");ui.OpenReview();U("Review.Request");yield return Type("Review.Summary","Please review this exact pair and the stated limitations.",true);yield return Type("Review.Time","13:40",true);U("Primary");yield return Shot("review-before-request");
            yield return Task6PerformanceProbe.MeasureIsolated(Path.Combine(folder,"performance"),"review-normal-packet",20,false,()=>U("Primary"),()=>contacts.VisiblePhase+" | "+npc.LeadPhase);
            yield return Shot("review-after-request");Check(S.ReviewStatus==ReviewStatus.Pending&&S.CurrentApproval==null,"Normal request performance window grants no approval");
            Log("Focused synthetic stale/details fixture uses reduced motion, no live semantic evaluation.");ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();var s=ui.Host.Session;
            var sources=new[]{s.Inspect("S_A",InspectionLocation.Archive).Reference,s.Inspect("S_B",InspectionLocation.Archive).Reference};
            Check(s.Submit(s.Stage(new EditTask(1,new[]{new Question("Q1","Explain two sources and their limits.",10,"O1"),new Question("Q2","Compare the sources for a bounded decision.",10,"O2")},sources,"Whether to trial additional support at the entrance."),"Explicit synthetic final visual fixture")).Status==ReceiptStatus.Accepted,"Synthetic task fixture");
            ui.Host.NotifyChanged();yield return Route("review");yield return RequestFixtureReview();DecideFixture(ReviewDecision.Approved,"Exact synthetic visual fixture, no arbitrary semantic evaluation.");
            yield return Route("studio");foreach(var id in new[]{"D02.rubric.v1.naming","D02.rubric.v1.listing"}){W("Inspect."+id);W("Primary");}W("Editor.Rubric");yield return Type("Rubric.0.Text","Explain the source and its limits.");W("Primary");W("Primary");Check(S.CurrentApproval==null&&S.ReviewStatus==ReviewStatus.Stale,"Native changed rubric makes prior approval historical");
            yield return Route("review");CampusDiagnostics.GameSize(1280,720);ui.Applied.TextScale=1;Apply();ui.OpenReview();yield return Shot("stale-short-primary-1280");
            U("Review.Details");Check(ui.ReadPresenterControl("Review.Provenance"),"Expanded exact provenance row remains reachable");yield return Shot("review-expanded-details-1280");ui.Applied.TextScale=2;Apply();ui.OpenReview();Check(ui.ReadPresenterControl("Review.Provenance"),"Expanded exact provenance reachable at200percent");yield return Shot("review-expanded-details-1280-text200");
            ui.Applied.TextScale=1;Apply();CampusDiagnostics.GameSize(1920,1080);yield return ResetMemoryCycles();Result="PASS final focused delta";Log(Result);
        }
    }
}
