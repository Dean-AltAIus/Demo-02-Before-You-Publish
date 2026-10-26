using System;
using System.Linq;
using Altaius.Demo02.Domain;
using E=Altaius.Demo02.Domain.EnglishCopy;
namespace Altaius.Demo02.Presentation
{
    /// <summary>Immutable context from the actual staged command, never inferred from a later current pair.</summary>
    public sealed class ReceiptContext
    {
        public Guid RequestId {get;} public DomainCommand Command {get;} public DocumentPair Pair {get;}
        public ReceiptContext(CommandEnvelope envelope,SessionSnapshot staged)
        {if(envelope==null)throw new ArgumentNullException(nameof(envelope));RequestId=envelope.CommandId;Command=envelope.Command;Pair=staged.Pair;}
        public ReceiptContext(Guid requestId,DomainCommand command,DocumentPair pair)
        {RequestId=requestId;Command=command;Pair=pair;}
    }
    public sealed class ReceiptWords
    {
        public string Heading {get;}public string Body {get;}
        public ReceiptWords(string heading,string body){Heading=heading;Body=body;}
    }
    public static class ReceiptCopy
    {
        public static string ScopeDetails(CommandReceipt receipt)
        {
            var scope=receipt.Scope;var e=scope?.Envelope;
            if(e==null)return E.Get("pair.unavailable","Exact pair unavailable");
            string unavailable=E.Get("pair.unavailable","Exact pair unavailable");
            string commandLink=e.Command is RequestRelease release?release.ApprovalReceiptId.ToString():e.Command is ScheduleReply schedule?schedule.RequestId.ToString():e.Command is DecideReview decision?decision.RequestId.ToString():e.Command is AcceptHold hold?hold.RequestId.ToString():"";
            return E.Format("history.scope.details","Attempt {attempt}\nOriginal {pair}\nTask SHA-256 {task}\nRubric SHA-256 {rubric}\nPayload SHA-256 {payload}\nExpected revision {expected} | Recorded revision {before} to {after}\nContract {rule} | Fixture SHA-256 {fixture}\nLinked approval/request {link}\nCausation {cause} | Opportunity {opportunity}\nReadset\n{readset}\nSource pins\n{sources}",
                "attempt",e.AttemptId,"pair",Pair(scope.Pair),"task",scope.Pair?.TaskDigest??unavailable,"rubric",scope.Pair?.RubricDigest??unavailable,"payload",receipt.PayloadDigest,
                "expected",e.ExpectedWorldRevision,"before",receipt.BeforeRevision,"after",receipt.AfterRevision,"rule",e.ScenarioVersion,"fixture",e.FixtureSha256,"link",commandLink,"cause",e.CausationId,"opportunity",e.OpportunityId,
                "readset",string.Join("\n",e.Readset.Select(p=>p.Key+" = "+p.Value)),"sources",string.Join("\n",e.SourceRefs.Where(p=>p!=null).Select(p=>p.SourceId+" / "+p.Version+" / "+p.PassageId+" / "+p.Sha256)));
        }
        public static string Pair(DocumentPair pair)=>pair==null?E.Get("pair.unavailable","Exact pair unavailable"):E.Format("pair.label","Task {task} / Rubric {rubric}","task",pair.TaskVersion,"rubric",pair.RubricVersion);
        public static ReceiptWords Describe(CommandReceipt receipt,ReceiptContext context,SessionSnapshot current)
        {
            if(context==null||receipt.RequestId!=context.RequestId)throw new InvalidOperationException("Receipt context does not match the original request.");
            string pair=Pair(context.Pair);string heading,body;
            if(receipt.Status!=ReceiptStatus.Accepted)
            {
                heading=E.Get("receipt.attention","Request needs attention");
                switch(receipt.ReasonCode)
                {
                    case "ReviewSlotOutsideWindow":body=E.Get("receipt.review.time.window","Choose a review time from 13:40 to 14:00, no earlier than the current logical time. Your draft is retained.");break;
                    case "ChangeSummaryRequired":body=E.Get("receipt.review.summary.required","Add your change summary before requesting review. Your draft is retained.");break;
                    case "HoldMustBeFuture":body=E.Get("receipt.hold.time.future","Choose a hold review time later than the current logical time. Your draft is retained.");break;
                    case "ExactCurrentApprovalRequired":
                        var approval=current.ApprovalHistory.LastOrDefault();
                        body=approval==null?E.Format("receipt.release.no.approval","Release was not accepted for {pair}. No exact current approval is recorded.","pair",pair):E.Format("receipt.release.different.approval","Release was not accepted for {pair}. The recorded approval covers {approved_pair}. Obtain a separate review of the exact current pair before release.","pair",pair,"approved_pair",Pair(approval.Pair));break;
                    case "DocumentPairMismatch":body=E.Format("receipt.pair.mismatch","This request refers to a different task or rubric version. The original request is for {pair}. Your draft is retained.","pair",pair);break;
                    default:body=E.Format("receipt.rejected","The request for {pair} was not accepted. Your draft and original request are retained. Inspect the exact receipt details before creating a fresh preview.","pair",pair);break;
                }
                if(receipt.Status==ReceiptStatus.Unknown||receipt.Status==ReceiptStatus.Pending)
                {heading=E.Get("receipt.unresolved","Request outcome unresolved");body=E.Format("receipt.unresolved.body","The outcome for {pair} is not confirmed. Keep the original request and inspect its details before retrying.","pair",pair);}
                return new ReceiptWords(heading,body);
            }
            var command=context.Command;
            if(command is RequestReview)
                return new ReceiptWords(E.Get("receipt.review.request.heading","Review requested"),E.Format("receipt.review.request.body","Your review request for {pair} was recorded. A time slot and a review decision are separate responses. No approval has been granted.","pair",pair));
            if(command is ScheduleReply schedule)
            {
                if(schedule.Decision==ScheduleDecision.Accepted)return new ReceiptWords(E.Get("receipt.schedule.accepted.heading","Review time accepted"),E.Format("receipt.schedule.accepted.body","The review of {pair} is scheduled for {time}. Scheduling grants no publication approval.","pair",pair,"time",schedule.ReviewBy));
                if(schedule.Decision==ScheduleDecision.Denied)return new ReceiptWords(E.Get("receipt.schedule.denied.heading","Review time declined"),E.Format("receipt.schedule.denied.body","The requested slot for {pair} was declined. No time slot or approval was recorded.","pair",pair));
                return new ReceiptWords(E.Get("receipt.schedule.unavailable.heading","Review time unavailable"),E.Format("receipt.schedule.unavailable.body","A review slot for {pair} was unavailable. No time slot or approval was recorded.","pair",pair));
            }
            if(command is DecideReview decision)
                return new ReceiptWords(decision.Decision==ReviewDecision.Approved?E.Get("receipt.review.approved.heading","Requested pair approved"):E.Get("receipt.review.returned.heading","Requested pair returned"),E.Format("receipt.review.decision.body","This decision applies to {pair}.\n{reason}","pair",pair,"reason",decision.Reason));
            if(command is RequestRelease)return new ReceiptWords(E.Get("receipt.release.heading","Release recorded"),E.Format("receipt.release.body","Publication was accepted for the exact approved {pair}. This release is retained in the read-only history.","pair",pair));
            if(command is RequestHold)return new ReceiptWords(E.Get("receipt.hold.request.heading","Hold proposed"),E.Format("receipt.hold.request.body","The hold proposal for {pair} was recorded. Owner acceptance is still separate. Publication has not been approved.","pair",pair));
            if(command is AcceptHold hold)return new ReceiptWords(hold.ConfirmedSelfCommitment?E.Get("receipt.hold.self.heading","Your commitment recorded"):E.Get("receipt.hold.owner.heading","Owner response recorded"),E.Format("receipt.hold.accept.body","The response concerns {pair}. Inspect the actual hold state and its owner, time and next action. A hold is not publication approval.","pair",pair));
            return new ReceiptWords(E.Get("receipt.accepted.heading","Request recorded"),E.Format("receipt.accepted.body","The operational request for {pair} was accepted. No additional semantic or competence claim is implied.","pair",pair));
        }
        public static string ReviewBody(SessionSnapshot s)
        {
            if(s.Publication==PublicationStatus.Released)return E.Get("review.released.body","Release is recorded for this exact current pair. Its approval and release receipts remain in the read-only history.");
            if(s.Publication==PublicationStatus.Held)return E.Get("review.held.body","The work is held with an accepted owner, review time and next action. The defect remains unresolved. Publication is closed.");
            if(s.Publication==PublicationStatus.HoldCommittedUnverified)return E.Get("review.self.hold.body","Your self-owned commitment is recorded. The stated defect remains unverified. This is not an NPC acceptance or publication approval.");
            if(s.Publication==PublicationStatus.HoldRequested)return E.Get("review.hold.requested.body","A hold has been proposed. Inspect the actual owner response or confirm your own commitment. No acceptance or publication approval is inferred.");
            if(s.ReviewStatus==ReviewStatus.Stale)
            {var earlier=s.ApprovalHistory.LastOrDefault();return earlier==null?E.Get("review.stale.body","The current documents need a new exact-pair review. Earlier review records remain in history."):E.Format("review.stale.approval.body","The approval covers {approved_pair}. The current {current_pair} requires a separate review before release.","approved_pair",Pair(earlier.Pair),"current_pair",Pair(s.Pair));}
            switch(s.ReviewStatus)
            {
                case ReviewStatus.Approved:return E.Get("review.approved.body","This exact current pair has an approval. Preview release to submit the separate publication request.");
                case ReviewStatus.Pending:return E.Get("review.pending.body","The current pair has been requested for review. A slot is not yet accepted and no approval is recorded.");
                case ReviewStatus.Scheduled:return E.Get("review.scheduled.body","A review time is accepted for the current pair. The review decision is separate. No publication approval is recorded.");
                case ReviewStatus.Returned:return E.Get("review.returned.body","The requested pair was returned. Read the actual reason, revise the documents and request a review of the new exact pair.");
                case ReviewStatus.Denied:return E.Get("review.denied.body","The review request was declined. No time slot or approval was recorded.");
                case ReviewStatus.Unavailable:return E.Get("review.unavailable.body","A review response is unavailable. The original request is retained. No approval is inferred.");
                case ReviewStatus.Unknown:return E.Get("review.unknown.body","The review outcome is not confirmed. Inspect the original request and receipt before retrying.");
                default:return E.Get("review.unrequested.body","Request a review of the exact current task and rubric. Scheduling, review decisions and release are separate actions.");
            }
        }
    }
}
