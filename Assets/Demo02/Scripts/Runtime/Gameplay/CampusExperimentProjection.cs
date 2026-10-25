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
        readonly List<WorldMarkingLane> additional = new List<WorldMarkingLane>();
        int revision = -1;
        string selected;
        void LateUpdate()
        {
            if(!Host || Host.Session == null)return;
            var s=Host.Session.Snapshot;
            if(revision==s.WorldRevision && selected==Gameplay.SelectedSpecimenId)return;
            revision=s.WorldRevision;selected=Gameplay.SelectedSpecimenId;
            WorkLabel.text=selected==null?"SELECT WORK AT TABLE":selected+" | inspected";
            TotalLabel.text=selected==null?"No marks assigned": "Draft "+s.DraftTotal(selected)+" / 20 | unverified";
            PairLabel.text="Task "+s.Task.Version+" / Rubric "+s.Rubric.Version;
            var lanes=AuthoredLanes.Concat(additional).ToList();
            while(lanes.Count<s.Rubric.Criteria.Count)
            {
                var lane=Instantiate(AdditionalLanePrefab,AdditionalLaneRoot);lane.transform.localPosition=new Vector3(0,0,additional.Count*.16f);
                additional.Add(lane);lanes.Add(lane);
            }
            for(int i=0;i<lanes.Count;i++)
            {
                lanes[i].gameObject.SetActive(i<s.Rubric.Criteria.Count);if(i>=s.Rubric.Criteria.Count)continue;
                var criterion=s.Rubric.Criteria[i];var mark=s.CurrentAnnotations.SingleOrDefault(a=>a.Content.SpecimenId==selected && a.Content.CriterionId==criterion.Id);
                lanes[i].CriterionLabel.text=criterion.QuestionId+" | "+criterion.Id;
                lanes[i].MarkLabel.text=(mark==null?"Unmarked":mark.Content.Marks.ToString())+" / "+criterion.Marks;
                lanes[i].PassageProxy.SetActive(mark!=null);
            }
        }
    }
}
