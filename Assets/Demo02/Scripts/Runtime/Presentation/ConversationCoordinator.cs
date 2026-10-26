using E=Altaius.Demo02.Domain.EnglishCopy;
using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
namespace Altaius.Demo02.Presentation
{
    public sealed class ConversationRecord
    {
        public ConversationRequest Request { get; internal set; }
        public ConversationReply Reply { get; internal set; }
        public ReplyState State { get; internal set; }
        public Guid? DisplayId { get; internal set; }
        public string Error { get; internal set; }
    }
    public sealed class ConversationCoordinator : IConversationHistoryPort
    {
        readonly OfflineSession session;
        readonly IOfflineConversationPort port;
        readonly List<ConversationRecord> history=new List<ConversationRecord>();
        readonly List<ConversationRecord> linkedHistory=new List<ConversationRecord>();
        readonly Dictionary<ActorRole,string> drafts=new Dictionary<ActorRole,string>();
        public ConversationCoordinator(OfflineSession session,IOfflineConversationPort port=null)
        {this.session=session??throw new ArgumentNullException(nameof(session));this.port=port??new SourceBoundedOfflineReplies();}
        public IReadOnlyList<ConversationRecord> History => history.AsReadOnly();
        public IReadOnlyList<ConversationRecord> LinkedHistory => linkedHistory.AsReadOnly();
        public IReadOnlyList<ConversationRequest> Requests => history.Select(r=>r.Request).ToArray();
        public string Draft(ActorRole role) => drafts.TryGetValue(role,out var value)?value:"";
        public void SetDraft(ActorRole role,string value) { RequireRole(role);drafts[role]=value??""; }
        public ConversationRecord Send(ActorRole role,string ownText)
        {
            RequireRole(role);
            if(session.Snapshot.Mode==SessionMode.Replay)throw new InvalidOperationException(E.Get("conversationcoordinator.setdraft.setdraft.message.replay.is.read.only","Replay is read-only."));
            if(string.IsNullOrWhiteSpace(ownText))throw new InvalidOperationException(E.Get("conversationcoordinator.setdraft.setdraft.message.write.a.question.before.sending","Write a question before sending."));
            if(history.Any(r=>r.Request.Recipient==role && (r.State==ReplyState.Sending||r.State==ReplyState.Ready)))
                throw new InvalidOperationException(E.Get("conversationcoordinator.setdraft.setdraft.message.reconcile.or.cancel.the.original.request","Reconcile or cancel the original request before sending again."));
            SetDraft(role,ownText);
            var record=new ConversationRecord{Request=new ConversationRequest(Guid.NewGuid(),session.Snapshot,role,ownText),State=ReplyState.Sending};
            history.Add(record);return record;
        }
        public bool Process(Guid requestId)
        {
            var record=history.SingleOrDefault(r=>r.Request.Id==requestId);
            if(record==null||record.State!=ReplyState.Sending)return false;
            try{return Complete(port.Compose(record.Request,session.Snapshot));}
            catch(Exception){record.State=ReplyState.Failed;record.Error=E.Get("conversationcoordinator.process.assign.record.error.the.local.reply.could.not.be","The local reply could not be prepared. Your draft and original request are retained.");return false;}
        }
        public bool Complete(ConversationReply reply)
        {
            if(reply==null)return false;
            var record=history.SingleOrDefault(r=>r.Request.Id==reply.RequestId);
            if(record==null||record.State!=ReplyState.Sending)return false;
            var s=session.Snapshot;var request=record.Request;
            if(request.AttemptId!=s.AttemptId||s.Mode==SessionMode.Replay||reply.AttemptId!=request.AttemptId)return false;
            if(reply.Recipient!=request.Recipient||!Same(reply.Pair,request.Pair))return false;
            if(!Same(request.Pair,s.Pair)||s.WorldRevision!=request.WorldRevision||
                reply.Sources.Any(r=>!request.AdmittedSources.Any(a=>Same(a,r))||!s.Notebook.Any(p=>Same(p.Reference,r))))
            {record.State=ReplyState.Stale;record.Error=E.Get("conversationcoordinator.complete.assign.record.error.the.version.or.admitted.sources.changed","The version or admitted sources changed. The original question is preserved; inspect the current pair before a fresh request.");return false;}
            record.Reply=reply;record.State=ReplyState.Ready;return true;
        }
        public bool Acknowledge(DisplayAcknowledgement acknowledgement)
        {
            if(acknowledgement==null||acknowledgement.DeliveryId==Guid.Empty||acknowledgement.AttemptId!=session.Snapshot.AttemptId||session.Snapshot.Mode==SessionMode.Replay)return false;
            var record=history.SingleOrDefault(r=>r.Request.Id==acknowledgement.RequestId);
            if(record==null||record.Request.AttemptId!=acknowledgement.AttemptId)return false;
            if(record.DisplayId!=null)return record.DisplayId==acknowledgement.DeliveryId;
            var snapshot=session.Snapshot;
            if(record.State!=ReplyState.Ready)return false;
            if(!Same(record.Request.Pair,snapshot.Pair)||record.Request.WorldRevision!=snapshot.WorldRevision||record.Reply.Sources.Any(r=>!snapshot.Notebook.Any(p=>Same(p.Reference,r))))
            {record.State=ReplyState.Stale;record.Error=E.Get("conversationcoordinator.acknowledge.assign.record.error.the.prepared.reply.is.historical.because","The prepared reply is historical because its version or source scope changed before display.");return false;}
            record.DisplayId=acknowledgement.DeliveryId;record.State=record.Reply.Available?ReplyState.Shown:ReplyState.Unavailable;return true;
        }
        public bool Cancel(CancelRequest request)
        {
            if(request==null||request.AttemptId!=session.Snapshot.AttemptId)return false;
            var record=history.SingleOrDefault(r=>r.Request.Id==request.RequestId&&r.Request.AttemptId==request.AttemptId);
            if(record==null||record.State!=ReplyState.Sending&&record.State!=ReplyState.Ready)return false;
            record.State=ReplyState.Cancelled;return true;
        }
        public void Reset(bool linkedPractice=false)
        {if(linkedPractice)linkedHistory.AddRange(history);else linkedHistory.Clear();foreach(var r in history.Where(r=>r.State==ReplyState.Sending||r.State==ReplyState.Ready))r.State=ReplyState.Cancelled;history.Clear();drafts.Clear();}
        static void RequireRole(ActorRole role)
        {if(role!=ActorRole.Colleague&&role!=ActorRole.ModuleLead&&role!=ActorRole.ExperimentReviewer)throw new InvalidOperationException(E.Get("conversationcoordinator.requirerole.requirerole.message.choose.a.conversation.role.not.an","Choose a conversation role, not an authority or evaluator identity."));}
        internal static bool Same(DocumentPair a,DocumentPair b)=>a!=null&&b!=null&&a.TaskVersion==b.TaskVersion&&a.RubricVersion==b.RubricVersion&&a.TaskDigest==b.TaskDigest&&a.RubricDigest==b.RubricDigest;
        internal static bool Same(PassageRef a,PassageRef b)=>a!=null&&b!=null&&a.SourceId==b.SourceId&&a.PassageId==b.PassageId&&a.Version==b.Version&&a.Sha256==b.Sha256;
    }
    /// <summary>Exact authored question lookup only. No parser, keyword grader, intent mutation or hidden-source admission.</summary>
    public sealed class SourceBoundedOfflineReplies : IOfflineConversationPort
    {
        public ConversationReply Compose(ConversationRequest request,SessionSnapshot current)
        {
            string question=request.OwnText.Trim().TrimEnd('?','.').ToLowerInvariant();string answer;bool available=true;
            if(question=="what is your role") answer=((request.Recipient==ActorRole.Colleague)?E.Get("conversationcoordinator.compose.assign.answer.i.brought.the.publication.request.i.brought.the.publication.request.i","I brought the publication request. I can discuss the practical context, but I cannot approve the task or rubric."):((request.Recipient==ActorRole.ModuleLead)?E.Get("conversationcoordinator.compose.assign.answer.i.own.exact.pair.publication.i.own.exact.pair.publication.i.own.exact.pair.publication.review","I own exact-pair publication review. A scheduled slot is separate from my decision. This local adapter cannot semantically approve arbitrary wording."):E.Get("conversationcoordinator.compose.assign.answer.i.own.exact.pair.publication.i.examine.recorded.work.and.i.examine.recorded.work.and.criteria","I examine recorded work and criteria. My findings are separate from publication approval, and arbitrary free-text evaluation is unavailable offline.")));
            else if(question=="what sources have i inspected")
                answer=((request.AdmittedSources.Count==0)?E.Get("conversationcoordinator.compose.assign.answer.no.source.inspection.is.recorded.no.source.inspection.is.recorded.in","No source inspection is recorded in this attempt."):E.Format("conversationcoordinator.compose.assign.answer.the.notebook.records.v.opening.the.notebook.records.opening.a.source","The notebook records: {value1}. Opening a source does not prove understanding.","value1",string.Join(", ",request.AdmittedSources.Select(p=>E.Format("conversationcoordinator.compose.assign.answer.the.notebook.records.v.opening.value1.item.v","{p_sourceid} v{p_version}","p_sourceid",p.SourceId,"p_version",p.Version)))));
            else if(question=="what time is publication due")answer=((request.Recipient==ActorRole.Colleague)?E.Get("conversationcoordinator.compose.assign.answer.the.request.is.to.publish.the.request.is.to.publish.before","The request is to publish before 14:00. There is no automatic time penalty while you read, type or think."):E.Get("conversationcoordinator.compose.assign.answer.publication.is.due.at.14.publication.is.due.at.14.00","Publication is due at 14:00. Only an accepted scheduling response changes the logical clock."));
            else if(question=="is the current pair approved")answer=((current.CurrentApproval==null)?E.Get("conversationcoordinator.compose.assign.answer.no.current.exact.pair.approval.no.current.exact.pair.approval.is","No current exact-pair approval is recorded. A request, a conversation or a total of 20 cannot supply that receipt."):E.Format("conversationcoordinator.compose.assign.answer.a.current.exact.pair.approval.a.current.exact.pair.approval.is","A current exact-pair approval is recorded: {current_currentapproval_receiptid}. Inspect its scope before requesting release.","current_currentapproval_receiptid",current.CurrentApproval.ReceiptId));
            else{available=false;answer=((request.Recipient==ActorRole.Colleague)?E.Get("conversationcoordinator.compose.assign.answer.i.cannot.interpret.that.question.i.cannot.interpret.that.question.in","I cannot interpret that question in this authored offline conversation. Your words are preserved. I have made no change or commitment."):((request.Recipient==ActorRole.ModuleLead)?E.Get("conversationcoordinator.compose.assign.answer.a.grounded.response.to.those.a.grounded.response.to.those.a.grounded.response.to.those.words","A grounded response to those words is unavailable offline. No review decision or approval has been created. Use the exact-pair review controls for a scoped request."):E.Get("conversationcoordinator.compose.assign.answer.a.grounded.response.to.those.i.cannot.verify.that.reasoning.i.cannot.verify.that.reasoning.with","I cannot verify that reasoning with this offline adapter. Your marks remain your hypothesis; there is no semantic finding from this reply.")));}
            return new ConversationReply(request,answer,available,question=="what sources have i inspected"?request.AdmittedSources:null);
        }
    }
}
