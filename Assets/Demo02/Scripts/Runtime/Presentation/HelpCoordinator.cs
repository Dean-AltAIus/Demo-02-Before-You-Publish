using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
namespace Altaius.Demo02.Presentation
{
    public sealed class HelpDelivery
    {
        public Guid Id { get; }=Guid.NewGuid();public Guid AttemptId { get; internal set; }
        public int WorldRevision { get; internal set; }
        public string OpportunityId { get; internal set; }public string Text { get; internal set; }
        public bool Strategic { get; internal set; }public bool Requested { get; internal set; }
        public bool Available { get; internal set; }public bool Shown { get; internal set; }
    }
    public sealed class HelpCoordinator
    {
        readonly OfflineSession session;readonly ScenarioController controller;readonly List<HelpDelivery> pending=new List<HelpDelivery>();
        public HelpCoordinator(OfflineSession session,ScenarioController controller){this.session=session;this.controller=controller;}
        public HelpDelivery Request(HelpIntent intent,string opportunityId,string ownText="")
        {
            if(session.Snapshot.Mode==SessionMode.Replay)throw new InvalidOperationException("Replay is read-only.");
            var s=session.Snapshot;var help=new HelpDelivery{AttemptId=s.AttemptId,WorldRevision=s.WorldRevision,OpportunityId=opportunityId,Requested=true,Available=true};
            if(intent==HelpIntent.Controls)help.Text="WASD moves with world focus. Hold right mouse to look. E inspects. Tab opens direct routes. Enter or Space activates a button. Escape goes back. Text fields keep your own draft. Preview, save and publish are separate actions.";
            else if(intent==HelpIntent.RecordedAttempt)help.Text="This attempt records "+s.Notebook.Count+" inspected passages, "+s.AnnotationHistory.Count+" saved marking hypotheses and "+s.Audit.Count+" command records. Current grounded evaluations: "+s.CurrentEvaluations.Count+". Review and assistance IDs are in the timeline. These records do not establish competence.";
            else if(s.CurrentAnnotations.Count>0&&string.IsNullOrWhiteSpace(ownText))
            {help.Strategic=true;help.Text="What does the work you marked actually demonstrate under the criterion you chose? Compare the actual passage with that criterion before deciding your next action.";}
            else{help.Available=false;help.Text="A grounded strategy response to your request is unavailable in this offline adapter. Your own question is retained. You can continue independently or inspect your recorded attempt.";}
            pending.Add(help);return help;
        }
        public bool Acknowledge(HelpDelivery delivery,bool inputBusy=false)
        {
            if(delivery==null||!pending.Contains(delivery)||delivery.AttemptId!=session.Snapshot.AttemptId||delivery.WorldRevision!=session.Snapshot.WorldRevision||delivery.Shown||inputBusy)return false;
            if(!delivery.Available){delivery.Shown=true;return true;}
            var accepted=controller.DeliverAssistance(delivery.AttemptId,delivery.Id,delivery.OpportunityId,delivery.Text,delivery.Strategic,delivery.Requested,inputBusy:inputBusy);
            if(accepted)delivery.Shown=true;return accepted;
        }
        public void Reset()=>pending.Clear();
        public static string AssistanceSummary(SessionSnapshot s)=>s.AssistanceHistory==AssistanceHistory.Unavailable?
            "Assistance history is unavailable. Independent or no-help status cannot be verified.":s.LinkedFromAttemptId!=null?
            "Linked practice retains historical assistance and is assisted practice.":s.Assistance.Count==0?
            "Complete local ledger: no help has been shown.":s.Assistance.Count+" actual help deliveries recorded; "+s.Assistance.Count(a=>a.Strategic)+" strategic. See delivery and opportunity IDs in the timeline.";
    }
}
