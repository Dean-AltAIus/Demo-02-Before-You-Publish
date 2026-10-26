using E=Altaius.Demo02.Domain.EnglishCopy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Altaius.Demo02.Domain;

namespace Altaius.Demo02.Gameplay
{
    /// <summary>Owns ephemeral free drafts and exact staged envelopes, never authority or semantic grading.</summary>
    public sealed class WorkbenchModel
    {
        public OfflineSession Session { get; }
        public CommandEnvelope Pending { get; private set; }
        public CommandReceipt LastReceipt { get; private set; }
        public string PendingKind { get; private set; }
        public MarkDraft Mark { get; private set; }
        public TaskDraft Task { get; private set; }
        public RubricDraft Rubric { get; private set; }
        readonly Dictionary<string, MarkDraft> markDrafts = new Dictionary<string, MarkDraft>();
        public WorkbenchModel(OfflineSession session) { Session = session ?? throw new ArgumentNullException(nameof(session)); }
        public bool MarkIsCurrent => Mark != null && Mark.TaskVersion == Session.Snapshot.Task.Version && Mark.RubricVersion == Session.Snapshot.Rubric.Version;
        public MarkDraft OpenMark(string specimenId, string criterionId)
        {
            var s = Session.Snapshot;
            var passage = s.Notebook.SingleOrDefault(p => p.Reference.SourceId == specimenId);
            if (passage == null) throw new InvalidOperationException(E.Get("workbenchmodel.openmark.openmark.message.inspect.this.work.at.the.experiment","Inspect this work at the experiment table first."));
            var criterion = s.Rubric.Criteria.SingleOrDefault(c => c.Id == criterionId);
            if (criterion == null || !s.Notebook.Any(p => p.Reference.SourceId == CriterionSource(s, criterionId)))
                throw new InvalidOperationException(E.Get("workbenchmodel.openmark.openmark.message.inspect.the.current.criterion.at.the","Inspect the current criterion at the task and rubric folios first."));
            var key = s.Task.Version + "/" + s.Rubric.Version + "/" + specimenId + "/" + criterionId;
            if (!markDrafts.TryGetValue(key, out var draft))
            {
                var old = s.CurrentAnnotations.LastOrDefault(a => a.Content.SpecimenId == specimenId && a.Content.CriterionId == criterionId);
                draft = new MarkDraft { TaskVersion = s.Task.Version, RubricVersion = s.Rubric.Version,
                    Work = passage.Reference, CriterionId = criterionId, Maximum = criterion.Marks,
                    Marks = old?.Content.Marks.ToString(CultureInfo.InvariantCulture) ?? "", Rationale = old?.Content.Rationale ?? "" };
                markDrafts.Add(key, draft);
            }
            Mark = draft; return draft;
        }
        public static string CriterionSource(SessionSnapshot s, string id) => "D02.rubric.v" + s.Rubric.Version + "." + id;
        public string MarkProblem()
        {
            if (Mark == null) return E.Get("workbenchmodel.markproblem.return.select.an.inspected.work.passage.and","Select an inspected work passage and criterion.");
            if (!MarkIsCurrent) return E.Get("workbenchmodel.markproblem.return.this.draft.belongs.to.an.earlier","This draft belongs to an earlier pair. Reopen the work against the current criteria.");
            if (!int.TryParse(Mark.Marks, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < 0 || n > Mark.Maximum)
                return E.Format("workbenchmodel.markproblem.return.enter.a.whole.number.mark.from","Enter a whole-number mark from 0 to {mark_maximum}.","mark_maximum",Mark.Maximum);
            if (string.IsNullOrWhiteSpace(Mark.Rationale)) return E.Get("workbenchmodel.markproblem.return.write.your.own.rationale.before.saving","Write your own rationale before saving.");
            return null;
        }
        public CommandEnvelope StageMark(bool refresh = false)
        {
            var problem = MarkProblem(); if (problem != null) throw new InvalidOperationException(problem);
            var cmd = new Annotate(Mark.Work.SourceId, Mark.Work.Version, Mark.CriterionId, new[] { Mark.Work },
                int.Parse(Mark.Marks, CultureInfo.InvariantCulture), Mark.Rationale, Mark.TaskVersion, Mark.RubricVersion);
            var digest = ContentDigest.Of(cmd);
            if (refresh || Mark.Envelope == null || Mark.CommandDigest != digest)
            {
                Mark.Envelope = Session.Stage(cmd, OpportunityForWork(Mark.Work.SourceId)); Mark.CommandDigest = digest;
            }
            Pending = Mark.Envelope; PendingKind = "annotation"; return Pending;
        }
        public static string OpportunityForWork(string sourceId)
        {
            switch (sourceId) { case "W_LIST": return "D02.counterexample.W_LIST"; case "W_COMPARE": return "D02.retest.W_COMPARE";
                case "W_PITCH": return "D02.retest.W_PITCH"; default: throw new ArgumentException(E.Get("workbenchmodel.opportunityforwork.opportunityforwork.message.unknown.authored.work.opportunity","Unknown authored work opportunity.")); }
        }
        public TaskDraft OpenTask(bool fresh = false)
        {
            if (Task == null || fresh) Task = TaskDraft.From(Session.Snapshot.Task); return Task;
        }
        public RubricDraft OpenRubric(bool fresh = false)
        {
            if (Rubric == null || fresh) Rubric = RubricDraft.From(Session.Snapshot.Rubric); return Rubric;
        }
        public CommandEnvelope PreviewTask()
        {
            var d = OpenTask(); var s = Session.Snapshot;
            if (d.BaseVersion != s.Task.Version) throw new InvalidOperationException(E.Get("workbenchmodel.previewtask.previewtask.message.task.draft.is.historical.keep.a","Task draft is historical. Keep a copy of your text, then load the current task explicitly."));
            var refs = s.Notebook.Where(p => d.SourceIds.Contains(p.Reference.SourceId)).Select(p => p.Reference).ToArray();
            if (refs.Length != d.SourceIds.Count) throw new InvalidOperationException(E.Get("workbenchmodel.previewtask.previewtask.message.inspect.selected.source.attachments.at.the","Inspect selected source attachments at the archive first."));
            var q = d.Questions.Select(x => new Question(x.Id, x.Text, WholeNumber(x.Marks), Outcomes(x.Outcomes))).ToArray();
            if (q.Sum(x => (long)x.Marks) != 20 || q.Any(x => x.Marks < 0 || string.IsNullOrWhiteSpace(x.Text)))
                throw new InvalidOperationException(E.Get("workbenchmodel.previewtask.previewtask.message.both.question.texts.are.required.marks","Both question texts are required. Marks must be nonnegative and total 20."));
            if (!d.SourceIds.SetEquals(new[] { "S_A", "S_B" }) || string.IsNullOrWhiteSpace(d.Context))
                throw new InvalidOperationException(E.Get("workbenchmodel.previewtask.previewtask.message.attach.inspected.s.a.and.s","Attach inspected S_A and S_B and write your decision context."));
            var cmd = new EditTask(d.BaseVersion, q, refs, d.Context);
            if (d.IsUnchanged(s.Task)) throw new InvalidOperationException(E.Get("workbenchmodel.previewtask.previewtask.message.change.the.task.before.creating.a","Change the task before creating a new edition."));
            Pending = Session.Stage(cmd, "D02.revision.task"); PendingKind = "task"; return Pending;
        }
        public CommandEnvelope PreviewRubric()
        {
            var d = OpenRubric(); var s = Session.Snapshot;
            if (d.BaseVersion != s.Rubric.Version) throw new InvalidOperationException(E.Get("workbenchmodel.previewrubric.previewrubric.message.rubric.draft.is.historical.keep.a","Rubric draft is historical. Keep a copy of your text, then load the current rubric explicitly."));
            var criteria = d.Rows.Select(x => new Criterion(x.Id, x.QuestionId, WholeNumber(x.Marks), x.Text, Outcomes(x.Outcomes))).ToArray();
            if (criteria.Length == 0 || criteria.Any(c => string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Description) || c.Marks < 0))
                throw new InvalidOperationException(E.Get("workbenchmodel.previewrubric.previewrubric.message.every.criterion.needs.a.unique.id","Every criterion needs a unique ID, description and nonnegative whole-number mark."));
            if (criteria.Select(x => x.Id).Distinct().Count() != criteria.Length) throw new InvalidOperationException(E.Get("workbenchmodel.previewrubric.previewrubric.message.criterion.ids.must.be.unique","Criterion IDs must be unique."));
            foreach (var q in s.Task.Questions)
            {
                if (criteria.Where(c => c.QuestionId == q.Id).Sum(c => (long)c.Marks) != q.Marks)
                    throw new InvalidOperationException(E.Format("workbenchmodel.previewrubric.previewrubric.message.criteria.must.total.as.in.the","{q_id} criteria must total {q_marks}, as in the current task.","q_id",q.Id,"q_marks",q.Marks));
            }
            if (criteria.Sum(c => (long)c.Marks) != 20 || criteria.Any(c => c.QuestionId != "Q1" && c.QuestionId != "Q2"))
                throw new InvalidOperationException(E.Get("workbenchmodel.previewrubric.previewrubric.message.use.q1.or.q2.and.reconcile","Use Q1 or Q2 and reconcile the total to 20."));
            if (criteria.Any(c => c.OutcomeIds.Any(o => !s.Task.Questions.Single(q => q.Id == c.QuestionId).OutcomeIds.Contains(o))))
                throw new InvalidOperationException(E.Get("workbenchmodel.previewrubric.previewrubric.message.each.criterion.outcome.must.belong.to","Each criterion outcome must belong to its current question."));
            if (ContentDigest.Of(criteria) == ContentDigest.Of(s.Rubric.Criteria)) throw new InvalidOperationException(E.Get("workbenchmodel.previewrubric.previewrubric.message.change.the.rubric.before.creating.a","Change the rubric before creating a new edition."));
            Pending = Session.Stage(new EditRubric(d.BaseVersion, criteria), "D02.revision.rubric"); PendingKind = "rubric"; return Pending;
        }
        public CommandReceipt ConfirmPending()
        {
            if (Pending == null) throw new InvalidOperationException(E.Get("workbenchmodel.confirmpending.confirmpending.message.preview.the.change.first","Preview the change first."));
            // Submit exactly the existing envelope. Retry must never silently restage it.
            LastReceipt = Session.FindReceipt(Pending.CommandId) ?? Session.Submit(Pending);
            return LastReceipt;
        }
        public CommandReceipt RetryOriginal() => ConfirmPending();
        public void CancelPreview() { /* Deliberately retains free drafts and their exact staged identity. */ }
        public static int WholeNumber(string value)
        { if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) throw new InvalidOperationException(E.Get("workbenchmodel.wholenumber.wholenumber.message.use.whole.numbers.for.marks","Use whole numbers for marks.")); return n; }
        public static string[] Outcomes(string value)
        {
            var ids = (value ?? "").Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (ids.Length == 0 || ids.Distinct().Count() != ids.Length || ids.Any(x => x != "O1" && x != "O2"))
                throw new InvalidOperationException(E.Get("workbenchmodel.outcomes.outcomes.message.outcomes.must.be.o1.o2.or","Outcomes must be O1, O2, or O1,O2 without duplicates."));
            return ids;
        }
    }
    public sealed class MarkDraft
    {
        public int TaskVersion, RubricVersion, Maximum;
        public PassageRef Work;
        public string CriterionId, Marks, Rationale;
        public CommandEnvelope Envelope;
        public string CommandDigest;
    }
    public sealed class QuestionDraft { public string Id, Text, Marks, Outcomes; }
    public sealed class CriterionDraft { public string Id, QuestionId, Text, Marks, Outcomes; }
    public sealed class TaskDraft
    {
        public int BaseVersion; public string Context; public List<QuestionDraft> Questions = new List<QuestionDraft>();
        public HashSet<string> SourceIds = new HashSet<string>();
        public static TaskDraft From(TaskDocument t) => new TaskDraft { BaseVersion = t.Version, Context = t.DecisionContext ?? "",
            SourceIds = new HashSet<string>(t.Sources.Select(p => p.SourceId)), Questions = t.Questions.Select(q => new QuestionDraft {
                Id = q.Id, Text = q.Text, Marks = q.Marks.ToString(CultureInfo.InvariantCulture), Outcomes = string.Join(",", q.OutcomeIds) }).ToList() };
        public bool IsUnchanged(TaskDocument t) => Context == (t.DecisionContext ?? "") && SourceIds.SetEquals(t.Sources.Select(s => s.SourceId)) &&
            Questions.Count == t.Questions.Count && Questions.Zip(t.Questions, (a,b) => a.Id == b.Id && a.Text == b.Text &&
                a.Marks == b.Marks.ToString(CultureInfo.InvariantCulture) && a.Outcomes == string.Join(",", b.OutcomeIds)).All(x => x);
    }
    public sealed class RubricDraft
    {
        public int BaseVersion; public List<CriterionDraft> Rows = new List<CriterionDraft>();
        public static RubricDraft From(RubricDocument r) => new RubricDraft { BaseVersion = r.Version, Rows = r.Criteria.Select(c => new CriterionDraft {
            Id = c.Id, QuestionId = c.QuestionId, Text = c.Description, Marks = c.Marks.ToString(CultureInfo.InvariantCulture), Outcomes = string.Join(",", c.OutcomeIds) }).ToList() };
    }
}
