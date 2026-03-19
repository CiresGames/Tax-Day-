using AHAKuo.Signalia.Framework;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AHAKuo.Signalia.GameSystems.PoolingSystem
{
    public struct Pool
    {
        public GameObject sourcePrefab;
        private List<(GameObject obj, Dictionary<Type, Component> cachedComponents, float creationTime)> pooledObjects;
        private Transform parentTransform;
        private bool hasWarmup;

        /// <summary>
        /// Called only once when the source prefab is created in the pool dictionary. If it was created via a different name, a new pool will be created.
        /// </summary>
        /// <param name="sourcePrefab"></param>
        public Pool(GameObject sourcePrefab)
        {
            this.sourcePrefab = sourcePrefab;
            pooledObjects = new();
            hasWarmup = false;

            var parentName = $"Pool_{sourcePrefab.name}";
            var parentGO = new GameObject(parentName);
            parentGO.SetActive(false);
            parentTransform = parentGO.transform;

            var first = Object.Instantiate(sourcePrefab, parentTransform);
            pooledObjects.Add((first, new Dictionary<Type, Component>(), Time.time));
            first.SetActive(false);

            parentGO.SetActive(true);
        }

        public (GameObject, Dictionary<Type, Component>) GetOneWithTypes(float lifetime, bool enabled, params (Type type, bool fromChildren)[] typedRequests)
        {
            CleanDestroyedEntries();

            var config = ConfigReader.GetConfig().PoolingSystem;
            var index = pooledObjects.FindIndex(x => !x.obj.activeSelf);
            
            if (index == -1)
            {
                // No inactive objects available, need to create new one or recycle
                if (config.CeilingLimit > 0 && pooledObjects.Count >= config.CeilingLimit)
                {
                    if (config.EnableRecycling)
                    {
                        // Find the oldest active object to recycle
                        var oldestIndex = -1;
                        var oldestTime = float.MaxValue;
                        
                        for (int i = 0; i < pooledObjects.Count; i++)
                        {
                            var (_obj, _, creationTime) = pooledObjects[i];
                            if (_obj.activeSelf && creationTime < oldestTime)
                            {
                                oldestTime = creationTime;
                                oldestIndex = i;
                            }
                        }
                        
                        if (oldestIndex != -1)
                        {
                            // Recycle the oldest object
                            var (oldObj, oldCache, _) = pooledObjects[oldestIndex];
                            oldObj.SetActive(false);
                            pooledObjects[oldestIndex] = (oldObj, oldCache, Time.time); // Update creation time
                            index = oldestIndex;
                        }
                        else
                        {
                            // No active objects to recycle, ceiling reached and recycling enabled but no active objects
                            Debug.LogWarning($"[{sourcePrefab.name} Pool] Ceiling limit ({config.CeilingLimit}) reached, recycling enabled, but no active objects available to recycle. Cannot create new object.");
                            return (null, new Dictionary<Type, Component>());
                        }
                    }
                    else
                    {
                        // Ceiling reached but recycling disabled - cannot create new objects
                        Debug.LogWarning($"[{sourcePrefab.name} Pool] Ceiling limit ({config.CeilingLimit}) reached and recycling is disabled. Cannot create new object. Enable recycling in pooling settings to allow object reuse.");
                        return (null, new Dictionary<Type, Component>());
                    }
                }
                else
                {
                    // Under ceiling limit, create new object
                    var newObj = Object.Instantiate(sourcePrefab, parentTransform);
                    var compCache = new Dictionary<Type, Component>();
                    pooledObjects.Add((newObj, compCache, Time.time));
                    newObj.SetActive(false);
                    index = pooledObjects.Count - 1;
                }
            }

            var (obj, cache, _) = pooledObjects[index];
            obj.SetActive(enabled);

            foreach (var (type, fromChildren) in typedRequests)
            {
                if (!cache.ContainsKey(type))
                {
                    Component found = fromChildren
                        ? obj.GetComponentInChildren(type)
                        : obj.GetComponent(type);

                    if (found != null)
                        cache[type] = found;
                    else
                        Debug.LogWarning($"[{sourcePrefab.name} Pool] Component of type {type.Name} not found on {(fromChildren ? "children" : "object")}.");
                }
            }

            if (lifetime > 0f)
            {
                var waiter = SIGS.DoIn(lifetime, () =>
                {
                    if (obj != null) obj.SetActive(false);
                }, unscaled: false);

                if (ConfigReader.GetConfig().PoolingSystem.SmartPoolLifetimeKill)
                {
                    bool GotDisabled() => obj != null && !obj.activeSelf;
                    SIGS.DoWhen(GotDisabled, () => waiter?.Kill());
                }
            }

            return (obj, cache);
        }

        public GameObject GetOne(float lifetime = -1f, bool enabled = true)
        {
            return GetOneWithTypes(lifetime, enabled).Item1;
        }

        public int ActiveInstanceCount()
        {
            CleanDestroyedEntries();

            int active = 0;
            for (int i = 0; i < pooledObjects.Count; i++)
            {
                var (obj, _, _) = pooledObjects[i];
                if (obj != null && obj.activeInHierarchy)
                    active++;
            }

            return active;
        }

        /// <summary>
        /// Warmup silently, without invoking OnEnable of the pooled objects.
        /// </summary>
        /// <param name="count"></param>
        public void Warmup(int count)
        {
            CleanDestroyedEntries();

            if (hasWarmup)
            {
                Debug.LogWarning($"Warmup has already been called for {sourcePrefab.name}.");
                return;
            }

            int toCreate = count - pooledObjects.Count;

            // disable parent to avoid OnEnable calls
            parentTransform.gameObject.SetActive(false);

            for (int i = 0; i < toCreate; i++)
            {
                var instance = Object.Instantiate(sourcePrefab, parentTransform);
                instance.SetActive(false);
                pooledObjects.Add((instance, new Dictionary<Type, Component>(), Time.time));
            }

            // re-enable parent
            parentTransform.gameObject.SetActive(true);
            hasWarmup = true;
        }

        private void CleanDestroyedEntries()
        {
            int removed = pooledObjects.RemoveAll(x => x.obj == null);
            if (removed > 0)
            {
                Debug.LogWarning($"[{sourcePrefab.name} Pool] Removed {removed} destroyed object(s) from the pool. You should not destroy pooled objects manually.");
            }
        }

        public bool Null()
        {
            return pooledObjects == null;
        }
    }
}
