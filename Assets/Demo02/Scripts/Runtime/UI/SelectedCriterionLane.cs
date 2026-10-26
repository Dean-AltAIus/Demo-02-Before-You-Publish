using System;
using System.Linq;
using System.Text;
using Altaius.Demo02.Gameplay;
using UnityEngine;
namespace Altaius.Demo02.UI
{
    /// <summary>The reusable physical lane is the exact criterion selected through the native placement controls.</summary>
    [DisallowMultipleComponent]
    public sealed class SelectedCriterionLane:MonoBehaviour
    {
        public WorkbenchView Workbench;public TextMesh Identity,LiteralCriterion,Mark;
        public string SelectedCriterionId {get;private set;}
        public string SelectedWorkId {get;private set;}
        public string PairIdentity {get;private set;}
        public bool CurrentSelection {get;private set;}
        Guid attempt;string signature;
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!Workbench||!Workbench.Host||Workbench.Host.Session==null)return;
            var s=Workbench.Host.Session.Snapshot;var model=Workbench.Host.Workbench;var m=model.Mark;
            if(attempt!=s.AttemptId){attempt=s.AttemptId;SelectedCriterionId=null;SelectedWorkId=null;PairIdentity=null;signature=null;}
            bool live=m!=null&&model.MarkIsCurrent&&s.Notebook.Any(p=>p.Reference.SourceId==m.Work.SourceId&&p.Reference.Version==m.Work.Version&&p.Reference.Sha256==m.Work.Sha256&&p.Reference.PassageId==m.Work.PassageId);
            if(live){SelectedCriterionId=m.CriterionId;SelectedWorkId=m.Work.SourceId;PairIdentity="Task "+m.TaskVersion+" / Rubric "+m.RubricVersion;}
            CurrentSelection=live;
            var criterion=live?s.Rubric.Criteria.Single(c=>c.Id==m.CriterionId):null;
            var annotation=live?s.CurrentAnnotations.LastOrDefault(a=>a.Content.SpecimenId==SelectedWorkId&&a.Content.CriterionId==SelectedCriterionId):null;
            var next=s.AttemptId+"/"+s.WorldRevision+"/"+SelectedWorkId+"/"+SelectedCriterionId+"/"+live;
            if(next==signature)return;signature=next;
            Identity.text=SelectedCriterionId==null?"SELECT A CRITERION LANE":(live?"SELECTED LANE  ":"EARLIER SELECTION  ")+SelectedCriterionId+"\n"+SelectedWorkId+"\n"+PairIdentity;
            LiteralCriterion.text=criterion==null?"Choose a criterion\nin the work controls.":Wrap(criterion.QuestionId+" | Maximum "+criterion.Marks+"\n"+criterion.Description,38);
            Mark.text=!live?"No current mark":annotation==null?"Unmarked | maximum "+criterion.Marks:"Draft hypothesis: "+annotation.Content.Marks+" / "+criterion.Marks;
            FitLabels();
        }
        public void FitLabels(){Fit(Identity,.44f,.055f);Fit(LiteralCriterion,.44f,.10f);Fit(Mark,.44f,.026f);}
        static void Fit(TextMesh text,float width,float height)
        {
            if(!text||!text.font)return;text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);float longest=0,current=0;int lines=1;
            foreach(char c in text.text){if(c=='\n'){longest=Mathf.Max(longest,current);current=0;lines++;continue;}if(text.font.GetCharacterInfo(c,out var info,text.fontSize,text.fontStyle))current+=info.advance;}
            longest=Mathf.Max(longest,current);float linePixels=(float)text.font.lineHeight*text.fontSize/Mathf.Max(1,text.font.fontSize);
            text.characterSize=Mathf.Min(.006f,width*10/Mathf.Max(1,longest),height*10/Mathf.Max(1,lines*linePixels));
        }
        static string Wrap(string value,int width)
        {
            var b=new StringBuilder();int column=0;
            foreach(var word in value.Replace("\r","").Split(' '))
            {if(column>0&&column+word.Length>width){b.Append('\n');column=0;}if(column>0){b.Append(' ');column++;}b.Append(word);int line=word.LastIndexOf('\n');column=line>=0?word.Length-line-1:column+word.Length;}
            return b.ToString();
        }
    }
}
