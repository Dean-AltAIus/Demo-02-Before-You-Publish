using System;
using System.Linq;
using NUnit.Framework;
using Altaius.Demo02.Domain;

namespace Altaius.Demo02.Tests
{
    public class DomainSessionTests
    {
        [Test]
        public void InspectedLiteralWorkCanBeMarkedWithoutGrantingApproval()
        {
            var session = OfflineSession.Create();
            var work = session.Inspect("W_LIST", InspectionLocation.ExperimentTable);
            session.Inspect("D02.rubric.v1.naming", InspectionLocation.Studio);
            var receipt = session.Submit(session.Stage(new Annotate("W_LIST", 1, "naming", new[] { work.Reference }, 10,
                "This passage names both source types.", 1, 1), "test-op"));
            Assert.That(receipt.Status, Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(session.Snapshot.DraftTotal("W_LIST"), Is.EqualTo(10));
            Assert.That(session.Snapshot.CurrentApproval, Is.Null);
        }

        [Test]
        public void ValidRubricEditCreatesNextImmutableEdition()
        {
            var session = OfflineSession.Create();
            var before = session.Snapshot.Rubric;
            var receipt = session.Submit(session.Stage(new EditRubric(1, new[] {
                new Criterion("explanation", "Q1", 10, "Explain how each source is used.", "O1"),
                new Criterion("judgement", "Q2", 10, "Compare and justify using evidence and limits.", "O2") }), "test-op"));
            Assert.That(receipt.Status, Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(session.Snapshot.Rubric.Version, Is.EqualTo(2));
            Assert.That(before.Criteria[0].Id, Is.EqualTo("naming"));
            Assert.That(session.Snapshot.Rubric.Digest, Is.Not.EqualTo(before.Digest));
        }
    }
}
