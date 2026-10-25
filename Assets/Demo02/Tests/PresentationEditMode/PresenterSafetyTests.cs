using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using Altaius.Demo02.Integration;
using Altaius.Demo02.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Altaius.Demo02.Tests
{
    public sealed class PresenterSafetyTests
    {
        Scene preview; PresentationView ui; ReviewNativeEvents events; Component runner;
        LocalPresentationPreferences preferences;
        readonly List<UnityEngine.Object> sequences=new List<UnityEngine.Object>();
        static Type PresenterType(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Altaius.Demo02.Presenter."+name)).FirstOrDefault(t=>t!=null);
        static object Get(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.Instance).Invoke(o,args);
        static void Private(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
        [SetUp] public void Setup()
        {
            preferences=LocalPresentationPreferences.Load();preview=EditorSceneManager.OpenPreviewScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            ui=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PresentationView>(true)).Single();
            var eventObject=new GameObject("Presenter isolated native events",typeof(ReviewNativeEvents));SceneManager.MoveGameObjectToScene(eventObject,preview);events=eventObject.GetComponent<ReviewNativeEvents>();events.BeginNative();EventSystem.current=events;
            ui.Host.BeginSession(SessionMode.Independent);Private(ui,"Start");Private(ui.Workbench,"Start");ui.CloseSurface(false);
        }
        [TearDown] public void Cleanup()
        {
            if(runner)Call(runner,"Cancel");foreach(var sequence in sequences)if(sequence)UnityEngine.Object.DestroyImmediate(sequence);sequences.Clear();if(events){events.SetSelectedGameObject(null);events.EndNative();}EditorSceneManager.ClosePreviewScene(preview);preferences.Save();
        }
        void Runner()
        {
            var type=PresenterType("PresenterRunner");Assert.That(type,Is.Not.Null,"A real presenter driver must own isolated session and native input lifecycle.");
            runner=ui.GetComponentsInChildren(type,true).FirstOrDefault()??ui.gameObject.AddComponent(type);
            type.GetField("UI").SetValue(runner,ui);type.GetField("Sequence").SetValue(runner,AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Demo02/Settings/Presenter/Walkthrough.asset"));
        }
        UnityEngine.Object Sequence(string steps)
        {
            var seq=ScriptableObject.CreateInstance(PresenterType("PresenterSequence"));JsonUtility.FromJsonOverwrite("{\"ScenarioId\":\"isolated-test\",\"Steps\":"+steps+"}",seq);sequences.Add(seq);return seq;
        }
        void Start(UnityEngine.Object sequence){Call(runner,"StartSequence",sequence,true);}
        [Test] public void SyntheticRunAndCancelRestoreTheOriginalFreeDraftAndSession()
        {
            var original=ui.Host.Session;original.Inspect("D02.task.v1.Q1",InspectionLocation.Studio);original.Inspect("D02.task.v1.Q2",InspectionLocation.Studio);ui.Gameplay.DirectRoute("studio");ui.Workbench.OpenTask();ui.Workbench.SetFieldText("Task.Context","My unsaved manual context");var draft=ui.Host.Workbench;
            Runner();Start(Sequence("[{\"Kind\":0,\"Dwell\":20}]"));Assert.That(ui.Host.Session,Is.Not.SameAs(original));Assert.That(ui.Host.Session.Snapshot.Mode,Is.EqualTo(SessionMode.Presenter));Assert.That(ui.Host.Session.Snapshot.Evidence,Is.Empty);
            Call(runner,"Cancel");Assert.That(ui.Host.Session,Is.SameAs(original));Assert.That(ui.Host.Workbench,Is.SameAs(draft));Assert.That(ui.Workbench.Rows.Find("Task.Context").Input.text,Is.EqualTo("My unsaved manual context"));Assert.That(ui.Gameplay.PresenterOwnsInput,Is.False);
        }
        [Test] public void PauseRetainsProgressiveTextAndCancelRestoresNativeOwnership()
        {
            Runner();Start(Sequence("[{\"Kind\":4},{\"Kind\":3,\"Control\":\"Composer\",\"Text\":\"First phrase. Second phrase.\",\"CharactersPerSecond\":10}]"));
            Call(runner,"Tick",.1f);Call(runner,"Tick",.4f);var field=ui.Surfaces.Single(s=>s.SurfaceId=="conversation").Composer;Assert.That(field.text.Length,Is.InRange(1,26));Assert.That(field.readOnly,Is.True,"Laptop keyboard text must not append to the automated draft.");var partial=field.text;
            Call(runner,"Pause");Call(runner,"Tick",5f);Assert.That(field.text,Is.EqualTo(partial));Assert.That(ui.Lifecycle.Paused,Is.True);Call(runner,"Resume");Call(runner,"Tick",.2f);Assert.That(field.text.Length,Is.GreaterThan(partial.Length));Call(runner,"Cancel");Assert.That(field.readOnly,Is.False);Assert.That(events.sendNavigationEvents,Is.True);
        }
        [Test] public void InterruptedNativeSurfaceStopsBeforeACommandCanSubmit()
        {
            Runner();Start(Sequence("[{\"Kind\":4},{\"Kind\":3,\"Control\":\"Composer\",\"Text\":\"What is your role?\",\"CharactersPerSecond\":5},{\"Kind\":2,\"Control\":\"Send\"}]"));Call(runner,"Tick",.1f);Call(runner,"Tick",.2f);ui.OpenReview();Call(runner,"Tick",.2f);
            Assert.That((bool)Get(runner,"Active"),Is.False);Assert.That(ui.Host.Session.Snapshot.Communications,Is.Empty);Assert.That(ui.Gameplay.PresenterOwnsInput,Is.False);Assert.That((string)Get(runner,"LastError"),Does.Contain("changed"));
        }
        [Test] public void ResetInvalidatesOldContinuationAndRepeatedCancelIsHarmless()
        {
            Runner();Start(Sequence("[{\"Kind\":4},{\"Kind\":3,\"Control\":\"Composer\",\"Text\":\"What is your role?\",\"CharactersPerSecond\":2},{\"Kind\":2,\"Control\":\"Send\"}]"));Call(runner,"Tick",.1f);Call(runner,"Tick",.5f);var old=ui.Host.Session;var attempt=old.Snapshot.AttemptId;Call(runner,"ResetRun");Assert.That(ui.Host.Session.Snapshot.AttemptId,Is.Not.EqualTo(attempt));Assert.That(ui.Host.Session.Snapshot.Mode,Is.EqualTo(SessionMode.Presenter));Call(runner,"Cancel");Call(runner,"Cancel");Call(runner,"Tick",100f);Assert.That(old.Snapshot.Communications,Is.Empty);Assert.That(ui.Host.Session.Snapshot.Mode,Is.EqualTo(SessionMode.Independent));
        }
        [Test] public void ChangedAuthoredSequenceCannotMintAnExactScopeReply()
        {
            Runner();var sequence=Sequence("[{\"Kind\":0,\"Dwell\":20}]");Start(sequence);JsonUtility.FromJsonOverwrite("{\"ScenarioId\":\"changed-after-start\"}",sequence);Call(runner,"Tick",.1f);Assert.That((bool)Get(runner,"Active"),Is.False);Assert.That((string)Get(runner,"LastError"),Does.Contain("changed"));Assert.That(ui.Host.Session.Snapshot.ApprovalHistory,Is.Empty);
        }
        [Test] public void ReturningToAnOriginalPreviewPreservesItsExactStagedCommand()
        {
            var session=ui.Host.Session;session.Inspect("D02.task.v1.Q1",InspectionLocation.Studio);session.Inspect("D02.task.v1.Q2",InspectionLocation.Studio);session.Inspect("S_A",InspectionLocation.Archive);session.Inspect("S_B",InspectionLocation.Archive);ui.Gameplay.DirectRoute("studio");ui.Workbench.OpenTask();ui.Workbench.SetFieldText("Task.Q1.Text","Explain both source types in this case.");ui.Workbench.SetFieldText("Task.Context","Review the term-weekday entrance.");ui.Workbench.ActivateControl("Task.Attach.S_A");ui.Workbench.ActivateControl("Task.Attach.S_B");ui.Workbench.ActivateControl("Primary");var pending=ui.Host.Workbench.Pending;
            Runner();Start(Sequence("[{\"Kind\":0,\"Dwell\":20}]"));Call(runner,"Cancel");Assert.That(ui.Workbench.ViewId,Is.EqualTo("preview"));Assert.That(ui.Host.Workbench.Pending,Is.SameAs(pending));Assert.That(session.Snapshot.Task.Version,Is.EqualTo(1));Assert.That(ui.Workbench.Rows.Find("Preview.After.Q1").Body.text,Is.EqualTo("Explain both source types in this case.\nMarks 10 | Outcomes O1"));
        }
        [Test] public void SameKindNativeRebindStopsBeforeContinuingAutomatedTyping()
        {
            Runner();Start(Sequence("[{\"Kind\":4},{\"Kind\":3,\"Control\":\"Composer\",\"Text\":\"What is your role?\",\"CharactersPerSecond\":5}]"));Call(runner,"Tick",.1f);Call(runner,"Tick",.2f);ui.OpenConversation(ui.ConversationRole);Call(runner,"Tick",.2f);Assert.That((bool)Get(runner,"Active"),Is.False);Assert.That((string)Get(runner,"LastError"),Does.Contain("changed"));
        }
        [Test] public void PendingResponseCancellationRestoresTheOriginalPendingConversation()
        {
            ui.OpenConversation(ActorRole.ModuleLead);ui.SetFieldText("Composer","What is your role?");ui.SendConversation();var original=ui.Lifecycle;var ownRequest=original.Conversation.History.Single();
            Runner();Start(Sequence("[{\"Kind\":4},{\"Kind\":3,\"Control\":\"Composer\",\"Text\":\"What is your role?\",\"CharactersPerSecond\":100},{\"Kind\":2,\"Control\":\"Send\",\"Dwell\":20}]"));for(int i=0;i<8;i++)Call(runner,"Tick",.2f);var synthetic=ui.Lifecycle;var record=synthetic.Conversation.History.Single();Assert.That(synthetic.Dispatch.PendingCount,Is.EqualTo(1));Call(runner,"Pause");Assert.That(synthetic.Dispatch.Tick(double.MaxValue,true),Is.False);Call(runner,"Resume");Call(runner,"Tick",.1f);Assert.That(synthetic.Conversation.History.Count,Is.EqualTo(1));Call(runner,"Cancel");Assert.That(record.State,Is.EqualTo(ReplyState.Cancelled));Assert.That(synthetic.Dispatch.PendingCount,Is.Zero);Assert.That(ui.Lifecycle,Is.SameAs(original));Assert.That(original.Dispatch.PendingCount,Is.EqualTo(1));Assert.That(ownRequest.Request.OwnText,Is.EqualTo("What is your role?"));Assert.That(ui.Gameplay.PresenterOwnsInput,Is.False);
        }
        [Test] public void WorkbenchNativeRebindStopsEvenWhenKindAndDraftTextAreIdentical()
        {
            Runner();Start(Sequence("[{\"Kind\":6,\"Station\":\"studio\"},{\"Kind\":1,\"Control\":\"Inspect.D02.rubric.v1.naming\"},{\"Kind\":1,\"Control\":\"Primary\"},{\"Kind\":6,\"Station\":\"experiment\"},{\"Kind\":1,\"Control\":\"Work.W_LIST\"},{\"Kind\":1,\"Control\":\"Criterion.naming\"},{\"Kind\":5,\"Control\":\"Mark.Marks\",\"Text\":\"10\",\"CharactersPerSecond\":1}]"));for(int i=0;i<7;i++)Call(runner,"Tick",.1f);Assert.That(ui.Workbench.ViewId,Is.EqualTo("mark"));ui.Workbench.OpenMark("W_LIST","naming");Call(runner,"Tick",.1f);Assert.That((bool)Get(runner,"Active"),Is.False);Assert.That((string)Get(runner,"LastError"),Does.Contain("changed"));
        }

        void OriginalListMark()
        {
            ui.Host.Session.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");ui.Workbench.OpenWork("W_LIST");ui.Workbench.OpenMark("W_LIST","naming");ui.Workbench.SetFieldText("Mark.Marks","7");ui.Workbench.SetFieldText("Mark.Rationale","My original W_LIST marking draft.");
        }
        void PresenterSelectsPitchAndReturns()
        {
            Runner();Start(Sequence("[{\"Kind\":6,\"Station\":\"experiment\"},{\"Kind\":1,\"Control\":\"Work.W_PITCH\"},{\"Kind\":0,\"Dwell\":20}]"));Call(runner,"Tick",.1f);Call(runner,"Tick",.1f);
            Assert.That(ui.Workbench.SelectedWork,Is.EqualTo("W_PITCH"));Assert.That(ui.Gameplay.SelectedSpecimenId,Is.EqualTo("W_PITCH"));Call(runner,"Cancel");
        }
        string RequestedHelpScope()
        {ui.OpenCoach();ui.RequestHelp(HelpIntent.Controls);return ui.Surfaces.Single(s=>s.SurfaceId=="coach").Rows.Find("Coach.Identity").Body.text;}
        [Test] public void HandbackRestoresDisplayedMarkAndBothOriginalWorkSelections()
        {
            OriginalListMark();var original=ui.Host.Session;var before=original.Snapshot;PresenterSelectsPitchAndReturns();
            {Assert.That(ui.Host.Session,Is.SameAs(original));Assert.That(ui.Workbench.ViewId,Is.EqualTo("mark"));Assert.That(ui.Host.Workbench.Mark.Work.SourceId,Is.EqualTo("W_LIST"));Assert.That(ui.Workbench.Rows.Find("Mark.Marks").Input.text,Is.EqualTo("7"));Assert.That(ui.Workbench.Rows.Find("Mark.Rationale").Input.text,Is.EqualTo("My original W_LIST marking draft."));Assert.That(ui.Workbench.SelectedWork,Is.EqualTo("W_LIST"));Assert.That(ui.Gameplay.SelectedSpecimenId,Is.EqualTo("W_LIST"));Assert.That(original.Snapshot.WorldRevision,Is.EqualTo(before.WorldRevision));Assert.That(original.Snapshot.Notebook.Select(p=>p.Reference.SourceId),Is.EquivalentTo(before.Notebook.Select(p=>p.Reference.SourceId)));Assert.That(original.Snapshot.Audit.Count,Is.EqualTo(before.Audit.Count));}
        }
        [Test] public void RestoredMarkEscapeAndCoachUseTheOriginalWorkOpportunity()
        {
            OriginalListMark();var before=ui.Host.Session.Snapshot;PresenterSelectsPitchAndReturns();
            ui.Gameplay.HandleEscape();Assert.That(ui.Workbench.ViewId,Is.EqualTo("work"));Assert.That(ui.Workbench.Heading.text,Does.StartWith("W_LIST"));Assert.That(ui.Workbench.SelectedWork,Is.EqualTo("W_LIST"));Assert.That(ui.Gameplay.SelectedSpecimenId,Is.EqualTo("W_LIST"));Assert.That(ui.Host.Session.Snapshot.WorldRevision,Is.EqualTo(before.WorldRevision));Assert.That(ui.Host.Session.Snapshot.Notebook.Select(p=>p.Reference.SourceId),Is.EquivalentTo(before.Notebook.Select(p=>p.Reference.SourceId)));Assert.That(RequestedHelpScope(),Does.Contain("Opportunity "+WorkbenchModel.OpportunityForWork("W_LIST")));
        }
        [Test] public void RestoredMarkCoachRequestIsScopedToTheOriginalWork()
        {
            OriginalListMark();PresenterSelectsPitchAndReturns();Assert.That(RequestedHelpScope(),Does.Contain("Opportunity "+WorkbenchModel.OpportunityForWork("W_LIST")));
        }
        [TestCase("world")] [TestCase("task")]
        public void HandbackRestoresNullSelectionWithoutInspectingPresenterWork(string surface)
        {
            if(surface=="task"){ui.Host.Session.Inspect("D02.task.v1.Q1",InspectionLocation.Studio);ui.Host.Session.Inspect("D02.task.v1.Q2",InspectionLocation.Studio);ui.Gameplay.DirectRoute("studio");ui.Workbench.OpenTask();ui.Workbench.SetFieldText("Task.Context","My original unselected task draft.");}
            var before=ui.Host.Session.Snapshot;Assert.That(ui.Workbench.SelectedWork,Is.Null);Assert.That(ui.Gameplay.SelectedSpecimenId,Is.Null);PresenterSelectsPitchAndReturns();
            {Assert.That(ui.Workbench.SelectedWork,Is.Null);Assert.That(ui.Gameplay.SelectedSpecimenId,Is.Null);if(surface=="task"){Assert.That(ui.Workbench.ViewId,Is.EqualTo("task"));Assert.That(ui.Workbench.Rows.Find("Task.Context").Input.text,Is.EqualTo("My original unselected task draft."));}Assert.That(ui.Host.Session.Snapshot.WorldRevision,Is.EqualTo(before.WorldRevision));Assert.That(ui.Host.Session.Snapshot.Notebook.Select(p=>p.Reference.SourceId),Is.EquivalentTo(before.Notebook.Select(p=>p.Reference.SourceId)));Assert.That(RequestedHelpScope(),Does.Contain("Opportunity "+LocalAuthority.PublicationOpportunity));}
        }
        [TestCase("world")] [TestCase("task")]
        public void HandbackRestoresPriorSelectionOnOtherOriginalSurfaces(string surface)
        {
            ui.Gameplay.DirectRoute("experiment");ui.Workbench.OpenWork("W_COMPARE");ui.Workbench.CloseWindow();
            if(surface=="task"){ui.Host.Session.Inspect("D02.task.v1.Q1",InspectionLocation.Studio);ui.Host.Session.Inspect("D02.task.v1.Q2",InspectionLocation.Studio);ui.Gameplay.DirectRoute("studio");ui.Workbench.OpenTask();}
            var before=ui.Host.Session.Snapshot;PresenterSelectsPitchAndReturns();
            {Assert.That(ui.Workbench.SelectedWork,Is.EqualTo("W_COMPARE"));Assert.That(ui.Gameplay.SelectedSpecimenId,Is.EqualTo("W_COMPARE"));Assert.That(ui.Host.Session.Snapshot.WorldRevision,Is.EqualTo(before.WorldRevision));Assert.That(ui.Host.Session.Snapshot.Notebook.Select(p=>p.Reference.SourceId),Is.EquivalentTo(before.Notebook.Select(p=>p.Reference.SourceId)));Assert.That(RequestedHelpScope(),Does.Contain("Opportunity "+WorkbenchModel.OpportunityForWork("W_COMPARE")));}
        }
        [Test] public void WorkSurfaceHandbackPreservesIndependentSelectionOwnersWithoutNewInspections()
        {
            ui.Gameplay.DirectRoute("experiment");ui.Workbench.OpenWork("W_COMPARE");ui.Workbench.OpenWork("W_LIST");ui.Gameplay.SelectInspectedWork("W_COMPARE");var before=ui.Host.Session.Snapshot;PresenterSelectsPitchAndReturns();
            {Assert.That(ui.Workbench.ViewId,Is.EqualTo("work"));Assert.That(ui.Workbench.Heading.text,Does.StartWith("W_LIST"));Assert.That(ui.Workbench.SelectedWork,Is.EqualTo("W_LIST"));Assert.That(ui.Gameplay.SelectedSpecimenId,Is.EqualTo("W_COMPARE"));Assert.That(ui.Host.Session.Snapshot.WorldRevision,Is.EqualTo(before.WorldRevision));Assert.That(ui.Host.Session.Snapshot.Notebook.Select(p=>p.Reference.SourceId),Is.EquivalentTo(before.Notebook.Select(p=>p.Reference.SourceId)));}
        }

    }
}
