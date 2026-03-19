using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Provides a simple rotation animation using basic vector control over time.
    /// Continuously rotates the object around a specified axis in a smooth, controllable loop.
    /// </summary>
    [AddComponentMenu("Signalia/Tools/Signalia | Simple Rotation")]
    public class SimpleRotation : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 90f; // degrees per second
        [SerializeField] private bool useUnscaledTime = false;

        private bool isPlaying = false;
        private bool isPaused = false;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (isPlaying && !isPaused)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float rotationAmount = rotationSpeed * deltaTime;
                transform.Rotate(rotationAxis, rotationAmount, Space.Self);
            }
        }

        /// <summary>
        /// Starts the rotation animation.
        /// </summary>
        public void Play()
        {
            isPlaying = true;
            isPaused = false;
        }

        /// <summary>
        /// Stops the rotation animation.
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            isPaused = false;
        }

        /// <summary>
        /// Pauses the rotation animation.
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// Resumes the rotation animation.
        /// </summary>
        public void Resume()
        {
            if (isPlaying)
            {
                isPaused = false;
            }
        }

        private void OnDisable()
        {
            isPaused = true;
        }

        private void OnEnable()
        {
            if (isPlaying && playOnStart)
            {
                isPaused = false;
            }
        }
    }
}

