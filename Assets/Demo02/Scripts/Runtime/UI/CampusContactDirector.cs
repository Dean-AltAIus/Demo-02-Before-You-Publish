using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using UnityEngine;

namespace Altaius.Demo02.UI
{
    /// <summary>Read-only presentation of actual native UI selections and accepted receipts.
    /// Prop contact completion cannot issue a domain command or admit a passage.</summary>
    [DefaultExecutionOrder(20),DisallowMultipleComponent]
    public sealed class CampusContactDirector:MonoBehaviour
    {
        public CampusGameplayFacade Gameplay;public WorkbenchView Workbench;public PresentationView UI;
        public Animator Learner;public ContactMotionLibrary Library;public ContactBinding[] Bindings;
        public AnimationClip StopClip,TurnClip;public string StopState="D02_Learner_Stop_v013",TurnState="D02_Learner_Turn_v013";
        public string IdleState="Idle";public float ArrivalTolerance=.008f,ApproachTimeout=6f;
        public string VisiblePhase {get;private set;}="Idle";
        public string LastProblem {get;private set;}
        public string ActiveBinding=>active?.Binding.Id;
        public bool Busy=>active!=null;
        public int QueuedCount=>queue.Count;
        public int Completed {get;private set;}
        public int Cancelled {get;private set;}
        public event Action<string> PresentationCompleted;
        public event Action<Guid,bool> PacketPresentationSettled;
        sealed class Job {public ContactBinding Binding;public ContactMotion Motion;public bool Reverse;public string WorkId,CriterionId;public int TaskVersion,RubricVersion;public Guid RequestId;}
        struct Pose {public Vector3 Position;public Quaternion Rotation;}
        readonly Queue<Job> queue=new Queue<Job>();
        readonly Dictionary<Transform,Pose> initial=new Dictionary<Transform,Pose>();
        readonly Dictionary<string,bool> endStates=new Dictionary<string,bool>();
        readonly HashSet<Guid> projectedReceipts=new HashSet<Guid>();
        Job active;Pose actionStart;float elapsed,travelElapsed,stalled,lastDistance;
        bool rewind;Guid attempt;int auditSequence,lastTaskVersion,lastRubricVersion;ulong generation;string lastView,lastWork,lastCriterion,lastFolio;
        bool Paused=>UI&&UI.Lifecycle?.Paused==true;
        bool Reduced=>UI&&UI.Applied?.ReducedMotion==true;
        void Awake()
        {
            foreach(var b in Bindings??Array.Empty<ContactBinding>())if(b.Prop&&!initial.ContainsKey(b.Prop))initial.Add(b.Prop,new Pose{Position=b.Prop.position,Rotation=b.Prop.rotation});
            if(Learner)Learner.applyRootMotion=false;
        }
        void OnEnable(){if(Gameplay){Gameplay.ContactCancelled+=Cancel;Gameplay.ContactRouteRequested+=DirectRouteHandback;}}
        void OnDisable(){if(Gameplay){Gameplay.ContactCancelled-=Cancel;Gameplay.ContactRouteRequested-=DirectRouteHandback;}ImmediateHandback(false);}
        void LateUpdate()
        {
            if(!Gameplay||!Workbench||!Learner||!Library||!Gameplay.SessionHost||Gameplay.SessionHost.Session==null)return;
            Observe();
            Gameplay.CameraRig.ContactFraming=Workbench.IsOpen&&Gameplay.CameraRig.ContactView;
            bool marking=Workbench.IsOpen&&Workbench.ViewId=="mark"&&Workbench.TextScale<=1;
            Gameplay.CameraRig.ContactScreenX=marking?.20f:.15f;Gameplay.CameraRig.ContactScreenY=marking?.30f:.5f;Gameplay.CameraRig.ContactFieldOfView=marking?55:72;
            if(Paused){if(active!=null)Learner.speed=0;return;}
            if(active==null){if(queue.Count>0)StartNext();return;}
            if(Reduced){ApplyMain(active.Reverse?0:active.Motion.Duration);Complete();return;}
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(VisiblePhase=="Travel")
            {
                var direction=active.Binding.Stance.position-Gameplay.Motor.transform.position;direction.y=0;
                var facing=direction.sqrMagnitude>.0001f?Quaternion.LookRotation(direction):Gameplay.Motor.CharacterVisual.rotation;
                Gameplay.Motor.MoveToContact(active.Binding.Stance.position,facing,dt);
                var delta=Gameplay.Motor.transform.position-active.Binding.Stance.position;delta.y=0;float d=delta.magnitude;
                travelElapsed+=dt;stalled=d>=lastDistance-.0002f?stalled+dt:0;lastDistance=d;
                if(d<=ArrivalTolerance)
                {VisiblePhase=StopClip?"Stop":"Turn";elapsed=0;}
                else if(travelElapsed>ApproachTimeout||stalled>1.2f){LastProblem="Contact stance is obstructed: "+active.Binding.Id;Cancelled++;Release();}
                return;
            }
            if(VisiblePhase=="Stop")
            {
                Gameplay.Motor.MoveToContact(Gameplay.Motor.transform.position,Gameplay.Motor.CharacterVisual.rotation,dt);
                elapsed+=dt;Sample(StopState,StopClip,Mathf.Min(elapsed,StopClip.length));
                if(elapsed>=StopClip.length){VisiblePhase="Turn";elapsed=0;}return;
            }
            if(VisiblePhase=="Turn")
            {
                Gameplay.Motor.MoveToContact(Gameplay.Motor.transform.position,active.Binding.Stance.rotation,dt);elapsed+=dt;
                if(TurnClip)Sample(TurnState,TurnClip,Mathf.Min(elapsed,TurnClip.length));
                if((!TurnClip||elapsed>=TurnClip.length)&&Quaternion.Angle(Gameplay.Motor.CharacterVisual.rotation,active.Binding.Stance.rotation)<1){VisiblePhase="Approach";elapsed=0;SampleApproach(0);}return;
            }
            // Maintain grounded collision ownership without world displacement while the hands work.
            Gameplay.Motor.MoveToContact(Gameplay.Motor.transform.position,active.Binding.Stance.rotation,dt);
            if(VisiblePhase=="Approach")
            {
                elapsed+=rewind?-dt:dt;float duration=EntryClip.length;SampleApproach(Mathf.Clamp(elapsed,0,duration));
                if(rewind&&elapsed<=0){Cancelled++;RestoreActionStart();Release();}
                else if(!rewind&&elapsed>=duration){VisiblePhase="Contact";elapsed=0;SampleContact(0);}
            }
            else if(VisiblePhase=="Contact")
            {
                elapsed+=rewind?-dt:dt;SampleContact(Mathf.Clamp(elapsed,0,active.Motion.Duration));
                if(rewind&&elapsed<=0){VisiblePhase="Approach";elapsed=EntryClip.length;}
                else if(!rewind&&elapsed>=active.Motion.Duration){VisiblePhase="Recovery";elapsed=0;SampleRecovery(0);}
            }
            else if(VisiblePhase=="Recovery")
            {
                elapsed+=dt;SampleRecovery(Mathf.Min(elapsed,ExitClip.length));
                if(elapsed>=ExitClip.length)Complete();
            }
        }
        void Observe()
        {
            var s=Gameplay.SessionHost.Session.Snapshot;
            if(attempt!=s.AttemptId)
            {
                ImmediateHandback(true);attempt=s.AttemptId;auditSequence=s.Audit.Count==0?0:s.Audit.Max(x=>x.Sequence);projectedReceipts.Clear();
                generation=0;lastView=null;lastWork=null;lastCriterion=null;lastFolio=null;lastTaskVersion=lastRubricVersion=0;
            }
            if(Workbench.IsOpen&&generation!=Workbench.BindingGeneration)
            {
                generation=Workbench.BindingGeneration;var view=Workbench.ViewId;
                if(view=="passage")
                {
                    string source=Workbench.InspectedSourceId??"";
                    string folio=source.StartsWith("D02.task.")?"Task":source.StartsWith("D02.rubric.")?"Rubric":null;
                    if(lastFolio!=null&&lastFolio!=folio)CloseFolio(lastFolio);
                    if(folio!=null)Ensure(folio+"Open",true);lastFolio=folio;
                }
                if(view=="sources"&&lastFolio!=null)CloseFolio(lastFolio);
                if(view=="work"&&!IsAtEnd("PlacePassage"))Ensure("WorkOpen",true);
                if(view=="mark"&&Gameplay.SessionHost.Workbench.MarkIsCurrent)
                {
                    var m=Gameplay.SessionHost.Workbench.Mark;
                    if(!SelectionMatches(m.Work.SourceId,m.CriterionId,m.TaskVersion,m.RubricVersion))
                    {
                        Ensure("WorkOpen",true);
                        if(IsAtEnd("EditionSlide"))Request("EditionSlide",true);
                        if(IsAtEnd("PlacePassage"))Request("PlacePassage",true);
                        Request("PlacePassage");
                    }
                }
                if((view=="task"||view=="rubric")&&view!=lastView)
                {
                    string folio=view=="task"?"Task":"Rubric";if(lastFolio!=null&&lastFolio!=folio)CloseFolio(lastFolio);
                    Ensure(folio+"Open",true);Ensure(folio+"Page",true);lastFolio=folio;
                }
                lastView=view;
            }
            foreach(var item in s.Audit.Where(x=>x.Sequence>auditSequence))
            {
                var r=item.Receipt;
                if(r.Status==ReceiptStatus.Accepted&&!projectedReceipts.Contains(r.RequestId))
                {
                    if(r.Action=="experiment.annotate"&&Workbench.IsOpen&&Workbench.ViewId=="mark"&&Gameplay.SessionHost.Workbench.MarkIsCurrent)
                    {
                        // Leave an unseen receipt behind the cursor until capacity exists. Retried arrivals share its identity.
                        if(queue.Count>=8)break;
                        Ensure("WorkOpen",false);if(queue.Count>=8)break;Request("MarkPoint");
                    }
                    if((r.Action=="task.edit"||r.Action=="rubric.edit")&&IsAtEnd("PlacePassage"))
                    {Ensure("WorkOpen",true);Ensure("EditionSlide",true);}
                    projectedReceipts.Add(r.RequestId);
                }
                auditSequence=Math.Max(auditSequence,item.Sequence);
                // Review packet preparation is scheduled by NpcInteractionDirector with its exact request identity.
            }
        }
        bool IsAtEnd(string id)
        {
            bool end=endStates.TryGetValue(id,out var value)&&value;
            var jobs=(active!=null&&!rewind?new[]{active}:Array.Empty<Job>()).Concat(queue);
            foreach(var job in jobs)
            {if(job.Binding.Id==id)end=!job.Reverse;else if(id.EndsWith("Open")&&job.Binding.Id==id.Replace("Open","Close"))end=job.Reverse;}
            return end;
        }
        bool SelectionMatches(string work,string criterion,int taskVersion,int rubricVersion)
        {
            string selectedWork=lastWork,selectedCriterion=lastCriterion;int selectedTask=lastTaskVersion,selectedRubric=lastRubricVersion;
            foreach(var job in (active!=null&&!rewind?new[]{active}:Array.Empty<Job>()).Concat(queue))
                if(job.Binding.Id=="PlacePassage")
                {selectedWork=job.Reverse?null:job.WorkId;selectedCriterion=job.Reverse?null:job.CriterionId;selectedTask=job.TaskVersion;selectedRubric=job.RubricVersion;}
            return selectedWork==work&&selectedCriterion==criterion&&selectedTask==taskVersion&&selectedRubric==rubricVersion&&!IsAtEnd("EditionSlide");
        }
        void CloseFolio(string stem){if(IsAtEnd(stem+"Page"))Request(stem+"Page",true);if(IsAtEnd(stem+"Open"))Request(stem+"Close");}
        void Ensure(string id,bool end){if(IsAtEnd(id)==end||queue.Any(j=>j.Binding.Id==id&&j.Reverse!=end)||active!=null&&active.Binding.Id==id&&active.Reverse!=end)return;Request(id,!end);}
        public bool Request(string id,bool reverse=false,Guid requestId=default(Guid))
        {
            var binding=Bindings?.SingleOrDefault(b=>b.Id==id);var motion=binding==null?null:Library.Find(binding.MotionId);
            if(binding==null||motion==null||!binding.Stance||!motion.Approach||!motion.Contact||!motion.Recovery){LastProblem="Missing saved contact binding: "+id;return false;}
            if(queue.Count>=8){LastProblem="Contact sequence queue is full.";return false;}
            var mark=Gameplay.SessionHost.Workbench.Mark;
            queue.Enqueue(new Job{Binding=binding,Motion=motion,Reverse=reverse,RequestId=requestId,WorkId=id=="PlacePassage"&&mark!=null?mark.Work.SourceId:null,CriterionId=id=="PlacePassage"&&mark!=null?mark.CriterionId:null,TaskVersion=mark?.TaskVersion??0,RubricVersion=mark?.RubricVersion??0});return true;
        }
        void StartNext()
        {
            active=queue.Dequeue();rewind=false;elapsed=travelElapsed=stalled=0;lastDistance=float.PositiveInfinity;LastProblem=null;
            actionStart=active.Binding.Prop?new Pose{Position=active.Binding.Prop.position,Rotation=active.Binding.Prop.rotation}:new Pose();
            // A single sheet/cover must already occupy this track's entry. Never teleport a held prop into contact.
            var key=active.Motion.Sample(active.Reverse?active.Motion.Duration:0);
            if(key!=null&&active.Binding.Prop)
            {
                var expected=active.Binding.Stance.TransformPoint(ContactMotionLibrary.NativePoint(key.Position));
                if(Vector3.Distance(expected,active.Binding.Prop.position)>.004f)
                {LastProblem="Prop entry does not match saved contact: "+active.Binding.Id;Cancelled++;Release();return;}
            }
            if(Reduced){ApplyMain(active.Reverse?0:active.Motion.Duration);Complete();return;}
            if(!Gameplay.ActiveStation||Gameplay.ActiveStation.DistanceTo(active.Binding.Stance.position)>Gameplay.ActiveStation.InteractionRange)
            {LastProblem="Contact stance is outside the active station range: "+active.Binding.Id;Cancelled++;Release();return;}
            Gameplay.SetContactInput(true);Learner.speed=1;Learner.Play(IdleState,0,0);VisiblePhase="Travel";
            if(active.Binding.CameraPose)Gameplay.CameraRig.ContactView=active.Binding.CameraPose;
            if(active.Binding.Id.EndsWith("Page")&&active.Binding.Prop)active.Binding.Prop.gameObject.SetActive(true);
        }
        AnimationClip EntryClip=>active.Reverse?active.Motion.Recovery:active.Motion.Approach;
        AnimationClip ExitClip=>active.Reverse?active.Motion.Approach:active.Motion.Recovery;
        void SampleApproach(float t)=>Sample(active.Reverse?active.Motion.RecoveryState:active.Motion.ApproachState,EntryClip,active.Reverse?EntryClip.length-t:t);
        void SampleRecovery(float t)=>Sample(active.Reverse?active.Motion.ApproachState:active.Motion.RecoveryState,ExitClip,active.Reverse?ExitClip.length-t:t);
        void SampleContact(float t){float source=active.Reverse?active.Motion.Duration-t:t;Sample(active.Motion.ContactState,active.Motion.Contact,source);ApplyMain(source);}
        void Sample(string state,AnimationClip clip,float t){Learner.speed=0;Learner.applyRootMotion=false;Learner.Play(state,0,clip.length>0?Mathf.Clamp01(t/clip.length):0);Learner.Update(0);}
        void ApplyMain(float t)
        {
            var key=active.Motion.Sample(t);var b=active.Binding;if(key==null||!b.Prop)return;
            b.Prop.position=b.Stance.TransformPoint(ContactMotionLibrary.NativePoint(key.Position));
            b.Prop.rotation=b.Stance.rotation*ContactMotionLibrary.NativeRotation(key.Rotation)*(b.ImportedNativeGeometry?Quaternion.Euler(-90,0,0):Quaternion.identity);
        }
        void Complete()
        {
            var id=active.Binding.Id;endStates[id]=!active.Reverse;
            if(id=="PlacePassage"){lastWork=active.Reverse?null:active.WorkId;lastCriterion=active.Reverse?null:active.CriterionId;lastTaskVersion=active.TaskVersion;lastRubricVersion=active.RubricVersion;}
            if(id.EndsWith("Close")){var stem=id.Substring(0,id.Length-5);endStates[stem+"Open"]=active.Reverse;SetLeafVisible(stem,active.Reverse);}
            if(id.EndsWith("Open")){var stem=id.Substring(0,id.Length-4);endStates[stem+"Close"]=active.Reverse;SetLeafVisible(stem,!active.Reverse);}
            Completed++;Release(true);PresentationCompleted?.Invoke(id);
        }
        void SetLeafVisible(string stem,bool visible)
        {
            var leaf=Bindings?.FirstOrDefault(b=>b.Id==(stem=="Work"?"PlacePassage":stem+"Page"));
            // Closing the empty folio does not take ownership of the sheet already placed on its criterion lane.
            if(leaf?.Prop)leaf.Prop.gameObject.SetActive(visible||stem=="Work"&&IsAtEnd("PlacePassage"));
        }
        public void Cancel()
        {
            ClearQueued();if(active==null)return;
            if(VisiblePhase=="Travel"||VisiblePhase=="Stop"||VisiblePhase=="Turn"){Cancelled++;Release();return;}
            if(VisiblePhase=="Recovery")return;rewind=true;
        }
        void RestoreActionStart(){if(active?.Binding.Prop){active.Binding.Prop.position=actionStart.Position;active.Binding.Prop.rotation=actionStart.Rotation;}}
        void ClearQueued()
        {
            while(queue.Count>0){var job=queue.Dequeue();if(job.RequestId!=Guid.Empty)PacketPresentationSettled?.Invoke(job.RequestId,false);}
            var snapshot=Gameplay?.SessionHost?.Session?.Snapshot;
            if(snapshot!=null&&snapshot.AttemptId==attempt)
                foreach(var audit in snapshot.Audit.Where(a=>a.Sequence>auditSequence))
                {if(audit.Receipt.Status==ReceiptStatus.Accepted)projectedReceipts.Add(audit.Receipt.RequestId);auditSequence=audit.Sequence;}
        }
        void Release(bool completed=false)
        {
            var requestId=active?.RequestId??Guid.Empty;active=null;rewind=false;VisiblePhase="Idle";Gameplay?.SetContactInput(false);
            if(Learner){Learner.speed=1;if(Learner.isActiveAndEnabled){Learner.Play(IdleState,0,0);Learner.Update(0);}}
            if(requestId!=Guid.Empty)PacketPresentationSettled?.Invoke(requestId,completed);
        }
        void DirectRouteHandback(){RestoreActionStart();ImmediateHandback(false);}
        void ImmediateHandback(bool reset)
        {
            ClearQueued();if(active!=null)RestoreActionStart();Release();
            if(reset){foreach(var pair in initial)if(pair.Key){pair.Key.position=pair.Value.Position;pair.Key.rotation=pair.Value.Rotation;}endStates.Clear();SetLeafVisible("Task",false);SetLeafVisible("Rubric",false);SetLeafVisible("Work",false);}
        }
    }
}
