using UnityEngine;

namespace Altaius.Demo02.Gameplay
{
    public sealed class CampusCamera : MonoBehaviour
    {
        public Transform FollowTarget;
        public float Distance = 5.5f;
        public float Height = 1.15f;
        public float Yaw = 180f;
        public float Pitch = 20f;
        public float MouseSensitivity = .12f;
        public float BlendSpeed = 9f;
        public bool ReducedMotion;
        public LayerMask CollisionLayers = 1;
        public bool Inspecting { get; private set; }
        Transform inspection;
        Vector3 savedPosition;
        Quaternion savedRotation;
        float savedYaw, savedPitch;
        bool restoreFrame;
        public void Look(Vector2 delta)
        { if (Inspecting) return; Yaw += delta.x * MouseSensitivity; Pitch = Mathf.Clamp(Pitch - delta.y * MouseSensitivity, 8f, 55f); }
        public void EnterInspection(Transform target)
        {
            if (!Inspecting) { savedPosition = transform.position; savedRotation = transform.rotation; savedYaw = Yaw; savedPitch = Pitch; }
            inspection = target; Inspecting = target != null;
        }
        public void ExitInspection()
        {
            if (!Inspecting) return;
            Inspecting = false; inspection = null; Yaw = savedYaw; Pitch = savedPitch;
            transform.SetPositionAndRotation(savedPosition, savedRotation); restoreFrame = true;
        }
        public void SnapFollow()
        { Inspecting = false; inspection = null; CalculateFollow(out var p, out var r); transform.SetPositionAndRotation(p,r); }
        void LateUpdate()
        {
            if (restoreFrame) { restoreFrame = false; return; }
            if (!FollowTarget) return;
            Vector3 p; Quaternion r;
            if (Inspecting && inspection) { p = inspection.position; r = inspection.rotation; }
            else CalculateFollow(out p, out r);
            var f = ReducedMotion ? 1f : 1f - Mathf.Exp(-BlendSpeed * Time.unscaledDeltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position,p,f), Quaternion.Slerp(transform.rotation,r,f));
        }
        void CalculateFollow(out Vector3 p, out Quaternion r)
        {
            r = Quaternion.Euler(Pitch, Yaw, 0); var origin = FollowTarget.position + Vector3.up * Height;
            var offset = r * new Vector3(0, 0, -Distance);
            if (Physics.SphereCast(origin, .18f, offset.normalized, out var hit, Distance, CollisionLayers, QueryTriggerInteraction.Ignore))
                offset = offset.normalized * Mathf.Max(.5f, hit.distance - .12f);
            p = origin + offset;
        }
    }
}
