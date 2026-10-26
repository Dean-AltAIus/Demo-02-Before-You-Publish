using Altaius.Demo02.Gameplay;
using UnityEngine;

namespace Altaius.Demo02.UI
{
    /// <summary>One saved shot, using the existing world camera. Ownership is explicitly reversible.</summary>
    [DisallowMultipleComponent]
    public sealed class TitleScenePresentation : MonoBehaviour
    {
        public CampusCamera Rig;
        public Transform Shot;
        [Range(35,85)] public float FieldOfView=58;
        public Renderer[] HiddenLearnerRenderers=System.Array.Empty<Renderer>();
        public bool Owned { get; private set; }
        Camera cameraComponent;
        Vector3 position;
        Quaternion rotation;
        float fieldOfView;
        Matrix4x4 projection;
        bool rigEnabled;
        bool[] rendererStates;
        public void Enter()
        {
            if(Owned||!Rig||!Shot)return;
            cameraComponent=Rig.GetComponent<Camera>();
            position=Rig.transform.position;rotation=Rig.transform.rotation;rigEnabled=Rig.enabled;
            fieldOfView=cameraComponent.fieldOfView;projection=cameraComponent.projectionMatrix;
            // Disabling the follow rig resets body culling. Capture the exact prior visibility first.
            rendererStates=new bool[HiddenLearnerRenderers.Length];
            for(int i=0;i<rendererStates.Length;i++)if(HiddenLearnerRenderers[i])rendererStates[i]=HiddenLearnerRenderers[i].forceRenderingOff;
            Rig.enabled=false;
            for(int i=0;i<rendererStates.Length;i++)if(HiddenLearnerRenderers[i])HiddenLearnerRenderers[i].forceRenderingOff=true;
            Rig.transform.SetPositionAndRotation(Shot.position,Shot.rotation);
            cameraComponent.fieldOfView=FieldOfView;cameraComponent.ResetProjectionMatrix();Owned=true;
        }
        public void Release()
        {
            if(!Owned)return;Owned=false;
            if(Rig){Rig.transform.SetPositionAndRotation(position,rotation);Rig.enabled=rigEnabled;}
            if(cameraComponent){cameraComponent.fieldOfView=fieldOfView;cameraComponent.projectionMatrix=projection;}
            for(int i=0;i<rendererStates.Length;i++)if(HiddenLearnerRenderers[i])HiddenLearnerRenderers[i].forceRenderingOff=rendererStates[i];
        }
        void OnDisable()=>Release();
    }
}
