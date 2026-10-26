using System;
using System.Collections;
using System.IO;
using System.Linq;
using Altaius.Demo02.Authoring;
using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Altaius.Demo02.Tests
{
    public sealed class FinalReviewNativeTests
    {
        const string ScenePath="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity";
        string output;bool captureOwned;
        void Log(string text)=>File.AppendAllText(Path.Combine(output,"native-log.txt"),Time.frameCount+" | "+text+"\n");
        IEnumerator Until(Func<bool> condition,float seconds,string label)
        {
            var end=Time.realtimeSinceStartup+seconds;
            while(!condition())
            {
                if(!Application.isPlaying)throw new InvalidOperationException("Manual Play exit respected.");
                if(Time.realtimeSinceStartup>end)throw new TimeoutException(label);
                yield return null;
            }
            Log(label);
        }
        IEnumerator Capture(string name,CampusContactDirector contact,NpcInteractionDirector npc)
        {
            var folder=Path.Combine(output,name);Canvas.ForceUpdateCanvases();Task5NativeCapture.Begin(folder,4);captureOwned=true;
            try
            {
                Task5NativeCapture.Tick(contact.VisiblePhase,npc.LeadPhase);
                yield return new WaitForSecondsRealtime(.6f);
            }
            finally{Task5NativeCapture.End();captureOwned=false;}
            yield return Until(()=>Task5NativeCapture.Pending==0,10,"GPU readback "+name);
            Assert.That(Task5NativeCapture.Error,Is.Null);
            Assert.That(Directory.GetFiles(folder,"*.rgba").Length,Is.GreaterThan(0),"Actual rendered frames required.");
            Log("Capture "+name+" "+Screen.width+"x"+Screen.height);
        }

        [UnityTest,Timeout(360000),Explicit("Final scoped loading spacing and real contact retry callbacks, with background RenderTexture capture.")]
        public IEnumerator LoadingSpacingAndReceiptDrivenContactsProjectOnce()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Assert.That(EditorSceneManager.GetActiveScene().isDirty,Is.False);
            yield return new EnterPlayMode();yield return null;
            yield return ExerciseNative();
            yield return new ExitPlayMode();
            Assert.That(EditorSceneManager.GetActiveScene().isDirty,Is.False);
            var saved=UnityEngine.Object.FindFirstObjectByType<PresentationView>();
            Assert.That(saved.Workbench.English,Is.SameAs(UnityEditor.AssetDatabase.LoadAssetAtPath<NativeEnglishStringTable>("Assets/Demo02/strings/STR_EN.asset")));
            Assert.That(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(t=>t.name=="Title workroom shot | saved editable composition"),Is.EqualTo(1));
        }
        // Captured locals are created after EnterPlayMode's domain reload, as in Task6RuntimeAcceptance.
        IEnumerator ExerciseNative()
        {
            output=Path.GetFullPath("LocalOnly/Validation/Acceptance/FinalReviewFix_v001/native-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(output);
            Log("Startup playing "+Application.isPlaying+" | scene "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+" | roots "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount);
            Assert.That(Application.isPlaying,Is.True,"Play ended before native startup. Do not restart after a manual exit.");
            PresentationView ui=null;
            yield return Until(()=>{ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>(FindObjectsInactive.Include);return ui&&ui.Host&&ui.Host.Session!=null;},10,"Saved native UI and session available");
            var view=ui.Workbench;Assert.That(view,Is.Not.Null);
            var contact=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var npc=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();
            var table=view.English;var originalScale=ui.Applied.TextScale;var originalMotion=ui.Applied.ReducedMotion;
            try
            {
                CampusDiagnostics.GameSize(1280,720);yield return null;yield return null;
                var loading=ui.Surfaces.Single(x=>x.SurfaceId=="loading");var brief=ui.Surfaces.Single(x=>x.SurfaceId=="brief");
                ui.OpenTitle();ui.Surfaces.Single(x=>x.SurfaceId=="title").Primary.onClick.Invoke();var attempt=ui.Host.Session.Snapshot.AttemptId;
                view.English=null;
                foreach(float scale in new[]{1f,2f})
                {
                    ui.Applied.TextScale=scale;view.SetTextScale(scale);brief.Primary.onClick.Invoke();
                    yield return Until(()=>loading.Heading.text=="Practice unavailable",10,"Local unavailable loading state");
                    Assert.That(ui.SurfaceId,Is.EqualTo("loading"));Assert.That(loading.Heading.text,Is.EqualTo("Practice unavailable"));
                    Assert.That(loading.Frame.rect.height,Is.LessThanOrEqualTo(scale==1?245:390));Assert.That(loading.Scroll.verticalScrollbar,Is.Null);
                    var gap=loading.Heading.rectTransform.offsetMin.x-loading.Icon.rectTransform.anchoredPosition.x-loading.Icon.rectTransform.rect.width;
                    Assert.That(gap,Is.GreaterThanOrEqualTo(12));Log("Loading scale "+scale+" gap "+gap+" height "+loading.Frame.rect.height);
                    yield return Capture("loading-1280-"+(int)(scale*100),contact,npc);
                    loading.Secondary.onClick.Invoke();Assert.That(ui.Host.Session.Snapshot.AttemptId,Is.EqualTo(attempt));
                }
                view.English=table;ui.Applied.TextScale=1;view.SetTextScale(1);brief.Primary.onClick.Invoke();
                yield return Until(()=>ui.SurfaceId=="onramp",10,"Restored catalogue starts onramp");ui.Surfaces.Single(x=>x.SurfaceId=="onramp").Primary.onClick.Invoke();
                var s=ui.Host.Session;ui.Applied.ReducedMotion=true;
                s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");yield return null;
                yield return Until(()=>!contact.Busy&&contact.QueuedCount==0,30,"Reduced placement settled");
                ui.Applied.ReducedMotion=false;int marks=0;contact.PresentationCompleted+=id=>{if(id=="MarkPoint"){marks++;Log("MarkPoint completed "+marks);}};
                view.SetFieldText("Mark.Marks","4");view.SetFieldText("Mark.Rationale","Literal annotation for physical retry verification.");view.Primary.onClick.Invoke();
                var annotation=s.Snapshot.Audit.Last().Receipt;Assert.That(annotation.Status,Is.EqualTo(ReceiptStatus.Accepted));var markEnvelope=annotation.Scope.Envelope;
                s.Submit(markEnvelope);s.Submit(markEnvelope);
                yield return Until(()=>contact.ActiveBinding=="MarkPoint"&&contact.VisiblePhase=="Contact",35,"Actual MarkPoint contact");
                s.Submit(markEnvelope);yield return Capture("annotation-contact",contact,npc);
                yield return Until(()=>!contact.Busy&&contact.QueuedCount==0,35,"MarkPoint settled");
                s.Submit(markEnvelope);yield return new WaitForSecondsRealtime(.5f);Assert.That(marks,Is.EqualTo(1));Assert.That(s.Snapshot.AnnotationHistory.Count,Is.EqualTo(1));
                Assert.That(contact.QueuedCount,Is.Zero);Log("Annotation retry arrivals "+s.Snapshot.Audit.Count(a=>a.Receipt.RequestId==annotation.RequestId)+" | physical completions "+marks);

                ui.Gameplay.DirectRoute("review");ui.OpenReviewRequest();yield return null;
                var request=ui.Surfaces.Single(x=>x.SurfaceId=="reviewRequest");request.Rows.Find("Review.Summary").Input.text="Review this exact recorded pair.";request.Rows.Find("Review.Time").Input.text="13:40";
                request.Primary.onClick.Invoke();ui.Surfaces.Single(x=>x.SurfaceId=="confirmation").Primary.onClick.Invoke();var reviewEnvelope=ui.Pending;var review=s.FindReceipt(reviewEnvelope.CommandId);
                Assert.That(review.Status,Is.EqualTo(ReceiptStatus.Accepted));int packetCallbacks=0;contact.PacketPresentationSettled+=(id,completed)=>{if(id==review.RequestId){packetCallbacks++;Log("Packet preparation callback "+completed);}};
                s.Submit(reviewEnvelope);s.Submit(reviewEnvelope);
                yield return Until(()=>npc.Busy&&npc.LeadPhase=="ReadPacket",45,"Actual reviewer packet read");
                s.Submit(reviewEnvelope);yield return Capture("review-contact",contact,npc);
                yield return Until(()=>!npc.Busy&&npc.Requests.TotalSettled==1,45,"Review request settled");
                s.Submit(reviewEnvelope);yield return new WaitForSecondsRealtime(.5f);
                Assert.That(npc.Requests.TotalSettled,Is.EqualTo(1));Assert.That(npc.Requests.PendingCount,Is.Zero);Assert.That(npc.CompletedCycles,Is.EqualTo(1));Assert.That(packetCallbacks,Is.EqualTo(1));
                Assert.That(s.Snapshot.ReviewHistory.Count,Is.EqualTo(1));Assert.That(s.Snapshot.CurrentApproval,Is.Null);Log("Review retry arrivals "+s.Snapshot.Audit.Count(a=>a.Receipt.RequestId==review.RequestId)+" | physical cycles "+npc.CompletedCycles+" | packet callbacks "+packetCallbacks);
                ui.Applied.ReducedMotion=true;var distinct=s.Stage(new RequestReview(1,1,"A distinct same-pair request.","13:40"),"native-final");s.Submit(distinct);s.Submit(distinct);
                yield return Until(()=>npc.Requests.TotalSettled==2,10,"Distinct request settled with reduced motion");s.Submit(distinct);yield return null;Assert.That(npc.Requests.TotalSettled,Is.EqualTo(2));
                ui.Lifecycle.Reset(true);yield return null;yield return null;Assert.That(npc.Requests.TotalSettled,Is.Zero);Assert.That(contact.QueuedCount,Is.Zero);Assert.That(s.Snapshot.Evidence,Is.Empty);
                Log("PASS loading, original receipt callbacks, reduced motion and reset. No semantic approval or new performance claim.");
            }
            finally
            {
                try{if(captureOwned){captureOwned=false;Task5NativeCapture.End();}}
                finally
                {
                    if(view)view.English=table;
                    if(ui&&ui.Applied!=null){ui.Applied.TextScale=originalScale;ui.Applied.ReducedMotion=originalMotion;}
                    if(view)view.SetTextScale(originalScale);
                }
            }

        }
    }
}
