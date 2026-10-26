using System;
using System.Linq;
using Altaius.Demo02.Domain;
using UnityEngine;
namespace Altaius.Demo02.UI
{
    [DisallowMultipleComponent]
    public sealed class PublicationCabinetProjection:MonoBehaviour
    {
        public PresentationView UI;public Transform Hinge,ClosedAnchor,OpenAnchor;public TextMesh Status;
        [Range(.35f,.6f)]public float TravelSeconds=.45f;
        public bool Released {get;private set;}public float OpenAmount {get;private set;}
        Guid attempt;
        void LateUpdate()
        {
            if(!UI||!UI.Host||UI.Host.Session==null||!Hinge||!ClosedAnchor||!OpenAnchor)return;
            var s=UI.Host.Session.Snapshot;if(attempt!=s.AttemptId){attempt=s.AttemptId;OpenAmount=0;}
            var approval=s.CurrentApproval;var pair=s.Pair;
            Released=s.Publication==PublicationStatus.Released&&approval!=null&&approval.Pair.TaskVersion==pair.TaskVersion&&approval.Pair.RubricVersion==pair.RubricVersion&&approval.Pair.TaskDigest==pair.TaskDigest&&approval.Pair.RubricDigest==pair.RubricDigest&&s.Audit.Any(a=>a.Receipt.Action=="release.request"&&a.Receipt.Status==ReceiptStatus.Accepted);
            var state=ReviewArtProjection.Resolve(s.Publication,s.ReviewStatus,s.CurrentApproval!=null);
            if(Status)Status.text="Task "+s.Task.Version+" / Rubric "+s.Rubric.Version+"\n"+(Released?"Released":state==ReviewArtState.Stale?"Locked | earlier approval":state==ReviewArtState.Unknown?"Locked | unknown":state==ReviewArtState.Held||state==ReviewArtState.UnverifiedHold?"Hold | closed":"Publication locked");
            if(UI.Applied?.ReducedMotion==true)OpenAmount=Released?1:0;
            else if(UI.Lifecycle?.Paused!=true)OpenAmount=Mathf.MoveTowards(OpenAmount,Released?1:0,Mathf.Min(Time.unscaledDeltaTime,.05f)/TravelSeconds);
            Hinge.localRotation=Quaternion.Slerp(ClosedAnchor.localRotation,OpenAnchor.localRotation,Mathf.SmoothStep(0,1,OpenAmount));
        }
    }
}
