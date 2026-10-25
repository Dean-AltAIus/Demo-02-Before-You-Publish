using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.Integration;
using Altaius.Demo02.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    /// <summary>Saved native system surfaces. Composition owns offline responses; text never grants authority.</summary>
    public sealed partial class PresentationView : MonoBehaviour,ICampusWorkspace
    {
        public CampusSession Host;public CampusGameplayFacade Gameplay;public WorkbenchView Workbench;
        public GameObject Overlay;public PresentationSurface[] Surfaces;
        public Button TalkLauncher,CoachLauncher;public DisconnectedIntegrationSettings Integration;
        public bool ShowTitleOnStart=true;
        public string SurfaceId { get; private set; }="world";
        public AttemptLifecycle Lifecycle { get; private set; }
        public LocalPresentationPreferences Applied { get; private set; }
        public LocalPresentationPreferences Staged { get; private set; }
        public bool IsOpen=>Overlay&&Overlay.activeSelf||Workbench.IsOpen;
        public ActorRole ConversationRole { get; private set; }=ActorRole.Colleague;
        public string ReviewSummary="",ReviewTime="",HoldDefect="",HoldTime="",HoldNextAction="",HelpDraft="";
        public ActorRole HoldOwner=ActorRole.Learner;public HoldDefectBasis HoldBasis=HoldDefectBasis.Unverified;
        public CommandEnvelope Pending { get; private set; }
        public AuthorityReply LastAuthorityReply { get; private set; }
        PresentationSurface current;SessionMode selectedMode=SessionMode.Independent;
        string settingsCaller="pause",confirmationKind="",confirmationReturn="review";int settingsTab;
        bool suspendedWorkbench;bool conversationRefresh;GameObject suspendedFocus;
        float previousAudioVolume;bool previousAudioPause;
        string lastEditedControl;
        readonly List<RenderedRowDelivery> deliveries=new List<RenderedRowDelivery>();
        SessionSnapshot S=>Host.Session.Snapshot;
        float Scale=>Applied?.TextScale??1;
        void Start()
        {
            previousAudioVolume=AudioListener.volume;previousAudioPause=AudioListener.pause;
            Applied=LocalPresentationPreferences.Load();BindLifecycle();
            Workbench.PauseRequested+=Pause;Workbench.ReviewRequested+=OpenReview;
            TalkLauncher.onClick.AddListener(()=>OpenConversation(ConversationRole));CoachLauncher.onClick.AddListener(OpenCoach);
            Canvas.willRenderCanvases+=OnCanvasRender;
            foreach(var screen in Surfaces)
            {
                var target=screen;target.Close.onClick.AddListener(Back);
                if(target.SurfaceId=="conversation")
                {target.Send.onClick.AddListener(SendConversation);target.CancelRequest.onClick.AddListener(CancelOrInspectConversation);
                    for(int i=0;i<target.TabButtons.Length;i++){int index=i;target.TabButtons[i].onClick.AddListener(()=>OpenConversation(RoleAt(index)));}}
                if(target.SurfaceId=="coach")
                {target.Send.onClick.AddListener(()=>RequestHelp(HelpIntent.RequestedStrategy,true));target.CancelRequest.onClick.AddListener(()=>{HelpDraft="";target.ComposerDraft.BindInput("Write your own help request...","",v=>HelpDraft=v,Scale,true);});
                    for(int i=0;i<target.TabButtons.Length;i++){var intent=(HelpIntent)i;target.TabButtons[i].onClick.AddListener(()=>RequestHelp(intent));}}
                if(target.SurfaceId=="settings")for(int i=0;i<target.TabButtons.Length;i++){int index=i;target.TabButtons[i].onClick.AddListener(()=>ShowSettingsTab(index));}
            }
            ApplyPreferences(false);if(ShowTitleOnStart)OpenTitle();else CloseSurface();
        }
        void OnDestroy()
        {Canvas.willRenderCanvases-=OnCanvasRender;if(Workbench){Workbench.PauseRequested-=Pause;Workbench.ReviewRequested-=OpenReview;}AudioListener.volume=previousAudioVolume;AudioListener.pause=previousAudioPause;}
        public void BindLifecycle()=>Lifecycle=new AttemptLifecycle(Host.Session,Host.TrustedScenarioController);
        void Update()
        {
            if(Lifecycle==null)return;
            if(Workbench.TextScale!=Scale&&!(Overlay&&Overlay.activeSelf)){Applied.TextScale=Workbench.TextScale;}
            if(Lifecycle.Dispatch.Tick(Time.unscaledTime,Lifecycle.Paused)&&SurfaceId=="conversation")RenderConversation();
            if(conversationRefresh&&SurfaceId=="conversation"){conversationRefresh=false;UpdateConversationStatus();}
            Workbench.LeenaStatus.text=Workbench.TextScale>1.5f?"Offline | Voice unavailable":"Offline | "+(SurfaceId=="coach"?"Help open":"Ask Leena")+" | Voice unavailable";
            TalkLauncher.gameObject.SetActive(!IsOpen); // The persistent coach anchor remains visible on every gameplay surface.
        }
        void OnCanvasRender()
        {
            if(!Application.isPlaying||Lifecycle==null||Lifecycle.Paused)return;
            foreach(var d in deliveries.ToArray())
            {
                if(!d.Matches){deliveries.Remove(d);continue;}
                if(!d.Text.gameObject.activeInHierarchy||d.Text.canvasRenderer.cull||d.Help&&(Gameplay.Typing||Gameplay.IsComposing||Lifecycle.Dispatch.PendingCount>0))continue;
                var scroll=d.Text.GetComponentInParent<ScrollRect>();
                if(scroll)
                {var b=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,d.Text.rectTransform);if(!scroll.viewport.rect.Overlaps(new Rect(b.min,b.size)))continue;}
                deliveries.Remove(d);d.TryCommit(true);
            }
        }
        void AfterRendered(Text body,Guid identity,Action commit,bool help=false)=>deliveries.Add(new RenderedRowDelivery(body,current,identity,commit,help));
        PresentationSurface Begin(string id,string heading)
        {
            if(Gameplay.IsComposing)throw new InvalidOperationException("Finish composing before changing this surface.");
            if(Workbench.IsOpen){suspendedWorkbench=true;suspendedFocus=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;if(!suspendedFocus||!suspendedFocus.transform.IsChildOf(Workbench.Window.transform))suspendedFocus=Workbench.Rows.Find(Workbench.LastEditedControl)?.Input?.gameObject;Workbench.Window.SetActive(false);}
            SurfaceId=id;Overlay.SetActive(true);current=Surfaces.Single(s=>s.SurfaceId==id);
            current.BeginBinding();
            foreach(var screen in Surfaces)screen.gameObject.SetActive(screen==current);
            current.Heading.text=heading;current.Status.text="";current.Rows.Begin();
            foreach(var b in new[]{current.Primary,current.Secondary,current.Tertiary}){b.gameObject.SetActive(false);b.onClick.RemoveAllListeners();b.interactable=true;}
            current.Scroll.verticalNormalizedPosition=1;current.Close.gameObject.SetActive(id!="title"&&id!="loading");
            if(current.SideText)current.SideText.text="Task "+S.Task.Version+"\nRubric "+S.Rubric.Version+"\n\n"+S.Publication+"\n\n"+LocalAuthority.Time(S.ClockMinute);
            Workbench.HudHeader.gameObject.SetActive(false);Workbench.WorldHelp.gameObject.SetActive(false);EventSystem.current?.SetSelectedGameObject(null);return current;
        }
        NativeRow Text(string id,string heading,string body)
        {var row=current.Rows.Take(NativeRowKind.Text,id);row.BindText(heading,WorkbenchView.Display(body),Scale);return row;}
        NativeRow Action(string id,string caption,Action action,bool enabled=true)
        {var row=current.Rows.Take(NativeRowKind.Action,id);row.BindAction(caption,()=>Guard(action),Scale,enabled);return row;}
        NativeRow Input(string id,string label,string value,Action<string> changed,bool multiline=false)
        {var row=current.Rows.Take(NativeRowKind.Input,id);row.BindInput(label,value,v=>{lastEditedControl=id;changed(v);},Scale,multiline);return row;}
        void Button(Button button,string label,Action action)
        {button.gameObject.SetActive(action!=null);button.GetComponentInChildren<Text>().text=label;button.onClick.RemoveAllListeners();if(action!=null)button.onClick.AddListener(()=>Guard(action));}
        void Finish(string status="")
        {current.Status.text=WorkbenchView.Display(status);current.ApplyScale(Scale);Canvas.ForceUpdateCanvases();var first=current.Primary.gameObject.activeSelf?current.Primary:current.GetComponentsInChildren<Selectable>().FirstOrDefault(s=>s.IsActive()&&s.IsInteractable()&&s!=current.Close);if(first)EventSystem.current?.SetSelectedGameObject(first.gameObject);}
        public void Guard(Action action)
        {if(Gameplay.IsComposing){if(current)current.Status.text="Finish composing before submitting.";return;}try{action();}catch(Exception ex){if(current)current.Status.text=WorkbenchView.Display(ex.Message);}}
        public void OpenTitle()
        {
            Lifecycle.Pause(false);Begin("title","Before You Publish");
            Text("Title.Intro","Fictional practice","Inspect, mark, revise and seek exact-pair review or an owned hold.");
            if(PresenterControls!=null)Action("Title.Presenter","Watch the authored gameplay walkthrough",PresenterControls.ShowBrief);
            Button(current.Primary,"Begin practice",OpenBrief);Button(current.Secondary,"Settings",()=>OpenSettings("title"));Button(current.Tertiary,"Exit",()=>ConfirmLifecycle("exit"));Finish("Authored offline. English available. Arabic and voice unavailable.");
        }
        public void OpenBrief()
        {
            Begin("brief","Before you begin");Text("Brief.Fiction","This is a fictional practice scenario.","Your actual actions and delivered assistance are retained for this local debrief. No real student data is used. This rehearsal does not measure student learning or universal teaching competence.");
            Action("Mode.Independent",(selectedMode==SessionMode.Independent?"Selected: ":"")+"Independent",()=>{selectedMode=SessionMode.Independent;OpenBrief();});
            Action("Mode.Coached",(selectedMode==SessionMode.Coached?"Selected: ":"")+"Coached",()=>{selectedMode=SessionMode.Coached;OpenBrief();});
            if(PresenterControls!=null)Action("Mode.Presenter","Authored synthetic presenter walkthrough",PresenterControls.ShowBrief);
            Text("Brief.Modes","Choose how to practice","Independent: strategy only when you request it. Coached: source-bounded assistance may be offered within the assistance limits. Controls support is always optional.");
            Button(current.Primary,"Continue",BeginPractice);Button(current.Secondary,"Back",OpenTitle);Finish("Help is optional. Opening or requesting help is different from actual delivery.");
        }
        public void BeginPractice()
        {
            Begin("loading","Preparing local practice");Text("Loading.State","Checking the local session","Only the admitted local catalogue is loaded. No network service is being contacted.");Button(current.Secondary,"Cancel",OpenBrief);Finish("Preparing a fresh temporary attempt.");
            StartCoroutine(StartLocalPractice());
        }
        System.Collections.IEnumerator StartLocalPractice()
        {
            yield return null;if(SurfaceId!="loading")yield break;
            Lifecycle.Reset(false);Host.BeginSession(selectedMode);BindLifecycle();ClearDrafts();CloseSurface(false);OpenOnramp();
        }
        public void OpenOnramp()
        {
            Begin("onramp","Optional controls");
            var help=Lifecycle.Help.Request(HelpIntent.Controls,"D02.controls");
            var helpRow=Text("Onramp.Keys","Controls you can reopen",help.Text);
            Text("Onramp.Step1","Inspect a selected folio","E opens the selected work. You can return without changing it. Open the studio and inspect a current version label.");
            Text("Onramp.Step2","Open an empty annotation","At the experiment table, inspect work and an inspected criterion. Your own explanation stays editable.");
            Text("Onramp.Step3","Stage a mark, then cancel","Staging is reversible. Saving and publishing are different actions. These controls carry no assessment credit.");
            Action("Onramp.Studio","Open studio controls",()=>{CloseSurface(false);Gameplay.DirectRoute("studio");});
            Button(current.Primary,"Continue to campus",ReturnToCampus);Button(current.Secondary,"Skip controls",ReturnToCampus);Finish("You can reopen controls from Leena at any time.");
            AfterRendered(helpRow.Body,help.Id,()=>{Lifecycle.Help.Acknowledge(help);Host.NotifyChanged();},true);
        }
        public void OpenConversation(ActorRole role)
        {
            ConversationRole=role;Begin("conversation",RoleName(role));current.ComposerDraft.BindInput("Write in your own words...",Lifecycle.Conversation.Draft(role),v=>{lastEditedControl="Composer";Lifecycle.Conversation.SetDraft(ConversationRole,v);},Scale,true);RenderConversation();
        }
        static ActorRole RoleAt(int i)=>i==0?ActorRole.Colleague:i==1?ActorRole.ModuleLead:ActorRole.ExperimentReviewer;
        public static string RoleName(ActorRole role)=>role==ActorRole.Colleague?"Colleague":role==ActorRole.ModuleLead?"Module reviewer":"Experiment reviewer";
        void RenderConversation()
        {
            current.Rows.Begin();var records=Lifecycle.Conversation.History.Where(r=>r.Request.Recipient==ConversationRole).ToArray();
            if(records.Length==0)Text("Conversation.Scope",RoleName(ConversationRole)+" | authored offline","Ask in your own words. Source-bounded authored replies are available for role, inspected sources, publication time and current approval. Unsupported questions receive an unavailable reply. Conversation cannot grant approval.");
            foreach(var record in records.Skip(Math.Max(0,records.Length-1)))
            {
                Text("Conversation.Own."+record.Request.Id,S.Mode==SessionMode.Presenter?"Authored presenter input":"You",record.Request.OwnText);
                if(record.Reply!=null&&record.State!=ReplyState.Cancelled&&record.State!=ReplyState.Stale)
                {var row=Text("Conversation.Reply."+record.Request.Id,RoleName(ConversationRole),record.Reply.Text);if(record.State==ReplyState.Ready){var captured=record;var displayId=Guid.NewGuid();AfterRendered(row.Body,displayId,()=>{Lifecycle.Conversation.Acknowledge(new DisplayAcknowledgement(captured.Request.AttemptId,captured.Request.Id,displayId));conversationRefresh=true;});}}
                if(record.Error!=null)Text("Conversation.Error."+record.Request.Id,"Preserved request",record.Error);
            }
            foreach(var b in new[]{current.Primary,current.Secondary,current.Tertiary})b.gameObject.SetActive(false);
            Finish();UpdateConversationStatus();Canvas.ForceUpdateCanvases();current.Scroll.verticalNormalizedPosition=1;
        }
        void UpdateConversationStatus()
        {
            var last=Lifecycle.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ConversationRole);
            current.Status.text=(last==null?"Draft only":last.State+". Draft retained")+" | Task "+S.Task.Version+" / Rubric "+S.Rubric.Version+" | Offline. Voice unavailable.";
            bool waiting=last!=null&&(last.State==ReplyState.Sending||last.State==ReplyState.Ready);
            current.Send.interactable=!waiting&&S.Mode!=SessionMode.Replay;current.CancelRequest.interactable=last!=null;current.CancelRequest.GetComponentInChildren<Text>().text=waiting?"Cancel":"Receipt";
        }
        void CancelOrInspectConversation()
        {
            var record=Lifecycle.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ConversationRole);if(record==null)return;
            if(record.State==ReplyState.Sending||record.State==ReplyState.Ready){CancelConversation();return;}
            Begin("receipt","Conversation receipt");Text("Conversation.Detail.State",RoleName(record.Request.Recipient)+" | "+record.State,"Original request "+record.Request.Id+"\nAttempt "+record.Request.AttemptId+"\nTask "+record.Request.Pair.TaskVersion+" / Rubric "+record.Request.Pair.RubricVersion+" | world revision "+record.Request.WorldRevision+"\nTask SHA-256 "+record.Request.Pair.TaskDigest+"\nRubric SHA-256 "+record.Request.Pair.RubricDigest+"\nDisplay acknowledgment "+(record.DisplayId?.ToString()??"Not recorded"));
            Text("Conversation.Detail.Words","Your original words",record.Request.OwnText);Text("Conversation.Detail.Reply","Actual reply and provenance",(record.Reply?.Text??"No reply displayed.")+"\n"+(record.Reply?.Provenance??"No response received."));
            Button(current.Primary,"Back to conversation",()=>OpenConversation(ConversationRole));Button(current.Secondary,"View timeline",OpenTimeline);Finish("Conversation receipts remain separate from domain review, evaluation and release authority.");
        }
        public void SendConversation()
        {
            Guard(()=>{if(Lifecycle.Paused)throw new InvalidOperationException("Resume before sending.");var record=Lifecycle.Conversation.Send(ConversationRole,current.Composer.text);Lifecycle.Dispatch.Schedule(record,Time.unscaledTime+.35f);RenderConversation();});
        }
        public void CancelConversation()
        {
            var record=Lifecycle.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ConversationRole);
            if(record!=null)Lifecycle.Dispatch.Cancel(new CancelRequest(record.Request.AttemptId,record.Request.Id));RenderConversation();current.Composer.Select();current.Composer.ActivateInputField();
        }
        public void OpenCoach()
        {
            Begin("coach","Leena | AI coach");current.ComposerDraft.BindInput("Write your own help request...",HelpDraft,v=>{lastEditedControl="Composer";HelpDraft=v;},Scale,true);
            Text("Coach.Scope","Source-bounded help","Controls, review of your recorded attempt and requested strategy are separate. Leena cannot approve publication, reveal hidden sources or prefill a successful repair.");
            Text("Coach.History","Actual delivered help",HelpCoordinator.AssistanceSummary(S));
            Action("Coach.Onramp","Reopen optional controls",OpenOnramp);
            Finish("Offline. Voice unavailable. Free-text strategy responses may be unavailable.");
        }
        public void RequestHelp(HelpIntent intent,bool freeText=false)
        {
            var help=Lifecycle.Help.Request(intent,CurrentOpportunity(),freeText?HelpDraft:"");
            current.Rows.Begin();var row=Text("Coach.Reply",help.Available?"Leena | requested help":"Help unavailable",help.Text);
            Text("Coach.Identity","Delivery scope","Delivery "+help.Id+"\nOpportunity "+help.OpportunityId+"\n"+(help.Strategic?"Strategic help, attributed only when displayed.":"Controls or recorded facts. No competence claim."));
            Action("Coach.Onramp","Reopen controls",OpenOnramp);Finish("Requested. Actual rendering is acknowledged separately.");
            AfterRendered(row.Body,help.Id,()=>{if(Lifecycle.Help.Acknowledge(help)){Host.NotifyChanged();current.Status.text=help.Available?"Help shown. Delivery recorded: "+help.Id:"Unavailable response shown. No strategy supplied.";}},true);
        }
        string CurrentOpportunity()=>Workbench.SelectedWork!=null?WorkbenchModel.OpportunityForWork(Workbench.SelectedWork):LocalAuthority.PublicationOpportunity;
        public void OpenReview()
        {
            Begin("review","Review and publication");Text("Review.Pair","Current Task "+S.Task.Version+" / Rubric "+S.Rubric.Version,"Task SHA-256 "+S.Task.Digest+"\nRubric SHA-256 "+S.Rubric.Digest+"\nWorld revision "+S.WorldRevision+" | Clock revision "+S.ClockRevision);
            Text("Review.Gate","Publication: "+S.Publication,"Review: "+S.ReviewStatus+". Current approval: "+(S.CurrentApproval?.ReceiptId.ToString()??"Not recorded")+". The cabinet needs a release receipt for this exact current pair.");
            Action("Review.Request","Request an exact-pair review time",OpenReviewRequest);Action("Review.Conversation","Talk to module reviewer",()=>OpenConversation(ActorRole.ModuleLead));
            var latest=S.ReviewHistory.LastOrDefault();if(latest!=null)
            {Text("Review.Original","Original review request",latest.Id+" | "+latest.Status+" | "+LocalAuthority.Time(latest.Minute)+"\n"+latest.ChangeSummary+"\n"+(latest.DecisionReason??"No decision reason recorded."));
                Action("Review.Schedule","Check local scheduling response",()=>ShowAuthority(Lifecycle.Authority.PrepareScheduling(latest.Id)),latest.Status==ReviewStatus.Pending);
                Action("Review.Decision","Request separate review decision",()=>ShowAuthority(Lifecycle.Authority.PrepareReviewDecision(latest.Id)),latest.Status==ReviewStatus.Scheduled);}
            Action("Review.Release","Preview current-pair release"+(S.CurrentApproval==null?" | current approval missing":""),()=>PreviewDomain(Lifecycle.Authority.StageRelease(),"release"));
            Action("Review.Hold","Propose an owned hold",OpenHold);Action("Review.Timeline","Inspect authority and receipt history",OpenTimeline);
            var hold=S.HoldHistory.LastOrDefault();if(hold!=null){Text("Hold.Current","Hold proposal "+hold.Id,hold.Proposal.Defect+"\nOwner "+hold.Proposal.Owner+" | Review "+hold.Proposal.ReviewAt+"\nNext action: "+hold.Proposal.NextAction+"\nAcceptance "+(hold.Accepted?.ToString()??"Not recorded")+" | Defect verified "+hold.DefectVerified+" | Self-owned "+hold.SelfOwned);
                if(hold.Accepted==null)Action("Hold.Accept",hold.Proposal.Owner==ActorRole.Learner?"Confirm my self-owned commitment":"Check actual owner response",()=>{if(hold.Proposal.Owner==ActorRole.Learner)PreviewDomain(Lifecycle.Authority.StageSelfAcceptance(hold.Id),"self hold");else ShowAuthority(Lifecycle.Authority.PrepareOwnerAcceptance(hold.Id));});}
            Button(current.Primary,"Back to campus",ReturnToCampus);Button(current.Secondary,"Debrief",OpenDebrief);Finish("Scheduling, review decisions and release are separate receipts. Arbitrary semantic review is unavailable offline.");
        }
        public void OpenReviewRequest()
        {
            Begin("reviewRequest","Request review time");Text("Schedule.Clock","Simulation time "+LocalAuthority.Time(S.ClockMinute),"Review window: "+LocalAuthority.Time(Mathf.Max(S.ClockMinute,820))+" to 14:00. Publication due 14:00. A proposal does not guarantee a slot or advance time.");
            Input("Review.Summary",S.Mode==SessionMode.Presenter?"Authored demonstration change summary":"Your change summary",ReviewSummary,v=>ReviewSummary=v,true);Input("Review.Time","Requested time (HH:MM)",ReviewTime,v=>ReviewTime=v);
            Button(current.Primary,"Preview request",()=>PreviewDomain(Lifecycle.Authority.StageReview(ReviewSummary,ReviewTime),"review"));Button(current.Secondary,"Keep draft",OpenReview);Finish("The current pair and exact readset will be captured in the preview.");
        }
        public void OpenHold()
        {
            Begin("hold","Hold proposal");Input("Hold.Defect","Specific defect, in your own words",HoldDefect,v=>HoldDefect=v,true);
            Action("Hold.Owner","Owner: "+HoldOwner+" | Change",()=>{HoldOwner=HoldOwner==ActorRole.Learner?ActorRole.ModuleLead:HoldOwner==ActorRole.ModuleLead?ActorRole.Colleague:ActorRole.Learner;OpenHold();});
            Input("Hold.Time","Future same-day review time (HH:MM)",HoldTime,v=>HoldTime=v);Input("Hold.NextAction","Owner's next action",HoldNextAction,v=>HoldNextAction=v,true);
            Action("Hold.Basis","Defect basis: "+HoldBasis+" | Change",()=>{HoldBasis=HoldBasis==HoldDefectBasis.Unverified?HoldDefectBasis.MissingSourceAttachments:HoldBasis==HoldDefectBasis.MissingSourceAttachments?HoldDefectBasis.MissingDecisionContext:HoldBasis==HoldDefectBasis.MissingDecisionContext?HoldDefectBasis.PerQuestionMarksMismatch:HoldDefectBasis.Unverified;OpenHold();});
            Text("Hold.BasisScope","Structural checks use actual anchors","A structural basis requires inspected current task/rubric passages and an actual matching defect. Free defect prose remains unverified. Proposal, owner acceptance and release are different states.");
            Button(current.Primary,"Preview proposal",()=>PreviewDomain(Lifecycle.Authority.StageHold(HoldDefect,HoldOwner,HoldTime,HoldNextAction,HoldBasis),"hold"));Button(current.Secondary,"Keep draft",OpenReview);Finish("No owner acceptance has been inferred.");
        }
        void PreviewDomain(CommandEnvelope envelope,string kind)
        {
            Pending=envelope;confirmationKind=kind;Begin("confirmation",kind=="self hold"?"Confirm self-ownership":"Confirm "+kind);
            if(envelope.Command is RequestReview review)Text("Confirm.Change","Review proposal",review.ChangeSummary+"\nRequested "+review.ReviewBy+". A request does not schedule or approve.");
            if(envelope.Command is RequestHold hold)Text("Confirm.Change","Hold proposal",hold.Defect+"\nOwner "+hold.Owner+" | "+hold.ReviewAt+"\nNext action: "+hold.NextAction+"\nBasis "+hold.Basis+" | inspected anchors "+hold.Anchors.Count);
            if(kind=="self hold")Text("Confirm.Change","Your explicit commitment","I accept the stated next action and future time as the learner owner. This is self-owned, not an NPC response. Unverified defect prose remains unverified.");
            if(kind=="release")Text("Confirm.Change","Release this exact pair",S.CurrentApproval==null?"Current approval is missing. Confirming submits an actual release request to the gate. The cabinet stays closed unless this exact pair has accepted authority.":"This will request publication using the current exact approval receipt. A changed pair or stale readset will reject the action.");
            Text("Confirm.Scope","Exact Task "+S.Task.Version+" / Rubric "+S.Rubric.Version,"Request "+envelope.CommandId+"\nAttempt "+envelope.AttemptId+"\nWorld revision "+envelope.ExpectedWorldRevision+"\nTask SHA-256 "+S.Task.Digest+"\nRubric SHA-256 "+S.Rubric.Digest);
            Button(current.Primary,kind=="self hold"?"I confirm my commitment":"Confirm "+kind,CommitDomain);Button(current.Secondary,"Keep editing",()=>{if(kind=="hold")OpenHold();else if(kind=="review")OpenReviewRequest();else OpenReview();});Finish("Confirming submits this original staged request once. Changed content needs an explicit fresh preview.");
        }
        public void CommitDomain()
        {
            if(Pending==null)throw new InvalidOperationException("Preview the request first.");var receipt=Lifecycle.Authority.Confirm(Pending);Host.NotifyChanged();
            Begin("receipt",receipt.Status==ReceiptStatus.Accepted?"Request receipt":"Request needs attention");
            Text("Receipt.Status",receipt.Action+" | "+receipt.Status,receipt.ReasonCode+"\nRequest "+receipt.RequestId+"\nRevision "+receipt.BeforeRevision+" to "+receipt.AfterRevision+"\nAuthority "+receipt.Authority+"\n"+DisplayDisclosure(receipt));
            Text("Receipt.Outcome","Actual current state",AttemptLifecycle.Outcome(S));Button(current.Primary,receipt.Status==ReceiptStatus.Accepted?"Review and publication":"Return to preserved draft",receipt.Status==ReceiptStatus.Accepted?(Action)OpenReview:ReturnDomainDraft);Button(current.Secondary,"View timeline",OpenTimeline);Finish(receipt.Status==ReceiptStatus.Accepted?"Accepted operational record. No semantic or competence claim.":"Your draft and original request are retained. Check this receipt before a fresh preview.");
        }
        void ReturnDomainDraft()
        {
            if(confirmationKind=="hold")OpenHold();else if(confirmationKind=="review")OpenReviewRequest();else{OpenReview();return;}
            string field=lastEditedControl;
            if(confirmationKind=="hold")field=string.IsNullOrWhiteSpace(HoldDefect)?"Hold.Defect":string.IsNullOrWhiteSpace(HoldTime)?"Hold.Time":string.IsNullOrWhiteSpace(HoldNextAction)?"Hold.NextAction":field;
            if(confirmationKind=="review")field=string.IsNullOrWhiteSpace(ReviewSummary)?"Review.Summary":string.IsNullOrWhiteSpace(ReviewTime)?"Review.Time":field;
            var row=current.Rows.Find(field);if(row&&row.Input){row.Input.Select();row.Input.ActivateInputField();current.Focus.EnsureVisible(row.Input.gameObject);}
        }
        public void ShowAuthority(AuthorityReply reply)
        {
            LastAuthorityReply=reply;Begin("receipt",reply.Status);var row=Text("Authority.Reply",RoleName(reply.AuthorityRole)+" | actual local response",reply.Text);
            Text("Authority.Identity","Scoped offline reply",reply.Provenance+"\nOriginal request "+reply.OriginalRequestId+"\nReply "+reply.Id+"\nAttempt "+reply.AttemptId);
            Button(current.Primary,"Review and publication",OpenReview);Button(current.Secondary,"View timeline",OpenTimeline);Finish("Response prepared. Role authority is admitted only after actual display.");
            AfterRendered(row.Body,reply.Id,()=>{Lifecycle.Authority.Acknowledge(reply);Host.NotifyChanged();if(current&&SurfaceId=="receipt"){current.Heading.text=reply.Status;if(current.SideText)current.SideText.text="Task "+S.Task.Version+"\nRubric "+S.Rubric.Version+"\n\n"+S.Publication+"\n\n"+LocalAuthority.Time(S.ClockMinute);current.Status.text=reply.Status+". "+reply.Provenance;}});
        }
        public void Pause()
        {
            if(PresenterControls?.Active==true){PresenterControls.Pause();return;}
            Lifecycle.Pause(true);Begin("pause","Paused");
            Text("Pause.State","Local practice is paused","Reading, typing and pausing do not advance the logical clock. Pending local responses wait. Your drafts and original request IDs stay in this attempt.");
            Action("Pause.Settings","Settings",()=>OpenSettings("pause"));Action("Pause.Debrief","View practice record",OpenDebrief);Action("Pause.Linked","Start linked practice",()=>ConfirmLifecycle("linked"));Action("Pause.Reset","Full reset",()=>ConfirmLifecycle("reset"));Action("Pause.Exit","Leave the simulation",()=>ConfirmLifecycle("exit"));
            Button(current.Primary,"Resume",Resume);Finish("Simulation time "+LocalAuthority.Time(S.ClockMinute)+". No countdown or time grade.");
        }
        public void Resume(){Lifecycle.Pause(false);CloseSurface();}
        public void ConfirmLifecycle(string kind)
        {
            Pending=null;confirmationKind=kind;confirmationReturn=SurfaceId;Begin("confirmation",kind=="linked"?"Start linked practice?":kind=="exit"?"Leave the simulation?":"Start again?");
            Text("Reset.Explanation",kind=="linked"?"New attempt linked to this practice":kind=="exit"?"Your current attempt ends":"Clear this attempt and linked practice",kind=="linked"?
                "Your original history and actual assistance remain read-only. Current marks, approvals, drafts and pending responses do not become new results. This is assisted rehearsal, not an unseen transfer test.":
                "Dialogue, drafts, evidence and linked retry data will be cleared. Pending replies cannot change the new attempt. Anonymous presentation settings remain local.");
            Button(current.Primary,kind=="linked"?"Start linked practice":kind=="exit"?"Leave":"Start a new attempt",()=>ResetAttempt(kind));Button(current.Secondary,"Keep playing",()=>{if(confirmationReturn=="title")OpenTitle();else Pause();});Finish("This action requires your explicit confirmation.");
        }
        public void ResetAttempt(string kind)
        {
            Gameplay.ReleasePresenter();Lifecycle.Reset(kind=="linked");Host.RebindWorkbench();ClearDrafts();suspendedWorkbench=false;Workbench.CloseWindow();
            if(kind=="exit")OpenTitle();else{CloseSurface(false);OpenOnramp();}
        }
        void ClearDrafts()
        {ReviewSummary=ReviewTime=HoldDefect=HoldTime=HoldNextAction=HelpDraft="";HoldOwner=ActorRole.Learner;HoldBasis=HoldDefectBasis.Unverified;Pending=null;LastAuthorityReply=null;suspendedFocus=null;deliveries.Clear();foreach(var screen in Surfaces)if(screen.Composer)screen.Composer.SetTextWithoutNotify("");}
        public void OpenDebrief()
        {
            Begin("debrief","Practice record");Text("Debrief.Outcome","Actual outcome",AttemptLifecycle.Outcome(S));Text("Debrief.Observation","Observed action and consequence",AttemptLifecycle.BoundedObservation(S)+"\nSemantic evaluation of arbitrary prose is unavailable offline. No measured learning or universal competence claim is available.");Text("Debrief.Assistance","Actual assistance",HelpCoordinator.AssistanceSummary(S));
            if(S.Mode==SessionMode.Presenter)
            {
                var approval=S.ApprovalHistory.LastOrDefault();var release=S.Audit.LastOrDefault(a=>a.Receipt.Action=="release.request");
                Text("Debrief.Presenter","Exact authored run records","Current Task "+S.Task.Version+" / Rubric "+S.Rubric.Version+". Current approval: "+(S.CurrentApproval==null?"Not recorded":S.CurrentApproval.ReceiptId.ToString())+".\nHistorical approval: "+(approval==null?"Not recorded":"Task "+approval.Pair.TaskVersion+" / Rubric "+approval.Pair.RubricVersion+" | "+approval.ReceiptId)+".\nLast release request: "+(release==null?"Not attempted":release.Receipt.RequestId+" | "+release.Receipt.Status+" | "+release.Receipt.ReasonCode)+".\nPublication: "+S.Publication+". Accepted hold: "+(S.HoldHistory.Any(h=>h.Accepted==true)?"Recorded":"Not recorded")+". Learner evidence records: "+S.Evidence.Count+".\nThe shown repair, hypotheses and scoped findings are authored demonstration content. Seek review of the current pair before release.");
            }
            Button(current.Primary,"View timeline",OpenTimeline);Button(current.Secondary,"Linked practice",()=>ConfirmLifecycle("linked"));Button(current.Tertiary,"Back",()=>{if(Lifecycle.Paused)Pause();else CloseSurface();});Finish(S.Mode==SessionMode.Presenter?"Synthetic presenter records. No learner evidence.":"This debrief describes only the recorded local attempt.");
        }
        public void OpenTimeline()
        {
            Begin("timeline","Read-only timeline");Text("Timeline.Scope","Current attempt "+S.AttemptId,"Mode "+S.Mode+" | "+AttemptLifecycle.Outcome(S)+"\nThis view cannot create marks, approvals or evidence.");
            foreach(var a in S.Audit)Text("Timeline.Command."+a.Sequence,"Command "+a.Sequence+" | "+a.Receipt.Action,a.Receipt.Status+" | "+a.Receipt.ReasonCode+"\nReceipt "+a.Receipt.RequestId+"\n"+DisplayDisclosure(a.Receipt));
            foreach(var r in S.ReviewHistory)Text("Timeline.Review."+r.Id+"."+r.Revision,"Review | "+r.Status,"Task "+r.Pair.TaskVersion+" / Rubric "+r.Pair.RubricVersion+" | Request "+r.Id+"\n"+(r.DecisionReason??r.ChangeSummary)+"\nDecision receipt "+(r.DecisionReceiptId?.ToString()??"Not recorded"));
            foreach(var reply in Lifecycle.Authority.LinkedReplies.Concat(Lifecycle.Authority.Replies))Text("Timeline.RoleReply."+reply.Id,RoleName(reply.AuthorityRole)+" | "+reply.Status,reply.Text+"\nOriginal request "+reply.OriginalRequestId+"\nReply "+reply.Id+" | Attempt "+reply.AttemptId+"\nActually displayed "+reply.Displayed+"\n"+reply.Provenance);
            foreach(var h in S.HoldHistory)Text("Timeline.Hold."+h.Id+"."+h.Revision,"Hold | acceptance "+(h.Accepted?.ToString()??"Not recorded"),h.Id+"\n"+h.Proposal.Defect+"\nOwner "+h.Proposal.Owner+" | "+h.Proposal.ReviewAt+" | "+h.Proposal.NextAction+"\nSelf-owned "+h.SelfOwned+" | Verified defect "+h.DefectVerified);
            foreach(var h in S.Assistance)Text("Timeline.Help."+h.Id,"Help actually delivered",h.Id+"\nOpportunity "+h.OpportunityId+" | Strategic "+h.Strategic+" | Requested "+h.Requested+"\n"+h.Text);
            foreach(var r in Lifecycle.Conversation.LinkedHistory.Concat(Lifecycle.Conversation.History))Text("Timeline.Conversation."+r.Request.Id,RoleName(r.Request.Recipient)+" | "+r.State,r.Request.Id+"\n"+r.Request.OwnText+"\n"+(r.Reply?.Text??"No reply displayed")+"\n"+(r.Reply?.Provenance??"Original request retained"));
            foreach(var linked in S.LinkedHistory)Text("Timeline.Linked."+linked.AttemptId,"Historical linked attempt",linked.AttemptId+"\n"+AttemptLifecycle.Outcome(linked)+"\nDelivered help "+linked.Assistance.Count);
            Button(current.Primary,"Back to record",OpenDebrief);Button(current.Secondary,"Enter read-only replay",EnterReplay,S.Mode!=SessionMode.Replay);Finish("History is attributed to its exact attempt, source versions and receipt IDs.");
        }
        void Button(Button button,string label,Action action,bool enabled){Button(button,label,action);button.interactable=enabled;}
        public void EnterReplay()
        {Lifecycle.EnterReplay();Begin("replay","Read-only replay");Text("Replay.Scope","Historical scope",AttemptLifecycle.Outcome(Lifecycle.ReplaySnapshot)+"\nAttempt "+Lifecycle.ReplaySnapshot.AttemptId+"\nReplay cannot mutate this attempt. New practice requires an explicit reset.");Button(current.Primary,"View timeline",OpenTimeline);Button(current.Secondary,"New linked practice",()=>ConfirmLifecycle("linked"));Button(current.Tertiary,"Full reset",()=>ConfirmLifecycle("reset"));Finish("Read-only. No new marks, communications, help or learner evidence.");}
        public void OpenSettings(string caller="pause")
        {settingsCaller=caller;Staged=Applied.Copy();ShowSettingsTab(0);}
        void ShowSettingsTab(int tab)
        {
            settingsTab=tab;Begin("settings","Settings | "+new[]{"Audio","Display","Input","Accessibility","Language"}[tab]);
            foreach(var row in current.SettingRows)row.gameObject.SetActive(row.Tab==tab);
            current.MasterSlider.onValueChanged.RemoveAllListeners();current.MasterSlider.SetValueWithoutNotify(Mathf.Round(Staged.Master*20)*5);current.MasterValue.text=Mathf.RoundToInt(Staged.Master*100)+"%";
            current.MasterSlider.onValueChanged.AddListener(v=>{var snapped=Mathf.Round(v/5)*5;current.MasterSlider.SetValueWithoutNotify(snapped);Staged.Master=snapped/100f;current.MasterValue.text=Mathf.RoundToInt(snapped)+"%";});
            BindToggle(current.SilentToggle,Staged.Silent,v=>Staged.Silent=v);BindToggle(current.MotionToggle,Staged.ReducedMotion,v=>Staged.ReducedMotion=v);BindToggle(current.ContrastToggle,Staged.HighContrast,v=>Staged.HighContrast=v);
            current.TextSizeDropdown.onValueChanged.RemoveAllListeners();var scales=new[]{1f,1.25f,1.5f,2f};current.TextSizeDropdown.SetValueWithoutNotify(Array.FindIndex(scales,v=>Mathf.Approximately(v,Staged.TextScale)));current.TextSizeDropdown.onValueChanged.AddListener(i=>Staged.TextScale=scales[i]);
            if(tab==0)
            {Text("Setting.AudioGate","Music, effects and voice","No admitted music, effects or voice clips are loaded by this UI. Separate effects volume and voice playback are unavailable. Silent mode preserves all text and status meaning.");}
            else if(tab==1){Text("Setting.Device","Current device rendering",Screen.width+" x "+Screen.height+" | "+Screen.fullScreenMode+"\nThe Editor controls the current viewport. Resolution, window-mode and brightness overrides are unavailable here. No source-approved rendering default is invented.");Text("Setting.DisplayPolicy","Stable presentation","Camera shake and head bob are not used. Reduced motion is available in Accessibility.");}
            else if(tab==2){Text("Setting.Input","Keyboard and mouse","WASD: move. Hold right mouse: look. E: inspect. Tab: direct routes or next UI focus. Shift+Tab: previous focus. Enter or Space: activate. Escape: back or pause. Wheel: focused scroll. Text Enter inserts a line break; use Send to submit.");Text("Setting.RemapGate","Binding changes unavailable","This local slice uses the source bindings. Remapping, controller input, speech input and pointer pan are unavailable. Direct routes and native numeric fields use the same domain commands.");}
            else if(tab==3)
            {Text("Setting.Access","Required accessibility cues","Visible keyboard focus, text status labels and non-colour cues stay on. All dialogue is readable text. Audio captions are unavailable because no voice or sound cues are provided. Direct routes avoid travel precision.");}
            else{Text("Setting.Language","English | Available","English is the current interface language. Arabic is pending terminology, RTL, font, input and intent review. Arabic and voice input are unavailable.");}
            Button(current.Primary,"Apply",()=>{Applied=Staged.Copy();ApplyPreferences(true);ReturnSettings();});Button(current.Secondary,"Cancel",ReturnSettings);Button(current.Tertiary,"Reset defaults",()=>{Staged=new LocalPresentationPreferences();ShowSettingsTab(settingsTab);});Finish("Changes are staged across tabs. Apply saves anonymous device preferences only. Cancel discards all staged changes.");
        }
        static void BindToggle(Toggle toggle,bool value,Action<bool> changed){toggle.onValueChanged.RemoveAllListeners();toggle.SetIsOnWithoutNotify(value);toggle.onValueChanged.AddListener(v=>changed(v));}
        void ApplyPreferences(bool persist)
        {
            AudioListener.volume=Applied.Silent?0:Applied.Master;Gameplay.CameraRig.ReducedMotion=Applied.ReducedMotion;
            Workbench.SetTextScale(Scale);
            foreach(var s in Surfaces)s.ApplyScale(Scale);
            if(Overlay.activeSelf)Workbench.HudHeader.gameObject.SetActive(false);
            foreach(var text in Workbench.GetComponentsInChildren<Text>(true))if(text.color.r<.4f&&text.color.g<.5f)text.color=Applied.HighContrast?new Color(.015f,.04f,.08f):NativeUiPalette.Ink;
            if(persist)Applied.Save();
        }
        void ReturnSettings(){Staged=null;if(settingsCaller=="title")OpenTitle();else Pause();}
        public void OpenStation(CampusStation station)
        {CloseSurface(false);if(station.Kind==StationKind.Review)OpenReview();else Workbench.OpenStation(station);}
        public void OpenRoutes(){CloseSurface(false);Workbench.OpenRoutes();}
        public void Back()
        {
            if(!Overlay.activeSelf){Workbench.Back();return;}
            if(SurfaceId=="settings"){ReturnSettings();return;}
            if(SurfaceId=="confirmation"){if(Pending!=null){if(confirmationKind=="hold")OpenHold();else if(confirmationKind=="review")OpenReviewRequest();else OpenReview();}else Pause();return;}
            if(SurfaceId=="title"){return;}if(SurfaceId=="brief"||SurfaceId=="loading"){OpenTitle();return;}
            if(S.Mode==SessionMode.Replay){EnterReplay();return;}if(SurfaceId=="pause"){Resume();return;}
            if(SurfaceId=="reviewRequest"||SurfaceId=="hold"||SurfaceId=="receipt"){OpenReview();return;}if(Lifecycle.Paused){Pause();return;}CloseSurface();
        }
        public void CloseSurface(bool restoreWorkbench=true)
        {Overlay.SetActive(false);SurfaceId="world";EventSystem.current?.SetSelectedGameObject(null);bool restoring=restoreWorkbench&&suspendedWorkbench;if(restoring){Workbench.Window.SetActive(true);suspendedWorkbench=false;}else if(restoreWorkbench)Gameplay.CloseInspection();else{suspendedWorkbench=false;suspendedFocus=null;}Workbench.ApplyInterfaceScale();if(restoring&&suspendedFocus&&suspendedFocus.activeInHierarchy){EventSystem.current?.SetSelectedGameObject(suspendedFocus);if(suspendedFocus.TryGetComponent<InputField>(out var field)){field.Select();field.ActivateInputField();}Workbench.Focus.EnsureVisible(suspendedFocus);}if(restoring)suspendedFocus=null;}
        void ReturnToCampus(){suspendedWorkbench=false;Workbench.CloseWindow();CloseSurface(false);}
        public bool SetFieldText(string id,string text)
        {
            if(Gameplay.IsComposing||current==null)return false;
            var field=id=="Composer"?current.Composer:current.Rows.Find(id)?.Input;if(!field)return false;
            field.Select();field.ActivateInputField();field.text=text;field.caretPosition=text.Length;field.ForceLabelUpdate();current.Focus.EnsureVisible(field.gameObject);return true;
        }
        public bool ActivateControl(string id)
        {
            if(!current)return false;Button b=id=="Primary"?current.Primary:id=="Secondary"?current.Secondary:id=="Tertiary"?current.Tertiary:id=="Send"?current.Send:id=="CancelRequest"?current.CancelRequest:id=="Close"?current.Close:current.Rows.Find(id)?.Action;
            if(!b||!b.gameObject.activeInHierarchy||!b.IsInteractable())return false;EventSystem.current?.SetSelectedGameObject(b.gameObject);b.onClick.Invoke();return true;
        }
    }
    public static class NativeUiPalette
    {public static readonly Color Ink=new Color(.025f,.09f,.18f);public static readonly Color Gold=new Color(.9f,.63f,.21f);}
}
