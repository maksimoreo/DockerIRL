using UnityEngine;
using UnityEngine.Events;

namespace DockerIrl
{
    // This object is given to "Highlightable", so it can show some text on highlightMenu.
    // Object can ShowText() multiple times while highlighted, to reflect up-to-date info when its state changes.
    public class HighlightMenuHandle
    {
        private HighlightMenu menu;
        private bool revoked = false;

        public HighlightMenuHandle(HighlightMenu menu)
        {
            this.menu = menu;
        }

        public void ShowText(string text)
        {
            Validate();
            menu.ShowText(text);
        }

        public void Hide()
        {
            Validate();
            menu.Hide();
        }

        public void Revoke() => revoked = true;

        public void Validate()
        {
            if (revoked)
                throw new System.Exception("This handle is revoked");
        }
    }

    public class PlayerHighlightBehavior : MonoBehaviour
    {
        public new Camera camera;
        public float interactionRange = 15f;
        public HighlightMenu highlightMenu;

        public UnityEvent onHighlight;
        public UnityEvent onUnhighlight;

        public GameObject currentTarget { get; private set; }
        public HighlightableBehaviour currentTargetHighlightable;
        private HighlightMenuHandle currentMenuHandle;

        private Vector3 screenCenter;

        void Awake()
        {
            screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        }

        void Update()
        {
            HighlightUpdate();
        }

        private void OnDisable()
        {
            UnhighlightCurrentTargetAndSetToNull();
        }

        public Ray GetCursorRay()
        {
            return camera.ScreenPointToRay(screenCenter);
        }

        public bool RaycastCursor(out RaycastHit raycastHit)
        {
            return Physics.Raycast(GetCursorRay(), out raycastHit, interactionRange);
        }

        private void HighlightUpdate()
        {
            RaycastHit raycastHit;

            if (!RaycastCursor(out raycastHit))
            {
                UnhighlightCurrentTargetAndSetToNull();
                return;
            }

            GameObject target = raycastHit.collider.gameObject;
            if (!target)
            {
                UnhighlightCurrentTargetAndSetToNull();
                return;
            }

            // No changes in target
            if (target == currentTarget) return;

            HighlightableBehaviour targetHighlightable = target.GetComponent<HighlightableBehaviour>();
            if (!targetHighlightable)
            {
                UnhighlightCurrentTargetAndSetToNull();
                return;
            }

            // Target changed, then unhighlight current target and highlight new target
            if (currentTargetHighlightable) CancelCurrentTargetHighlight();

            currentTarget = target;
            currentTargetHighlightable = targetHighlightable;
            currentMenuHandle = new HighlightMenuHandle(highlightMenu);

            targetHighlightable.InvokeHighlight(currentMenuHandle);
            onHighlight.Invoke();
        }

        private void CancelCurrentTargetHighlight()
        {
            onUnhighlight.Invoke();
            currentTargetHighlightable.InvokeUnhighlight();
            highlightMenu.Hide();
        }

        private void UnhighlightCurrentTargetAndSetToNull()
        {
            if (currentTargetHighlightable)
            {
                CancelCurrentTargetHighlight();
                currentTarget = null;
                currentTargetHighlightable = null;
            }
        }
    }
}
