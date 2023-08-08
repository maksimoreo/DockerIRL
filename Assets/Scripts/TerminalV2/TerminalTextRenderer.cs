using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace DockerIrl.Terminal
{
    public class TerminalTextRenderer : MonoBehaviour
    {
        public TerminalController terminalController;

        public UnityEvent OnSetText;
        public UnityEvent OnSetErrorText;

        [SerializeField]
        private TextMeshProUGUI foregroundTmpro;
        [SerializeField]
        private TextMeshProUGUI backgroundTmpro;

        public float fontSize
        {
            get => foregroundTmpro.fontSize;
            set
            {
                foregroundTmpro.fontSize = value;
                backgroundTmpro.fontSize = value;
            }
        }

        public void SetText(string foregroundTmproText, string backgroundTmproText)
        {
            foregroundTmpro.text = foregroundTmproText;
            backgroundTmpro.text = backgroundTmproText;

            OnSetText?.Invoke();
        }

        public void SetErrorText(string text)
        {
            foregroundTmpro.text = text;
            backgroundTmpro.text = "";

            OnSetErrorText?.Invoke();
        }
    }
}
