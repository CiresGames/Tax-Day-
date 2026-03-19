using AHAKuo.Signalia.Framework;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AHAKuo.Signalia.GameSystems.PoolingSystem
{
    /// <summary>
    /// Signalia's object pooling system. Provides on-demand pooling, optional warmup, and component caching.
    /// </summary>
    public static class Pooling
    {
        public static readonly Dictionary<string, Pool> pooledObjects = new();

        private static Pool EnsurePool(GameObject sourcePrefab)
        {
            Watchman.Watch();

            var fullName = sourcePrefab.name;

            if (!pooledObjects.TryGetValue(fullName, out var pool) || pool.Null())
            {
                pool = new Pool(sourcePrefab);
                pooledObjects[fullName] = pool;
            }

            return pool;
        }

        /// <summary>
        /// Retrieves a pooled GameObject. Optionally auto-deactivates after `lifetime` seconds.
        ///
        /// Example:
        /// <code>
        /// var bullet = prefab.FromPool(2f, true);
        /// bullet.transform.position = firePoint.position;
        /// </code>
        /// </summary>
        public static GameObject Get(GameObject sourcePrefab, float lifetime = -1f, bool enabled = true)
        {
            var pool = EnsurePool(sourcePrefab);
            return pool.GetOne(lifetime, enabled);
        }

        /// <summary>
        /// Retrieves a list of pooled GameObjects.
        ///
        /// Example:
        /// <code>
        /// var enemies = enemyPrefab.FromPool(5, 10f, true);
        /// foreach (var e in enemies)
        ///     e.transform.position = GetRandomSpawnPoint();
        /// </code>
        /// </summary>
        public static List<GameObject> Get(GameObject sourcePrefab, int count, float lifetime = -1f, bool enabled = true)
        {
            var pool = EnsurePool(sourcePrefab);
            var results = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                results.Add(pool.GetOne(lifetime, enabled));
            }

            return results;
        }

        /// <summary>
        /// Pre-allocates a pool with inactive instances of the prefab. Optional and should be called once per prefab.
        ///
        /// Example:
        /// <code>
        /// explosionPrefab.WarmupPool(10); // Prepares 10 inactive objects for later use
        /// </code>
        /// </summary>
        public static void Warmup(GameObject sourcePrefab, int count)
        {
            var pool = EnsurePool(sourcePrefab);
            pool.Warmup(count);
        }

        /// <summary>
        /// Checks whether the pool has at least the requested number of active instances.
        /// </summary>
        /// <param name="sourcePrefab">Prefab whose pool should be inspected.</param>
        /// <param name="count">Minimum number of active instances required.</param>
        public static bool ActiveCount(GameObject sourcePrefab, int count = 1)
        {
            if (sourcePrefab == null)
                throw new ArgumentNullException(nameof(sourcePrefab));

            if (count <= 0)
                return true;

            if (!pooledObjects.TryGetValue(sourcePrefab.name, out var pool) || pool.Null())
                return false;

            return pool.ActiveInstanceCount() >= count;
        }

        /// <summary>
        /// Retrieves a pooled GameObject and caches specific components.
        /// Each component type can specify whether it should be searched from children.
        ///
        /// Example:
        /// <code>
        /// var (obj, comps) = projectilePrefab.FromPool(3f, true,
        ///     (typeof(ProjectileScript), false),
        ///     (typeof(ParticleSystem), true));
        ///
        /// comps.GetCached&lt;ProjectileScript&gt;()?.Launch();
        /// comps.GetCached&lt;ParticleSystem&gt;()?.Play();
        /// </code>
        /// </summary>
        public static (GameObject gameObject, Dictionary<Type, Component> compCache) Get(GameObject sourcePrefab, float lifetime, bool enabled, params (Type type, bool fromChildren)[] types)
        {
            var pool = EnsurePool(sourcePrefab);
            return pool.GetOneWithTypes(lifetime, enabled, types);
        }

        /// <summary>
        /// Retrieves multiple pooled GameObjects with cached components per object.
        /// Each component type can specify whether it should be searched from children.
        ///
        /// Example:
        /// <code>
        /// var batch = enemyPrefab.FromPool(3, 5f, true,
        ///     (typeof(EnemyAI), false),
        ///     (typeof(Animator), true));
        ///
        /// foreach (var (obj, comps) in batch)
        ///     comps.GetCached&lt;EnemyAI&gt;()?.Initialize();
        /// </code>
        /// </summary>
        public static List<(GameObject gameObject, Dictionary<Type, Component> compCache)> Get(GameObject sourcePrefab, int count, float lifetime, bool enabled, params (Type type, bool fromChildren)[] types)
        {
            var pool = EnsurePool(sourcePrefab);
            var results = new List<(GameObject, Dictionary<Type, Component>)>(count);
            for (int i = 0; i < count; i++)
                results.Add(pool.GetOneWithTypes(lifetime, enabled, types));
            return results;
        }

        /// <summary>
        /// Helper method to retrieve a cached component safely from the pool dictionary.
        /// </summary>
        public static T TryGetCached<T>(Dictionary<Type, Component> cache) where T : Component
        {
            return cache.TryGetValue(typeof(T), out var comp) ? comp as T : null;
        }

        /// <summary>
        /// Clears all pooled object registries.
        /// </summary>
        public static void ClearPools()
        {
            pooledObjects?.Clear();
        }
    }
}
