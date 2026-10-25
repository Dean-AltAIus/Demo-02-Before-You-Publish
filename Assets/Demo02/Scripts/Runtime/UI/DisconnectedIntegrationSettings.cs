using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    [CreateAssetMenu(menuName="Altaius/Demo02/Disabled integration preparation")]
    public sealed class DisconnectedIntegrationSettings : ScriptableObject
    {
        [Tooltip("No network implementation ships. This slot is deliberately disabled.")]public bool ExternalTransportEnabled;
        public string BootstrapEndpoint="";
        public string Environment="",SimulationId="",LevelId="",ConfigurationSnapshotId="",SceneDefinitionId="";
        public string ColleagueActorId="",ModuleLeadActorId="",ExperimentReviewerActorId="",LeenaAssistantId="",FrameworkId="";
        public bool SieWritesEnabled;
        public string Status=>"Disconnected. Endpoint and real binding identities are unassigned. Local authored replies only.";
        void OnValidate(){ExternalTransportEnabled=false;SieWritesEnabled=false;}
    }
}
