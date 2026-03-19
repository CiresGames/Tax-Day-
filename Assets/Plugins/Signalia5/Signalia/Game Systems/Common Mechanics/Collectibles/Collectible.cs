using System;
using System.Collections.Generic;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using DG.Tweening;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.GameSystems.Inventory.Game;
using AHAKuo.Signalia.GameSystems.SaveSystem;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics
{
    /// <summary>
    /// Collectible provides a unified collectible utility that works with both collider callbacks and raycast zones.
    /// Can give items to an inventory or award currency when collected.
    /// Supports 2D and 3D gameplay, optional layer/tag filters, visual effects, and persistent collection state.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Common Mechanics/Signalia | Collectible")]
    public class Collectible : MonoBehaviour
    {
        public enum TriggerSpace
        {
            Space3D,
            Space2D
        }

        public enum TriggerMode
        {
            Collider,
            Raycast
        }

        public enum CollectibleType
        {
            Item,
            Currency
        }

        public enum EffectMode
        {
            None,
            Pooled,
            Local
        }

        [Header("Trigger Behaviour")]
        [SerializeField] private TriggerMode triggerMode = TriggerMode.Collider;
        [SerializeField] private TriggerSpace triggerSpace = TriggerSpace.Space3D;

        /// <summary>
        /// Current trigger mode (Collider or Raycast).
        /// </summary>
        public TriggerMode Mode => triggerMode;

        /// <summary>
        /// Current physics space (2D or 3D).
        /// </summary>
        public TriggerSpace Space => triggerSpace;

        /// <summary>
        /// Indicates whether the editor should render gizmos for the raycast zone.
        /// </summary>
        public bool DrawsGizmos() => drawDebugGizmos;

        [Tooltip("Local space size of the raycast box. For 2D mode only X/Y are used.")]
        [SerializeField] private Vector3 raycastBoxSize = Vector3.one;
        [Tooltip("Local space offset from the transform where the raycast box is evaluated.")]
        [SerializeField] private Vector3 raycastBoxOffset = Vector3.zero;
        [Tooltip("Draw the configured raycast box in the Scene view for easier authoring.")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.25f);

        [Tooltip("Only allow objects on specific layers to collect this item.")]
        [SerializeField] private bool useLayerFilter = false;
        [SerializeField] private LayerMask allowedLayers = ~0;
        [Tooltip("Only allow objects with a specific tag to collect this item.")]
        [SerializeField] private bool useTagFilter = false;
        [SerializeField] private string requiredTag = "Player";

        [SerializeField] private CollectibleType collectibleType = CollectibleType.Item;

        [Tooltip("The item definition to give when collected.")]
        [SerializeField] private ItemSO itemDefinition;
        [Tooltip("Quantity of the item to give.")]
        [SerializeField] private int itemQuantity = 1;
        [Tooltip("The target GameInventory to add the item to. If null, will search for one on the collecting object.")]
        [SerializeField] private GameInventory targetInventory;
        [Tooltip("Auto-save the inventory after adding the item.")]
        [SerializeField] private bool autoSaveInventory = true;

        [Tooltip("The name of the currency to award.")]
        [SerializeField] private string currencyName = "gold";
        [Tooltip("Amount of currency to award.")]
        [SerializeField] private float currencyAmount = 10f;
        [Tooltip("Auto-save the currency after modification.")]
        [SerializeField] private bool autoSaveCurrency = true;
        [Tooltip("Notify listeners about the currency change.")]
        [SerializeField] private bool notifyCurrencyChange = true;

        [SerializeField] private EffectMode effectMode = EffectMode.None;
        [Tooltip("Prefab to spawn from pool when collected.")]
        [SerializeField] private GameObject pooledEffectPrefab;
        [Tooltip("Lifetime of the pooled effect (-1 for manual control).")]
        [SerializeField] private float pooledEffectLifetime = 2f;
        [Tooltip("Offset for the spawned effect position.")]
        [SerializeField] private Vector3 effectOffset = Vector3.zero;
        [Tooltip("Local effect object to enable when collected. Will be played if it has a ParticleSystem.")]
        [SerializeField] private GameObject localEffectObject;
        [Tooltip("Play particle systems on the local effect object.")]
        [SerializeField] private bool playLocalParticles = true;

        [SerializeField] private string collectAudio = string.Empty;

        [Tooltip("Visual object to disable. If null, uses the first child.")]
        [SerializeField] private GameObject visualObject;
        [Tooltip("When true, the visual object is hidden immediately upon collection (before the disable delay).")]
        [SerializeField] private bool hideVisualImmediately = true;
        [Tooltip("Delay before disabling this GameObject after collection. Useful for letting local effects play before the object is disabled or returns to the pool.")]
        [SerializeField] private float disableGameObjectDelay = 0f;

        [Tooltip("Remember that this collectible was collected across sessions.")]
        [SerializeField] private bool persistentCollection = false;
        [Tooltip("Unique key for persistent collection state. Click Generate to create one.")]
        [SerializeField] private string persistentSaveKey = string.Empty;

        private bool hasBeenCollected = false;
        private bool isPersistentlyCollected = false;
        private readonly HashSet<GameObject> trackedObjects = new HashSet<GameObject>();
        private readonly HashSet<GameObject> raycastResults = new HashSet<GameObject>();
        private Tween delayedDisableTween;

        /// <summary>
        /// Generates a unique key for persistent save state usage.
        /// </summary>
        public string GeneratePersistentKey() => $"col_{Guid.NewGuid():N}";

        /// <summary>
        /// Gets the collectible type.
        /// </summary>
        public CollectibleType Type => collectibleType;

        /// <summary>
        /// Returns true if this collectible has been collected (this session or persistently).
        /// </summary>
        public bool IsCollected => hasBeenCollected;

        private void Awake()
        {
            CheckPersistentState();
        }

        /// <summary>
        /// Checks if this collectible was persistently collected and updates state accordingly.
        /// </summary>
        private void CheckPersistentState()
        {
            if (persistentCollection && !string.IsNullOrWhiteSpace(persistentSaveKey))
            {
                bool alreadyCollected = GameSaving.Load(persistentSaveKey,
                    ConfigReader.GetConfig().SavingSystem.SettingsFileName, false);
                if (alreadyCollected)
                {
                    isPersistentlyCollected = true;
                    hasBeenCollected = true;
                    DisableCollectible(forceDisableGameObject: true);
                }
            }
            else if (persistentCollection)
            {
                Debug.LogWarning("[Collectible] Persistent collection is enabled but no save key is provided.", this);
            }
        }

        private void OnEnable()
        {
            // Check persistent state first
            CheckPersistentState();

            // If persistently collected, stay disabled
            if (isPersistentlyCollected)
            {
                DisableCollectible(forceDisableGameObject: true);
                return;
            }

            // Reset state for pooled/re-enabled collectibles
            ResetCollectibleState();
        }

        private void OnDisable()
        {
            delayedDisableTween?.Kill();
            delayedDisableTween = null;
            trackedObjects.Clear();
            raycastResults.Clear();
        }

        /// <summary>
        /// Resets the collectible state so it can be collected again.
        /// Called automatically on enable for pooled collectibles.
        /// Can also be called manually if needed.
        /// </summary>
        public void ResetCollectibleState()
        {
            // Don't reset if persistently collected
            if (isPersistentlyCollected)
            {
                return;
            }

            delayedDisableTween?.Kill();
            delayedDisableTween = null;

            // Track if we're resetting after a previous collection (for pooling)
            bool wasCollected = hasBeenCollected;

            hasBeenCollected = false;
            trackedObjects.Clear();
            raycastResults.Clear();

            // Re-enable visual object if it was disabled (either by DisableVisualOnly mode or hideVisualImmediately)
            if (wasCollected)
            {
                GameObject toEnable = GetVisualObject();
                if (toEnable != null) toEnable.SetActive(true);
            }

            // Only reset local effect object if this collectible was previously collected.
            // This prevents ambient particles from being disabled on initial scene load,
            // while still properly resetting for pooled/reused collectibles.
            if (wasCollected && localEffectObject != null)
            {
                localEffectObject.SetActive(false);
            }
        }

        /// <summary>
        /// Manually resets this collectible, including clearing any persistent state.
        /// Use this if you want a persistently collected item to be collectable again.
        /// </summary>
        public void ForceReset()
        {
            // Clear persistent state if it exists
            if (persistentCollection && !string.IsNullOrWhiteSpace(persistentSaveKey))
            {
                GameSaving.Save(persistentSaveKey, false, ConfigReader.GetConfig().SavingSystem.SettingsFileName);
            }
            isPersistentlyCollected = false;
            ResetCollectibleState();
        }

        private void FixedUpdate()
        {
            if (!isActiveAndEnabled || hasBeenCollected)
            {
                return;
            }

            if (triggerMode == TriggerMode.Raycast)
            {
                EvaluateRaycastZone();
            }
        }

        private void EvaluateRaycastZone()
        {
            raycastResults.Clear();

            if (triggerSpace == TriggerSpace.Space3D)
            {
                Vector3 center = transform.TransformPoint(raycastBoxOffset);
                Vector3 halfExtents = new Vector3(
                    Mathf.Max(0.001f, raycastBoxSize.x * 0.5f),
                    Mathf.Max(0.001f, raycastBoxSize.y * 0.5f),
                    Mathf.Max(0.001f, raycastBoxSize.z * 0.5f));
                int mask = useLayerFilter ? allowedLayers.value : Physics.AllLayers;
                Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, mask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hit = hits[i];
                    if (hit == null)
                    {
                        continue;
                    }

                    GameObject target = hit.gameObject;
                    if (!IsValidCollector(target))
                    {
                        continue;
                    }

                    raycastResults.Add(target);
                    ProcessCollection(target);
                    if (hasBeenCollected) return;
                }
            }
            else
            {
                Vector3 worldCenter = transform.TransformPoint(raycastBoxOffset);
                Vector2 center = new Vector2(worldCenter.x, worldCenter.y);
                Vector2 size = new Vector2(Mathf.Max(0.001f, raycastBoxSize.x), Mathf.Max(0.001f, raycastBoxSize.y));
                float angle = transform.eulerAngles.z;
                int mask = useLayerFilter ? allowedLayers.value : Physics2D.DefaultRaycastLayers;
                Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, mask);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D hit = hits[i];
                    if (hit == null)
                    {
                        continue;
                    }

                    GameObject target = hit.gameObject;
                    if (!IsValidCollector(target))
                    {
                        continue;
                    }

                    raycastResults.Add(target);
                    ProcessCollection(target);
                    if (hasBeenCollected) return;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space3D || hasBeenCollected)
            {
                return;
            }

            ProcessCollection(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space2D || hasBeenCollected)
            {
                return;
            }

            ProcessCollection(other.gameObject);
        }

        private void ProcessCollection(GameObject collector)
        {
            if (collector == null || !IsValidCollector(collector) || hasBeenCollected)
            {
                return;
            }

            bool success = false;

            switch (collectibleType)
            {
                case CollectibleType.Item:
                    success = CollectItem(collector);
                    break;
                case CollectibleType.Currency:
                    success = CollectCurrency();
                    break;
            }

            if (success)
            {
                hasBeenCollected = true;
                PlayCollectionEffects();
                PersistCollectionState();
                DisableCollectible();
            }
        }

        private bool CollectItem(GameObject collector)
        {
            if (itemDefinition == null)
            {
                Debug.LogWarning("[Collectible] No item definition assigned.", this);
                return false;
            }

            GameInventory inventory = targetInventory;

            // If no target inventory specified, try to find one on the collector
            if (inventory == null)
            {
                inventory = collector.GetComponent<GameInventory>();
                if (inventory == null)
                {
                    inventory = collector.GetComponentInParent<GameInventory>();
                }
            }

            if (inventory == null)
            {
                Debug.LogWarning("[Collectible] No GameInventory found to add item to.", this);
                return false;
            }

            inventory.AddItem(itemDefinition, itemQuantity);

            if (autoSaveInventory && inventory.IsPersistent)
            {
                inventory.Inventory?.SaveToDisk();
            }

            return true;
        }

        private bool CollectCurrency()
        {
            if (string.IsNullOrEmpty(currencyName))
            {
                Debug.LogWarning("[Collectible] No currency name specified.", this);
                return false;
            }

            // Use the cached currency modification - no disk I/O unless autoSave is enabled
            CMN_Currencies.ModifyCurrency(currencyName, currencyAmount, autoSaveCurrency, notifyCurrencyChange);

            return true;
        }

        private void PlayCollectionEffects()
        {
            // Play audio
            if (collectAudio.HasValue())
            {
                SIGS.PlayAudio(collectAudio);
            }

            // Handle effects based on mode
            switch (effectMode)
            {
                case EffectMode.Pooled:
                    SpawnPooledEffect();
                    break;
                case EffectMode.Local:
                    ActivateLocalEffect();
                    break;
            }
        }

        private void SpawnPooledEffect()
        {
            if (pooledEffectPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = transform.position + effectOffset;
            GameObject spawned = SIGS.PoolingGet(pooledEffectPrefab, pooledEffectLifetime, true);
            if (spawned != null)
            {
                spawned.transform.position = spawnPosition;
                spawned.transform.rotation = transform.rotation;

                // Auto-play particle systems
                foreach (var ps in spawned.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps?.Play(true);
                }
            }
        }

        private void ActivateLocalEffect()
        {
            if (localEffectObject == null)
            {
                return;
            }

            localEffectObject.SetActive(true);

            if (playLocalParticles)
            {
                foreach (var ps in localEffectObject.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps?.Play(true);
                }
            }
        }

        private void DisableCollectible(bool forceDisableGameObject = false)
        {
            delayedDisableTween?.Kill();
            delayedDisableTween = null;

            if (forceDisableGameObject)
            {
                gameObject.SetActive(false);
                return;
            }

            // Hide visual immediately if requested (works with both modes)
            if (hideVisualImmediately)
            {
                GameObject toDisable = GetVisualObject();
                if (toDisable != null) toDisable.SetActive(false);
            }

            // Handle disable delay (works with both modes now)
            float delay = Mathf.Max(0f, disableGameObjectDelay);
            if (delay <= 0f)
            {
                gameObject.SetActive(false);
            }
            else
            {
                delayedDisableTween = SIGS.DoIn(delay, () =>
                {
                    if (this != null && gameObject != null)
                    {
                        gameObject.SetActive(false);
                    }
                }, unscaled: false);
            }
        }

        private GameObject GetVisualObject()
        {
            if (visualObject != null)
            {
                return visualObject;
            }

            if (transform.childCount > 0)
            {
                return transform.GetChild(0).gameObject;
            }

            return null;
        }

        private void PersistCollectionState()
        {
            if (!persistentCollection)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(persistentSaveKey))
            {
                Debug.LogWarning("[Collectible] Persistent collection is enabled but no save key is provided.", this);
                return;
            }

            GameSaving.Save(persistentSaveKey, true, ConfigReader.GetConfig().SavingSystem.SettingsFileName);
        }

        private bool IsValidCollector(GameObject target)
        {
            if (target == null || target == gameObject)
            {
                return false;
            }

            if (target.transform != null && target.transform.IsChildOf(transform))
            {
                return false;
            }

            if (useLayerFilter && (allowedLayers.value & (1 << target.layer)) == 0)
            {
                return false;
            }

            if (useTagFilter && !string.IsNullOrEmpty(requiredTag) && !target.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            raycastBoxSize.x = Mathf.Max(0f, raycastBoxSize.x);
            raycastBoxSize.y = Mathf.Max(0f, raycastBoxSize.y);
            raycastBoxSize.z = Mathf.Max(0f, raycastBoxSize.z);
            itemQuantity = Mathf.Max(1, itemQuantity);
            pooledEffectLifetime = Mathf.Max(-1f, pooledEffectLifetime);
            disableGameObjectDelay = Mathf.Max(0f, disableGameObjectDelay);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos || triggerMode != TriggerMode.Raycast)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Quaternion rotation = triggerSpace == TriggerSpace.Space3D
                ? transform.rotation
                : Quaternion.Euler(0f, 0f, transform.eulerAngles.z);
            Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(raycastBoxOffset), rotation, Vector3.one);

            if (triggerSpace == TriggerSpace.Space3D)
            {
                Gizmos.DrawCube(Vector3.zero, new Vector3(raycastBoxSize.x, raycastBoxSize.y, raycastBoxSize.z));
            }
            else
            {
                Gizmos.DrawCube(Vector3.zero, new Vector3(raycastBoxSize.x, raycastBoxSize.y, 0.05f));
            }

            Gizmos.matrix = previousMatrix;
        }
    }
}
