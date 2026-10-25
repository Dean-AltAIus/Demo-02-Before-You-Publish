using System;

namespace Altaius.Demo02.Domain
{
    // Future adapter contract. OfflineSession never connects or invokes a live evaluator.
    // An implementation must authenticate the issuer and validate this exact attempt,
    // command digest, communicated clauses, sources and rule version before admission.
    public interface ITrustedEvaluatorPort
    {
        EvaluatorVerification Verify(EvaluatorScope scope, string opaqueReceipt);
    }
    public sealed class EvaluatorScope
    {
        public Guid AttemptId { get; } public string CommandDigest { get; } public DocumentPair Pair { get; }
        public Guid? CommunicationReceiptId { get; } public string RuleVersion => LearnerCatalogue.RuleVersion;
        public EvaluatorScope(Guid attemptId, string commandDigest, DocumentPair pair, Guid? communicationReceiptId)
        { AttemptId = attemptId; CommandDigest = commandDigest; Pair = pair; CommunicationReceiptId = communicationReceiptId; }
    }
    public sealed class EvaluatorVerification
    {
        public ReceiptStatus Status { get; } public string Reason { get; }
        public EvaluatorVerification(ReceiptStatus status, string reason) { Status = status; Reason = reason; }
    }
    public sealed class ScriptedPermit
    {
        internal Guid ControllerId { get; } internal Guid AttemptId { get; } internal string EnvelopeDigest { get; }
        internal ActorRole Role { get; } internal string Condition { get; }
        internal ScriptedPermit(Guid controllerId, Guid attemptId, string envelopeDigest, ActorRole role, string condition)
        { ControllerId = controllerId; AttemptId = attemptId; EnvelopeDigest = envelopeDigest; Role = role; Condition = condition; }
    }
    // Keep this handle in the composition root/script runner, never in learner input bindings.
    public sealed class ScenarioController
    {
        private readonly OfflineSession session;
        internal readonly Guid Id = Guid.NewGuid();
        internal ScenarioController(OfflineSession session) { this.session = session; }
        public ScriptedPermit PermitExactScriptedResponse(CommandEnvelope envelope, ActorRole role, string satisfiedScriptCondition)
            => new ScriptedPermit(Id, envelope.AttemptId, ContentDigest.Of(envelope), role, satisfiedScriptCondition);
        public CommandReceipt SubmitScripted(CommandEnvelope envelope, ScriptedPermit permit) => session.SubmitScripted(envelope, permit);
        public CommandReceipt SubmitSyntheticEvaluation(CommandEnvelope envelope, ActorRole role) => session.SubmitSyntheticEvaluation(envelope, role);
        public bool Revoke(Guid attemptId, PassageRef passage) => session.IsCurrentAttempt(attemptId) && session.Revoke(passage);
        public bool DeliverAssistance(Guid attemptId, Guid deliveryId, string opportunityId, string text, bool strategic, bool requested, bool debrief = false, bool inputBusy = false)
            => session.IsCurrentAttempt(attemptId) && session.DeliverAssistance(deliveryId, opportunityId, text, strategic, requested, debrief, inputBusy);
        public void MarkAssistanceHistoryUnavailable(Guid attemptId)
        { if (session.IsCurrentAttempt(attemptId)) session.MarkAssistanceHistoryUnavailable(); }
    }
}
