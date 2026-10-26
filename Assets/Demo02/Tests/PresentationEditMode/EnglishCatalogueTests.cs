using System;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presentation;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Tests
{
    public sealed class EnglishCatalogueTests
    {
        Func<string,string> original;
        [SetUp]public void Setup(){original=EnglishCopy.Resolver;EnglishCopy.Resolver=null;}
        [TearDown]public void Cleanup(){EnglishCopy.Resolver=original;}
        [Test]public void NativeTableEditsResolveLiveAndLiteralSlotsAreSubstitutedOnlyOnce()
        {
            var asset=AssetDatabase.LoadAssetAtPath<NativeEnglishStringTable>("Assets/Demo02/strings/STR_EN.asset");Assert.That(asset,Is.Not.Null);
            var copy=UnityEngine.Object.Instantiate(asset);
            try{var entry=copy.Entries.Single(e=>e.Key=="pair.label");entry.English="Exact {task} with {rubric}";copy.Invalidate();EnglishCopy.Resolver=copy.Get;
                const string literal="{rubric} $1 <b>not approved</b> \u2013 \u2014 20 kg";
                Assert.That(EnglishCopy.Format("pair.label","unused","task",literal,"rubric","2"),Is.EqualTo("Exact "+literal+" with 2"));
                Assert.Throws<System.Collections.Generic.KeyNotFoundException>(()=>copy.Get("missing.key"));
                Assert.Throws<System.Collections.Generic.KeyNotFoundException>(()=>EnglishCopy.Format("pair.label","unused","task","1"));
                copy.Entries=copy.Entries.Concat(new[]{entry}).ToArray();copy.Invalidate();Assert.Throws<InvalidOperationException>(()=>copy.Get("pair.label"));
            }finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        [TestCase(SessionMode.Independent,false)] [TestCase(SessionMode.Presenter,true)]
        public void DecisionDisclosureDistinguishesOrdinaryAndSyntheticExactScope(SessionMode mode,bool synthetic)
        {
            var s=OfflineSession.Create(mode,out var controller);var authority=new LocalAuthority(s,controller);
            if(synthetic)
            {
                var sources=new[]{s.Inspect("S_A",InspectionLocation.Archive).Reference,s.Inspect("S_B",InspectionLocation.Archive).Reference};
                var edit=s.Submit(s.Stage(new EditTask(1,s.Snapshot.Task.Questions,sources,"Explicit structural test pair."),"D02.revision.task"));
                Assert.That(edit.Status,Is.EqualTo(ReceiptStatus.Accepted),edit.ReasonCode);
            }
            var requested=authority.Confirm(authority.StageReview("Review this exact pair.","13:40"));
            var scheduling=authority.PrepareScheduling(requested.RequestId);Assert.That(authority.Acknowledge(scheduling),Is.True);
            AuthorityReply decision;
            if(synthetic){var snap=s.Snapshot;var scope=new PresenterDecisionScope(snap.AttemptId,requested.RequestId,snap.Task.Version,snap.Rubric.Version,snap.Task.Digest,snap.Rubric.Digest,"explicit-test",new string('a',64));decision=authority.PreparePresenterDecision(scope,ReviewDecision.Approved,"Only the explicit authored pair.");}
            else decision=authority.PrepareReviewDecision(requested.RequestId);
            Assert.That(decision.RequestedCommand,Is.TypeOf<DecideReview>());Assert.That(decision.Provenance,Does.Contain(synthetic?"exact synthetic pair":"exact requested pair"));
            Assert.That(decision.Provenance,Does.Not.Contain("No live service or semantic approval"));Assert.That(authority.Acknowledge(decision),Is.True);
            Assert.That(s.Snapshot.CurrentApproval!=null,Is.EqualTo(synthetic));
        }
        [Test]public void SoftWrapPreservesEveryOriginalCharacterAndExplicitLineBreak()
        {
            const string literal="not  20 kg\r\nline\t\twith  repeated words \u2013 \u2014";
            string result=SelectedCriterionLane.SoftWrap(literal,12);
            Assert.That(result,Does.Contain("\r\n"));Assert.That(result.Replace("\n",""),Is.EqualTo(literal.Replace("\n","")));Assert.That(result,Does.Contain("  "));
        }
        [TestCase(ScheduleDecision.Accepted,"Review time accepted",820)]
        [TestCase(ScheduleDecision.Denied,"Review time declined",800)]
        [TestCase(ScheduleDecision.Unavailable,"Review time unavailable",800)]
        public void CommandAcceptanceDoesNotMislabelTheActualSchedulingDecision(ScheduleDecision decision,string expected,int minute)
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);
            var request=s.Submit(s.Stage(new RequestReview(1,1,"Please review the actual pair.","13:40"),LocalAuthority.PublicationOpportunity));
            var envelope=s.Stage(new ScheduleReply(request.RequestId,1,1,"13:40",decision,Guid.NewGuid()),LocalAuthority.PublicationOpportunity);var context=new ReceiptContext(envelope,s.Snapshot);
            var receipt=controller.SubmitScripted(envelope,controller.PermitExactScriptedResponse(envelope,ActorRole.ModuleLead,"Explicit test response."));
            Assert.That(receipt.Status,Is.EqualTo(ReceiptStatus.Accepted));var words=ReceiptCopy.Describe(receipt,context,s.Snapshot);
            Assert.That(words.Heading,Is.EqualTo(expected));Assert.That(words.Body,Does.Contain("Task 1 / Rubric 1"));Assert.That(s.Snapshot.ClockMinute,Is.EqualTo(minute));Assert.That(s.Snapshot.CurrentApproval,Is.Null);
            if(decision==ScheduleDecision.Denied)Assert.That(words.Body,Does.Not.Contain("reason"));
        }
        [TestCase(ReviewDecision.Approved)] [TestCase(ReviewDecision.Returned)]
        public void HistoricalDecisionKeepsOriginalPairAndReasonAfterDocumentsChange(ReviewDecision decision)
        {
            var s=OfflineSession.Create(SessionMode.Independent,out var controller);
            var sources=new[]{s.Inspect("S_A",InspectionLocation.Archive).Reference,s.Inspect("S_B",InspectionLocation.Archive).Reference};
            Assert.That(s.Submit(s.Stage(new EditTask(1,new[]{new Question("Q1","Explain a source.",10,"O1"),new Question("Q2","Compare evidence.",10,"O2")},sources,"A bounded decision."),"D02.revision.task")).Status,Is.EqualTo(ReceiptStatus.Accepted));
            var request=s.Submit(s.Stage(new RequestReview(2,1,"Review this pair.","13:40"),LocalAuthority.PublicationOpportunity));
            var schedule=s.Stage(new ScheduleReply(request.RequestId,2,1,"13:40",ScheduleDecision.Accepted,Guid.NewGuid()),LocalAuthority.PublicationOpportunity);
            Assert.That(controller.SubmitScripted(schedule,controller.PermitExactScriptedResponse(schedule,ActorRole.ModuleLead,"Explicit test schedule.")).Status,Is.EqualTo(ReceiptStatus.Accepted));
            const string reason="Not universal approval \u2013 {pair}  20 kg.";
            var envelope=s.Stage(new DecideReview(request.RequestId,2,1,decision,reason),LocalAuthority.PublicationOpportunity);var context=new ReceiptContext(envelope,s.Snapshot);
            var receipt=controller.SubmitScripted(envelope,controller.PermitExactScriptedResponse(envelope,ActorRole.ModuleLead,"Explicit test decision."));Assert.That(receipt.Status,Is.EqualTo(ReceiptStatus.Accepted),receipt.ReasonCode);
            s.Submit(s.Stage(new EditRubric(1,new[]{new Criterion("naming","Q1",10,"Changed own description.","O1"),new Criterion("listing","Q2",10,"Changed own criterion.","O2")}),"D02.revision.rubric"));
            var words=ReceiptCopy.Describe(receipt,context,s.Snapshot);Assert.That(s.Snapshot.Rubric.Version,Is.EqualTo(2));Assert.That(words.Heading,Does.StartWith("Requested pair"));Assert.That(words.Body,Does.Contain("Task 2 / Rubric 1"));Assert.That(words.Body,Does.Contain(reason));Assert.That(words.Body,Does.Not.Contain("Task 2 / Rubric 2"));
        }
    }
}
