using System;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presentation;
namespace Altaius.Demo02.Presenter
{
    /// <summary>Immutable exact-edition demonstration capability, never a free-text interpreter or worked oracle.</summary>
    public sealed class PresenterScenarioScope
    {
        readonly Guid attempt;readonly string task,rubric,scenario,digest,reason;readonly DemonstrationMark[] marks;
        public PresenterScenarioScope(Guid attempt,PresenterSequence frozen)
        {
            this.attempt=attempt;task=frozen.ExpectedTaskDigest;rubric=frozen.ExpectedRubricDigest;scenario=frozen.ScenarioId;digest=frozen.Digest;reason=frozen.ReviewReason;
            marks=frozen.Marks.Select(m=>new DemonstrationMark{Work=m.Work,WorkSha256=m.WorkSha256,Criterion=m.Criterion,Marks=m.Marks,Rationale=m.Rationale,Finding=m.Finding,Decision=m.Decision}).ToArray();
        }
        public void Validate(SessionSnapshot s)
        {
            if(s.Mode!=SessionMode.Presenter||s.AttemptId!=attempt||s.Task.Version!=2||s.Rubric.Version!=2||string.IsNullOrEmpty(task)||string.IsNullOrEmpty(rubric)||s.Task.Digest!=task||s.Rubric.Digest!=rubric)
                throw new InvalidOperationException("The exact authored presenter document scope changed. No synthetic response was admitted.");
        }
        public Evaluate PrepareEvaluation(SessionSnapshot s,string work)
        {
            Validate(s);var admitted=marks.Where(m=>m.Work==work).ToArray();var actual=s.CurrentAnnotations.Where(a=>a.Content.SpecimenId==work).ToArray();var passage=s.Notebook.SingleOrDefault(p=>p.Reference.SourceId==work);
            if(admitted.Length!=4||actual.Length!=admitted.Length||passage==null||admitted.Any(m=>m.WorkSha256!=passage.Reference.Sha256)||actual.Any(a=>!admitted.Any(m=>m.Criterion==a.Content.CriterionId&&m.Marks==a.Content.Marks&&m.Rationale==a.Content.Rationale&&a.Content.SpecimenVersion==passage.Reference.Version)))
                throw new InvalidOperationException("The exact authored work, annotation or rationale scope changed. Findings are unavailable.");
            var sources=s.Notebook.Where(p=>p.Reference.SourceId==work||p.Reference.SourceId=="S_A"||p.Reference.SourceId=="S_B").Select(p=>p.Reference).ToArray();
            var findings=actual.Select(a=>{var m=admitted.Single(x=>x.Criterion==a.Content.CriterionId);return new CriterionFinding(a.Id,m.Criterion,m.Decision,sources,"Authored demonstration: "+m.Finding);}).ToArray();
            return new Evaluate(s.ExperimentRevision,work,FindingDecision.Supported,findings,FindingCoverage.Complete,actual.Select(a=>a.Id));
        }
        public PresenterDecisionScope DecisionScope(SessionSnapshot s,Guid request)
        {
            Validate(s);foreach(var work in new[]{"W_COMPARE","W_PITCH"})
            {
                PrepareEvaluation(s,work);
                if(!s.CurrentEvaluations.Any(e=>e.Content.SpecimenId==work&&e.Synthetic))throw new InvalidOperationException("Display both exact authored retests before the separate publication decision.");
            }
            return new PresenterDecisionScope(attempt,request,2,2,task,rubric,scenario,digest);
        }
        public string ReviewReason=>reason;
    }
}
