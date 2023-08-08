using UnityEngine;
using UnityEngine.InputSystem;

namespace DockerIrl.Player
{
    public class ObjectMovingBehaviour : MonoBehaviour
    {
        [Header("General")]
        public GameObject movingGameObject;
        public MovableBehaviour movingMovable;
        public bool stickToSurface;

        [Header("References")]
        public GeneralCharacterBehaviour characterBehaviour;
        public PlayerHighlightBehavior highlightBehavior;
        public HighlightMenu highlightMenu;

        public InputLoader input;

        [Header("Other")]
        public bool debug = false;

        [HideInInspector]
        private InputAction rotateModifierAction;
        private InputAction lookAction;

        private bool pickedUpObjectThisFrame = false;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private void Start()
        {
            rotateModifierAction = input.playerInput.actions.FindAction("RotateModifier", true);
            lookAction = input.playerInput.actions.FindAction("Look", true);
        }

        private void Update()
        {
            if (movingGameObject)
            {
                MoveSelectedObjectToPointer();
                UpdateHighlightMenuText();
            }

            if (pickedUpObjectThisFrame) pickedUpObjectThisFrame = false;
        }

        // !InputAction
        private void OnMoveObject()
        {
            if (!enabled) return;
            if (movingGameObject) return;

            if (!highlightBehavior.currentTargetHighlightable)
            {
                LogMe(".OnMoveObject(): No object highlighted (highlightBehavior.currentTargetHighlightable is null)");
                return;
            }

            MovableBehaviour movable = highlightBehavior.currentTargetHighlightable.rootGameObject.GetComponent<MovableBehaviour>();

            if (!movable)
            {
                LogMe(".OnMoveObject(): Object is not movable (Missing MovableBehaviour on highlighted object)");
                return;
            }

            LogMe(".OnMoveObject(): Starting moving object");

            movingMovable = movable;
            movingGameObject = movable.BeginMove();

            originalPosition = movingGameObject.transform.position;
            originalRotation = movingGameObject.transform.rotation;

            characterBehaviour.cinemachineBreathing = false;
            highlightBehavior.enabled = false;

            UpdateHighlightMenuText();

            pickedUpObjectThisFrame = true;

            LogMe(".OnMoveObject(): Done");
        }

        // !InputAction
        private void OnPlaceObject()
        {
            if (!enabled) return;
            if (!movingGameObject) return;
            if (pickedUpObjectThisFrame) return;

            if (!movingMovable.CanPlace())
            {
                LogMe(".OnPlaceObject(): Object refused to be placed (.CanPlace() returned false)");
                return;
            }

            LogMe(".OnMoveObject(): Starting placing object");

            movingMovable.EndMove();

            ResetState();

            LogMe(".OnMoveObject(): Done");
        }

        // !InputAction
        private void OnCancelMoveObject()
        {
            if (!enabled) return;
            if (!movingGameObject) return;
            if (pickedUpObjectThisFrame) return;

            LogMe(".OnCancelMoveObject(): Starting placing object");

            movingGameObject.transform.position = originalPosition;
            movingGameObject.transform.rotation = originalRotation;
            movingMovable.EndMove();

            ResetState();

            LogMe(".OnCancelMoveObject(): Done");
        }

        // !InputAction
        private void OnLook()
        {
            if (!enabled) return;
            if (!movingGameObject) return;
            if (rotateModifierAction.ReadValue<float>() < 0.01) return;

            var currentLook = lookAction.ReadValue<Vector2>();
            movingGameObject.transform.Rotate(
                currentLook.y,
                currentLook.x,
                0
            );
        }

        private void ResetState()
        {
            LogMe(".ResetState()");

            highlightMenu.Hide();
            characterBehaviour.cinemachineBreathing = true;
            highlightBehavior.enabled = true;

            movingGameObject = null;
            movingMovable = null;
            originalPosition = Vector3.zero;
            originalRotation = Quaternion.identity;
        }

        private void MoveSelectedObjectToPointer()
        {
            Ray cursorRay = highlightBehavior.GetCursorRay();

            if (Physics.Raycast(cursorRay, out RaycastHit raycastHit, highlightBehavior.interactionRange))
            {
                movingGameObject.transform.position = raycastHit.point;
            }
            else
            {
                movingGameObject.transform.position = characterBehaviour.transform.position + cursorRay.direction * highlightBehavior.interactionRange;
            }
        }

        private void UpdateHighlightMenuText()
        {
            var position = movingGameObject.transform.position;
            var rotation = movingGameObject.transform.rotation.eulerAngles;

            var placeObjectText = movingMovable.CanPlace() ? $"{input.placeObjectActionBindingDisplayString} - place object" : "Cannot place here";

            highlightMenu.ShowText($@"Moving {movingGameObject.name}
Position:
    x: {position.x}
    y: {position.y}
    z: {position.z}
Rotation:
    x: {rotation.x}
    y: {rotation.y}
    z: {rotation.z}
{placeObjectText}
{input.cancelMoveObjectActionBindingDisplayString} - cancel move");
        }

        private void LogMe(string message)
        {
            if (!debug) return;

            Debug.Log($"ObjectMovingehaviourV2{message}");
        }
    }
}
