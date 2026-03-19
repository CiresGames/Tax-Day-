using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.PackageHandlers;
using AHAKuo.Signalia.Radio;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.PoolingSystem
{
    /// <summary>
    /// A simple auxiliary script that calls and spawns objects using the pooling system.
    /// Features spawn object from array, randomizable with chances, spawn into target transform position or Vector3 space, lifetime option, and after spawn event.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Pooling/Signalia | Pooling Caller")]
    public class PoolingSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnableObject
        {
            [SerializeField] public GameObject prefab;
            [SerializeField, Range(0f, 1f)] public float spawnChance = 1f;

            public GameObject Prefab => prefab;
            public float SpawnChance => spawnChance;
        }

        [SerializeField] private List<SpawnableObject> spawnableObjects = new();
        [SerializeField] private bool useRandomization = true;
        [SerializeField] private int spawnCount = 1;

        public enum SpawnMode { Target, WorldPosition, Here }

        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 worldPosition = Vector3.zero;
        [SerializeField] private SpawnMode spawnMode = SpawnMode.Here;
        [SerializeField] private bool spawnAsChild = false;
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        // Reset options when spawning as child
        [SerializeField] private bool resetPosition = false;
        [SerializeField] private bool resetRotation = false;
        [SerializeField] private bool resetScale = false;

        // Rotation options
        [SerializeField] private bool overrideRotation = false;
        [SerializeField] private bool useRotationOffset = true;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        // Scale options
        [SerializeField] private bool useScaleOverride = false;
        [SerializeField] private Vector3 scaleOverride = Vector3.one;

        // Fill target by name options
        [SerializeField] private bool fillTargetByName = false;
        [SerializeField] private bool useLiveKey = true;
        [SerializeField] private string targetNameKey = "";

        [SerializeField, Min(-1f)] private float lifetime = -1f;
        [SerializeField, Range(0f, 100f)] private float cooldown = 0f;
        [SerializeField] private bool spawnEnabled = true;
        [SerializeField] private string afterSpawnEvent = "";
        [SerializeField] private string listenerEvent = "";

        [SerializeField] private bool enableWarming = false;
        [SerializeField, Min(1)] private int warmupCount = 5;
        [SerializeField] private bool warmupOnStart = true;
        [SerializeField] private bool warmupOnAwake = false;

        private Listener listenerDisposable;

        private void Awake()
        {
            if (enableWarming && warmupOnAwake)
            {
                WarmupPools();
            }
        }

        private void Start()
        {
            InitializeListener();
            
            if (enableWarming && warmupOnStart)
            {
                WarmupPools();
            }

            // Handle Fill Target By Name functionality
            if (fillTargetByName && !string.IsNullOrEmpty(targetNameKey))
            {
                FillTargetTransformByName();
            }
        }

        private void OnDestroy()
        {
            CleanupListener();
        }

        /// <summary>
        /// Initializes the SIGS.Listener to respond to the configured event
        /// </summary>
        private void InitializeListener()
        {
            if (!string.IsNullOrEmpty(listenerEvent))
            {
                listenerDisposable = SIGS.Listener(listenerEvent, OnListenerEvent);
            }
        }

        /// <summary>
        /// Cleans up the SIGS.Listener when the component is destroyed
        /// </summary>
        private void CleanupListener()
        {
            listenerDisposable?.Dispose();
            listenerDisposable = null;
        }

        /// <summary>
        /// Event handler for SIGS.Listener - automatically spawns objects when the event is received
        /// </summary>
        private void OnListenerEvent()
        {
            Spawn();
        }

        /// <summary>
        /// Spawns objects based on the configuration. Uses randomization if enabled.
        /// </summary>
        public void Spawn()
        {
            if (spawnableObjects == null || spawnableObjects.Count == 0)
            {
                Debug.LogWarning($"[PoolingSpawner] No spawnable objects configured on {gameObject.name}");
                return;
            }

            // Check cooldown gate
            if (!SIGS.CooldownGate($"PoolingSpawner_{gameObject.GetInstanceID()}", cooldown))
            {
                return;
            }

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject prefabToSpawn = SelectPrefabToSpawn();
                if (prefabToSpawn == null) continue;

                GameObject spawnedObject = prefabToSpawn.FromPool(lifetime, spawnEnabled);
                if (spawnedObject == null) continue;

                SetSpawnedObjectPosition(spawnedObject);
                FireAfterSpawnEvent(spawnedObject);
            }
        }

        /// <summary>
        /// Spawns a specific prefab by index, bypassing randomization.
        /// </summary>
        /// <param name="prefabIndex">Index of the prefab in the spawnableObjects list</param>
        public void SpawnSpecific(int prefabIndex)
        {
            if (spawnableObjects == null || prefabIndex < 0 || prefabIndex >= spawnableObjects.Count)
            {
                Debug.LogWarning($"[PoolingSpawner] Invalid prefab index {prefabIndex} on {gameObject.name}");
                return;
            }

            // Check cooldown gate
            if (!SIGS.CooldownGate($"PoolingSpawner_{gameObject.GetInstanceID()}", cooldown))
            {
                return;
            }

            var spawnable = spawnableObjects[prefabIndex];
            if (spawnable.Prefab == null)
            {
                Debug.LogWarning($"[PoolingSpawner] Prefab at index {prefabIndex} is null on {gameObject.name}");
                return;
            }

            GameObject spawnedObject = spawnable.Prefab.FromPool(lifetime, spawnEnabled);
            if (spawnedObject == null) return;

            SetSpawnedObjectPosition(spawnedObject);
            FireAfterSpawnEvent(spawnedObject);
        }

        /// <summary>
        /// Spawns a specific prefab by reference, bypassing randomization.
        /// </summary>
        /// <param name="prefab">The prefab to spawn</param>
        public void SpawnSpecific(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolingSpawner] Prefab is null on {gameObject.name}");
                return;
            }

            // Check cooldown gate
            if (!SIGS.CooldownGate($"PoolingSpawner_{gameObject.GetInstanceID()}", cooldown))
            {
                return;
            }

            GameObject spawnedObject = prefab.FromPool(lifetime, spawnEnabled);
            if (spawnedObject == null) return;

            SetSpawnedObjectPosition(spawnedObject);
            FireAfterSpawnEvent(spawnedObject);
        }

        private GameObject SelectPrefabToSpawn()
        {
            if (!useRandomization)
            {
                // Return first valid prefab
                foreach (var spawnable in spawnableObjects)
                {
                    if (spawnable.Prefab != null) return spawnable.Prefab;
                }
                return null;
            }

            // Use SIGS.ThrowDice for weighted random selection
            var validSpawnables = new List<SpawnableObject>();
            foreach (var spawnable in spawnableObjects)
            {
                if (spawnable.Prefab != null && spawnable.SpawnChance > 0f)
                {
                    validSpawnables.Add(spawnable);
                }
            }

            if (validSpawnables.Count == 0) return null;

            // Use SIGS.ThrowDice to select based on individual chances
            foreach (var spawnable in validSpawnables)
            {
                if (SIGS.ThrowDice(spawnable.SpawnChance))
                {
                    return spawnable.Prefab;
                }
            }

            // If no dice roll succeeded, return the first valid prefab as fallback
            return validSpawnables[0].Prefab;
        }

        private void SetSpawnedObjectPosition(GameObject spawnedObject)
        {
            Vector3 targetPosition;
            Transform targetParent = null;

            switch (spawnMode)
            {
                case SpawnMode.Here:
                    // Spawn at this component's transform position
                    targetPosition = transform.position + positionOffset;
                    targetParent = spawnAsChild ? transform : null;
                    break;
                    
                case SpawnMode.Target:
                    if (targetTransform != null)
                    {
                        targetPosition = targetTransform.position + positionOffset;
                        targetParent = spawnAsChild ? targetTransform : null;
                    }
                    else
                    {
                        // Fallback to world position if target is null
                        targetPosition = worldPosition + positionOffset;
                    }
                    break;
                    
                case SpawnMode.WorldPosition:
                default:
                    targetPosition = worldPosition + positionOffset;
                    break;
            }

            // Set position
            spawnedObject.transform.position = targetPosition;
            
            // Handle rotation
            if (overrideRotation)
            {
                switch (spawnMode)
                {
                    case SpawnMode.Here:
                        if (useRotationOffset)
                        {
                            spawnedObject.transform.rotation = transform.rotation * Quaternion.Euler(rotationOffset);
                        }
                        else
                        {
                            spawnedObject.transform.rotation = Quaternion.Euler(rotationOffset);
                        }
                        break;
                        
                    case SpawnMode.Target:
                        if (targetTransform != null)
                        {
                            if (useRotationOffset)
                            {
                                spawnedObject.transform.rotation = targetTransform.rotation * Quaternion.Euler(rotationOffset);
                            }
                            else
                            {
                                spawnedObject.transform.rotation = Quaternion.Euler(rotationOffset);
                            }
                        }
                        else
                        {
                            // Fallback to absolute rotation if target is null
                            spawnedObject.transform.rotation = Quaternion.Euler(rotationOffset);
                        }
                        break;
                        
                    case SpawnMode.WorldPosition:
                    default:
                        // World position mode - use absolute rotation
                        spawnedObject.transform.rotation = Quaternion.Euler(rotationOffset);
                        break;
                }
            }
            // If overrideRotation is false, rotation is left as default (prefab's original rotation)
            
            // Handle scale
            if (useScaleOverride)
            {
                spawnedObject.transform.localScale = scaleOverride;
            }
            
            // Set parent and handle reset options
            if (targetParent != null)
            {
                spawnedObject.transform.SetParent(targetParent);
                
                // Apply reset options when spawning as child
                if (resetPosition)
                {
                    spawnedObject.transform.localPosition = Vector3.zero;
                }
                if (resetRotation)
                {
                    spawnedObject.transform.localRotation = Quaternion.identity;
                }
                if (resetScale)
                {
                    spawnedObject.transform.localScale = Vector3.one;
                }
            }
        }

        private void FireAfterSpawnEvent(GameObject spawnedObject)
        {
            if (!string.IsNullOrEmpty(afterSpawnEvent))
            {
                afterSpawnEvent.SendEvent(spawnedObject);
            }
        }

        /// <summary>
        /// Adds a spawnable object to the list
        /// </summary>
        public void AddSpawnableObject(GameObject prefab, float spawnChance = 1f)
        {
            if (prefab == null) return;

            spawnableObjects.Add(new SpawnableObject
            {
                prefab = prefab,
                spawnChance = spawnChance,
            });
        }

        /// <summary>
        /// Removes a spawnable object from the list by prefab reference
        /// </summary>
        public void RemoveSpawnableObject(GameObject prefab)
        {
            spawnableObjects.RemoveAll(x => x.Prefab == prefab);
        }

        /// <summary>
        /// Clears all spawnable objects
        /// </summary>
        public void ClearSpawnableObjects()
        {
            spawnableObjects.Clear();
        }

        /// <summary>
        /// Gets the current spawnable objects count
        /// </summary>
        public int SpawnableObjectsCount => spawnableObjects?.Count ?? 0;

        /// <summary>
        /// Gets a spawnable object by index
        /// </summary>
        public SpawnableObject GetSpawnableObject(int index)
        {
            if (spawnableObjects == null || index < 0 || index >= spawnableObjects.Count)
                return null;
            return spawnableObjects[index];
        }

        /// <summary>
        /// Warms up all configured pools with the specified warmup count
        /// </summary>
        public void WarmupPools()
        {
            if (!enableWarming || spawnableObjects == null || spawnableObjects.Count == 0)
            {
                return;
            }

            foreach (var spawnable in spawnableObjects)
            {
                if (spawnable.Prefab != null)
                {
                    spawnable.Prefab.WarmupPool(warmupCount);
                }
            }
        }

        /// <summary>
        /// Warms up a specific pool by prefab index
        /// </summary>
        /// <param name="prefabIndex">Index of the prefab in the spawnableObjects list</param>
        /// <param name="count">Number of objects to warm up (uses warmupCount if not specified)</param>
        public void WarmupPool(int prefabIndex, int count = -1)
        {
            if (!enableWarming || spawnableObjects == null || prefabIndex < 0 || prefabIndex >= spawnableObjects.Count)
            {
                return;
            }

            var spawnable = spawnableObjects[prefabIndex];
            if (spawnable.Prefab != null)
            {
                int warmupAmount = count > 0 ? count : warmupCount;
                spawnable.Prefab.WarmupPool(warmupAmount);
            }
        }

        /// <summary>
        /// Warms up a specific pool by prefab reference
        /// </summary>
        /// <param name="prefab">The prefab to warm up</param>
        /// <param name="count">Number of objects to warm up (uses warmupCount if not specified)</param>
        public void WarmupPool(GameObject prefab, int count = -1)
        {
            if (!enableWarming || prefab == null)
            {
                return;
            }

            int warmupAmount = count > 0 ? count : warmupCount;
            prefab.WarmupPool(warmupAmount);
        }

        /// <summary>
        /// Enables warming and warms up all pools
        /// </summary>
        public void EnableWarmingAndWarmup()
        {
            enableWarming = true;
            WarmupPools();
        }

        /// <summary>
        /// Disables warming
        /// </summary>
        public void DisableWarming()
        {
            enableWarming = false;
        }

        /// <summary>
        /// Gets whether warming is currently enabled
        /// </summary>
        public bool IsWarmingEnabled => enableWarming;

        /// <summary>
        /// Gets the current warmup count
        /// </summary>
        public int WarmupCount => warmupCount;

        /// <summary>
        /// Fills the target transform reference using LiveKey or DeadKey system
        /// </summary>
        private void FillTargetTransformByName()
        {
            if (string.IsNullOrEmpty(targetNameKey))
            {
                Debug.LogWarning($"[PoolingSpawner] Target name key is empty on {gameObject.name}");
                return;
            }

            Transform foundTransform = null;

            if (useLiveKey)
            {
                // Try to get from LiveKey system
                if (SIGS.LiveKeyExists(targetNameKey))
                {
                    foundTransform = SIGS.GetLiveValue<Transform>(targetNameKey);
                }
            }
            else
            {
                // Try to get from DeadKey system
                if (SIGS.DeadKeyExists(targetNameKey))
                {
                    foundTransform = SIGS.GetDeadValue<Transform>(targetNameKey);
                }
            }

            if (foundTransform != null)
            {
                targetTransform = foundTransform;
                Debug.Log($"[PoolingSpawner] Successfully filled target transform '{foundTransform.name}' using {(useLiveKey ? "LiveKey" : "DeadKey")} '{targetNameKey}' on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[PoolingSpawner] Could not find transform using {(useLiveKey ? "LiveKey" : "DeadKey")} '{targetNameKey}' on {gameObject.name}");
            }
        }
    }
}
