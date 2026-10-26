using System;
using System.Collections.Generic;
using System.Linq;

namespace Altaius.Demo02.Domain
{
    /// <summary>Original immutable request and historical document pins, retained even for rejection.
    /// Missing historical digests stay unavailable; current documents cannot fill them.</summary>
    public sealed class CommandScope
    {
        public CommandEnvelope Envelope { get; }
        public DocumentPair Pair { get; }
        internal CommandScope(CommandEnvelope envelope, DocumentPair pair) { Envelope = envelope; Pair = pair; }
    }
    public sealed class CommandReceipt
    {
        public Guid RequestId { get; } public string PayloadDigest { get; } public ReceiptStatus Status { get; }
        public int BeforeRevision { get; } public int AfterRevision { get; } public string ReasonCode { get; }
        public ActorRole Authority { get; } public string AuthorityDisclosure { get; } public string Action { get; }
        public bool Synthetic { get; } public IReadOnlyList<string> ChangedResources { get; }
        public CommandScope Scope { get; }
        public string RuleVersion => LearnerCatalogue.RuleVersion;
        internal CommandReceipt(Guid id, string digest, ReceiptStatus status, int before, int after, string reason,
            ActorRole actor, string disclosure, string action, bool synthetic, IEnumerable<string> changed = null, CommandScope scope = null)
        { RequestId = id; PayloadDigest = digest; Status = status; BeforeRevision = before; AfterRevision = after; ReasonCode = reason;
          Authority = actor; AuthorityDisclosure = disclosure; Action = action; Synthetic = synthetic; ChangedResources = status == ReceiptStatus.Unknown ? null : Frozen.List(changed); Scope = scope; }
    }
    public sealed class AuditRecord
    {
        public int Sequence { get; } public CommandReceipt Receipt { get; }
        internal AuditRecord(int sequence, CommandReceipt receipt) { Sequence = sequence; Receipt = receipt; }
    }
    public sealed class AnnotationRecord
    {
        public Guid Id { get; } public int Revision { get; } public int ExperimentRevision { get; }
        public DocumentPair Pair { get; } public Annotate Content { get; }
        internal AnnotationRecord(Guid id, int revision, int experimentRevision, DocumentPair pair, Annotate content)
        { Id = id; Revision = revision; ExperimentRevision = experimentRevision; Pair = pair; Content = content; }
    }
    public sealed class EvaluationRecord
    {
        public Guid Id { get; } public DocumentPair Pair { get; } public Evaluate Content { get; } public bool Synthetic { get; }
        internal EvaluationRecord(Guid id, DocumentPair pair, Evaluate content, bool synthetic) { Id = id; Pair = pair; Content = content; Synthetic = synthetic; }
    }
    public sealed class ReviewRequestRecord
    {
        public Guid Id { get; } public int Revision { get; } public DocumentPair Pair { get; } public int Minute { get; }
        public string ChangeSummary { get; } public ReviewStatus Status { get; } public Guid? DecisionReceiptId { get; }
        public string DecisionReason { get; }
        public string AuthorityDisclosure { get; }
        internal ReviewRequestRecord(Guid id, int revision, DocumentPair pair, int minute, string summary, ReviewStatus status,
            Guid? decision = null, string disclosure = null, string decisionReason = null)
        { Id = id; Revision = revision; Pair = pair; Minute = minute; ChangeSummary = summary; Status = status; DecisionReceiptId = decision; AuthorityDisclosure = disclosure; DecisionReason = decisionReason; }
    }
    public sealed class ApprovalRecord
    {
        public Guid ReceiptId { get; } public Guid RequestId { get; } public DocumentPair Pair { get; }
        public string Reason { get; } public string AuthorityDisclosure { get; }
        internal ApprovalRecord(Guid receiptId, Guid requestId, DocumentPair pair, string reason, string disclosure)
        { ReceiptId = receiptId; RequestId = requestId; Pair = pair; Reason = reason; AuthorityDisclosure = disclosure; }
    }
    public sealed class HoldRecord
    {
        public Guid Id { get; } public int Revision { get; } public DocumentPair Pair { get; } public RequestHold Proposal { get; }
        public bool? Accepted { get; } public bool SelfOwned { get; } public string AuthorityDisclosure { get; }
        public bool DefectVerified { get; }
        internal HoldRecord(Guid id, int revision, DocumentPair pair, RequestHold proposal, bool? accepted, bool selfOwned, string disclosure = null, bool defectVerified = false)
        { Id = id; Revision = revision; Pair = pair; Proposal = proposal; Accepted = accepted; SelfOwned = selfOwned; AuthorityDisclosure = disclosure; DefectVerified = defectVerified; }
    }
    public sealed class CommunicationRecord
    {
        public Guid Id { get; } public int Revision => 1; public RecordCommunication Content { get; } public bool Synthetic { get; }
        internal CommunicationRecord(Guid id, RecordCommunication content, bool synthetic) { Id = id; Content = content; Synthetic = synthetic; }
    }
    public sealed class AdjudicationRecord
    {
        public Guid Id { get; } public int Revision => 1; public AdjudicateAct Content { get; } public bool Synthetic { get; }
        internal AdjudicationRecord(Guid id, AdjudicateAct content, bool synthetic) { Id = id; Content = content; Synthetic = synthetic; }
    }
    public sealed class RiskIncident
    {
        public Guid Id { get; } public int Revision { get; } public AssertRisk Assertion { get; } public CorrectRisk Correction { get; }
        public bool Active => Correction == null;
        internal RiskIncident(Guid id, int revision, AssertRisk assertion, CorrectRisk correction = null) { Id = id; Revision = revision; Assertion = assertion; Correction = correction; }
    }
    public sealed class AssistanceRecord
    {
        public Guid Id { get; } public string OpportunityId { get; } public string Text { get; } public bool Strategic { get; }
        public bool Requested { get; } public bool Debrief { get; } public int LearnerTurn { get; }
        internal AssistanceRecord(Guid id, string opportunityId, string text, bool strategic, bool requested, bool debrief, int turn)
        { Id = id; OpportunityId = opportunityId; Text = text; Strategic = strategic; Requested = requested; Debrief = debrief; LearnerTurn = turn; }
    }
    public sealed class EvidenceObservation
    {
        public Guid CommandId { get; } public string Action { get; } public string OpportunityId { get; }
        public string Status { get; } public bool Assisted { get; } public bool AssistanceKnown { get; }
        public bool CompetenceClaim => false;
        internal EvidenceObservation(Guid id, string action, string opportunityId, bool assisted, bool known)
        { CommandId = id; Action = action; OpportunityId = opportunityId; Status = "Unverified learner hypothesis or operational act"; Assisted = assisted; AssistanceKnown = known; }
    }
    public sealed class SessionSnapshot
    {
        public Guid AttemptId { get; internal set; } public Guid? LinkedFromAttemptId { get; internal set; }
        public SessionMode Mode { get; internal set; } public int WorldRevision { get; internal set; }
        public SessionMode UnderlyingMode { get; internal set; }
        public int ClockMinute { get; internal set; } public int ClockRevision { get; internal set; }
        public int ExperimentRevision { get; internal set; } public TaskDocument Task { get; internal set; } public RubricDocument Rubric { get; internal set; }
        public DocumentPair Pair => new DocumentPair(Task, Rubric);
        public ReviewStatus ReviewStatus { get; internal set; } public PublicationStatus Publication { get; internal set; }
        public ApprovalRecord CurrentApproval { get; internal set; } public AssistanceHistory AssistanceHistory { get; internal set; }
        public IReadOnlyList<TaskDocument> TaskHistory { get; internal set; } public IReadOnlyList<RubricDocument> RubricHistory { get; internal set; }
        public IReadOnlyList<Passage> Notebook { get; internal set; } public IReadOnlyList<AnnotationRecord> AnnotationHistory { get; internal set; }
        public IReadOnlyList<EvaluationRecord> EvaluationHistory { get; internal set; } public IReadOnlyList<ReviewRequestRecord> ReviewHistory { get; internal set; }
        public IReadOnlyList<ApprovalRecord> ApprovalHistory { get; internal set; } public IReadOnlyList<HoldRecord> HoldHistory { get; internal set; }
        public IReadOnlyList<CommunicationRecord> Communications { get; internal set; } public IReadOnlyList<AdjudicationRecord> Adjudications { get; internal set; }
        public IReadOnlyList<RiskIncident> RiskHistory { get; internal set; } public IReadOnlyList<AssistanceRecord> Assistance { get; internal set; }
        public IReadOnlyList<EvidenceObservation> Evidence { get; internal set; } public IReadOnlyList<AuditRecord> Audit { get; internal set; }
        public IReadOnlyList<SessionSnapshot> LinkedHistory { get; internal set; }
        public IReadOnlyList<AnnotationRecord> CurrentAnnotations => Frozen.List(AnnotationHistory.Where(a => a.Pair.Same(Pair) &&
            a.Content.Passages.All(r => Notebook.Any(p => p.Reference.Same(r))) &&
            Notebook.Any(p => p.Reference.SourceId == "D02.rubric.v" + Rubric.Version + "." + a.Content.CriterionId))
            .GroupBy(a => a.Content.SpecimenId + "/" + a.Content.CriterionId).Select(g => g.Last()));
        public IReadOnlyList<EvaluationRecord> CurrentEvaluations => Frozen.List(EvaluationHistory.Where(e => e.Pair.Same(Pair) && e.Content.ExperimentRevision == ExperimentRevision &&
            e.Content.SubmittedAnnotationIds.All(id => CurrentAnnotations.Any(a => a.Id == id)) &&
            e.Content.Findings.SelectMany(f => f.Sources).All(r => Notebook.Any(p => p.Reference.Same(r)))));
        public IReadOnlyList<RiskIncident> ActiveRisks => Frozen.List(RiskHistory.GroupBy(r => r.Id).Select(g => g.Last()).Where(r => r.Active));
        public int DraftTotal(string specimenId) => CurrentAnnotations.Where(a => a.Content.SpecimenId == specimenId).Sum(a => a.Content.Marks);
        public bool HasLearnerEvidence => Mode != SessionMode.Presenter && Mode != SessionMode.Replay && Evidence.Count > 0;
    }
}
