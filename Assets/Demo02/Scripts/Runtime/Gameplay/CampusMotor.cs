using UnityEngine;

namespace Altaius.Demo02.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CampusMotor : MonoBehaviour
    {
        public Transform CharacterVisual;
        public Camera ViewCamera;
        public float MoveSpeed = 1.35f;
        public float TurnSpeed = 540f;
        public float Gravity = 18f;
        public bool InputBlocked;
        public bool KeepVisualGrounded=true;
        public float CurrentSpeed { get; private set; }
        CharacterController controller;
        float vertical;
        Vector3 authoredVisualOffset;
        void Awake() { controller = GetComponent<CharacterController>();if(CharacterVisual)authoredVisualOffset=CharacterVisual.localPosition; }
        public void Move(Vector2 input, float deltaTime)
        {
            if (!controller) controller = GetComponent<CharacterController>();
            input = InputBlocked ? Vector2.zero : Vector2.ClampMagnitude(input, 1);
            var forward = ViewCamera ? ViewCamera.transform.forward : Vector3.forward;
            forward.y = 0; forward.Normalize(); var right = Vector3.Cross(Vector3.up, forward);
            var motion = forward * input.y + right * input.x;
            if (motion.sqrMagnitude > .001f && CharacterVisual)
                CharacterVisual.rotation = Quaternion.RotateTowards(CharacterVisual.rotation, Quaternion.LookRotation(motion), TurnSpeed * deltaTime);
            vertical = controller.isGrounded && vertical < 0 ? -2f : vertical - Gravity * deltaTime;
            var before=transform.position;
            controller.Move((motion * MoveSpeed + Vector3.up * vertical) * deltaTime);
            var travelled=transform.position-before;travelled.y=0;
            CurrentSpeed=deltaTime>0?travelled.magnitude/deltaTime:0;
            AlignVisualToGround();
        }
        public void GoTo(Vector3 position, Quaternion facing)
        {
            if (!controller) controller = GetComponent<CharacterController>();
            controller.enabled = false; transform.position = position; controller.enabled = true; vertical = 0;
            if (CharacterVisual) CharacterVisual.rotation = facing;
        }
        // Separate authored contact stances preserve the reviewed station arrival/range contract.
        // CharacterController resolves the complete movement, so obstacles can prevent arrival.
        public void MoveToContact(Vector3 position, Quaternion facing, float deltaTime)
        {
            if(!controller)controller=GetComponent<CharacterController>();
            var delta=position-transform.position;delta.y=0;
            var motion=Vector3.ClampMagnitude(delta,MoveSpeed*deltaTime);
            vertical=controller.isGrounded&&vertical<0?-2f:vertical-Gravity*deltaTime;
            var before=transform.position;controller.Move(motion+Vector3.up*vertical*deltaTime);
            var travelled=transform.position-before;travelled.y=0;CurrentSpeed=deltaTime>0?travelled.magnitude/deltaTime:0;
            if(CharacterVisual)CharacterVisual.rotation=Quaternion.RotateTowards(CharacterVisual.rotation,facing,TurnSpeed*deltaTime);
            AlignVisualToGround();
        }
        void AlignVisualToGround()
        {
            if(!KeepVisualGrounded||!CharacterVisual||!controller.isGrounded)return;
            // CharacterController skin keeps its origin above a supporting plane. Preserve the
            // authored visual offset while grounding the visible shoes on that actual plane.
            var start=transform.position+Vector3.up*.12f;
            if(Physics.Raycast(start,Vector3.down,out var hit,.25f,~(1<<gameObject.layer),QueryTriggerInteraction.Ignore))
            {var local=CharacterVisual.localPosition;local.y=authoredVisualOffset.y+hit.point.y-transform.position.y;CharacterVisual.localPosition=local;}
        }
    }
}
