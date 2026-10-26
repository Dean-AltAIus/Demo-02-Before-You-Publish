using E=Altaius.Demo02.Domain.EnglishCopy;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Altaius.Demo02.Gameplay
{
    /// <summary>Receipt-driven physical table labels. This projection never issues commands or grants authority.</summary>
    public sealed class CampusExperimentProjection : MonoBehaviour
    {
        public CampusSession Host;
        public CampusGameplayFacade Gameplay;
        public TextMesh WorkLabel, TotalLabel, PairLabel;
        public List<WorldMarkingLane> AuthoredLanes = new List<WorldMarkingLane>();
        public WorldMarkingLane AdditionalLanePrefab;
        public Transform AdditionalLaneRoot;
        public Transform ExpandedLaneRoot;
        public float ExpandedColumnSpacing=.60f,ExpandedRowSpacing=.32f,ExpandedDepth=.68f;
        readonly List<WorldMarkingLane> additional = new List<WorldMarkingLane>();
        Vector3[] savedPositions,savedScales;Quaternion[] savedRotations;
        int revision = -1;
        System.Guid attempt;
        string selected;
        void Awake(){savedPositions=AuthoredLanes.Select(l=>l.transform.position).ToArray();savedRotations=AuthoredLanes.Select(l=>l.transform.rotation).ToArray();savedScales=AuthoredLanes.Select(l=>l.transform.localScale).ToArray();}
        void LateUpdate()
        {
            if(!Host || Host.Session == null)return;
            var s=Host.Session.Snapshot;
            if(attempt==s.AttemptId&&revision==s.WorldRevision && selected==Gameplay.SelectedSpecimenId)return;
            attempt=s.AttemptId;revision=s.WorldRevision;selected=Gameplay.SelectedSpecimenId;
            WorkLabel.text=((selected==null)?E.Get("campusexperimentprojection.lateupdate.assign.worklabel.text.select.work.at.table.select.work.at.table","SELECT WORK AT TABLE"):E.Format("campusexperimentprojection.lateupdate.assign.worklabel.text.inspected.inspected","{selected} | inspected","selected",selected));
            TotalLabel.text=((selected==null)?E.Get("campusexperimentprojection.lateupdate.assign.totallabel.text.no.marks.assigned.no.marks.assigned","No marks assigned"):E.Format("campusexperimentprojection.lateupdate.assign.totallabel.text.draft.hypothesis.20.unverified.draft.hypothesis.20.unverified","Draft hypothesis\n{s_drafttotal_selected} / 20 | unverified","s_drafttotal_selected",s.DraftTotal(selected)));
            PairLabel.text=E.Format("campusexperimentprojection.lateupdate.assign.pairlabel.text.task.rubric","Task {s_task_version} / Rubric {s_rubric_version}","s_task_version",s.Task.Version,"s_rubric_version",s.Rubric.Version);
            var lanes=AuthoredLanes.Concat(additional).ToList();
            while(lanes.Count<s.Rubric.Criteria.Count)
            {
                var lane=Instantiate(AdditionalLanePrefab,AdditionalLaneRoot);lane.transform.localPosition=new Vector3(0,0,additional.Count*.16f);
                additional.Add(lane);lanes.Add(lane);
            }
            for(int i=0;i<lanes.Count;i++)
            {
                lanes[i].gameObject.SetActive(i<s.Rubric.Criteria.Count);if(i>=s.Rubric.Criteria.Count)continue;
                if(s.Rubric.Criteria.Count>AuthoredLanes.Count&&ExpandedLaneRoot)
                {
                    int rows=Mathf.CeilToInt(s.Rubric.Criteria.Count/2f);float scale=Mathf.Min(1,ExpandedDepth/(rows*ExpandedRowSpacing));
                    var local=new Vector3((i%2-.5f)*ExpandedColumnSpacing*scale,0,((rows-1)*.5f-i/2)*ExpandedRowSpacing*scale);
                    lanes[i].transform.SetPositionAndRotation(ExpandedLaneRoot.TransformPoint(local),ExpandedLaneRoot.rotation);lanes[i].transform.localScale=Vector3.one*scale;
                }
                else if(i<AuthoredLanes.Count&&savedPositions!=null){lanes[i].transform.SetPositionAndRotation(savedPositions[i],savedRotations[i]);lanes[i].transform.localScale=savedScales[i];}
                var criterion=s.Rubric.Criteria[i];var mark=s.CurrentAnnotations.SingleOrDefault(a=>a.Content.SpecimenId==selected && a.Content.CriterionId==criterion.Id);
                lanes[i].CriterionLabel.text=criterion.QuestionId+" | "+criterion.Id;
                lanes[i].MarkLabel.text=((((mark==null)?E.Get("campusexperimentprojection.lateupdate.assign.lanes.i.marklabel.text.value0.unmarked.unmarked","Unmarked"):mark.Content.Marks.ToString()))+(" / ")+(criterion.Marks));
                lanes[i].FitLabels();
                lanes[i].PassageProxy.SetActive(mark!=null);
            }
        }
    }
}
