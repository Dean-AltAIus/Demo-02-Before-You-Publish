using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Altaius.Demo02.Domain
{
    public enum SessionMode { Coached, Independent, Presenter, Replay }
    public enum ActorRole { Learner, ScenarioController, ModuleLead, Colleague, ExperimentReviewer, RecipientChannel, GroundedEvaluator }
    public enum ReceiptStatus { Accepted, Rejected, Pending, Unknown }
    public enum ReviewStatus { Unrequested, Pending, Scheduled, Approved, Returned, Unavailable, Denied, Stale, Unknown }
    public enum PublicationStatus { Draft, Released, HoldRequested, Held, Stale, HoldCommittedUnverified }
    public enum HoldDefectBasis { Unverified, MissingSourceAttachments, MissingDecisionContext, PerQuestionMarksMismatch, ReviewedInterpretation }
    public enum FindingDecision { Supported, Unsupported, Ambiguous }
    public enum FindingCoverage { Partial, Complete }
    public enum ScheduleDecision { Accepted, Denied, Unavailable }
    public enum ReviewDecision { Approved, Returned }
    public enum ActFinding { SupportedCommitment, SupportedCorrection, Ambiguous, Nonassertive, Unsupported }
    public enum InspectionLocation { Studio, Archive, ExperimentTable, PortableWorkspace }
    public enum AssistanceHistory { Complete, Unavailable }

    internal static class Frozen
    {
        internal static IReadOnlyList<T> List<T>(IEnumerable<T> values) => (values ?? Enumerable.Empty<T>()).ToList().AsReadOnly();
        internal static IReadOnlyDictionary<string, int?> Revisions(IDictionary<string, int?> values) => new ReadOnlyDictionary<string, int?>(new Dictionary<string, int?>(values));
    }

    // Canonical, culture independent, length-delimited object encoding. It does not interpret prose.
    public static class ContentDigest
    {
        public static string Text(string text)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "").ToLowerInvariant();
        }
        public static string Of(object value) => Text(Encode(value));
        private static string Encode(object value)
        {
            if (value == null) return "null;";
            if (value is string s) return "s" + s.Length + ":" + s;
            if (value is Guid g) return "g" + g.ToString("D");
            if (value is Enum) return "e" + value.GetType().Name + ":" + Convert.ToInt32(value, CultureInfo.InvariantCulture) + ";";
            if (value is bool b) return b ? "true;" : "false;";
            if (value is int || value is long) return "n" + Convert.ToString(value, CultureInfo.InvariantCulture) + ";";
            if (value is IReadOnlyDictionary<string, int?> dict)
                return "{" + string.Concat(dict.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => Encode(x.Key) + Encode(x.Value))) + "}";
            if (value is IEnumerable seq) return "[" + string.Concat(seq.Cast<object>().Select(Encode)) + "]";
            return value.GetType().Name + "{" + string.Concat(value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => Encode(p.Name) + Encode(p.GetValue(value)))) + "}";
        }
    }

    public sealed class PassageRef
    {
        public string SourceId { get; }
        public int Version { get; }
        public string Sha256 { get; }
        public string PassageId { get; }
        public PassageRef(string sourceId, int version, string sha256, string passageId)
        { SourceId = sourceId; Version = version; Sha256 = sha256; PassageId = passageId; }
        internal string Key => SourceId + "/" + Version + "/" + PassageId;
        internal bool Same(PassageRef other) => other != null && Key == other.Key && Sha256 == other.Sha256;
    }
    public sealed class Passage
    {
        public PassageRef Reference { get; }
        public string DisplayText { get; }
        public InspectionLocation Location { get; }
        internal string ExactText { get; }
        internal Passage(PassageRef reference, string exactText, InspectionLocation location)
        { Reference = reference; ExactText = exactText; DisplayText = exactText; Location = location; }
    }
    public sealed class Question
    {
        public string Id { get; }
        public string Text { get; }
        public int Marks { get; }
        public IReadOnlyList<string> OutcomeIds { get; }
        public Question(string id, string text, int marks, params string[] outcomeIds)
        { Id = id; Text = text; Marks = marks; OutcomeIds = Frozen.List(outcomeIds); }
    }
    public sealed class Criterion
    {
        public string Id { get; }
        public string QuestionId { get; }
        public IReadOnlyList<string> OutcomeIds { get; }
        public int Marks { get; }
        public string Description { get; }
        public Criterion(string id, string questionId, int marks, string description, params string[] outcomeIds)
        { Id = id; QuestionId = questionId; Marks = marks; Description = description; OutcomeIds = Frozen.List(outcomeIds); }
    }
    public sealed class TaskDocument
    {
        public int Version { get; }
        public string Digest { get; }
        public IReadOnlyList<Question> Questions { get; }
        public IReadOnlyList<PassageRef> Sources { get; }
        public string DecisionContext { get; }
        public int TotalMarks => Questions.Sum(q => q.Marks);
        internal TaskDocument(int version, IEnumerable<Question> questions, IEnumerable<PassageRef> sources, string context)
        { Version = version; Questions = Frozen.List(questions); Sources = Frozen.List(sources); DecisionContext = context;
          Digest = ContentDigest.Of(new { Version, Questions, Sources, DecisionContext }); }
    }
    public sealed class RubricDocument
    {
        public int Version { get; }
        public string Digest { get; }
        public IReadOnlyList<Criterion> Criteria { get; }
        internal RubricDocument(int version, IEnumerable<Criterion> criteria)
        { Version = version; Criteria = Frozen.List(criteria); Digest = ContentDigest.Of(new { Version, Criteria }); }
    }
    public sealed class DocumentPair
    {
        public int TaskVersion { get; }
        public int RubricVersion { get; }
        public string TaskDigest { get; }
        public string RubricDigest { get; }
        internal DocumentPair(TaskDocument task, RubricDocument rubric)
        { TaskVersion = task.Version; RubricVersion = rubric.Version; TaskDigest = task.Digest; RubricDigest = rubric.Digest; }
        internal DocumentPair(int taskVersion, int rubricVersion, string taskDigest, string rubricDigest)
        { TaskVersion = taskVersion; RubricVersion = rubricVersion; TaskDigest = taskDigest; RubricDigest = rubricDigest; }
        internal bool Same(DocumentPair other) => other != null && TaskVersion == other.TaskVersion && RubricVersion == other.RubricVersion && TaskDigest == other.TaskDigest && RubricDigest == other.RubricDigest;
    }
    public sealed class CriterionFinding
    {
        public Guid AnnotationId { get; }
        public string CriterionId { get; }
        public FindingDecision Decision { get; }
        public IReadOnlyList<PassageRef> Sources { get; }
        public string Reason { get; }
        public CriterionFinding(Guid annotationId, string criterionId, FindingDecision decision, IEnumerable<PassageRef> sources, string reason)
        { AnnotationId = annotationId; CriterionId = criterionId; Decision = decision; Sources = Frozen.List(sources); Reason = reason; }
    }
    public sealed class ClauseRef
    {
        public int Start { get; }
        public int Length { get; }
        public string ExactText { get; }
        public ClauseRef(int start, int length, string exactText) { Start = start; Length = length; ExactText = exactText; }
    }
}
