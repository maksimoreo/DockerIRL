using UnityEngine;

namespace DockerIrl
{
    [RequireComponent(typeof(IndicatorToggle))]
    public class IndicatorSignal : MonoBehaviour
    {
        public float blinkTime = 0.1f;

        private IndicatorToggle indicatorToggle;
        private float currentBlinkTime = 0f;

        private void Awake()
        {
            indicatorToggle = GetComponent<IndicatorToggle>();
        }

        private void Update()
        {
            if (currentBlinkTime < 0f)
                return;

            currentBlinkTime -= Time.deltaTime;

            if (currentBlinkTime < 0f)
                indicatorToggle.DisableIndicator();
        }

        public void Signal()
        {
            currentBlinkTime = blinkTime;
            indicatorToggle.EnableIndicator();
        }
    }
}
