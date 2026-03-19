using UnityEngine;
using TMPro;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.UI;
using DG.Tweening;
using AHAKuo.Signalia.GameSystems.Localization.Internal;

namespace AHAKuo.Signalia.GameSystems.Notifications
{
    /// <summary>
    /// Component for burner notification objects that float upward and disappear.
    /// These are pooled objects spawned by BurnerSpot.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Notifications/Signalia | Burner Object")]
    public class BurnerObject : MonoBehaviour
    {
        [Tooltip("The UIView component for this burner")]
        [SerializeField] private UIView uiView;
        
        [Tooltip("The TMP_Text component that displays the message")]
        [SerializeField] private TMP_Text messageText;

        [Tooltip("Distance to float upward")]
        [SerializeField] private float floatDistance = 100f;
        
        [Tooltip("Duration of the float animation")]
        [SerializeField] private float floatDuration = 2f;

        [Tooltip("Audio to play when burner appears")]
        [SerializeField] private string audioOnStart;

        private Tween floatTween;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            Watchman.Watch();

            if (uiView == null)
            {
                uiView = GetComponentInChildren<UIView>();
            }

            if (messageText == null)
            {
                messageText = GetComponentInChildren<TMP_Text>();
            }

            // Get or add CanvasGroup for fade
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Sets the message text for this burner.
        /// </summary>
        /// <param name="message">The message to display</param>
        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                // Use localization if available
                messageText.SetLocalizedText(message);
            }
        }

        /// <summary>
        /// Starts the burner animation sequence.
        /// </summary>
        public void StartBurnerAnimation()
        {
            // Show UIView if available
            if (uiView != null)
            {
                uiView.Show(); // Instant show
            }

            // Play audio
            if (!string.IsNullOrEmpty(audioOnStart))
            {
                SIGS.PlayAudio(audioOnStart);
            }

            // Kill any existing tween
            floatTween?.Kill();

            // Capture current Y position and float upward (only Y axis, preserving X and Z)
            float startY = transform.localPosition.y;
            float endY = startY + floatDistance;
            
            floatTween = transform.DOLocalMoveY(endY, floatDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            // Ensure object is deactivated after float duration
            SIGS.DoIn(floatDuration, () =>
            {
                uiView.Hide();
            });
        }

        private void OnDisable()
        {
            floatTween?.Kill();
            canvasGroup.alpha = 1f;
        }
    }
}

