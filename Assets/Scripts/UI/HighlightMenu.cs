using UnityEngine;

namespace DockerIrl
{
    public class HighlightMenu : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI textComponent;

        public void ShowText(string text)
        {
            textComponent.SetText(text);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
