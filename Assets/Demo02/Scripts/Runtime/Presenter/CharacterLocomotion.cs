using Altaius.Demo02.Gameplay;
using UnityEngine;
namespace Altaius.Demo02.Presenter
{
    [DefaultExecutionOrder(40)]
    public sealed class CharacterLocomotion:MonoBehaviour
    {
        public CampusMotor Motor;public Animator Animator;public string SpeedParameter="Speed";
        int parameter;
        void Awake(){parameter=UnityEngine.Animator.StringToHash(SpeedParameter);if(Animator)Animator.applyRootMotion=false;}
        void LateUpdate(){if(Motor&&Animator){Animator.applyRootMotion=false;Animator.SetFloat(parameter,Motor.CurrentSpeed);}}
        void OnDisable(){if(Animator&&Animator.isActiveAndEnabled)Animator.SetFloat(UnityEngine.Animator.StringToHash(SpeedParameter),0);}
    }
}
