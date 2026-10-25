using System;
using System.Collections.Generic;

namespace Altaius.Demo02.Domain
{
    public abstract class DomainCommand { public abstract string Action { get; } }
    public sealed class Annotate : DomainCommand
    {
        public override string Action => "experiment.annotate";
        public string SpecimenId { get; } public int SpecimenVersion { get; } public string CriterionId { get; }
        public IReadOnlyList<PassageRef> Passages { get; } public int Marks { get; } public string Rationale { get; }
        public int TaskVersion { get; } public int RubricVersion { get; }
        public Annotate(string specimenId, int specimenVersion, string criterionId, IEnumerable<PassageRef> passages, int marks, string rationale, int taskVersion, int rubricVersion)
        { SpecimenId = specimenId; SpecimenVersion = specimenVersion; CriterionId = criterionId; Passages = Frozen.List(passages); Marks = marks; Rationale = rationale; TaskVersion = taskVersion; RubricVersion = rubricVersion; }
    }
    public sealed class Evaluate : DomainCommand
    {
        public override string Action => "experiment.evaluate";
        public int ExperimentRevision { get; } public string SpecimenId { get; } public FindingDecision Decision { get; }
        public IReadOnlyList<CriterionFinding> Findings { get; } public FindingCoverage Coverage { get; }
        public IReadOnlyList<Guid> SubmittedAnnotationIds { get; }
        public Evaluate(int experimentRevision, string specimenId, FindingDecision decision, IEnumerable<CriterionFinding> findings, FindingCoverage coverage, IEnumerable<Guid> submittedAnnotationIds)
        { ExperimentRevision = experimentRevision; SpecimenId = specimenId; Decision = decision; Findings = Frozen.List(findings); Coverage = coverage; SubmittedAnnotationIds = Frozen.List(submittedAnnotationIds); }
    }
    public sealed class EditTask : DomainCommand
    {
        public override string Action => "task.edit";
        public int BaseVersion { get; } public IReadOnlyList<Question> Questions { get; } public IReadOnlyList<PassageRef> Sources { get; }
        public string DecisionContext { get; } public int TotalMarks { get; }
        public EditTask(int baseVersion, IEnumerable<Question> questions, IEnumerable<PassageRef> sources, string decisionContext, int totalMarks = 20)
        { BaseVersion = baseVersion; Questions = Frozen.List(questions); Sources = Frozen.List(sources); DecisionContext = decisionContext; TotalMarks = totalMarks; }
    }
    public sealed class EditRubric : DomainCommand
    {
        public override string Action => "rubric.edit";
        public int BaseVersion { get; } public IReadOnlyList<Criterion> Allocations { get; }
        public EditRubric(int baseVersion, IEnumerable<Criterion> allocations) { BaseVersion = baseVersion; Allocations = Frozen.List(allocations); }
    }
    public sealed class RequestReview : DomainCommand
    {
        public override string Action => "review.request";
        public int TaskVersion { get; } public int RubricVersion { get; } public string ChangeSummary { get; } public string ReviewBy { get; }
        public RequestReview(int taskVersion, int rubricVersion, string changeSummary, string reviewBy)
        { TaskVersion = taskVersion; RubricVersion = rubricVersion; ChangeSummary = changeSummary; ReviewBy = reviewBy; }
    }
    public sealed class ScheduleReply : DomainCommand
    {
        public override string Action => "review.schedule_reply";
        public Guid RequestId { get; } public int TaskVersion { get; } public int RubricVersion { get; }
        public string ReviewBy { get; } public ScheduleDecision Decision { get; } public Guid ReplyId { get; }
        public ScheduleReply(Guid requestId, int taskVersion, int rubricVersion, string reviewBy, ScheduleDecision decision, Guid replyId)
        { RequestId = requestId; TaskVersion = taskVersion; RubricVersion = rubricVersion; ReviewBy = reviewBy; Decision = decision; ReplyId = replyId; }
    }
    public sealed class DecideReview : DomainCommand
    {
        public override string Action => "review.decide";
        public Guid RequestId { get; } public int TaskVersion { get; } public int RubricVersion { get; }
        public ReviewDecision Decision { get; } public string Reason { get; }
        public DecideReview(Guid requestId, int taskVersion, int rubricVersion, ReviewDecision decision, string reason)
        { RequestId = requestId; TaskVersion = taskVersion; RubricVersion = rubricVersion; Decision = decision; Reason = reason; }
    }
    public sealed class RequestRelease : DomainCommand
    {
        public override string Action => "release.request";
        public int TaskVersion { get; } public int RubricVersion { get; } public Guid ApprovalReceiptId { get; }
        public RequestRelease(int taskVersion, int rubricVersion, Guid approvalReceiptId)
        { TaskVersion = taskVersion; RubricVersion = rubricVersion; ApprovalReceiptId = approvalReceiptId; }
    }
    public sealed class RequestHold : DomainCommand
    {
        public override string Action => "release.hold_request";
        public string Defect { get; } public ActorRole Owner { get; } public string ReviewAt { get; } public string NextAction { get; }
        public HoldDefectBasis Basis { get; } public IReadOnlyList<PassageRef> Anchors { get; }
        public RequestHold(string defect, ActorRole owner, string reviewAt, string nextAction, HoldDefectBasis basis = HoldDefectBasis.Unverified, IEnumerable<PassageRef> anchors = null)
        { Defect = defect; Owner = owner; ReviewAt = reviewAt; NextAction = nextAction; Basis = basis; Anchors = Frozen.List(anchors); }
    }
    public sealed class AcceptHold : DomainCommand
    {
        public override string Action => "release.hold_accept";
        public Guid RequestId { get; } public int TaskVersion { get; } public int RubricVersion { get; } public string ReviewAt { get; }
        public bool Accepted { get; } public bool ConfirmedSelfCommitment { get; }
        public bool ConfirmsSubstantiveDefect { get; }
        public AcceptHold(Guid requestId, int taskVersion, int rubricVersion, string reviewAt, bool accepted, bool confirmedSelfCommitment = false, bool confirmsSubstantiveDefect = false)
        { RequestId = requestId; TaskVersion = taskVersion; RubricVersion = rubricVersion; ReviewAt = reviewAt; Accepted = accepted; ConfirmedSelfCommitment = confirmedSelfCommitment; ConfirmsSubstantiveDefect = confirmsSubstantiveDefect; }
    }
    public sealed class RecordCommunication : DomainCommand
    {
        public override string Action => "communication.record";
        public Guid UtteranceId { get; } public ActorRole Recipient { get; } public string ExactText { get; } public Guid DeliveryReceiptId { get; }
        public RecordCommunication(Guid utteranceId, ActorRole recipient, string exactText, Guid deliveryReceiptId)
        { UtteranceId = utteranceId; Recipient = recipient; ExactText = exactText; DeliveryReceiptId = deliveryReceiptId; }
    }
    public sealed class AdjudicateAct : DomainCommand
    {
        public override string Action => "act.adjudicate";
        public Guid CommunicationReceiptId { get; } public ActFinding Finding { get; } public IReadOnlyList<ClauseRef> Clauses { get; }
        public string RuleVersion { get; } public IReadOnlyList<PassageRef> Sources { get; } public Guid AdjudicationId { get; }
        public Guid? IncidentId { get; }
        public AdjudicateAct(Guid communicationReceiptId, ActFinding finding, IEnumerable<ClauseRef> clauses, string ruleVersion, IEnumerable<PassageRef> sources, Guid adjudicationId, Guid? incidentId = null)
        { CommunicationReceiptId = communicationReceiptId; Finding = finding; Clauses = Frozen.List(clauses); RuleVersion = ruleVersion; Sources = Frozen.List(sources); AdjudicationId = adjudicationId; IncidentId = incidentId; }
    }
    public sealed class AssertRisk : DomainCommand
    {
        public override string Action => "risk.assert";
        public Guid UtteranceId { get; } public string UnsupportedCommitment { get; } public Guid CommunicationReceiptId { get; } public Guid AdjudicationReceiptId { get; }
        public AssertRisk(Guid utteranceId, string unsupportedCommitment, Guid communicationReceiptId, Guid adjudicationReceiptId)
        { UtteranceId = utteranceId; UnsupportedCommitment = unsupportedCommitment; CommunicationReceiptId = communicationReceiptId; AdjudicationReceiptId = adjudicationReceiptId; }
    }
    public sealed class CorrectRisk : DomainCommand
    {
        public override string Action => "risk.correct";
        public Guid IncidentId { get; } public string Withdrawal { get; } public IReadOnlyList<PassageRef> ReplacementSources { get; }
        public Guid CommunicationReceiptId { get; } public Guid AdjudicationReceiptId { get; }
        public CorrectRisk(Guid incidentId, string withdrawal, IEnumerable<PassageRef> replacementSources, Guid communicationReceiptId, Guid adjudicationReceiptId)
        { IncidentId = incidentId; Withdrawal = withdrawal; ReplacementSources = Frozen.List(replacementSources); CommunicationReceiptId = communicationReceiptId; AdjudicationReceiptId = adjudicationReceiptId; }
    }
    public sealed class CommandEnvelope
    {
        public Guid CommandId { get; } public Guid AttemptId { get; } public int ExpectedWorldRevision { get; }
        public string ScenarioVersion { get; } public string FixtureSha256 { get; } public DomainCommand Command { get; }
        public IReadOnlyDictionary<string, int?> Readset { get; } public IReadOnlyList<PassageRef> SourceRefs { get; }
        public Guid? CausationId { get; } public IReadOnlyList<Guid> AssistanceContextIds { get; }
        public CommandEnvelope(Guid commandId, Guid attemptId, int expectedWorldRevision, DomainCommand command, IDictionary<string, int?> readset,
            IEnumerable<PassageRef> sourceRefs = null, Guid? causationId = null, IEnumerable<Guid> assistanceContextIds = null,
            string scenarioVersion = "6.0-proposed", string fixtureSha256 = LearnerCatalogue.FixtureSha256)
        { CommandId = commandId; AttemptId = attemptId; ExpectedWorldRevision = expectedWorldRevision; Command = command;
          Readset = Frozen.Revisions(readset ?? new Dictionary<string, int?>()); SourceRefs = Frozen.List(sourceRefs); CausationId = causationId;
          AssistanceContextIds = Frozen.List(assistanceContextIds); ScenarioVersion = scenarioVersion; FixtureSha256 = fixtureSha256; }
    }
}
