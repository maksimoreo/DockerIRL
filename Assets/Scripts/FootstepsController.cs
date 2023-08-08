using System.Collections.Generic;
using UnityEngine;

namespace DockerIrl
{
    /// <summary>
    /// Plays random audio clip when character is moving.
    /// </summary>
    public class FootstepsController : MonoBehaviour
    {
        public AudioSource audioSource;
        public CharacterController characterController;
        public List<AudioClip> stepsAudioClips = new();
        public float secondsBetweenSteps = 0.5f;

        private float timeToNextStep = 0;

        private void Update()
        {
            if (characterController.isGrounded && characterController.velocity.sqrMagnitude > 0.01)
            {
                timeToNextStep -= Time.deltaTime;

                if (timeToNextStep < 0.01)
                {
                    audioSource.clip = Utils.X.RandomSample(stepsAudioClips);
                    audioSource.Play();
                    timeToNextStep += secondsBetweenSteps;
                }
            }
        }
    }
}
