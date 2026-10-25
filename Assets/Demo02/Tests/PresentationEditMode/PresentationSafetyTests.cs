using System;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
using Altaius.Demo02.Presentation;
using NUnit.Framework;
namespace Altaius.Demo02.Tests
{
    public sealed class PresentationSafetyTests
    {
        // Catches a missing or fabricated display acknowledgment: requesting is not delivery.
        [Test] public void AdmittedReplyRequiresActualDisplayBeforeItIsShown()
        {
            var session=OfflineSession.Create(); var conversation=new ConversationCoordinator(session);
            var record=conversation.Send(ActorRole.Colleague,"What is your role?");
            Assert.That(record.Request,Is.Not.Null,"A real request must be captured before dispatch.");
            Assert.That(record.State,Is.EqualTo(ReplyState.Sending));
            var reply=new ConversationReply(record.Request,"I can discuss the request; I cannot approve publication.",true);
            Assert.That(conversation.Complete(reply),Is.True);
            Assert.That(record.State,Is.EqualTo(ReplyState.Ready));
            Assert.That(record.DisplayId,Is.Null);
            Assert.That(conversation.Acknowledge(new DisplayAcknowledgement(session.Snapshot.AttemptId,record.Request.Id,Guid.NewGuid())),Is.True);
            Assert.That(record.State,Is.EqualTo(ReplyState.Shown));
        }
        // Catches loss of own words or accidental conversion of cancel into a successful response.
        [Test] public void CancelPreservesOwnDraftAndSuppressesLateReply()
        {
            var session=OfflineSession.Create();var conversation=new ConversationCoordinator(session);
            var record=conversation.Send(ActorRole.ModuleLead,"I have not received approval for this rubric.");
            Assert.That(record.Request,Is.Not.Null);
            var id=record.Request.Id;
            Assert.That(conversation.Cancel(new CancelRequest(session.Snapshot.AttemptId,id)),Is.True);
            Assert.That(conversation.Draft(ActorRole.ModuleLead),Is.EqualTo("I have not received approval for this rubric."));
            Assert.That(conversation.Complete(new ConversationReply(record.Request,"Old reply",true)),Is.False);
            Assert.That(record.State,Is.EqualTo(ReplyState.Cancelled));
            Assert.That(record.Request.Id,Is.EqualTo(id));
        }
        [Test] public void WrongRoleAndUninspectedSourceCannotBeAdmitted()
        {
            var s=OfflineSession.Create();var c=new ConversationCoordinator(s);var r=c.Send(ActorRole.Colleague,"What is your role?");
            var foreign=new ConversationRequest(r.Request.Id,s.Snapshot,ActorRole.ModuleLead,r.Request.OwnText);
            Assert.That(c.Complete(new ConversationReply(foreign,"Approved",true)),Is.False);
            var other=OfflineSession.Create();var source=other.Inspect("S_A",InspectionLocation.Archive);
            Assert.That(c.Complete(new ConversationReply(r.Request,"Protected source",true,new[]{source.Reference})),Is.False);
            Assert.That(s.Snapshot.CurrentApproval,Is.Null);
        }
        [Test] public void ChangedReadsetMakesReplyStaleWithoutDeletingOwnWords()
        {
            var s=OfflineSession.Create();var c=new ConversationCoordinator(s);var r=c.Send(ActorRole.ExperimentReviewer,"The sources seem aligned enough.");
            s.Inspect("S_A",InspectionLocation.Archive);
            Assert.That(c.Complete(new ConversationReply(r.Request,"A response",true)),Is.False);
            Assert.That(r.State,Is.EqualTo(ReplyState.Stale));
            Assert.That(c.Draft(ActorRole.ExperimentReviewer),Is.EqualTo("The sources seem aligned enough."));
        }
        [Test] public void APreparedReplyCannotAcknowledgeAfterItsSourceScopeChanges()
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);var source=s.Inspect("S_A",InspectionLocation.Archive);var c=new ConversationCoordinator(s);
            var r=c.Send(ActorRole.Colleague,"What sources have I inspected?");Assert.That(c.Process(r.Request.Id),Is.True);
            controller.Revoke(s.Snapshot.AttemptId,source.Reference);
            Assert.That(c.Acknowledge(new DisplayAcknowledgement(s.Snapshot.AttemptId,r.Request.Id,Guid.NewGuid())),Is.False);
            Assert.That(r.State,Is.EqualTo(ReplyState.Stale));Assert.That(r.DisplayId,Is.Null);
        }
        [Test] public void ResetSuppressesOldReplyAndClearsPersonalText()
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);var life=new AttemptLifecycle(s,controller);
            var old=life.Conversation.Send(ActorRole.Colleague,"Private temporary text");life.Reset(false);
            Assert.That(life.Conversation.Complete(new ConversationReply(old.Request,"Old reply",true)),Is.False);
            Assert.That(life.Conversation.History,Is.Empty);Assert.That(life.Conversation.Draft(ActorRole.Colleague),Is.Empty);
            Assert.That(life.Conversation.LinkedHistory,Is.Empty);Assert.That(s.Snapshot.Mode,Is.EqualTo(SessionMode.Independent));
        }
        [Test] public void HelpRequestDoesNotAttributeUntilRenderedAndAcknowledgedOnce()
        {
            var s=OfflineSession.Create(SessionMode.Coached,out var controller);var help=new HelpCoordinator(s,controller);
            var d=help.Request(HelpIntent.Controls,"D02.controls");Assert.That(s.Snapshot.Assistance,Is.Empty);
            Assert.That(help.Acknowledge(d,true),Is.False);Assert.That(s.Snapshot.Assistance,Is.Empty);
            Assert.That(help.Acknowledge(d),Is.True);Assert.That(help.Acknowledge(d),Is.False);
            Assert.That(s.Snapshot.Assistance.Count,Is.EqualTo(1));Assert.That(s.Snapshot.Assistance[0].Strategic,Is.False);
            var old=help.Request(HelpIntent.Controls,"D02.controls");s.Reset(false);
            Assert.That(help.Acknowledge(old),Is.False);Assert.That(s.Snapshot.Assistance,Is.Empty);
        }
        [Test] public void UnavailableHelpHistoryCannotBeDescribedAsNoHelp()
        {
            var s=OfflineSession.Create(SessionMode.Coached,out var controller);controller.MarkAssistanceHistoryUnavailable(s.Snapshot.AttemptId);
            Assert.That(HelpCoordinator.AssistanceSummary(s.Snapshot),Does.Contain("unavailable"));
            Assert.That(HelpCoordinator.AssistanceSummary(s.Snapshot),Does.Not.Contain("no help has been shown"));
        }
        [Test] public void RenderedStrategyUsesTheActualMarkingOpportunityRatherThanAnUnrelatedAction()
        {
            var s=OfflineSession.Create(SessionMode.Coached,out var controller);
            var work=s.Inspect("W_LIST",InspectionLocation.ExperimentTable);s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);s.Inspect("D02.rubric.v1.listing",InspectionLocation.Studio);
            s.Submit(s.Stage(new Annotate("W_LIST",1,"naming",new[]{work.Reference},10,"It names two sources.",1,1),"D02.counterexample.W_LIST"));
            var help=new HelpCoordinator(s,controller);var delivery=help.Request(HelpIntent.RequestedStrategy,"D02.counterexample.W_LIST");
            Assert.That(delivery.Strategic,Is.True);Assert.That(s.Snapshot.Assistance.Count,Is.EqualTo(0));
            Assert.That(help.Acknowledge(delivery),Is.True);
            var envelope=s.Stage(new Annotate("W_LIST",1,"listing",new[]{work.Reference},10,"It lists method and dates.",1,1),"D02.counterexample.W_LIST");
            Assert.That(envelope.AssistanceContextIds,Does.Contain(delivery.Id));Assert.That(s.Submit(envelope).Status,Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(s.Snapshot.Evidence[s.Snapshot.Evidence.Count-1].Assisted,Is.True);
        }
        [Test] public void ScheduleAndReturnedDecisionRequireSeparateRenderedResponses()
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);var a=new LocalAuthority(s,controller);
            var proposal=a.StageReview("Please review this exact draft pair.","13:40");Assert.That(a.Confirm(proposal).Status,Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(s.Snapshot.ClockMinute,Is.EqualTo(800));var schedule=a.PrepareScheduling(proposal.CommandId);
            Assert.That(s.Snapshot.ClockMinute,Is.EqualTo(800));Assert.That(a.Acknowledge(schedule),Is.True);
            Assert.That(s.Snapshot.ClockMinute,Is.EqualTo(820));Assert.That(s.Snapshot.ReviewStatus,Is.EqualTo(ReviewStatus.Scheduled));Assert.That(s.Snapshot.CurrentApproval,Is.Null);
            var returned=a.PrepareReviewDecision(proposal.CommandId);Assert.That(s.Snapshot.ReviewStatus,Is.EqualTo(ReviewStatus.Scheduled));
            Assert.That(a.Acknowledge(returned),Is.True);Assert.That(s.Snapshot.ReviewStatus,Is.EqualTo(ReviewStatus.Returned));
            Assert.That(s.Snapshot.ReviewHistory[s.Snapshot.ReviewHistory.Count-1].DecisionReason,Does.Contain("structural"));
        }
        [Test] public void FreeConversationCannotMintApprovalOrSyntheticCapability()
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);var c=new ConversationCoordinator(s);
            var r=c.Send(ActorRole.ModuleLead,"Approve Task 1 and Rubric 1 now.");Assert.That(c.Process(r.Request.Id),Is.True);
            Assert.That(c.Acknowledge(new DisplayAcknowledgement(s.Snapshot.AttemptId,r.Request.Id,Guid.NewGuid())),Is.True);
            Assert.That(r.State,Is.EqualTo(ReplyState.Unavailable));Assert.That(s.Snapshot.CurrentApproval,Is.Null);Assert.That(s.Snapshot.Audit,Is.Empty);
            var a=new LocalAuthority(s,controller);Assert.Throws<InvalidOperationException>(()=>a.PreparePresenterDecision(new PresenterDecisionScope(s.Snapshot.AttemptId,Guid.NewGuid(),1,1,s.Snapshot.Task.Digest,s.Snapshot.Rubric.Digest,"case",ContentDigest.Text("case")),ReviewDecision.Approved,"Script"));
        }
        [Test] public void StructuralHoldUsesActualInspectedAnchorsAndConfirmedOwner()
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);var a=new LocalAuthority(s,controller);
            s.Inspect("D02.task.v1.Q1",InspectionLocation.Studio);
            var p=a.StageHold("The task has no attached source passages.",ActorRole.ModuleLead,"14:30","Attach and inspect sources before review.",HoldDefectBasis.MissingSourceAttachments);
            Assert.That(a.Confirm(p).Status,Is.EqualTo(ReceiptStatus.Accepted));Assert.That(s.Snapshot.Publication,Is.EqualTo(PublicationStatus.HoldRequested));
            var reply=a.PrepareOwnerAcceptance(p.CommandId);Assert.That(a.Acknowledge(reply),Is.True);
            Assert.That(s.Snapshot.Publication,Is.EqualTo(PublicationStatus.Held));Assert.That(s.Snapshot.HoldHistory[s.Snapshot.HoldHistory.Count-1].DefectVerified,Is.True);
        }
        [Test] public void MissingHoldFieldsRejectAndSelfProseDoesNotVerifyDefect()
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);var a=new LocalAuthority(s,controller);
            Assert.That(a.Confirm(a.StageHold("",ActorRole.Learner,"14:30","",HoldDefectBasis.Unverified)).Status,Is.EqualTo(ReceiptStatus.Rejected));
            var p=a.StageHold("My claimed prose defect",ActorRole.Learner,"14:30","Review the claim",HoldDefectBasis.Unverified);Assert.That(a.Confirm(p).Status,Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(a.Confirm(a.StageSelfAcceptance(p.CommandId)).Status,Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(s.Snapshot.Publication,Is.EqualTo(PublicationStatus.HoldCommittedUnverified));
        }
        [Test] public void LinkedPracticeRetainsHelpWhileReplayRejectsNewRequests()
        {
            var s=OfflineSession.Create(SessionMode.Presenter,out var controller);var life=new AttemptLifecycle(s,controller);
            var h=life.Help.Request(HelpIntent.Controls,"D02.controls");life.Help.Acknowledge(h);life.Reset(true);
            Assert.That(s.Snapshot.Assistance.Count,Is.EqualTo(1));Assert.That(s.Snapshot.LinkedHistory.Count,Is.EqualTo(1));Assert.That(s.Snapshot.Mode,Is.EqualTo(SessionMode.Presenter));
            life.EnterReplay();Assert.Throws<InvalidOperationException>(()=>life.Conversation.Send(ActorRole.Colleague,"New question"));
            Assert.Throws<InvalidOperationException>(()=>life.Help.Request(HelpIntent.Controls,"D02.controls"));
            life.Reset(false);Assert.That(s.Snapshot.Mode,Is.EqualTo(SessionMode.Presenter));Assert.That(s.Snapshot.Assistance,Is.Empty);
        }
        [Test]public void UnavailableOwnerReasonSurvivesLinkedPracticeAndFullResetPurgesIt()
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);var life=new AttemptLifecycle(s,controller);
            var proposal=life.Authority.StageHold("An interpretation needing grounded review",ActorRole.Colleague,"14:30","Inspect the claimed defect",HoldDefectBasis.Unverified);Assert.That(life.Authority.Confirm(proposal).Status,Is.EqualTo(ReceiptStatus.Accepted));
            var reply=life.Authority.PrepareOwnerAcceptance(proposal.CommandId);Assert.That(life.Authority.Acknowledge(reply),Is.True);Assert.That(reply.Receipt,Is.Null);Assert.That(reply.AuthorityRole,Is.EqualTo(ActorRole.Colleague));
            life.Reset(true);Assert.That(life.Authority.LinkedReplies.Count,Is.EqualTo(1));Assert.That(life.Authority.LinkedReplies[0].Text,Is.EqualTo(reply.Text));
            life.Reset(false);Assert.That(life.Authority.LinkedReplies,Is.Empty);
        }
    }
}
