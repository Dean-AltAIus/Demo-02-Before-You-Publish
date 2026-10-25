using System;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
using Altaius.Demo02.Presentation;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Altaius.Demo02.Tests
{
    public sealed class PresentationReviewRegressionTests
    {
        GameObject root;PresentationSurface surface;NativeRowPool pool;
        [SetUp]public void Setup()
        {
            root=new GameObject("Isolated native delivery surface",typeof(RectTransform));surface=root.AddComponent<PresentationSurface>();
            pool=root.AddComponent<NativeRowPool>();var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Prefabs/UI/Presentation/TextRow.prefab");
            pool.TextPrefab=asset.GetComponent<NativeRow>();pool.AuthoredRows.Add(UnityEngine.Object.Instantiate(asset,root.transform).GetComponent<NativeRow>());
        }
        [TearDown]public void Cleanup()=>UnityEngine.Object.DestroyImmediate(root);
        [Test]public void RecycledReplyRowCannotAcknowledgeTheEarlierUnseenRole()
        {
            var s=OfflineSession.Create();var c=new ConversationCoordinator(s);var a=c.Send(ActorRole.Colleague,new string('x',600));c.Process(a.Request.Id);var b=c.Send(ActorRole.ModuleLead,"What is your role?");c.Process(b.Request.Id);
            pool.Begin();var row=pool.Take(NativeRowKind.Text,"reply."+a.Request.Id);row.BindText("Colleague",a.Reply.Text,2);
            var old=new RenderedRowDelivery(row.Body,surface,a.Request.Id,()=>c.Acknowledge(new DisplayAcknowledgement(s.Snapshot.AttemptId,a.Request.Id,Guid.NewGuid())));
            Assert.That(old.TryCommit(false),Is.False);pool.Begin();var reused=pool.Take(NativeRowKind.Text,"reply."+b.Request.Id);Assert.That(reused,Is.SameAs(row));reused.BindText("Module reviewer",b.Reply.Text,2);
            var visible=new RenderedRowDelivery(reused.Body,surface,b.Request.Id,()=>c.Acknowledge(new DisplayAcknowledgement(s.Snapshot.AttemptId,b.Request.Id,Guid.NewGuid())));
            Assert.That(old.TryCommit(true),Is.False,"Reusing the object must not count as displaying its old reply.");Assert.That(visible.TryCommit(true),Is.True);Assert.That(a.State,Is.EqualTo(ReplyState.Ready));Assert.That(b.State,Is.EqualTo(ReplyState.Shown));
        }
        [Test]public void RecycledCoachRowRecordsOnlyTheActuallyVisibleHelp()
        {
            var s=OfflineSession.Create(SessionMode.Coached,out var controller);var work=s.Inspect("W_LIST",InspectionLocation.ExperimentTable);s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);s.Submit(s.Stage(new Annotate("W_LIST",1,"naming",new[]{work.Reference},10,"Names the sources",1,1),"D02.counterexample.W_LIST"));
            var help=new HelpCoordinator(s,controller);var strategy=help.Request(HelpIntent.RequestedStrategy,"D02.counterexample.W_LIST");var controls=help.Request(HelpIntent.Controls,"D02.controls");
            pool.Begin();var row=pool.Take(NativeRowKind.Text,"strategy");row.BindText("Strategy",strategy.Text,2);var old=new RenderedRowDelivery(row.Body,surface,strategy.Id,()=>help.Acknowledge(strategy),true);
            pool.Begin();var reused=pool.Take(NativeRowKind.Text,"controls");reused.BindText("Controls",controls.Text,2);var shown=new RenderedRowDelivery(reused.Body,surface,controls.Id,()=>help.Acknowledge(controls),true);
            Assert.That(old.TryCommit(true),Is.False);Assert.That(shown.TryCommit(true),Is.True);Assert.That(s.Snapshot.Assistance.Count,Is.EqualTo(1));Assert.That(s.Snapshot.Assistance.Single().Strategic,Is.False);Assert.That(strategy.Shown,Is.False);
        }
        [Test]public void ConcurrentRoleDispatchSurvivesPauseAndCancelsOnlyItsOriginalRequest()
        {
            var s=OfflineSession.Create();var c=new ConversationCoordinator(s);var queue=new ConversationDispatchQueue(c);var a=c.Send(ActorRole.Colleague,"What is your role?");var b=c.Send(ActorRole.ModuleLead,"What is your role?");queue.Schedule(a,1);queue.Schedule(b,1);
            Assert.That(queue.Tick(2,true),Is.False);Assert.That(a.State,Is.EqualTo(ReplyState.Sending));Assert.That(b.State,Is.EqualTo(ReplyState.Sending));
            Assert.That(queue.Cancel(new CancelRequest(s.Snapshot.AttemptId,b.Request.Id)),Is.True);Assert.That(queue.Tick(2,false),Is.True);Assert.That(a.State,Is.EqualTo(ReplyState.Ready));Assert.That(b.State,Is.EqualTo(ReplyState.Cancelled));Assert.That(c.Draft(ActorRole.ModuleLead),Is.EqualTo("What is your role?"));
        }
        [Test]public void ConcurrentRoleDispatchProcessesBothRequestsWithoutChangingTheirIds()
        {var s=OfflineSession.Create();var c=new ConversationCoordinator(s);var queue=new ConversationDispatchQueue(c);var a=c.Send(ActorRole.Colleague,"What is your role?");var b=c.Send(ActorRole.ExperimentReviewer,"What is your role?");queue.Schedule(a,1);queue.Schedule(b,1);Assert.That(queue.Tick(2,false),Is.True);Assert.That(a.State,Is.EqualTo(ReplyState.Ready));Assert.That(b.State,Is.EqualTo(ReplyState.Ready));Assert.That(queue.PendingCount,Is.EqualTo(0));}
        [Test]public void OwningSurfaceRebindInvalidatesIdenticalTextAndFreshDeliveryCommitsOnlyOnce()
        {
            pool.Begin();var row=pool.Take(NativeRowKind.Text,"same.control");row.BindText("Reply","Identical text",1);int oldCount=0,newCount=0;var old=new RenderedRowDelivery(row.Body,surface,Guid.NewGuid(),()=>oldCount++);
            surface.BeginBinding();Assert.That(old.TryCommit(true),Is.False);var fresh=new RenderedRowDelivery(row.Body,surface,Guid.NewGuid(),()=>newCount++);Assert.That(fresh.TryCommit(true),Is.True);Assert.That(fresh.TryCommit(true),Is.False);Assert.That(oldCount,Is.EqualTo(0));Assert.That(newCount,Is.EqualTo(1));
        }
        [Test]public void RevisingFullMarksDoesNotEraseTheEarlierRecordedFullMarkAction()
        {
            var s=OfflineSession.Create();var work=s.Inspect("W_LIST",InspectionLocation.ExperimentTable);s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);s.Inspect("D02.rubric.v1.listing",InspectionLocation.Studio);
            foreach(var criterion in new[]{"naming","listing"})s.Submit(s.Stage(new Annotate("W_LIST",1,criterion,new[]{work.Reference},10,"The literal work meets this criterion in my marking.",1,1),"D02.counterexample.W_LIST"));
            s.Submit(s.Stage(new Annotate("W_LIST",1,"listing",new[]{work.Reference},9,"I revised my marking.",1,1),"D02.counterexample.W_LIST"));
            Assert.That(s.Snapshot.AnnotationHistory.Count,Is.EqualTo(3));Assert.That(s.Snapshot.DraftTotal("W_LIST"),Is.EqualTo(19));Assert.That(AttemptLifecycle.BoundedObservation(s.Snapshot),Does.Not.Contain("not recorded in this attempt"));Assert.That(AttemptLifecycle.BoundedObservation(s.Snapshot),Does.Contain("most recent"));
        }
    }
    public sealed class ReviewNativeEvents : EventSystem
    {public void BeginNative()=>base.OnEnable();public void EndNative()=>base.OnDisable();}
    public sealed class RetainedWorkspaceSettingsTests
    {
        Scene preview;PresentationView ui;WorkbenchView view;ReviewNativeEvents events;LocalPresentationPreferences preferences;
        [SetUp]public void Setup()
        {
            preferences=LocalPresentationPreferences.Load();preview=EditorSceneManager.OpenPreviewScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");var all=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();ui=all.Select(t=>t.GetComponent<PresentationView>()).First(x=>x);view=ui.Workbench;
            var eventObject=new GameObject("Isolated registered native events",typeof(ReviewNativeEvents));SceneManager.MoveGameObjectToScene(eventObject,preview);events=eventObject.GetComponent<ReviewNativeEvents>();events.BeginNative();EventSystem.current=events;ui.Host.BeginSession(SessionMode.Independent);Call(ui,"Start");Call(view,"Start");ui.CloseSurface(false);
        }
        static void Call(object instance,string method)=>instance.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(instance,null);
        [TearDown]public void Cleanup(){if(events){events.SetSelectedGameObject(null);events.EndNative();}EditorSceneManager.ClosePreviewScene(preview);preferences.Save();}
        [TestCase(0,1f)] [TestCase(1,1.25f)] [TestCase(2,1.5f)] [TestCase(3,2f)]
        public void SettingsRescaleRetainedMarkingRowsAndRestoreOwnDraftFocus(int selected,float scale)
        {
            var s=ui.Host.Session;s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");view.SetFieldText("Mark.Marks","7");view.SetFieldText("Mark.Rationale","My retained own rationale");var input=view.Rows.Find("Mark.Rationale").Input;
            ui.Pause();ui.OpenSettings("pause");var settings=ui.Surfaces.Single(x=>x.SurfaceId=="settings");settings.TextSizeDropdown.value=selected;ui.ActivateControl("Primary");ui.Resume();
            Assert.That(view.ViewId,Is.EqualTo("mark"));Assert.That(view.Rows.Find("Mark.Rationale").Input,Is.SameAs(input));Assert.That(input.text,Is.EqualTo("My retained own rationale"));Assert.That(ui.Host.Workbench.Mark.Rationale,Is.EqualTo("My retained own rationale"));
            Assert.That(input.textComponent.fontSize,Is.EqualTo(Mathf.RoundToInt(21*scale)));Assert.That(view.Rows.Find("Mark.Rationale").InputLayout.preferredHeight,Is.EqualTo(112*scale));Assert.That(view.HudTitle.fontSize,Is.EqualTo(Mathf.RoundToInt(23*scale)));Assert.That(view.LeenaIdentity.fontSize,Is.EqualTo(Mathf.RoundToInt(21*scale)));Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(input.gameObject));
        }
        [Test]public void ViewQueuesBothRolesAndCancellingOneDoesNotStrandTheOther()
        {
            ui.OpenConversation(ActorRole.Colleague);ui.SetFieldText("Composer","What is your role?");ui.SendConversation();var a=ui.Lifecycle.Conversation.History.Last();ui.OpenConversation(ActorRole.ModuleLead);ui.SetFieldText("Composer","What is your role?");ui.SendConversation();var b=ui.Lifecycle.Conversation.History.Last();ui.CancelConversation();ui.Lifecycle.Dispatch.Tick(double.MaxValue,false);
            Assert.That(a.State,Is.EqualTo(ReplyState.Ready));Assert.That(b.State,Is.EqualTo(ReplyState.Cancelled));
        }
        [Test]public void SettingsResumeRetainsTheTaskEditorDraftAndItsFocusedField()
        {
            var s=ui.Host.Session;s.Inspect("D02.task.v1.Q1",InspectionLocation.Studio);s.Inspect("D02.task.v1.Q2",InspectionLocation.Studio);ui.Gameplay.DirectRoute("studio");view.OpenTask();view.SetFieldText("Task.Q1.Text","My own revised question, not yet saved.");view.SetFieldText("Task.Context","My retained decision context");var input=view.Rows.Find("Task.Context").Input;
            ui.Pause();ui.OpenSettings("pause");ui.Surfaces.Single(x=>x.SurfaceId=="settings").TextSizeDropdown.value=2;ui.ActivateControl("Primary");ui.Resume();
            Assert.That(view.ViewId,Is.EqualTo("task"));Assert.That(input.text,Is.EqualTo("My retained decision context"));Assert.That(view.Rows.Find("Task.Q1.Text").Input.text,Is.EqualTo("My own revised question, not yet saved."));Assert.That(input.textComponent.fontSize,Is.EqualTo(32));Assert.That(input.GetComponent<LayoutElement>().preferredHeight,Is.EqualTo(168));Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(input.gameObject));Assert.That(s.Snapshot.Task.Version,Is.EqualTo(1));
        }
    }
}
