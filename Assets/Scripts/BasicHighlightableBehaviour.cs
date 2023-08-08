using UnityEngine.Events;

namespace DockerIrl
{
    /// <summary>
    /// Invokes UnityEvents on highlight
    /// </summary>
    public sealed class BasicHighlightableBehaviour : HighlightableBehaviour
    {
        public UnityEvent<HighlightMenuHandle> onHighlight;
        public UnityEvent onUnhighlight;

        public override void Highlight(HighlightMenuHandle highlightMenuHandle) => onHighlight?.Invoke(highlightMenuHandle);
        public override void Unhighlight() => onUnhighlight?.Invoke();
    }
}
