using E=Altaius.Demo02.Domain.EnglishCopy;
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
        {if(s.ActiveRisks.Count>0)return E.Format("attemptlifecycle.outcome.return.active.risk.recorded.incident.s","Active risk: {s_activerisks} recorded incident(s).","s_activerisks",s.ActiveRisks.Count);
            if(s.Publication==PublicationStatus.Released)return E.Get("attemptlifecycle.outcome.return.exact.current.pair.release.recorded","Exact current-pair release recorded.");
            if(s.Publication==PublicationStatus.Held)return E.Get("attemptlifecycle.outcome.return.owned.unresolved.a.verified.defect.and","Owned unresolved: a verified defect and actual owner acceptance are recorded.");
            if(s.Publication==PublicationStatus.HoldCommittedUnverified)return E.Get("attemptlifecycle.outcome.return.self.owned.commitment.recorded.defect.interpretation","Self-owned commitment recorded. Defect interpretation remains unverified.");
            return E.Get("attemptlifecycle.outcome.return.partial.practice.no.current.approved.release","Partial practice. No current approved release or verified accepted hold is recorded.");}
        public static string BoundedObservation(SessionSnapshot s)
        {
            var baseline=s.AnnotationHistory.Where(a=>a.Content.SpecimenId=="W_LIST"&&a.Pair.TaskVersion==1&&a.Pair.RubricVersion==1).GroupBy(a=>a.Content.CriterionId).Select(g=>g.Last()).ToArray();
            return ((baseline.Length==2&&baseline.Sum(a=>a.Content.Marks)==20)?E.Format("attemptlifecycle.boundedobservation.return.authored.demonstration.w.list.received.under.task.1.rubric.1.those","{value0} under Task 1 / Rubric 1. Those marks are marking hypotheses. They do not establish that O2 evaluation was observed. Annotation IDs: {value2}","value0",((s.Mode==SessionMode.Presenter)?E.Get("attemptlifecycle.boundedobservation.return.authored.demonstration.w.list.received.value0.authored.demonstration.w.list.received.authored.demonstration.w.list.received.full","Authored demonstration: W_LIST received full 20 draft marks"):E.Get("attemptlifecycle.boundedobservation.return.authored.demonstration.w.list.received.value0.recorded.w.list.received.your.recorded.w.list.received.your.full","Recorded: W_LIST received your full 20 draft marks")),"value2",string.Join(", ",baseline.Select(a=>a.Id.ToString()))):E.Get("attemptlifecycle.boundedobservation.return.the.most.recent.baseline.w.the.most.recent.baseline.w.list","The most recent baseline W_LIST annotations do not form a complete 20-mark total. Earlier annotations, if any, remain in the timeline. No counterexample explanation or independent evaluation skill is inferred."));
        }
    }
}
