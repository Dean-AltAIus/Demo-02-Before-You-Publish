using System;
using System.Collections.Generic;
using System.Linq;

namespace Altaius.Demo02.Domain
{
    /// <summary>Ephemeral offline domain. Stage preserves drafts; Submit alone commits a command.</summary>
    public sealed partial class OfflineSession
    {
        private Guid attempt;
        private Guid? linkedFrom;
        private SessionMode mode;
        private readonly SessionMode underlyingMode;
        private readonly ScenarioController controller;
        private readonly HashSet<Guid> tombstones = new HashSet<Guid>();
        private readonly List<SessionSnapshot> linkedHistory = new List<SessionSnapshot>();
        private readonly Dictionary<Guid, CommandReceipt> receipts = new Dictionary<Guid, CommandReceipt>();
        private readonly Dictionary<string, Passage> registry = new Dictionary<string, Passage>(StringComparer.Ordinal);
        private readonly HashSet<string> inspected = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> revoked = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> revisions = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<TaskDocument> tasks = new List<TaskDocument>();
        private readonly List<RubricDocument> rubrics = new List<RubricDocument>();
        private readonly List<AnnotationRecord> annotations = new List<AnnotationRecord>();
        private readonly List<EvaluationRecord> evaluations = new List<EvaluationRecord>();
        private readonly List<ReviewRequestRecord> reviews = new List<ReviewRequestRecord>();
        private readonly List<ApprovalRecord> approvals = new List<ApprovalRecord>();
        private readonly List<HoldRecord> holds = new List<HoldRecord>();
        private readonly List<CommunicationRecord> communications = new List<CommunicationRecord>();
        private readonly List<AdjudicationRecord> adjudications = new List<AdjudicationRecord>();
        private readonly List<RiskIncident> risks = new List<RiskIncident>();
        private readonly List<AssistanceRecord> assistance = new List<AssistanceRecord>();
        private readonly List<EvidenceObservation> evidence = new List<EvidenceObservation>();
        private readonly List<AuditRecord> audit = new List<AuditRecord>();
        private readonly HashSet<string> evidenceCreditKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<Guid> consumedDeliveryIds = new HashSet<Guid>();
        private readonly HashSet<Guid> consumedReplyIds = new HashSet<Guid>();
        private readonly HashSet<string> assertedActKeys = new HashSet<string>(StringComparer.Ordinal);
        private int world, minute, learnerTurns;
        private int? approvalRevision;
        private Guid? currentReview, currentHold;
        private ApprovalRecord approval;
        private ReviewStatus reviewStatus;
        private PublicationStatus publication;
        private AssistanceHistory assistanceHistory;
        private TaskDocument Task => tasks.Last();
        private RubricDocument Rubric => rubrics.Last();
        private DocumentPair Pair => new DocumentPair(Task, Rubric);

        private OfflineSession(SessionMode selectedMode)
        {
            if (!Enum.IsDefined(typeof(SessionMode), selectedMode)) throw new ArgumentOutOfRangeException(nameof(selectedMode));
            mode = selectedMode; underlyingMode = selectedMode; controller = new ScenarioController(this); Initialise();
        }
        public static OfflineSession Create(SessionMode mode = SessionMode.Coached) => new OfflineSession(mode);
        public static OfflineSession Create(SessionMode mode, out ScenarioController controller)
        { var session = new OfflineSession(mode); controller = session.controller; return session; }

        private void Initialise()
        {
            attempt = Guid.NewGuid(); world = 0; minute = 800; learnerTurns = 0; approvalRevision = null;
            approval = null; currentReview = null; currentHold = null; reviewStatus = ReviewStatus.Unrequested; publication = PublicationStatus.Draft;
            receipts.Clear(); registry.Clear(); inspected.Clear(); revoked.Clear(); revisions.Clear(); tasks.Clear(); rubrics.Clear();
            annotations.Clear(); evaluations.Clear(); reviews.Clear(); approvals.Clear(); holds.Clear(); communications.Clear(); adjudications.Clear(); risks.Clear();
            assistance.Clear(); evidence.Clear(); audit.Clear(); evidenceCreditKeys.Clear(); consumedDeliveryIds.Clear(); consumedReplyIds.Clear(); assertedActKeys.Clear();
            assistanceHistory = AssistanceHistory.Complete;
            foreach (var name in new[] { "experiment_revision", "request_collection_revision", "risk_revision", "clock_revision", "review_request_revision", "hold_request_revision", "source_registry_revision", "communication_revision", "adjudication_revision", "admitted_registry_revision" }) revisions[name] = 1;
            foreach (var p in LearnerCatalogue.Create())
            {
                if (ContentDigest.Text(p.ExactText) != p.Reference.Sha256) throw new InvalidOperationException("Pinned learner passage integrity failure.");
                registry.Add(p.Reference.SourceId, p);
            }
            tasks.Add(new TaskDocument(1, new[] { new Question("Q1", "Name two sources of information.", 10, "O1"), new Question("Q2", "List three features of a reliable source.", 10, "O2") }, null, null));
            rubrics.Add(new RubricDocument(1, new[] { new Criterion("naming", "Q1", 10, "naming", "O1"), new Criterion("listing", "Q2", 10, "listing", "O2") }));
        }
        public SessionSnapshot Snapshot => new SessionSnapshot
        {
            AttemptId = attempt, LinkedFromAttemptId = linkedFrom, Mode = mode, UnderlyingMode = underlyingMode, WorldRevision = world, ClockMinute = minute,
            ClockRevision = revisions["clock_revision"], ExperimentRevision = revisions["experiment_revision"], Task = Task, Rubric = Rubric,
            ReviewStatus = reviewStatus, Publication = publication, CurrentApproval = approval != null && approval.Pair.Same(Pair) ? approval : null,
            AssistanceHistory = assistanceHistory, TaskHistory = Frozen.List(tasks), RubricHistory = Frozen.List(rubrics),
            Notebook = Frozen.List(registry.Values.Where(p => inspected.Contains(p.Reference.Key) && !revoked.Contains(p.Reference.Key))),
            AnnotationHistory = Frozen.List(annotations), EvaluationHistory = Frozen.List(evaluations), ReviewHistory = Frozen.List(reviews),
            ApprovalHistory = Frozen.List(approvals), HoldHistory = Frozen.List(holds), Communications = Frozen.List(communications), Adjudications = Frozen.List(adjudications),
            RiskHistory = Frozen.List(risks), Assistance = Frozen.List(assistance), Evidence = Frozen.List(evidence), Audit = Frozen.List(audit), LinkedHistory = Frozen.List(linkedHistory)
        };
        public IReadOnlyList<string> InspectableSourceIds(InspectionLocation location) => Frozen.List(registry.Values
            .Where(p => !revoked.Contains(p.Reference.Key) && (p.Location == location || location == InspectionLocation.PortableWorkspace && inspected.Contains(p.Reference.Key)))
            .Select(p => p.Reference.SourceId));
        public Passage Inspect(string sourceId, InspectionLocation location)
        {
            if (mode == SessionMode.Replay) throw new InvalidOperationException("ReplayReadOnly");
            if (sourceId == null || !registry.TryGetValue(sourceId, out var p) || revoked.Contains(p.Reference.Key)) throw new InvalidOperationException("PassageUnavailable");
            if (location != p.Location && !(location == InspectionLocation.PortableWorkspace && inspected.Contains(p.Reference.Key))) throw new InvalidOperationException("LocalInspectionRequired");
            if (inspected.Add(p.Reference.Key)) { revisions["admitted_registry_revision"]++; world++; }
            return p;
        }
        public CommandEnvelope Stage(DomainCommand command, string opportunityId, Guid? id = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(opportunityId)) throw new ArgumentException("An explicit opportunity ID is required.", nameof(opportunityId));
            var commandId = id ?? Guid.NewGuid();
            return new CommandEnvelope(commandId, attempt, world, command, RequiredReadset(command, commandId), opportunityId, SourcesFor(command), null,
                RelevantAssistanceIds(opportunityId));
        }
        public CommandReceipt Submit(CommandEnvelope envelope) => Execute(envelope, ActorRole.Learner, "Actual learner action in local offline session", true, false);
        public CommandReceipt FindReceipt(Guid commandId) => receipts.TryGetValue(commandId, out var receipt) ? receipt : null;
        internal bool IsCurrentAttempt(Guid expectedAttempt) => expectedAttempt == attempt && mode != SessionMode.Replay;
        internal CommandReceipt SubmitScripted(CommandEnvelope envelope, ScriptedPermit permit)
        {
            var valid = permit != null && permit.ControllerId == controller.Id && permit.AttemptId == envelope?.AttemptId &&
                permit.EnvelopeDigest == ContentDigest.Of(envelope) && !string.IsNullOrWhiteSpace(permit.Condition) &&
                (permit.Role == ActorRole.ModuleLead || permit.Role == ActorRole.Colleague || permit.Role == ActorRole.RecipientChannel);
            return Execute(envelope, permit?.Role ?? ActorRole.ScenarioController,
                "Authored offline scripted response: " + permit?.Condition, valid, false);
        }
        internal CommandReceipt SubmitSyntheticEvaluation(CommandEnvelope envelope, ActorRole role)
            => Execute(envelope, role, "Synthetic presenter finding; no learner evidence or semantic evaluation", mode == SessionMode.Presenter, true);

        private CommandReceipt Execute(CommandEnvelope envelope, ActorRole actor, string disclosure, bool capabilityValid, bool evaluatorRoute)
        {
            var before = world;
            var digest = ContentDigest.Of(new { Envelope = envelope, Actor = actor, Disclosure = disclosure });
            var scope = CaptureScope(envelope);
            CommandReceipt Result(ReceiptStatus status, string reason, IEnumerable<string> changed = null)
            {
                var r = new CommandReceipt(envelope?.CommandId ?? Guid.Empty, digest, status, before, world, reason, actor, disclosure,
                    envelope?.Command?.Action, mode == SessionMode.Presenter || actor != ActorRole.Learner, changed, scope);
                if (mode != SessionMode.Replay) audit.Add(new AuditRecord(audit.Count + 1, r));
                return r;
            }
            if (envelope == null || envelope.Command == null || envelope.CommandId == Guid.Empty) return Result(ReceiptStatus.Rejected, "MalformedEnvelope");
            if (mode == SessionMode.Replay) return Result(ReceiptStatus.Rejected, "ReplayReadOnly");
            if (tombstones.Contains(envelope.AttemptId) || envelope.AttemptId != attempt) return Result(ReceiptStatus.Rejected, "AttemptTombstoned");
            if (receipts.TryGetValue(envelope.CommandId, out var old))
            {
                if (old.PayloadDigest != digest) return Result(ReceiptStatus.Rejected, "CommandIdConflict");
                audit.Add(new AuditRecord(audit.Count + 1, old)); return old;
            }
            CommandReceipt finish;
            try
            {
                Require(envelope.ScenarioVersion == LearnerCatalogue.RuleVersion && envelope.FixtureSha256 == LearnerCatalogue.FixtureSha256, "ContractPinMismatch");
                Require(!string.IsNullOrWhiteSpace(envelope.OpportunityId), "OpportunityRequired");
                if (evaluatorRoute && mode != SessionMode.Presenter) throw new DomainUnavailable("EvaluatorUnavailable");
                Require(!evaluatorRoute || envelope.Command is Evaluate && actor == ActorRole.ExperimentReviewer ||
                    evaluatorRoute && (envelope.Command is AdjudicateAct || envelope.Command is AssertRisk || envelope.Command is CorrectRisk) && actor == ActorRole.GroundedEvaluator, "EvaluatorRouteScopeRejected");
                Require(capabilityValid, "AuthorityScopeRejected");
                Require(RoleAllows(envelope.Command, actor), "UnauthorizedActor");
                Require(envelope.ExpectedWorldRevision == world, "StaleWorldRevision");
                var expected = RequiredReadset(envelope.Command, envelope.CommandId);
                Require(envelope.Readset.Count == expected.Count && expected.All(x => envelope.Readset.TryGetValue(x.Key, out var value) && value == x.Value), "ReadsetMismatch");
                Require(envelope.CausationId == null || receipts.TryGetValue(envelope.CausationId.Value, out var cause) && cause.Status == ReceiptStatus.Accepted, "UnknownCausation");
                Require(envelope.AssistanceContextIds.Distinct().Count() == envelope.AssistanceContextIds.Count && envelope.AssistanceContextIds.All(id => assistance.Any(a => a.Id == id)), "UnknownAssistanceContext");
                Require(envelope.AssistanceContextIds.OrderBy(id => id).SequenceEqual(RelevantAssistanceIds(envelope.OpportunityId).OrderBy(id => id)), "AssistanceScopeMismatch");
                Require(RefsEqual(envelope.SourceRefs, SourcesFor(envelope.Command)), "SourceScopeMismatch");
                foreach (var reference in envelope.SourceRefs) ValidatePassage(reference);
                var mutation = PrepareMutation(envelope, actor, disclosure);
                mutation(); world++;
                if (actor == ActorRole.Learner)
                {
                    learnerTurns++;
                    if (mode != SessionMode.Presenter)
                    {
                        var key = ContentDigest.Of(new { envelope.OpportunityId, envelope.Command });
                        if (evidenceCreditKeys.Add(key)) evidence.Add(new EvidenceObservation(envelope.CommandId, envelope.Command.Action, envelope.OpportunityId,
                            linkedFrom != null || RelevantAssistanceIds(envelope.OpportunityId).Any(), assistanceHistory == AssistanceHistory.Complete));
                    }
                }
                finish = Result(ReceiptStatus.Accepted, "Accepted", ChangedResources(envelope.Command));
            }
            catch (DomainUnavailable e) { finish = Result(ReceiptStatus.Unknown, e.Message); }
            catch (DomainRejection e) { finish = Result(ReceiptStatus.Rejected, e.Message); }
            receipts.Add(envelope.CommandId, finish);
            return finish;
        }
        private CommandScope CaptureScope(CommandEnvelope envelope)
        {
            if (envelope == null) return null;
            int? taskVersion = envelope.Readset.TryGetValue("task_version", out var t) ? t : null;
            int? rubricVersion = envelope.Readset.TryGetValue("rubric_version", out var r) ? r : null;
            // Explicit requested versions take precedence over readset pins, including malformed or stale requests.
            if (envelope.Command is Annotate a) { taskVersion = a.TaskVersion; rubricVersion = a.RubricVersion; }
            else if (envelope.Command is RequestReview q) { taskVersion = q.TaskVersion; rubricVersion = q.RubricVersion; }
            else if (envelope.Command is ScheduleReply s) { taskVersion = s.TaskVersion; rubricVersion = s.RubricVersion; }
            else if (envelope.Command is DecideReview d) { taskVersion = d.TaskVersion; rubricVersion = d.RubricVersion; }
            else if (envelope.Command is RequestRelease release) { taskVersion = release.TaskVersion; rubricVersion = release.RubricVersion; }
            else if (envelope.Command is AcceptHold h) { taskVersion = h.TaskVersion; rubricVersion = h.RubricVersion; }
            var historical = linkedHistory.FirstOrDefault(x => x.AttemptId == envelope.AttemptId);
            IEnumerable<TaskDocument> taskHistory = envelope.AttemptId == attempt ? tasks : historical?.TaskHistory ?? Enumerable.Empty<TaskDocument>();
            IEnumerable<RubricDocument> rubricHistory = envelope.AttemptId == attempt ? rubrics : historical?.RubricHistory ?? Enumerable.Empty<RubricDocument>();
            var pair = taskVersion.HasValue && rubricVersion.HasValue ? new DocumentPair(taskVersion.Value, rubricVersion.Value,
                taskHistory.FirstOrDefault(x => x.Version == taskVersion)?.Digest, rubricHistory.FirstOrDefault(x => x.Version == rubricVersion)?.Digest) : null;
            return new CommandScope(envelope, pair);
        }
        private static IEnumerable<string> ChangedResources(DomainCommand c)
        { yield return c.Action; yield return "world_revision"; if (c is EditTask || c is EditRubric) { yield return "approval_revision"; yield return "experiment_revision"; } }
        private IEnumerable<Guid> RelevantAssistanceIds(string opportunityId) =>
            assistance.Where(a => a.Strategic && a.OpportunityId == opportunityId).Select(a => a.Id);
        private static bool RoleAllows(DomainCommand c, ActorRole actor)
        {
            if (c is Evaluate) return actor == ActorRole.ExperimentReviewer;
            if (c is AdjudicateAct || c is AssertRisk || c is CorrectRisk) return actor == ActorRole.GroundedEvaluator;
            if (c is RecordCommunication) return actor == ActorRole.RecipientChannel;
            if (c is ScheduleReply || c is DecideReview) return actor == ActorRole.ModuleLead;
            if (c is AcceptHold) return actor == ActorRole.Learner || actor == ActorRole.ModuleLead || actor == ActorRole.Colleague;
            return actor == ActorRole.Learner && (c is Annotate || c is EditTask || c is EditRubric || c is RequestReview || c is RequestRelease || c is RequestHold);
        }
        private Dictionary<string, int?> RequiredReadset(DomainCommand c, Guid commandId)
        {
            var result = new Dictionary<string, int?>(StringComparer.Ordinal);
            void Add(params string[] names) { foreach (var n in names) result[n] = Revision(n); }
            void Request(Guid id) { result["requests/" + id.ToString("D") + "/revision"] = RequestRevision(id); Add("request_collection_revision"); }
            if (c is Annotate a) { Add("task_version", "rubric_version", "experiment_revision"); result["specimen_revisions/" + a.SpecimenId] = SpecimenRevision(a.SpecimenId); }
            else if (c is Evaluate e) { Add("task_version", "rubric_version", "experiment_revision"); result["specimen_revisions/" + e.SpecimenId] = SpecimenRevision(e.SpecimenId); foreach (var id in e.SubmittedAnnotationIds.Distinct()) result["annotations/" + id.ToString("D") + "/revision"] = annotations.LastOrDefault(x => x.Id == id)?.Revision; }
            else if (c is EditTask) Add("task_version");
            else if (c is EditRubric) Add("task_version", "rubric_version");
            else if (c is RequestReview) { Add("task_version", "rubric_version", "clock_revision", "review_request_revision"); Request(commandId); }
            else if (c is ScheduleReply sr) { Add("task_version", "rubric_version", "clock_revision", "review_request_revision"); Request(sr.RequestId); }
            else if (c is DecideReview dr) { Add("task_version", "rubric_version", "clock_revision", "review_request_revision"); Request(dr.RequestId); }
            else if (c is RequestRelease) Add("task_version", "rubric_version", "approval_revision");
            else if (c is RequestHold) { Add("task_version", "rubric_version", "clock_revision", "hold_request_revision"); Request(commandId); }
            else if (c is AcceptHold ah) { Add("task_version", "rubric_version", "clock_revision", "hold_request_revision"); Request(ah.RequestId); }
            else if (c is RecordCommunication) { Add("communication_revision", "request_collection_revision"); result["communication_receipts/" + commandId.ToString("D") + "/revision"] = communications.FirstOrDefault(x => x.Id == commandId)?.Revision; }
            else if (c is AdjudicateAct aa) { Add("communication_revision", "adjudication_revision", "risk_revision", "admitted_registry_revision"); result["adjudications/" + aa.AdjudicationId.ToString("D") + "/revision"] = adjudications.FirstOrDefault(x => x.Id == aa.AdjudicationId)?.Revision; }
            else if (c is AssertRisk ar) { RiskReadset(ar.CommunicationReceiptId, ar.AdjudicationReceiptId, result); result["risk_incidents/" + commandId.ToString("D") + "/revision"] = risks.LastOrDefault(x => x.Id == commandId)?.Revision; }
            else if (c is CorrectRisk cr) { RiskReadset(cr.CommunicationReceiptId, cr.AdjudicationReceiptId, result); result["risk_incidents/" + cr.IncidentId.ToString("D") + "/revision"] = risks.LastOrDefault(x => x.Id == cr.IncidentId)?.Revision; }
            if (SourcesFor(c).Any()) Add("admitted_registry_revision", "source_registry_revision");
            return result;
        }
        private void RiskReadset(Guid communication, Guid adjudication, Dictionary<string, int?> target)
        {
            target["risk_revision"] = revisions["risk_revision"];
            target["communication_receipts/" + communication.ToString("D") + "/revision"] = communications.FirstOrDefault(x => x.Id == communication)?.Revision;
            target["adjudications/" + adjudication.ToString("D") + "/revision"] = adjudications.FirstOrDefault(x => x.Id == adjudication)?.Revision;
            target["communication_revision"] = revisions["communication_revision"]; target["adjudication_revision"] = revisions["adjudication_revision"];
        }
        private int? Revision(string path)
        {
            if (path == "task_version") return Task.Version;
            if (path == "rubric_version") return Rubric.Version;
            if (path == "approval_revision") return approvalRevision;
            return revisions.TryGetValue(path, out var value) ? value : (int?)null;
        }
        private int? SpecimenRevision(string id) => id != null && id.StartsWith("W_", StringComparison.Ordinal) && registry.TryGetValue(id, out var p) ? p.Reference.Version : (int?)null;
        private int? RequestRevision(Guid id) => reviews.LastOrDefault(x => x.Id == id)?.Revision ?? holds.LastOrDefault(x => x.Id == id)?.Revision;
        private static IEnumerable<PassageRef> SourcesFor(DomainCommand c)
        {
            IEnumerable<PassageRef> refs = Enumerable.Empty<PassageRef>();
            if (c is Annotate a) refs = a.Passages;
            else if (c is EditTask t) refs = t.Sources;
            else if (c is Evaluate e) refs = e.Findings.Where(f => f != null).SelectMany(f => f.Sources);
            else if (c is AdjudicateAct aa) refs = aa.Sources;
            else if (c is CorrectRisk cr) refs = cr.ReplacementSources;
            else if (c is RequestHold hold) refs = hold.Anchors;
            return refs.GroupBy(r => r == null ? "null" : r.Key + "/" + r.Sha256, StringComparer.Ordinal).Select(g => g.First());
        }
        private void ValidatePassage(PassageRef reference)
        {
            Require(reference != null && reference.SourceId != null && registry.TryGetValue(reference.SourceId, out var p) && p.Reference.Same(reference), "PassageNotAdmitted");
            Require(!revoked.Contains(reference.Key), "PassageRevoked"); Require(inspected.Contains(reference.Key), "PassageNotInspected");
        }
        private static bool RefsEqual(IEnumerable<PassageRef> a, IEnumerable<PassageRef> b)
        {
            string Key(PassageRef p) => p == null ? "null" : p.Key + "/" + p.Sha256;
            return a.Select(Key).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(b.Select(Key).OrderBy(x => x, StringComparer.Ordinal));
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new DomainRejection(reason); }
        private sealed class DomainRejection : Exception { internal DomainRejection(string reason) : base(reason) { } }
        private sealed class DomainUnavailable : Exception { internal DomainUnavailable(string reason) : base(reason) { } }
        public void EnterReplay() { mode = SessionMode.Replay; }
        public void Reset(bool linkedPractice)
        {
            var previous = Snapshot; tombstones.Add(attempt);
            if (linkedPractice) linkedHistory.Add(previous); else linkedHistory.Clear();
            mode = underlyingMode;
            Initialise(); linkedFrom = linkedPractice ? previous.AttemptId : (Guid?)null;
            if (linkedPractice) { assistance.AddRange(previous.Assistance); assistanceHistory = previous.AssistanceHistory; }
        }
        internal bool Revoke(PassageRef reference)
        {
            if (mode == SessionMode.Replay || reference == null || !registry.TryGetValue(reference.SourceId, out var p) || !p.Reference.Same(reference) || revoked.Contains(reference.Key)) return false;
            revoked.Add(reference.Key); revisions["admitted_registry_revision"]++; revisions["source_registry_revision"]++;
            InvalidateDependencies(); world++; return true;
        }
        internal bool DeliverAssistance(Guid id, string opportunity, string text, bool strategic, bool requested, bool debrief, bool inputBusy)
        {
            if (mode == SessionMode.Replay || id == Guid.Empty || string.IsNullOrWhiteSpace(opportunity) || string.IsNullOrWhiteSpace(text) || assistance.Any(a => a.Id == id)) return false;
            if (strategic && !requested && !debrief)
            {
                var unsolicited = assistance.Where(a => a.Strategic && !a.Requested && !a.Debrief).ToList();
                if (mode == SessionMode.Independent || inputBusy || unsolicited.Count >= 2 || unsolicited.Count > 0 && learnerTurns - unsolicited.Last().LearnerTurn < 4) return false;
            }
            assistance.Add(new AssistanceRecord(id, opportunity, text, strategic, requested, debrief, learnerTurns)); world++; return true;
        }
        internal void MarkAssistanceHistoryUnavailable()
        { if (mode != SessionMode.Replay && assistanceHistory != AssistanceHistory.Unavailable) { assistanceHistory = AssistanceHistory.Unavailable; world++; } }
    }
}
