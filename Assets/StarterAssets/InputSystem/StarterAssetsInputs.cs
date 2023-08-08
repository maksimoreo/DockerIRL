using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("References")]
        public DockerIrl.GeneralCharacterBehaviour characterBehaviour;

        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;
        public bool rotateModifier;
        public float rotate;

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorInputForLook = true;

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        public void OnJump(InputValue value)
        {
            if (characterBehaviour.focusedTerminalMonitor != null) return;

            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void OnRotateModifier(InputValue value)
        {
            RotateModifierInput(value.isPressed);
        }

        public void OnRotate(InputValue value)
        {
            RotateInput(value.Get<float>());
        }

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        public void RotateModifierInput(bool newRotateModifierState)
        {
            rotateModifier = newRotateModifierState;
        }

        public void RotateInput(float newRotateValue)
        {
            rotate = newRotateValue;
        }
    }
}
