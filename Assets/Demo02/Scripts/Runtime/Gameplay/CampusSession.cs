using System;
using Altaius.Demo02.Domain;
using UnityEngine;

namespace Altaius.Demo02.Gameplay
{
    [DefaultExecutionOrder(-100)]
    public sealed class CampusSession : MonoBehaviour
    {
        public SessionMode InitialMode = SessionMode.Independent;
        public OfflineSession Session { get; private set; }
        public WorkbenchModel Workbench { get; private set; }
        // Composition-only port for the later authored scenario controller. Never wired to learner controls.
        public ScenarioController TrustedScenarioController { get; private set; }
        public event Action Changed;
        void Awake() { if (Session == null) BeginSession(InitialMode); }
        public void BeginSession(SessionMode mode)
        {
            Session = OfflineSession.Create(mode, out var controller); TrustedScenarioController = controller;
            Workbench = new WorkbenchModel(Session); NotifyChanged();
        }
        public void NotifyChanged() => Changed?.Invoke();
        public void RebindWorkbench() { Workbench = new WorkbenchModel(Session); NotifyChanged(); }
        public CampusSessionState CaptureState()=>new CampusSessionState(Session,Workbench,TrustedScenarioController);
        public void RestoreState(CampusSessionState state)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            Session=state.Session;Workbench=state.Workbench;TrustedScenarioController=state.Controller;NotifyChanged();
        }
    }
    public sealed class CampusSessionState
    {
        public OfflineSession Session {get;} public WorkbenchModel Workbench {get;} public ScenarioController Controller {get;}
        internal CampusSessionState(OfflineSession session,WorkbenchModel workbench,ScenarioController controller)
        {Session=session;Workbench=workbench;Controller=controller;}
    }
}
