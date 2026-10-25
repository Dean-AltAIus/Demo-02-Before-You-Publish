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
        public string LastEditedControl { get; private set; }
        string previewReturn;
        string passageReturn;
        CampusStation station;
        WorkbenchModel Model => Host.Workbench;
        SessionSnapshot S => Host.Session.Snapshot;
        void Start()
        {
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
            var s = S; string pair = "Task " + s.Task.Version + " / Rubric " + s.Rubric.Version;
            PairLabel.text = pair + "   |   " + (ViewId=="mark" && Model.Mark!=null ? Model.Mark.Work.SourceId+" > "+Model.Mark.CriterionId : s.Publication.ToString());
            HudStatus.text = pair + "   |   " + (s.ClockMinute / 60).ToString("00") + ":" + (s.ClockMinute % 60).ToString("00") + "   |   Offline";
        }
        void Begin(string view, string title)
        {
            BindingGeneration++;
            ViewId = view; Window.SetActive(true); Heading.text = title; Message.text = ""; Rows.Begin();
            Primary.gameObject.SetActive(false); Secondary.gameObject.SetActive(false);
            Primary.interactable = true; Secondary.interactable = true;
            Primary.onClick.RemoveAllListeners(); Secondary.onClick.RemoveAllListeners();
            Scroll.verticalNormalizedPosition = 1; UpdateStatus(); EventSystem.current?.SetSelectedGameObject(null);
            ApplyInterfaceScale();
        }
        NativeRow Text(string id, string title, string body) { var row = Rows.Take(NativeRowKind.Text,id); row.BindText(title,Display(body),TextScale); return row; }
        NativeRow Action(string id, string caption, System.Action callback, bool available = true)
        { var row = Rows.Take(NativeRowKind.Action,id); row.BindAction(caption, () => Guard(callback),TextScale,available); return row; }
        NativeRow Input(string id, string title, string value, Action<string> changed, bool multiline = false)
        {
            var row = Rows.Take(NativeRowKind.Input,id);
            row.BindInput(title,Display(value),v => { LastEditedControl = id; changed(v); Message.text = "Draft retained locally. Preview before committing."; RefreshMarkAvailability(); }, TextScale,multiline);
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
            if (Gameplay.IsComposing) { Message.text = "Finish composing text before this action."; return; }
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
            Begin("routes","Campus routes");
            Text("Routes.Intro","Choose a destination", "Direct routes reach the same station, inspection and controls. Travel carries no assessment credit.");
            foreach (var s in Gameplay.Stations) { var target = s; Action("Route." + s.StationId,s.DisplayName,() => Gameplay.DirectRoute(target.StationId)); }
            Action("Route.Notebook","Open inspected notebook",OpenNotebook); Action("Route.History","Open version history",OpenHistory);
            Buttons("Return to campus",CloseWindow); FirstFocus();
        }
        public void OpenStation(CampusStation target)
        {
            station = target;
            switch(target.Kind) { case StationKind.Experiment: OpenWorkList(); break; case StationKind.Review: OpenReview(); break; default: OpenSourceList(); break; }
        }
        public void OpenSourceList()
        {
            Begin("sources",station.DisplayName);
            Text("Sources.Intro","Inspect a literal passage", "Only the passage you open is admitted to your notebook. Inspection itself is not evidence of understanding.");
            foreach(var id in Host.Session.InspectableSourceIds(station.Location))
            {
                if (station.Kind == StationKind.Studio && id.StartsWith("D02.task.v") && !id.StartsWith("D02.task.v" + S.Task.Version + ".")) continue;
                if (station.Kind == StationKind.Studio && id.StartsWith("D02.rubric.v") && !id.StartsWith("D02.rubric.v" + S.Rubric.Version + ".")) continue;
                string selected = id; Action("Inspect."+id,id + (S.Notebook.Any(p=>p.Reference.SourceId==id) ? "  |  Inspected" : "  |  Open"),()=>OpenPassage(selected,"sources"));
            }
            if(station.Kind == StationKind.Studio)
            {
                Action("Editor.Task","Edit full task",OpenTask); Action("Editor.Rubric","Edit full rubric",OpenRubric);
                Action("Studio.Experiment","Go to the experiment table",()=>Gameplay.DirectRoute("experiment"));
            }
            Buttons("Campus routes",OpenRoutes); FirstFocus();
        }
        public void OpenPassage(string sourceId,string returnTo)
        {
            var passage = returnTo == "notebook" ? Host.Session.Inspect(sourceId,InspectionLocation.PortableWorkspace) : Gameplay.InspectPassage(sourceId);
            passageReturn = returnTo; Begin("passage",sourceId);
            Text("Passage.Body","Literal passage",passage.DisplayText);
            Text("Passage.Provenance","Source identity",passage.Reference.SourceId + " | Version " + passage.Reference.Version + "\nPassage " + passage.Reference.PassageId + "\nSHA-256 " + passage.Reference.Sha256);
            Buttons("Back",()=> { if(returnTo == "notebook") OpenNotebook(); else OpenSourceList(); }); FirstFocus();
        }
        public void OpenNotebook()
        {
            Begin("notebook","Inspected notebook");
            Text("Notebook.Intro","Permitted content only", S.Notebook.Count == 0 ? "Your notebook is empty. Inspect a passage at a campus station." : "These are your inspected passages, with their original identities. Earlier document editions remain labelled by version.");
            foreach(var passage in S.Notebook)
            {
                Text("Notebook." + passage.Reference.SourceId, passage.Reference.SourceId + " | v" + passage.Reference.Version,passage.DisplayText);
            }
            Buttons("Campus routes",OpenRoutes); FirstFocus();
        }
        public void OpenWorkList()
        {
            Begin("works","Experiment table");
            Text("Work.Intro","Choose fictional work", "Open the literal work before assigning its passage to an inspected criterion. Marks and rationale are your hypothesis.");
            foreach(var id in Host.Session.InspectableSourceIds(InspectionLocation.ExperimentTable))
            { string work = id; Action("Work."+id,"Inspect " + id,()=>OpenWork(work)); }
            Action("Work.Folios","Inspect current task and rubric folios",()=>Gameplay.DirectRoute("studio"));
            Action("Work.History","Compare current marks and history",OpenHistory);
            Buttons("Campus routes",OpenRoutes); FirstFocus();
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
            if(passage==null)throw new InvalidOperationException("The original inspected work is unavailable.");
            ShowWork(work,passage);
        }
        void ShowWork(string work,Passage passage)
        {
            Begin("work",work + " | current marking");
            Text("Work.Literal","Literal work | v"+passage.Reference.Version,passage.DisplayText);
            Text("Work.Total","Saved draft total: " + S.DraftTotal(work) + " / 20", (S.Mode==SessionMode.Presenter?"Authored demonstration hypothesis.":"Learner hypothesis.")+" Evaluation is unverified. Offline evaluation is unavailable for arbitrary prose.");
            foreach(var c in S.Rubric.Criteria)
            {
                var criterion = c; bool inspected = S.Notebook.Any(p=>p.Reference.SourceId == WorkbenchModel.CriterionSource(S,c.Id));
                Action("Criterion."+c.Id, c.QuestionId + " | " + c.Id + " | max " + c.Marks + (inspected ? " | Assign passage" : " | Inspect at folios first"),
                    ()=>OpenMark(work,criterion.Id),inspected);
            }
            Action("Work.Folios","Go to task and rubric folios",()=>Gameplay.DirectRoute("studio"));
            Buttons("Other work / rerun",OpenWorkList); FirstFocus();
        }
        public void OpenMark(string work,string criterionId)
        {
            var draft = Model.OpenMark(work,criterionId); var criterion = S.Rubric.Criteria.Single(c=>c.Id==criterionId);
            Begin("mark","Assign passage and mark");
            Text("Mark.Link",work + " -> " + criterionId + " | " + criterion.QuestionId,
                S.Notebook.Single(p=>p.Reference.SourceId==work).DisplayText);
            Text("Mark.Criterion","Current criterion | maximum " + criterion.Marks,criterion.Description);
            if(MarkContextWork)MarkContextWork.BindText(work+" | inspected passage",Display(S.Notebook.Single(p=>p.Reference.SourceId==work).DisplayText),1);
            if(MarkContextCriterion)MarkContextCriterion.BindText(criterion.QuestionId+" | "+criterionId+" | maximum "+criterion.Marks,Display(criterion.Description),1);
            Input("Mark.Marks",(S.Mode==SessionMode.Presenter?"Authored numeric mark, 0 to ":"Your numeric mark, 0 to ") + draft.Maximum,draft.Marks,v=>draft.Marks=v);
            Input("Mark.Rationale",S.Mode==SessionMode.Presenter?"Authored demonstration rationale":"Your rationale",draft.Rationale,v=>draft.Rationale=v,true);
            Text("Mark.Scope","Draft hypothesis", "Saving records your exact passage, criterion, mark and own rationale. It does not validate evaluation or approve publication.");
            Buttons("Save annotation",SaveAnnotation,"Back to work",()=>OpenWork(work)); RefreshMarkAvailability(); FirstFocus();
        }
        void RefreshMarkAvailability()
        {
            if(ViewId!="mark")return;
            var problem=Model.MarkProblem();Primary.interactable=problem==null;
            Message.text=problem??"Ready to save your marking hypothesis.";
        }
        public void SaveAnnotation()
        {
            Model.StageMark(); var r = Model.ConfirmPending(); Host.NotifyChanged();
            Message.text = ReceiptText(r);
            if(r.Status != ReceiptStatus.Accepted)
            {
                Action("Mark.Refresh","Refresh staged annotation after a stale result",()=>{Model.StageMark(true);Message.text="Fresh annotation preview staged. Save to submit it.";});
                RestoreEditorFocus();
            }
        }
        bool TaskInspected() => S.Task.Questions.All(q=>S.Notebook.Any(p=>p.Reference.SourceId=="D02.task.v"+S.Task.Version+"."+q.Id));
        bool RubricInspected() => S.Rubric.Criteria.All(c=>S.Notebook.Any(p=>p.Reference.SourceId==WorkbenchModel.CriterionSource(S,c.Id)));
        public void OpenTask()
        {
            if(!TaskInspected()) throw new InvalidOperationException("Open both current task passages before editing.");
            var d = Model.OpenTask(); Begin("task",S.Mode==SessionMode.Presenter?"Task editor | authored demonstration":"Task editor | own draft");
            Text("Task.Base","Editing Task " + d.BaseVersion, "Full replacement of Q1 and Q2. Per-question marks must sum to 20. Accepted arithmetic is not semantic approval.");
            foreach(var q in d.Questions)
            {
                Input("Task."+q.Id+".Text",q.Id+" | question text",q.Text,v=>q.Text=v,true);
                Input("Task."+q.Id+".Marks",q.Id+" | marks",q.Marks,v=>q.Marks=v);
                Input("Task."+q.Id+".Outcomes",q.Id+" | outcomes (O1, O2)",q.Outcomes,v=>q.Outcomes=v);
            }
            foreach(var id in new[]{"S_A","S_B"})
            {
                string source=id; bool admitted=S.Notebook.Any(p=>p.Reference.SourceId==id);
                Action("Task.Attach."+id,(d.SourceIds.Contains(id)?"Attached: ":"Not attached: ")+id+(admitted?" | Toggle":" | Inspect at archive first"),()=>
                { if(!d.SourceIds.Add(source)) d.SourceIds.Remove(source); OpenTask(); },admitted);
            }
            Input("Task.Context",S.Mode==SessionMode.Presenter?"Authored demonstration decision context":"Decision context, in your own words",d.Context,v=>d.Context=v,true);
            if(d.BaseVersion != S.Task.Version) Action("Task.Reload","Load current task (replace this local draft)",()=>{Model.OpenTask(true);OpenTask();});
            Action("Task.Archive","Inspect sources at archive (draft retained)",()=>Gameplay.DirectRoute("archive"));
            Buttons("Preview task",()=>Preview("task"),"Cancel / keep draft",OpenSourceList); FirstFocus();
        }
        public void OpenRubric()
        {
            if(!RubricInspected()) throw new InvalidOperationException("Open each current criterion passage before editing.");
            var d=Model.OpenRubric(); Begin("rubric",S.Mode==SessionMode.Presenter?"Rubric editor | authored demonstration":"Rubric editor | own draft");
            Text("Rubric.Base","Editing Rubric "+d.BaseVersion,"Free descriptions and allocations. Each question must reconcile to its current task marks. Outcomes: O1, O2 or O1,O2.");
            for(int i=0;i<d.Rows.Count;i++)
            {
                var c=d.Rows[i]; int row=i; var key="Rubric."+i;
                Input(key+".Id","Criterion ID",c.Id,v=>c.Id=v);
                Input(key+".Question","Question ID (Q1 or Q2)",c.QuestionId,v=>c.QuestionId=v);
                Input(key+".Outcomes","Outcome IDs",c.Outcomes,v=>c.Outcomes=v);
                Input(key+".Marks","Maximum marks",c.Marks,v=>c.Marks=v);
                Input(key+".Text","Criterion description",c.Text,v=>c.Text=v,true);
                Action(key+".Remove","Remove criterion "+c.Id,()=>{ d.Rows.RemoveAt(row); OpenRubric(); });
            }
            Action("Rubric.Add","Add empty criterion",()=>{d.Rows.Add(new CriterionDraft{Id="",QuestionId="",Outcomes="",Marks="",Text=""});OpenRubric();});
            Text("Rubric.Allocation","Current task allocation",string.Join(" | ",S.Task.Questions.Select(q=>q.Id+": "+q.Marks))+" | Total 20");
            if(d.BaseVersion != S.Rubric.Version) Action("Rubric.Reload","Load current rubric (replace this local draft)",()=>{Model.OpenRubric(true);OpenRubric();});
            Buttons("Preview rubric",()=>Preview("rubric"),"Cancel / keep draft",OpenSourceList); FirstFocus();
        }
        void Preview(string kind)
        {
            if(kind=="task") Model.PreviewTask(); else Model.PreviewRubric(); previewReturn=kind;
            ShowPendingPreview();
        }
        public void RestorePendingPreview()
        {
            if(!(Model.Pending?.Command is EditTask)&&!(Model.Pending?.Command is EditRubric))throw new InvalidOperationException("No original task or rubric preview is retained.");
            previewReturn=Model.Pending.Command is EditTask?"task":"rubric";ShowPendingPreview();
        }
        void ShowPendingPreview()
        {
            Begin("preview","Before / after preview");
            Text("Preview.Scope","Exact staged change", "Saving creates one immutable next edition. Existing annotations and approval become historical. No semantic review is implied.");
            if(Model.Pending.Command is EditTask t)
            {
                foreach(var q in t.Questions)
                {var before=S.Task.Questions.Single(x=>x.Id==q.Id); Text("Preview.Before."+q.Id,q.Id+" | before",before.Text+"\nMarks "+before.Marks+" | Outcomes "+string.Join(",",before.OutcomeIds));
                 Text("Preview.After."+q.Id,q.Id+" | after",q.Text+"\nMarks "+q.Marks+" | Outcomes "+string.Join(",",q.OutcomeIds));}
                Text("Preview.Context","Decision context before / after",(S.Task.DecisionContext??"No context")+"\nAfter: "+t.DecisionContext);
                Text("Preview.Sources","Attached sources",string.Join(", ",t.Sources.Select(x=>x.SourceId+" v"+x.Version)));
            }
            else if(Model.Pending.Command is EditRubric r)
            {
                foreach(var c in S.Rubric.Criteria) Text("Preview.Before."+c.Id,"Before | "+c.Id,c.QuestionId+" | "+c.Marks+" marks | "+string.Join(",",c.OutcomeIds)+"\n"+c.Description);
                foreach(var c in r.Allocations) Text("Preview.After."+c.Id,"After | "+c.Id,c.QuestionId+" | "+c.Marks+" marks | "+string.Join(",",c.OutcomeIds)+"\n"+c.Description);
            }
            Buttons("Save new edition",CommitPreview,"Keep editing",CancelPreview); FirstFocus();
        }
        public void CommitPreview()
        {
            var r=Model.ConfirmPending(); Host.NotifyChanged(); Message.text=ReceiptText(r);
            if(r.Status==ReceiptStatus.Accepted)
            {
                Primary.interactable=false; SecondaryText.text="Inspect current edition";
                Secondary.onClick.RemoveAllListeners(); Secondary.onClick.AddListener(()=>Guard(()=>Gameplay.DirectRoute("studio")));
            }
        }
        public void CancelPreview()
        { Model.CancelPreview(); if(previewReturn=="task") OpenTask(); else OpenRubric(); RestoreEditorFocus(); }
        public void OpenHistory()
        {
            Begin("history","Current pair and immutable history");
            Text("History.Current","Current Task "+S.Task.Version+" / Rubric "+S.Rubric.Version,"Publication: "+S.Publication+". Review: "+S.ReviewStatus+". Current annotations: "+S.CurrentAnnotations.Count+". Current grounded evaluations: "+S.CurrentEvaluations.Count+".");
            foreach(var work in new[]{"W_LIST","W_COMPARE","W_PITCH"})
                Text("History.Total."+work,work+" | current saved draft total "+S.DraftTotal(work)+" / 20","Your marking hypothesis. A full total does not establish evaluation or publication approval.");
            foreach(var a in S.AnnotationHistory.Reverse())
                Text("History.Annotation."+a.Id,(S.CurrentAnnotations.Any(x=>x.Id==a.Id)?"Current hypothesis":"Historical annotation")+" | "+a.Content.SpecimenId+" / "+a.Content.CriterionId,
                    "Task "+a.Pair.TaskVersion+" / Rubric "+a.Pair.RubricVersion+" | revision "+a.Revision+" | mark "+a.Content.Marks+"\n"+a.Content.Rationale);
            foreach(var t in S.TaskHistory.Reverse())
                Text("History.Task."+t.Version,"Task "+t.Version+(t.Version==S.Task.Version?" | Current":" | Historical"),
                    string.Join("\n\n",t.Questions.Select(q=>q.Id+" | "+q.Marks+" marks | "+
                        (S.Notebook.Any(p=>p.Reference.SourceId=="D02.task.v"+t.Version+"."+q.Id) || t.Version>1 ? q.Text : "Not inspected. Open at the task folios.")))+
                    "\nContext: "+(t.DecisionContext??"Not supplied"));
            foreach(var r in S.RubricHistory.Reverse())
                Text("History.Rubric."+r.Version,"Rubric "+r.Version+(r.Version==S.Rubric.Version?" | Current":" | Historical"),
                    string.Join("\n\n",r.Criteria.Select(c=>c.Id+" | "+c.QuestionId+" | "+c.Marks+" marks | "+
                        (S.Notebook.Any(p=>p.Reference.SourceId=="D02.rubric.v"+r.Version+"."+c.Id) || r.Version>1 ? c.Description : "Not inspected. Open at the rubric folios."))));
            Buttons("Campus routes",OpenRoutes); FirstFocus();
        }
        void OpenReview()
        {
            if(ReviewRequested!=null){ReviewRequested();return;}
            Begin("review","Review and publication");
            Text("Review.Scope","Current pair", "Task "+S.Task.Version+" / Rubric "+S.Rubric.Version+"\nReview: "+S.ReviewStatus+"\nPublication: "+S.Publication+"\nThe cabinet stays closed until an accepted current release. Offline authority dialogue is not available in this slice.");
            Action("Review.History","Inspect current pair and history",OpenHistory); Buttons("Campus routes",OpenRoutes); FirstFocus();
        }
        public void Pause()
        {if(PauseRequested!=null){PauseRequested();return;}Begin("pause","Paused"); Text("Pause.Info","Offline local session","Reading, typing and pausing do not advance the logical clock. Drafts stay in this session."); Buttons("Resume",CloseWindow,"Campus routes",OpenRoutes);FirstFocus();}
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
            TextScale=Mathf.Clamp(scale,1,2);TextSizeLabel.text="Text "+Mathf.RoundToInt(TextScale*100)+"%";
            ApplyInterfaceScale();
        }
        public void ReadMarkContext()
        {
            if(ViewId!="mark")return;
            EventSystem.current?.SetSelectedGameObject(null);
            Canvas.ForceUpdateCanvases();Scroll.verticalNormalizedPosition=1;
            Message.text="Read the inspected passage and criterion above. Your marking draft is retained below.";
        }
        public void ApplyInterfaceScale()
        {
            if(!HudHeader || !LeenaDock || !WorldHelp)return;
            bool large=TextScale>1f;float scale=TextScale;
            HudHeader.gameObject.SetActive(!large || !IsOpen);
            HudHeader.sizeDelta=new Vector2(0,large?202:78);
            HudTitle.fontSize=Mathf.RoundToInt(23*scale);HudStatus.fontSize=Mathf.RoundToInt(17*scale);
            Place(HudTitle.rectTransform,18,10,large?1220:410,large?58:30);
            Place(HudStatus.rectTransform,18,large?70:44,large?1220:510,large?48:24);
            var buttons=new[]{RoutesButton,NotebookButton,HistoryButton,TextSizeButton,PauseButton};
            var normalX=new[]{560f,692,840,972,1132};var normalWidth=new[]{120f,136,120,148,130};
            var largeX=new[]{18f,230,478,690,998};var largeWidth=new[]{200f,236,200,296,264};
            for(int i=0;i<buttons.Length;i++)if(buttons[i])
            {buttons[i].GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(20*scale);Place((RectTransform)buttons[i].transform,large?largeX[i]:normalX[i],large?122:20,large?largeWidth[i]:normalWidth[i],large?68:40);}
            LeenaDock.sizeDelta=large?new Vector2(684,146):new Vector2(342,112);
            LeenaIdentity.fontSize=Mathf.RoundToInt(21*scale);LeenaStatus.fontSize=Mathf.RoundToInt(17*scale);
            Place(LeenaIdentity.rectTransform,112,large?10:20,large?558:220,large?54:32);
            Place(LeenaStatus.rectTransform,112,large?62:58,large?558:220,large?82:40);
            WorldHelp.gameObject.SetActive(!IsOpen);
            WorldHelp.anchorMin=new Vector2(large?.56f:.29f,0);WorldHelp.anchorMax=new Vector2(.99f,0);WorldHelp.sizeDelta=new Vector2(0,large?128:48);
            Gameplay.WorldPrompt.fontSize=Mathf.RoundToInt(18*scale);
            var wr=(RectTransform)Window.transform;wr.anchorMin=new Vector2(large?.015f:.30f,0);wr.anchorMax=new Vector2(.99f,1);
            wr.offsetMin=new Vector2(0,large?152:76);wr.offsetMax=new Vector2(0,large?-12:-90);
            Heading.fontSize=Mathf.RoundToInt(27*scale);PairLabel.fontSize=Mathf.RoundToInt(17*scale);Message.fontSize=Mathf.RoundToInt(17*scale);
            Heading.rectTransform.offsetMin=new Vector2(20,large?-76:-44);Heading.rectTransform.offsetMax=new Vector2(large?-110:-66,-10);
            PairLabel.rectTransform.offsetMin=new Vector2(20,large?-124:-72);PairLabel.rectTransform.offsetMax=new Vector2(-20,large?-82:-46);
            var closeRect=(RectTransform)Close.transform;closeRect.sizeDelta=large?new Vector2(80,68):new Vector2(44,40);Close.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(20*scale);
            var scrollRect=(RectTransform)Scroll.transform;scrollRect.offsetMin=new Vector2(16,large?218:132);scrollRect.offsetMax=new Vector2(-16,large?-132:-80);
            Message.rectTransform.offsetMin=new Vector2(20,large?102:64);Message.rectTransform.offsetMax=new Vector2(-20,large?210:124);
            var first=(RectTransform)Primary.transform;var second=(RectTransform)Secondary.transform;
            first.offsetMin=new Vector2(20,12);first.offsetMax=new Vector2(-8,large?92:56);second.offsetMin=new Vector2(8,12);second.offsetMax=new Vector2(-20,large?92:56);
            PrimaryText.fontSize=Mathf.RoundToInt(20*scale);SecondaryText.fontSize=Mathf.RoundToInt(20*scale);
            if(WorkspaceTextSizeButton){WorkspaceTextSizeButton.gameObject.SetActive(large&&IsOpen);WorkspaceTextSizeButton.GetComponentInChildren<Text>().text="Text "+Mathf.RoundToInt(scale*100)+"%";WorkspaceTextSizeButton.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(16*scale);}
            if(MarkContextPanel)MarkContextPanel.SetActive(ViewId=="mark" && IsOpen && !large);
            if(MarkContextButton){MarkContextButton.gameObject.SetActive(ViewId=="mark" && IsOpen && large);MarkContextButton.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(16*scale);}
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
        string ReceiptText(CommandReceipt r) => r.Status==ReceiptStatus.Accepted ? "Saved. "+(r.Action=="experiment.annotate"?(S.Mode==SessionMode.Presenter?"Authored hypothesis; evaluation unverified.":"Learner hypothesis; evaluation unverified."):"New edition accepted. Earlier annotations and approvals are historical.") :
            r.Status+": "+r.ReasonCode+". Your draft and original request are retained. Reconcile or explicitly preview a fresh change.";
        public static string Display(string text) => (text??"").Replace("\u2013"," to ").Replace("\u2014",", ");
    }
}
