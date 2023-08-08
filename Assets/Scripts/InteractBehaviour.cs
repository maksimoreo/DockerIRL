using UnityEngine;

namespace DockerIrl
{
    public class InteractBehaviour : MonoBehaviour
    {
        public PlayerHighlightBehavior highlightBehavior;

        public InteractableBehaviour currentInteractable;

        public void OnInteract()
        {
            if (!enabled) return;
            if (!highlightBehavior.enabled) return;

            HighlightableBehaviour highlightable = highlightBehavior.currentTargetHighlightable;
            if (highlightable == null) return;

            if (!highlightable.rootGameObject.TryGetComponent<InteractableBehaviour>(out var interactable)) return;

            Debug.Log("Sending Interact() to InteractableBehaviour");

            interactable.Interact();
        }
    }
}
