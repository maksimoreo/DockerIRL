using UnityEngine;

namespace DockerIrl
{
    public abstract class HighlightableBehaviour : MonoBehaviour
    {
        public GameObject rootGameObject;
        public HighlightMenuHandle highlightMenuHandle { get; private set; }

        public bool isSelected => highlightMenuHandle != null;

        public void InvokeHighlight(HighlightMenuHandle highlightMenuHandle)
        {
            this.highlightMenuHandle = highlightMenuHandle;
            Highlight(highlightMenuHandle);
        }

        public void InvokeUnhighlight()
        {
            Unhighlight();
            highlightMenuHandle = null;
        }

        public abstract void Highlight(HighlightMenuHandle handle);
        public virtual void Unhighlight() { }
    }
}
