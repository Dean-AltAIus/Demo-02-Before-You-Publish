using E=Altaius.Demo02.Domain.EnglishCopy;
using System;
using System.Linq;
using System.Text;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    /// <summary>Live native controls bound only to admitted domain projections and own drafts.</summary>
    public sealed class WorkbenchView : MonoBehaviour, ICampusWorkspace
    {
        public NativeUiStyle Style;
        public NativeEnglishStringTable English;
        public CampusGameplayFacade Gameplay;
        public CampusSession Host;
        public GameObject Window;
        public Text Heading, PairLabel, Message, HudStatus;
        public NativeRowPool Rows;
        public ScrollRect Scroll;
        public Button Primary, Secondary, Close;
        public Text PrimaryText, SecondaryText;
        public Button NotebookButton, HistoryButton, RoutesButton, TextSizeButton;
        public Text TextSizeLabel;
        public RectTransform HudHeader, LeenaDock, WorldHelp;
        public Text HudTitle, LeenaIdentity, LeenaStatus;
        public Button PauseButton, WorkspaceTextSizeButton;
        public GameObject MarkContextPanel;
        public NativeRow MarkContextWork, MarkContextCriterion;
        public Button MarkContextButton;
        public KeyboardFocus Focus;
        [Range(1f,2f)] public float TextScale = 1f;
        public Transform ConversationExtensionSlot;
        public Transform CoachExtensionSlot;
        public event System.Action ReviewRequested;
        public event System.Action PauseRequested;
        public bool IsOpen => Window && Window.activeSelf;
        public string ViewId { get; private set; }
        public ulong BindingGeneration {get;private set;}
        public string SelectedWork { get; private set; }
        public string InspectedSourceId {get;private set;}
        public string LastEditedControl { get; private set; }
        string previewReturn;
        string passageReturn;
        Passage visiblePassage;
        bool showProvenance;
        CampusStation station;
        WorkbenchModel Model => Host.Workbench;
        SessionSnapshot S => Host.Session.Snapshot;
        void Start()
        {
            if(English)E.Resolver=English.Get;
            Close.onClick.AddListener(CloseWindow);
            NotebookButton.onClick.AddListener(OpenNotebook); HistoryButton.onClick.AddListener(OpenHistory);
            RoutesButton.onClick.AddListener(OpenRoutes); TextSizeButton.onClick.AddListener(ToggleTextScale);
            if(WorkspaceTextSizeButton) WorkspaceTextSizeButton.onClick.AddListener(ToggleTextScale);
            if(MarkContextButton) MarkContextButton.onClick.AddListener(ReadMarkContext);
            Host.Changed += UpdateStatus; UpdateStatus(); ApplyInterfaceScale();
        }
        void OnDestroy() { if (Host) Host.Changed -= UpdateStatus; }
        public void UpdateStatus()
        {
            if (Host.Session == null) return;
            var s = S; string pair = E.Format("campusexperimentprojection.lateupdate.assign.pairlabel.text.task.rubric","Task {s_task_version} / Rubric {s_rubric_version}","s_task_version",s.Task.Version,"s_rubric_version",s.Rubric.Version);
            PairLabel.text = pair + "   |   " + (ViewId=="mark" && Model.Mark!=null ? Model.Mark.Work.SourceId+" > "+Model.Mark.CriterionId : s.Publication.ToString());
            HudStatus.text = E.Format("workbenchview.updatestatus.assign.hudstatus.text.offline","{pair}   |   {s_clockminute_60_00}:{s_clockminute_60_00_4}   |   Offline","pair",pair,"s_clockminute_60_00",(s.ClockMinute / 60).ToString("00"),"s_clockminute_60_00_4",(s.ClockMinute % 60).ToString("00"));
        }
        void Begin(string view, string title)
        {
            BindingGeneration++;
            ViewId = view;InspectedSourceId=null; Window.SetActive(true); Heading.text = title; Message.text = ""; Rows.Begin();
            Primary.gameObject.SetActive(false); Secondary.gameObject.SetActive(false);
            Primary.interactable = true; Secondary.interactable = true;
            Primary.onClick.RemoveAllListeners(); Secondary.onClick.RemoveAllListeners();
            Scroll.verticalNormalizedPosition = 1; UpdateStatus(); EventSystem.current?.SetSelectedGameObject(null);
            ApplyInterfaceScale();
        }
        NativeRow Text(string id, string title, string body) { var row = Rows.Take(NativeRowKind.Text,id); row.BindText(title,body,TextScale); return row; }
        NativeRow Action(string id, string caption, System.Action callback, bool available = true)
        { var row = Rows.Take(NativeRowKind.Action,id); row.BindAction(caption, () => Guard(callback),TextScale,available); return row; }
        NativeRow Input(string id, string title, string value, Action<string> changed, bool multiline = false)
        {
            var row = Rows.Take(NativeRowKind.Input,id);
            row.BindInput(title,value,v => { LastEditedControl = id; changed(v); Message.text = E.Get("workbenchview.input.assign.message.text.draft.retained.locally.preview.before.committing","Draft retained locally. Preview before committing."); RefreshMarkAvailability(); }, TextScale,multiline);
            return row;
        }
        void Buttons(string first, System.Action action, string second = null, System.Action secondary = null)
        {
            Primary.gameObject.SetActive(action != null); PrimaryText.text = first;
            if (action != null) Primary.onClick.AddListener(() => Guard(action));
            Secondary.gameObject.SetActive(secondary != null); SecondaryText.text = second;
            if (secondary != null) Secondary.onClick.AddListener(() => Guard(secondary));
        }
        public void Guard(System.Action action)
        {
            if (Gameplay.IsComposing) { Message.text = E.Get("workbenchview.guard.assign.message.text.finish.composing.text.before.this.action","Finish composing text before this action."); return; }
            try { action(); }
            catch (Exception ex) { Message.text = ex.Message; RestoreEditorFocus(); }
        }
        void FirstFocus()
        {
            Canvas.ForceUpdateCanvases();
            var selectable = Rows.GetComponentsInChildren<Selectable>().FirstOrDefault(x => x.IsActive() && x.IsInteractable());
            if (selectable) EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
        }
        void RestoreEditorFocus()
        {
            var row = Rows.Find(LastEditedControl); if (row && row.Input) { row.Input.Select(); row.Input.ActivateInputField(); Focus.EnsureVisible(row.Input.gameObject); }
        }
        public void OpenRoutes()
        {
            Begin("routes",E.Get("workbenchview.openroutes.openroutes.heading.campus.routes","Campus routes"));
            Text("Routes.Intro",E.Get("workbenchview.openroutes.routes.intro.heading.choose.a.destination","Choose a destination"), E.Get("workbenchview.openroutes.routes.intro.body.direct.routes.reach.the.same.station","Direct routes reach the same station, inspection and controls. Travel carries no assessment credit."));
            foreach (var s in Gameplay.Stations) { var target = s; Action("Route." + s.StationId,s.DisplayName,() => Gameplay.DirectRoute(target.StationId)); }
            Action("Route.Notebook",E.Get("workbenchview.openroutes.route.notebook.caption.open.inspected.notebook","Open inspected notebook"),OpenNotebook); Action("Route.History",E.Get("workbenchview.openroutes.route.history.caption.open.version.history","Open version history"),OpenHistory);
            Buttons(E.Get("workbenchview.openroutes.openroutes.primary.caption.return.to.campus","Return to campus"),CloseWindow); FirstFocus();
        }
        public void OpenStation(CampusStation target)
        {
            station = target;
            switch(target.Kind) { case StationKind.Experiment: OpenWorkList(); break; case StationKind.Review: OpenReview(); break; default: OpenSourceList(); break; }
        }
        public void OpenSourceList()
        {
            Begin("sources",station.DisplayName);
            Text("Sources.Intro",E.Get("workbenchview.opensourcelist.sources.intro.heading.inspect.a.literal.passage","Inspect a literal passage"), E.Get("workbenchview.opensourcelist.sources.intro.body.only.the.passage.you.open.is","Only the passage you open is admitted to your notebook. Inspection itself is not evidence of understanding."));
            foreach(var id in Host.Session.InspectableSourceIds(station.Location))
            {
                if (station.Kind == StationKind.Studio && id.StartsWith("D02.task.v") && !id.StartsWith("D02.task.v" + S.Task.Version + ".")) continue;
                if (station.Kind == StationKind.Studio && id.StartsWith("D02.rubric.v") && !id.StartsWith("D02.rubric.v" + S.Rubric.Version + ".")) continue;
                string selected = id; Action("Inspect."+id,((id)+(((S.Notebook.Any(p=>p.Reference.SourceId==id))?E.Get("workbenchview.opensourcelist.inspect.id.caption.value1.inspected.inspected","  |  Inspected"):E.Get("workbenchview.opensourcelist.inspect.id.caption.value1.open.open","  |  Open")))),()=>OpenPassage(selected,"sources"));
            }
            if(station.Kind == StationKind.Studio)
            {
                Action("Editor.Task",E.Get("workbenchview.opensourcelist.editor.task.caption.edit.full.task","Edit full task"),OpenTask); Action("Editor.Rubric",E.Get("workbenchview.opensourcelist.editor.rubric.caption.edit.full.rubric","Edit full rubric"),OpenRubric);
                Action("Studio.Experiment",E.Get("workbenchview.opensourcelist.studio.experiment.caption.go.to.the.experiment.table","Go to the experiment table"),()=>Gameplay.DirectRoute("experiment"));
            }
            Buttons(E.Get("workbenchview.openroutes.openroutes.heading.campus.routes","Campus routes"),OpenRoutes); FirstFocus();
        }
        public void OpenPassage(string sourceId,string returnTo)
        {
            var passage = returnTo == "notebook" ? Host.Session.Inspect(sourceId,InspectionLocation.PortableWorkspace) : Gameplay.InspectPassage(sourceId);
            passageReturn = returnTo;visiblePassage=passage;showProvenance=false;RenderPassage();
        }
        void RenderPassage()
        {
            var passage=visiblePassage;var source=passage.Reference.SourceId;var item=source.Split('.').Last();
            string title=((source.StartsWith("D02.task."))?E.Format("workbenchview.renderpassage.assign.string.title.task.question.task.question","Task {passage_reference_version} | Question {item}","passage_reference_version",passage.Reference.Version,"item",item):((source.StartsWith("D02.rubric."))?E.Format("workbenchview.renderpassage.assign.string.title.rubric.criterion.work.source.rubric.criterion.rubric.criterion","Rubric {passage_reference_version} | Criterion {item}","passage_reference_version",passage.Reference.Version,"item",item):((source.StartsWith("W_"))?E.Format("workbenchview.renderpassage.assign.string.title.rubric.criterion.work.source.work.source.work.work","Work {source}","source",source):E.Format("workbenchview.renderpassage.assign.string.title.rubric.criterion.work.source.work.source.source.source","Source {source}","source",source))));
            Begin("passage",title);InspectedSourceId=source;
            Text("Passage.Body",((source.StartsWith("D02.task."))?E.Format("workbenchview.renderpassage.passage.body.heading.question.question","Question {item}","item",item):((source.StartsWith("D02.rubric."))?E.Get("workbenchview.renderpassage.passage.body.heading.current.criterion.literal.passage.current.criterion.current.criterion","Current criterion"):E.Get("workbenchview.renderpassage.passage.body.heading.current.criterion.literal.passage.literal.passage.literal.passage","Literal passage"))),passage.DisplayText);
            Text("Passage.Identity",E.Get("workbenchview.renderpassage.passage.identity.heading.inspected.source","Inspected source"),E.Format("workbenchview.renderpassage.passage.identity.body.version.passage","{source} | Version {passage_reference_version}\nPassage {passage_reference_passageid}","source",source,"passage_reference_version",passage.Reference.Version,"passage_reference_passageid",passage.Reference.PassageId));
            Action("Passage.Details",((showProvenance)?E.Get("presentationview.openreview.review.details.caption.hide.exact.provenance.hide.exact.provenance","Hide exact provenance"):E.Get("presentationview.openreview.review.details.caption.view.exact.provenance.view.exact.provenance","View exact provenance")),()=>{showProvenance=!showProvenance;RenderPassage();});
            if(showProvenance)Text("Passage.Provenance",E.Get("workbenchview.renderpassage.passage.provenance.heading.exact.sha.256","Exact SHA-256"),passage.Reference.Sha256);
            Buttons(E.Get("presentationview.openbrief.openbrief.caption.back","Back"),()=> { if(passageReturn == "notebook") OpenNotebook(); else OpenSourceList(); });FirstFocus();
        }
        public void OpenNotebook()
        {
            Begin("notebook",E.Get("workbenchview.opennotebook.opennotebook.heading.inspected.notebook","Inspected notebook"));
            Text("Notebook.Intro",E.Get("workbenchview.opennotebook.notebook.intro.heading.permitted.content.only","Permitted content only"), ((S.Notebook.Count == 0)?E.Get("workbenchview.opennotebook.notebook.intro.body.your.notebook.is.empty.inspect.your.notebook.is.empty.inspect.a","Your notebook is empty. Inspect a passage at a campus station."):E.Get("workbenchview.opennotebook.notebook.intro.body.these.are.your.inspected.passages.these.are.your.inspected.passages.with","These are your inspected passages, with their original identities. Earlier document editions remain labelled by version.")));
            foreach(var passage in S.Notebook)
            {
                Text("Notebook." + passage.Reference.SourceId, E.Format("workbenchview.opennotebook.notebook.passage.reference.sourceid.heading.v","{passage_reference_sourceid} | v{passage_reference_version}","passage_reference_sourceid",passage.Reference.SourceId,"passage_reference_version",passage.Reference.Version),passage.DisplayText);
            }
            Buttons(E.Get("workbenchview.openroutes.openroutes.heading.campus.routes","Campus routes"),OpenRoutes); FirstFocus();
        }
        public void OpenWorkList()
        {
            Begin("works",E.Get("workbenchview.openworklist.openworklist.heading.experiment.table","Experiment table"));
            Text("Work.Intro",E.Get("workbenchview.openworklist.work.intro.heading.choose.fictional.work","Choose fictional work"), E.Get("workbenchview.openworklist.work.intro.body.open.the.literal.work.before.assigning","Open the literal work before assigning its passage to an inspected criterion. Marks and rationale are your hypothesis."));
            foreach(var id in Host.Session.InspectableSourceIds(InspectionLocation.ExperimentTable))
            { string work = id; Action("Work."+id,E.Format("workbenchview.openworklist.work.id.caption.inspect","Inspect {id}","id",id),()=>OpenWork(work)); }
            Action("Work.Folios",E.Get("workbenchview.openworklist.work.folios.caption.inspect.current.task.and.rubric.folios","Inspect current task and rubric folios"),()=>Gameplay.DirectRoute("studio"));
            Action("Work.History",E.Get("workbenchview.openworklist.work.history.caption.compare.current.marks.and.history","Compare current marks and history"),OpenHistory);
            Buttons(E.Get("workbenchview.openroutes.openroutes.heading.campus.routes","Campus routes"),OpenRoutes); FirstFocus();
        }
        public void OpenWork(string work)
        {
            var passage = Gameplay.InspectPassage(work); SelectedWork = work;
            Gameplay.SelectInspectedWork(work);
            ShowWork(work,passage);
        }
        // Restore the saved context before rendering. Neither operation inspects or selects new work.
        public void RestoreSelectedWork(string work) { SelectedWork=work; }
        public void RestoreWorkSurface(string work)
        {
            var passage=S.Notebook.SingleOrDefault(p=>p.Reference.SourceId==work);
            if(passage==null)throw new InvalidOperationException(E.Get("workbenchview.restoreworksurface.restoreworksurface.message.the.original.inspected.work.is.unavailable","The original inspected work is unavailable."));
            ShowWork(work,passage);
        }
        void ShowWork(string work,Passage passage)
        {
            Begin("work",E.Format("workbenchview.showwork.showwork.heading.current.marking","{work} | current marking","work",work));
            Text("Work.Literal",E.Format("workbenchview.showwork.work.literal.heading.literal.work.v","Literal work | v{passage_reference_version}","passage_reference_version",passage.Reference.Version),passage.DisplayText);
            Text("Work.Total",E.Format("workbenchview.showwork.work.total.heading.draft.hypothesis.20","Draft hypothesis: {s_drafttotal_work} / 20","s_drafttotal_work",S.DraftTotal(work)), E.Format("workbenchview.showwork.work.total.body.evaluation.is.unverified.offline.evaluation.is","{value0} Evaluation is unverified. Offline evaluation is unavailable for arbitrary prose.","value0",((S.Mode==SessionMode.Presenter)?E.Get("workbenchview.showwork.work.total.body.value0.authored.demonstration.hypothesis.authored.demonstration.hypothesis","Authored demonstration hypothesis."):E.Get("workbenchview.showwork.work.total.body.value0.learner.hypothesis.learner.hypothesis","Learner hypothesis."))));
            foreach(var c in S.Rubric.Criteria)
            {
                var criterion = c; bool inspected = S.Notebook.Any(p=>p.Reference.SourceId == WorkbenchModel.CriterionSource(S,c.Id));
                Action("Criterion."+c.Id, E.Format("workbenchview.showwork.criterion.c.id.caption.max","{c_questionid} | {c_id} | max {c_marks}{value5}","c_questionid",c.QuestionId,"c_id",c.Id,"c_marks",c.Marks,"value5",((inspected)?E.Get("workbenchview.showwork.criterion.c.id.caption.value5.assign.passage.assign.passage"," | Assign passage"):E.Get("workbenchview.showwork.criterion.c.id.caption.value5.inspect.at.folios.first.inspect.at.folios.first"," | Inspect at folios first"))),
                    ()=>OpenMark(work,criterion.Id),inspected);
            }
            Action("Work.Folios",E.Get("workbenchview.showwork.work.folios.caption.go.to.task.and.rubric.folios","Go to task and rubric folios"),()=>Gameplay.DirectRoute("studio"));
            Buttons(E.Get("workbenchview.showwork.showwork.primary.caption.other.work.rerun","Other work / rerun"),OpenWorkList); FirstFocus();
        }
        public void OpenMark(string work,string criterionId)
        {
            var draft = Model.OpenMark(work,criterionId); var criterion = S.Rubric.Criteria.Single(c=>c.Id==criterionId);
            Begin("mark",E.Get("workbenchview.openmark.openmark.heading.assign.passage.and.mark","Assign passage and mark"));
            Text("Mark.Link",work + " -> " + criterionId + " | " + criterion.QuestionId,
                S.Notebook.Single(p=>p.Reference.SourceId==work).DisplayText);
            Text("Mark.Criterion",E.Format("workbenchview.openmark.mark.criterion.heading.current.criterion.maximum","Current criterion | maximum {criterion_marks}","criterion_marks",criterion.Marks),criterion.Description);
            if(MarkContextWork)MarkContextWork.BindText(E.Format("workbenchview.openmark.work.inspected.passage.heading.inspected.passage","{work} | inspected passage","work",work),S.Notebook.Single(p=>p.Reference.SourceId==work).DisplayText,1);
            if(MarkContextCriterion)MarkContextCriterion.BindText(E.Format("workbenchview.openmark.criterion.questionid.criterionid.maximum.criterion.marks.heading.maximum","{criterion_questionid} | {criterionid} | maximum {criterion_marks}","criterion_questionid",criterion.QuestionId,"criterionid",criterionId,"criterion_marks",criterion.Marks),criterion.Description,1);
            Input("Mark.Marks",((((S.Mode==SessionMode.Presenter)?E.Get("workbenchview.openmark.mark.marks.label.value0.authored.numeric.mark.0.to.authored.numeric.mark.0.to","Authored numeric mark, 0 to "):E.Get("workbenchview.openmark.mark.marks.label.value0.your.numeric.mark.0.to.your.numeric.mark.0.to","Your numeric mark, 0 to ")))+(draft.Maximum)),draft.Marks,v=>draft.Marks=v);
            Input("Mark.Rationale",((S.Mode==SessionMode.Presenter)?E.Get("workbenchview.openmark.mark.rationale.label.authored.demonstration.rationale.authored.demonstration.rationale","Authored demonstration rationale"):E.Get("workbenchview.openmark.mark.rationale.label.your.rationale.your.rationale","Your rationale")),draft.Rationale,v=>draft.Rationale=v,true);
            Text("Mark.Scope",E.Get("workbenchview.openmark.mark.scope.heading.draft.hypothesis","Draft hypothesis"), E.Get("workbenchview.openmark.mark.scope.body.saving.records.your.exact.passage.criterion","Saving records your exact passage, criterion, mark and own rationale. It does not validate evaluation or approve publication."));
            Buttons(E.Get("workbenchview.openmark.openmark.primary.caption.save.annotation","Save annotation"),SaveAnnotation,E.Get("workbenchview.openmark.openmark.secondary.caption.back.to.work","Back to work"),()=>OpenWork(work)); RefreshMarkAvailability(); FirstFocus();
        }
        void RefreshMarkAvailability()
        {
            if(ViewId!="mark")return;
            var problem=Model.MarkProblem();Primary.interactable=problem==null;
            Message.text=((problem != null)?problem:E.Get("workbenchview.refreshmarkavailability.assign.message.text.ready.to.save.your.marking.ready.to.save.your.marking.hypothesis","Ready to save your marking hypothesis."));
        }
        public void SaveAnnotation()
        {
            Model.StageMark(); var r = Model.ConfirmPending(); Host.NotifyChanged();
            Message.text = ReceiptText(r);
            if(r.Status != ReceiptStatus.Accepted)
            {
                Action("Mark.Refresh",E.Get("workbenchview.saveannotation.mark.refresh.caption.refresh.staged.annotation.after.a.stale","Refresh staged annotation after a stale result"),()=>{Model.StageMark(true);Message.text=E.Get("workbenchview.saveannotation.assign.message.text.fresh.annotation.preview.staged.save.to","Fresh annotation preview staged. Save to submit it.");});
                RestoreEditorFocus();
            }
        }
        bool TaskInspected() => S.Task.Questions.All(q=>S.Notebook.Any(p=>p.Reference.SourceId=="D02.task.v"+S.Task.Version+"."+q.Id));
        bool RubricInspected() => S.Rubric.Criteria.All(c=>S.Notebook.Any(p=>p.Reference.SourceId==WorkbenchModel.CriterionSource(S,c.Id)));
        public void OpenTask()
        {
            if(!TaskInspected()) throw new InvalidOperationException(E.Get("workbenchview.opentask.opentask.message.open.both.current.task.passages.before","Open both current task passages before editing."));
            var d = Model.OpenTask(); Begin("task",((S.Mode==SessionMode.Presenter)?E.Get("workbenchview.opentask.opentask.heading.task.editor.authored.demonstration.task.editor.authored.demonstration","Task editor | authored demonstration"):E.Get("workbenchview.opentask.opentask.heading.task.editor.own.draft.task.editor.own.draft","Task editor | own draft")));
            Text("Task.Base",E.Format("workbenchview.opentask.task.base.heading.editing.task","Editing Task {d_baseversion}","d_baseversion",d.BaseVersion), E.Get("workbenchview.opentask.task.base.body.full.replacement.of.q1.and.q2","Full replacement of Q1 and Q2. Per-question marks must sum to 20. Accepted arithmetic is not semantic approval."));
            foreach(var q in d.Questions)
            {
                Input("Task."+q.Id+".Text",E.Format("workbenchview.opentask.task.q.id.text.label.question.text","{q_id} | question text","q_id",q.Id),q.Text,v=>q.Text=v,true);
                Input("Task."+q.Id+".Marks",E.Format("workbenchview.opentask.task.q.id.marks.label.marks","{q_id} | marks","q_id",q.Id),q.Marks,v=>q.Marks=v);
                Input("Task."+q.Id+".Outcomes",E.Format("workbenchview.opentask.task.q.id.outcomes.label.outcomes.o1.o2","{q_id} | outcomes (O1, O2)","q_id",q.Id),q.Outcomes,v=>q.Outcomes=v);
            }
            foreach(var id in new[]{"S_A","S_B"})
            {
                string source=id; bool admitted=S.Notebook.Any(p=>p.Reference.SourceId==id);
                Action("Task.Attach."+id,((((d.SourceIds.Contains(id))?E.Get("workbenchview.opentask.task.attach.id.caption.value0.attached.attached","Attached: "):E.Get("workbenchview.opentask.task.attach.id.caption.value0.not.attached.not.attached","Not attached: ")))+(id)+(((admitted)?E.Get("workbenchview.opentask.task.attach.id.caption.value2.toggle.toggle"," | Toggle"):E.Get("workbenchview.opentask.task.attach.id.caption.value2.inspect.at.archive.first.inspect.at.archive.first"," | Inspect at archive first")))),()=>
                { if(!d.SourceIds.Add(source)) d.SourceIds.Remove(source); OpenTask(); },admitted);
            }
            Input("Task.Context",((S.Mode==SessionMode.Presenter)?E.Get("workbenchview.opentask.task.context.label.authored.demonstration.decision.context.authored.demonstration.decision.context","Authored demonstration decision context"):E.Get("workbenchview.opentask.task.context.label.decision.context.in.your.own.decision.context.in.your.own.words","Decision context, in your own words")),d.Context,v=>d.Context=v,true);
            if(d.BaseVersion != S.Task.Version) Action("Task.Reload",E.Get("workbenchview.opentask.task.reload.caption.load.current.task.replace.this.local","Load current task (replace this local draft)"),()=>{Model.OpenTask(true);OpenTask();});
            Action("Task.Archive",E.Get("workbenchview.opentask.task.archive.caption.inspect.sources.at.archive.draft.retained","Inspect sources at archive (draft retained)"),()=>Gameplay.DirectRoute("archive"));
            Buttons(E.Get("workbenchview.opentask.opentask.primary.caption.preview.task","Preview task"),()=>Preview("task"),E.Get("workbenchview.opentask.opentask.secondary.caption.cancel.keep.draft","Cancel / keep draft"),OpenSourceList); FirstFocus();
        }
        public void OpenRubric()
        {
            if(!RubricInspected()) throw new InvalidOperationException(E.Get("workbenchview.openrubric.openrubric.message.open.each.current.criterion.passage.before","Open each current criterion passage before editing."));
            var d=Model.OpenRubric(); Begin("rubric",((S.Mode==SessionMode.Presenter)?E.Get("workbenchview.openrubric.openrubric.heading.rubric.editor.authored.demonstration.rubric.editor.authored.demonstration","Rubric editor | authored demonstration"):E.Get("workbenchview.openrubric.openrubric.heading.rubric.editor.own.draft.rubric.editor.own.draft","Rubric editor | own draft")));
            Text("Rubric.Base",E.Format("workbenchview.openrubric.rubric.base.heading.editing.rubric","Editing Rubric {d_baseversion}","d_baseversion",d.BaseVersion),E.Get("workbenchview.openrubric.rubric.base.body.free.descriptions.and.allocations.each.question","Free descriptions and allocations. Each question must reconcile to its current task marks. Outcomes: O1, O2 or O1,O2."));
            for(int i=0;i<d.Rows.Count;i++)
            {
                var c=d.Rows[i]; int row=i; var key="Rubric."+i;
                Input(key+".Id",E.Get("workbenchview.openrubric.key.id.label.criterion.id","Criterion ID"),c.Id,v=>c.Id=v);
                Input(key+".Question",E.Get("workbenchview.openrubric.key.question.label.question.id.q1.or.q2","Question ID (Q1 or Q2)"),c.QuestionId,v=>c.QuestionId=v);
                Input(key+".Outcomes",E.Get("workbenchview.openrubric.key.outcomes.label.outcome.ids","Outcome IDs"),c.Outcomes,v=>c.Outcomes=v);
                Input(key+".Marks",E.Get("workbenchview.openrubric.key.marks.label.maximum.marks","Maximum marks"),c.Marks,v=>c.Marks=v);
                Input(key+".Text",E.Get("workbenchview.openrubric.key.text.label.criterion.description","Criterion description"),c.Text,v=>c.Text=v,true);
                Action(key+".Remove",E.Format("workbenchview.openrubric.key.remove.caption.remove.criterion","Remove criterion {c_id}","c_id",c.Id),()=>{ d.Rows.RemoveAt(row); OpenRubric(); });
            }
            Action("Rubric.Add",E.Get("workbenchview.openrubric.rubric.add.caption.add.empty.criterion","Add empty criterion"),()=>{d.Rows.Add(new CriterionDraft{Id="",QuestionId="",Outcomes="",Marks="",Text=""});OpenRubric();});
            Text("Rubric.Allocation",E.Get("workbenchview.openrubric.rubric.allocation.heading.current.task.allocation","Current task allocation"),E.Format("workbenchview.openrubric.rubric.allocation.body.total.20","{value0} | Total 20","value0",string.Join(" | ",S.Task.Questions.Select(q=>((q.Id)+(": ")+(q.Marks))))));
            if(d.BaseVersion != S.Rubric.Version) Action("Rubric.Reload",E.Get("workbenchview.openrubric.rubric.reload.caption.load.current.rubric.replace.this.local","Load current rubric (replace this local draft)"),()=>{Model.OpenRubric(true);OpenRubric();});
            Buttons(E.Get("workbenchview.openrubric.openrubric.primary.caption.preview.rubric","Preview rubric"),()=>Preview("rubric"),E.Get("workbenchview.opentask.opentask.secondary.caption.cancel.keep.draft","Cancel / keep draft"),OpenSourceList); FirstFocus();
        }
        void Preview(string kind)
        {
            if(kind=="task") Model.PreviewTask(); else Model.PreviewRubric(); previewReturn=kind;
            ShowPendingPreview();
        }
        public void RestorePendingPreview()
        {
            if(!(Model.Pending?.Command is EditTask)&&!(Model.Pending?.Command is EditRubric))throw new InvalidOperationException(E.Get("workbenchview.restorependingpreview.restorependingpreview.message.no.original.task.or.rubric.preview","No original task or rubric preview is retained."));
            previewReturn=Model.Pending.Command is EditTask?"task":"rubric";ShowPendingPreview();
        }
        void ShowPendingPreview()
        {
            Begin("preview",E.Get("workbenchview.showpendingpreview.showpendingpreview.heading.before.after.preview","Before / after preview"));
            Text("Preview.Scope",E.Get("workbenchview.showpendingpreview.preview.scope.heading.exact.staged.change","Exact staged change"), E.Get("workbenchview.showpendingpreview.preview.scope.body.saving.creates.one.immutable.next.edition","Saving creates one immutable next edition. Existing annotations and approval become historical. No semantic review is implied."));
            if(Model.Pending.Command is EditTask t)
            {
                foreach(var q in t.Questions)
                {var before=S.Task.Questions.Single(x=>x.Id==q.Id); Text("Preview.Before."+q.Id,E.Format("workbenchview.showpendingpreview.preview.before.q.id.heading.before","{q_id} | before","q_id",q.Id),E.Format("workbenchview.showpendingpreview.preview.before.q.id.body.marks.outcomes","{before_text}\nMarks {before_marks} | Outcomes {value4}","before_text",before.Text,"before_marks",before.Marks,"value4",string.Join(",",before.OutcomeIds.Select(item=>item))));
                 Text("Preview.After."+q.Id,E.Format("workbenchview.showpendingpreview.preview.after.q.id.heading.after","{q_id} | after","q_id",q.Id),E.Format("workbenchview.showpendingpreview.preview.after.q.id.body.marks.outcomes","{q_text}\nMarks {q_marks} | Outcomes {value4}","q_text",q.Text,"q_marks",q.Marks,"value4",string.Join(",",q.OutcomeIds.Select(item=>item))));}
                Text("Preview.Context",E.Get("workbenchview.showpendingpreview.preview.context.heading.decision.context.before.after","Decision context before / after"),E.Format("workbenchview.showpendingpreview.preview.context.body.after","{value0}\nAfter: {t_decisioncontext}","value0",((S.Task.DecisionContext != null)?S.Task.DecisionContext:E.Get("workbenchview.showpendingpreview.preview.context.body.value0.no.context.no.context","No context")),"t_decisioncontext",t.DecisionContext));
                Text("Preview.Sources",E.Get("workbenchview.showpendingpreview.preview.sources.heading.attached.sources","Attached sources"),string.Join(", ",t.Sources.Select(x=>E.Format("workbenchview.showpendingpreview.preview.sources.body.item.v","{x_sourceid} v{x_version}","x_sourceid",x.SourceId,"x_version",x.Version))));
            }
            else if(Model.Pending.Command is EditRubric r)
            {
                foreach(var c in S.Rubric.Criteria) Text("Preview.Before."+c.Id,E.Format("workbenchview.showpendingpreview.preview.before.c.id.heading.before","Before | {c_id}","c_id",c.Id),E.Format("workbenchview.showpendingpreview.preview.before.c.id.body.marks","{c_questionid} | {c_marks} marks | {value4}\n{c_description}","c_questionid",c.QuestionId,"c_marks",c.Marks,"value4",string.Join(",",c.OutcomeIds.Select(item=>item)),"c_description",c.Description));
                foreach(var c in r.Allocations) Text("Preview.After."+c.Id,E.Format("workbenchview.showpendingpreview.preview.after.c.id.heading.after","After | {c_id}","c_id",c.Id),E.Format("workbenchview.showpendingpreview.preview.before.c.id.body.marks","{c_questionid} | {c_marks} marks | {value4}\n{c_description}","c_questionid",c.QuestionId,"c_marks",c.Marks,"value4",string.Join(",",c.OutcomeIds.Select(item=>item)),"c_description",c.Description));
            }
            Buttons(E.Get("workbenchview.showpendingpreview.showpendingpreview.primary.caption.save.new.edition","Save new edition"),CommitPreview,E.Get("presentationview.previewdomain.previewdomain.caption.keep.editing","Keep editing"),CancelPreview); FirstFocus();
        }
        public void CommitPreview()
        {
            var r=Model.ConfirmPending(); Host.NotifyChanged(); Message.text=ReceiptText(r);
            if(r.Status==ReceiptStatus.Accepted)
            {
                Primary.interactable=false; SecondaryText.text=E.Get("workbenchview.commitpreview.assign.secondarytext.text.inspect.current.edition","Inspect current edition");
                Secondary.onClick.RemoveAllListeners(); Secondary.onClick.AddListener(()=>Guard(()=>Gameplay.DirectRoute("studio")));
            }
        }
        public void CancelPreview()
        { Model.CancelPreview(); if(previewReturn=="task") OpenTask(); else OpenRubric(); RestoreEditorFocus(); }
        public void OpenHistory()
        {
            Begin("history",E.Get("workbenchview.openhistory.openhistory.heading.current.pair.and.immutable.history","Current pair and immutable history"));
            Text("History.Current",E.Format("workbenchview.openhistory.history.current.heading.current.task.rubric","Current Task {s_task_version} / Rubric {s_rubric_version}","s_task_version",S.Task.Version,"s_rubric_version",S.Rubric.Version),E.Format("workbenchview.openhistory.history.current.body.publication.review.current.annotations.current.grounded","Publication: {s_publication}. Review: {s_reviewstatus}. Current annotations: {s_currentannotations}. Current grounded evaluations: {s_currentevaluations}.","s_publication",S.Publication,"s_reviewstatus",S.ReviewStatus,"s_currentannotations",S.CurrentAnnotations.Count,"s_currentevaluations",S.CurrentEvaluations.Count));
            foreach(var work in new[]{"W_LIST","W_COMPARE","W_PITCH"})
                Text("History.Total."+work,E.Format("workbenchview.openhistory.history.total.work.heading.current.saved.draft.total.20","{work} | current saved draft total {s_drafttotal_work} / 20","work",work,"s_drafttotal_work",S.DraftTotal(work)),E.Get("workbenchview.openhistory.history.total.work.body.your.marking.hypothesis.a.full.total","Your marking hypothesis. A full total does not establish evaluation or publication approval."));
            foreach(var a in S.AnnotationHistory.Reverse())
                Text("History.Annotation."+a.Id,((((S.CurrentAnnotations.Any(x=>x.Id==a.Id))?E.Get("workbenchview.openhistory.history.annotation.a.id.heading.value0.current.hypothesis.current.hypothesis","Current hypothesis"):E.Get("workbenchview.openhistory.history.annotation.a.id.heading.value0.historical.annotation.historical.annotation","Historical annotation")))+(" | ")+(a.Content.SpecimenId)+(" / ")+(a.Content.CriterionId)),
                    E.Format("workbenchview.openhistory.history.annotation.a.id.body.task.rubric.revision.mark","Task {a_pair_taskversion} / Rubric {a_pair_rubricversion} | revision {a_revision} | mark {a_content_marks}\n{a_content_rationale}","a_pair_taskversion",a.Pair.TaskVersion,"a_pair_rubricversion",a.Pair.RubricVersion,"a_revision",a.Revision,"a_content_marks",a.Content.Marks,"a_content_rationale",a.Content.Rationale));
            foreach(var t in S.TaskHistory.Reverse())
                Text("History.Task."+t.Version,E.Format("workbenchview.openhistory.history.task.t.version.heading.task","Task {t_version}{value2}","t_version",t.Version,"value2",((t.Version==S.Task.Version)?E.Get("workbenchview.openhistory.history.task.t.version.heading.value2.current.current"," | Current"):E.Get("workbenchview.openhistory.history.task.t.version.heading.value2.historical.historical"," | Historical"))),
                    E.Format("workbenchview.openhistory.history.task.t.version.body.context","{value0}\nContext: {value2}","value0",string.Join("\n\n",t.Questions.Select(q=>E.Format("workbenchview.openhistory.history.task.t.version.body.value0.item.marks","{q_id} | {q_marks} marks | {value4}","q_id",q.Id,"q_marks",q.Marks,"value4",((S.Notebook.Any(p=>p.Reference.SourceId=="D02.task.v"+t.Version+"."+q.Id) || t.Version>1)?q.Text:E.Get("workbenchview.openhistory.history.task.t.version.body.value0.item.value4.not.inspected.open.at.the.not.inspected.open.at.the.task","Not inspected. Open at the task folios."))))),"value2",((t.DecisionContext != null)?t.DecisionContext:E.Get("workbenchview.openhistory.history.task.t.version.body.value2.not.supplied.not.supplied","Not supplied"))));
            foreach(var r in S.RubricHistory.Reverse())
                Text("History.Rubric."+r.Version,E.Format("workbenchview.openhistory.history.rubric.r.version.heading.rubric","Rubric {r_version}{value2}","r_version",r.Version,"value2",((r.Version==S.Rubric.Version)?E.Get("workbenchview.openhistory.history.task.t.version.heading.value2.current.current"," | Current"):E.Get("workbenchview.openhistory.history.task.t.version.heading.value2.historical.historical"," | Historical"))),
                    string.Join("\n\n",r.Criteria.Select(c=>E.Format("workbenchview.openhistory.history.rubric.r.version.body.item.marks","{c_id} | {c_questionid} | {c_marks} marks | {value6}","c_id",c.Id,"c_questionid",c.QuestionId,"c_marks",c.Marks,"value6",((S.Notebook.Any(p=>p.Reference.SourceId=="D02.rubric.v"+r.Version+"."+c.Id) || r.Version>1)?c.Description:E.Get("workbenchview.openhistory.history.rubric.r.version.body.item.value6.not.inspected.open.at.the.not.inspected.open.at.the.rubric","Not inspected. Open at the rubric folios."))))));
            Buttons(E.Get("workbenchview.openroutes.openroutes.heading.campus.routes","Campus routes"),OpenRoutes); FirstFocus();
        }
        void OpenReview()
        {
            if(ReviewRequested!=null){ReviewRequested();return;}
            Begin("review",E.Get("presentationview.openreview.openreview.heading.review.and.publication","Review and publication"));
            Text("Review.Scope",E.Get("workbenchview.openreview.review.scope.heading.current.pair","Current pair"), E.Format("workbenchview.openreview.review.scope.body.task.rubric.review.publication.the.cabinet","Task {s_task_version} / Rubric {s_rubric_version}\nReview: {s_reviewstatus}\nPublication: {s_publication}\nThe cabinet stays closed until an accepted current release. Offline authority dialogue is not available in this slice.","s_task_version",S.Task.Version,"s_rubric_version",S.Rubric.Version,"s_reviewstatus",S.ReviewStatus,"s_publication",S.Publication));
            Action("Review.History",E.Get("workbenchview.openreview.review.history.caption.inspect.current.pair.and.history","Inspect current pair and history"),OpenHistory); Buttons(E.Get("workbenchview.openroutes.openroutes.heading.campus.routes","Campus routes"),OpenRoutes); FirstFocus();
        }
        public void Pause()
        {if(PauseRequested!=null){PauseRequested();return;}Begin("pause",E.Get("presentationview.pause.pause.heading.paused","Paused")); Text("Pause.Info",E.Get("workbenchview.pause.pause.info.heading.offline.local.session","Offline local session"),E.Get("workbenchview.pause.pause.info.body.reading.typing.and.pausing.do.not","Reading, typing and pausing do not advance the logical clock. Drafts stay in this session.")); Buttons(E.Get("presenterrunner.refreshdock.assign.text.resume.resume","Resume"),CloseWindow,E.Get("workbenchview.openroutes.openroutes.heading.campus.routes","Campus routes"),OpenRoutes);FirstFocus();}
        public void Back()
        {
            switch(ViewId)
            {
                case "preview": CancelPreview(); return;
                case "mark": OpenWork(SelectedWork); return;
                case "work": OpenWorkList(); return;
                case "passage": if(passageReturn=="notebook")OpenNotebook();else OpenSourceList();return;
                case "task": case "rubric": OpenSourceList();return;
                default: CloseWindow();return;
            }
        }
        public void CloseWindow()
        {Window.SetActive(false); Gameplay.CloseInspection(); EventSystem.current?.SetSelectedGameObject(null); ViewId="world";ApplyInterfaceScale();}
        public void ToggleTextScale()
        {SetTextScale(TextScale<1.5f?2f:1f);}
        public void SetTextScale(float scale)
        {
            TextScale=Mathf.Clamp(scale,1,2);TextSizeLabel.text=E.Format("workbenchview.settextscale.assign.textsizelabel.text.text","Text {mathf_roundtoint_textscale_100}%","mathf_roundtoint_textscale_100",Mathf.RoundToInt(TextScale*100));
            ApplyInterfaceScale();
        }
        public void ReadMarkContext()
        {
            if(ViewId!="mark")return;
            EventSystem.current?.SetSelectedGameObject(null);
            Canvas.ForceUpdateCanvases();Scroll.verticalNormalizedPosition=1;
            Message.text=E.Get("workbenchview.readmarkcontext.assign.message.text.read.the.inspected.passage.and.criterion","Read the inspected passage and criterion above. Your marking draft is retained below.");
        }
        float lastHudWidth;
        float CanvasWidth=>((RectTransform)HudHeader.parent).rect.width;
        void LateUpdate(){if(HudHeader&&Mathf.Abs(CanvasWidth-lastHudWidth)>1)ApplyInterfaceScale();}
        public void ApplyInterfaceScale()
        {
            if(!HudHeader || !LeenaDock || !WorldHelp)return;
            bool large=TextScale>1f;float scale=TextScale;
            float canvasWidth=CanvasWidth;lastHudWidth=canvasWidth;
            bool overlay=Gameplay&&Gameplay.Workspace is PresentationView presentation&&presentation.Overlay&&presentation.Overlay.activeSelf;
            HudHeader.gameObject.SetActive(!overlay&&(!large || !IsOpen));
            HudHeader.anchorMin=HudHeader.anchorMax=HudHeader.pivot=new Vector2(0,1);
            HudHeader.anchoredPosition=new Vector2(14,-12);HudHeader.sizeDelta=large?new Vector2(420,82):(Style?Style.SessionSize:new Vector2(282,46));
            HudTitle.fontSize=Mathf.RoundToInt((Style?Style.HeadingFont-4:16)*scale);HudStatus.fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);
            Place(HudTitle.rectTransform,10,4,large?400:262,large?34:22);
            Place(HudStatus.rectTransform,10,large?40:26,large?400:262,large?34:16);
            var buttons=new[]{RoutesButton,NotebookButton,HistoryButton,TextSizeButton,PauseButton};
            var widths=new[]{64f,84f,70f,88f,60f};float total=widths.Sum()*scale+24;
            float x=canvasWidth-total-28,y=0;
            if(large&&x<440){x=0;y=94;}
            for(int i=0;i<buttons.Length;i++)if(buttons[i])
            {float width=widths[i]*scale;var label=buttons[i].GetComponentInChildren<Text>();label.fontSize=Mathf.RoundToInt((Style?Style.ControlFont:13)*scale);label.rectTransform.offsetMin=new Vector2(4,0);label.rectTransform.offsetMax=new Vector2(-4,0);Place((RectTransform)buttons[i].transform,x,y,width,(Style?Style.ControlHeight:32)*scale);x+=width+6;}
            LeenaDock.sizeDelta=large?new Vector2(330,100):(Style?Style.LeenaSize:new Vector2(200,62));
            LeenaIdentity.fontSize=Mathf.RoundToInt((Style?Style.BodyFont:14)*scale);LeenaStatus.fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);
            Place(LeenaIdentity.rectTransform,large?84:60,8,large?238:132,22*scale);
            Place(LeenaStatus.rectTransform,large?84:60,large?48:32,large?238:132,large?44:24);
            var leena=LeenaDock.GetComponent<LeenaHudProjection>();
            if(leena){if(leena.Portrait)Place(leena.Portrait.rectTransform,6,6,large?70:48,large?86:50);if(leena.CanonicalFallback)Place(leena.CanonicalFallback.rectTransform,6,6,large?70:48,large?86:50);}
            WorldHelp.gameObject.SetActive(!IsOpen&&!overlay&&(!Gameplay||!Gameplay.PresenterOwnsInput));
            WorldHelp.anchorMin=WorldHelp.anchorMax=new Vector2(.5f,0);WorldHelp.pivot=new Vector2(.5f,0);WorldHelp.anchoredPosition=new Vector2(0,12);WorldHelp.sizeDelta=new Vector2(large?460:430,large?74:32);
            if(Gameplay&&Gameplay.WorldPrompt)Gameplay.WorldPrompt.fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);
            var wr=(RectTransform)Window.transform;wr.anchorMin=wr.anchorMax=wr.pivot=new Vector2(1,1);
            wr.anchoredPosition=new Vector2(-14,large?-12:-64);wr.sizeDelta=new Vector2(large?canvasWidth-28:(Style?Style.WorkspaceWidth:450),large?((RectTransform)HudHeader.parent).rect.height-126:Mathf.Min(ViewId=="task"||ViewId=="rubric"||ViewId=="mark"||ViewId=="preview"?(Style?Style.PanelHeight:500):390,((RectTransform)HudHeader.parent).rect.height-150));
            Heading.fontSize=Mathf.RoundToInt((Style?Style.HeadingFont:20)*scale);PairLabel.fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);Message.fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);
            Heading.rectTransform.offsetMin=new Vector2(12,large?-58:-34);Heading.rectTransform.offsetMax=new Vector2(large?-90:-50,-6);
            PairLabel.rectTransform.offsetMin=new Vector2(12,large?-94:-54);PairLabel.rectTransform.offsetMax=new Vector2(-12,large?-60:-36);
            var closeRect=(RectTransform)Close.transform;closeRect.sizeDelta=new Vector2(32*scale,30*scale);Close.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((Style?Style.BodyFont:14)*scale);var closeText=Close.GetComponentInChildren<Text>();closeText.alignment=TextAnchor.MiddleCenter;closeText.rectTransform.offsetMin=Vector2.zero;closeText.rectTransform.offsetMax=Vector2.zero;
            var scrollRect=(RectTransform)Scroll.transform;scrollRect.offsetMin=new Vector2(12,large?154:92);scrollRect.offsetMax=new Vector2(-12,large?-100:-60);
            Message.rectTransform.offsetMin=new Vector2(12,large?76:48);Message.rectTransform.offsetMax=new Vector2(-12,large?148:86);
            var first=(RectTransform)Primary.transform;var second=(RectTransform)Secondary.transform;
            first.offsetMin=new Vector2(12,10);first.offsetMax=new Vector2(-4,large?70:42);second.offsetMin=new Vector2(4,10);second.offsetMax=new Vector2(-12,large?70:42);
            PrimaryText.fontSize=Mathf.RoundToInt((Style?Style.BodyFont:14)*scale);SecondaryText.fontSize=Mathf.RoundToInt((Style?Style.BodyFont:14)*scale);
            if(WorkspaceTextSizeButton){WorkspaceTextSizeButton.gameObject.SetActive(large&&IsOpen);WorkspaceTextSizeButton.GetComponentInChildren<Text>().text=E.Format("workbenchview.applyinterfacescale.assign.text.text","Text {mathf_roundtoint_scale_100}%","mathf_roundtoint_scale_100",Mathf.RoundToInt(scale*100));WorkspaceTextSizeButton.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((Style?Style.ControlFont:13)*scale);}
            // Literal work and criterion are the first scroll rows. One context action
            // returns to those rows instead of covering the actor with a second panel.
            if(MarkContextPanel)MarkContextPanel.SetActive(false);
            if(MarkContextButton){MarkContextButton.gameObject.SetActive(ViewId=="mark"&&IsOpen);var r=(RectTransform)MarkContextButton.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(large?358:226,18);r.sizeDelta=new Vector2(128*scale,32*scale);MarkContextButton.GetComponentInChildren<Text>().text=E.Get("workbenchview.applyinterfacescale.assign.text.read.context","Read context");MarkContextButton.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((Style?Style.ControlFont:13)*scale);}
            foreach(var row in Rows.GetComponentsInChildren<NativeRow>(true))row.ApplyScale(scale);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Rows.transform);
        }
        static void Place(RectTransform r,float x,float y,float width,float height)
        {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        public bool SetFieldText(string controlId,string value)
        {
            var row=Rows.Find(controlId); if(!row || !row.Input || Gameplay.IsComposing)return false;
            row.Input.Select();row.Input.ActivateInputField();row.Input.text=value;row.Input.caretPosition=value.Length;row.Input.ForceLabelUpdate(); Focus.EnsureVisible(row.Input.gameObject);return true;
        }
        public bool ActivateControl(string controlId)
        {
            Button button;
            switch(controlId)
            {
                case "Primary":button=Primary;break;case "Secondary":button=Secondary;break;case "Close":button=Close;break;
                case "Routes":button=RoutesButton;break;case "Notebook":button=NotebookButton;break;case "History":button=HistoryButton;break;
                case "TextSize":button=WorkspaceTextSizeButton && WorkspaceTextSizeButton.gameObject.activeInHierarchy?WorkspaceTextSizeButton:TextSizeButton;break;
                case "ReadContext":button=MarkContextButton;break;
                default:var row=Rows.Find(controlId);button=row?row.Action:null;break;
            }
            if(!button || !button.gameObject.activeInHierarchy || !button.IsInteractable())return false;
            EventSystem.current?.SetSelectedGameObject(button.gameObject);button.onClick.Invoke();return true;
        }
        string ReceiptText(CommandReceipt r) => ((r.Status==ReceiptStatus.Accepted)?E.Format("workbenchview.receipttext.return.saved.authored.hypothesis.evaluation.unverified.saved","Saved. {value1}","value1",((r.Action=="experiment.annotate")?((S.Mode==SessionMode.Presenter)?E.Get("workbenchview.receipttext.return.saved.authored.hypothesis.evaluation.unverified.value1.authored.hypothesis.evaluation.unverified.learner.authored.hypothesis.evaluation.unverified.authored.hypothesis.evaluation.unverified","Authored hypothesis; evaluation unverified."):E.Get("workbenchview.receipttext.return.saved.authored.hypothesis.evaluation.unverified.value1.authored.hypothesis.evaluation.unverified.learner.learner.hypothesis.evaluation.unverified.learner.hypothesis.evaluation.unverified","Learner hypothesis; evaluation unverified.")):E.Get("workbenchview.receipttext.return.saved.authored.hypothesis.evaluation.unverified.value1.new.edition.accepted.earlier.annotations.new.edition.accepted.earlier.annotations.and","New edition accepted. Earlier annotations and approvals are historical."))):E.Format("workbenchview.receipttext.return.your.draft.and.original.request.your.draft.and.original.request.are","{r_status}: {r_reasoncode}. Your draft and original request are retained. Reconcile or explicitly preview a fresh change.","r_status",r.Status,"r_reasoncode",r.ReasonCode));
    }
}
