using E=Altaius.Demo02.Domain.EnglishCopy;
using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;
namespace Altaius.Demo02.Presentation
{
    public sealed class AuthorityReply
    {
        public Guid Id { get; }=Guid.NewGuid();public Guid AttemptId { get; internal set; }
        public Guid OriginalRequestId { get; internal set; }public string Text { get; internal set; }
        public string Status { get; internal set; }
        public string Provenance => Envelope?.Command is DecideReview
            ? (IsSynthetic ? E.Get("authority.synthetic.decision.disclosure","Authored offline demonstration. This decision covers only the exact synthetic pair. No live service is connected.") : E.Get("authority.local.decision.disclosure","Authored offline decision for the exact requested pair. No live service is connected."))
            : E.Get("localauthority.initialization.return.authored.offline.role.response.no.live","Authored offline role response. No live service or semantic approval.");
        public CommandReceipt Receipt { get; internal set; }public bool Displayed { get; internal set; }
        public ActorRole AuthorityRole=>Role;
        public DomainCommand RequestedCommand=>Envelope?.Command;
        public DocumentPair RequestedPair {get;internal set;}
        internal CommandEnvelope Envelope;internal ActorRole Role;internal bool IsReviewUnavailable,IsSynthetic;
    }
    /// <summary>Composition service. Learner controls request work; they cannot select or mint the role's decision.</summary>
    public sealed class LocalAuthority
    {
        public const string PublicationOpportunity="D02.publication.current-pair";
        readonly OfflineSession session;readonly ScenarioController controller;readonly List<AuthorityReply> replies=new List<AuthorityReply>();
        readonly List<AuthorityReply> linkedReplies=new List<AuthorityReply>();
        public IReadOnlyList<AuthorityReply> Replies=>replies.AsReadOnly();
        public IReadOnlyList<AuthorityReply> LinkedReplies=>linkedReplies.AsReadOnly();
        public LocalAuthority(OfflineSession session,ScenarioController controller){this.session=session;this.controller=controller;}
        public CommandEnvelope StageReview(string ownSummary,string time)
        {var s=session.Snapshot;return session.Stage(new RequestReview(s.Task.Version,s.Rubric.Version,ownSummary,time),PublicationOpportunity);}
        public CommandEnvelope StageHold(string defect,ActorRole owner,string time,string nextAction,HoldDefectBasis basis)
        {
            var s=session.Snapshot;var anchors=s.Notebook.Where(p=>p.Reference.SourceId.StartsWith("D02.task.v"+s.Task.Version+".",StringComparison.Ordinal)||p.Reference.SourceId.StartsWith("D02.rubric.v"+s.Rubric.Version+".",StringComparison.Ordinal)).Select(p=>p.Reference).ToArray();
            return session.Stage(new RequestHold(defect,owner,time,nextAction,basis,basis==HoldDefectBasis.Unverified?null:anchors),PublicationOpportunity);
        }
        public CommandEnvelope StageRelease()
        {var s=session.Snapshot;return session.Stage(new RequestRelease(s.Task.Version,s.Rubric.Version,s.CurrentApproval?.ReceiptId??Guid.Empty),PublicationOpportunity);}
        public CommandEnvelope StageSelfAcceptance(Guid requestId)
        {
            var s=session.Snapshot;var hold=s.HoldHistory.LastOrDefault(h=>h.Id==requestId);
            if(hold==null||hold.Proposal.Owner!=ActorRole.Learner)throw new InvalidOperationException(E.Get("localauthority.stageselfacceptance.stageselfacceptance.message.a.current.self.owned.proposal.is","A current self-owned proposal is required."));
            return session.Stage(new AcceptHold(requestId,s.Task.Version,s.Rubric.Version,hold.Proposal.ReviewAt,true,true),PublicationOpportunity);
        }
        public CommandReceipt Confirm(CommandEnvelope originalEnvelope)=>session.Submit(originalEnvelope);
        public AuthorityReply PrepareScheduling(Guid requestId)
        {
            var existing=replies.LastOrDefault(r=>r.OriginalRequestId==requestId&&r.Envelope?.Command is ScheduleReply);
            if(existing!=null)return existing;
            var s=session.Snapshot;var request=s.ReviewHistory.LastOrDefault(r=>r.Id==requestId);
            if(request==null||!ConversationCoordinator.Same(request.Pair,s.Pair)||request.Status!=ReviewStatus.Pending)
                return Unavailable(requestId,E.Get("localauthority.preparescheduling.requestid.body.scheduling.cannot.be.reconciled.for.this","Scheduling cannot be reconciled for this current request. Inspect the original receipt and exact pair."),E.Get("localauthority.preparescheduling.requestid.status.unknown","Unknown"));
            var envelope=session.Stage(new ScheduleReply(request.Id,s.Task.Version,s.Rubric.Version,Time(request.Minute),ScheduleDecision.Accepted,Guid.NewGuid()),PublicationOpportunity);
            return Add(requestId,E.Format("localauthority.preparescheduling.requestid.body.the.local.module.lead.accepted.the","The local module lead accepted the requested {time_request_minute} slot for Task {s_task_version} / Rubric {s_rubric_version}. This is scheduling only. No publication approval is recorded.","time_request_minute",Time(request.Minute),"s_task_version",s.Task.Version,"s_rubric_version",s.Rubric.Version),E.Get("localauthority.preparescheduling.requestid.status.scheduling.response.ready","Scheduling response ready"),envelope,ActorRole.ModuleLead);
        }
        public AuthorityReply PrepareReviewDecision(Guid requestId)
        {
            var existing=replies.LastOrDefault(r=>r.OriginalRequestId==requestId&&(r.Envelope?.Command is DecideReview||r.IsReviewUnavailable));
            if(existing!=null)return existing;
            var s=session.Snapshot;var request=s.ReviewHistory.LastOrDefault(r=>r.Id==requestId);
            if(request==null||!ConversationCoordinator.Same(request.Pair,s.Pair)||request.Status!=ReviewStatus.Scheduled)
                return ReviewUnavailable(requestId,E.Get("localauthority.preparereviewdecision.requestid.body.a.current.accepted.schedule.is.required","A current accepted schedule is required before a separate review decision."),E.Get("localauthority.preparereviewdecision.requestid.status.review.unavailable","Review unavailable"));
            string defect=StructuralProblem(s);
            if(defect==null)return ReviewUnavailable(requestId,E.Get("localauthority.preparereviewdecision.requestid.body.a.semantic.review.of.your.own","A semantic review of your own task, criteria and rationale is unavailable in this offline adapter. The pair remains scheduled and unapproved. No approval is inferred from arithmetic or polished wording."),E.Get("localauthority.preparereviewdecision.requestid.status.review.unavailable","Review unavailable"));
            string reason=E.Format("localauthority.preparereviewdecision.assign.string.reason.returned.after.a.structural.check.no","Returned after a structural check: {defect} No semantic alignment judgement has been made.","defect",defect);
            return Add(requestId,reason,E.Get("localauthority.preparereviewdecision.requestid.status.returned.response.ready","Returned response ready"),session.Stage(new DecideReview(requestId,s.Task.Version,s.Rubric.Version,ReviewDecision.Returned,reason),PublicationOpportunity),ActorRole.ModuleLead);
        }
        public AuthorityReply PrepareOwnerAcceptance(Guid requestId)
        {
            var existing=replies.LastOrDefault(r=>r.OriginalRequestId==requestId);if(existing!=null)return existing;
            var s=session.Snapshot;var hold=s.HoldHistory.LastOrDefault(h=>h.Id==requestId);
            if(hold==null||!ConversationCoordinator.Same(hold.Pair,s.Pair)||hold.Accepted!=null)return Unavailable(requestId,E.Get("localauthority.prepareowneracceptance.requestid.body.this.hold.proposal.has.no.pending","This hold proposal has no pending current owner response."),E.Get("localauthority.preparescheduling.requestid.status.unknown","Unknown"));
            if(hold.Proposal.Owner==ActorRole.Learner)return Unavailable(requestId,E.Get("localauthority.prepareowneracceptance.requestid.body.self.ownership.needs.your.explicit.confirmation","Self-ownership needs your explicit confirmation. This is not an NPC acceptance."),E.Get("localauthority.prepareowneracceptance.requestid.status.self.confirmation.required","Self confirmation required"));
            if(hold.Proposal.Basis==HoldDefectBasis.Unverified||hold.Proposal.Basis==HoldDefectBasis.ReviewedInterpretation)
                return Unavailable(requestId,E.Get("localauthority.prepareowneracceptance.requestid.body.the.proposed.owner.cannot.verify.arbitrary","The proposed owner cannot verify arbitrary defect prose with this offline adapter. The proposal is retained; no acceptance has been recorded."),E.Get("localauthority.prepareowneracceptance.requestid.status.owner.response.unavailable","Owner response unavailable"),hold.Proposal.Owner);
            return Add(requestId,E.Format("localauthority.prepareowneracceptance.requestid.body.review.at.next.action.this.holds","{value0}Review at {hold_proposal_reviewat}. Next action: {hold_proposal_nextaction}. This holds the work unresolved; it is not publication approval.","value0",((hold.Proposal.Owner==ActorRole.Colleague)?E.Get("localauthority.prepareowneracceptance.requestid.body.value0.i.can.own.that.concrete.i.can.own.that.concrete.follow","I can own that concrete follow-up. "):E.Get("localauthority.prepareowneracceptance.requestid.body.value0.i.accept.ownership.of.this.i.accept.ownership.of.this.structurally","I accept ownership of this structurally verified defect. ")),"hold_proposal_reviewat",hold.Proposal.ReviewAt,"hold_proposal_nextaction",hold.Proposal.NextAction),E.Get("localauthority.prepareowneracceptance.requestid.status.owner.acceptance.ready","Owner acceptance ready"),session.Stage(new AcceptHold(requestId,s.Task.Version,s.Rubric.Version,hold.Proposal.ReviewAt,true),PublicationOpportunity),hold.Proposal.Owner);
        }
        public bool Acknowledge(AuthorityReply reply)
        {
            if(reply==null||!replies.Contains(reply)||reply.AttemptId!=session.Snapshot.AttemptId||session.Snapshot.Mode==SessionMode.Replay)return false;
            if(reply.Displayed)return true;
            reply.Displayed=true;
            if(reply.Envelope==null)return true;
            // The role reply has now been rendered. Only this composition service owns the permit.
            var permit=controller.PermitExactScriptedResponse(reply.Envelope,reply.Role,"Actual rendered local response "+reply.Id+" for original request "+reply.OriginalRequestId);
            reply.Receipt=controller.SubmitScripted(reply.Envelope,permit);
            reply.Status=((reply.Receipt.Status==ReceiptStatus.Accepted)?((reply.Envelope.Command is ScheduleReply schedule)?((schedule.Decision==ScheduleDecision.Accepted)?E.Get("localauthority.acknowledge.assign.reply.status.scheduled.owner.accepted.scheduled.scheduled.scheduled","Scheduled"):schedule.Decision.ToString()):((reply.Envelope.Command is DecideReview decision)?decision.Decision.ToString():E.Get("localauthority.acknowledge.assign.reply.status.scheduled.owner.accepted.owner.accepted.owner.accepted.owner.accepted","Owner accepted"))):((reply.Receipt.Status)+(": ")+(reply.Receipt.ReasonCode)));
            return reply.Receipt.Status==ReceiptStatus.Accepted;
        }
        // Task 4 receives an exact authored scope from its isolated synthetic driver. Ordinary UI never calls this port.
        public AuthorityReply PreparePresenterDecision(PresenterDecisionScope scope,ReviewDecision decision,string authoredReason)
        {
            var s=session.Snapshot;
            if(scope==null||s.Mode!=SessionMode.Presenter||scope.AttemptId!=s.AttemptId||scope.TaskVersion!=s.Task.Version||scope.RubricVersion!=s.Rubric.Version||scope.ExpectedTaskDigest!=s.Task.Digest||scope.ExpectedRubricDigest!=s.Rubric.Digest||string.IsNullOrWhiteSpace(scope.ScenarioId)||string.IsNullOrWhiteSpace(scope.ScenarioDigest)||scope.ScenarioDigest.Length!=64||
                !s.ReviewHistory.Any(r=>r.Id==scope.RequestId&&r.Status==ReviewStatus.Scheduled&&ConversationCoordinator.Same(r.Pair,s.Pair)))
                throw new InvalidOperationException(E.Get("localauthority.preparepresenterdecision.preparepresenterdecision.message.exact.isolated.authored.presenter.scope.required","Exact isolated authored presenter scope required."));
            return Add(scope.RequestId,E.Format("localauthority.preparepresenterdecision.scope.requestid.body.synthetic.authored.scenario","Synthetic authored scenario: {authoredreason}","authoredreason",authoredReason),E.Format("localauthority.preparepresenterdecision.scope.requestid.status.authored.response.ready","{decision} authored response ready","decision",decision),session.Stage(new DecideReview(scope.RequestId,s.Task.Version,s.Rubric.Version,decision,"Synthetic authored scenario "+scope.ScenarioId+": "+authoredReason),PublicationOpportunity),ActorRole.ModuleLead);
        }
        AuthorityReply ReviewUnavailable(Guid request,string text,string status){var reply=Unavailable(request,text,status);reply.IsReviewUnavailable=true;return reply;}
        AuthorityReply Unavailable(Guid request,string text,string status,ActorRole role=ActorRole.ModuleLead)=>Add(request,text,status,null,role);
        AuthorityReply Add(Guid request,string text,string status,CommandEnvelope envelope,ActorRole role)
        {var s=session.Snapshot;var pair=s.ReviewHistory.LastOrDefault(x=>x.Id==request)?.Pair??s.HoldHistory.LastOrDefault(x=>x.Id==request)?.Pair??s.Pair;var r=new AuthorityReply{AttemptId=s.AttemptId,OriginalRequestId=request,Text=text,Status=status,Envelope=envelope,Role=role,RequestedPair=pair,IsSynthetic=s.Mode==SessionMode.Presenter};replies.Add(r);return r;}
        public void Reset(bool linkedPractice=false){if(linkedPractice)linkedReplies.AddRange(replies);else linkedReplies.Clear();replies.Clear();}
        public static string Time(int minute)=>(minute/60).ToString("00")+":"+(minute%60).ToString("00");
        public static string StructuralProblem(SessionSnapshot s)
        {if(s.Task.Sources.Count!=2)return E.Get("localauthority.structuralproblem.return.the.task.does.not.attach.both","The task does not attach both source passages.");if(string.IsNullOrWhiteSpace(s.Task.DecisionContext))return E.Get("localauthority.structuralproblem.return.the.task.has.no.decision.context","The task has no decision context.");if(s.Task.Questions.Any(q=>s.Rubric.Criteria.Where(c=>c.QuestionId==q.Id).Sum(c=>c.Marks)!=q.Marks))return E.Get("localauthority.structuralproblem.return.rubric.marks.do.not.reconcile.for","Rubric marks do not reconcile for each question.");return null;}
    }
    public sealed class PresenterDecisionScope
    {
        public Guid AttemptId { get; }public Guid RequestId { get; }public int TaskVersion { get; }public int RubricVersion { get; }
        public string ExpectedTaskDigest { get; }public string ExpectedRubricDigest { get; }
        public string ScenarioId { get; }public string ScenarioDigest { get; }
        public PresenterDecisionScope(Guid attemptId,Guid requestId,int taskVersion,int rubricVersion,string exactAuthoredTaskDigest,string exactAuthoredRubricDigest,string exactAuthoredScenarioId,string scenarioDigest)
        {AttemptId=attemptId;RequestId=requestId;TaskVersion=taskVersion;RubricVersion=rubricVersion;ExpectedTaskDigest=exactAuthoredTaskDigest;ExpectedRubricDigest=exactAuthoredRubricDigest;ScenarioId=exactAuthoredScenarioId;ScenarioDigest=scenarioDigest;}
    }
}
