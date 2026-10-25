using System;
using System.Linq;
using Altaius.Demo02.Authoring;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presenter;
using NUnit.Framework;
using UnityEngine;
namespace Altaius.Demo02.Tests
{
    public sealed class PresenterScopeTests
    {
        PresenterSequence sequence;
        [SetUp] public void Setup()=>sequence=PresenterSequenceAuthoring.CreateDefault();
        [TearDown] public void Cleanup()=>UnityEngine.Object.DestroyImmediate(sequence);
        OfflineSession Repaired(SessionMode mode,out ScenarioController controller)
        {
            var s=OfflineSession.Create(mode,out controller);var sources=new[]{s.Inspect("S_A",InspectionLocation.Archive).Reference,s.Inspect("S_B",InspectionLocation.Archive).Reference};
            string Value(string id)=>sequence.Steps.First(x=>x.Control==id&&x.Kind==PresenterStepKind.TypeWorkbench).Text;
            var questions=new[]{"Q1","Q2"}.Select(id=>new Question(id,Value("Task."+id+".Text"),int.Parse(Value("Task."+id+".Marks")),Value("Task."+id+".Outcomes"))).ToArray();
            Assert.That(s.Submit(s.Stage(new EditTask(1,questions,sources,Value("Task.Context")),"D02.revision.task")).Status,Is.EqualTo(ReceiptStatus.Accepted));
            var criteria=Enumerable.Range(0,4).Select(i=>new Criterion(Value("Rubric."+i+".Id"),Value("Rubric."+i+".Question"),int.Parse(Value("Rubric."+i+".Marks")),Value("Rubric."+i+".Text"),Value("Rubric."+i+".Outcomes"))).ToArray();
            Assert.That(s.Submit(s.Stage(new EditRubric(1,criteria),"D02.revision.rubric")).Status,Is.EqualTo(ReceiptStatus.Accepted));
            foreach(var c in criteria)s.Inspect("D02.rubric.v2."+c.Id,InspectionLocation.Studio);
            foreach(var work in new[]{"W_COMPARE","W_PITCH"})
            {
                var passage=s.Inspect(work,InspectionLocation.ExperimentTable);
                foreach(var mark in sequence.Marks.Where(m=>m.Work==work))Assert.That(s.Submit(s.Stage(new Annotate(work,1,mark.Criterion,new[]{passage.Reference},mark.Marks,mark.Rationale,2,2),"D02.retest."+work)).Status,Is.EqualTo(ReceiptStatus.Accepted));
            }
            return s;
        }
        [Test] public void ExactSelectiveFindingsRemainSyntheticAndCannotEnterLearnerMode()
        {
            var s=Repaired(SessionMode.Presenter,out var controller);var scope=new PresenterScenarioScope(s.Snapshot.AttemptId,sequence);var command=scope.PrepareEvaluation(s.Snapshot,"W_COMPARE");
            var envelope=s.Stage(command,"D02.retest.W_COMPARE");var receipt=controller.SubmitSyntheticEvaluation(envelope,ActorRole.ExperimentReviewer);var again=controller.SubmitSyntheticEvaluation(envelope,ActorRole.ExperimentReviewer);
            Assert.That(receipt.Status,Is.EqualTo(ReceiptStatus.Accepted));Assert.That(again,Is.SameAs(receipt));Assert.That(s.Snapshot.EvaluationHistory.Count,Is.EqualTo(1));Assert.That(s.Snapshot.Evidence,Is.Empty);Assert.That(command.Findings.Count,Is.EqualTo(4));
            var learner=Repaired(SessionMode.Independent,out _);Assert.Throws<InvalidOperationException>(()=>new PresenterScenarioScope(learner.Snapshot.AttemptId,sequence).PrepareEvaluation(learner.Snapshot,"W_COMPARE"));
        }
        [Test] public void AChangedRationaleCannotAcquireTheAuthoredFindingByResemblance()
        {
            var s=Repaired(SessionMode.Presenter,out _);var scope=new PresenterScenarioScope(s.Snapshot.AttemptId,sequence);var m=sequence.Marks.First(x=>x.Work=="W_COMPARE");var work=s.Snapshot.Notebook.Single(p=>p.Reference.SourceId=="W_COMPARE");
            s.Submit(s.Stage(new Annotate("W_COMPARE",1,m.Criterion,new[]{work.Reference},m.Marks,"My different rationale, even with similar words.",2,2),"D02.retest.W_COMPARE"));
            Assert.Throws<InvalidOperationException>(()=>scope.PrepareEvaluation(s.Snapshot,"W_COMPARE"));Assert.That(s.Snapshot.EvaluationHistory,Is.Empty);
        }
        [Test] public void TheScopeIsFrozenAndAResetTombstonesAPreparedEvaluation()
        {
            var s=Repaired(SessionMode.Presenter,out var controller);var scope=new PresenterScenarioScope(s.Snapshot.AttemptId,sequence);sequence.Marks[0].Rationale="Changed asset data";
            var envelope=s.Stage(scope.PrepareEvaluation(s.Snapshot,"W_COMPARE"),"D02.retest.W_COMPARE");s.Reset(false);var receipt=controller.SubmitSyntheticEvaluation(envelope,ActorRole.ExperimentReviewer);
            Assert.That(receipt.ReasonCode,Is.EqualTo("AttemptTombstoned"));Assert.That(s.Snapshot.EvaluationHistory,Is.Empty);Assert.That(s.Snapshot.Evidence,Is.Empty);
        }
        [Test] public void PublicationScopeNeedsBothRetestsAtTheCurrentExperimentRevision()
        {
            var s=Repaired(SessionMode.Presenter,out var controller);var scope=new PresenterScenarioScope(s.Snapshot.AttemptId,sequence);
            Assert.Throws<InvalidOperationException>(()=>scope.DecisionScope(s.Snapshot,Guid.NewGuid()));
            foreach(var work in new[]{"W_COMPARE","W_PITCH"})controller.SubmitSyntheticEvaluation(s.Stage(scope.PrepareEvaluation(s.Snapshot,work),"D02.retest."+work),ActorRole.ExperimentReviewer);
            Assert.That(scope.DecisionScope(s.Snapshot,Guid.NewGuid()),Is.Not.Null);var mark=sequence.Marks.Last();var passage=s.Snapshot.Notebook.Single(p=>p.Reference.SourceId==mark.Work);
            s.Submit(s.Stage(new Annotate(mark.Work,1,mark.Criterion,new[]{passage.Reference},mark.Marks,mark.Rationale,2,2),"D02.retest."+mark.Work));Assert.That(s.Snapshot.CurrentEvaluations,Is.Empty);Assert.Throws<InvalidOperationException>(()=>scope.DecisionScope(s.Snapshot,Guid.NewGuid()));
        }
        [Test] public void AuthoredSequenceActuallyVisitsPreviewsAndBothOriginalConfirmations()
        {
            Assert.That(sequence.Steps.Count(s=>s.Kind==PresenterStepKind.MoveStation),Is.GreaterThan(6));Assert.That(sequence.Steps.Count(s=>s.Kind==PresenterStepKind.TypeWorkbench),Is.GreaterThan(30));
            var findings=sequence.Steps.Select((s,i)=>new{s,i}).Where(x=>x.s.Kind==PresenterStepKind.Finding).ToArray();var lastMark=sequence.Steps.Select((s,i)=>new{s,i}).Last(x=>x.s.Control=="Mark.Rationale").i;
            Assert.That(findings.All(x=>x.i>lastMark),Is.True,"All annotation mutations must precede exact-revision reviewer findings.");Assert.That(sequence.Steps.Count(s=>s.Kind==PresenterStepKind.ReviewDecision),Is.EqualTo(1));Assert.That(sequence.Steps.Any(s=>s.Control=="Review.Release"),Is.True);
        }
    }
}
