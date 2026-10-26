using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;

namespace Altaius.Demo02.UI
{
    /// <summary>Bounded presentation jobs backed by the existing immutable domain audit.
    /// A full queue stops the cursor before the next request, including receipts after it.</summary>
    public sealed class ReviewPresentationQueue
    {
        public sealed class Job
        {
            public Guid Id { get; }
            public DocumentPair Pair { get; }
            public bool PreparationStarted { get; internal set; }
            public bool Ready { get; internal set; }
            internal Job(ReviewRequestRecord request) { Id = request.Id; Pair = request.Pair; }
        }
        public sealed class Settlement
        {
            public Guid RequestId { get; }
            public string Reason { get; }
            internal Settlement(Guid id, string reason) { RequestId = id; Reason = reason; }
        }
        public const int Capacity = 8, HistoryCapacity = 64;
        readonly Queue<Job> pending = new Queue<Job>();
        readonly Queue<Settlement> history = new Queue<Settlement>();
        public Job Active { get; private set; }
        public Job Next => pending.Count == 0 ? null : pending.Peek();
        public int PendingCount => pending.Count;
        public int Cursor { get; private set; }
        public int TotalSettled { get; private set; }
        public IEnumerable<Settlement> RecentSettlements => history;
        public event Action<Settlement> Settled;

        static bool IsRequest(AuditRecord audit) => audit.Receipt.Status == ReceiptStatus.Accepted && audit.Receipt.Action == "review.request";
        public int DeferredCount(SessionSnapshot snapshot) => snapshot.Audit.Count(a => a.Sequence > Cursor && IsRequest(a));
        public void Observe(SessionSnapshot snapshot)
        {
            foreach (var audit in snapshot.Audit.Where(a => a.Sequence > Cursor).OrderBy(a => a.Sequence))
            {
                if (IsRequest(audit))
                {
                    if (pending.Count == Capacity) break;
                    var request = snapshot.ReviewHistory.First(r => r.Id == audit.Receipt.RequestId);
                    pending.Enqueue(new Job(request));
                }
                Cursor = audit.Sequence;
            }
        }
        public bool Prepare(Guid id)
        {
            if (Next == null || Next.Id != id || Next.PreparationStarted) return false;
            Next.PreparationStarted = true;
            return true;
        }
        public bool Ready(Guid id)
        {
            var job = pending.FirstOrDefault(j => j.Id == id);
            if (job == null || !job.PreparationStarted) return false;
            job.Ready = true;
            return true;
        }
        public bool StartReady()
        {
            if (Active != null || Next == null || !Next.Ready) return false;
            Active = pending.Dequeue();
            return true;
        }
        public void Complete(string reason)
        {
            if (Active == null) return;
            Record(Active.Id, reason);
            Active = null;
        }
        public void SettleAll(SessionSnapshot snapshot, string reason)
        {
            Complete(reason);
            while (pending.Count > 0) Record(pending.Dequeue().Id, reason);
            // Explicit interruption settles even accepted requests not yet loaded due to backpressure.
            foreach (var audit in snapshot.Audit.Where(a => a.Sequence > Cursor).OrderBy(a => a.Sequence))
            {
                if (IsRequest(audit)) Record(audit.Receipt.RequestId, reason);
                Cursor = audit.Sequence;
            }
        }
        void Record(Guid id, string reason)
        {
            var entry = new Settlement(id, reason);
            if (history.Count == HistoryCapacity) history.Dequeue();
            history.Enqueue(entry); TotalSettled++; Settled?.Invoke(entry);
        }
    }
}
