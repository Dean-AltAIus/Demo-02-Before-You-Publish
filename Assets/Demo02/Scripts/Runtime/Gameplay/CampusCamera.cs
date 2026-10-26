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
        public Renderer[] LocalBodyRenderers=System.Array.Empty<Renderer>();
        public float HideBodyDistance=.75f,RestoreBodyDistance=.95f;
        public bool BodyOccluded { get; private set; }
        bool[] originalForceOff;
        public bool Inspecting { get; private set; }
        public Transform ContactView;
        public bool ContactFraming;
        [Range(.05f,.5f)]public float ContactScreenX=.15f;
        [Range(.15f,.65f)]public float ContactScreenY=.5f;
        [Range(35,85)]public float ContactFieldOfView=72;
        Camera renderCamera;
        float authoredFieldOfView;
        Transform inspection;
        public Transform InspectionTarget=>inspection;
        Vector3 savedPosition;
        Quaternion savedRotation;
        float savedYaw, savedPitch;
        bool restoreFrame;
        void Awake(){renderCamera=GetComponent<Camera>();if(renderCamera)authoredFieldOfView=renderCamera.fieldOfView;originalForceOff=new bool[LocalBodyRenderers.Length];for(int i=0;i<LocalBodyRenderers.Length;i++)if(LocalBodyRenderers[i])originalForceOff[i]=LocalBodyRenderers[i].forceRenderingOff;}
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
            Inspecting = false; inspection = null;ContactFraming=false;ContactView=null;ResetFraming(); Yaw = savedYaw; Pitch = savedPitch;
            transform.SetPositionAndRotation(savedPosition, savedRotation); restoreFrame = true;
        }
        public void SnapFollow()
        { Inspecting = false; inspection = null;ContactFraming=false;ContactView=null;ResetFraming(); CalculateFollow(out var p, out var r); transform.SetPositionAndRotation(p,r); }
        void LateUpdate()
        {
            if (restoreFrame) { restoreFrame = false; return; }
            if (!FollowTarget) return;
            Vector3 p; Quaternion r;
            if(Inspecting&&ContactFraming&&ContactView){p=ContactView.position;r=ContactView.rotation;}
            else if (Inspecting && inspection) { p = inspection.position; r = inspection.rotation; }
            else CalculateFollow(out p, out r);
            var f = ReducedMotion ? 1f : 1f - Mathf.Exp(-BlendSpeed * Time.unscaledDeltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position,p,f), Quaternion.Slerp(transform.rotation,r,f));
            float bodyDistance=Vector3.Distance(transform.position,FollowTarget.position+Vector3.up*Height);
            SetBodyOcclusion(!Inspecting&&(BodyOccluded?bodyDistance<RestoreBodyDistance:bodyDistance<HideBodyDistance));
            if(!renderCamera)renderCamera=GetComponent<Camera>();
            if(renderCamera){renderCamera.fieldOfView=Inspecting&&ContactFraming&&ContactView?ContactFieldOfView:authoredFieldOfView;renderCamera.ResetProjectionMatrix();if(Inspecting&&ContactFraming&&ContactView){var matrix=renderCamera.projectionMatrix;matrix.m02=1-2*ContactScreenX;matrix.m12=1-2*ContactScreenY;renderCamera.projectionMatrix=matrix;}}
        }
        void ResetFraming(){if(!renderCamera)renderCamera=GetComponent<Camera>();if(renderCamera){if(authoredFieldOfView>0)renderCamera.fieldOfView=authoredFieldOfView;renderCamera.ResetProjectionMatrix();}}
        void OnDisable(){ResetFraming();SetBodyOcclusion(false);}
        void SetBodyOcclusion(bool hidden)
        {
            BodyOccluded=hidden;
            for(int i=0;i<LocalBodyRenderers.Length;i++)if(LocalBodyRenderers[i])LocalBodyRenderers[i].forceRenderingOff=hidden||(originalForceOff!=null&&i<originalForceOff.Length&&originalForceOff[i]);
        }
        void CalculateFollow(out Vector3 p, out Quaternion r)
        {
            r = Quaternion.Euler(Pitch, Yaw, 0); var origin = FollowTarget.position + Vector3.up * Height;
            var offset = r * new Vector3(0, 0, -Distance);
            if (Physics.SphereCast(origin, .18f, offset.normalized, out var hit, Distance, CollisionLayers, QueryTriggerInteraction.Ignore))
                offset = offset.normalized * Mathf.Max(0f, hit.distance - .12f);
            p = origin + offset;
        }
    }
}
