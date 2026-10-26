using E=Altaius.Demo02.Domain.EnglishCopy;
using System;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.Integration;
using Altaius.Demo02.Presentation;
using Altaius.Demo02.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Altaius.Demo02.Presenter
{
    [DefaultExecutionOrder(-25)]
    public sealed class PresenterRunner:MonoBehaviour,IPresenterControls
    {
        public PresentationView UI;public PresenterSequence Sequence;public Transform[] RouteNodes=Array.Empty<Transform>();
        public GameObject Dock;public Text Status,Commentary;public Button PauseButton,CancelButton,ResetButton,ExitButton,CommentaryButton;
        public bool Active {get;private set;} public bool Paused {get;private set;} public bool Completed {get;private set;}
        public int StepIndex {get;private set;} public float PresentedSeconds {get;private set;} public float TravelledMetres {get;private set;}
        public string LastError {get;private set;} public Guid AttemptId {get;private set;}
        public PresenterStep CurrentStep=>frozen&&StepIndex<frozen.Steps.Length?frozen.Steps[StepIndex]:null;
        PresenterSequence frozen,source;PresenterScenarioScope scope;PresenterInputLease lease;CampusSessionState original;PresenterWorkspaceBookmark bookmark;
        Vector3 originalPosition,lastPosition;Quaternion originalFacing;float yaw,pitch,timeScale,elapsed,typed,phraseWait;bool originalMotion,direct,entered,actionDone,showCommentary,bindings;
        string digest,identity,taskDigest,rubricDigest,fieldExpected;int world,epoch;InputField typingField;PresenterRoute route;bool shuttingDown;
        AuthorityReply authority;CommandReceipt findingReceipt;Guid findingDelivery;bool findingAcknowledged;
        CampusGameplayFacade Game=>UI.Gameplay;SessionSnapshot S=>UI.Host.Session.Snapshot;
        void Start(){BindButtons();if(Dock)Dock.SetActive(false);}
        void BindButtons()
        {
            if(bindings)return;bindings=true;
            if(PauseButton)PauseButton.onClick.AddListener(()=>{if(Paused)Resume();else Pause();});
            if(CancelButton)CancelButton.onClick.AddListener(Cancel);if(ResetButton)ResetButton.onClick.AddListener(ResetRun);if(ExitButton)ExitButton.onClick.AddListener(Exit);
            if(CommentaryButton)CommentaryButton.onClick.AddListener(()=>{showCommentary=!showCommentary;RefreshDock();});
        }
        public void ShowBrief()=>UI.ShowPresenterBrief(()=>StartSequence(Sequence,false),()=>StartSequence(Sequence,true),UI.OpenTitle);
        public void StartSequence(PresenterSequence sequence,bool useDirectRoutes)
        {
            if(!UI||!sequence)throw new InvalidOperationException(E.Get("presenterrunner.startsequence.startsequence.message.bind.the.saved.presenter.sequence.and","Bind the saved presenter sequence and native campus UI."));
            if(Game.IsComposing)throw new InvalidOperationException(E.Get("presenterrunner.startsequence.startsequence.message.finish.text.composition.before.starting.the","Finish text composition before starting the walkthrough."));
            if(Active)Cancel();BindButtons();original=UI.Host.CaptureState();bookmark=UI.CapturePresenterWorkspace();originalPosition=Game.Motor.transform.position;originalFacing=Game.Motor.CharacterVisual?Game.Motor.CharacterVisual.rotation:Quaternion.identity;yaw=Game.CameraRig.Yaw;pitch=Game.CameraRig.Pitch;timeScale=Time.timeScale;originalMotion=Game.CameraRig.ReducedMotion;
            source=sequence;digest=source.Digest;frozen=Instantiate(sequence);frozen.hideFlags=HideFlags.HideAndDontSave;
            direct=useDirectRoutes||UI.Applied?.ReducedMotion==true;showCommentary=frozen.BuyerCommentary;StepIndex=0;PresentedSeconds=TravelledMetres=0;LastError=null;Completed=Paused=false;ResetStep();epoch++;
            if(direct)Game.CameraRig.ReducedMotion=true;
            UI.EnterPresenterSession();AttemptId=S.AttemptId;scope=new PresenterScenarioScope(AttemptId,frozen);Game.PresenterOwnsInput=true;Game.DriveMovement(Vector2.zero);lease=new PresenterInputLease(UI);Active=true;lastPosition=Game.Motor.transform.position;
            if(Dock){Dock.SetActive(true);Dock.transform.SetAsLastSibling();}Checkpoint();RefreshDock();
        }
        void Update()=>Tick(Time.unscaledDeltaTime);
        float dockScale=-1,dockCanvasWidth=-1;
        void LateUpdate(){if(!Active||!UI||!UI.Workbench)return;if(Mathf.Abs(dockScale-UI.Workbench.TextScale)>.01f||Mathf.Abs(dockCanvasWidth-((RectTransform)UI.Workbench.HudHeader.parent).rect.width)>1)RefreshDock();UI.Workbench.WorldHelp.gameObject.SetActive(false);UI.TalkLauncher.gameObject.SetActive(false);}
        public void Tick(float deltaTime)
        {
            if(!Active)return;
            try
            {
                lease?.Apply();AcceptOwnedAuthorityChange();
                if(source.Digest!=digest||S.AttemptId!=AttemptId||S.Mode!=SessionMode.Presenter||S.Task.Digest!=taskDigest||S.Rubric.Digest!=rubricDigest||S.WorldRevision!=world||UI.NativeSurfaceIdentity!=identity)
                    throw new InvalidOperationException(E.Get("presenterrunner.tick.tick.message.the.authored.sequence.document.attempt.or","The authored sequence, document, attempt or native surface changed. Walkthrough stopped safely."));
                if(typingField&&typingField.text!=fieldExpected)throw new InvalidOperationException(E.Get("presenterrunner.tick.tick.message.the.automated.native.draft.changed.outside","The automated native draft changed outside its input driver."));
                if(Game.IsComposing){Pause();return;}if(Paused||Completed){Game.DriveMovement(Vector2.zero);return;}
                var displacement=Game.Motor.transform.position-lastPosition;displacement.y=0;TravelledMetres+=displacement.magnitude;lastPosition=Game.Motor.transform.position;
                PresentedSeconds+=Mathf.Max(0,deltaTime);var step=CurrentStep;if(step==null){Completed=true;Game.DriveMovement(Vector2.zero);RefreshDock();return;}
                if(step.TaskVersion>0&&S.Task.Version!=step.TaskVersion||step.RubricVersion>0&&S.Rubric.Version!=step.RubricVersion)throw new InvalidOperationException(E.Get("presenterrunner.tick.tick.message.the.presenter.step.s.required.version","The presenter step's required version guard changed."));
                float dt=Mathf.Max(0,deltaTime)*Mathf.Max(.1f,frozen.Pace);
                if(!entered){entered=true;Enter(step);Checkpoint();lease?.Apply();}
                elapsed+=dt;
                if(step.Kind==PresenterStepKind.TypePresentation||step.Kind==PresenterStepKind.TypeWorkbench)Type(step,dt);
                else if(step.Kind==PresenterStepKind.MoveStation&&!actionDone)
                {
                    if(route.Tick(Mathf.Max(.001f,deltaTime)))
                    {Game.DriveMovement(Vector2.zero);if(!Game.TryInspectNearest()||Game.ActiveStation.StationId!=step.Station)throw new InvalidOperationException(E.Get("presenterrunner.tick.tick.message.actual.range.inspection.did.not.reach","Actual range inspection did not reach the intended saved station."));actionDone=true;elapsed=0;Checkpoint();}
                    else if(elapsed>90*frozen.Pace)throw new InvalidOperationException(E.Get("presenterrunner.tick.tick.message.movement.did.not.reach.the.saved","Movement did not reach the saved station. Check the collision route."));
                }
                else if(step.Kind==PresenterStepKind.Await&&!actionDone)
                {actionDone=Ready(step.Control);if(elapsed>45)throw new InvalidOperationException(E.Get("presenterrunner.tick.tick.message.the.actual.rendered.reply.was.not","The actual rendered reply was not delivered. No synthetic authority was inferred."));}
                if(actionDone&&elapsed>=step.Dwell){StepIndex++;ResetStep();}
                RefreshDock();
            }
            catch(Exception error){LastError=error.Message;Cancel();UI.ShowPresenterStopNotice(E.Format("presenterrunner.tick.walkthrough.stopped.safely.lasterror.status.walkthrough.stopped.safely","Walkthrough stopped safely: {lasterror}","lasterror",LastError));}
        }
        void Enter(PresenterStep step)
        {
            switch(step.Kind)
            {
                case PresenterStepKind.Dwell:actionDone=true;break;
                case PresenterStepKind.WorkbenchControl:Require(UI.Workbench.ActivateControl(step.Control),step.Control);actionDone=true;break;
                case PresenterStepKind.PresentationControl:
                    Require(UI.ActivateControl(step.Control),step.Control);authority=UI.LastAuthorityReply;
                    if(step.Control=="Send")authority=null;actionDone=true;break;
                case PresenterStepKind.OpenConversation:UI.OpenConversation(step.Role);actionDone=true;break;
                case PresenterStepKind.TypePresentation:case PresenterStepKind.TypeWorkbench:
                    typingField=step.Kind==PresenterStepKind.TypePresentation?UI.FindPresenterField(step.Control):UI.Workbench.Rows.Find(step.Control)?.Input;
                    if(!typingField)throw new InvalidOperationException(E.Format("presenterrunner.enter.enter.message.the.expected.native.field.is.unavailable","The expected native field is unavailable: {step_control}","step_control",step.Control));SetText(step,"");break;
                case PresenterStepKind.MoveStation:
                    CloseWorkspace();var station=Game.Stations.Single(s=>s.StationId==step.Station);
                    if(direct){Require(Game.DirectRoute(step.Station),step.Station);lastPosition=Game.Motor.transform.position;actionDone=true;}
                    else route=new PresenterRoute(Game,RouteNodes,station);break;
                case PresenterStepKind.CloseWorkspace:CloseWorkspace();actionDone=true;break;
                case PresenterStepKind.ObserveBaseline:
                    UI.ShowPresenterObservation(E.Get("presenterrunner.enter.w.list.full.mark.counterexample.heading.w.list.full.mark.counterexample","W_LIST | full-mark counterexample"),E.Format("presenterrunner.enter.w.list.full.mark.counterexample.body.20.saved.authored.hypothesis.under.task","{s_drafttotal_w_list} / 20 saved authored hypothesis under Task {s_task_version} / Rubric {s_rubric_version}.\nCurrent evaluations: {s_currentevaluations}. Naming and listing do not require comparison or a justified decision. O2 evaluation remains unobserved.\nAnnotation receipts: {value8}","s_drafttotal_w_list",S.DraftTotal("W_LIST"),"s_task_version",S.Task.Version,"s_rubric_version",S.Rubric.Version,"s_currentevaluations",S.CurrentEvaluations.Count,"value8",string.Join(", ",S.CurrentAnnotations.Select(a=>a.Id.ToString()))));actionDone=true;break;
                case PresenterStepKind.Finding:ShowFinding(step.Control);actionDone=true;break;
                case PresenterStepKind.ReviewDecision:
                    var request=S.ReviewHistory.Last();authority=UI.Lifecycle.Authority.PreparePresenterDecision(scope.DecisionScope(S,request.Id),ReviewDecision.Approved,scope.ReviewReason);UI.ShowAuthority(authority);actionDone=true;break;
                case PresenterStepKind.Await:actionDone=Ready(step.Control);break;
                case PresenterStepKind.Debrief:UI.OpenDebrief();actionDone=true;break;
                case PresenterStepKind.ReadWorkbench:
                    var row=UI.Workbench.Rows.Find(step.Control);Require(row,step.Control);EventSystem.current?.SetSelectedGameObject(null);UI.Workbench.Focus.EnsureVisible(row.gameObject);actionDone=true;break;
                case PresenterStepKind.ReadPresentation:Require(UI.ReadPresenterControl(step.Control),step.Control);actionDone=true;break;
            }
        }
        void Type(PresenterStep step,float deltaTime)
        {
            if(actionDone)return;if(EventSystem.current&&EventSystem.current.currentSelectedGameObject!=typingField.gameObject)throw new InvalidOperationException(E.Get("presenterrunner.type.type.message.native.typing.focus.changed.the.draft","Native typing focus changed. The draft was preserved."));
            if(phraseWait>0){phraseWait-=deltaTime;return;}typed+=deltaTime*Mathf.Max(1,step.CharactersPerSecond);int count=Mathf.Min(step.Text?.Length??0,Mathf.FloorToInt(typed));
            if(count>fieldExpected.Length)
            {
                int boundary=-1;for(int i=fieldExpected.Length;i<count;i++)if(".,;:!?".IndexOf(step.Text[i])>=0){boundary=i+1;break;}
                if(boundary>0){count=boundary;typed=count;phraseWait=step.PhrasePause;}
                SetText(step,step.Text.Substring(0,count));
            }
            if(count==(step.Text?.Length??0)){actionDone=true;elapsed=0;}
        }
        void SetText(PresenterStep step,string text)
        {Require(step.Kind==PresenterStepKind.TypePresentation?UI.SetFieldText(step.Control,text):UI.Workbench.SetFieldText(step.Control,text),step.Control);fieldExpected=text;typingField.ForceLabelUpdate();}
        void ShowFinding(string work)
        {
            var command=scope.PrepareEvaluation(S,work);var envelope=UI.Host.Session.Stage(command,WorkbenchModel.OpportunityForWork(work));int ownerEpoch=epoch;findingReceipt=null;findingAcknowledged=false;findingDelivery=Guid.NewGuid();
            var content=new[]{E.Format("presenter.finding.source","{passage}\nSource {source} v1 | SHA-256 {sha}\nTask 2 / Rubric 2 | submitted {submitted}","passage",S.Notebook.Single(p=>p.Reference.SourceId==work).DisplayText,"source",work,"sha",S.Notebook.Single(p=>p.Reference.SourceId==work).Reference.Sha256,"submitted",string.Join(", ",command.SubmittedAnnotationIds))}.Concat(command.Findings.Select(f=>{var a=S.CurrentAnnotations.Single(x=>x.Id==f.AnnotationId);var c=S.Rubric.Criteria.Single(x=>x.Id==f.CriterionId);return E.Format("presenter.finding.detail","{criterion} | {mark} / {maximum} authored marking hypothesis\nCriterion: {description}\nDisplayed rationale: {rationale}\nScoped finding: {decision}. {reason}","criterion",c.Id,"mark",a.Content.Marks,"maximum",c.Marks,"description",c.Description,"rationale",a.Content.Rationale,"decision",f.Decision,"reason",f.Reason);})).ToArray();
            UI.ShowPresenterFinding(work,content,findingDelivery,()=>
            {
                if(!Active||Paused||epoch!=ownerEpoch||findingAcknowledged)return;
                try{scope.Validate(S);findingAcknowledged=true;findingReceipt=UI.Host.TrustedScenarioController.SubmitSyntheticEvaluation(envelope,ActorRole.ExperimentReviewer);Require(findingReceipt.Status==ReceiptStatus.Accepted,findingReceipt.ReasonCode);UI.Host.NotifyChanged();Checkpoint();}
                catch(Exception error){LastError=error.Message;Cancel();}
            });
        }
        bool Ready(string condition)
        {
            if(condition=="authority")return authority!=null&&authority.Displayed&&authority.Receipt?.Status==ReceiptStatus.Accepted;
            if(condition=="finding")return findingReceipt?.Status==ReceiptStatus.Accepted;
            if(condition=="conversation"){var reply=UI.Lifecycle.Conversation.History.LastOrDefault();return reply!=null&&(reply.State==ReplyState.Shown||reply.State==ReplyState.Unavailable);}
            throw new InvalidOperationException(E.Format("presenterrunner.ready.ready.message.unknown.presenter.guard","Unknown presenter guard: {condition}","condition",condition));
        }
        void AcceptOwnedAuthorityChange()
        {
            if(S.WorldRevision==world)return;
            var receipt=authority?.Receipt;if(authority?.Displayed==true&&receipt?.Status==ReceiptStatus.Accepted&&receipt.BeforeRevision==world&&receipt.AfterRevision==S.WorldRevision&&S.Task.Digest==taskDigest&&S.Rubric.Digest==rubricDigest)world=S.WorldRevision;
        }
        void Checkpoint(){identity=UI.NativeSurfaceIdentity;world=S.WorldRevision;taskDigest=S.Task.Digest;rubricDigest=S.Rubric.Digest;}
        void CloseWorkspace(){UI.Workbench.CloseWindow();UI.CloseSurface(false);Game.DriveMovement(Vector2.zero);EventSystem.current?.SetSelectedGameObject(null);}
        void ResetStep(){entered=actionDone=false;elapsed=typed=phraseWait=0;typingField=null;fieldExpected=null;route=null;}
        public void Pause(){if(!Active||Paused)return;Paused=true;Game.DriveMovement(Vector2.zero);UI.Lifecycle.Pause(true);RefreshDock();}
        public void Resume(){if(!Active||!Paused)return;Paused=false;UI.Lifecycle.Pause(false);if(typingField){typingField.Select();typingField.ActivateInputField();}RefreshDock();}
        public void ResetRun(){if(!Active)return;var again=source;bool useDirect=direct;Cancel();StartSequence(again,useDirect);}
        public void Exit(){Cancel();if(UI)UI.OpenTitle();}
        public void Cancel()
        {
            if(!Active)return;Active=false;Paused=false;epoch++;Game.DriveMovement(Vector2.zero);UI.InvalidatePresenterDeliveries();UI.Lifecycle.Reset(false);lease?.Dispose();lease=null;Game.ReleasePresenter();Time.timeScale=timeScale;
            UI.Host.RestoreState(original);Game.Motor.GoTo(originalPosition,originalFacing);Game.CameraRig.Yaw=yaw;Game.CameraRig.Pitch=pitch;Game.CameraRig.ReducedMotion=originalMotion;Game.CameraRig.SnapFollow();UI.RestorePresenterWorkspace(bookmark);if(Dock)Dock.SetActive(false);if(Commentary){Commentary.gameObject.SetActive(false);var backing=Commentary.GetComponent<PresenterCaptionBacking>();if(backing&&backing.Backing)backing.Backing.SetActive(false);}
            if(frozen){if(Application.isPlaying)Destroy(frozen);else DestroyImmediate(frozen);}frozen=null;scope=null;ResetStep();
        }
        void RefreshDock()
        {
            LayoutDock();string label=((Completed)?E.Get("presenterrunner.refreshdock.assign.string.label.complete.return.to.your.practice.complete.return.to.your.practice","Complete. Return to your practice."):((Paused)?E.Get("presenterrunner.refreshdock.assign.string.label.paused.draft.and.step.retained.paused.draft.and.step.retained.paused.draft.and.step.retained","Paused. Draft and step retained."):((CurrentStep?.Label != null)?CurrentStep?.Label:E.Get("presenter.starting","Starting"))));if(label.Length>39)label=label.Substring(0,36)+"...";
            if(Status)Status.text=E.Format("presenterrunner.refreshdock.assign.status.text.synthetic.authored.offline.no.learner.evidence","SYNTHETIC / AUTHORED / OFFLINE\n{label}\n{value3}No learner evidence","label",label,"value3",((Paused||Completed)?E.Format("presenterrunner.refreshdock.assign.status.text.value3.s.m.s.m","{presentedseconds_0_0}s | {travelledmetres_0_0}m | ","presentedseconds_0_0",PresentedSeconds.ToString("0.0"),"travelledmetres_0_0",TravelledMetres.ToString("0.0")):""));
            if(Status&&UI.Workbench.TextScale>1)Status.text=((Paused)?E.Get("presenterrunner.refreshdock.assign.status.text.authored.offline.paused.draft.retained.authored.offline.paused.draft.retained.no","AUTHORED / OFFLINE\nPaused. Draft retained.\nNo learner evidence"):E.Get("presenterrunner.refreshdock.assign.status.text.authored.offline.no.evidence.authored.offline.no.evidence","AUTHORED / OFFLINE | no evidence"));
            if(PauseButton)PauseButton.GetComponentInChildren<Text>().text=((Paused)?E.Get("presenterrunner.refreshdock.assign.text.resume.resume","Resume"):E.Get("presenter.pause","Pause"));
            if(Commentary){bool shown=showCommentary&&!string.IsNullOrEmpty(CurrentStep?.BuyerCommentary)&&typingField==null&&!UI.IsOpen;Commentary.gameObject.SetActive(shown);var backing=Commentary.GetComponent<PresenterCaptionBacking>();if(backing&&backing.Backing)backing.Backing.SetActive(shown);Commentary.text=E.Format("presenterrunner.refreshdock.assign.commentary.text.buyer.commentary","Buyer commentary\n{currentstep_buyercommentary}","currentstep_buyercommentary",CurrentStep?.BuyerCommentary);}
        }
        void LayoutDock()
        {
            if(!Dock||!Status)return;var rect=(RectTransform)Dock.transform;bool panel=UI.IsOpen,large=UI.Workbench.TextScale>1;float scale=UI.Workbench.TextScale;
            dockScale=scale;dockCanvasWidth=((RectTransform)UI.Workbench.HudHeader.parent).rect.width;float left=Mathf.Max(large?.56f:.30f,(UI.Workbench.LeenaDock.anchoredPosition.x+UI.Workbench.LeenaDock.rect.width+16)/dockCanvasWidth);
            if(Commentary){Commentary.fontSize=Mathf.RoundToInt(13*scale);Commentary.horizontalOverflow=HorizontalWrapMode.Wrap;Commentary.verticalOverflow=VerticalWrapMode.Overflow;var caption=Commentary.rectTransform;caption.anchorMin=caption.anchorMax=caption.pivot=new Vector2(0,1);caption.anchoredPosition=new Vector2(24,large?-182:-76);caption.sizeDelta=new Vector2(large?Mathf.Min(570,dockCanvasWidth-48):310,0);caption.sizeDelta=new Vector2(caption.sizeDelta.x,Mathf.Max(48,Commentary.preferredHeight+8));Commentary.GetComponent<PresenterCaptionBacking>()?.Fit();}
            var controls=new[]{PauseButton,CancelButton,ResetButton,ExitButton,CommentaryButton};
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-14,12);rect.sizeDelta=new Vector2(large?Mathf.Min(640,dockCanvasWidth-370):550,large?128+64*scale:96);
            float width=rect.rect.width-20;Place(Status.rectTransform,10,6,width,large?96:42);Status.fontSize=Mathf.RoundToInt(12*scale);
            var widths=new[]{62f,64,62,52,116};float cursor=10;
            for(int i=0;i<controls.Length;i++)if(controls[i]){controls[i].gameObject.SetActive(true);var text=controls[i].GetComponentInChildren<Text>(true);text.fontSize=Mathf.RoundToInt(12*scale);text.rectTransform.offsetMin=new Vector2(3,0);text.rectTransform.offsetMax=new Vector2(-3,0);float bw=large?(width-12)/3:widths[i];float x=large?10+(i%3)*(bw+6):cursor;float y=large?108+(i/3)*(32*scale+6):54;Place((RectTransform)controls[i].transform,x,y,bw,32*scale);cursor+=bw+6;}
            if(!panel)UI.Workbench.WorldHelp.gameObject.SetActive(false);
            if(PauseButton)PauseButton.GetComponentInChildren<Text>().color=new Color(1,.97f,.87f);
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        static void Require(bool valid,string control){if(!valid)throw new InvalidOperationException(E.Format("presenterrunner.require.require.message.the.expected.native.control.or.response","The expected native control or response is unavailable: {control}","control",control));}
        void OnApplicationQuit(){shuttingDown=true;AbandonDestroyedWorkspace();}
        void AbandonDestroyedWorkspace()
        {
            if(!Active)return;Active=false;Paused=false;epoch++;Time.timeScale=timeScale;lease=null;scope=null;
            if(frozen){if(Application.isPlaying)Destroy(frozen);else DestroyImmediate(frozen);}frozen=null;
        }
        void OnDisable()
        {
            if(!Active)return;
            if(shuttingDown||!UI||!UI.Gameplay||!UI.Gameplay.CameraRig||!UI.Gameplay.Motor||!UI.Workbench)AbandonDestroyedWorkspace();
            else Cancel();
        }
        void OnDestroy(){OnDisable();}
    }
}
