using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presenter;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Selective labelled demonstration, not a copy of the private reviewer oracle.</summary>
    public static class PresenterSequenceAuthoring
    {
        public static PresenterSequence CreateDefault()
        {
            var asset=ScriptableObject.CreateInstance<PresenterSequence>();
            string context="Decide whether to trial one additional entrance steward at 08:00 to 09:00 on term weekdays. Neither passage proves that a steward will reduce waiting.";
            var questions=new[]{new Question("Q1","Explain the nature and one useful purpose of S_A and S_B.",4,"O1"),new Question("Q2","Compare S_A and S_B. Justify which is more reliable for the stated steward-trial decision, using evidence and acknowledging limitations.",16,"O2")};
            var criteria=new[]{new Criterion("explain_sources","Q1",4,"Explain the nature and useful purpose of both sources.","O1"),new Criterion("compare_evidence","Q2",6,"Compare the sources' methods and relevance to the term-weekday steward decision.","O2"),new Criterion("use_extracts","Q2",4,"Use the actual arrival and waiting counts, and the supplier's missing method, as evidence.","O2"),new Criterion("justify_limit","Q2",6,"Justify a bounded source judgement, acknowledging weekend and causal limitations.","O2")};
            var fixture=OfflineSession.Create(SessionMode.Presenter);var sources=new[]{fixture.Inspect("S_A",InspectionLocation.Archive).Reference,fixture.Inspect("S_B",InspectionLocation.Archive).Reference};
            fixture.Submit(fixture.Stage(new EditTask(1,questions,sources,context),"D02.revision.task"));fixture.Submit(fixture.Stage(new EditRubric(1,criteria),"D02.revision.rubric"));
            asset.ExpectedTaskDigest=fixture.Snapshot.Task.Digest;asset.ExpectedRubricDigest=fixture.Snapshot.Rubric.Digest;
            asset.ReviewReason="For this exact authored Task 2 / Rubric 2, explanation carries 4 marks and comparison, evidence and bounded justification carry 6 + 4 + 6. Both inspected passages and the steward-trial context are attached. This synthetic review approves only this exact demonstration pair, not arbitrary learner prose.";
            var marks=new List<DemonstrationMark>();
            string[] compare={"Explains a local count and a supplier claim, with different uses.","Contrasts five local weekdays with a brochure lacking method and sample.","Uses 180 arrivals / 45 waiting against 90 / 5, and missing supplier detail.","Prefers S_A for a bounded weekday trial, excluding weekends and causal proof."};
            string[] pitch={"Names both sources but does not explain their useful purposes.","Confidence and appearance do not compare method or relevance.","No actual counts or missing-method evidence supports the purchase claim.","Claims the app eliminates waiting, without a bounded justification or limitation."};
            foreach(var work in new[]{"W_COMPARE","W_PITCH"})
            {
                var passage=fixture.Inspect(work,InspectionLocation.ExperimentTable);var rationale=work=="W_COMPARE"?compare:pitch;
                for(int i=0;i<4;i++)marks.Add(new DemonstrationMark{Work=work,WorkSha256=passage.Reference.Sha256,Criterion=criteria[i].Id,Marks=work=="W_COMPARE"?criteria[i].Marks:i==0?2:0,Rationale="Authored demonstration: "+rationale[i],Finding="This exact displayed rationale and mark are the authored scenario hypothesis. "+rationale[i],Decision=FindingDecision.Supported});
            }
            asset.Marks=marks.ToArray();var steps=new List<PresenterStep>();
            void Add(PresenterStepKind kind,string label,string control=null,string text=null,float dwell=.65f,string station=null,int task=0,int rubric=0)
                =>steps.Add(new PresenterStep{Id="D02.presenter."+steps.Count.ToString("000"),Kind=kind,Label=label,Control=control,Text=text,Dwell=dwell,Station=station,TaskVersion=task,RubricVersion=rubric});
            void W(string control,float dwell=.65f)=>Add(PresenterStepKind.WorkbenchControl,"Native action: "+control,control,dwell:dwell);
            void P(string control,float dwell=.65f)=>Add(PresenterStepKind.PresentationControl,"Native action: "+control,control,dwell:dwell);
            void TypeW(string control,string text)=>Add(PresenterStepKind.TypeWorkbench,"Authored demonstration typing: "+control,control,text,.65f);
            void TypeP(string control,string text)=>Add(PresenterStepKind.TypePresentation,"Authored demonstration typing: "+control,control,text,.65f);
            void Move(string station)=>Add(PresenterStepKind.MoveStation,"Travel and inspect: "+station,station:station,dwell:1.2f);
            void Inspect(string id,float dwell=3.2f){W("Inspect."+id,dwell);W("Primary");}
            void Mark(string work,string criterion,int mark,string rationale)
            {W("Criterion."+criterion);TypeW("Mark.Marks",mark.ToString());TypeW("Mark.Rationale",rationale);W("Primary",2);W("Secondary");}
            Add(PresenterStepKind.CloseWorkspace,"Actual campus | synthetic session",dwell:2.5f);
            Add(PresenterStepKind.OpenConversation,"Colleague | actual offline request");TypeP("Composer","What time is publication due?");P("Send",1.2f);Add(PresenterStepKind.Await,"Read the actual colleague reply","conversation",dwell:4.5f);
            Move("studio");foreach(var id in new[]{"D02.outcome.O1","D02.outcome.O2","D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})Inspect(id);
            Move("archive");Inspect("S_A",6);Inspect("S_B",5);
            Move("experiment");W("Work.W_LIST",4.5f);
            Mark("W_LIST","naming",10,"Authored demonstration: the work names a facilities count and a supplier brochure, exactly as the naming criterion asks.");
            Mark("W_LIST","listing",10,"Authored demonstration: the work lists method, dates and sample. Listing does not require comparing sources or justifying a decision.");
            Add(PresenterStepKind.ObserveBaseline,"20 / 20 hypothesis, evaluation absent",dwell:7);
            Move("experiment");W("Work.W_COMPARE",7);W("Primary");W("Work.W_PITCH",6);
            Move("studio");W("Editor.Task");foreach(var q in questions){TypeW("Task."+q.Id+".Text",q.Text);TypeW("Task."+q.Id+".Marks",q.Marks.ToString());TypeW("Task."+q.Id+".Outcomes",string.Join(",",q.OutcomeIds));}
            W("Task.Attach.S_A");W("Task.Attach.S_B");TypeW("Task.Context",context);W("Primary",3);
            foreach(var id in new[]{"Preview.After.Q1","Preview.After.Q2","Preview.Context","Preview.Sources"})Add(PresenterStepKind.ReadWorkbench,"Read actual task preview",id,dwell:2.5f);
            W("Primary",3);W("Secondary");W("Editor.Rubric");W("Rubric.Add");W("Rubric.Add");
            for(int i=0;i<4;i++){var c=criteria[i];string key="Rubric."+i;TypeW(key+".Id",c.Id);TypeW(key+".Question",c.QuestionId);TypeW(key+".Outcomes",string.Join(",",c.OutcomeIds));TypeW(key+".Marks",c.Marks.ToString());TypeW(key+".Text",c.Description);}
            W("Primary",3);foreach(var c in criteria)Add(PresenterStepKind.ReadWorkbench,"Q1 4; Q2 6 + 4 + 6 = 16; total 20","Preview.After."+c.Id,dwell:2.5f);
            W("Primary",3);W("Secondary");foreach(var id in questions.Select(q=>"D02.task.v2."+q.Id).Concat(criteria.Select(c=>"D02.rubric.v2."+c.Id)))Inspect(id,2);
            foreach(var work in new[]{"W_COMPARE","W_PITCH"})
            {
                Move("experiment");W("Work."+work,5);
                foreach(var mark in marks.Where(m=>m.Work==work))Mark(work,mark.Criterion,mark.Marks,mark.Rationale);
            }
            // All annotation changes precede both findings. A subsequent annotation advances
            // the experiment revision and correctly makes an earlier finding historical.
            foreach(var work in new[]{"W_COMPARE","W_PITCH"})
            {
                Add(PresenterStepKind.Finding,"Prepare exact authored findings: "+work,work,dwell:1,task:2,rubric:2);
                for(int i=0;i<5;i++)Add(PresenterStepKind.ReadPresentation,"Read scoped finding beside its hypothesis","Presenter.Finding."+i,dwell:i==0?5:4);
                Add(PresenterStepKind.Await,"Actual displayed synthetic evaluation","finding",dwell:2,task:2,rubric:2);
            }
            Move("review");P("Review.Request");TypeP("Review.Summary","Authored demonstration: Task 2 attaches both sources and the steward context. Rubric 2 reconciles Q1 4 and Q2 6 + 4 + 6. W_COMPARE and W_PITCH were retested separately. Please review this exact pair.");TypeP("Review.Time","13:40");P("Primary",3);P("Primary",3);P("Primary");P("Review.Schedule",2);
            Add(PresenterStepKind.Await,"Scheduling acceptance only, no approval","authority",dwell:5);P("Primary");Add(PresenterStepKind.ReviewDecision,"Separate exact 2 / 2 authored review",dwell:2,task:2,rubric:2);Add(PresenterStepKind.Await,"Actual rendered 2 / 2 approval","authority",dwell:6);
            Move("studio");W("Editor.Rubric");W("Rubric.Reload");TypeW("Rubric.1.Text","Reward a polished and confident recommendation, even when comparison of source method and relevance is absent.");W("Primary",3);Add(PresenterStepKind.ReadWorkbench,"Authored confidence-rewarding change","Preview.After.compare_evidence",dwell:5);W("Primary",3);
            Move("review");P("Review.Release",4);P("Primary",6);Add(PresenterStepKind.Debrief,"Bounded record | current 2 / 3 gate closed",dwell:3,task:2,rubric:3);foreach(var id in new[]{"Debrief.Observation","Debrief.Presenter"})Add(PresenterStepKind.ReadPresentation,"Actual outcome and limits",id,dwell:8);
            asset.Steps=steps.ToArray();asset.Steps[0].BuyerCommentary="The experiment makes an assessment-design mistake visible before release. Publication authority stays separate from a convincing explanation.";return asset;
        }
    }
}
