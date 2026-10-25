using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Altaius.Demo02.Domain;

namespace Altaius.Demo02.Tests
{
    public class DomainInvariantTests
    {
        private OfflineSession s;
        private ScenarioController controller;
        [SetUp] public void Setup() { s = OfflineSession.Create(SessionMode.Coached, out controller); }
        private CommandReceipt Commit(DomainCommand c)
        { var r = s.Submit(s.Stage(c, "experiment.annotate")); Assert.That(r.Status, Is.EqualTo(ReceiptStatus.Accepted), r.ReasonCode); return r; }
        private PassageRef Inspect(string id, InspectionLocation location) => s.Inspect(id, location).Reference;
        private PassageRef[] Sources() => new[] { Inspect("S_A", InspectionLocation.Archive), Inspect("S_B", InspectionLocation.Archive) };
        private Criterion[] Rubric(int q1 = 4, int q2 = 16) => new[] {
            new Criterion("nature", "Q1", q1, "Explain each source's nature and use.", "O1"),
            new Criterion("decision", "Q2", q2, "Contrast evidence for this decision and justify with a limitation.", "O2") };
        private void RepairTask()
        { Commit(new EditTask(s.Snapshot.Task.Version, new[] { new Question("Q1", "Explain each source.", 4, "O1"), new Question("Q2", "Select a source for the steward trial, contrasting its evidence and limitations.", 16, "O2") }, Sources(), "Should we trial a morning steward?")); }
        private void RepairPair() { RepairTask(); Commit(new EditRubric(s.Snapshot.Rubric.Version, Rubric())); }
        private CommandReceipt Scripted(DomainCommand command, ActorRole role = ActorRole.ModuleLead)
        {
            var e = s.Stage(command, "test-op");
            var permit = controller.PermitExactScriptedResponse(e, role, "Exact authored local scenario condition, no semantic grade.");
            return controller.SubmitScripted(e, permit);
        }
        private CommandReceipt Synthetic(DomainCommand command, ActorRole role = ActorRole.GroundedEvaluator)
        { var r = controller.SubmitSyntheticEvaluation(s.Stage(command, "test-op"), role); Assert.That(r.Status, Is.EqualTo(ReceiptStatus.Accepted), r.ReasonCode); return r; }
        private Guid Request(string slot = "13:40")
        { return Commit(new RequestReview(s.Snapshot.Task.Version, s.Snapshot.Rubric.Version, "My exact current pair needs review.", slot)).RequestId; }
        private Guid Approve()
        {
            var request = Request();
            Assert.That(Scripted(new ScheduleReply(request, 2, 2, "13:40", ScheduleDecision.Accepted, Guid.NewGuid())).Status, Is.EqualTo(ReceiptStatus.Accepted));
            var result = Scripted(new DecideReview(request, 2, 2, ReviewDecision.Approved, "Authored local response to the exact scripted request."));
            Assert.That(result.Status, Is.EqualTo(ReceiptStatus.Accepted), result.ReasonCode); return result.RequestId;
        }
        private Guid Mark(string criterion = "naming", int marks = 10, string rationale = "This work names sources.")
        {
            var work = Inspect("W_LIST", InspectionLocation.ExperimentTable);
            Inspect("D02.rubric.v" + s.Snapshot.Rubric.Version + "." + criterion, InspectionLocation.Studio);
            return Commit(new Annotate("W_LIST", 1, criterion, new[] { work }, marks, rationale, s.Snapshot.Task.Version, s.Snapshot.Rubric.Version)).RequestId;
        }
        private void RejectedWithoutMutation(DomainCommand command)
        {
            var before = s.Snapshot; var r = s.Submit(s.Stage(command, "test-op"));
            Assert.That(r.Status, Is.EqualTo(ReceiptStatus.Rejected), r.ReasonCode);
            Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(before.WorldRevision));
            Assert.That(s.Snapshot.Pair.TaskDigest, Is.EqualTo(before.Pair.TaskDigest));
            Assert.That(s.Snapshot.Pair.RubricDigest, Is.EqualTo(before.Pair.RubricDigest));
        }

        [Test] public void BaselineFullDraftMarksAreHypothesesAndNotApprovalOrCompetence()
        {
            Mark(); Mark("listing", 10, "This response lists three features, without a comparison.");
            Assert.That(s.Snapshot.DraftTotal("W_LIST"), Is.EqualTo(20));
            Assert.That(s.Snapshot.CurrentEvaluations, Is.Empty); Assert.That(s.Snapshot.CurrentApproval, Is.Null);
            Assert.That(s.Snapshot.Evidence.All(e => !e.CompetenceClaim), Is.True);
        }
        [Test] public void PerQuestionMismatchRejectsEvenWhenGrandTotalIsTwenty()
        { RepairTask(); RejectedWithoutMutation(new EditRubric(1, Rubric(6, 14))); Assert.That(s.Snapshot.Rubric.Version, Is.EqualTo(1)); }
        [TestCase("duplicate")][TestCase("missing")][TestCase("negative")][TestCase("overflow")]
        public void TaskReplacementRejectsMalformedArithmeticAndQuestionSets(string kind)
        {
            var refs = Sources(); var questions = new[] { new Question("Q1", "Explain", 4, "O1"), new Question("Q2", "Judge", 16, "O2") };
            if (kind == "duplicate") questions[1] = new Question("Q1", "Other", 16, "O2");
            if (kind == "missing") questions = new[] { new Question("Q1", "Explain", 20, "O1") };
            if (kind == "negative") questions[0] = new Question("Q1", "Explain", -4, "O1");
            if (kind == "overflow") questions = new[] { new Question("Q1", "Explain", int.MaxValue, "O1"), new Question("Q2", "Judge", int.MaxValue, "O2") };
            RejectedWithoutMutation(new EditTask(1, questions, refs, "Trial decision"));
        }
        [Test] public void FullEditRequiresBothActuallyInspectedSources()
        {
            var a = Inspect("S_A", InspectionLocation.Archive);
            RejectedWithoutMutation(new EditTask(1, new[] { new Question("Q1", "Explain", 10, "O1"), new Question("Q2", "Judge", 10, "O2") }, new[] { a }, "Decision"));
        }
        [Test] public void DuplicateCommandReturnsOriginalButChangedPayloadConflicts()
        {
            RepairPair(); var command = new RequestReview(2, 2, "First summary", "13:40"); var envelope = s.Stage(command, "test-op");
            var receipt = s.Submit(envelope); Assert.That(receipt.Status, Is.EqualTo(ReceiptStatus.Accepted));
            var revision = s.Snapshot.WorldRevision;
            Assert.That(s.Submit(envelope), Is.SameAs(receipt)); Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(revision));
            var changed = new CommandEnvelope(envelope.CommandId, envelope.AttemptId, envelope.ExpectedWorldRevision,
                new RequestReview(2, 2, "Changed summary", "13:40"), envelope.Readset.ToDictionary(x => x.Key, x => x.Value), opportunityId: "test-op");
            Assert.That(s.Submit(changed).ReasonCode, Is.EqualTo("CommandIdConflict")); Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(revision));
        }
        [Test] public void MissingOrStaleReadsetRejectsAtomically()
        {
            RepairPair(); var envelope = s.Stage(new EditRubric(2, Rubric()), "test-op"); var before = s.Snapshot.WorldRevision;
            var readset = envelope.Readset.ToDictionary(x => x.Key, x => x.Value); readset.Remove("task_version");
            var missing = new CommandEnvelope(Guid.NewGuid(), envelope.AttemptId, envelope.ExpectedWorldRevision, envelope.Command, readset, opportunityId: "test-op");
            Assert.That(s.Submit(missing).ReasonCode, Is.EqualTo("ReadsetMismatch")); Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(before));
            Commit(new EditRubric(2, Rubric())); var after = s.Snapshot.WorldRevision;
            Assert.That(s.Submit(envelope).ReasonCode, Is.EqualTo("StaleWorldRevision")); Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(after));
        }
        [Test] public void LearnerCannotImpersonateModuleLeadOrDeliveryGateway()
        {
            RepairPair(); var id = Request();
            RejectedWithoutMutation(new ScheduleReply(id, 2, 2, "13:40", ScheduleDecision.Accepted, Guid.NewGuid()));
            RejectedWithoutMutation(new RecordCommunication(Guid.NewGuid(), ActorRole.ModuleLead, "Already approved", Guid.NewGuid()));
        }
        [Test] public void PortableWorkspaceDoesNotDiscloseRemoteSource()
        {
            Assert.Throws<InvalidOperationException>(() => s.Inspect("S_A", InspectionLocation.PortableWorkspace));
            Assert.That(s.Snapshot.Notebook, Is.Empty);
            var a = Inspect("S_A", InspectionLocation.Archive);
            Assert.That(s.Inspect("S_A", InspectionLocation.PortableWorkspace).Reference.Sha256, Is.EqualTo(a.Sha256));
            Assert.That(s.Snapshot.Notebook.Count, Is.EqualTo(1));
        }
        [Test] public void RevokedOrWrongHashPassageCannotBeLinked()
        {
            Mark(); var w = s.Snapshot.Notebook.Single(p => p.Reference.SourceId == "W_LIST").Reference;
            var wrong = new PassageRef(w.SourceId, w.Version, new string('0', 64), w.PassageId);
            RejectedWithoutMutation(new Annotate("W_LIST", 1, "naming", new[] { wrong }, 10, "Names", 1, 1));
            Assert.That(controller.Revoke(s.Snapshot.AttemptId, w), Is.True);
            RejectedWithoutMutation(new Annotate("W_LIST", 1, "naming", new[] { w }, 10, "Names", 1, 1));
        }
        [Test] public void UninspectedWorkRefCannotBeUsedEvenWithCorrectDigest()
        {
            Inspect("D02.rubric.v1.naming", InspectionLocation.Studio);
            var w = new PassageRef("W_LIST", 1, "451ead07d1527259518a5cf9b848e0356478d956e873dc72373a2a102830dd24", "W_LIST.p1");
            RejectedWithoutMutation(new Annotate("W_LIST", 1, "naming", new[] { w }, 10, "Names", 1, 1));
        }
        [TestCase(-1, "Reason")][TestCase(11, "Reason")][TestCase(10, " ")]
        public void AnnotationRejectsInvalidMarkOrEmptyOwnRationale(int marks, string rationale)
        {
            Mark(); var w = Inspect("W_LIST", InspectionLocation.ExperimentTable);
            RejectedWithoutMutation(new Annotate("W_LIST", 1, "naming", new[] { w }, marks, rationale, 1, 1));
        }
        [Test] public void AnnotationRevisionPreservesHistoryAndEditsMakeItHistorical()
        {
            Mark(); Mark("naming", 5, "I revised my interpretation.");
            Assert.That(s.Snapshot.AnnotationHistory.Count, Is.EqualTo(2)); Assert.That(s.Snapshot.DraftTotal("W_LIST"), Is.EqualTo(5));
            Commit(new EditRubric(1, Rubric(10, 10)));
            Assert.That(s.Snapshot.CurrentAnnotations, Is.Empty); Assert.That(s.Snapshot.AnnotationHistory[0].Content.Marks, Is.EqualTo(10));
        }
        [Test] public void OfflineArbitraryProseCannotReceiveEvaluatorAuthority()
        {
            var id = Mark(); var c = new Evaluate(s.Snapshot.ExperimentRevision, "W_LIST", FindingDecision.Supported,
                new[] { new CriterionFinding(id, "naming", FindingDecision.Supported, s.Snapshot.Notebook.Select(p => p.Reference), "Matched a keyword") }, FindingCoverage.Complete, new[] { id });
            var r = controller.SubmitSyntheticEvaluation(s.Stage(c, "experiment.annotate"), ActorRole.ExperimentReviewer);
            Assert.That(r.Status, Is.EqualTo(ReceiptStatus.Unknown)); Assert.That(r.ReasonCode, Is.EqualTo("EvaluatorUnavailable"));
            Assert.That(s.Snapshot.CurrentEvaluations, Is.Empty);
        }
        [Test] public void PartialFindingsRemainPartialAndCompleteRequiresEverySubmittedAnnotation()
        {
            s = OfflineSession.Create(SessionMode.Presenter, out controller); var first = Mark(); var second = Mark("listing");
            var finding = new CriterionFinding(first, "naming", FindingDecision.Ambiguous, new[] { Inspect("W_LIST", InspectionLocation.ExperimentTable) }, "Authored synthetic ambiguity.");
            var partial = new Evaluate(s.Snapshot.ExperimentRevision, "W_LIST", FindingDecision.Ambiguous, new[] { finding }, FindingCoverage.Partial, new[] { first, second });
            Synthetic(partial, ActorRole.ExperimentReviewer);
            Assert.That(s.Snapshot.CurrentEvaluations.Single().Content.Coverage, Is.EqualTo(FindingCoverage.Partial));
            var complete = new Evaluate(s.Snapshot.ExperimentRevision, "W_LIST", FindingDecision.Supported, new[] { finding }, FindingCoverage.Complete, new[] { first, second });
            var before = s.Snapshot.WorldRevision; var r = controller.SubmitSyntheticEvaluation(s.Stage(complete, "test-op"), ActorRole.ExperimentReviewer);
            Assert.That(r.Status, Is.EqualTo(ReceiptStatus.Rejected)); Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(before));
        }
        [Test] public void DuplicateAndUnknownFindingsRejectInsteadOfEndorsingTotal()
        {
            s = OfflineSession.Create(SessionMode.Presenter, out controller); var id = Mark();
            var f = new CriterionFinding(id, "naming", FindingDecision.Supported, new[] { Inspect("W_LIST", InspectionLocation.ExperimentTable) }, "Synthetic");
            var duplicate = new Evaluate(s.Snapshot.ExperimentRevision, "W_LIST", FindingDecision.Supported, new[] { f, f }, FindingCoverage.Complete, new[] { id });
            Assert.That(controller.SubmitSyntheticEvaluation(s.Stage(duplicate, "test-op"), ActorRole.ExperimentReviewer).Status, Is.EqualTo(ReceiptStatus.Rejected));
            var unknown = new Evaluate(s.Snapshot.ExperimentRevision, "W_LIST", FindingDecision.Supported, new[] { new CriterionFinding(Guid.NewGuid(), "other", FindingDecision.Supported, f.Sources, "Synthetic") }, FindingCoverage.Complete, new[] { id });
            Assert.That(controller.SubmitSyntheticEvaluation(s.Stage(unknown, "test-op"), ActorRole.ExperimentReviewer).Status, Is.EqualTo(ReceiptStatus.Rejected));
        }
        [Test] public void ExactApprovalCannotReleaseAfterRubricEdit()
        {
            RepairPair(); var approval = Approve(); Assert.That(s.Snapshot.CurrentApproval, Is.Not.Null);
            Commit(new EditRubric(2, Rubric()));
            Assert.That(s.Snapshot.ReviewStatus, Is.EqualTo(ReviewStatus.Stale)); Assert.That(s.Snapshot.CurrentApproval, Is.Null);
            RejectedWithoutMutation(new RequestRelease(2, 3, approval)); Assert.That(s.Snapshot.ApprovalHistory.Count, Is.EqualTo(1));
        }
        [Test] public void AcceptedExactScriptedReviewCanReleaseButDoesNotClaimSemanticCompetence()
        {
            RepairPair(); var approval = Approve(); Commit(new RequestRelease(2, 2, approval));
            Assert.That(s.Snapshot.Publication, Is.EqualTo(PublicationStatus.Released));
            Assert.That(s.Snapshot.CurrentApproval.AuthorityDisclosure, Does.Contain("scripted"));
            Assert.That(s.Snapshot.Evidence.All(e => !e.CompetenceClaim), Is.True);
        }
        [Test] public void ApprovalNeedsSchedulingAndExperimentReviewerCannotApprove()
        {
            RepairPair(); var request = Request();
            Assert.That(Scripted(new DecideReview(request, 2, 2, ReviewDecision.Approved, "Not scheduled")).Status, Is.EqualTo(ReceiptStatus.Rejected));
            Assert.That(Scripted(new DecideReview(request, 2, 2, ReviewDecision.Approved, "Wrong role"), ActorRole.ExperimentReviewer).Status, Is.EqualTo(ReceiptStatus.Rejected));
        }
        [Test] public void ReviewProposalAndRefusalNeverAdvanceClockButAcceptanceDoes()
        {
            RepairPair(); var first = Request(); Assert.That(s.Snapshot.ClockMinute, Is.EqualTo(800));
            Assert.That(Scripted(new ScheduleReply(first, 2, 2, "13:40", ScheduleDecision.Unavailable, Guid.NewGuid())).Status, Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(s.Snapshot.ClockMinute, Is.EqualTo(800)); Assert.That(s.Snapshot.ReviewStatus, Is.EqualTo(ReviewStatus.Unavailable));
            var second = Request(); Assert.That(Scripted(new ScheduleReply(second, 2, 2, "13:40", ScheduleDecision.Accepted, Guid.NewGuid())).Status, Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(s.Snapshot.ClockMinute, Is.EqualTo(820)); Assert.That(s.Snapshot.CurrentApproval, Is.Null);
        }
        [TestCase("13:39")][TestCase("14:01")][TestCase("1:40")][TestCase("24:00")]
        public void ReviewSlotsMustBeExactAndWithinDeadline(string slot)
        { RepairPair(); RejectedWithoutMutation(new RequestReview(2, 2, "Review", slot)); }
        [Test] public void StaleReplyCannotMutateClockOrCurrentAuthority()
        {
            RepairPair(); var id = Request(); var e = s.Stage(new ScheduleReply(id, 2, 2, "13:40", ScheduleDecision.Accepted, Guid.NewGuid()), "test-op");
            var permit = controller.PermitExactScriptedResponse(e, ActorRole.ModuleLead, "Exact script");
            Commit(new EditRubric(2, Rubric())); var before = s.Snapshot;
            Assert.That(controller.SubmitScripted(e, permit).Status, Is.EqualTo(ReceiptStatus.Rejected));
            Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(before.WorldRevision)); Assert.That(s.Snapshot.ClockMinute, Is.EqualTo(800));
        }
        [Test] public void HoldProposalRequiresActualExactOwnerAcceptance()
        {
            var anchor = Inspect("D02.task.v1.Q2", InspectionLocation.Studio);
            var proposal = Commit(new RequestHold("Q2 rewards listing instead of a justified comparison.", ActorRole.Colleague, "14:30", "Rework criteria with the lead.", HoldDefectBasis.ReviewedInterpretation, new[] { anchor }));
            Assert.That(s.Snapshot.Publication, Is.EqualTo(PublicationStatus.HoldRequested));
            var accept = new AcceptHold(proposal.RequestId, 1, 1, "14:30", true, false, true);
            RejectedWithoutMutation(accept);
            Assert.That(Scripted(accept, ActorRole.ModuleLead).Status, Is.EqualTo(ReceiptStatus.Rejected));
            Assert.That(Scripted(accept, ActorRole.Colleague).Status, Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(s.Snapshot.Publication, Is.EqualTo(PublicationStatus.Held)); Assert.That(s.Snapshot.HoldHistory.Last().SelfOwned, Is.False);
        }
        [Test] public void SelfOwnedHoldNeedsExplicitConfirmedCommitment()
        {
            var id = Commit(new RequestHold("Source evidence is not supplied with Q2.", ActorRole.Learner, "14:30", "Attach both sources and request review.")).RequestId;
            RejectedWithoutMutation(new AcceptHold(id, 1, 1, "14:30", true));
            Commit(new AcceptHold(id, 1, 1, "14:30", true, true)); Assert.That(s.Snapshot.HoldHistory.Last().SelfOwned, Is.True);
        }
        [TestCase("", "14:30", "Revise")][TestCase("Specific missing comparison", "13:20", "Revise")][TestCase("Specific missing comparison", "24:00", "Revise")][TestCase("Specific missing comparison", "14:30", "")]
        public void IncompleteOrNonFutureHoldRejects(string defect, string time, string action)
        { RejectedWithoutMutation(new RequestHold(defect, ActorRole.Learner, time, action)); }
        [Test] public void ResetTombstonesLateCallbackAndLinkedRetryPreservesOriginalHistory()
        {
            RepairPair(); var id = Request(); var e = s.Stage(new ScheduleReply(id, 2, 2, "13:40", ScheduleDecision.Accepted, Guid.NewGuid()), "test-op");
            var permit = controller.PermitExactScriptedResponse(e, ActorRole.ModuleLead, "Exact script"); var old = s.Snapshot.AttemptId;
            s.Reset(true); Assert.That(s.Snapshot.LinkedFromAttemptId, Is.EqualTo(old)); Assert.That(s.Snapshot.LinkedHistory.Count, Is.EqualTo(1));
            var before = s.Snapshot.WorldRevision; Assert.That(controller.SubmitScripted(e, permit).ReasonCode, Is.EqualTo("AttemptTombstoned"));
            Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(before)); s.Reset(false);
            Assert.That(s.Snapshot.LinkedHistory, Is.Empty); Assert.That(s.Snapshot.LinkedFromAttemptId, Is.Null); Assert.That(s.Snapshot.Notebook, Is.Empty);
        }
        [Test] public void ReplayRejectsAllWritesAndPresenterProducesNoLearnerEvidence()
        {
            Mark(); var count = s.Snapshot.Evidence.Count; s.EnterReplay(); var before = s.Snapshot.WorldRevision;
            Assert.That(s.Submit(s.Stage(new EditRubric(1, Rubric(10, 10)), "test-op")).ReasonCode, Is.EqualTo("ReplayReadOnly"));
            Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(before)); Assert.That(s.Snapshot.Evidence.Count, Is.EqualTo(count));
            Assert.Throws<InvalidOperationException>(() => s.Inspect("S_A", InspectionLocation.Archive));
            s = OfflineSession.Create(SessionMode.Presenter, out controller); Mark();
            Assert.That(s.Snapshot.Evidence, Is.Empty); Assert.That(s.Snapshot.HasLearnerEvidence, Is.False);
        }
        [Test] public void DeliveredStrategicHelpIsAttributedAndUnknownHistoryNeverMeansNoHelp()
        {
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "experiment.annotate", "What does this marked work demonstrate?", true, true), Is.True);
            Mark(); Assert.That(s.Snapshot.Evidence.Last().Assisted, Is.True);
            controller.MarkAssistanceHistoryUnavailable(s.Snapshot.AttemptId); Mark("listing"); Assert.That(s.Snapshot.Evidence.Last().AssistanceKnown, Is.False);
            s.Reset(true); Assert.That(s.Snapshot.Assistance.Count, Is.EqualTo(1)); Assert.That(s.Snapshot.AssistanceHistory, Is.EqualTo(AssistanceHistory.Unavailable));
        }
        [Test] public void ResetRejectsLateAssistanceAndSourcePermissionCallbacks()
        {
            var oldAttempt = s.Snapshot.AttemptId; var oldSource = Inspect("S_A", InspectionLocation.Archive);
            s.Reset(false); var revision = s.Snapshot.WorldRevision;
            Assert.That(controller.DeliverAssistance(oldAttempt, Guid.NewGuid(), "mark", "Late help", true, true), Is.False);
            Assert.That(controller.Revoke(oldAttempt, oldSource), Is.False);
            controller.MarkAssistanceHistoryUnavailable(oldAttempt);
            Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(revision)); Assert.That(s.Snapshot.Assistance, Is.Empty);
            Assert.That(s.Snapshot.AssistanceHistory, Is.EqualTo(AssistanceHistory.Complete));
        }

        [Test] public void IndependentModeDoesNotDeliverUnrequestedStrategicHelp()
        {
            s = OfflineSession.Create(SessionMode.Independent, out controller);
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "mark", "Focus on judgement", true, false), Is.False);
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "mark", "Focus on judgement", true, true), Is.True);
        }

        [Test] public void ArbitrarySelfHoldCommitsOwnershipButDoesNotVerifySubstantiveDefect()
        {
            var id = Commit(new RequestHold("Something seems wrong to me.", ActorRole.Learner, "14:30", "Investigate the exact mismatch.")).RequestId;
            Commit(new AcceptHold(id, 1, 1, "14:30", true, true));
            Assert.That(s.Snapshot.Publication, Is.EqualTo(PublicationStatus.HoldCommittedUnverified));
            Assert.That(s.Snapshot.HoldHistory.Last().SelfOwned, Is.True);
            Assert.That(s.Snapshot.HoldHistory.Last().DefectVerified, Is.False);
        }
        [Test] public void StructuralHoldBasisNeedsCurrentValidAnchorsAndActualDefect()
        {
            var anchor = Inspect("D02.task.v1.Q2", InspectionLocation.Studio);
            var id = Commit(new RequestHold("No source extracts are attached.", ActorRole.Learner, "14:30", "Attach sources.", HoldDefectBasis.MissingSourceAttachments, new[] { anchor })).RequestId;
            Commit(new AcceptHold(id, 1, 1, "14:30", true, true));
            Assert.That(s.Snapshot.Publication, Is.EqualTo(PublicationStatus.Held)); Assert.That(s.Snapshot.HoldHistory.Last().DefectVerified, Is.True);
            RepairPair();
            var invalid = new RequestHold("No sources", ActorRole.Learner, "14:30", "Attach", HoldDefectBasis.MissingSourceAttachments, new[] { anchor });
            RejectedWithoutMutation(invalid);
            var current = Inspect("D02.task.v2.Q2", InspectionLocation.Studio);
            RejectedWithoutMutation(new RequestHold("No sources", ActorRole.Learner, "14:30", "Attach", HoldDefectBasis.MissingSourceAttachments, new[] { current }));
            RejectedWithoutMutation(new RequestHold("No sources", ActorRole.Learner, "14:30", "Attach", HoldDefectBasis.MissingSourceAttachments));
        }
        [Test] public void RevocationRemovesCurrentAnnotationInterpretationButKeepsHistory()
        {
            Mark(); var work = Inspect("W_LIST", InspectionLocation.ExperimentTable); controller.Revoke(s.Snapshot.AttemptId, work);
            Assert.That(s.Snapshot.CurrentAnnotations, Is.Empty); Assert.That(s.Snapshot.AnnotationHistory.Count, Is.EqualTo(1));
        }
        [Test] public void LinkedPracticeCannotClaimIndependentEvidenceEvenWithEmptyHelpLedger()
        { s.Reset(true); Mark(); Assert.That(s.Snapshot.Evidence.Single().Assisted, Is.True); }
        [Test] public void SyntheticEvaluatorRouteCannotMintModuleLeadAuthority()
        {
            s = OfflineSession.Create(SessionMode.Presenter, out controller); RepairPair(); var request = Request();
            var e = s.Stage(new ScheduleReply(request, 2, 2, "13:40", ScheduleDecision.Accepted, Guid.NewGuid()), "test-op");
            Assert.That(controller.SubmitSyntheticEvaluation(e, ActorRole.ModuleLead).Status, Is.EqualTo(ReceiptStatus.Rejected));
        }
        [Test] public void MissingExplicitNullResourceReadsetIsNotEquivalentToAbsence()
        {
            var envelope = s.Stage(new RequestReview(1, 1, "Please review.", "13:40"), "test-op");
            var readset = envelope.Readset.ToDictionary(x => x.Key, x => x.Value);
            var absent = readset.Single(x => x.Key.StartsWith("requests/")); Assert.That(absent.Value, Is.Null); readset.Remove(absent.Key);
            var malformed = new CommandEnvelope(envelope.CommandId, envelope.AttemptId, envelope.ExpectedWorldRevision, envelope.Command, readset, opportunityId: "test-op");
            Assert.That(s.Submit(malformed).ReasonCode, Is.EqualTo("ReadsetMismatch")); Assert.That(s.Snapshot.ReviewHistory, Is.Empty);
        }
        [Test] public void CoachedUnsolicitedHelpHonoursCooldownBusyInputAndTwoInterventionCap()
        {
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "mark", "Question one", true, false, false, true), Is.False);
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "mark", "Question one", true, false), Is.True);
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "mark", "Question two", true, false), Is.False);
            for (var i = 0; i < 4; i++) Mark("naming", i);
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "mark", "Question two", true, false), Is.True);
            for (var i = 0; i < 4; i++) Mark("naming", i);
            Assert.That(controller.DeliverAssistance(s.Snapshot.AttemptId, Guid.NewGuid(), "mark", "Question three", true, false), Is.False);
        }

        [Test] public void DistinctRiskyClausesInOneDeliveryAreSeparateIncidents()
        {
            s = OfflineSession.Create(SessionMode.Presenter, out controller); var sources = Sources();
            const string first = "I promise release."; const string second = "I guarantee the app works.";
            var communication = Communicate(first + " " + second); var adjudication = Guid.NewGuid();
            Synthetic(new AdjudicateAct(communication, ActFinding.SupportedCommitment,
                new[] { new ClauseRef(0, first.Length, first), new ClauseRef(first.Length + 1, second.Length, second) }, LearnerCatalogue.RuleVersion, sources, adjudication));
            var utterance = s.Snapshot.Communications.Single().Content.UtteranceId;
            Synthetic(new AssertRisk(utterance, first, communication, adjudication));
            Synthetic(new AssertRisk(utterance, second, communication, adjudication));
            Assert.That(s.Snapshot.ActiveRisks.Count, Is.EqualTo(2));
        }

        private Guid Communicate(string text)
        { var result = Scripted(new RecordCommunication(Guid.NewGuid(), ActorRole.ModuleLead, text, Guid.NewGuid()), ActorRole.RecipientChannel); Assert.That(result.Status, Is.EqualTo(ReceiptStatus.Accepted)); return result.RequestId; }
        private Guid Adjudicate(Guid communication, ActFinding finding, string text, Guid? incident = null)
        {
            var id = Guid.NewGuid(); Synthetic(new AdjudicateAct(communication, finding, new[] { new ClauseRef(0, text.Length, text) }, LearnerCatalogue.RuleVersion, Sources(), id, incident)); return id;
        }
        private Guid Risk(string text)
        {
            var communication = Communicate(text); var adjudication = Adjudicate(communication, ActFinding.SupportedCommitment, text);
            var utterance = s.Snapshot.Communications.Single(c => c.Id == communication).Content.UtteranceId;
            return Synthetic(new AssertRisk(utterance, text, communication, adjudication)).RequestId;
        }
        [Test] public void RecurringRiskSurvivesCreditDedupAndCorrectionOnlyClosesNamedIncident()
        {
            s = OfflineSession.Create(SessionMode.Presenter, out controller); Sources();
            var first = Risk("I promise publication without current approval."); var second = Risk("I promise publication without current approval.");
            var withdrawal = "I withdraw the first publication commitment and will request exact review.";
            var communication = Communicate(withdrawal); var adjudication = Adjudicate(communication, ActFinding.SupportedCorrection, withdrawal, first);
            Synthetic(new CorrectRisk(first, withdrawal, Sources(), communication, adjudication));
            Assert.That(s.Snapshot.ActiveRisks.Select(r => r.Id), Is.EqualTo(new[] { second }));
            Assert.That(s.Snapshot.RiskHistory.Count, Is.EqualTo(3));
            var third = Risk("I promise publication without current approval."); Assert.That(s.Snapshot.ActiveRisks.Count, Is.EqualTo(2));
            Assert.That(third, Is.Not.EqualTo(first));
        }
        [Test] public void RiskRejectsWrongCommunicationClauseAndCorrectionIncident()
        {
            s = OfflineSession.Create(SessionMode.Presenter, out controller); Sources();
            var first = Risk("I promise release."); var second = Risk("I promise release again.");
            var text = "I withdraw the first promise."; var communication = Communicate(text);
            var adjudication = Adjudicate(communication, ActFinding.SupportedCorrection, text, first);
            var bad = new CorrectRisk(second, text, Sources(), communication, adjudication); var before = s.Snapshot.WorldRevision;
            Assert.That(controller.SubmitSyntheticEvaluation(s.Stage(bad, "test-op"), ActorRole.GroundedEvaluator).Status, Is.EqualTo(ReceiptStatus.Rejected));
            Assert.That(s.Snapshot.ActiveRisks.Count, Is.EqualTo(2)); Assert.That(s.Snapshot.WorldRevision, Is.EqualTo(before));
            var malformed = new AdjudicateAct(communication, ActFinding.SupportedCommitment, new[] { new ClauseRef(0, 4, "fake") }, LearnerCatalogue.RuleVersion, Sources(), Guid.NewGuid());
            Assert.That(controller.SubmitSyntheticEvaluation(s.Stage(malformed, "test-op"), ActorRole.GroundedEvaluator).Status, Is.EqualTo(ReceiptStatus.Rejected));
        }
        [Test] public void NonassertiveFindingDoesNotManufactureRisk()
        {
            s = OfflineSession.Create(SessionMode.Presenter, out controller); Sources(); var text = "I have not received approval.";
            var communication = Communicate(text); var adjudication = Adjudicate(communication, ActFinding.Nonassertive, text);
            var utterance = s.Snapshot.Communications.Last().Content.UtteranceId;
            var result = controller.SubmitSyntheticEvaluation(s.Stage(new AssertRisk(utterance, text, communication, adjudication), "test-op"), ActorRole.GroundedEvaluator);
            Assert.That(result.Status, Is.EqualTo(ReceiptStatus.Rejected)); Assert.That(s.Snapshot.ActiveRisks, Is.Empty);
        }
    }
}
