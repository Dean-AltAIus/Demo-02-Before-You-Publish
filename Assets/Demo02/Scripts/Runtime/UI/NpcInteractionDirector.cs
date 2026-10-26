using System;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.Integration;
using UnityEngine;
namespace Altaius.Demo02.UI
{
    /// <summary>Presentation of recorded requests and displayed authored dialogue only.
    /// One saved packet, one track owner, no domain mutation or delivery callback.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(25)]
    public sealed class NpcInteractionDirector:MonoBehaviour
    {
        public PresentationView UI;public CampusContactDirector Learner;
        public Animator Colleague,Lead;public NpcMotionLibrary ColleagueLibrary,LeadLibrary;
        public Transform Packet,PacketCover,LeadOrigin,ReviewCamera;public TextMesh PairLabel;
        public Transform ColleagueConversationCamera,StudioSourceCamera;
        [Range(35,75)]public float ReviewFieldOfView=55;
        public string PacketOwner {get;private set;}="Desk";
        public string LeadPhase {get;private set;}="SeatedIdle";
        public string ColleaguePhase {get;private set;}="Idle";
        public Guid PresentedRequest {get;private set;}
        public int CompletedCycles {get;private set;}
        public bool Busy=>step>=0;
        static readonly string[] Cycle={"PickupPacket","ReadPacket","PlaceOpen","PointCurrentPair","RecoverPoint","ReturnPacket","RecoverToReady"};
        Guid attempt,pendingRequest,colleagueDelivery,leadDelivery;int sequence,step=-1;float time,colleagueTime,leadTalkTime,idleTime;
        bool rewind,waitingLearner;ulong sourceGeneration;string sourceId;int pairTask,pairRubric;
        Transform ownedView,priorView,priorInspection;bool enteredInspection;float priorX,priorY,priorFov;bool priorFraming;
        bool Paused=>UI.Lifecycle?.Paused==true;
        bool Reduced=>UI.Applied?.ReducedMotion==true;
        void OnEnable(){if(Learner)Learner.PresentationCompleted+=LearnerFinished;if(UI&&UI.Gameplay){UI.Gameplay.ContactCancelled+=Cancel;UI.Gameplay.ContactRouteRequested+=DirectHandback;}}
        void OnDisable(){if(Learner)Learner.PresentationCompleted-=LearnerFinished;if(UI&&UI.Gameplay){UI.Gameplay.ContactCancelled-=Cancel;UI.Gameplay.ContactRouteRequested-=DirectHandback;}ReleaseView();ResetPose();}
        void LearnerFinished(string binding){if(binding=="PacketPresent"&&waitingLearner){waitingLearner=false;BeginCycle();}}
        void LateUpdate()
        {
            if(!UI||!UI.Host||UI.Host.Session==null||!Colleague||!Lead||!LeadOrigin||!LeadLibrary||!ColleagueLibrary)return;
            var s=UI.Host.Session.Snapshot;
            ApplyVisibleRoom();
            if(UI.PhysicalReviewRoomVisible&&ReviewCamera){var camera=UI.Gameplay.CameraRig;camera.ContactFraming=true;camera.ContactView=ReviewCamera;camera.ContactScreenX=.20f;camera.ContactScreenY=.5f;camera.ContactFieldOfView=ReviewFieldOfView;}
            if(attempt!=s.AttemptId){ResetPose();attempt=s.AttemptId;sequence=s.Audit.Count==0?0:s.Audit.Max(a=>a.Sequence);sourceId=null;colleagueDelivery=leadDelivery=Guid.Empty;sourceGeneration=0;}
            foreach(var audit in s.Audit.Where(a=>a.Sequence>sequence))
            {
                sequence=Math.Max(sequence,audit.Sequence);if(audit.Receipt.Status!=ReceiptStatus.Accepted||audit.Receipt.Action!="review.request")continue;
                var request=s.ReviewHistory.LastOrDefault();if(request==null)continue;
                pendingRequest=request.Id;pairTask=request.Pair.TaskVersion;pairRubric=request.Pair.RubricVersion;
                waitingLearner=UI.Gameplay.ActiveStation&&UI.Gameplay.ActiveStation.Kind==StationKind.Review&&!Reduced;
                if(!waitingLearner&&!Busy)BeginCycle();
            }
            if(waitingLearner&&!Learner.Busy&&Learner.LastProblem!=null){waitingLearner=false;BeginCycle();}
            // Literal current identity stays native text. Earlier request identity remains explicit.
            if(PairLabel){PairLabel.text="Task "+s.Task.Version+" / Rubric "+s.Rubric.Version+(PresentedRequest!=Guid.Empty&&(pairTask!=s.Task.Version||pairRubric!=s.Rubric.Version)?"\nEarlier request "+pairTask+" / "+pairRubric:"");}
            if(Paused)return;float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);idleTime+=dt;
            ObserveColleague(dt);ObserveLeadConversation(dt);
            if(Busy)
            {
                if(Reduced){ResetPose(false);return;}
                var take=LeadLibrary.Find(Cycle[step]);time+=rewind?-dt:dt;
                SampleLead(take,Mathf.Clamp(time,0,take.Clip.length));
                if(rewind&&time<=0){if(--step<0){ResetPose(false);return;}time=LeadLibrary.Find(Cycle[step]).Clip.length;}
                else if(!rewind&&time>=take.Clip.length)
                {if(++step>=Cycle.Length){CompletedCycles++;ResetPose(false);return;}time=0;SampleLead(LeadLibrary.Find(Cycle[step]),0);}
            }
            else if(!waitingLearner)
            {
                var talk=UI.SurfaceId=="conversation"&&UI.ConversationRole==ActorRole.ModuleLead;
                string id=leadTalkTime>0?"SeatedSpeak":talk?"SeatedListen":"SeatedIdle";LeadPhase=id;var take=LeadLibrary.Find(id);SampleActor(Lead,take,Reduced?0:leadTalkTime>0?take.Clip.length-leadTalkTime:idleTime);ApplyPacket(LeadLibrary.Find("SeatedIdle").Sample(0));
            }
        }
        void ApplyVisibleRoom()
        {
            var work=UI.Workbench;var camera=UI.Gameplay.CameraRig;Transform desired=null;
            if(UI.PhysicalConversationVisible)desired=UI.ConversationRole==ActorRole.Colleague?ColleagueConversationCamera:ReviewCamera;
            else if(!UI.Overlay.activeSelf&&work.IsOpen&&work.TextScale<1.5f&&work.ViewId=="passage"&&UI.Gameplay.ActiveStation&&UI.Gameplay.ActiveStation.Kind==StationKind.Studio)desired=StudioSourceCamera;
            if(desired!=ownedView){ReleaseView();if(desired){ownedView=desired;priorView=camera.ContactView;priorInspection=camera.InspectionTarget;priorFraming=camera.ContactFraming;priorX=camera.ContactScreenX;priorY=camera.ContactScreenY;priorFov=camera.ContactFieldOfView;enteredInspection=!camera.Inspecting;if(enteredInspection)camera.EnterInspection(desired);}}
            if(desired){if(camera.ContactView&&camera.ContactView!=desired)priorView=camera.ContactView;camera.ContactView=desired;camera.ContactFraming=true;camera.ContactScreenX=.20f;camera.ContactScreenY=.5f;camera.ContactFieldOfView=55;}
        }
        void ReleaseView()
        {
            if(!ownedView||!UI||!UI.Gameplay)return;var camera=UI.Gameplay.CameraRig;
            if(enteredInspection&&camera.InspectionTarget==ownedView)camera.ExitInspection();
            else if(camera.ContactView==ownedView){camera.ContactView=priorView;camera.ContactFraming=priorFraming;camera.ContactScreenX=priorX;camera.ContactScreenY=priorY;camera.ContactFieldOfView=priorFov;}
            ownedView=null;priorView=null;priorInspection=null;enteredInspection=false;
        }
        void BeginCycle(){if(Busy||pendingRequest==Guid.Empty)return;PresentedRequest=pendingRequest;pendingRequest=Guid.Empty;step=0;time=0;rewind=false;SampleLead(LeadLibrary.Find(Cycle[0]),0);}
        void ObserveColleague(float dt)
        {
            var work=UI.Workbench;
            if(work.IsOpen&&work.BindingGeneration!=sourceGeneration)
            {sourceGeneration=work.BindingGeneration;if(work.ViewId=="passage"&&work.InspectedSourceId!=sourceId&&UI.Gameplay.ActiveStation&&UI.Gameplay.ActiveStation.Kind==StationKind.Studio){sourceId=work.InspectedSourceId;ColleaguePhase="SourcePoint";colleagueTime=0;}}
            var record=UI.Lifecycle?.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ActorRole.Colleague&&r.DisplayId!=null&&r.Reply?.Available==true);
            if(record!=null&&record.DisplayId.Value!=colleagueDelivery){colleagueDelivery=record.DisplayId.Value;ColleaguePhase="Speak";colleagueTime=0;}
            if(ColleaguePhase=="SourcePoint"||ColleaguePhase=="Speak")
            {var take=ColleagueLibrary.Find(ColleaguePhase);colleagueTime+=dt;if(!Reduced&&colleagueTime<take.Clip.length){SampleActor(Colleague,take,colleagueTime);return;}}
            ColleaguePhase=UI.SurfaceId=="conversation"&&UI.ConversationRole==ActorRole.Colleague?"Listen":"Idle";SampleActor(Colleague,ColleagueLibrary.Find(ColleaguePhase),Reduced?0:idleTime);
        }
        void ObserveLeadConversation(float dt)
        {
            var record=UI.Lifecycle?.Conversation.History.LastOrDefault(r=>r.Request.Recipient==ActorRole.ModuleLead&&r.DisplayId!=null&&r.Reply?.Available==true);
            if(record!=null&&record.DisplayId.Value!=leadDelivery){leadDelivery=record.DisplayId.Value;leadTalkTime=LeadLibrary.Find("SeatedSpeak").Clip.length;}
            leadTalkTime=Mathf.Max(0,leadTalkTime-dt);if(Reduced)leadTalkTime=0;
        }
        void SampleLead(NpcTake take,float t)
        {LeadPhase=take.Id;SampleActor(Lead,take,t);ApplyPacket(take.Sample(t));PacketOwner=take.Id=="PickupPacket"?(t<.6f?"Desk":"Module lead"):take.Id=="PlaceOpen"?(t>=take.Clip.length?"Desk":"Module lead"):take.Id=="ReadPacket"||take.Id=="ReturnPacket"&&t<take.Clip.length?"Module lead":"Desk";}
        static void SampleActor(Animator actor,NpcTake take,float t)
        {if(!actor||take?.Clip==null)return;actor.speed=0;actor.applyRootMotion=false;float seconds=take.Clip.isLooping?Mathf.Repeat(t,take.Clip.length):Mathf.Min(t,take.Clip.length);actor.Play(take.Id,0,seconds/take.Clip.length);actor.Update(0);}
        void ApplyPacket(PacketPoseKey key)
        {if(key==null||!Packet||!LeadOrigin)return;Packet.SetPositionAndRotation(LeadOrigin.TransformPoint(ContactMotionLibrary.NativePoint(key.Position)),LeadOrigin.rotation*ContactMotionLibrary.NativeRotation(key.Rotation)*Quaternion.Euler(-90,0,0));if(PacketCover)PacketCover.localRotation=NpcMotionLibrary.ImportedLocalRotation(key.Cover);}
        public void Cancel(){pendingRequest=Guid.Empty;waitingLearner=false;if(Busy)rewind=true;}
        void DirectHandback(){ReleaseView();pendingRequest=Guid.Empty;waitingLearner=false;ResetPose(false);}
        void ResetPose(bool clearIdentity=true)
        {step=-1;time=0;rewind=false;waitingLearner=false;pendingRequest=Guid.Empty;PacketOwner="Desk";LeadPhase="SeatedIdle";if(clearIdentity){PresentedRequest=Guid.Empty;pairTask=pairRubric=0;}if(LeadLibrary){SampleActor(Lead,LeadLibrary.Find("SeatedIdle"),0);ApplyPacket(LeadLibrary.Find("SeatedIdle")?.Sample(0));}}
    }
}
