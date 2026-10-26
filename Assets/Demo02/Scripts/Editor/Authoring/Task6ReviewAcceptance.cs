using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presentation;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static partial class Task6RuntimeAcceptance
    {
        public static IEnumerator ReviewAccessibility(string output)
        {
            Bind(output);ui.Host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();
            Log("Explicit synthetic fixture setup for returned/approved/released/stale display. Role decisions below use the test-only trusted composition port. Ordinary request/release controls remain real. No live or arbitrary semantic review claimed.");
            var s=ui.Host.Session;var sources=new[]{s.Inspect("S_A",InspectionLocation.Archive).Reference,s.Inspect("S_B",InspectionLocation.Archive).Reference};
            Check(s.Submit(s.Stage(new EditTask(1,new[]{new Question("Q1","Explain two sources and their limits.",10,"O1"),new Question("Q2","Compare the sources for a bounded decision.",10,"O2")},sources,"Whether to trial additional support at the entrance."),"Explicit synthetic visual fixture")).Status==ReceiptStatus.Accepted,"Synthetic task2 setup");
            Check(s.Submit(s.Stage(new EditRubric(1,new[]{new Criterion("naming","Q1",10,"Explain the source and its limitations.","O1"),new Criterion("listing","Q2",10,"Compare methods and justify a bounded decision.","O2")}),"Explicit synthetic visual fixture")).Status==ReceiptStatus.Accepted,"Synthetic rubric2 setup");
            ui.Host.NotifyChanged();ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;ui.Applied.HighContrast=false;ui.Applied.TextScale=1;Apply();yield return Route("review");
            yield return RequestFixtureReview();DecideFixture(ReviewDecision.Returned,"The comparison needs a clearer reason tied to the recorded evidence. No approval is granted.");
            yield return ReviewViews("returned");
            yield return RequestFixtureReview();DecideFixture(ReviewDecision.Approved,"Explicit synthetic exact-pair visual fixture approval, not a judgement of arbitrary learner prose.");
            Check(S.CurrentApproval!=null,"Actual scoped synthetic approval exists separately from scheduling");yield return ReviewViews("approved");
            ui.OpenReview();U("Review.Release");U("Primary");Check(S.Publication==PublicationStatus.Released,"Ordinary native release requires and accepts exact current approval");yield return Shot("release-receipt-final");yield return ReviewViews("released");
            ui.Applied.TextScale=1;Apply();yield return Route("studio");foreach(var source in new[]{"D02.rubric.v2.naming","D02.rubric.v2.listing"}){W("Inspect."+source);W("Primary");}W("Editor.Rubric");yield return Type("Rubric.0.Text","Explain the source and its limits, keeping uncertainty visible.");W("Primary");W("Primary");Check(S.Rubric.Version==3&&S.CurrentApproval==null,"Native rubric edit makes prior approval historical");yield return Route("review");yield return ReviewViews("stale");
            U("Review.Release");U("Primary");Check(S.Publication==PublicationStatus.Stale,"Actual stale release remains closed");yield return Shot("stale-release-rejected-final");
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440)})
            foreach(float scale in new[]{1f,2f})
            {
                CampusDiagnostics.GameSize(size.x,size.y);ui.Applied.TextScale=scale;ui.Applied.Silent=true;ui.Applied.ReducedMotion=true;ui.Applied.HighContrast=true;Apply();yield return Frames(6);string suffix=size.x+"-text"+(int)(scale*100);
                ui.OpenTitle();yield return Shot("title-"+suffix);ui.OpenBrief();yield return Shot("brief-"+suffix);ui.OpenSettings("title");
                for(int tab=0;tab<5;tab++){typeof(PresentationView).GetMethod("ShowSettingsTab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,new object[]{tab});yield return Shot("settings-"+tab+"-"+suffix);}
                U("Secondary");ui.OpenCoach();ui.RequestHelp(Altaius.Demo02.Integration.HelpIntent.Controls);yield return Frames(8);yield return Shot("help-"+suffix);
                var before=EventSystem.current.currentSelectedGameObject;input.Keys=new[]{Key.Tab};yield return Frames(2);input.Keys=Array.Empty<Key>();yield return Frames(2);Check(EventSystem.current.currentSelectedGameObject!=before,"Native Tab changes visible focus "+suffix);
                input.Keys=new[]{Key.LeftShift,Key.Tab};yield return Frames(2);input.Keys=Array.Empty<Key>();yield return Frames(2);
                ui.CloseSurface(false);view.OpenNotebook();yield return Shot("notebook-"+suffix);view.CloseWindow();ui.OpenTimeline();yield return Shot("timeline-"+suffix);
                Check(AudioListener.volume==0&&view.LeenaDock.gameObject.activeInHierarchy,"Silent text remains available with Leena "+suffix);
            }
            ui.Applied.TextScale=1;Apply();CampusDiagnostics.GameSize(1920,1080);yield return Frames(6);
            yield return ResetMemoryCycles();Result="PASS review and accessibility";Log(Result);
        }
        public static IEnumerator ResetMemory(string output)
        {
            Bind(output);ui.CloseSurface(false);view.CloseWindow();yield return ResetMemoryCycles();Result="PASS reset memory stability";Log(Result);
        }
        static IEnumerator ResetMemoryCycles()
        {
            var memory=new List<string>{"cycle,total_allocated_bytes,total_reserved_bytes,mono_used_bytes,native_rows,objects"};int baselineRows=0,baselineObjects=0;
            for(int i=0;i<6;i++)
            {
                ui.CloseSurface(false);view.CloseWindow();ui.ConfirmLifecycle("reset");U("Primary");yield return Frames(8);ui.EnterReplay();ui.ConfirmLifecycle("linked");U("Primary");yield return Frames(8);ui.ConfirmLifecycle("reset");U("Primary");yield return Frames(8);
                GC.Collect();yield return Frames(8);var objects=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
                int rows=UnityEngine.Object.FindObjectsByType<NativeRow>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
                memory.Add(i+","+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()+","+UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong()+","+UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()+","+rows+","+objects);File.WriteAllLines(Path.Combine(folder,"reset-memory.csv"),memory);if(i==0){baselineRows=rows;baselineObjects=objects;}Check(rows==baselineRows&&objects==baselineObjects,"Reset/replay stable runtime pools after warm cycle "+i+": "+rows+" rows, "+objects+" objects");
            }
            File.WriteAllLines(Path.Combine(folder,"reset-memory.csv"),memory);
        }
        static IEnumerator RequestFixtureReview()
        {
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=true;Apply();ui.OpenReview();U("Review.Request");yield return Type("Review.Summary","Exact synthetic review state for final readable display verification.",true);yield return Type("Review.Time","13:40",true);U("Primary");U("Primary");U("Primary");U("Review.Schedule");yield return Frames(8);Check(S.ReviewStatus==ReviewStatus.Scheduled,"Native requested schedule actually displayed");
        }
        static void DecideFixture(ReviewDecision decision,string reason)
        {
            var session=ui.Host.Session;var request=S.ReviewHistory.Last();var envelope=session.Stage(new DecideReview(request.Id,request.Pair.TaskVersion,request.Pair.RubricVersion,decision,reason),"Explicit synthetic final visual fixture");var context=new ReceiptContext(envelope,S);var controller=ui.Host.TrustedScenarioController;var receipt=controller.SubmitScripted(envelope,controller.PermitExactScriptedResponse(envelope,ActorRole.ModuleLead,"Explicit synthetic final visual fixture, no learner evidence."));Check(receipt.Status==ReceiptStatus.Accepted,"Scoped synthetic "+decision);ui.Host.NotifyChanged();ui.ShowCommandReceipt(receipt,context);
        }
        static IEnumerator ReviewViews(string state)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440)})
            foreach(float scale in new[]{1f,2f})
            {
                CampusDiagnostics.GameSize(size.x,size.y);ui.Applied.TextScale=scale;Apply();yield return Frames(6);ui.OpenReview();yield return Shot(state+"-"+size.x+"-text"+(int)(scale*100));
                Check(ui.ReadPresenterControl("Review.Details"),"Exact provenance remains reachable "+state+" "+size.x+" "+scale);yield return Shot(state+"-details-scroll-"+size.x+"-text"+(int)(scale*100));
            }
            ui.Applied.TextScale=1;Apply();
        }
    }
}
