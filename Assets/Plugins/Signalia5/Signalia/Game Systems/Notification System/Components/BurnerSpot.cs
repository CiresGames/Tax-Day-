using UnityEngine;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.PackageHandlers;
using AHAKuo.Signalia.Radio;
using DG.Tweening;
using AHAKuo.Signalia.GameSystems.PoolingSystem;

namespace AHAKuo.Signalia.GameSystems.Notifications
{
    /// <summary>
    /// Component that manages burner notifications at a specific spot.
    /// Burners are pooled objects that appear, float upward, and disappear.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Notifications/Signalia | Burner Spot")]
    public class BurnerSpot : MonoBehaviour
    {
        [Tooltip("Unique name identifier for this BurnerSpot. Used by SIGS.ShowBurner()")]
        [SerializeField] private string spotName;

        [Tooltip("Prefab for the burner object that will be spawned")]
        [SerializeField] private GameObject burnerPrefab;

        [Tooltip("Offset from this transform's position where burners will spawn")]
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        [Tooltip("Whether to use world position or local position for spawn offset")]
        [SerializeField] private bool useWorldSpace = true;

        [Tooltip("Delay between showing burners when multiple are queued (in seconds)")]
        [SerializeField] private float bufferDelay = 0.5f;

        private DeadKey deadKey;
        private Queue<string> burnerQueue = new Queue<string>();
        private bool isProcessingQueue = false;
        private Tween queueTween;

        private void Awake()
        {
            Watchman.Watch();

            // Generate name if not set
            if (string.IsNullOrEmpty(spotName))
            {
                spotName = gameObject.name;
            }

            if (burnerPrefab == null)
            {
                Debug.LogError($"[BurnerSpot] No burner prefab assigned on {gameObject.name}. Please assign a prefab with BurnerObject component.", this);
            }
            else
            {
                burnerPrefab.WarmupPool(1);
            }
            

            // Register as DeadKey
            deadKey = new DeadKey($"BurnerSpot_{spotName}", this, gameObject);
        }

        private void OnDestroy()
        {
            queueTween?.Kill();
            deadKey?.Dispose();
        }

        /// <summary>
        /// Shows a burner notification at this spot.
        /// Requires SIGS_PS (Pooling System) to be defined.
        /// Queues multiple requests to prevent overlapping burners.
        /// </summary>
        /// <param name="message">Optional message text to display on the burner</param>
        public void ShowBurner(string message = null)
        {
            if (burnerPrefab == null)
            {
                Debug.LogError($"[BurnerSpot] Cannot show burner - no prefab assigned on {gameObject.name}", this);
                return;
            }

            // Add to queue
            burnerQueue.Enqueue(message ?? string.Empty);

            // Start processing queue if not already processing
            if (!isProcessingQueue)
            {
                ProcessQueue();
            }
        }

        /// <summary>
        /// Processes the burner queue one at a time with delays between each.
        /// </summary>
        private void ProcessQueue()
        {
            if (burnerQueue.Count == 0)
            {
                isProcessingQueue = false;
                return;
            }

            isProcessingQueue = true;

            // Get next message from queue
            string message = burnerQueue.Dequeue();

            // Show the burner
            ShowBurnerImmediate(message);

            // Schedule next burner in queue after buffer delay
            queueTween?.Kill();
            queueTween = SIGS.DoIn(bufferDelay, () =>
            {
                ProcessQueue();
            });
        }

        /// <summary>
        /// Immediately shows a burner notification without queuing.
        /// Internal method used by the queue processor.
        /// </summary>
        /// <param name="message">Optional message text to display on the burner</param>
        private void ShowBurnerImmediate(string message = null)
        {
            // Get pooled burner object
            GameObject burnerObj = SIGS.PoolingGet(burnerPrefab, -1f, true);

            if (burnerObj == null)
            {
                Debug.LogError($"[BurnerSpot] Failed to get pooled burner object from {burnerPrefab.name}", this);
                return;
            }

            // Parent to this transform so it's in the canvas
            burnerObj.transform.SetParent(transform, false);
            
            // Position the burner
            Vector3 spawnPosition = useWorldSpace 
                ? spawnOffset 
                : spawnOffset;
            
            burnerObj.transform.localPosition = spawnPosition;

            // Set message if provided
            BurnerObject burnerComponent = burnerObj.GetComponent<BurnerObject>();
            if (burnerComponent != null && !string.IsNullOrEmpty(message))
            {
                burnerComponent.SetMessage(message);
            }

            // Activate and start animation
            burnerObj.SetActive(true);
            if (burnerComponent != null)
            {
                burnerComponent.StartBurnerAnimation();
            }
        }
    }
}

