using System;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presentation;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Altaius.Demo02.Tests
{
    public sealed class EnglishBoundaryTests
    {
        Scene preview; PresentationView ui; WorkbenchView view; ReviewNativeEvents events;
        LocalPresentationPreferences preferences;
        const string Own = "Not approved \u2013 20 kg \u2014  not 2 kg.  {task_version} $1 <b>literal</b>\nArabic: \u0644\u0627";
        [SetUp] public void Setup()
        {
            preferences=LocalPresentationPreferences.Load();
            preview=EditorSceneManager.OpenPreviewScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            ui=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PresentationView>(true)).Single();view=ui.Workbench;
            var go=new GameObject("Isolated native English events",typeof(ReviewNativeEvents));SceneManager.MoveGameObjectToScene(go,preview);
            events=go.GetComponent<ReviewNativeEvents>();events.BeginNative();EventSystem.current=events;
            ui.Host.BeginSession(SessionMode.Independent);Call(ui,"Start");Call(view,"Start");ui.CloseSurface(false);
        }
        static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
        [TearDown] public void Cleanup(){if(events){events.SetSelectedGameObject(null);events.EndNative();}EditorSceneManager.ClosePreviewScene(preview);preferences.Save();}
        [Test] public void MarkDraftRemainsLiteralAfterNativeReopenScaleAndLaterEdit()
        {
            var s=ui.Host.Session;s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");
            view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");view.SetFieldText("Mark.Marks","7");view.SetFieldText("Mark.Rationale",Own);
            view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");view.SetTextScale(2);
            var field=view.Rows.Find("Mark.Rationale").Input;
            Assert.That(field.text,Is.EqualTo(Own));Assert.That(field.textComponent.supportRichText,Is.False);
            field.text+=" !";Assert.That(ui.Host.Workbench.Mark.Rationale,Is.EqualTo(Own+" !"));
            view.SaveAnnotation();Assert.That(s.Snapshot.AnnotationHistory.Last().Content.Rationale,Is.EqualTo(Own+" !"));
        }
        [Test] public void TaskAndRubricDraftRemainLiteralThroughPreviewCancelAndReopen()
        {
            var s=ui.Host.Session;foreach(var id in new[]{"D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})s.Inspect(id,InspectionLocation.Studio);
            ui.Gameplay.DirectRoute("studio");view.OpenTask();view.SetFieldText("Task.Q1.Text",Own);view.SetFieldText("Task.Context",Own);
            s.Inspect("S_A",InspectionLocation.Archive);s.Inspect("S_B",InspectionLocation.Archive);ui.Host.Workbench.Task.SourceIds.UnionWith(new[]{"S_A","S_B"});
            view.ActivateControl("Primary");Assert.That(view.ViewId,Is.EqualTo("preview"));view.ActivateControl("Secondary");view.OpenTask();
            Assert.That(view.Rows.Find("Task.Q1.Text").Input.text,Is.EqualTo(Own));Assert.That(view.Rows.Find("Task.Context").Input.text,Is.EqualTo(Own));
            view.OpenRubric();view.SetFieldText("Rubric.0.Text",Own);view.ActivateControl("Primary");view.ActivateControl("Secondary");view.OpenRubric();
            Assert.That(view.Rows.Find("Rubric.0.Text").Input.text,Is.EqualTo(Own));
        }
        [Test] public void EmptyNumericMarkUsesNumericPromptWithoutPrefill()
        {
            var s=ui.Host.Session;s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");
            var field=view.Rows.Find("Mark.Marks").Input;Assert.That(field.text,Is.Empty);
            Assert.That(((Text)field.placeholder).text,Does.Contain("whole number"));Assert.That(view.Primary.interactable,Is.False);
        }
        [Test] public void SavedTaskAndRubricPassagesAndNotebookPreserveLiteralPayload()
        {
            var s=ui.Host.Session;
            foreach(var id in new[]{"D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})s.Inspect(id,InspectionLocation.Studio);
            s.Inspect("S_A",InspectionLocation.Archive);s.Inspect("S_B",InspectionLocation.Archive);
            ui.Gameplay.DirectRoute("studio");view.OpenTask();view.SetFieldText("Task.Q1.Text",Own);view.SetFieldText("Task.Context",Own);
            ui.Host.Workbench.Task.SourceIds.UnionWith(new[]{"S_A","S_B"});
            view.ActivateControl("Primary");view.ActivateControl("Primary");
            Assert.That(s.Snapshot.Task.Version,Is.EqualTo(2));Assert.That(s.Snapshot.Task.Questions[0].Text,Is.EqualTo(Own));
            view.OpenRubric();view.SetFieldText("Rubric.0.Text",Own);view.ActivateControl("Primary");view.ActivateControl("Primary");
            Assert.That(s.Snapshot.Rubric.Version,Is.EqualTo(2));
            foreach(var id in new[]{"D02.task.v2.Q1","D02.rubric.v2.naming"})
            {
                view.OpenPassage(id,"sources");Assert.That(view.Rows.Find("Passage.Body").Body.text,Is.EqualTo(Own));
                Assert.That(view.Rows.Find("Passage.Body").Body.supportRichText,Is.False);
            }
            view.OpenNotebook();
            foreach(var id in new[]{"D02.task.v2.Q1","D02.rubric.v2.naming"})
            {Assert.That(view.Rows.Find("Notebook."+id).Body.text,Is.EqualTo(Own));Assert.That(s.Snapshot.Notebook.Single(p=>p.Reference.SourceId==id).Reference.Sha256,Is.EqualTo(ContentDigest.Text(Own)));}
            var source=s.Snapshot.Notebook.Single(p=>p.Reference.SourceId=="S_A");
            Assert.That(ContentDigest.Text(source.DisplayText),Is.EqualTo(source.Reference.Sha256),"Admitted source projection must have its exact source bytes.");
            ui.Gameplay.DirectRoute("experiment");view.OpenWork("W_COMPARE");
            var work=s.Snapshot.Notebook.Single(p=>p.Reference.SourceId=="W_COMPARE");
            Assert.That(ContentDigest.Text(view.Rows.Find("Work.Literal").Body.text),Is.EqualTo(work.Reference.Sha256));
            view.OpenMark("W_COMPARE","naming");
            Assert.That(view.Rows.AuthoredRows.Where(r=>r.gameObject.activeSelf&&r.Body).Any(r=>r.Body.text.Contains(Own)),Is.True,"Marking must expose the saved literal criterion.");
        }
        [Test] public void OriginalReceiptDetailsSurviveOtherReceiptsAndLinkedPractice()
        {
            var s=ui.Host.Session;var pair=s.Snapshot.Pair;
            ui.OpenReview();ui.Surfaces.Single(x=>x.SurfaceId=="review").Rows.Find("Review.Release").Action.onClick.Invoke();var first=ui.Pending;ui.CommitDomain();
            var receipt=s.FindReceipt(first.CommandId);
            s.Submit(s.Stage(new EditRubric(1,s.Snapshot.Rubric.Criteria),"history-native"));
            ui.OpenReview();ui.Surfaces.Single(x=>x.SurfaceId=="review").Rows.Find("Review.Release").Action.onClick.Invoke();ui.CommitDomain();
            ui.OpenTimeline();var timeline=ui.Surfaces.Single(x=>x.SurfaceId=="timeline");
            Assert.That(timeline.Rows.Find("Timeline.Command.1").Body.text,Does.Contain("Task 1 / Rubric 1"));
            timeline.Rows.Find("Timeline.Command.Details.1").Action.onClick.Invoke();
            var surface=ui.Surfaces.Single(x=>x.SurfaceId=="receipt");var details=surface.Rows.Find("History.Scope").Body;
            Assert.That(details.text,Does.Contain(pair.RubricDigest));Assert.That(details.text,Does.Contain(first.AttemptId.ToString()));Assert.That(details.supportRichText,Is.False);
            ui.Lifecycle.Reset(true);ui.OpenTimeline();timeline.Rows.Find("Timeline.Linked.Commands."+first.AttemptId).Action.onClick.Invoke();
            timeline.Rows.Find("Timeline.Command.Details.1").Action.onClick.Invoke();Assert.That(surface.Rows.Find("History.Scope").Body.text,Does.Contain(pair.RubricDigest));
            Assert.That(s.Snapshot.Evidence,Is.Empty);Assert.That(s.FindReceipt(receipt.RequestId),Is.Null);
            ui.Lifecycle.Reset(false);ui.OpenTimeline();Assert.That(s.Snapshot.LinkedHistory,Is.Empty);
        }
        [Test] public void AnnotationRetriesDoNotRequeueAfterPendingFullQueueOrCancellation()
        {
            var c=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampusContactDirector>(true)).Single();var s=ui.Host.Session;
            s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");Call(c,"Observe");c.Cancel();
            var work=s.Snapshot.Notebook.Single(p=>p.Reference.SourceId=="W_LIST");
            var e=s.Stage(new Annotate("W_LIST",1,"naming",new[]{work.Reference},4,Own,1,1),"contact-retry");s.Submit(e);s.Submit(e);Call(c,"Observe");
            Assert.That(c.QueuedCount,Is.EqualTo(1));s.Submit(e);Call(c,"Observe");Assert.That(c.QueuedCount,Is.EqualTo(1));
            c.Cancel();s.Submit(e);Call(c,"Observe");Assert.That(c.QueuedCount,Is.Zero);
            for(int i=0;i<8;i++)Assert.That(c.Request("MarkPoint"),Is.True);
            var next=s.Stage(new Annotate("W_LIST",1,"naming",new[]{work.Reference},5,Own,1,1),"contact-retry");s.Submit(next);s.Submit(next);Call(c,"Observe");Assert.That(c.QueuedCount,Is.EqualTo(8));
            c.Cancel();s.Submit(next);Call(c,"Observe");Assert.That(c.QueuedCount,Is.Zero,"Cancellation settles deferred arrivals too.");Assert.That(s.Snapshot.AnnotationHistory.Count,Is.EqualTo(2));
        }
    }
}
