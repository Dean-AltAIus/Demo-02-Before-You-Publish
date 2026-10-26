using E=Altaius.Demo02.Domain.EnglishCopy;
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
            if(Status)Status.text=E.Format("publicationcabinetprojection.lateupdate.assign.status.text.task.rubric","Task {s_task_version} / Rubric {s_rubric_version}\n{value5}","s_task_version",s.Task.Version,"s_rubric_version",s.Rubric.Version,"value5",((Released)?E.Get("publicationcabinetprojection.lateupdate.assign.status.text.value5.released.released","Released"):((state==ReviewArtState.Stale)?E.Get("publicationcabinetprojection.lateupdate.assign.status.text.value5.locked.earlier.approval.locked.unknown.locked.earlier.approval.locked.earlier.approval","Locked | earlier approval"):((state==ReviewArtState.Unknown)?E.Get("publicationcabinetprojection.lateupdate.assign.status.text.value5.locked.earlier.approval.locked.unknown.locked.unknown.hold.closed.publication.locked.unknown.locked.unknown","Locked | unknown"):((state==ReviewArtState.Held||state==ReviewArtState.UnverifiedHold)?E.Get("publicationcabinetprojection.lateupdate.assign.status.text.value5.locked.earlier.approval.locked.unknown.locked.unknown.hold.closed.publication.hold.closed.publication.locked.hold.closed.hold.closed","Hold | closed"):E.Get("publicationcabinetprojection.lateupdate.assign.status.text.value5.locked.earlier.approval.locked.unknown.locked.unknown.hold.closed.publication.hold.closed.publication.locked.publication.locked.publication.locked","Publication locked"))))));
            if(UI.Applied?.ReducedMotion==true)OpenAmount=Released?1:0;
            else if(UI.Lifecycle?.Paused!=true)OpenAmount=Mathf.MoveTowards(OpenAmount,Released?1:0,Mathf.Min(Time.unscaledDeltaTime,.05f)/TravelSeconds);
            Hinge.localRotation=Quaternion.Slerp(ClosedAnchor.localRotation,OpenAnchor.localRotation,Mathf.SmoothStep(0,1,OpenAmount));
        }
    }
}
