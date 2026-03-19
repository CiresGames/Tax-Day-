using UnityEngine;
using TMPro;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Radio;
using System.Collections.Generic;
using System;
using AHAKuo.Signalia.GameSystems.Localization.Internal;

namespace AHAKuo.Signalia.GameSystems.Notifications
{
    /// <summary>
    /// Component for displaying queued system messages that show and hide their parent UIView.
    /// Messages are displayed in sequence with OnShow and OnHide events.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Notifications/Signalia | System Message")]
    public class SystemMessage : MonoBehaviour
    {
        [Tooltip("The UIView that will be shown/hidden when displaying messages")]
        [SerializeField] private UIView uiView;
        
        [Tooltip("The TMP_Text component that displays the notification message")]
        [SerializeField] private TMP_Text messageText;

        [Tooltip("Audio to play when the notification starts showing")]
        [SerializeField] private string audioOnStart;
        
        [Tooltip("Audio to play when the notification hides")]
        [SerializeField] private string audioOnHide;

        [Tooltip("Radio event string to send when notification shows")]
        [SerializeField] private string onShowRadioEvent;
        
        [Tooltip("Radio event string to send when notification hides")]
        [SerializeField] private string onHideRadioEvent;

        [Tooltip("Unity event invoked when notification shows")]
        [SerializeField] private UnityEngine.Events.UnityEvent onShowUnityEvent;
        
        [Tooltip("Unity event invoked when notification hides")]
        [SerializeField] private UnityEngine.Events.UnityEvent onHideUnityEvent;

        [Tooltip("Unique name identifier for this SystemMessage. Used by SIGS.ShowNotification()")]
        [SerializeField] private string messageName;

        [Tooltip("Default display duration in seconds. Set to -1 to keep showing until manually hidden.")]
        [SerializeField] private float defaultDisplayDuration = 3f;

        private Queue<string> messageQueue = new Queue<string>();
        private bool isDisplaying = false;
        private DeadKey deadKey;

        private void Awake()
        {
            Watchman.Watch();

            if (uiView == null)
            {
                uiView = GetComponentInParent<UIView>();
                if (uiView == null)
                {
                    Debug.LogError($"[SystemMessage] No UIView found on {gameObject.name} or its parents. Please assign a UIView.", this);
                }
            }

            if (messageText == null)
            {
                messageText = GetComponentInChildren<TMP_Text>();
                if (messageText == null)
                {
                    Debug.LogError($"[SystemMessage] No TMP_Text found on {gameObject.name} or its children. Please assign a TMP_Text.", this);
                }
            }

            // Generate name if not set
            if (string.IsNullOrEmpty(messageName))
            {
                messageName = gameObject.name;
            }

            // Register as DeadKey
            deadKey = new DeadKey($"SystemMessage_{messageName}", this, gameObject);
        }

        private void OnDestroy()
        {
            deadKey?.Dispose();
        }

        /// <summary>
        /// Shows a notification message. If a message is already displaying, queues this message.
        /// </summary>
        /// <param name="message">The message text to display</param>
        public void ShowNotification(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning($"[SystemMessage] Attempted to show empty message on {gameObject.name}", this);
                return;
            }

            messageQueue.Enqueue(message);

            if (!isDisplaying)
            {
                ProcessNextMessage();
            }
        }

        private void ProcessNextMessage()
        {
            if (messageQueue.Count == 0)
            {
                isDisplaying = false;
                return;
            }

            isDisplaying = true;
            string message = messageQueue.Dequeue();

            if (messageText != null)
            {
                // Use localization if available
                messageText.SetLocalizedText(message);
            }

            if (uiView != null)
            {
                // Subscribe to hide end event to process next message
                uiView.OnHideEnd -= OnHideComplete;
                uiView.OnHideEnd += OnHideComplete;

                uiView.Show();
            }

            // Play audio
            if (!string.IsNullOrEmpty(audioOnStart))
            {
                SIGS.PlayAudio(audioOnStart);
            }

            // Send radio event
            if (!string.IsNullOrEmpty(onShowRadioEvent))
            {
                SIGS.Send(onShowRadioEvent);
            }

            // Invoke Unity event
            onShowUnityEvent?.Invoke();

            // Auto-hide after duration if set
            if (defaultDisplayDuration > 0)
            {
                SIGS.DoIn(defaultDisplayDuration, () =>
                {
                    if (uiView != null && uiView.IsShown)
                    {
                        HideNotification();
                    }
                });
            }
        }

        /// <summary>
        /// Hides the current notification.
        /// </summary>
        public void HideNotification()
        {
            if (uiView != null && uiView.IsShown)
            {
                // Play audio
                if (!string.IsNullOrEmpty(audioOnHide))
                {
                    SIGS.PlayAudio(audioOnHide);
                }

                // Send radio event
                if (!string.IsNullOrEmpty(onHideRadioEvent))
                {
                    SIGS.Send(onHideRadioEvent);
                }

                // Invoke Unity event
                onHideUnityEvent?.Invoke();

                uiView.Hide();
            }
        }

        private void OnHideComplete()
        {
            // Process next message in queue
            ProcessNextMessage();
        }

        /// <summary>
        /// Clears all queued messages.
        /// </summary>
        public void ClearQueue()
        {
            messageQueue.Clear();
        }

        /// <summary>
        /// Gets the number of messages currently queued.
        /// </summary>
        public int QueueCount => messageQueue.Count;

        /// <summary>
        /// Gets whether a message is currently being displayed.
        /// </summary>
        public bool IsDisplaying => isDisplaying;
    }
}

