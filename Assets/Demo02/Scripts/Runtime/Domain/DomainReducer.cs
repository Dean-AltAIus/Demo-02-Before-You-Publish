using System;
using System.Globalization;
using System.Linq;

namespace Altaius.Demo02.Domain
{
    public sealed partial class OfflineSession
    {
        // Every branch validates and prepares immutable values before returning its atomic commit.
        private Action PrepareMutation(CommandEnvelope envelope, ActorRole actor, string disclosure)
        {
            var command = envelope.Command;
            var id = envelope.CommandId;
            if (command is Annotate a)
            {
                ExactPair(a.TaskVersion, a.RubricVersion);
                Require(SpecimenRevision(a.SpecimenId) == a.SpecimenVersion, "SpecimenVersionMismatch");
                var criterion = Rubric.Criteria.SingleOrDefault(x => x.Id == a.CriterionId);
                Require(criterion != null, "UnknownCriterion");
                Require(a.Marks >= 0 && a.Marks <= criterion.Marks && !string.IsNullOrWhiteSpace(a.Rationale), "InvalidAnnotation");
                Require(a.Passages.Count > 0 && a.Passages.All(p => p != null && p.SourceId == a.SpecimenId && p.Version == a.SpecimenVersion) &&
                    a.Passages.Select(p => p.Key).Distinct().Count() == a.Passages.Count, "InvalidWorkPassages");
                var criterionSource = "D02.rubric.v" + Rubric.Version + "." + criterion.Id;
                Require(registry.ContainsKey(criterionSource), "CriterionPassageMissing"); ValidatePassage(registry[criterionSource].Reference);
                var previous = annotations.LastOrDefault(x => x.Pair.Same(Pair) && x.Content.SpecimenId == a.SpecimenId && x.Content.CriterionId == a.CriterionId);
                var record = new AnnotationRecord(id, (previous?.Revision ?? 0) + 1, revisions["experiment_revision"] + 1, Pair, a);
                return () => { annotations.Add(record); revisions["experiment_revision"]++; };
            }
            if (command is Evaluate e)
            {
                Require(e.ExperimentRevision == revisions["experiment_revision"] && SpecimenRevision(e.SpecimenId) != null, "ExperimentVersionMismatch");
                Require(Enum.IsDefined(typeof(FindingDecision), e.Decision) && Enum.IsDefined(typeof(FindingCoverage), e.Coverage), "InvalidFindingDecision");
                Require(e.SubmittedAnnotationIds.Count > 0 && e.SubmittedAnnotationIds.All(x => x != Guid.Empty) && e.SubmittedAnnotationIds.Distinct().Count() == e.SubmittedAnnotationIds.Count, "InvalidSubmittedAnnotations");
                var current = Snapshot.CurrentAnnotations.Where(x => x.Content.SpecimenId == e.SpecimenId).ToDictionary(x => x.Id);
                Require(e.SubmittedAnnotationIds.All(current.ContainsKey), "AnnotationNotCurrent");
                Require(e.Findings.All(f => f != null) && e.Findings.Select(f => f.AnnotationId).Distinct().Count() == e.Findings.Count && e.Findings.Select(f => f.CriterionId).Distinct().Count() == e.Findings.Count, "DuplicateFindings");
                foreach (var finding in e.Findings)
                {
                    Require(e.SubmittedAnnotationIds.Contains(finding.AnnotationId) && current.ContainsKey(finding.AnnotationId) && current[finding.AnnotationId].Content.CriterionId == finding.CriterionId, "UnknownFindingScope");
                    Require(Enum.IsDefined(typeof(FindingDecision), finding.Decision) && !string.IsNullOrWhiteSpace(finding.Reason) && finding.Sources.Count > 0, "UngroundedFinding");
                    Require(finding.Sources.Any(p => p.SourceId == e.SpecimenId), "FindingNeedsExactWorkPassage");
                }
                Require(e.Coverage != FindingCoverage.Complete || e.Findings.Count == e.SubmittedAnnotationIds.Count, "IncompleteFindingCoverage");
                // No worked-edition matching or inferred oracle. Only the isolated presenter capability reaches this local branch.
                var record = new EvaluationRecord(id, Pair, e, true);
                return () => evaluations.Add(record);
            }
            if (command is EditTask t)
            {
                Require(t.BaseVersion == Task.Version, "TaskBaseVersionMismatch");
                Require(t.Questions.Count == 2 && t.Questions.All(q => q != null) && t.Questions.Select(q => q.Id).OrderBy(x => x).SequenceEqual(new[] { "Q1", "Q2" }), "FullQuestionReplacementRequired");
                Require(t.TotalMarks == 20 && t.Questions.All(q => !string.IsNullOrWhiteSpace(q.Text) && q.Marks >= 0 && ValidOutcomes(q.OutcomeIds)) && t.Questions.Sum(q => (long)q.Marks) == 20, "InvalidTaskArithmeticOrContent");
                Require(!string.IsNullOrWhiteSpace(t.DecisionContext) && t.Sources.Count == 2 && t.Sources.All(p => p != null) &&
                    t.Sources.Select(p => p.SourceId).OrderBy(x => x).SequenceEqual(new[] { "S_A", "S_B" }), "BothSourcesAndDecisionRequired");
                var next = new TaskDocument(Task.Version + 1, t.Questions, t.Sources, t.DecisionContext);
                var passages = next.Questions.Select(q => NewPassage("D02.task.v" + next.Version + "." + q.Id, next.Version, q.Text)).ToList();
                return () => { tasks.Add(next); foreach (var p in passages) registry.Add(p.Reference.SourceId, p); RegistryChanged(); InvalidateDependencies(); };
            }
            if (command is EditRubric r)
            {
                Require(r.BaseVersion == Rubric.Version, "RubricBaseVersionMismatch");
                Require(r.Allocations.Count > 0 && r.Allocations.All(c => c != null && !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Description) &&
                    (c.QuestionId == "Q1" || c.QuestionId == "Q2") && c.Marks >= 0 && ValidOutcomes(c.OutcomeIds)), "InvalidRubricContent");
                Require(r.Allocations.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() == r.Allocations.Count, "DuplicateCriterion");
                Require(r.Allocations.Sum(c => (long)c.Marks) == 20 && Task.Questions.All(q => r.Allocations.Where(c => c.QuestionId == q.Id).Sum(c => (long)c.Marks) == q.Marks), "PerQuestionMarksMismatch");
                Require(r.Allocations.All(c => c.OutcomeIds.All(o => Task.Questions.Single(q => q.Id == c.QuestionId).OutcomeIds.Contains(o))), "CriterionOutcomeNotInQuestion");
                var next = new RubricDocument(Rubric.Version + 1, r.Allocations);
                var passages = next.Criteria.Select(c => NewPassage("D02.rubric.v" + next.Version + "." + c.Id, next.Version, c.Description)).ToList();
                return () => { rubrics.Add(next); foreach (var p in passages) registry.Add(p.Reference.SourceId, p); RegistryChanged(); InvalidateDependencies(); };
            }
            if (command is RequestReview requestCommand)
            {
                ExactPair(requestCommand.TaskVersion, requestCommand.RubricVersion);
                Require(!string.IsNullOrWhiteSpace(requestCommand.ChangeSummary), "ChangeSummaryRequired");
                var slot = ParseMinute(requestCommand.ReviewBy); Require(slot >= Math.Max(minute, 820) && slot <= 840, "ReviewSlotOutsideWindow");
                var record = new ReviewRequestRecord(id, 1, Pair, slot, requestCommand.ChangeSummary, ReviewStatus.Pending);
                return () => { reviews.Add(record); currentReview = id; revisions["review_request_revision"]++; revisions["request_collection_revision"]++; reviewStatus = ReviewStatus.Pending; };
            }
            if (command is ScheduleReply reply)
            {
                ExactPair(reply.TaskVersion, reply.RubricVersion);
                var request = CurrentReview(reply.RequestId);
                Require(request.Status == ReviewStatus.Pending, "SchedulingRequestNotPending");
                var slot = ParseMinute(reply.ReviewBy);
                Require(reply.ReplyId != Guid.Empty && !consumedReplyIds.Contains(reply.ReplyId), "DuplicateSchedulingReply");
                Require(Enum.IsDefined(typeof(ScheduleDecision), reply.Decision) && slot == request.Minute && slot >= Math.Max(minute, 820) && slot <= 840, "SchedulingScopeMismatch");
                var status = reply.Decision == ScheduleDecision.Accepted ? ReviewStatus.Scheduled : reply.Decision == ScheduleDecision.Denied ? ReviewStatus.Denied : ReviewStatus.Unavailable;
                var record = new ReviewRequestRecord(request.Id, request.Revision + 1, request.Pair, request.Minute, request.ChangeSummary, status, null, disclosure);
                return () =>
                {
                    reviews.Add(record); consumedReplyIds.Add(reply.ReplyId); revisions["review_request_revision"]++; reviewStatus = status;
                    if (reply.Decision == ScheduleDecision.Accepted) { minute = slot; revisions["clock_revision"]++; }
                };
            }
            if (command is DecideReview decision)
            {
                ExactPair(decision.TaskVersion, decision.RubricVersion);
                var request = CurrentReview(decision.RequestId);
                Require(request.Status == ReviewStatus.Scheduled && request.Minute == minute, "CurrentScheduledReviewRequired");
                Require(Enum.IsDefined(typeof(ReviewDecision), decision.Decision) && !string.IsNullOrWhiteSpace(decision.Reason), "ReviewReasonRequired");
                if (decision.Decision == ReviewDecision.Approved) ValidateReleaseStructure();
                var status = decision.Decision == ReviewDecision.Approved ? ReviewStatus.Approved : ReviewStatus.Returned;
                var record = new ReviewRequestRecord(request.Id, request.Revision + 1, request.Pair, request.Minute, request.ChangeSummary, status, id, disclosure);
                var approved = decision.Decision == ReviewDecision.Approved ? new ApprovalRecord(id, request.Id, Pair, decision.Reason, disclosure) : null;
                return () =>
                {
                    reviews.Add(record); revisions["review_request_revision"]++; reviewStatus = status; approval = approved;
                    if (approved != null) { approvals.Add(approved); approvalRevision = (approvalRevision ?? 0) + 1; } else approvalRevision = null;
                };
            }
            if (command is RequestRelease release)
            {
                ExactPair(release.TaskVersion, release.RubricVersion); ValidateReleaseStructure();
                Require(approval != null && approval.Pair.Same(Pair) && approval.ReceiptId == release.ApprovalReceiptId && reviewStatus == ReviewStatus.Approved, "ExactCurrentApprovalRequired");
                Require(publication != PublicationStatus.Released, "AlreadyReleased");
                return () => publication = PublicationStatus.Released;
            }
            if (command is RequestHold holdCommand)
            {
                Require(!string.IsNullOrWhiteSpace(holdCommand.Defect) && !string.IsNullOrWhiteSpace(holdCommand.NextAction), "DefectAndActionRequired");
                Require(holdCommand.Owner == ActorRole.ModuleLead || holdCommand.Owner == ActorRole.Colleague || holdCommand.Owner == ActorRole.Learner, "InvalidHoldOwner");
                Require(ParseMinute(holdCommand.ReviewAt) > minute, "HoldMustBeFuture");
                ValidateHoldBasis(holdCommand, false);
                // The proposal records the learner's specific defect hypothesis. Actual owner acceptance is required separately.
                var record = new HoldRecord(id, 1, Pair, holdCommand, null, false);
                return () => { holds.Add(record); currentHold = id; revisions["hold_request_revision"]++; revisions["request_collection_revision"]++; publication = PublicationStatus.HoldRequested; };
            }
            if (command is AcceptHold accept)
            {
                ExactPair(accept.TaskVersion, accept.RubricVersion);
                var hold = holds.LastOrDefault(x => x.Id == accept.RequestId);
                Require(hold != null && currentHold == hold.Id && hold.Pair.Same(Pair) && hold.Accepted == null, "CurrentHoldRequestRequired");
                Require(actor == hold.Proposal.Owner, "ExactOwnerAcceptanceRequired");
                Require(accept.ReviewAt == hold.Proposal.ReviewAt && ParseMinute(accept.ReviewAt) > minute, "HoldTimeScopeMismatch");
                Require(actor != ActorRole.Learner || accept.ConfirmedSelfCommitment, "ConfirmedSelfCommitmentRequired");
                Require(actor == ActorRole.Learner || !accept.ConfirmedSelfCommitment, "NpcCannotUseSelfCommitment");
                Require(actor != ActorRole.Learner || !accept.ConfirmsSubstantiveDefect, "LearnerCannotSelfValidateDefectInterpretation");
                var defectVerified = ValidateHoldBasis(hold.Proposal, actor != ActorRole.Learner && accept.ConfirmsSubstantiveDefect);
                var record = new HoldRecord(hold.Id, hold.Revision + 1, hold.Pair, hold.Proposal, accept.Accepted, actor == ActorRole.Learner, disclosure, defectVerified);
                return () => { holds.Add(record); revisions["hold_request_revision"]++; publication = !accept.Accepted ? PublicationStatus.Draft : defectVerified ? PublicationStatus.Held : PublicationStatus.HoldCommittedUnverified; };
            }
            if (command is RecordCommunication communicationCommand)
            {
                Require(communicationCommand.UtteranceId != Guid.Empty && communicationCommand.DeliveryReceiptId != Guid.Empty && !string.IsNullOrWhiteSpace(communicationCommand.ExactText), "ActualDeliveryRequired");
                Require(communicationCommand.Recipient == ActorRole.ModuleLead || communicationCommand.Recipient == ActorRole.Colleague || communicationCommand.Recipient == ActorRole.ExperimentReviewer, "UnknownRecipient");
                Require(!consumedDeliveryIds.Contains(communicationCommand.DeliveryReceiptId) && !communications.Any(c => c.Content.UtteranceId == communicationCommand.UtteranceId), "DeliveryAlreadyAdmitted");
                var record = new CommunicationRecord(id, communicationCommand, true);
                return () => { communications.Add(record); consumedDeliveryIds.Add(communicationCommand.DeliveryReceiptId); revisions["communication_revision"]++; revisions["request_collection_revision"]++; learnerTurns++; };
            }
            if (command is AdjudicateAct act)
            {
                var communication = communications.SingleOrDefault(c => c.Id == act.CommunicationReceiptId);
                Require(communication != null, "CommunicationReceiptRequired");
                Require(act.AdjudicationId != Guid.Empty && !adjudications.Any(x => x.Id == act.AdjudicationId), "AdjudicationIdConflict");
                Require(Enum.IsDefined(typeof(ActFinding), act.Finding) && act.RuleVersion == LearnerCatalogue.RuleVersion, "UnknownAdjudicationRule");
                Require(act.Clauses.Count > 0 && act.Clauses.All(c => c != null && c.Start >= 0 && c.Length > 0 &&
                    (long)c.Start + c.Length <= communication.Content.ExactText.Length && c.ExactText == communication.Content.ExactText.Substring(c.Start, c.Length)), "ExactCommunicatedClausesRequired");
                var ordered = act.Clauses.OrderBy(c => c.Start).ToList();
                Require(!ordered.Zip(ordered.Skip(1), (a, b) => (long)a.Start + a.Length > b.Start).Any(overlap => overlap), "OverlappingClauseRefs");
                if (act.Finding == ActFinding.SupportedCommitment || act.Finding == ActFinding.SupportedCorrection) Require(act.Sources.Count > 0, "AdjudicationSourcesRequired");
                if (act.Finding == ActFinding.SupportedCorrection)
                {
                    var incident = act.IncidentId == null ? null : risks.LastOrDefault(x => x.Id == act.IncidentId);
                    Require(incident != null && incident.Active, "CorrectionIncidentRequired");
                    var original = communications.Single(x => x.Id == incident.Assertion.CommunicationReceiptId);
                    Require(original.Content.Recipient == communication.Content.Recipient && original.Id != communication.Id, "CorrectionRecipientMismatch");
                }
                else Require(act.IncidentId == null, "UnexpectedIncidentScope");
                var record = new AdjudicationRecord(act.AdjudicationId, act, true);
                return () => { adjudications.Add(record); revisions["adjudication_revision"]++; };
            }
            if (command is AssertRisk risk)
            {
                var communication = communications.SingleOrDefault(c => c.Id == risk.CommunicationReceiptId);
                var adjudication = adjudications.SingleOrDefault(a => a.Id == risk.AdjudicationReceiptId);
                Require(communication != null && adjudication != null, "ActualRiskProvenanceRequired");
                Require(adjudication.Content.CommunicationReceiptId == communication.Id && adjudication.Content.Finding == ActFinding.SupportedCommitment &&
                    communication.Content.UtteranceId == risk.UtteranceId && !string.IsNullOrWhiteSpace(risk.UnsupportedCommitment) &&
                    adjudication.Content.Clauses.Any(c => c.ExactText == risk.UnsupportedCommitment), "RiskScopeMismatch");
                var matchingClauses = adjudication.Content.Clauses.Where(c => c.ExactText == risk.UnsupportedCommitment).ToList();
                Require(matchingClauses.Count == 1, "AmbiguousRiskClause");
                var actKey = communication.Id.ToString("D") + "/" + matchingClauses[0].Start + "/" + matchingClauses[0].Length;
                Require(!assertedActKeys.Contains(actKey), "RiskAlreadyAssertedForAct");
                foreach (var p in adjudication.Content.Sources) ValidatePassage(p);
                var record = new RiskIncident(id, 1, risk);
                return () => { risks.Add(record); assertedActKeys.Add(actKey); revisions["risk_revision"]++; };
            }
            if (command is CorrectRisk correction)
            {
                var incident = risks.LastOrDefault(x => x.Id == correction.IncidentId);
                var communication = communications.SingleOrDefault(c => c.Id == correction.CommunicationReceiptId);
                var adjudication = adjudications.SingleOrDefault(a => a.Id == correction.AdjudicationReceiptId);
                Require(incident != null && incident.Active && communication != null && adjudication != null, "ActualCorrectionProvenanceRequired");
                Require(adjudication.Content.CommunicationReceiptId == communication.Id && adjudication.Content.Finding == ActFinding.SupportedCorrection &&
                    adjudication.Content.IncidentId == incident.Id && !string.IsNullOrWhiteSpace(correction.Withdrawal) &&
                    adjudication.Content.Clauses.Any(c => c.ExactText == correction.Withdrawal), "CorrectionScopeMismatch");
                Require(correction.ReplacementSources.Count > 0 && RefsEqual(correction.ReplacementSources, adjudication.Content.Sources), "CorrectionSourceScopeMismatch");
                var original = communications.Single(c => c.Id == incident.Assertion.CommunicationReceiptId);
                Require(original.Content.Recipient == communication.Content.Recipient && original.Id != communication.Id, "CorrectionRecipientMismatch");
                var record = new RiskIncident(incident.Id, incident.Revision + 1, incident.Assertion, correction);
                return () => { risks.Add(record); revisions["risk_revision"]++; };
            }
            throw new DomainRejection("UnknownCommand");
        }

        private static bool ValidOutcomes(System.Collections.Generic.IReadOnlyList<string> outcomes) => outcomes.Count > 0 &&
            outcomes.All(o => o == "O1" || o == "O2") && outcomes.Distinct().Count() == outcomes.Count;
        private bool ValidateHoldBasis(RequestHold proposal, bool trustedOwnerReviewed)
        {
            Require(Enum.IsDefined(typeof(HoldDefectBasis), proposal.Basis), "UnknownDefectBasis");
            if (proposal.Basis == HoldDefectBasis.Unverified) return false;
            Require(proposal.Anchors.Count > 0 && proposal.Anchors.All(p => p != null &&
                (p.SourceId.StartsWith("D02.task.v" + Task.Version + ".", StringComparison.Ordinal) ||
                 p.SourceId.StartsWith("D02.rubric.v" + Rubric.Version + ".", StringComparison.Ordinal))), "CurrentDefectAnchorsRequired");
            foreach (var p in proposal.Anchors) ValidatePassage(p);
            if (proposal.Basis == HoldDefectBasis.ReviewedInterpretation) return trustedOwnerReviewed;
            var present = proposal.Basis == HoldDefectBasis.MissingSourceAttachments ? Task.Sources.Count != 2 :
                proposal.Basis == HoldDefectBasis.MissingDecisionContext ? string.IsNullOrWhiteSpace(Task.DecisionContext) :
                Task.Questions.Any(q => Rubric.Criteria.Where(c => c.QuestionId == q.Id).Sum(c => (long)c.Marks) != q.Marks);
            Require(present, "AssertedStructuralDefectNotPresent"); return true;
        }
        private void ExactPair(int taskVersion, int rubricVersion)
        { Require(taskVersion == Task.Version && rubricVersion == Rubric.Version, "DocumentPairMismatch"); }
        private ReviewRequestRecord CurrentReview(Guid id)
        {
            var request = reviews.LastOrDefault(x => x.Id == id);
            Require(request != null && currentReview == id && request.Pair.Same(Pair), "CurrentReviewRequestRequired"); return request;
        }
        private void ValidateReleaseStructure()
        {
            Require(Task.Version > 1 && Task.TotalMarks == 20 && Task.Sources.Count == 2 && !string.IsNullOrWhiteSpace(Task.DecisionContext), "SubstantiveCurrentPairRequired");
            foreach (var p in Task.Sources) ValidatePassage(p);
            Require(Rubric.Criteria.Sum(c => (long)c.Marks) == 20 && Task.Questions.All(q => Rubric.Criteria.Where(c => c.QuestionId == q.Id).Sum(c => (long)c.Marks) == q.Marks), "PerQuestionMarksMismatch");
            // Arithmetic and attachment are operational prerequisites, never semantic alignment proof.
        }
        private static int ParseMinute(string text)
        {
            Require(text != null && text.Length == 5 && text[2] == ':' && text[0] >= '0' && text[0] <= '9' && text[1] >= '0' && text[1] <= '9' &&
                text[3] >= '0' && text[3] <= '9' && text[4] >= '0' && text[4] <= '9', "InvalidSameDayTime");
            var hour = int.Parse(text.Substring(0, 2), CultureInfo.InvariantCulture); var minute = int.Parse(text.Substring(3, 2), CultureInfo.InvariantCulture);
            Require(hour <= 23 && minute <= 59, "InvalidSameDayTime"); return hour * 60 + minute;
        }
        private static Passage NewPassage(string sourceId, int version, string text) =>
            new Passage(new PassageRef(sourceId, version, ContentDigest.Text(text), sourceId + ".p1"), text, InspectionLocation.Studio);
        private void RegistryChanged() { revisions["admitted_registry_revision"]++; revisions["source_registry_revision"]++; }
        private void InvalidateDependencies()
        {
            if (reviewStatus != ReviewStatus.Unrequested) reviewStatus = ReviewStatus.Stale;
            approval = null; approvalRevision = null; revisions["experiment_revision"]++;
            if (publication != PublicationStatus.Draft) publication = PublicationStatus.Stale;
        }
    }
}
