using System;
using System.Linq;
using NUnit.Framework;
using Altaius.Demo02.Domain;

namespace Altaius.Demo02.Tests
{
    public class ReviewRegressionTests
    {
        private OfflineSession session;
        private ScenarioController controller;
        [SetUp] public void Setup() { session = OfflineSession.Create(SessionMode.Coached, out controller); }
        private CommandReceipt Commit(DomainCommand command, string opportunity = "pair-repair")
        {
            var r = session.Submit(session.Stage(command, opportunity));
            Assert.That(r.Status, Is.EqualTo(ReceiptStatus.Accepted), r.ReasonCode); return r;
        }
        private Annotate MarkingCommand()
        {
            var work = session.Inspect("W_LIST", InspectionLocation.ExperimentTable).Reference;
            session.Inspect("D02.rubric.v1.naming", InspectionLocation.Studio);
            return new Annotate("W_LIST", 1, "naming", new[] { work }, 10, "It names two sources.", 1, 1);
        }
        private CommandReceipt Script(DomainCommand command)
        {
            var e = session.Stage(command, "authored-review-response");
            var permit = controller.PermitExactScriptedResponse(e, ActorRole.ModuleLead, "Exact authored response delivered for this request.");
            return controller.SubmitScripted(e, permit);
        }
        private Guid Schedule()
        {
            var pair = session.Snapshot.Pair;
            var id = Commit(new RequestReview(pair.TaskVersion, pair.RubricVersion, "Review this pair.", "13:40"), "pair-review").RequestId;
            Assert.That(Script(new ScheduleReply(id, pair.TaskVersion, pair.RubricVersion, "13:40", ScheduleDecision.Accepted, Guid.NewGuid())).Status, Is.EqualTo(ReceiptStatus.Accepted));
            return id;
        }

        [TestCase(SessionMode.Presenter, false)]
        [TestCase(SessionMode.Presenter, true)]
        [TestCase(SessionMode.Independent, false)]
        [TestCase(SessionMode.Independent, true)]
        public void ReplayResetRetainsUnderlyingModeAndEvidenceBoundary(SessionMode originalMode, bool linked)
        {
            session = OfflineSession.Create(originalMode, out controller);
            session.EnterReplay(); session.EnterReplay(); session.Reset(linked);
            Assert.That(session.Snapshot.Mode, Is.EqualTo(originalMode));
            var receipt = Commit(MarkingCommand(), "restarted-marking");
            Assert.That(receipt.Synthetic, Is.EqualTo(originalMode == SessionMode.Presenter));
            if (originalMode == SessionMode.Presenter)
            { Assert.That(session.Snapshot.Evidence, Is.Empty); Assert.That(session.Snapshot.HasLearnerEvidence, Is.False); }
            else Assert.That(session.Snapshot.Evidence.Single().Assisted, Is.EqualTo(linked));
        }

        [Test] public void SameActionDistinctOpportunitiesRetainRelevantHelpAndSeparateEvidence()
        {
            var help = Guid.NewGuid();
            Assert.That(controller.DeliverAssistance(session.Snapshot.AttemptId, help, "counterexample-marking", "Consider what this work demonstrates.", true, true), Is.True);
            var command = MarkingCommand();
            var helped = session.Stage(command, "counterexample-marking");
            var plain = session.Stage(command, "unseen-transfer-marking");
            Assert.That(helped.AssistanceContextIds, Is.EqualTo(new[] { help })); Assert.That(plain.AssistanceContextIds, Is.Empty);
            Commit(command, "counterexample-marking"); Commit(command, "unseen-transfer-marking");
            Assert.That(session.Snapshot.Evidence.Count, Is.EqualTo(2));
            Assert.That(session.Snapshot.Evidence[0].OpportunityId, Is.EqualTo("counterexample-marking"));
            Assert.That(session.Snapshot.Evidence[0].Assisted, Is.True);
            Assert.That(session.Snapshot.Evidence[1].OpportunityId, Is.EqualTo("unseen-transfer-marking"));
            Assert.That(session.Snapshot.Evidence[1].Assisted, Is.False);
            Assert.That(session.Snapshot.Evidence[1].AssistanceKnown, Is.True);
        }

        [Test] public void OpportunityCannotBeChangedOnRetryOrLoseItsActualHelpContext()
        {
            var command = MarkingCommand();
            controller.DeliverAssistance(session.Snapshot.AttemptId, Guid.NewGuid(), "guided-marking", "Look for evidence.", true, true);
            var staged = session.Stage(command, "guided-marking");
            var stripped = new CommandEnvelope(staged.CommandId, staged.AttemptId, staged.ExpectedWorldRevision, command,
                staged.Readset.ToDictionary(x => x.Key, x => x.Value), "guided-marking", staged.SourceRefs);
            var before = session.Snapshot.WorldRevision;
            Assert.That(session.Submit(stripped).ReasonCode, Is.EqualTo("AssistanceScopeMismatch"));
            Assert.That(session.Snapshot.WorldRevision, Is.EqualTo(before));
            staged = session.Stage(command, "guided-marking"); Assert.That(session.Submit(staged).Status, Is.EqualTo(ReceiptStatus.Accepted));
            var changed = new CommandEnvelope(staged.CommandId, staged.AttemptId, staged.ExpectedWorldRevision, command,
                staged.Readset.ToDictionary(x => x.Key, x => x.Value), "independent-marking", staged.SourceRefs, assistanceContextIds: staged.AssistanceContextIds);
            Assert.That(session.Submit(changed).ReasonCode, Is.EqualTo("CommandIdConflict"));
        }

        [Test] public void TaskOutcomeChangeBlocksExactApprovalUntilRubricIsReconciled()
        {
            var sources = new[] { session.Inspect("S_A", InspectionLocation.Archive).Reference, session.Inspect("S_B", InspectionLocation.Archive).Reference };
            Commit(new EditTask(1, new[] { new Question("Q1", "Evaluate the first source.", 10, "O2"), new Question("Q2", "Evaluate the second source.", 10, "O2") }, sources, "Should we trial a steward?"));
            var request = Schedule(); var before = session.Snapshot;
            var rejected = Script(new DecideReview(request, 2, 1, ReviewDecision.Approved, "Scripted approval request."));
            Assert.That(rejected.Status, Is.EqualTo(ReceiptStatus.Rejected)); Assert.That(rejected.ReasonCode, Is.EqualTo("CriterionOutcomeNotInQuestion"));
            Assert.That(session.Snapshot.WorldRevision, Is.EqualTo(before.WorldRevision));
            Assert.That(session.Snapshot.ReviewHistory.Count, Is.EqualTo(before.ReviewHistory.Count)); Assert.That(session.Snapshot.CurrentApproval, Is.Null);
            var release = session.Submit(session.Stage(new RequestRelease(2, 1, rejected.RequestId), "publication"));
            Assert.That(release.ReasonCode, Is.EqualTo("CriterionOutcomeNotInQuestion")); Assert.That(session.Snapshot.WorldRevision, Is.EqualTo(before.WorldRevision));
            Commit(new EditRubric(1, new[] { new Criterion("first-judgement", "Q1", 10, "Evaluate source one.", "O2"), new Criterion("second-judgement", "Q2", 10, "Evaluate source two.", "O2") }));
            request = Schedule(); var approved = Script(new DecideReview(request, 2, 2, ReviewDecision.Approved, "Exact reconciled scripted pair."));
            Assert.That(approved.Status, Is.EqualTo(ReceiptStatus.Accepted)); Commit(new RequestRelease(2, 2, approved.RequestId), "publication");
            Assert.That(session.Snapshot.Publication, Is.EqualTo(PublicationStatus.Released));
        }

        [Test] public void ReturnedReasonSurvivesLaterEditsAndRereviewInImmutableHistory()
        {
            const string originalReason = "The task asks for listing; explain how the selected work can demonstrate a comparison.";
            var request = Schedule(); var returned = Script(new DecideReview(request, 1, 1, ReviewDecision.Returned, originalReason));
            Assert.That(returned.Status, Is.EqualTo(ReceiptStatus.Accepted));
            var snapshot = session.Snapshot; var original = snapshot.ReviewHistory.Single(r => r.DecisionReceiptId == returned.RequestId);
            Assert.That(original.DecisionReason, Is.EqualTo(originalReason));
            Commit(new EditRubric(1, new[] { new Criterion("first", "Q1", 10, "Explain each source.", "O1"), new Criterion("second", "Q2", 10, "Compare evidence.", "O2") }));
            var nextRequest = Schedule(); var later = Script(new DecideReview(nextRequest, 1, 2, ReviewDecision.Returned, "The task still needs a decision context."));
            Assert.That(later.Status, Is.EqualTo(ReceiptStatus.Accepted)); session.EnterReplay();
            Assert.That(session.Snapshot.ReviewHistory.Single(r => r.DecisionReceiptId == returned.RequestId).DecisionReason, Is.EqualTo(originalReason));
            Assert.That(original.DecisionReason, Is.EqualTo(originalReason));
            Assert.That(snapshot.ReviewHistory.Count, Is.EqualTo(3));
        }
    }
}
