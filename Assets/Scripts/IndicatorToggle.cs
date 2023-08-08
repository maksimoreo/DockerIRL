using UnityEngine;

namespace DockerIrl
{
    public class IndicatorToggle : MonoBehaviour
    {
        [Header("General")]
        [Tooltip("Will change this object's mesh renderer's material")]
        public MeshRenderer targetMeshRenderer;

        [Tooltip("Initial state / Enabled by default")]
        public bool initiallyEnabled;

        [Header("Enabled state")]
        public Color enabledColor;

        [Header("Disabled state")]
        public Color disabledColor;

        public bool indicatorEnabled
        {
            get => _indicatorEnabled;
            set
            {
                _indicatorEnabled = value;

                // Note: See shader code for property names
                // See: Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader

                if (value)
                {
                    targetMeshRenderer.material.SetColor("_BaseColor", enabledColor);
                }
                else
                {
                    targetMeshRenderer.material.SetColor("_BaseColor", disabledColor);
                }
            }
        }
        private bool _indicatorEnabled;

        private void Start()
        {
            indicatorEnabled = initiallyEnabled;
        }

        public void EnableIndicator() => indicatorEnabled = true;
        public void DisableIndicator() => indicatorEnabled = false;
        public void ToggleIndicator() => indicatorEnabled = !indicatorEnabled;
    }
}
