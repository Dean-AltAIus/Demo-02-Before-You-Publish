using System;
using System.Linq;
using Altaius.Demo02.Domain;
namespace Altaius.Demo02.Presentation
{
    public sealed class AttemptLifecycle
    {
        public OfflineSession Session { get; }public ConversationCoordinator Conversation { get; }
        public HelpCoordinator Help { get; }public LocalAuthority Authority { get; }
        public ConversationDispatchQueue Dispatch { get; }
        public bool Paused { get; private set; }public SessionSnapshot ReplaySnapshot { get; private set; }
        public AttemptLifecycle(OfflineSession session,ScenarioController controller)
        {Session=session;Conversation=new ConversationCoordinator(session);Dispatch=new ConversationDispatchQueue(Conversation);Help=new HelpCoordinator(session,controller);Authority=new LocalAuthority(session,controller);}
        public void Pause(bool value)=>Paused=value;
        public void Reset(bool linkedPractice)
        {Dispatch.Clear();Conversation.Reset(linkedPractice);Help.Reset();Authority.Reset(linkedPractice);ReplaySnapshot=null;Session.Reset(linkedPractice);Paused=false;}
        public SessionSnapshot EnterReplay()
        {ReplaySnapshot=Session.Snapshot;Session.EnterReplay();Paused=true;return ReplaySnapshot;}
        public static string Outcome(SessionSnapshot s)
        {if(s.ActiveRisks.Count>0)return "Active risk: "+s.ActiveRisks.Count+" recorded incident(s).";
            if(s.Publication==PublicationStatus.Released)return "Exact current-pair release recorded.";
            if(s.Publication==PublicationStatus.Held)return "Owned unresolved: a verified defect and actual owner acceptance are recorded.";
            if(s.Publication==PublicationStatus.HoldCommittedUnverified)return "Self-owned commitment recorded. Defect interpretation remains unverified.";
            return "Partial practice. No current approved release or verified accepted hold is recorded.";}
        public static string BoundedObservation(SessionSnapshot s)
        {
            var baseline=s.AnnotationHistory.Where(a=>a.Content.SpecimenId=="W_LIST"&&a.Pair.TaskVersion==1&&a.Pair.RubricVersion==1).GroupBy(a=>a.Content.CriterionId).Select(g=>g.Last()).ToArray();
            return baseline.Length==2&&baseline.Sum(a=>a.Content.Marks)==20?
                "Recorded: W_LIST received your full 20 draft marks under Task 1 / Rubric 1. Those marks are marking hypotheses. They do not establish that O2 evaluation was observed. Annotation IDs: "+string.Join(", ",baseline.Select(a=>a.Id.ToString())):
                "The most recent baseline W_LIST annotations do not form a complete 20-mark total. Earlier annotations, if any, remain in the timeline. No counterexample explanation or independent evaluation skill is inferred.";
        }
    }
}
