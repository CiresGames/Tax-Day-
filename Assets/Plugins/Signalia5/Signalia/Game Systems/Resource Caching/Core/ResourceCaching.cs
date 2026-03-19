using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.ResourceCaching
{
    /// <summary>
    /// Manages resource caching using ResourceAsset ScriptableObjects.
    /// Provides efficient resource loading by pre-caching assets with string identifiers.
    /// </summary>
    public static class ResourceCachingManager
    {
        private static ResourceAsset[] _resourceAssets;
        private static Dictionary<string, Object> _globalCache;
        private static bool _initialized = false;

        /// <summary>
        /// Initializes the resource caching system by loading all ResourceAssets from the config.
        /// Called automatically by GameSystemsHandler during Watchman.Awake().
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            Watchman.Watch();
            
            // Get ResourceAssets from config
            var config = ConfigReader.GetConfig();
            if (config != null && config.ResourceAssets != null && config.ResourceAssets.Length > 0)
            {
                // Filter out null references
                var validAssets = config.ResourceAssets.Where(asset => asset != null).ToArray();
                if (validAssets.Length > 0)
                {
                    _resourceAssets = validAssets;
                }
            }

            // If config has no ResourceAssets, try to auto-populate them
            if (config != null && (config.ResourceAssets == null || config.ResourceAssets.Length == 0))
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Signalia Resource Caching] No ResourceAssets assigned in config. Attempting to find and assign them automatically. " +
                               "Use 'Tools > Signalia > Load Resource Assets' to populate them manually.");
                LoadResourceAssets();
                
                // Try again after auto-population
                if (config.ResourceAssets != null && config.ResourceAssets.Length > 0)
                {
                    var validAssets = config.ResourceAssets.Where(asset => asset != null).ToArray();
                    if (validAssets.Length > 0)
                    {
                        _resourceAssets = validAssets;
                    }
                }
#else
                Debug.LogWarning("[Signalia Resource Caching] No ResourceAssets assigned in config. " +
                               "Use 'Tools > Signalia > Load Resource Assets' in the editor to populate them.");
#endif
            }

            // Fallback to Resources.LoadAll if still no assets
            if (_resourceAssets == null || _resourceAssets.Length == 0)
            {
                _resourceAssets = Resources.LoadAll<ResourceAsset>(FrameworkConstants.PATH_RESOURCE);
            }

            // Build global cache from all ResourceAssets
            _globalCache = new Dictionary<string, Object>();
            if (_resourceAssets != null)
            {
                foreach (var resourceAsset in _resourceAssets)
                {
                    if (resourceAsset != null)
                    {
                        foreach (var kvp in resourceAsset.ResourceDictionary)
                        {
                            if (!_globalCache.ContainsKey(kvp.Key))
                            {
                                _globalCache[kvp.Key] = kvp.Value;
                            }
                            else
                            {
                                // Duplicate key found, using first occurrence
                            }
                        }
                    }
                }
            }

            _initialized = true;
            
            if (_resourceAssets == null || _resourceAssets.Length == 0)
            {
                Debug.LogWarning("[Signalia Resource Caching] No ResourceAssets found. Please create ResourceAssets and assign them to the config.");
            }
        }

        /// <summary>
        /// Gets a cached resource by its key.
        /// </summary>
        /// <typeparam name="T">The type of resource to retrieve</typeparam>
        /// <param name="key">The key identifying the resource</param>
        /// <returns>The cached resource or null if not found</returns>
        public static T GetResource<T>(string key) where T : Object
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (_globalCache.TryGetValue(key, out var resource))
            {
                if (resource is T typedResource)
                {
                    return typedResource;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a resource exists for the given key.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the resource exists</returns>
        public static bool HasResource(string key)
        {
            if (!_initialized)
            {
                Initialize();
            }

            return !string.IsNullOrEmpty(key) && _globalCache.ContainsKey(key);
        }

        /// <summary>
        /// Gets all available resource keys.
        /// </summary>
        /// <returns>Array of all resource keys</returns>
        public static string[] GetAllKeys()
        {
            if (!_initialized)
            {
                Initialize();
            }

            return _globalCache.Keys.ToArray();
        }

        /// <summary>
        /// Gets the number of cached resources.
        /// </summary>
        /// <returns>Number of cached resources</returns>
        public static int GetCacheSize()
        {
            if (!_initialized)
            {
                Initialize();
            }

            return _globalCache.Count;
        }

        /// <summary>
        /// Clears the resource cache and resets the initialization state.
        /// Called by GameSystemsHandler during cleanup.
        /// </summary>
        public static void Clear()
        {
            _globalCache?.Clear();
            _resourceAssets = null;
            _initialized = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Loads ResourceAssets from the Resources/Signalia folder and assigns them to the config.
        /// This is called automatically when no ResourceAssets are found in the config.
        /// </summary>
        /// <returns>True if resource assets were found and assigned, false otherwise</returns>
        private static bool LoadResourceAssets()
        {
            return ResourceHandler.LoadResourceAssets();
        }
#endif

        /// <summary>
        /// Legacy ResourceCache class for backward compatibility.
        /// </summary>
        [System.Obsolete("Use ResourceCachingManager instead. This class will be removed in future versions.")]
        public static class ResourceCache
        {
            private static readonly Dictionary<string, Object> _cache = new();

            /// <summary>
            /// Loads and caches a resource of type T from the given path.
            /// </summary>
            /// <typeparam name="T">Type of asset to load</typeparam>
            /// <param name="path">Resource path</param>
            /// <returns>Cached or newly loaded resource</returns>
            public static T LoadFromCache<T>(string path) where T : Object
            {
                if (_cache.TryGetValue(path, out var cachedObj) && cachedObj is T typedObj)
                    return typedObj;

                T loaded = Resources.Load<T>(path);
                if (loaded != null)
                    _cache[path] = loaded;

                return loaded;
            }

            public static T LoadFromCache<T>(string path, bool forceReload) where T : Object
            {
                if (forceReload)
                {
                    if (_cache.TryGetValue(path, out var cachedObj))
                        _cache.Remove(path);
                }
                return LoadFromCache<T>(path);
            }

            /// <summary>
            /// Clears the entire resource cache.
            /// </summary>
            public static void Clear()
            {
                _cache.Clear();
            }
        }
    }
}
