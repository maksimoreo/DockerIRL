using StarterAssets;
using UnityEngine;

namespace DockerIrl.Player
{
    public class PauseBehaviour : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private bool _paused = false;
        public bool paused { get => _paused; }

        [Header("References")]
        [SerializeField]
        private FirstPersonController firstPersonController;

        private void OnPause()
        {
            if (!enabled)
            {
                return;
            }

            if (paused)
            {
                firstPersonController.enabled = true;
                _paused = false;
            }
            else
            {
                firstPersonController.enabled = false;
                _paused = true;
            }
        }
    }
}
