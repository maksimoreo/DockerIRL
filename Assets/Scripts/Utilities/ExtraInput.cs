using UnityEngine;
using UnityEngine.UIElements;

namespace DockerIrl.Utils
{
    /// <summary>
    /// Literally just captures input. It took several eternities to produce this code.
    /// If u hold a key for 0.5 secs, then it will print single `a`, but if hold it a bit longer, it will suddenly
    /// rapidly print `aaaaaaa`
    /// Thats said, if u know a better way to do this, please refactor this.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ExtraInput : MonoBehaviour
    {
        public float visibilityOnStart = 0;

        private TextField textField;

        public event System.Action<KeyCode> OnInput;

        public bool capturing { get; private set; }

        private void OnEnable()
        {
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;

            // Hack: Create invisible textField that will capture input.
            textField = new TextField();
            rootVisualElement.Add(textField);

            textField.style.opacity = visibilityOnStart;
            textField.style.width = 1f;
            textField.style.height = 1f;

            // Note: these options will cause input capturing to be skipped:
            // textField.style.display = DisplayStyle.None;
            // textField.visible = false;

            textField.RegisterCallback<KeyDownEvent>((e) =>
            {
                e.PreventDefault();
                OnInput?.Invoke(e.keyCode);
            });
        }

        private void Update()
        {
            if (capturing && textField.focusController.focusedElement != textField)
            {
                textField.Focus();
            }
        }

        public void StartCapturing()
        {
            textField.Focus();
            capturing = true;
        }

        public void StopCapturing()
        {
            textField.Blur();
            capturing = false;
        }
    }
}
