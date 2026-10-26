using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using NUnit.Framework;

namespace Altaius.Demo02.Tests
{
    public sealed class ReviewPresentationQueueTests
    {
        OfflineSession session;
        ReviewPresentationQueue queue;
        [SetUp] public void Setup() { session = OfflineSession.Create(); queue = new ReviewPresentationQueue(); }
        CommandReceipt Commit(DomainCommand command)
        {
            var receipt = session.Submit(session.Stage(command, "presentation-lifecycle-test"));
            Assert.That(receipt.Status, Is.EqualTo(ReceiptStatus.Accepted), receipt.ReasonCode);
            return receipt;
        }
        Guid Request() => Commit(new RequestReview(session.Snapshot.Task.Version, session.Snapshot.Rubric.Version, "Review the exact recorded pair.", "13:40")).RequestId;
        void Ready(Guid id) { Assert.That(queue.Prepare(id), Is.True); Assert.That(queue.Ready(id), Is.True); }

        [TestCase(true)] [TestCase(false)]
        public void LaterLearnerCompletionBeforeOrAfterActiveCompletionKeepsBothIdentities(bool before)
        {
            var a = Request(); queue.Observe(session.Snapshot); Ready(a); Assert.That(queue.StartReady(), Is.True);
            Commit(new EditRubric(1, session.Snapshot.Rubric.Criteria));
            var b = Request(); queue.Observe(session.Snapshot); Assert.That(queue.Prepare(b), Is.True);
            if (before) queue.Ready(b);
            Assert.That(queue.Active.Id, Is.EqualTo(a)); Assert.That(queue.Active.Pair.RubricVersion, Is.EqualTo(1));
            Assert.That(queue.StartReady(), Is.False); queue.Complete("Presented");
            if (!before) { Assert.That(queue.StartReady(), Is.False); queue.Ready(b); }
            Assert.That(queue.StartReady(), Is.True); Assert.That(queue.Active.Id, Is.EqualTo(b));
            Assert.That(queue.Active.Pair.RubricVersion, Is.EqualTo(2)); queue.Complete("Presented");
            Assert.That(queue.RecentSettlements.Select(x => x.RequestId), Is.EqualTo(new[] { a, b }));
            Assert.That(session.Snapshot.ReviewHistory.Count, Is.EqualTo(2)); Assert.That(session.Snapshot.ApprovalHistory, Is.Empty);
        }
        [Test] public void SeveralUnseenSamePairAuditsResolveTheirOwnReceiptIds()
        {
            var ids = Enumerable.Range(0, 4).Select(_ => Request()).ToArray(); queue.Observe(session.Snapshot);
            foreach (var id in ids) { Assert.That(queue.Next.Id, Is.EqualTo(id)); Ready(id); queue.StartReady(); queue.Complete("Presented"); }
            Assert.That(queue.RecentSettlements.Select(x => x.RequestId), Is.EqualTo(ids));
        }
        [Test] public void FullQueueStopsBeforeDeferredRequestAndFollowingNonReviewReceipt()
        {
            var ids = Enumerable.Range(0, 12).Select(_ => Request()).ToArray();
            Commit(new EditRubric(1, session.Snapshot.Rubric.Criteria));
            queue.Observe(session.Snapshot); Assert.That(queue.PendingCount, Is.EqualTo(8)); Assert.That(queue.DeferredCount(session.Snapshot), Is.EqualTo(4));
            Assert.That(queue.Cursor, Is.LessThan(session.Snapshot.Audit.Last().Sequence));
            foreach (var id in ids) { Ready(id); queue.StartReady(); queue.Complete("Presented"); queue.Observe(session.Snapshot); }
            Assert.That(queue.RecentSettlements.Select(x => x.RequestId), Is.EqualTo(ids)); Assert.That(queue.Cursor, Is.EqualTo(session.Snapshot.Audit.Last().Sequence));
        }
        [TestCase("Cancelled")] [TestCase("Route exit")]
        public void InterruptionSettlesLoadedAndDeferredRequestsOnceButAllowsNewRequests(string reason)
        {
            var ids = Enumerable.Range(0, 12).Select(_ => Request()).ToArray(); queue.Observe(session.Snapshot); Ready(ids[0]); queue.StartReady();
            queue.SettleAll(session.Snapshot, reason); Assert.That(queue.TotalSettled, Is.EqualTo(12));
            queue.Observe(session.Snapshot); Assert.That(queue.PendingCount, Is.Zero); Assert.That(queue.Active, Is.Null);
            Assert.That(queue.Ready(ids[1]), Is.False, "Late preparation callback cannot revive a settled request.");
            var next = Request(); queue.Observe(session.Snapshot); Ready(next); Assert.That(queue.StartReady(), Is.True); queue.Complete("Presented");
            Assert.That(queue.RecentSettlements.Select(x => x.RequestId), Is.EqualTo(ids.Concat(new[] { next })));
            Assert.That(session.Snapshot.ReviewHistory.Count, Is.EqualTo(13)); Assert.That(session.Snapshot.CurrentApproval, Is.Null);
        }
        [Test] public void BoundedHistoryEmitsEverySettlementWithoutRetainingUnboundedCopies()
        {
            var seen = new List<Guid>(); queue.Settled += x => seen.Add(x.RequestId);
            for (int i = 0; i < 70; i++) { var id = Request(); queue.Observe(session.Snapshot); Ready(id); queue.StartReady(); queue.Complete("Reduced motion"); }
            Assert.That(seen.Distinct().Count(), Is.EqualTo(70)); Assert.That(queue.TotalSettled, Is.EqualTo(70));
            Assert.That(queue.RecentSettlements.Count(), Is.EqualTo(64)); Assert.That(queue.PendingCount, Is.Zero);
        }
    }
}
