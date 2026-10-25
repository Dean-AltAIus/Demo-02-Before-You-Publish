using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;

namespace Altaius.Demo02.Integration
{
    public enum ReplyState { Sending, Ready, Shown, Cancelled, Unavailable, Failed, Stale }
    public enum HelpIntent { Controls, RecordedAttempt, RequestedStrategy }
    public sealed class ConversationRequest
    {
        public Guid Id { get; }
        public Guid AttemptId { get; }
        public ActorRole Recipient { get; }
        public string OwnText { get; }
        public int WorldRevision { get; }
        public DocumentPair Pair { get; }
        public IReadOnlyList<PassageRef> AdmittedSources { get; }
        public ConversationRequest(Guid id, SessionSnapshot snapshot, ActorRole recipient, string ownText)
        { Id=id; AttemptId=snapshot.AttemptId; Recipient=recipient; OwnText=ownText; WorldRevision=snapshot.WorldRevision; Pair=snapshot.Pair; AdmittedSources=snapshot.Notebook.Select(p=>p.Reference).ToArray(); }
    }
    public sealed class ConversationReply
    {
        public Guid RequestId { get; }
        public Guid AttemptId { get; }
        public ActorRole Recipient { get; }
        public DocumentPair Pair { get; }
        public string Text { get; }
        public bool Available { get; }
        public IReadOnlyList<PassageRef> Sources { get; }
        public string Provenance => "Authored offline reply. No language interpretation or semantic evaluation.";
        public ConversationReply(ConversationRequest request, string text, bool available, IEnumerable<PassageRef> sources=null)
        { RequestId=request.Id; AttemptId=request.AttemptId; Recipient=request.Recipient; Pair=request.Pair; Text=text; Available=available; Sources=(sources??Enumerable.Empty<PassageRef>()).ToArray(); }
    }
    public sealed class CancelRequest
    { public Guid AttemptId { get; } public Guid RequestId { get; } public CancelRequest(Guid attemptId,Guid requestId){AttemptId=attemptId;RequestId=requestId;} }
    public sealed class DisplayAcknowledgement
    { public Guid AttemptId { get; } public Guid RequestId { get; } public Guid DeliveryId { get; } public DisplayAcknowledgement(Guid attemptId,Guid requestId,Guid deliveryId){AttemptId=attemptId;RequestId=requestId;DeliveryId=deliveryId;} }
    public interface IOfflineConversationPort { ConversationReply Compose(ConversationRequest request, SessionSnapshot current); }
    public interface IConversationHistoryPort { IReadOnlyList<ConversationRequest> Requests { get; } }
}
