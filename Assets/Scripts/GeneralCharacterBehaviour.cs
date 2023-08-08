using Cinemachine;
using DockerIrl.Serialization;
using DockerIrl.Terminal;
using UnityEngine;

namespace DockerIrl
{
    // Common features related to character controlling
    public class GeneralCharacterBehaviour : MonoBehaviour
    {
        [Header("General")]
        public TerminalMonitor focusedTerminalMonitor;

        [Header("References")]
        public Transform playerCameraRoot;
        public Transform playerCapsule;
        public CharacterController characterController;
        public StarterAssets.FirstPersonController firstPersonController;
        public Player.PauseBehaviour pauseBehaviour;
        public Player.ObjectMovingBehaviour objectMovingBehaviour;
        public PlayerHighlightBehavior highlightBehavior;
        public UnityEngine.InputSystem.PlayerInput input;
        public CinemachineVirtualCamera cinemachineCamera;
        public FootstepsController footstepsController;

        private CinemachineBasicMultiChannelPerlin cinemachineNoise;

        public bool cinemachineBreathing
        {
            get => cinemachineNoise.enabled;
            set => cinemachineNoise.enabled = value;
        }

        private void Awake()
        {
            cinemachineNoise = cinemachineCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }

        public void SetCameraRotation(Vector2 rotation) => SetCameraRotation(rotation.x, rotation.y);
        public void SetCameraRotation(float x, float y)
        {
            playerCapsule.rotation = Quaternion.Euler(0, y, 0);
            playerCameraRoot.localRotation = Quaternion.Euler(x, 0, 0);
        }

        public Vector2 GetCameraRotation()
        {
            return new Vector2
            (
                playerCameraRoot.localRotation.eulerAngles.x,
                playerCapsule.rotation.eulerAngles.y
            );
        }

        public SerializedPlayer ToSerializableObject()
        {
            return new SerializedPlayer()
            {
                position = transform.position,
                rotation = GetCameraRotation(),
            };
        }

        // !InputAction
        public void OnReload()
        {
            if (focusedTerminalMonitor) return;

            _ = DockerIrlApp.instance.reload.Call();
        }

        // !InputAction
        public void OnQuit()
        {
            if (focusedTerminalMonitor) return;

            _ = DockerIrlApp.instance.quit.Call();
        }
    }
}
