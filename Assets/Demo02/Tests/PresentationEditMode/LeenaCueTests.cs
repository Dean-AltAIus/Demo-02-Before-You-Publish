using System;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
using Altaius.Demo02.Presentation;
using NUnit.Framework;
namespace Altaius.Demo02.Tests
{
    public sealed class LeenaCueTests
    {
        [Test]public void RequestDoesNotAnimateAndDisplayedDeliveryDoesNotRepeat()
        {
            var session=OfflineSession.Create(SessionMode.Independent,out var controller);
            var help=new HelpCoordinator(session,controller);var cue=new LeenaCueState();cue.Observe(session.Snapshot);
            var pending=help.Request(HelpIntent.Controls,"D02.controls");cue.Observe(session.Snapshot);
            Assert.That(cue.State,Is.EqualTo("Attention"));Assert.That(session.Snapshot.Assistance,Is.Empty);
            Assert.That(help.Acknowledge(pending),Is.True);cue.Observe(session.Snapshot);
            Assert.That(cue.State,Is.EqualTo("DeliveredHelp"));cue.Tick(3,false);cue.Observe(session.Snapshot);
            Assert.That(cue.Remaining,Is.EqualTo(5));cue.Tick(9,true);Assert.That(cue.Remaining,Is.EqualTo(5));
            cue.Tick(5,false);Assert.That(cue.State,Is.EqualTo("Attention"));Assert.That(session.Snapshot.Assistance.Count,Is.EqualTo(1));
            Assert.That(session.Snapshot.Evidence,Is.Empty);Assert.That(session.Snapshot.ApprovalHistory,Is.Empty);
        }
        [Test]public void ResetAndHandbackDoNotReplayHistoricalHelp()
        {
            var old=OfflineSession.Create(SessionMode.Independent,out var controller);var cue=new LeenaCueState();cue.Observe(old.Snapshot);
            var help=new HelpCoordinator(old,controller);help.Acknowledge(help.Request(HelpIntent.Controls,"D02.controls"));cue.Observe(old.Snapshot);
            var fresh=OfflineSession.Create();cue.Observe(fresh.Snapshot);Assert.That(cue.State,Is.EqualTo("Attention"));
            cue.Observe(old.Snapshot);Assert.That(cue.State,Is.EqualTo("Attention"));Assert.That(cue.Remaining,Is.Zero);
            Assert.That(old.Snapshot.Assistance.Count,Is.EqualTo(1));Assert.That(fresh.Snapshot.Assistance,Is.Empty);
        }
        [Test]public void OnlyCurrentDistinctReportDisplayStartsDebriefCue()
        {
            var session=OfflineSession.Create();var cue=new LeenaCueState();cue.Observe(session.Snapshot);var display=Guid.NewGuid();
            Assert.That(cue.ReportRendered(Guid.NewGuid(),display),Is.False);Assert.That(cue.ReportRendered(session.Snapshot.AttemptId,Guid.Empty),Is.False);
            Assert.That(cue.ReportRendered(session.Snapshot.AttemptId,display),Is.True);cue.Tick(2,false);
            Assert.That(cue.ReportRendered(session.Snapshot.AttemptId,display),Is.False);Assert.That(cue.Remaining,Is.EqualTo(6));
            cue.Cancel();Assert.That(cue.State,Is.EqualTo("Attention"));Assert.That(session.Snapshot.Assistance,Is.Empty);Assert.That(session.Snapshot.Evidence,Is.Empty);
        }
    }
}
