using UnityEngine.Events;

namespace DockerIrl
{
    /// <summary>
    /// Invokes UnityEvents on interact
    /// </summary>
    public sealed class BasicInteractable : InteractableBehaviour
    {
        public UnityEvent OnInteract;

        public override void Interact() => OnInteract?.Invoke();
    }
}
