using Altaius.Demo02.Domain;
using UnityEngine;

namespace Altaius.Demo02.Gameplay
{
    public enum StationKind { Studio, Experiment, Archive, Review }
    public sealed class CampusStation : MonoBehaviour
    {
        public string StationId;
        public string DisplayName;
        public StationKind Kind;
        public InspectionLocation Location;
        public Transform InteractionPoint;
        public Transform Arrival;
        public Transform InspectionCamera;
        [Min(0.1f)] public float InteractionRange = 2f;
        public Transform NpcVisualSlot;
        public Transform EditableFolioHinge;
        public Transform EditableCabinetHinge;
        public float DistanceTo(Vector3 position)
        { var delta = (InteractionPoint ? InteractionPoint.position : transform.position) - position; delta.y = 0; return delta.magnitude; }
    }
}
