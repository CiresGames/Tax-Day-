using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Provides a simple scale animation using basic vector control over time.
    /// Continuously scales the object in a smooth, controllable loop.
    /// </summary>
    [AddComponentMenu("Signalia/Tools/Signalia | Simple Scale")]
    public class SimpleScale : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private Vector3 targetScale = Vector3.one * 1.2f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool useUnscaledTime = false;
        [SerializeField] private bool useYoyoLoop = true;

        private Vector3 startScale;
        private bool isPlaying = false;
        private bool isPaused = false;
        private float elapsedTime = 0f;

        private void Start()
        {
            startScale = transform.localScale;
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

                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            }
        }

        /// <summary>
        /// Starts the scale animation.
        /// </summary>
        public void Play()
        {
            isPlaying = true;
            isPaused = false;
            elapsedTime = 0f;
        }

        /// <summary>
        /// Stops the scale animation and returns to start scale.
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            isPaused = false;
            elapsedTime = 0f;
            transform.localScale = startScale;
        }

        /// <summary>
        /// Pauses the scale animation.
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// Resumes the scale animation.
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

