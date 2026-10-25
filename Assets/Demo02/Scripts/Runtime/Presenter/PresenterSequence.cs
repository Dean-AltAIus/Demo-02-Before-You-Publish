using System;
using Altaius.Demo02.Domain;
using UnityEngine;
namespace Altaius.Demo02.Presenter
{
    public enum PresenterStepKind { Dwell,WorkbenchControl,PresentationControl,TypePresentation,OpenConversation,TypeWorkbench,MoveStation,CloseWorkspace,ObserveBaseline,Finding,ReviewDecision,Await,Debrief,ReadWorkbench,ReadPresentation }
    [Serializable] public sealed class PresenterStep
    {
        public string Id,Label,Control,Text,Station;
        public PresenterStepKind Kind;
        [Min(0)] public float Dwell;
        [Min(1)] public float CharactersPerSecond=38;
        [Min(0)] public float PhrasePause=.16f;
        public int TaskVersion,RubricVersion;
        public ActorRole Role=ActorRole.Colleague;
        [TextArea] public string BuyerCommentary;
    }
    [Serializable] public sealed class DemonstrationMark
    {
        public string Work,WorkSha256,Criterion;
        public int Marks;
        [TextArea] public string Rationale,Finding;
        public FindingDecision Decision=FindingDecision.Supported;
    }
    /// <summary>Editable source for one explicitly labelled demonstration. It is frozen on each run.</summary>
    [CreateAssetMenu(menuName="Altaius/Demo02/Presenter sequence")]
    public sealed class PresenterSequence:ScriptableObject
    {
        public string ScenarioId="D02.authored.compare-and-justify.v1";
        [TextArea] public string Disclosure="Synthetic authored demonstration. Offline. No learner evidence.";
        [Min(.1f)] public float Pace=1;
        public bool BuyerCommentary=true;
        public string ExpectedTaskDigest,ExpectedRubricDigest;
        [TextArea] public string ReviewReason;
        public DemonstrationMark[] Marks=Array.Empty<DemonstrationMark>();
        public PresenterStep[] Steps=Array.Empty<PresenterStep>();
        public string Digest=>ContentDigest.Text(JsonUtility.ToJson(this));
    }
}
