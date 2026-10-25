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
        public float CurrentSpeed { get; private set; }
        CharacterController controller;
        float vertical;
        void Awake() { controller = GetComponent<CharacterController>(); }
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
        }
        public void GoTo(Vector3 position, Quaternion facing)
        {
            if (!controller) controller = GetComponent<CharacterController>();
            controller.enabled = false; transform.position = position; controller.enabled = true; vertical = 0;
            if (CharacterVisual) CharacterVisual.rotation = facing;
        }
    }
}
