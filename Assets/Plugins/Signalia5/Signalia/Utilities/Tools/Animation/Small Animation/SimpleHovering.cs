using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Provides a simple hovering animation using basic vector control over time.
    /// Continuously moves the object up and down in a smooth, controllable loop.
    /// </summary>
    [AddComponentMenu("Signalia/Tools/Signalia | Simple Hovering")]
    public class SimpleHovering : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float hoverDistance = 1f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool useUnscaledTime = false;
        [SerializeField] private bool useYoyoLoop = true;

        private Vector3 startPosition;
        private bool isPlaying = false;
        private bool isPaused = false;
        private float elapsedTime = 0f;

        private void Start()
        {
            startPosition = transform.localPosition;
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
                elapsedTime += deltaTime;

                float t = elapsedTime / duration;
                if (useYoyoLoop)
                {
                    // Ping-pong between 0 and 1 using sine wave for smooth easing
                    t = Mathf.PingPong(t, 1f);
                    t = Mathf.SmoothStep(0f, 1f, t);
                }
                else
                {
                    // Restart loop
                    t = t % 1f;
                    t = Mathf.SmoothStep(0f, 1f, t);
                }

                Vector3 offset = Vector3.up * (hoverDistance * t);
                transform.localPosition = startPosition + offset;
            }
        }

        /// <summary>
        /// Starts the hovering animation.
        /// </summary>
        public void Play()
        {
            isPlaying = true;
            isPaused = false;
            elapsedTime = 0f;
        }

        /// <summary>
        /// Stops the hovering animation and returns to start position.
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            isPaused = false;
            elapsedTime = 0f;
            transform.localPosition = startPosition;
        }

        /// <summary>
        /// Pauses the hovering animation.
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// Resumes the hovering animation.
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

