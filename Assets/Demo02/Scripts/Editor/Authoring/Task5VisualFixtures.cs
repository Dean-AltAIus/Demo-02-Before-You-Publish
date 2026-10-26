using System;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit synthetic Play-only art fixtures using real reducer receipts. Never learner controls.</summary>
    public static class Task5VisualFixtures
    {
        public static string ShowReview(string state)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Disposable synthetic Play fixture only.");
            var ui=UnityEngine.Object.FindFirstObjectByType<PresentationView>();var host=ui.Host;host.BeginSession(SessionMode.Presenter);ui.BindLifecycle();
            var session=host.Session;var controller=host.TrustedScenarioController;
            Action<DomainCommand> submit=c=>{var receipt=session.Submit(session.Stage(c,"Task5.synthetic.visual.fixture"));if(receipt.Status!=ReceiptStatus.Accepted)throw new InvalidOperationException(receipt.ReasonCode);};
            Func<DomainCommand,Guid> role=c=>{var envelope=session.Stage(c,"Task5.synthetic.visual.fixture");var receipt=controller.SubmitScripted(envelope,controller.PermitExactScriptedResponse(envelope,ActorRole.ModuleLead,"Synthetic visual fixture of the exact receipt, no semantic grade."));if(receipt.Status!=ReceiptStatus.Accepted)throw new InvalidOperationException(receipt.ReasonCode);return receipt.RequestId;};
            if(state!="locked")
            {
                var sources=new[]{session.Inspect("S_A",InspectionLocation.Archive).Reference,session.Inspect("S_B",InspectionLocation.Archive).Reference};
                submit(new EditTask(1,new[]{new Question("Q1","Explain each source.",4,"O1"),new Question("Q2","Select a source for the steward trial, contrasting its evidence and limitations.",16,"O2")},sources,"Should we trial a morning steward?"));
                var criteria=new[]{new Criterion("nature","Q1",4,"Explain each source's nature and use.","O1"),new Criterion("decision","Q2",16,"Contrast evidence for this decision and justify with a limitation.","O2")};
                submit(new EditRubric(1,criteria));
                submit(new RequestReview(2,2,"Synthetic exact-pair visual fixture, no semantic grade.","13:40"));var request=session.Snapshot.ReviewHistory.Last().Id;
                if(state!="pending")
                {
                    role(new ScheduleReply(request,2,2,"13:40",ScheduleDecision.Accepted,Guid.NewGuid()));
                    var approval=role(new DecideReview(request,2,2,state=="returned"?ReviewDecision.Returned:ReviewDecision.Approved,"Synthetic visual fixture. No learner competence claim."));
                    if(state=="released")submit(new RequestRelease(2,2,approval));
                    if(state=="stale")submit(new EditRubric(2,criteria));
                    if(state=="hold")
                    {
                        submit(new RequestHold("Own unresolved interpretation for visual fixture",ActorRole.Learner,"14:10","Recheck the exact current passages."));
                        var hold=session.Snapshot.HoldHistory.Last();submit(new AcceptHold(hold.Id,2,2,"14:10",true,true));
                    }
                }
            }
            host.NotifyChanged();ui.OpenReview();Canvas.ForceUpdateCanvases();
            return "SYNTHETIC ART FIXTURE | "+session.Snapshot.ReviewStatus+" | "+session.Snapshot.Publication+" | records="+session.Snapshot.Audit.Count+" | evidence="+session.Snapshot.Evidence.Count;
        }
    }
}
