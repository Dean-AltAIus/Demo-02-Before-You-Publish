using E=Altaius.Demo02.Domain.EnglishCopy;
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
        public TitleScenePresentation TitleStage;
        bool titleHelp;
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
        int loadingGeneration;
        string settingsCaller="pause",confirmationKind="",confirmationReturn="review";int settingsTab;
        bool suspendedWorkbench;bool conversationRefresh;GameObject suspendedFocus;
        float previousAudioVolume;bool previousAudioPause;
        string lastEditedControl;
        ReceiptContext pendingReceiptContext,lastReceiptContext;
        CommandReceipt lastCommandReceipt;
        bool showReviewDetails,showReceiptDetails,showAuthorityDetails;
        readonly List<RenderedRowDelivery> deliveries=new List<RenderedRowDelivery>();
        SessionSnapshot S=>Host.Session.Snapshot;
        float Scale=>Applied?.TextScale??1;
        void Start()
        {
            if(Workbench.English)E.Resolver=Workbench.English.Get;
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
                {target.Send.onClick.AddListener(()=>RequestHelp(HelpIntent.RequestedStrategy,true));target.CancelRequest.onClick.AddListener(()=>{HelpDraft="";target.ComposerDraft.BindInput(E.Get("presentationview.start.write.your.own.help.request.label.write.your.own.help.request","Write your own help request..."),"",v=>HelpDraft=v,Scale,true);});
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
            Workbench.LeenaStatus.text=E.Format("presentationview.update.assign.workbench.leenastatus.text.offline","Offline | {value1}","value1",((SurfaceId=="coach")?E.Get("presentationview.update.assign.workbench.leenastatus.text.value1.help.open.help.open","Help open"):E.Get("presentationview.update.assign.workbench.leenastatus.text.value1.ask.leena.ask.leena","Ask Leena")));
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
            if(Gameplay.IsComposing)throw new InvalidOperationException(E.Get("presentationview.afterrendered.afterrendered.message.finish.composing.before.changing.this.surface","Finish composing before changing this surface."));
            if(Workbench.IsOpen){suspendedWorkbench=true;suspendedFocus=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;if(!suspendedFocus||!suspendedFocus.transform.IsChildOf(Workbench.Window.transform))suspendedFocus=Workbench.Rows.Find(Workbench.LastEditedControl)?.Input?.gameObject;Workbench.Window.SetActive(false);}
            if(id!="title"&&TitleStage)TitleStage.Release();
            Workbench.LeenaDock.gameObject.SetActive(id!="title");TalkLauncher.gameObject.SetActive(false);
            SurfaceId=id;Overlay.SetActive(true);current=Surfaces.Single(s=>s.SurfaceId==id);
            // The companion passage belongs to the suspended work surface, not the system overlay.
            if(Workbench.MarkContextPanel)Workbench.MarkContextPanel.SetActive(false);
            if(Workbench.MarkContextButton)Workbench.MarkContextButton.gameObject.SetActive(false);
            if(Workbench.WorkspaceTextSizeButton)Workbench.WorkspaceTextSizeButton.gameObject.SetActive(false);
            current.RoomComposition=(Gameplay.ActiveStation&&Gameplay.ActiveStation.Kind==StationKind.Review&&(id=="review"||id=="reviewRequest"||id=="receipt"||id=="confirmation"))||(id=="conversation"&&ConversationStation);
            current.BeginBinding();
            foreach(var screen in Surfaces)screen.gameObject.SetActive(screen==current);
            current.Heading.text=heading;current.Status.text="";current.Rows.Begin();
            foreach(var b in new[]{current.Primary,current.Secondary,current.Tertiary}){b.gameObject.SetActive(false);b.onClick.RemoveAllListeners();b.interactable=true;}
            current.Scroll.verticalNormalizedPosition=1;current.Close.gameObject.SetActive(id!="title"&&id!="loading");
            if(current.SideText)current.SideText.text=SidebarState();
            Workbench.HudHeader.gameObject.SetActive(false);Workbench.WorldHelp.gameObject.SetActive(false);EventSystem.current?.SetSelectedGameObject(null);return current;
        }
        string SidebarState()=>E.Format("presentationview.sidebarstate.return.task.rubric","Task {s_task_version} / Rubric {s_rubric_version}\n{publicationlabel_s_publication_true}\n{localauthority_time_s_clockminute}","s_task_version",S.Task.Version,"s_rubric_version",S.Rubric.Version,"publicationlabel_s_publication_true",PublicationLabel(S.Publication,true),"localauthority_time_s_clockminute",LocalAuthority.Time(S.ClockMinute));
        public CampusStation ConversationStation=>SurfaceId=="conversation"?Gameplay.Stations.FirstOrDefault(s=>s&&((ConversationRole==ActorRole.Colleague&&s.Kind==StationKind.Studio)||(ConversationRole==ActorRole.ModuleLead&&s.Kind==StationKind.Review))&&s.DistanceTo(Gameplay.Motor.transform.position)<=s.InteractionRange):null;
        public bool PhysicalConversationVisible=>Overlay&&Overlay.activeSelf&&SurfaceId=="conversation"&&current&&current.RoomComposition&&Scale<1.5f&&ConversationStation;
        public bool PhysicalReviewRoomVisible=>Overlay&&Overlay.activeSelf&&SurfaceId!="conversation"&&current&&current.RoomComposition&&Scale<1.5f;
        public static string PublicationLabel(PublicationStatus state,bool compact=false)
        {return ((state==PublicationStatus.HoldCommittedUnverified)?((compact)?E.Get("presentationview.publicationlabel.return.hold.committed.unverified.hold.committed.hold.committed.unverified.hold.committed.unverified","Hold committed\nUnverified"):E.Get("presentationview.publicationlabel.return.hold.committed.unverified.hold.committed.hold.committed.unverified.hold.committed.unverified.inline","Hold committed (unverified)")):((state==PublicationStatus.HoldRequested)?E.Get("presentationview.publicationlabel.return.hold.requested.hold.requested.hold.requested","Hold requested"):E.Label(state)));}
        NativeRow Text(string id,string heading,string body)
        {var row=current.Rows.Take(NativeRowKind.Text,id);row.BindText(heading,body,Scale);return row;}
        NativeRow Action(string id,string caption,Action action,bool enabled=true)
        {var row=current.Rows.Take(NativeRowKind.Action,id);row.BindAction(caption,()=>Guard(action),Scale,enabled);return row;}
        NativeRow Input(string id,string label,string value,Action<string> changed,bool multiline=false)
        {var row=current.Rows.Take(NativeRowKind.Input,id);row.BindInput(label,value,v=>{lastEditedControl=id;changed(v);},Scale,multiline);return row;}
        void Button(Button button,string label,Action action)
        {button.gameObject.SetActive(action!=null);button.GetComponentInChildren<Text>().text=label;button.onClick.RemoveAllListeners();if(action!=null)button.onClick.AddListener(()=>Guard(action));}
        void Finish(string status="")
        {current.Status.text=status;current.ApplyScale(Scale);Canvas.ForceUpdateCanvases();var first=current.Primary.gameObject.activeSelf?current.Primary:current.GetComponentsInChildren<Selectable>().FirstOrDefault(s=>s.IsActive()&&s.IsInteractable()&&s!=current.Close);if(first)EventSystem.current?.SetSelectedGameObject(first.gameObject);}
        public void Guard(Action action)
        {if(Gameplay.IsComposing){if(current)current.Status.text=E.Get("presentationview.guard.assign.current.status.text.finish.composing.before.submitting","Finish composing before submitting.");return;}try{action();}catch(Exception ex){if(current)current.Status.text=ex.Message;}}
        public void OpenTitle()
        {
            titleHelp=false;Lifecycle.Pause(false);Begin("title",E.Get("presentationview.opentitle.opentitle.heading.before.you.publish","Before You Publish"));
            if(TitleStage)TitleStage.Enter();
            if(PresenterControls!=null)Action("Title.Presenter",E.Get("title.walkthrough","Watch walkthrough"),PresenterControls.ShowBrief);
            if(current.TitleLayout){current.TitleLayout.RefreshLabels();Button(current.TitleLayout.CompanionHelp,E.Get("title.leena.identity","Leena | AI coach"),()=>{titleHelp=true;OpenCoach();});}
            Button(current.Primary,E.Get("presentationview.opentitle.opentitle.caption.begin.practice","Begin practice"),OpenBrief);Button(current.Secondary,E.Get("presentationview.opentitle.opentitle.caption.settings","Settings"),()=>OpenSettings("title"));Button(current.Tertiary,E.Get("presentationview.opentitle.opentitle.caption.exit","Exit"),()=>ConfirmLifecycle("exit"));Finish(E.Get("title.disclosure","Fictional practice. No real student data."));
        }
        public void OpenBrief()
        {
            Begin("brief",E.Get("presentationview.openbrief.openbrief.heading.before.you.begin","Before you begin"));Text("Brief.Fiction",E.Get("presentationview.openbrief.brief.fiction.heading.this.is.a.fictional.practice.scenario","This is a fictional practice scenario."),E.Get("presentationview.openbrief.brief.fiction.body.your.actual.actions.and.delivered.assistance","Your actual actions and delivered assistance are retained for this local debrief. No real student data is used. This rehearsal does not measure student learning or universal teaching competence."));
            Action("Mode.Independent",E.Format("presentationview.openbrief.mode.independent.caption.independent","{value0}Independent","value0",((selectedMode==SessionMode.Independent)?E.Get("presentationview.openbrief.mode.independent.caption.value0.selected.selected","Selected: "):"")),()=>{selectedMode=SessionMode.Independent;OpenBrief();});
            Action("Mode.Coached",E.Format("presentationview.openbrief.mode.coached.caption.coached","{value0}Coached","value0",((selectedMode==SessionMode.Coached)?E.Get("presentationview.openbrief.mode.independent.caption.value0.selected.selected","Selected: "):"")),()=>{selectedMode=SessionMode.Coached;OpenBrief();});
            if(PresenterControls!=null)Action("Mode.Presenter",E.Get("presentationview.openbrief.mode.presenter.caption.authored.synthetic.presenter.walkthrough","Authored synthetic presenter walkthrough"),PresenterControls.ShowBrief);
            Text("Brief.Modes",E.Get("presentationview.openbrief.brief.modes.heading.choose.how.to.practice","Choose how to practice"),E.Get("presentationview.openbrief.brief.modes.body.independent.strategy.only.when.you.request","Independent: strategy only when you request it. Coached: source-bounded assistance may be offered within the assistance limits. Controls support is always optional."));
            Button(current.Primary,E.Get("presentationview.openbrief.openbrief.caption.continue","Continue"),BeginPractice);Button(current.Secondary,E.Get("presentationview.openbrief.openbrief.caption.back","Back"),OpenTitle);Finish(E.Get("presentationview.openbrief.openbrief.status.help.is.optional.opening.or.requesting","Help is optional. Opening or requesting help is different from actual delivery."));
        }
        public void BeginPractice()
        {
            var generation=++loadingGeneration;
            Begin("loading",E.Get("presentationview.beginpractice.beginpractice.heading.preparing.local.practice","Preparing practice"));Text("Loading.State",E.Get("presentationview.beginpractice.loading.state.heading.checking.the.local.session","Getting ready"),E.Get("presentationview.beginpractice.loading.state.body.only.the.admitted.local.catalogue.is","Your practice stays on this device. No internet connection is needed."));Button(current.Secondary,E.Get("presentationview.beginpractice.beginpractice.caption.cancel","Cancel"),OpenBrief);Finish();
            StartCoroutine(StartLocalPractice(generation));
        }
        System.Collections.IEnumerator StartLocalPractice(int generation)
        {
            // Keep the saved loading and Cancel surface visible long enough to inspect and use.
            yield return new WaitForSecondsRealtime(.75f);if(generation!=loadingGeneration||SurfaceId!="loading")yield break;
            if(!Workbench.English||Workbench.English.Entries==null||Workbench.English.Entries.Length==0||!Host||Host.Session==null)
            {
                var heading=E.Get("presentationview.loading.unavailable.heading","Practice unavailable");
                var body=E.Get("presentationview.loading.unavailable.body","The practice content is missing. No new attempt was started. Return to the brief.");
                Begin("loading",heading);Text("Loading.Unavailable",E.Get("presentationview.loading.unavailable.detail.heading","Practice content missing"),body);
                Button(current.Secondary,E.Get("presentationview.beginpractice.beginpractice.caption.cancel","Cancel"),OpenBrief);Finish();
                yield break;
            }
            Lifecycle.Reset(false);Host.BeginSession(selectedMode);BindLifecycle();ClearDrafts();CloseSurface(false);OpenOnramp();
        }
        public void OpenOnramp()
        {
            Begin("onramp",E.Get("presentationview.openonramp.openonramp.heading.optional.controls","Optional controls"));
            var help=Lifecycle.Help.Request(HelpIntent.Controls,"D02.controls");
            var helpRow=Text("Onramp.Keys",E.Get("presentationview.openonramp.onramp.keys.heading.controls.you.can.reopen","Controls you can reopen"),help.Text);
            Text("Onramp.Step1",E.Get("presentationview.openonramp.onramp.step1.heading.inspect.a.selected.folio","Inspect a selected folio"),E.Get("presentationview.openonramp.onramp.step1.body.e.opens.the.selected.work.you","E opens the selected work. You can return without changing it. Open the studio and inspect a current version label."));
            Text("Onramp.Step2",E.Get("presentationview.openonramp.onramp.step2.heading.open.an.empty.annotation","Open an empty annotation"),E.Get("presentationview.openonramp.onramp.step2.body.at.the.experiment.table.inspect.work","At the experiment table, inspect work and an inspected criterion. Your own explanation stays editable."));
            Text("Onramp.Step3",E.Get("presentationview.openonramp.onramp.step3.heading.stage.a.mark.then.cancel","Stage a mark, then cancel"),E.Get("presentationview.openonramp.onramp.step3.body.staging.is.reversible.saving.and.publishing","Staging is reversible. Saving and publishing are different actions. These controls carry no assessment credit."));
            if(!titleHelp)Action("Onramp.Studio",E.Get("presentationview.openonramp.onramp.studio.caption.open.studio.controls","Open studio controls"),()=>{CloseSurface(false);Gameplay.DirectRoute("studio");});
            if(titleHelp)Button(current.Primary,E.Get("presentationview.openbrief.openbrief.caption.back","Back"),OpenTitle);
            else{Button(current.Primary,E.Get("presentationview.openonramp.openonramp.caption.continue.to.campus","Continue to campus"),ReturnToCampus);Button(current.Secondary,E.Get("presentationview.openonramp.openonramp.caption.skip.controls","Skip controls"),ReturnToCampus);}
            Finish(E.Get("presentationview.openonramp.openonramp.status.you.can.reopen.controls.from.leena","You can reopen controls from Leena at any time."));
            AfterRendered(helpRow.Body,help.Id,()=>{Lifecycle.Help.Acknowledge(help);Host.NotifyChanged();},true);
        }
        public void OpenConversation(ActorRole role)
        {
            ConversationRole=role;Begin("conversation",RoleName(role));current.ComposerDraft.BindInput(E.Get("presentationview.openconversation.write.in.your.own.words.label.write.in.your.own.words","Write in your own words..."),Lifecycle.Conversation.Draft(role),v=>{lastEditedControl="Composer";Lifecycle.Conversation.SetDraft(ConversationRole,v);},Scale,true);RenderConversation();
        }
        static ActorRole RoleAt(int i)=>i==0?ActorRole.Colleague:i==1?ActorRole.ModuleLead:ActorRole.ExperimentReviewer;
        public static string RoleName(ActorRole role)=>role==ActorRole.Learner?E.Get("role.learner","Learner"):role==ActorRole.Colleague?E.Get("role.colleague","Colleague"):role==ActorRole.ModuleLead?E.Get("role.module.reviewer","Module reviewer"):E.Get("role.experiment.reviewer","Experiment reviewer");
        void RenderConversation()
        {
            current.Rows.Begin();var records=Lifecycle.Conversation.History.Where(r=>r.Request.Recipient==ConversationRole).ToArray();
            if(records.Length==0)Text("Conversation.Scope",E.Format("presentationview.renderconversation.conversation.scope.heading.authored.offline","{rolename_conversationrole} | authored offline","rolename_conversationrole",RoleName(ConversationRole)),E.Get("presentationview.renderconversation.conversation.scope.body.ask.in.your.own.words.source","Ask in your own words. Source-bounded authored replies are available for role, inspected sources, publication time and current approval. Unsupported questions receive an unavailable reply. Conversation cannot grant approval."));
            foreach(var record in records.Skip(Math.Max(0,records.Length-1)))
            {
                Text("Conversation.Own."+record.Request.Id,((S.Mode==SessionMode.Presenter)?E.Get("presentationview.renderconversation.conversation.own.record.request.id.heading.authored.presenter.input.authored.presenter.input","Authored presenter input"):E.Get("speaker.you","You")),record.Request.OwnText);
                if(record.Reply!=null&&record.State!=ReplyState.Cancelled&&record.State!=ReplyState.Stale)
                {var row=Text("Conversation.Reply."+record.Request.Id,RoleName(ConversationRole),record.Reply.Text);if(record.State==ReplyState.Ready){var captured=record;var displayId=Guid.NewGuid();AfterRendered(row.Body,displayId,()=>{Lifecycle.Conversation.Acknowledge(new DisplayAcknowledgement(captured.Request.AttemptId,captured.Request.Id,displayId));conversationRefresh=true;});}}
                if(record.Error!=null)Text("Conversation.Error."+record.Request.Id,E.Get("presentationview.renderconversation.conversation.error.record.request.id.heading.preserved.request","Preserved request"),record.Error);
            }
            foreach(var b in new[]{current.Primary,current.Secondary,current.Tertiary})b.gameObject.SetActive(false);
            Finish();UpdateConversationStatus();Canvas.ForceUpdateCanvases();current.Scroll.verticalNormalizedPosition=1;
        }
        void UpdateConversationStatus()
        {
            var last=Lifecycle.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ConversationRole);
            current.Status.text=E.Format("presentationview.updateconversationstatus.assign.current.status.text.task.rubric.offline.voice.unavailable","{value0} | Task {s_task_version} / Rubric {s_rubric_version} | Offline. Voice unavailable.","value0",((last==null)?E.Get("presentationview.updateconversationstatus.assign.current.status.text.value0.draft.only.draft.only","Draft only"):E.Format("presentationview.updateconversationstatus.assign.current.status.text.value0.draft.retained.draft.retained","{last_state}. Draft retained","last_state",E.Label(last.State))),"s_task_version",S.Task.Version,"s_rubric_version",S.Rubric.Version);
            bool waiting=last!=null&&(last.State==ReplyState.Sending||last.State==ReplyState.Ready);
            current.Send.interactable=!waiting&&S.Mode!=SessionMode.Replay;current.CancelRequest.interactable=last!=null;current.CancelRequest.GetComponentInChildren<Text>().text=((waiting)?E.Get("presentationview.beginpractice.beginpractice.caption.cancel","Cancel"):E.Get("presentationview.updateconversationstatus.assign.text.receipt.receipt","Receipt"));current.ApplyScale(Scale);
        }
        void CancelOrInspectConversation()
        {
            var record=Lifecycle.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ConversationRole);if(record==null)return;
            if(record.State==ReplyState.Sending||record.State==ReplyState.Ready){CancelConversation();return;}
            Begin("receipt",E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.heading.conversation.receipt","Conversation receipt"));Text("Conversation.Detail.State",RoleName(record.Request.Recipient)+" | "+record.State,E.Format("presentationview.cancelorinspectconversation.conversation.detail.state.body.original.request.attempt.task.rubric.world","Original request {record_request_id}\nAttempt {record_request_attemptid}\nTask {record_request_pair_taskversion} / Rubric {record_request_pair_rubricversion} | world revision {record_request_worldrevision}\nTask SHA-256 {record_request_pair_taskdigest}\nRubric SHA-256 {record_request_pair_rubricdigest}\nDisplay acknowledgment {value15}","record_request_id",record.Request.Id,"record_request_attemptid",record.Request.AttemptId,"record_request_pair_taskversion",record.Request.Pair.TaskVersion,"record_request_pair_rubricversion",record.Request.Pair.RubricVersion,"record_request_worldrevision",record.Request.WorldRevision,"record_request_pair_taskdigest",record.Request.Pair.TaskDigest,"record_request_pair_rubricdigest",record.Request.Pair.RubricDigest,"value15",((record.DisplayId?.ToString() != null)?record.DisplayId?.ToString():E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded"))));
            Text("Conversation.Detail.Words",E.Get("presentationview.cancelorinspectconversation.conversation.detail.words.heading.your.original.words","Your original words"),record.Request.OwnText);Text("Conversation.Detail.Reply",E.Get("presentationview.cancelorinspectconversation.conversation.detail.reply.heading.actual.reply.and.provenance","Actual reply and provenance"),((((record.Reply?.Text != null)?record.Reply?.Text:E.Get("presentationview.cancelorinspectconversation.conversation.detail.reply.body.value0.no.reply.displayed.no.reply.displayed","No reply displayed.")))+("\n")+(((record.Reply?.Provenance != null)?record.Reply?.Provenance:E.Get("presentationview.cancelorinspectconversation.conversation.detail.reply.body.value2.no.response.received.no.response.received","No response received.")))));
            Button(current.Primary,E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.caption.back.to.conversation","Back to conversation"),()=>OpenConversation(ConversationRole));Button(current.Secondary,E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.caption.view.timeline","View timeline"),OpenTimeline);Finish(E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.status.conversation.receipts.remain.separate.from.domain","Conversation receipts remain separate from domain review, evaluation and release authority."));
        }
        public void SendConversation()
        {
            Guard(()=>{if(Lifecycle.Paused)throw new InvalidOperationException(E.Get("presentationview.sendconversation.sendconversation.message.resume.before.sending","Resume before sending."));var record=Lifecycle.Conversation.Send(ConversationRole,current.Composer.text);Lifecycle.Dispatch.Schedule(record,Time.unscaledTime+.35f);RenderConversation();});
        }
        public void CancelConversation()
        {
            var record=Lifecycle.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ConversationRole);
            if(record!=null)Lifecycle.Dispatch.Cancel(new CancelRequest(record.Request.AttemptId,record.Request.Id));RenderConversation();current.Composer.Select();current.Composer.ActivateInputField();
        }
        public void OpenCoach()
        {
            Begin("coach",E.Get("presentationview.opencoach.opencoach.heading.leena.ai.coach","Leena | AI coach"));current.ComposerDraft.BindInput(E.Get("presentationview.start.write.your.own.help.request.label.write.your.own.help.request","Write your own help request..."),HelpDraft,v=>{lastEditedControl="Composer";HelpDraft=v;},Scale,true);
            Text("Coach.Scope",E.Get("presentationview.opencoach.coach.scope.heading.source.bounded.help","Source-bounded help"),E.Get("presentationview.opencoach.coach.scope.body.controls.review.of.your.recorded.attempt","Controls, review of your recorded attempt and requested strategy are separate. Leena cannot approve publication, reveal hidden sources or prefill a successful repair."));
            Text("Coach.History",E.Get("presentationview.opencoach.coach.history.heading.actual.delivered.help","Actual delivered help"),HelpCoordinator.AssistanceSummary(S));
            Action("Coach.Onramp",E.Get("presentationview.opencoach.coach.onramp.caption.reopen.optional.controls","Reopen optional controls"),OpenOnramp);
            Finish(E.Get("presentationview.opencoach.opencoach.status.offline.voice.unavailable.free.text.strategy","Offline. Voice unavailable. Free-text strategy responses may be unavailable."));
        }
        public void RequestHelp(HelpIntent intent,bool freeText=false)
        {
            var help=Lifecycle.Help.Request(intent,CurrentOpportunity(),freeText?HelpDraft:"");
            current.Rows.Begin();var row=Text("Coach.Reply",((help.Available)?E.Get("presentationview.requesthelp.coach.reply.heading.leena.requested.help.leena.requested.help","Leena | requested help"):E.Get("presentationview.requesthelp.coach.reply.heading.help.unavailable.help.unavailable","Help unavailable")),help.Text);
            Text("Coach.Identity",E.Get("presentationview.requesthelp.coach.identity.heading.delivery.scope","Delivery scope"),E.Format("presentationview.requesthelp.coach.identity.body.delivery.opportunity","Delivery {help_id}\nOpportunity {help_opportunityid}\n{value5}","help_id",help.Id,"help_opportunityid",help.OpportunityId,"value5",((help.Strategic)?E.Get("presentationview.requesthelp.coach.identity.body.value5.strategic.help.attributed.only.when.strategic.help.attributed.only.when.displayed","Strategic help, attributed only when displayed."):E.Get("presentationview.requesthelp.coach.identity.body.value5.controls.or.recorded.facts.no.controls.or.recorded.facts.no.competence","Controls or recorded facts. No competence claim."))));
            Action("Coach.Onramp",E.Get("presentationview.requesthelp.coach.onramp.caption.reopen.controls","Reopen controls"),OpenOnramp);Finish(E.Get("presentationview.requesthelp.requesthelp.status.requested.actual.rendering.is.acknowledged.separately","Requested. Actual rendering is acknowledged separately."));
            AfterRendered(row.Body,help.Id,()=>{if(Lifecycle.Help.Acknowledge(help)){Host.NotifyChanged();current.Status.text=((help.Available)?E.Format("presentationview.requesthelp.assign.current.status.text.help.shown.delivery.recorded.help.shown.delivery.recorded","Help shown. Delivery recorded: {help_id}","help_id",help.Id):E.Get("presentationview.requesthelp.assign.current.status.text.unavailable.response.shown.no.strategy.unavailable.response.shown.no.strategy.supplied","Unavailable response shown. No strategy supplied."));}},true);
        }
        string CurrentOpportunity()=>Workbench.SelectedWork!=null?WorkbenchModel.OpportunityForWork(Workbench.SelectedWork):LocalAuthority.PublicationOpportunity;
        public void OpenReview()
        {
            Begin("review",E.Get("presentationview.openreview.openreview.heading.review.and.publication","Review and publication"));Text("Review.Pair",E.Format("presentationview.openreview.review.pair.heading.current","Current {receiptcopy_pair_s_pair}","receiptcopy_pair_s_pair",ReceiptCopy.Pair(S.Pair)),ReceiptCopy.ReviewBody(S));
            Text("Review.Gate",E.Format("presentationview.openreview.review.gate.heading.publication","Publication: {publicationlabel_s_publication}","publicationlabel_s_publication",PublicationLabel(S.Publication)),((S.Publication==PublicationStatus.Released)?E.Get("presentationview.openreview.review.gate.body.the.exact.current.pair.is.the.exact.current.pair.is.released","The exact current pair is released."):((S.CurrentApproval!=null)?E.Get("presentationview.openreview.review.gate.body.current.approval.is.recorded.release.current.approval.is.recorded.release.current.approval.is.recorded.release.is","Current approval is recorded. Release is a separate request."):E.Get("presentationview.openreview.review.gate.body.current.approval.is.recorded.release.no.exact.current.approval.is.no.exact.current.approval.is.recorded","No exact current approval is recorded. Publication remains closed."))));
            Action("Review.Request",E.Get("presentationview.openreview.review.request.caption.request.an.exact.pair.review.time","Request an exact-pair review time"),OpenReviewRequest);Action("Review.Conversation",E.Get("presentationview.openreview.review.conversation.caption.talk.to.module.reviewer","Talk to module reviewer"),()=>OpenConversation(ActorRole.ModuleLead));
            var latest=S.ReviewHistory.LastOrDefault();if(latest!=null)
            {Text("Review.Original",E.Format("presentationview.openreview.review.original.heading.requested","Requested {receiptcopy_pair_latest_pair}","receiptcopy_pair_latest_pair",ReceiptCopy.Pair(latest.Pair)),E.Format("presentationview.openreview.review.original.body.review.requested.time","Review: {latest_status} | Requested time {localauthority_time_latest_minute}\n{latest_changesummary}{value6}","latest_status",E.Label(latest.Status),"localauthority_time_latest_minute",LocalAuthority.Time(latest.Minute),"latest_changesummary",latest.ChangeSummary,"value6",((latest.DecisionReason==null)?"":(("\n")+(latest.DecisionReason)))));
                Action("Review.Schedule",E.Get("presentationview.openreview.review.schedule.caption.check.local.scheduling.response","Check local scheduling response"),()=>ShowAuthority(Lifecycle.Authority.PrepareScheduling(latest.Id)),latest.Status==ReviewStatus.Pending);
                Action("Review.Decision",E.Get("presentationview.openreview.review.decision.caption.request.separate.review.decision","Request separate review decision"),()=>ShowAuthority(Lifecycle.Authority.PrepareReviewDecision(latest.Id)),latest.Status==ReviewStatus.Scheduled);}
            Action("Review.Release",E.Format("presentationview.openreview.review.release.caption.preview.current.pair.release","Preview current-pair release{value1}","value1",((S.CurrentApproval==null)?E.Get("presentationview.openreview.review.release.caption.value1.current.approval.missing.current.approval.missing"," | current approval missing"):"")),()=>PreviewDomain(Lifecycle.Authority.StageRelease(),"release"));
            Action("Review.Hold",E.Get("presentationview.openreview.review.hold.caption.propose.an.owned.hold","Propose an owned hold"),OpenHold);Action("Review.Timeline",E.Get("presentationview.openreview.review.timeline.caption.inspect.authority.and.receipt.history","Inspect authority and receipt history"),OpenTimeline);
            var hold=S.HoldHistory.LastOrDefault();if(hold!=null){Text("Hold.Current",E.Format("presentationview.openreview.hold.current.heading.hold.for","Hold for {receiptcopy_pair_hold_pair}","receiptcopy_pair_hold_pair",ReceiptCopy.Pair(hold.Pair)),E.Format("presentationview.openreview.hold.current.body.owner.review.next.action.acceptance.defect","{hold_proposal_defect}\nOwner {rolename_hold_proposal_owner} | Review {hold_proposal_reviewat}\nNext action: {hold_proposal_nextaction}\nAcceptance {value8} | Defect verified {hold_defectverified} | Self-owned {hold_selfowned}","hold_proposal_defect",hold.Proposal.Defect,"rolename_hold_proposal_owner",RoleName(hold.Proposal.Owner),"hold_proposal_reviewat",hold.Proposal.ReviewAt,"hold_proposal_nextaction",hold.Proposal.NextAction,"value8",((hold.Accepted?.ToString() != null)?hold.Accepted?.ToString():E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded")),"hold_defectverified",hold.DefectVerified,"hold_selfowned",hold.SelfOwned));
                if(hold.Accepted==null)Action("Hold.Accept",((hold.Proposal.Owner==ActorRole.Learner)?E.Get("presentationview.openreview.hold.accept.caption.confirm.my.self.owned.commitment.confirm.my.self.owned.commitment","Confirm my self-owned commitment"):E.Get("presentationview.openreview.hold.accept.caption.check.actual.owner.response.check.actual.owner.response","Check actual owner response")),()=>{if(hold.Proposal.Owner==ActorRole.Learner)PreviewDomain(Lifecycle.Authority.StageSelfAcceptance(hold.Id),"self hold");else ShowAuthority(Lifecycle.Authority.PrepareOwnerAcceptance(hold.Id));});}
            Action("Review.Details",((showReviewDetails)?E.Get("presentationview.openreview.review.details.caption.hide.exact.provenance.hide.exact.provenance","Hide exact provenance"):E.Get("presentationview.openreview.review.details.caption.view.exact.provenance.view.exact.provenance","View exact provenance")),()=>{showReviewDetails=!showReviewDetails;OpenReview();});
            if(showReviewDetails)Text("Review.Provenance",E.Get("presentationview.openreview.review.provenance.heading.read.only.provenance","Read-only provenance"),E.Format("presentationview.openreview.review.provenance.body.task.sha.256.rubric.sha.256","Task SHA-256 {s_task_digest}\nRubric SHA-256 {s_rubric_digest}\nWorld revision {s_worldrevision} | Clock revision {s_clockrevision}\nApproval receipt {value9}\nLatest review request {value11}","s_task_digest",S.Task.Digest,"s_rubric_digest",S.Rubric.Digest,"s_worldrevision",S.WorldRevision,"s_clockrevision",S.ClockRevision,"value9",((S.CurrentApproval?.ReceiptId.ToString() != null)?S.CurrentApproval?.ReceiptId.ToString():E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded")),"value11",((latest?.Id.ToString() != null)?latest?.Id.ToString():E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded"))));
            if(S.Publication==PublicationStatus.Released)Button(current.Primary,E.Get("presentationview.openreview.openreview.caption.view.release.history","View release history"),OpenTimeline);
            else if(S.CurrentApproval!=null)Button(current.Primary,E.Get("presentationview.openreview.openreview.caption.preview.release","Preview release"),()=>PreviewDomain(Lifecycle.Authority.StageRelease(),"release"));
            else if(latest!=null&&latest.Status==ReviewStatus.Pending)Button(current.Primary,E.Get("presentationview.openreview.openreview.caption.check.review.time","Check review time"),()=>ShowAuthority(Lifecycle.Authority.PrepareScheduling(latest.Id)));
            else if(latest!=null&&latest.Status==ReviewStatus.Scheduled)Button(current.Primary,E.Get("presentationview.openreview.openreview.caption.check.review.decision","Check review decision"),()=>ShowAuthority(Lifecycle.Authority.PrepareReviewDecision(latest.Id)));
            else Button(current.Primary,E.Get("presentationview.openreview.openreview.caption.request.current.review","Request review"),OpenReviewRequest);
            Button(current.Secondary,E.Get("presentationview.openreview.openreview.caption.back.to.campus","Back to campus"),ReturnToCampus);Button(current.Tertiary,E.Get("presentationview.openreview.openreview.caption.debrief","Debrief"),OpenDebrief);Finish(E.Get("presentationview.openreview.openreview.status.scheduling.review.decisions.and.release.are","Scheduling, review decisions and release are separate receipts. Arbitrary semantic review is unavailable offline."));
        }
        public void OpenReviewRequest()
        {
            Begin("reviewRequest",E.Get("presentationview.openreviewrequest.openreviewrequest.heading.request.review.time","Request review time"));Text("Schedule.Clock",E.Format("presentationview.openreviewrequest.schedule.clock.heading.simulation.time","Simulation time {localauthority_time_s_clockminute}","localauthority_time_s_clockminute",LocalAuthority.Time(S.ClockMinute)),E.Format("presentationview.openreviewrequest.schedule.clock.body.review.window.to.14.00.publication","Review window: {localauthority_time_mathf_max_s_clockminute_820} to 14:00. Publication due 14:00. A proposal does not guarantee a slot or advance time.","localauthority_time_mathf_max_s_clockminute_820",LocalAuthority.Time(Mathf.Max(S.ClockMinute,820))));
            Input("Review.Summary",((S.Mode==SessionMode.Presenter)?E.Get("presentationview.openreviewrequest.review.summary.label.authored.demonstration.change.summary.authored.demonstration.change.summary","Authored demonstration change summary"):E.Get("presentationview.openreviewrequest.review.summary.label.your.change.summary.your.change.summary","Your change summary")),ReviewSummary,v=>ReviewSummary=v,true);Input("Review.Time",E.Get("presentationview.openreviewrequest.review.time.label.requested.time.hh.mm","Requested time (HH:MM)"),ReviewTime,v=>ReviewTime=v);
            Button(current.Primary,E.Get("presentationview.openreviewrequest.openreviewrequest.caption.preview.request","Preview request"),()=>PreviewDomain(Lifecycle.Authority.StageReview(ReviewSummary,ReviewTime),"review"));Button(current.Secondary,E.Get("presentationview.openreviewrequest.openreviewrequest.caption.keep.draft","Keep draft"),OpenReview);Finish(E.Get("presentationview.openreviewrequest.openreviewrequest.status.the.current.pair.and.exact.readset","The current pair and exact readset will be captured in the preview."));
        }
        public void OpenHold()
        {
            Begin("hold",E.Get("presentationview.openhold.openhold.heading.hold.proposal","Hold proposal"));Input("Hold.Defect",E.Get("presentationview.openhold.hold.defect.label.specific.defect.in.your.own.words","Specific defect, in your own words"),HoldDefect,v=>HoldDefect=v,true);
            Action("Hold.Owner",E.Format("presentationview.openhold.hold.owner.caption.owner.change","Owner: {holdowner} | Change","holdowner",E.Label(HoldOwner)),()=>{HoldOwner=HoldOwner==ActorRole.Learner?ActorRole.ModuleLead:HoldOwner==ActorRole.ModuleLead?ActorRole.Colleague:ActorRole.Learner;OpenHold();});
            Input("Hold.Time",E.Get("presentationview.openhold.hold.time.label.future.same.day.review.time.hh","Future same-day review time (HH:MM)"),HoldTime,v=>HoldTime=v);Input("Hold.NextAction",E.Get("presentationview.openhold.hold.nextaction.label.owner.s.next.action","Owner's next action"),HoldNextAction,v=>HoldNextAction=v,true);
            Action("Hold.Basis",E.Format("presentationview.openhold.hold.basis.caption.defect.basis.change","Defect basis: {holdbasis} | Change","holdbasis",E.Label(HoldBasis)),()=>{HoldBasis=HoldBasis==HoldDefectBasis.Unverified?HoldDefectBasis.MissingSourceAttachments:HoldBasis==HoldDefectBasis.MissingSourceAttachments?HoldDefectBasis.MissingDecisionContext:HoldBasis==HoldDefectBasis.MissingDecisionContext?HoldDefectBasis.PerQuestionMarksMismatch:HoldDefectBasis.Unverified;OpenHold();});
            Text("Hold.BasisScope",E.Get("presentationview.openhold.hold.basisscope.heading.structural.checks.use.actual.anchors","Structural checks use actual anchors"),E.Get("presentationview.openhold.hold.basisscope.body.a.structural.basis.requires.inspected.current","A structural basis requires inspected current task/rubric passages and an actual matching defect. Free defect prose remains unverified. Proposal, owner acceptance and release are different states."));
            Button(current.Primary,E.Get("presentationview.openhold.openhold.caption.preview.proposal","Preview proposal"),()=>PreviewDomain(Lifecycle.Authority.StageHold(HoldDefect,HoldOwner,HoldTime,HoldNextAction,HoldBasis),"hold"));Button(current.Secondary,E.Get("presentationview.openreviewrequest.openreviewrequest.caption.keep.draft","Keep draft"),OpenReview);Finish(E.Get("presentationview.openhold.openhold.status.no.owner.acceptance.has.been.inferred","No owner acceptance has been inferred."));
        }
        void PreviewDomain(CommandEnvelope envelope,string kind)
        {
            Pending=envelope;pendingReceiptContext=new ReceiptContext(envelope,S);confirmationKind=kind;Begin("confirmation",((kind=="self hold")?E.Get("presentationview.previewdomain.previewdomain.heading.confirm.self.ownership.confirm.self.ownership","Confirm self-ownership"):E.Format("presentationview.previewdomain.previewdomain.heading.confirm.confirm","Confirm {kind}","kind",kind)));
            if(envelope.Command is RequestReview review)Text("Confirm.Change",E.Get("presentationview.previewdomain.confirm.change.heading.review.proposal","Review proposal"),E.Format("presentationview.previewdomain.confirm.change.body.requested.a.request.does.not.schedule","{review_changesummary}\nRequested {review_reviewby}. A request does not schedule or approve.","review_changesummary",review.ChangeSummary,"review_reviewby",review.ReviewBy));
            if(envelope.Command is RequestHold hold)Text("Confirm.Change",E.Get("presentationview.openhold.openhold.heading.hold.proposal","Hold proposal"),E.Format("presentationview.previewdomain.confirm.change.body.owner.next.action.basis.inspected.anchors","{hold_defect}\nOwner {hold_owner} | {hold_reviewat}\nNext action: {hold_nextaction}\nBasis {hold_basis} | inspected anchors {hold_anchors}","hold_defect",hold.Defect,"hold_owner",E.Label(hold.Owner),"hold_reviewat",hold.ReviewAt,"hold_nextaction",hold.NextAction,"hold_basis",E.Label(hold.Basis),"hold_anchors",hold.Anchors.Count));
            if(kind=="self hold")Text("Confirm.Change",E.Get("presentationview.previewdomain.confirm.change.heading.your.explicit.commitment","Your explicit commitment"),E.Get("presentationview.previewdomain.confirm.change.body.i.accept.the.stated.next.action","I accept the stated next action and future time as the learner owner. This is self-owned, not an NPC response. Unverified defect prose remains unverified."));
            if(kind=="release")Text("Confirm.Change",E.Get("presentationview.previewdomain.confirm.change.heading.release.this.exact.pair","Release this exact pair"),((S.CurrentApproval==null)?E.Get("presentationview.previewdomain.confirm.change.body.current.approval.is.missing.confirming.current.approval.is.missing.confirming.submits","Current approval is missing. Confirming submits an actual release request to the gate. The cabinet stays closed unless this exact pair has accepted authority."):E.Get("presentationview.previewdomain.confirm.change.body.this.will.request.publication.using.this.will.request.publication.using.the","This will request publication using the current exact approval receipt. A changed pair or stale readset will reject the action.")));
            Text("Confirm.Scope",E.Format("presentationview.previewdomain.confirm.scope.heading.exact.task.rubric","Exact Task {s_task_version} / Rubric {s_rubric_version}","s_task_version",S.Task.Version,"s_rubric_version",S.Rubric.Version),E.Format("presentationview.previewdomain.confirm.scope.body.request.attempt.world.revision.task.sha","Request {envelope_commandid}\nAttempt {envelope_attemptid}\nWorld revision {envelope_expectedworldrevision}\nTask SHA-256 {s_task_digest}\nRubric SHA-256 {s_rubric_digest}","envelope_commandid",envelope.CommandId,"envelope_attemptid",envelope.AttemptId,"envelope_expectedworldrevision",envelope.ExpectedWorldRevision,"s_task_digest",S.Task.Digest,"s_rubric_digest",S.Rubric.Digest));
            Button(current.Primary,((kind=="self hold")?E.Get("presentationview.previewdomain.previewdomain.caption.i.confirm.my.commitment.i.confirm.my.commitment","I confirm my commitment"):E.Format("presentationview.previewdomain.previewdomain.heading.confirm.confirm","Confirm {kind}","kind",kind)),CommitDomain);Button(current.Secondary,E.Get("presentationview.previewdomain.previewdomain.caption.keep.editing","Keep editing"),()=>{if(kind=="hold")OpenHold();else if(kind=="review")OpenReviewRequest();else OpenReview();});Finish(E.Get("presentationview.previewdomain.previewdomain.status.confirming.submits.this.original.staged.request","Confirming submits this original staged request once. Changed content needs an explicit fresh preview."));
        }
        public void CommitDomain()
        {
            if(Pending==null||pendingReceiptContext==null)throw new InvalidOperationException(E.Get("presentationview.commitdomain.commitdomain.message.preview.the.request.first","Preview the request first."));var receipt=Lifecycle.Authority.Confirm(Pending);Host.NotifyChanged();
            showReceiptDetails=false;ShowCommandReceipt(receipt,pendingReceiptContext);
        }
        public void ShowCommandReceipt(CommandReceipt receipt,ReceiptContext context)
        {
            LastAuthorityReply=null;var words=ReceiptCopy.Describe(receipt,context,S);lastCommandReceipt=receipt;lastReceiptContext=context;
            Begin("receipt",words.Heading);Text("Receipt.Status",ReceiptCopy.Pair(context.Pair),words.Body);
            Text("Receipt.Outcome",E.Format("presentationview.openreview.review.pair.heading.current","Current {receiptcopy_pair_s_pair}","receiptcopy_pair_s_pair",ReceiptCopy.Pair(S.Pair)),ReceiptCopy.ReviewBody(S));
            Action("Receipt.Details",((showReceiptDetails)?E.Get("presentationview.showcommandreceipt.receipt.details.caption.hide.exact.receipt.details.hide.exact.receipt.details","Hide exact receipt details"):E.Get("presentationview.showcommandreceipt.receipt.details.caption.view.exact.receipt.details.view.exact.receipt.details","View exact receipt details")),()=>{showReceiptDetails=!showReceiptDetails;ShowCommandReceipt(lastCommandReceipt,lastReceiptContext);});
            if(showReceiptDetails)Text("Receipt.Provenance",E.Get("presentationview.showcommandreceipt.receipt.provenance.heading.read.only.original.receipt","Read-only original receipt"),E.Format("presentationview.showcommandreceipt.receipt.provenance.body.request.revision.to.task.sha.256","{receipt_action} | {receipt_status} | {receipt_reasoncode}\nRequest {receipt_requestid}\nRevision {receipt_beforerevision} to {receipt_afterrevision}\nTask SHA-256 {context_pair_taskdigest}\nRubric SHA-256 {context_pair_rubricdigest}\nAuthority {receipt_authority}\n{displaydisclosure_receipt}","receipt_action",receipt.Action,"receipt_status",receipt.Status,"receipt_reasoncode",receipt.ReasonCode,"receipt_requestid",receipt.RequestId,"receipt_beforerevision",receipt.BeforeRevision,"receipt_afterrevision",receipt.AfterRevision,"context_pair_taskdigest",context.Pair.TaskDigest,"context_pair_rubricdigest",context.Pair.RubricDigest,"receipt_authority",receipt.Authority,"displaydisclosure_receipt",DisplayDisclosure(receipt)));
            Button(current.Primary,((receipt.Status==ReceiptStatus.Accepted)?E.Get("presentationview.openreview.openreview.heading.review.and.publication","Review and publication"):E.Get("presentationview.showcommandreceipt.showcommandreceipt.caption.return.to.preserved.draft.return.to.preserved.draft","Return to preserved draft")),receipt.Status==ReceiptStatus.Accepted?(Action)OpenReview:ReturnDomainDraft);Button(current.Secondary,E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.caption.view.timeline","View timeline"),OpenTimeline);
            Finish(((S.Mode==SessionMode.Presenter)?E.Get("presentationview.showcommandreceipt.showcommandreceipt.status.authored.synthetic.demonstration.no.learner.authored.synthetic.demonstration.no.learner.evidence","Authored synthetic demonstration. No learner evidence."):E.Get("presentationview.showcommandreceipt.showcommandreceipt.status.original.request.retained.operational.acceptance.original.request.retained.operational.acceptance.is","The original request is kept in your read-only history.")));
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
            lastCommandReceipt=null;lastReceiptContext=null;if(LastAuthorityReply!=reply)showAuthorityDetails=false;LastAuthorityReply=reply;Begin("receipt",reply.Status);var row=Text("Authority.Reply",ReceiptCopy.Pair(reply.RequestedPair)+" | "+RoleName(reply.AuthorityRole),reply.Text);
            Action("Authority.Details",showAuthorityDetails?E.Get("authority.details.hide","Hide exact reply details"):E.Get("authority.details.show","View exact reply details"),()=>{showAuthorityDetails=!showAuthorityDetails;ShowAuthority(reply);});
            if(showAuthorityDetails)Text("Authority.Identity",E.Get("presentationview.showauthority.authority.identity.heading.scoped.offline.reply","Scoped offline reply"),E.Format("presentationview.showauthority.authority.identity.body.original.request.reply.attempt","{reply_provenance}\nOriginal request {reply_originalrequestid}\nReply {reply_id}\nAttempt {reply_attemptid}","reply_provenance",reply.Provenance,"reply_originalrequestid",reply.OriginalRequestId,"reply_id",reply.Id,"reply_attemptid",reply.AttemptId));
            Button(current.Primary,E.Get("presentationview.openreview.openreview.heading.review.and.publication","Review and publication"),OpenReview);Button(current.Secondary,E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.caption.view.timeline","View timeline"),OpenTimeline);Finish(E.Get("presentationview.showauthority.showauthority.status.response.prepared.role.authority.is.admitted","Response prepared. Role authority is admitted only after actual display."));
            AfterRendered(row.Body,reply.Id,()=>{Lifecycle.Authority.Acknowledge(reply);Host.NotifyChanged();if(current&&SurfaceId=="receipt"){if(reply.Receipt!=null){var words=ReceiptCopy.Describe(reply.Receipt,new ReceiptContext(reply.Receipt.RequestId,reply.RequestedCommand,reply.RequestedPair),S);current.Heading.text=words.Heading;row.BindText(ReceiptCopy.Pair(reply.RequestedPair)+" | "+RoleName(reply.AuthorityRole),words.Body,Scale);}else current.Heading.text=reply.Status;if(current.SideText)current.SideText.text=SidebarState();current.Status.text=reply.Provenance;}});
        }
        public void Pause()
        {
            if(PresenterControls?.Active==true){PresenterControls.Pause();return;}
            Lifecycle.Pause(true);Begin("pause",E.Get("presentationview.pause.pause.heading.paused","Paused"));
            Text("Pause.State",E.Get("presentationview.pause.pause.state.heading.local.practice.is.paused","Local practice is paused"),E.Get("presentationview.pause.pause.state.body.reading.typing.and.pausing.do.not","Reading, typing and pausing do not advance the logical clock. Pending local responses wait. Your drafts and original request IDs stay in this attempt."));
            Action("Pause.Settings",E.Get("presentationview.opentitle.opentitle.caption.settings","Settings"),()=>OpenSettings("pause"));Action("Pause.Debrief",E.Get("presentationview.pause.pause.debrief.caption.view.practice.record","View practice record"),OpenDebrief);Action("Pause.Linked",E.Get("presentationview.pause.pause.linked.caption.start.linked.practice","Start linked practice"),()=>ConfirmLifecycle("linked"));Action("Pause.Reset",E.Get("presentationview.pause.pause.reset.caption.full.reset","Full reset"),()=>ConfirmLifecycle("reset"));Action("Pause.Exit",E.Get("presentationview.pause.pause.exit.caption.leave.the.simulation","Leave the simulation"),()=>ConfirmLifecycle("exit"));
            Button(current.Primary,E.Get("presenterrunner.refreshdock.assign.text.resume.resume","Resume"),Resume);Finish(E.Format("presentationview.pause.pause.status.simulation.time.no.countdown.or.time","Simulation time {localauthority_time_s_clockminute}. No countdown or time grade.","localauthority_time_s_clockminute",LocalAuthority.Time(S.ClockMinute)));
        }
        public void Resume(){Lifecycle.Pause(false);CloseSurface();}
        public void ConfirmLifecycle(string kind)
        {
            Pending=null;confirmationKind=kind;confirmationReturn=SurfaceId;Begin("confirmation",((kind=="linked")?E.Get("presentationview.confirmlifecycle.confirmlifecycle.heading.start.linked.practice.start.linked.practice","Start linked practice?"):((kind=="exit")?E.Get("presentationview.confirmlifecycle.confirmlifecycle.heading.leave.the.simulation.start.again.leave.the.simulation.leave.the.simulation","Leave the simulation?"):E.Get("presentationview.confirmlifecycle.confirmlifecycle.heading.leave.the.simulation.start.again.start.again.start.again","Start again?"))));
            Text("Reset.Explanation",((kind=="linked")?E.Get("presentationview.confirmlifecycle.reset.explanation.heading.new.attempt.linked.to.this.new.attempt.linked.to.this.practice","New attempt linked to this practice"):((kind=="exit")?E.Get("presentationview.confirmlifecycle.reset.explanation.heading.your.current.attempt.ends.clear.your.current.attempt.ends.your.current.attempt.ends","Your current attempt ends"):E.Get("presentationview.confirmlifecycle.reset.explanation.heading.your.current.attempt.ends.clear.clear.this.attempt.and.linked.clear.this.attempt.and.linked.practice","Clear this attempt and linked practice"))),((kind=="linked")?E.Get("presentationview.confirmlifecycle.reset.explanation.body.your.original.history.and.actual.your.original.history.and.actual.assistance","Your original history and actual assistance remain read-only. Current marks, approvals, drafts and pending responses do not become new results. This is assisted rehearsal, not an unseen transfer test."):E.Get("presentationview.confirmlifecycle.reset.explanation.body.dialogue.drafts.evidence.and.linked.dialogue.drafts.evidence.and.linked.retry","Dialogue, drafts, evidence and linked retry data will be cleared. Pending replies cannot change the new attempt. Anonymous presentation settings remain local.")));
            Button(current.Primary,((kind=="linked")?E.Get("presentationview.pause.pause.linked.caption.start.linked.practice","Start linked practice"):((kind=="exit")?E.Get("presentationview.confirmlifecycle.confirmlifecycle.caption.leave.start.a.new.attempt.leave.leave","Leave"):E.Get("presentationview.confirmlifecycle.confirmlifecycle.caption.leave.start.a.new.attempt.start.a.new.attempt.start.a.new.attempt","Start a new attempt"))),()=>ResetAttempt(kind));Button(current.Secondary,E.Get("presentationview.confirmlifecycle.confirmlifecycle.caption.keep.playing","Keep playing"),()=>{if(confirmationReturn=="title")OpenTitle();else Pause();});Finish(E.Get("presentationview.confirmlifecycle.confirmlifecycle.status.this.action.requires.your.explicit.confirmation","This action requires your explicit confirmation."));
        }
        public void ResetAttempt(string kind)
        {
            Gameplay.ReleasePresenter();Lifecycle.Reset(kind=="linked");Host.RebindWorkbench();ClearDrafts();suspendedWorkbench=false;Workbench.CloseWindow();
            if(kind=="exit")OpenTitle();else{CloseSurface(false);OpenOnramp();}
        }
        void ClearDrafts()
        {ReviewSummary=ReviewTime=HoldDefect=HoldTime=HoldNextAction=HelpDraft="";HoldOwner=ActorRole.Learner;HoldBasis=HoldDefectBasis.Unverified;Pending=null;LastAuthorityReply=null;pendingReceiptContext=null;lastReceiptContext=null;lastCommandReceipt=null;showReceiptDetails=false;showReviewDetails=false;showAuthorityDetails=false;suspendedFocus=null;deliveries.Clear();foreach(var screen in Surfaces)if(screen.Composer)screen.Composer.SetTextWithoutNotify("");}
        public void OpenDebrief()
        {
            Begin("debrief",E.Get("presentationview.opendebrief.opendebrief.heading.practice.record","Practice record"));Text("Debrief.Outcome",E.Get("presentationview.opendebrief.debrief.outcome.heading.actual.outcome","Actual outcome"),AttemptLifecycle.Outcome(S));Text("Debrief.Observation",E.Get("presentationview.opendebrief.debrief.observation.heading.observed.action.and.consequence","Observed action and consequence"),E.Format("presentationview.opendebrief.debrief.observation.body.semantic.evaluation.of.arbitrary.prose.is","{attemptlifecycle_boundedobservation_s}\nSemantic evaluation of arbitrary prose is unavailable offline. No measured learning or universal competence claim is available.","attemptlifecycle_boundedobservation_s",AttemptLifecycle.BoundedObservation(S)));Text("Debrief.Assistance",E.Get("presentationview.opendebrief.debrief.assistance.heading.actual.assistance","Actual assistance"),HelpCoordinator.AssistanceSummary(S));
            if(S.Mode==SessionMode.Presenter)
            {
                var approval=S.ApprovalHistory.LastOrDefault();var release=S.Audit.LastOrDefault(a=>a.Receipt.Action=="release.request");
                Text("Debrief.Presenter",E.Get("presentationview.opendebrief.debrief.presenter.heading.exact.authored.run.records","Exact authored run records"),E.Format("presentationview.opendebrief.debrief.presenter.body.current.task.rubric.current.approval.historical","Current Task {s_task_version} / Rubric {s_rubric_version}. Current approval: {value5}.\nHistorical approval: {value7}.\nLast release request: {value9}.\nPublication: {s_publication}. Accepted hold: {value13}. Learner evidence records: {s_evidence}.\nThe shown repair, hypotheses and scoped findings are authored demonstration content. Seek review of the current pair before release.","s_task_version",S.Task.Version,"s_rubric_version",S.Rubric.Version,"value5",((S.CurrentApproval==null)?E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded"):S.CurrentApproval.ReceiptId.ToString()),"value7",((approval==null)?E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded"):E.Format("presentationview.opendebrief.debrief.presenter.body.value7.task.rubric.task.rubric","Task {approval_pair_taskversion} / Rubric {approval_pair_rubricversion} | {approval_receiptid}","approval_pair_taskversion",approval.Pair.TaskVersion,"approval_pair_rubricversion",approval.Pair.RubricVersion,"approval_receiptid",approval.ReceiptId)),"value9",((release==null)?E.Get("presentationview.opendebrief.debrief.presenter.body.value9.not.attempted.not.attempted","Not attempted"):((release.Receipt.RequestId)+(" | ")+(release.Receipt.Status)+(" | ")+(release.Receipt.ReasonCode))),"s_publication",S.Publication,"value13",((S.HoldHistory.Any(h=>h.Accepted==true))?E.Get("record.recorded","Recorded"):E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded")),"s_evidence",S.Evidence.Count));
            }
            Button(current.Primary,E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.caption.view.timeline","View timeline"),OpenTimeline);Button(current.Secondary,E.Get("presentationview.opendebrief.opendebrief.caption.linked.practice","Linked practice"),()=>ConfirmLifecycle("linked"));Button(current.Tertiary,E.Get("presentationview.openbrief.openbrief.caption.back","Back"),()=>{if(Lifecycle.Paused)Pause();else CloseSurface();});Finish(((S.Mode==SessionMode.Presenter)?E.Get("presentationview.opendebrief.opendebrief.status.synthetic.presenter.records.no.learner.synthetic.presenter.records.no.learner.evidence","Synthetic presenter records. No learner evidence."):E.Get("presentationview.opendebrief.opendebrief.status.this.debrief.describes.only.the.this.debrief.describes.only.the.recorded","This debrief describes only the recorded local attempt.")));
            var reportRow=current.Rows.Find("Debrief.Observation");var reportAttempt=S.AttemptId;var reportDisplay=Guid.NewGuid();
            if(reportRow)AfterRendered(reportRow.Body,reportDisplay,()=>Workbench.LeenaDock.GetComponent<LeenaHudProjection>()?.ReportRendered(reportAttempt,reportDisplay));
        }
        public void OpenTimeline()
        {
            Begin("timeline",E.Get("presentationview.opentimeline.opentimeline.heading.read.only.timeline","Read-only timeline"));Text("Timeline.Scope",E.Format("presentationview.opentimeline.timeline.scope.heading.current.attempt","Current attempt {s_attemptid}","s_attemptid",S.AttemptId),E.Format("presentationview.opentimeline.timeline.scope.body.mode.this.view.cannot.create.marks","Mode {s_mode} | {attemptlifecycle_outcome_s}\nThis view cannot create marks, approvals or evidence.","s_mode",S.Mode,"attemptlifecycle_outcome_s",AttemptLifecycle.Outcome(S)));
            foreach(var a in S.Audit)Text("Timeline.Command."+a.Sequence,E.Format("presentationview.opentimeline.timeline.command.a.sequence.heading.command","Command {a_sequence} | {a_receipt_action}","a_sequence",a.Sequence,"a_receipt_action",a.Receipt.Action),E.Format("presentationview.opentimeline.timeline.command.a.sequence.body.receipt","{a_receipt_status} | {a_receipt_reasoncode}\nReceipt {a_receipt_requestid}\n{displaydisclosure_a_receipt}","a_receipt_status",a.Receipt.Status,"a_receipt_reasoncode",a.Receipt.ReasonCode,"a_receipt_requestid",a.Receipt.RequestId,"displaydisclosure_a_receipt",DisplayDisclosure(a.Receipt)));
            foreach(var r in S.ReviewHistory)Text("Timeline.Review."+r.Id+"."+r.Revision,E.Format("presentationview.opentimeline.timeline.review.r.id.r.revision.heading.review","Review | {r_status}","r_status",r.Status),E.Format("presentationview.opentimeline.timeline.review.r.id.r.revision.body.task.rubric.request.decision.receipt","Task {r_pair_taskversion} / Rubric {r_pair_rubricversion} | Request {r_id}\n{value7}\nDecision receipt {value9}","r_pair_taskversion",r.Pair.TaskVersion,"r_pair_rubricversion",r.Pair.RubricVersion,"r_id",r.Id,"value7",((r.DecisionReason != null)?r.DecisionReason:r.ChangeSummary),"value9",((r.DecisionReceiptId?.ToString() != null)?r.DecisionReceiptId?.ToString():E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded"))));
            foreach(var reply in Lifecycle.Authority.LinkedReplies.Concat(Lifecycle.Authority.Replies))Text("Timeline.RoleReply."+reply.Id,RoleName(reply.AuthorityRole)+" | "+reply.Status,E.Format("presentationview.opentimeline.timeline.rolereply.reply.id.body.original.request.reply.attempt.actually.displayed","{reply_text}\nOriginal request {reply_originalrequestid}\nReply {reply_id} | Attempt {reply_attemptid}\nActually displayed {reply_displayed}\n{reply_provenance}","reply_text",reply.Text,"reply_originalrequestid",reply.OriginalRequestId,"reply_id",reply.Id,"reply_attemptid",reply.AttemptId,"reply_displayed",reply.Displayed,"reply_provenance",reply.Provenance));
            foreach(var h in S.HoldHistory)Text("Timeline.Hold."+h.Id+"."+h.Revision,E.Format("presentationview.opentimeline.timeline.hold.h.id.h.revision.heading.hold.acceptance","Hold | acceptance {value1}","value1",((h.Accepted?.ToString() != null)?h.Accepted?.ToString():E.Get("presentationview.cancelorinspectconversation.conversation.detail.state.body.value15.not.recorded.not.recorded","Not recorded"))),E.Format("presentationview.opentimeline.timeline.hold.h.id.h.revision.body.owner.self.owned.verified.defect","{h_id}\n{h_proposal_defect}\nOwner {h_proposal_owner} | {h_proposal_reviewat} | {h_proposal_nextaction}\nSelf-owned {h_selfowned} | Verified defect {h_defectverified}","h_id",h.Id,"h_proposal_defect",h.Proposal.Defect,"h_proposal_owner",h.Proposal.Owner,"h_proposal_reviewat",h.Proposal.ReviewAt,"h_proposal_nextaction",h.Proposal.NextAction,"h_selfowned",h.SelfOwned,"h_defectverified",h.DefectVerified));
            foreach(var h in S.Assistance)Text("Timeline.Help."+h.Id,E.Get("presentationview.opentimeline.timeline.help.h.id.heading.help.actually.delivered","Help actually delivered"),E.Format("presentationview.opentimeline.timeline.help.h.id.body.opportunity.strategic.requested","{h_id}\nOpportunity {h_opportunityid} | Strategic {h_strategic} | Requested {h_requested}\n{h_text}","h_id",h.Id,"h_opportunityid",h.OpportunityId,"h_strategic",h.Strategic,"h_requested",h.Requested,"h_text",h.Text));
            foreach(var r in Lifecycle.Conversation.LinkedHistory.Concat(Lifecycle.Conversation.History))Text("Timeline.Conversation."+r.Request.Id,RoleName(r.Request.Recipient)+" | "+r.State,((r.Request.Id)+("\n")+(r.Request.OwnText)+("\n")+(((r.Reply?.Text != null)?r.Reply?.Text:E.Get("presentationview.opentimeline.timeline.conversation.r.request.id.body.value4.no.reply.displayed.no.reply.displayed","No reply displayed")))+("\n")+(((r.Reply?.Provenance != null)?r.Reply?.Provenance:E.Get("presentationview.opentimeline.timeline.conversation.r.request.id.body.value6.original.request.retained.original.request.retained","Original request retained")))));
            foreach(var linked in S.LinkedHistory)Text("Timeline.Linked."+linked.AttemptId,E.Get("presentationview.opentimeline.timeline.linked.linked.attemptid.heading.historical.linked.attempt","Historical linked attempt"),E.Format("presentationview.opentimeline.timeline.linked.linked.attemptid.body.delivered.help","{linked_attemptid}\n{attemptlifecycle_outcome_linked}\nDelivered help {linked_assistance}","linked_attemptid",linked.AttemptId,"attemptlifecycle_outcome_linked",AttemptLifecycle.Outcome(linked),"linked_assistance",linked.Assistance.Count));
            Button(current.Primary,E.Get("presentationview.opentimeline.opentimeline.caption.back.to.record","Back to record"),OpenDebrief);Button(current.Secondary,E.Get("presentationview.opentimeline.opentimeline.caption.enter.read.only.replay","Enter read-only replay"),EnterReplay,S.Mode!=SessionMode.Replay);Finish(E.Get("presentationview.opentimeline.opentimeline.status.history.is.attributed.to.its.exact","History is attributed to its exact attempt, source versions and receipt IDs."));
        }
        void Button(Button button,string label,Action action,bool enabled){Button(button,label,action);button.interactable=enabled;}
        public void EnterReplay()
        {Lifecycle.EnterReplay();Begin("replay",E.Get("presentationview.enterreplay.enterreplay.heading.read.only.replay","Read-only replay"));Text("Replay.Scope",E.Get("presentationview.enterreplay.replay.scope.heading.historical.scope","Historical scope"),E.Format("presentationview.enterreplay.replay.scope.body.attempt.replay.cannot.mutate.this.attempt","{attemptlifecycle_outcome_lifecycle_replaysnapshot}\nAttempt {lifecycle_replaysnapshot_attemptid}\nReplay cannot mutate this attempt. New practice requires an explicit reset.","attemptlifecycle_outcome_lifecycle_replaysnapshot",AttemptLifecycle.Outcome(Lifecycle.ReplaySnapshot),"lifecycle_replaysnapshot_attemptid",Lifecycle.ReplaySnapshot.AttemptId));Button(current.Primary,E.Get("presentationview.cancelorinspectconversation.cancelorinspectconversation.caption.view.timeline","View timeline"),OpenTimeline);Button(current.Secondary,E.Get("presentationview.enterreplay.enterreplay.caption.new.linked.practice","New linked practice"),()=>ConfirmLifecycle("linked"));Button(current.Tertiary,E.Get("presentationview.pause.pause.reset.caption.full.reset","Full reset"),()=>ConfirmLifecycle("reset"));Finish(E.Get("presentationview.enterreplay.enterreplay.status.read.only.no.new.marks.communications","Read-only. No new marks, communications, help or learner evidence."));}
        public void OpenSettings(string caller="pause")
        {settingsCaller=caller;Staged=Applied.Copy();ShowSettingsTab(0);}
        void ShowSettingsTab(int tab)
        {
            settingsTab=tab;Begin("settings",E.Format("presentationview.showsettingstab.showsettingstab.heading.settings","Settings | {value1}","value1",new[]{E.Get("settings.tab.audio","Audio"),E.Get("settings.tab.display","Display"),E.Get("settings.tab.input","Input"),E.Get("settings.tab.accessibility","Accessibility"),E.Get("settings.tab.language","Language")}[tab]));
            foreach(var row in current.SettingRows)row.gameObject.SetActive(row.Tab==tab);
            current.MasterSlider.onValueChanged.RemoveAllListeners();current.MasterSlider.SetValueWithoutNotify(Mathf.Round(Staged.Master*20)*5);current.MasterValue.text=Mathf.RoundToInt(Staged.Master*100)+"%";
            current.MasterSlider.onValueChanged.AddListener(v=>{var snapped=Mathf.Round(v/5)*5;current.MasterSlider.SetValueWithoutNotify(snapped);Staged.Master=snapped/100f;current.MasterValue.text=Mathf.RoundToInt(snapped)+"%";});
            BindToggle(current.SilentToggle,Staged.Silent,v=>Staged.Silent=v);BindToggle(current.MotionToggle,Staged.ReducedMotion,v=>Staged.ReducedMotion=v);BindToggle(current.ContrastToggle,Staged.HighContrast,v=>Staged.HighContrast=v);
            current.TextSizeDropdown.onValueChanged.RemoveAllListeners();var scales=new[]{1f,1.25f,1.5f,2f};current.TextSizeDropdown.SetValueWithoutNotify(Array.FindIndex(scales,v=>Mathf.Approximately(v,Staged.TextScale)));current.TextSizeDropdown.onValueChanged.AddListener(i=>Staged.TextScale=scales[i]);
            if(tab==0)
            {Text("Setting.AudioGate",E.Get("presentationview.showsettingstab.setting.audiogate.heading.music.effects.and.voice","Music, effects and voice"),E.Get("presentationview.showsettingstab.setting.audiogate.body.no.admitted.music.effects.or.voice","No admitted music, effects or voice clips are loaded by this UI. Separate effects volume and voice playback are unavailable. Silent mode preserves all text and status meaning."));}
            else if(tab==1){Text("Setting.Device",E.Get("presentationview.showsettingstab.setting.device.heading.current.device.rendering","Current device rendering"),E.Format("presentationview.showsettingstab.setting.device.body.x.the.editor.controls.the.current","{screen_width} x {screen_height} | {screen_fullscreenmode}\nThe Editor controls the current viewport. Resolution, window-mode and brightness overrides are unavailable here. No source-approved rendering default is invented.","screen_width",Screen.width,"screen_height",Screen.height,"screen_fullscreenmode",Screen.fullScreenMode));Text("Setting.DisplayPolicy",E.Get("presentationview.showsettingstab.setting.displaypolicy.heading.stable.presentation","Stable presentation"),E.Get("presentationview.showsettingstab.setting.displaypolicy.body.camera.shake.and.head.bob.are","Camera shake and head bob are not used. Reduced motion is available in Accessibility."));}
            else if(tab==2){Text("Setting.Input",E.Get("presentationview.showsettingstab.setting.input.heading.keyboard.and.mouse","Keyboard and mouse"),E.Get("presentationview.showsettingstab.setting.input.body.wasd.move.hold.right.mouse.look","Arrow keys: move. Hold right mouse: look. E: inspect. Tab: direct routes or next UI focus. Shift+Tab: previous focus. Enter or Space: activate. Escape: back or pause. Wheel: focused scroll. Text Enter inserts a line break; use Send to submit."));Text("Setting.RemapGate",E.Get("presentationview.showsettingstab.setting.remapgate.heading.binding.changes.unavailable","Binding changes unavailable"),E.Get("presentationview.showsettingstab.setting.remapgate.body.this.local.slice.uses.the.source","This demo uses fixed keyboard controls. Remapping, controller input, speech input and pointer pan are unavailable. Direct routes and native numeric fields use the same domain commands."));}
            else if(tab==3)
            {Text("Setting.Access",E.Get("presentationview.showsettingstab.setting.access.heading.required.accessibility.cues","Required accessibility cues"),E.Get("presentationview.showsettingstab.setting.access.body.visible.keyboard.focus.text.status.labels","Visible keyboard focus, text status labels and non-colour cues stay on. All dialogue is readable text. Audio captions are unavailable because no voice or sound cues are provided. Direct routes avoid travel precision."));}
            else{Text("Setting.Language",E.Get("presentationview.showsettingstab.setting.language.heading.english.available","English | Available"),E.Get("presentationview.showsettingstab.setting.language.body.english.is.the.current.interface.language","English is the current interface language. Arabic is pending terminology, RTL, font, input and intent review. Arabic and voice input are unavailable."));}
            Button(current.Primary,E.Get("settings.apply","Apply"),()=>{Applied=Staged.Copy();ApplyPreferences(true);ReturnSettings();});Button(current.Secondary,E.Get("presentationview.beginpractice.beginpractice.caption.cancel","Cancel"),ReturnSettings);Button(current.Tertiary,E.Get("presentationview.showsettingstab.showsettingstab.caption.reset.defaults","Reset defaults"),()=>{Staged=new LocalPresentationPreferences();ShowSettingsTab(settingsTab);});Finish(E.Get("presentationview.showsettingstab.showsettingstab.status.changes.are.staged.across.tabs.apply","Changes are staged across tabs. Apply saves anonymous device preferences only. Cancel discards all staged changes."));
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
            if(titleHelp&&(SurfaceId=="coach"||SurfaceId=="onramp")){OpenTitle();return;}
            if(!Overlay.activeSelf){Workbench.Back();return;}
            if(SurfaceId=="settings"){ReturnSettings();return;}
            if(SurfaceId=="confirmation"){if(Pending!=null){if(confirmationKind=="hold")OpenHold();else if(confirmationKind=="review")OpenReviewRequest();else OpenReview();}else if(confirmationReturn=="title")OpenTitle();else Pause();return;}
            if(SurfaceId=="title"){return;}if(SurfaceId=="brief"||SurfaceId=="loading"){OpenTitle();return;}
            if(S.Mode==SessionMode.Replay){EnterReplay();return;}if(SurfaceId=="pause"){Resume();return;}
            if(SurfaceId=="reviewRequest"||SurfaceId=="hold"||SurfaceId=="receipt"){OpenReview();return;}if(Lifecycle.Paused){Pause();return;}CloseSurface();
        }
        public void CloseSurface(bool restoreWorkbench=true)
        {titleHelp=false;if(TitleStage)TitleStage.Release();Workbench.LeenaDock.gameObject.SetActive(true);Overlay.SetActive(false);SurfaceId="world";EventSystem.current?.SetSelectedGameObject(null);bool restoring=restoreWorkbench&&suspendedWorkbench;if(restoring){Workbench.Window.SetActive(true);suspendedWorkbench=false;}else if(restoreWorkbench)Gameplay.CloseInspection();else{suspendedWorkbench=false;suspendedFocus=null;}Workbench.ApplyInterfaceScale();if(restoring&&suspendedFocus&&suspendedFocus.activeInHierarchy){EventSystem.current?.SetSelectedGameObject(suspendedFocus);if(suspendedFocus.TryGetComponent<InputField>(out var field)){field.Select();field.ActivateInputField();}Workbench.Focus.EnsureVisible(suspendedFocus);}if(restoring)suspendedFocus=null;}
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
